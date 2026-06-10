using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// Tools/LayaUI/analyze_layaui.py 产出的 ui_manifest.json 的数据模型。
    /// 决策含义见该脚本头注释。
    /// </summary>
    public class LayaUIManifest
    {
        [JsonProperty("version")] public int Version = 0;
        [JsonProperty("designWidth")] public int DesignWidth = 720;
        [JsonProperty("designHeight")] public int DesignHeight = 1280;
        [JsonProperty("moduleDirCase")] public Dictionary<string, string> ModuleDirCase = new Dictionary<string, string>();
        [JsonProperty("scenes")] public Dictionary<string, SceneEntry> Scenes = new Dictionary<string, SceneEntry>();

        public class SceneEntry
        {
            [JsonProperty("module")] public string Module = "";
            [JsonProperty("name")] public string Name = "";
            [JsonProperty("json")] public string Json = "";
            [JsonProperty("tsClass")] public string TsClass = null;
            [JsonProperty("kind")] public string Kind = "";
            [JsonProperty("decision")] public string Decision = "";
            [JsonProperty("inlineHost")] public string InlineHost = null;
            [JsonProperty("inlineItems")] public List<string> InlineItems = new List<string>();
            [JsonProperty("ownerClasses")] public List<string> OwnerClasses = new List<string>();
            [JsonProperty("missingSkins")] public List<string> MissingSkins = new List<string>();
            [JsonProperty("skinSource")] public Dictionary<string, string> SkinSource = new Dictionary<string, string>();
        }

        private static LayaUIManifest _cached;

        public static LayaUIManifest Load(bool force = false)
        {
            if (_cached != null && !force) return _cached;
            string path = Path.Combine(Directory.GetCurrentDirectory(), LayaUISettings.MANIFEST_PATH);
            if (!File.Exists(path))
            {
                Debug.LogError("[LayaUI] 找不到 manifest,先跑 python3 Tools/LayaUI/analyze_layaui.py。 " + path);
                return null;
            }
            _cached = JsonConvert.DeserializeObject<LayaUIManifest>(File.ReadAllText(path));
            return _cached;
        }

        /// <summary>scene key("module/Name") -> entry,找不到返回 null。</summary>
        public SceneEntry Get(string key)
        {
            SceneEntry e;
            return Scenes.TryGetValue(key, out e) ? e : null;
        }

        /// <summary>按 ts 类名反查 scene key(inlineHost 用)。</summary>
        public string FindSceneKeyByClass(string tsClass)
        {
            if (string.IsNullOrEmpty(tsClass)) return null;
            foreach (KeyValuePair<string, SceneEntry> kv in Scenes)
            {
                if (kv.Value.TsClass == tsClass) return kv.Key;
            }
            return null;
        }

        public string ModuleDir(string module)
        {
            string dir;
            if (ModuleDirCase != null && ModuleDirCase.TryGetValue(module, out dir)) return dir;
            return char.ToUpperInvariant(module[0]) + module.Substring(1);
        }
    }
}
