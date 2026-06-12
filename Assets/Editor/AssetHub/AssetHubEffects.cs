using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>
    /// 特效映射查询(SceneObjectParticle.json):模型 → 常驻(always)/按动作(action)特效。
    /// 对标老客户端 UIModelClass3D.GetObjectParticleData + LoadActionParticle 的查表逻辑;
    /// 特效资源 = resource/effect/objs/{EffectResType 目录}/{name}.lh(Laya 粒子系统,转换线待开)。
    /// </summary>
    public static class AssetHubEffects
    {
        public enum SrcState { Missing, Lfs, Ok }

        public sealed class EffectRef
        {
            public string Bone;
            public string Name;
            public string RelPath;   // resource/effect/objs/... 相对 cdn/resource
            public SrcState State;
        }

        public sealed class EffectInfo
        {
            public bool Supported;
            public string Section = "";
            public readonly List<EffectRef> Always = new List<EffectRef>();
            public readonly Dictionary<string, List<EffectRef>> Actions = new Dictionary<string, List<EffectRef>>();
            public readonly List<string> Variants = new List<string>();
            // 默认装套装维度(ConfigLogin.CreateRole):创角骨骼特效 + 套装武器的常驻特效
            public readonly List<EffectRef> CreateEffects = new List<EffectRef>();
            public readonly List<EffectRef> SetWeapon = new List<EffectRef>();
            public int ActionEffectTotal
            {
                get { int n = 0; foreach (var kv in Actions) n += kv.Value.Count; return n; }
            }
            public bool IsEmpty => Always.Count == 0 && ActionEffectTotal == 0
                && CreateEffects.Count == 0 && SetWeapon.Count == 0;
        }

        // 模型 module 目录 → (SceneObjectParticle 节名, 特效目录) —— 与 electron 工具端同一张表
        private static readonly Dictionary<string, (string section, string dir)> SECTION_BY_MODULE =
            new Dictionary<string, (string, string)>
            {
                ["role"] = ("Body", "role_effect"),
                ["weapon"] = ("Weapon", "weapon_effect"),
                ["wing"] = ("Wing", "wing_effect"),
                ["mount"] = ("Horse", "mount_effect"),
                ["back"] = ("BackOrnament", "back_effect"),
                ["monster"] = ("Monster", "monster_effect"),
                ["pet"] = ("Pet", "pet_effect"),
                ["god"] = ("God", "god_effect"),
                ["littlepet"] = ("Demon", "littlepet_effect"),
                ["spirit"] = ("Sprite", "spirit_effect"),
                ["fabao"] = ("FaBao", "fabao_effect"),
                ["ghost"] = ("Ghost", "ghost_effect"),
            };

        private static JObject _cfg;
        private static System.DateTime _cfgMTime;

        public static EffectInfo Query(AssetEntry e)
        {
            var info = new EffectInfo();
            string module = ModuleOf(e);
            if (module == null || !SECTION_BY_MODULE.TryGetValue(module, out (string section, string dir) map))
                return info;

            JObject cfg = LoadConfig();
            if (cfg == null) return info;
            info.Supported = true;
            info.Section = map.section;

            if (!(cfg[map.section] is JObject sec)) return info;
            string key = ModelKeyOf(e);
            foreach (KeyValuePair<string, JToken> kv in sec)
            {
                if (kv.Key.StartsWith(key + "_")) info.Variants.Add(kv.Key);
            }
            if (sec[key] is JObject entry)
            {
                Resolve(entry["always"] as JObject, map.dir, info.Always);
                if (entry["action"] is JObject actions)
                {
                    foreach (KeyValuePair<string, JToken> kv in actions)
                    {
                        var list = new List<EffectRef>();
                        Resolve(kv.Value as JObject, map.dir, list);
                        if (list.Count > 0) info.Actions[kv.Key] = list;
                    }
                }
            }
            if (module == "role") QueryCreateRoleSet(key, cfg, info);
            return info;
        }

        /// <summary>
        /// 默认装套装特效(ConfigLogin.CreateRole):该衣服若是某职业 role_res,
        /// 补上创角骨骼特效(Effect[career@sex].clothe,skills_effect)与套装武器的常驻特效
        /// (SceneObjectParticle.Weapon[weapon_res])——"默认装看着有特效但 Body 无记录"的真实来源。
        /// </summary>
        private static void QueryCreateRoleSet(string modelKey, JObject particleCfg, EffectInfo info)
        {
            string loginPath = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "client", "ConfigLogin.json");
            if (!File.Exists(loginPath)) return;
            JObject create;
            try { create = JObject.Parse(File.ReadAllText(loginPath))["CreateRole"] as JObject; }
            catch { return; }
            if (!(create?["Res"] is JObject res)) return;

            foreach (KeyValuePair<string, JToken> kv in res)
            {
                if (kv.Value?.Value<string>("role_res") != modelKey) continue;
                // 创角骨骼特效:{"clothe":{"root":"cj_1100"}},值是字符串(特效名),目录 skills_effect
                if (create["Effect"]?[kv.Key]?["clothe"] is JObject boneMap)
                {
                    foreach (KeyValuePair<string, JToken> b in boneMap)
                    {
                        string name = b.Value?.Value<string>();
                        if (string.IsNullOrEmpty(name)) continue;
                        info.CreateEffects.Add(MakeRef(b.Key, name, "skills_effect"));
                    }
                }
                string weaponRes = kv.Value.Value<string>("weapon_res");
                if (!string.IsNullOrEmpty(weaponRes)
                    && particleCfg["Weapon"]?[weaponRes]?["always"] is JObject weaponAlways)
                {
                    foreach (KeyValuePair<string, JToken> b in weaponAlways)
                    {
                        string name = (b.Value as JObject)?.Value<string>("name");
                        if (string.IsNullOrEmpty(name)) continue;
                        info.SetWeapon.Add(MakeRef(b.Key, name, "weapon_effect"));
                    }
                }
                break;
            }
        }

        private static EffectRef MakeRef(string bone, string name, string dir)
        {
            string rel = $"effect/objs/{dir}/{name}.lh";
            string abs = Path.Combine(LayaUISettings.CdnResourceRoot, rel);
            SrcState state = !File.Exists(abs) ? SrcState.Missing
                : AssetHubDomains.IsLfsPlaceholder(abs) ? SrcState.Lfs
                : SrcState.Ok;
            return new EffectRef { Bone = bone, Name = name, RelPath = "resource/" + rel, State = state };
        }

        private static void Resolve(JObject boneMap, string dir, List<EffectRef> output)
        {
            if (boneMap == null) return;
            foreach (KeyValuePair<string, JToken> kv in boneMap)
            {
                // 同骨骼可单个对象或数组(LoadActionParticle 两种都吃)
                IEnumerable<JToken> vos = kv.Value is JArray arr ? (IEnumerable<JToken>)arr : new[] { kv.Value };
                foreach (JToken vo in vos)
                {
                    string name = (vo as JObject)?.Value<string>("name");
                    if (string.IsNullOrEmpty(name)) continue;
                    output.Add(MakeRef(kv.Key, name, dir));
                }
            }
        }

        /// <summary>OutDir 约定 Assets/GameRes/object/{module}/{name} → module。</summary>
        private static string ModuleOf(AssetEntry e)
        {
            string[] parts = (e.OutDir ?? "").Replace('\\', '/').Split('/');
            return parts.Length >= 4 && parts[2] == "object" ? parts[3] : null;
        }

        /// <summary>配置键 = 文件名最后一段数字 id(model_clothe_1201 → 1201)。</summary>
        private static string ModelKeyOf(AssetEntry e)
        {
            string stem = Path.GetFileNameWithoutExtension(e.LhPath);
            int us = stem.LastIndexOf('_');
            return us >= 0 ? stem.Substring(us + 1) : stem;
        }

        private static JObject LoadConfig()
        {
            string path = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "client", "SceneObjectParticle.json");
            if (!File.Exists(path)) return null;
            System.DateTime mtime = File.GetLastWriteTimeUtc(path);
            if (_cfg != null && mtime == _cfgMTime) return _cfg;
            try
            {
                _cfg = JObject.Parse(File.ReadAllText(path));
                _cfgMTime = mtime;
            }
            catch { _cfg = null; }
            return _cfg;
        }

        public static string StateIcon(SrcState s)
        {
            switch (s)
            {
                case SrcState.Ok: return "✅";
                case SrcState.Lfs: return "⚠";
                default: return "❌";
            }
        }
    }
}
