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
            public int ActionEffectTotal
            {
                get { int n = 0; foreach (var kv in Actions) n += kv.Value.Count; return n; }
            }
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
            if (!(sec[key] is JObject entry)) return info;

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
            return info;
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
                    string rel = $"effect/objs/{dir}/{name}.lh";
                    string abs = Path.Combine(LayaUISettings.CdnResourceRoot, rel);
                    SrcState state = !File.Exists(abs) ? SrcState.Missing
                        : AssetHubDomains.IsLfsPlaceholder(abs) ? SrcState.Lfs
                        : SrcState.Ok;
                    output.Add(new EffectRef { Bone = kv.Key, Name = name, RelPath = "resource/" + rel, State = state });
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
