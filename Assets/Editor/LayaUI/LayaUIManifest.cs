using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            /// <summary>节点名 -> TS 里静态解析出的运行时图(scene 里 skin 为空时烘焙用)。</summary>
            [JsonProperty("bakedSkins")] public Dictionary<string, string> BakedSkins = new Dictionary<string, string>();
            /// <summary>被 TS this.xxx 引用的非下划线节点(Bind 收集 = "_"前缀 ∪ codeNodes)。</summary>
            [JsonProperty("codeNodes")] public List<string> CodeNodes = new List<string>();

            // ---- 根锚定推导链的三个输入(analyze_layaui.py 从 TS 源码静态提取)----
            // 这三个字段【必须】默认 null,不能像上面那样初始化成 new List/new JObject:
            // 转换器要靠 null 区分"生成器还没产出这个字段"(→ 沿用旧的居中兜底)与
            // "提取到了、但链上确实没有根锚"(→ 修正成左上绝对定位)。初始化成空集合会把前者
            // 误判成后者,一次性把 500 多个本来就对的 view 全改错。

            /// <summary>TS 继承链,顺序为【自身类 → 根基类】,例如
            /// ["BagSmeltView","BaseView1","UIEffect","BaseComponent","BaseClass","HashObject"]。
            /// 未提取到为 null(不是空数组)。</summary>
            [JsonProperty("tsChain")] public List<string> TsChain = null;

            /// <summary>从 TS 源码静态提取的 display_obj 根定位赋值(子类覆写层),可能的键:
            /// left/right/top/bottom/centerX/centerY/scaleX/scaleY(数值)、safeAreaTop(布尔)。
            /// 未提取到为 null。safeAreaTop=true 表示原本是 top=Util.GetLiuhaiHeight(),已按
            /// 手册铁律折算成 0(安全区统一交给 Unity 的 SafeAreaRoot,不镜像老端硬编码的 60)。</summary>
            [JsonProperty("rootLayout")] public JObject RootLayout = null;

            /// <summary>is_center 沿继承链求得的生效值。true/false 是静态可判定;
            /// 字符串(如 "dynamic",指 tooltip 那批构造期 true、SetData 里按入参切 false 的类)
            /// 表示运行时切换、不可静态折叠;未提取到为 null。</summary>
            [JsonProperty("isCenter")] public JToken IsCenter = null;

            /// <summary>同一个 .scene 被多个 TS 类共用、且各自算出来的根锚定【不一致】时,这里列出
            /// 冲突的类名(≥2 个);无冲突为 null。rootLayout 里那份只是取值顺序上先到的一个,
            /// 不代表裁决结果——转换器遇到它必须维持现状 + 告警,交人工到 ui_root_layouts.json 定夺,
            /// 否则等于拿一个类的锚去套另一个类的界面。</summary>
            [JsonProperty("rootLayoutConflict")] public List<string> RootLayoutConflict = null;
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
