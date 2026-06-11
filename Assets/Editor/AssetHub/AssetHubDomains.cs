using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;
using UnityEditor;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>条目状态:列表徽标用。</summary>
    public enum EntryStatus
    {
        SourceMissing,  // ❌ 源 .lh 不存在
        SourceLfs,      // ⚠ 源是 LFS 占位(需在本机 git lfs pull)
        NotConverted,   // ⬜ 未转换
        Stale,          // 🔶 源比产物新,需重转
        Converted,      // ✅ 已转换
    }

    /// <summary>一条可管理的资源(目前=一个 model_id 的角色模型)。</summary>
    public sealed class AssetEntry
    {
        public string Id;                 // "1101"
        public string DisplayName;        // "剑客之殇"
        public int Career;
        public int Sex;
        public string Note;               // 来源备注(时装名全集/默认装职业)
        public string LhPath;             // 源 .lh 绝对路径
        public string ActionDir;          // 动作目录绝对路径(按职业共用)
        public string OutDir;             // "Assets/GameRes/object/role/model_clothe_1101"
        public string PrefabPath;         // OutDir/model_clothe_1101.prefab

        public string SearchText;         // 小写,Id+名字,供过滤
    }

    /// <summary>左侧栏的一个资源域:绑定数据源(配置表)与扫描逻辑。</summary>
    public sealed class AssetDomain
    {
        public string Name;
        public bool Enabled;
        public string DisabledNote;       // 未接入域的说明
        public Func<List<AssetEntry>> Scan;
    }

    /// <summary>
    /// 资源域定义与状态计算。原则:配置表=清单真相源,工具只读表、管资源。
    /// 路径约定全部来自老客户端(objs/model_clothe_{id}.lh、action/{career*1000+100})。
    /// </summary>
    public static class AssetHubDomains
    {
        public static readonly string[] CAREER_NAMES = { "?", "剑士", "武姬", "枪使", "弓手" };

        public static List<AssetDomain> Build()
        {
            return new List<AssetDomain>
            {
                new AssetDomain { Name = "角色时装", Enabled = true, Scan = ScanFashion },
                new AssetDomain { Name = "创角默认装", Enabled = true, Scan = ScanCreateRole },
                // 部件域:清单=objs 目录文件(目录即资产清单;明细配置表待配表线接入后补充名称等信息)
                new AssetDomain { Name = "头饰", Enabled = true, Scan = () => ScanObjsDir("head", "model_head_") },
                new AssetDomain { Name = "武器", Enabled = true, Scan = () => ScanObjsDir("weapon", "model_weapon_") },
                new AssetDomain { Name = "背饰", Enabled = true, Scan = () => ScanObjsDir("back", "model_back_") },
                new AssetDomain { Name = "翅膀", Enabled = true, Scan = () => ScanObjsDir("wing", "model_wing_") },
                new AssetDomain { Name = "坐骑", Enabled = false, DisabledNote = "待接入:mount(含骑乘组合逻辑,先验一个真实样本)" },
            };
        }

        // ---------- 扫描:角色时装(config_fashion_model.json,88 个 model_id) ----------
        private static List<AssetEntry> ScanFashion()
        {
            string path = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "server", "config_fashion_model.json");
            var byModel = new Dictionary<int, AssetEntry>();
            var order = new List<int>();
            JObject cfg = JObject.Parse(File.ReadAllText(path));
            foreach (KeyValuePair<string, JToken> kv in cfg)
            {
                var row = (JObject)kv.Value;
                int modelId = row.Value<int>("model_id");
                if (!byModel.TryGetValue(modelId, out AssetEntry e))
                {
                    e = NewRoleEntry(modelId, row.Value<int>("career"), row.Value<int>("sex"));
                    byModel[modelId] = e;
                    order.Add(modelId);
                }
                string name = row.Value<string>("name");
                if (!string.IsNullOrEmpty(name) && name != "[]")
                {
                    // 同 model 多品质名(剑客之殇·珍/优…):展示名取「·」前公共部分,全集进备注
                    if (string.IsNullOrEmpty(e.DisplayName)) e.DisplayName = name.Split('·')[0];
                    if (!e.Note.Contains(name)) e.Note += (e.Note.Length > 0 ? " / " : "") + name;
                }
            }
            return Finish(order.Select(id => byModel[id]));
        }

        // ---------- 扫描:创角默认装(ConfigLogin.CreateRole,4 职业) ----------
        private static List<AssetEntry> ScanCreateRole()
        {
            string path = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "client", "ConfigLogin.json");
            JObject cfg = JObject.Parse(File.ReadAllText(path));
            JObject create = (JObject)cfg["CreateRole"];
            var res = (JObject)create["Res"];
            var list = new List<AssetEntry>();
            foreach (JObject ui in (JArray)create["UI"])
            {
                int career = ui.Value<int>("career");
                int sex = ui.Value<int>("sex");
                JToken r = res[$"{career}@{sex}"];
                if (r == null) continue;
                AssetEntry e = NewRoleEntry(r.Value<int>("role_res"), career, sex);
                e.DisplayName = ui.Value<string>("name") + "·默认装";
                e.Note = $"ConfigLogin.CreateRole.Res[{career}@{sex}].role_res";
                list.Add(e);
            }
            return Finish(list);
        }

        private static AssetEntry NewRoleEntry(int modelId, int career, int sex)
        {
            // 角色动作目录按职业共用:career*1000+100(老客户端约定)
            AssetEntry e = NewObjectEntry("role", "model_clothe_" + modelId, (career * 1000 + 100).ToString());
            e.Id = modelId.ToString();
            e.Career = career;
            e.Sex = sex;
            return e;
        }

        /// <summary>object/{module} 的通用条目:源 objs/{name}.lh,动作 action/{actionDirName}。</summary>
        private static AssetEntry NewObjectEntry(string module, string name, string actionDirName)
        {
            string root = Path.Combine(LayaUISettings.CdnResourceRoot, "object", module);
            return new AssetEntry
            {
                Id = name,
                DisplayName = "",
                Career = 0,
                Sex = 0,
                Note = "",
                LhPath = Path.Combine(root, "objs", name + ".lh"),
                ActionDir = Path.Combine(root, "action", actionDirName),
                OutDir = $"Assets/GameRes/object/{module}/{name}",
                PrefabPath = $"Assets/GameRes/object/{module}/{name}/{name}.prefab",
            };
        }

        /// <summary>部件域扫描:objs/*.lh 即清单;动作目录=action/{id 后缀}(部件每 id 一个目录)。</summary>
        private static List<AssetEntry> ScanObjsDir(string module, string prefix)
        {
            string objsDir = Path.Combine(LayaUISettings.CdnResourceRoot, "object", module, "objs");
            var list = new List<AssetEntry>();
            if (!Directory.Exists(objsDir)) return list;
            foreach (string f in Directory.GetFiles(objsDir, "*.lh").OrderBy(p => p))
            {
                string name = Path.GetFileNameWithoutExtension(f);
                // 动作目录名 = 名字里最后一段数字 id(model_weapon_r_1100 → 1100)
                int us = name.LastIndexOf('_');
                string actionDirName = us >= 0 ? name.Substring(us + 1) : name;
                AssetEntry e = NewObjectEntry(module, name, actionDirName);
                e.DisplayName = name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;
                e.Note = "清单=objs 目录(明细配置待配表线)";
                list.Add(e);
            }
            return Finish(list);
        }

        private static List<AssetEntry> Finish(IEnumerable<AssetEntry> entries)
        {
            var list = entries.ToList();
            foreach (AssetEntry e in list)
            {
                if (string.IsNullOrEmpty(e.DisplayName)) e.DisplayName = "(无名)";
                e.SearchText = (e.Id + " " + e.DisplayName + " " + e.Note + " "
                    + CAREER_NAMES[Math.Clamp(e.Career, 0, 4)]).ToLowerInvariant();
            }
            return list;
        }

        // ---------- 状态 ----------

        public static EntryStatus GetStatus(AssetEntry e)
        {
            if (!File.Exists(e.LhPath)) return EntryStatus.SourceMissing;
            if (IsLfsPlaceholder(e.LhPath)) return EntryStatus.SourceLfs;
            string prefabAbs = AssetPathToAbs(e.PrefabPath);
            if (!File.Exists(prefabAbs)) return EntryStatus.NotConverted;
            return SourceMTime(e) > File.GetLastWriteTimeUtc(prefabAbs)
                ? EntryStatus.Stale
                : EntryStatus.Converted;
        }

        /// <summary>源最新改动时间:objs 下同 model 前缀的所有文件(.lh/.lm/.lmat/贴图)取最大。</summary>
        private static DateTime SourceMTime(AssetEntry e)
        {
            string dir = Path.GetDirectoryName(e.LhPath);
            string prefix = Path.GetFileNameWithoutExtension(e.LhPath);
            DateTime t = DateTime.MinValue;
            foreach (string f in Directory.GetFiles(dir, prefix + "*"))
            {
                DateTime ft = File.GetLastWriteTimeUtc(f);
                if (ft > t) t = ft;
            }
            return t;
        }

        public static bool IsLfsPlaceholder(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (info.Length > 300) return false;
                using var sr = new StreamReader(path);
                char[] head = new char[30];
                int n = sr.Read(head, 0, head.Length);
                return new string(head, 0, n).StartsWith("version https://git-lfs");
            }
            catch { return false; }
        }

        public static string AssetPathToAbs(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath));
        }

        public static string StatusIcon(EntryStatus s)
        {
            switch (s)
            {
                case EntryStatus.Converted: return "✅";
                case EntryStatus.Stale: return "🔶";
                case EntryStatus.NotConverted: return "⬜";
                case EntryStatus.SourceLfs: return "⚠";
                default: return "❌";
            }
        }

        public static string StatusText(EntryStatus s)
        {
            switch (s)
            {
                case EntryStatus.Converted: return "已转换";
                case EntryStatus.Stale: return "源已更新,需重转";
                case EntryStatus.NotConverted: return "未转换";
                case EntryStatus.SourceLfs: return "源是 LFS 占位(本机 git lfs pull 后刷新)";
                default: return "源 .lh 缺失";
            }
        }
    }
}
