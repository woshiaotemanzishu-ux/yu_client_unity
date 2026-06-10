using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 核心转换器:cdn 运行时 scene json -> UGUI prefab。
    /// 粒度按 ui_manifest.json 决策:窗口/独立组件出 prefab,inline item 进宿主的
    /// __Templates 节点(禁用),shared item 先转成共享 prefab 再嵌套进宿主。
    /// 布局换算全部走 LayaRectMath,皮肤全部走 LayaSpriteImporter。
    /// </summary>
    public static class LayaSceneConverter
    {
        // 已处理过的布局属性(其余属性记入"未映射"报告)
        private static readonly HashSet<string> HandledProps = new HashSet<string>
        {
            "name", "x", "y", "width", "height", "left", "right", "top", "bottom",
            "centerX", "centerY", "anchorX", "anchorY", "pivotX", "pivotY",
            "scaleX", "scaleY", "rotation", "alpha", "visible", "zOrder",
            "skin", "sizeGrid", "texture", "mouseEnabled", "mouseThrough", "hitTestPrior",
            "text", "fontSize", "color", "align", "valign", "bold", "italic", "underline",
            "stroke", "strokeColor", "leading", "wordWrap", "overflow", "font",
            "innerHTML", "prompt", "promptColor", "maxChars", "multiline", "type", "padding",
            "repeatX", "repeatY", "spaceX", "spaceY", "space", "elasticEnabled",
            "vScrollBarSkin", "hScrollBarSkin", "selectEnable", "selectedIndex",
            "sceneColor", "sceneBg", "cacheAs", "label", "var", "renderType",
            "autoDestroyAtClosed", "hideSlider", "disabled", "gray", "child",
        };

        public static void ConvertModule(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err)) { Debug.LogError("[LayaUI] " + err); return; }

            LayaSpriteImporter.ResetCache();
            LayaUIReport report = new LayaUIReport(module);
            int done = 0;
            try
            {
                List<string> keys = new List<string>();
                foreach (KeyValuePair<string, LayaUIManifest.SceneEntry> kv in manifest.Scenes)
                {
                    LayaUIManifest.SceneEntry e = kv.Value;
                    if (e.Module != module) continue;
                    if (e.Decision == "view-prefab" || e.Decision == "standalone-prefab" || e.Decision == "shared-prefab")
                        keys.Add(kv.Key);
                }
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("LayaUI 转换 " + module, keys[i], (float)i / keys.Count);
                    ConvertOne(keys[i], manifest, report, new HashSet<string>());
                    done++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                LayaSpriteImporter.ResetCache();
            }
            report.Save();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LayaUI] 模块 " + module + " 转换完成,共 " + done + " 个 prefab。缺图 " + report.MissingCount +
                      " 处,详见报告。之后请编译通过后执行『回填 Bind 引用』。");
        }

        public static void ConvertSingle(string sceneKey)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            LayaUIManifest.SceneEntry e = manifest.Get(sceneKey);
            if (e == null) { Debug.LogError("[LayaUI] manifest 里没有 " + sceneKey); return; }
            LayaUIReport report = new LayaUIReport(e.Module);
            LayaSpriteImporter.ResetCache();
            try { ConvertOne(sceneKey, manifest, report, new HashSet<string>()); }
            finally { LayaSpriteImporter.ResetCache(); }
            report.Save();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>转换一个 scene 为 prefab,返回 prefab 资产路径。</summary>
        private static string ConvertOne(string sceneKey, LayaUIManifest manifest, LayaUIReport report, HashSet<string> stack)
        {
            LayaUIManifest.SceneEntry entry = manifest.Get(sceneKey);
            if (entry == null) return null;
            string prefabPath = PrefabPath(entry, manifest);
            if (stack.Contains(sceneKey)) return prefabPath; // 防环
            stack.Add(sceneKey);

            JObject root = LoadSceneJson(entry);
            if (root == null)
            {
                report.BeginScene(sceneKey);
                report.Note("❌ 运行时 json 读取失败: " + entry.Json);
                return null;
            }

            report.BeginScene(sceneKey);
            GameObject go = BuildRoot(entry.Name, root, manifest, report);

            // 内联 item:挂在禁用的 __Templates 下,业务代码用 _tpl_xxx 字段 Instantiate
            List<string> inline = entry.InlineItems;
            List<GameObject> templates = new List<GameObject>();
            if (inline != null && inline.Count > 0)
            {
                foreach (string itemKey in inline)
                {
                    LayaUIManifest.SceneEntry ie = manifest.Get(itemKey);
                    JObject ij = ie != null ? LoadSceneJson(ie) : null;
                    if (ij == null) { report.Note("内联 item 读不到 json: " + itemKey); continue; }
                    GameObject item = BuildRoot(ie.Name, ij, manifest, report);
                    templates.Add(item);
                    // item 自己的内联链(item 套 item)
                    if (ie.InlineItems != null)
                    {
                        foreach (string sub in ie.InlineItems)
                        {
                            LayaUIManifest.SceneEntry se = manifest.Get(sub);
                            JObject sj = se != null ? LoadSceneJson(se) : null;
                            if (sj == null) continue;
                            templates.Add(BuildRoot(se.Name, sj, manifest, report));
                        }
                    }
                }
            }
            // 共享 item:先保证共享 prefab 存在,再嵌套进 __Templates
            if (entry.TsClass != null)
            {
                foreach (KeyValuePair<string, LayaUIManifest.SceneEntry> kv in manifest.Scenes)
                {
                    LayaUIManifest.SceneEntry se = kv.Value;
                    if (se.Decision != "shared-prefab" || se.OwnerClasses == null) continue;
                    if (!se.OwnerClasses.Contains(entry.TsClass)) continue;
                    string sharedPath = ConvertOne(kv.Key, manifest, report, stack);
                    GameObject sharedAsset = sharedPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(sharedPath) : null;
                    if (sharedAsset == null) continue;
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(sharedAsset);
                    templates.Add(inst);
                }
            }
            if (templates.Count > 0)
            {
                GameObject tplRoot = new GameObject("__Templates", typeof(RectTransform));
                tplRoot.transform.SetParent(go.transform, false);
                foreach (GameObject t in templates) t.transform.SetParent(tplRoot.transform, false);
                tplRoot.SetActive(false);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            LayaBindGenerator.Generate(entry, manifest, prefabPath, report);
            return prefabPath;
        }

        public static string PrefabPath(LayaUIManifest.SceneEntry entry, LayaUIManifest manifest)
        {
            return LayaUISettings.PREFAB_ROOT + "/" + manifest.ModuleDir(entry.Module) + "/" + entry.Name + ".prefab";
        }

        private static JObject LoadSceneJson(LayaUIManifest.SceneEntry entry)
        {
            string path = Path.Combine(LayaUISettings.ClientRoot, entry.Json.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return null;
            try { return JObject.Parse(File.ReadAllText(path)); }
            catch (System.Exception e) { Debug.LogError("[LayaUI] 解析失败 " + path + ": " + e.Message); return null; }
        }

        private static GameObject BuildRoot(string name, JObject root, LayaUIManifest manifest, LayaUIReport report)
        {
            JObject props = root["props"] as JObject ?? new JObject();
            float w = LayaRectMath.F(props, "width") ?? manifest.DesignWidth;
            float h = LayaRectMath.F(props, "height") ?? manifest.DesignHeight;

            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            // 根节点居中锚定;LoginView 这类窗口由 ViewManager 挂到全屏层下,居中即原 is_center 行为
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            float cx = LayaRectMath.F(props, "centerX") ?? 0f;
            float cy = LayaRectMath.F(props, "centerY") ?? 0f;
            rt.anchoredPosition = new Vector2(cx, -cy);

            JArray children = root["child"] as JArray;
            if (children != null)
            {
                foreach (JToken c in children)
                {
                    BuildNode((JObject)c, rt, report);
                }
                ApplyZOrder(rt, children);
            }
            CollectUnknownProps("View", props, report);
            return go;
        }

        private static void BuildNode(JObject node, RectTransform parent, LayaUIReport report)
        {
            string type = (string)node["type"] ?? "Box";
            JObject p = node["props"] as JObject ?? new JObject();
            string name = (string)p["name"];
            if (string.IsNullOrEmpty(name)) name = type;

            GameObject go;
            RectTransform childContainer; // 子节点挂到哪(Panel/List 有 Content)
            Vector2 size;

            switch (type)
            {
                case "Image":
                case "Sprite":
                case "CheckBox":
                    go = BuildImage(p, name, type, report, out size);
                    childContainer = (RectTransform)go.transform;
                    break;
                case "Label":
                case "Text":
                    go = BuildLabel(p, name, false, report, out size);
                    childContainer = (RectTransform)go.transform;
                    break;
                case "HTMLDivElement":
                    go = BuildLabel(p, name, true, report, out size);
                    childContainer = (RectTransform)go.transform;
                    break;
                case "TextInput":
                    go = BuildTextInput(p, name, report, out size);
                    childContainer = (RectTransform)go.transform;
                    break;
                case "List":
                    go = BuildList(p, name, report, out size, out childContainer);
                    break;
                case "Panel":
                    go = LayaUITemplates.Spawn("Panel", null);
                    go.name = name;
                    size = SizeOf(p, 100, 100);
                    childContainer = (RectTransform)go.transform.Find("Content");
                    break;
                case "HBox":
                case "VBox":
                    go = BuildBoxLayout(p, name, type == "HBox", report, out size);
                    childContainer = (RectTransform)go.transform;
                    break;
                default:
                    if (type != "Box" && type != "View" && type != "Scene")
                        report.Note("未支持的组件类型 `" + type + "`(节点 " + name + "),按空容器处理");
                    go = LayaUITemplates.Spawn("Box", null);
                    go.name = name;
                    size = SizeOf(p, 0, 0);
                    childContainer = (RectTransform)go.transform;
                    break;
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            LayaRectMath.Apply(rt, p, size);

            if ((LayaRectMath.F(p, "alpha") ?? 1f) < 1f)
            {
                go.AddComponent<CanvasGroup>().alpha = LayaRectMath.F(p, "alpha").Value;
            }
            JToken visible = p["visible"];
            if (visible != null && visible.Type == JTokenType.Boolean && !(bool)visible)
            {
                go.SetActive(false);
            }

            JArray children = node["child"] as JArray;
            if (children == null || children.Count == 0)
            {
                children = p["child"] as JArray; // 少数节点(如 Label)子级挂在 props.child
            }
            if (children != null && children.Count > 0)
            {
                foreach (JToken c in children) BuildNode((JObject)c, childContainer, report);
                ApplyZOrder(childContainer, children);
            }
            CollectUnknownProps(type, p, report);
        }

        private static GameObject BuildImage(JObject p, string name, string type, LayaUIReport report, out Vector2 size)
        {
            GameObject go = LayaUITemplates.Spawn("Image", null);
            go.name = name;
            Image img = go.GetComponent<Image>();

            string skin = (string)p["skin"] ?? (string)p["texture"];
            string sizeGrid = (string)p["sizeGrid"];
            Vector4 border = string.IsNullOrEmpty(sizeGrid) ? Vector4.zero : LayaRectMath.SizeGridToBorder(sizeGrid);

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(skin))
            {
                string assetPath = LayaSpriteImporter.EnsureSprite(skin, border, report);
                sprite = LayaSpriteImporter.LoadSprite(assetPath);
            }
            else
            {
                report.RuntimeAssigned(name, "Image 无 skin(代码运行时赋图),保留透明占位");
            }

            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
            else
            {
                img.enabled = false; // 占位:不画白块
            }
            img.raycastTarget = IsTrue(p, "mouseEnabled");

            // Laya 不写宽高 = 用贴图原始尺寸
            float w = LayaRectMath.F(p, "width") ?? (sprite != null ? sprite.rect.width : 100f);
            float h = LayaRectMath.F(p, "height") ?? (sprite != null ? sprite.rect.height : 100f);
            size = new Vector2(w, h);
            if (type == "CheckBox") report.Approx(name + " 是 CheckBox,只转出了底图,交互需手工补 Toggle");
            return go;
        }

        private static GameObject BuildLabel(JObject p, string name, bool html, LayaUIReport report, out Vector2 size)
        {
            GameObject go = LayaUITemplates.Spawn("Label", null);
            go.name = name;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();

            string text = (string)p["text"] ?? "";
            if (html)
            {
                string raw = (string)p["innerHTML"] ?? text;
                text = HtmlToTmp(raw);
                tmp.richText = true;
                if (!string.IsNullOrEmpty(raw) && raw.IndexOf('<') >= 0)
                    report.Approx(name + " HTML 富文本按近似规则转 TMP,需人工核对");
            }
            tmp.text = text;
            tmp.fontSize = LayaRectMath.F(p, "fontSize") ?? 24f;
            tmp.color = LayaRectMath.ParseColor((string)p["color"], Color.white);

            FontStyles style = FontStyles.Normal;
            if (IsTrue(p, "bold")) style |= FontStyles.Bold;
            if (IsTrue(p, "italic")) style |= FontStyles.Italic;
            if (IsTrue(p, "underline")) style |= FontStyles.Underline;
            tmp.fontStyle = style;

            tmp.alignment = MapAlign((string)p["align"], (string)p["valign"]);
            bool wrap = IsTrue(p, "wordWrap");
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            float? leading = LayaRectMath.F(p, "leading");
            if (leading.HasValue)
            {
                tmp.lineSpacing = leading.Value;
                report.Approx(name + " leading=" + leading.Value + " 直接映射 TMP lineSpacing,行距需核对");
            }

            float? stroke = LayaRectMath.F(p, "stroke");
            if (stroke.HasValue && stroke.Value > 0f)
            {
                Color sc = LayaRectMath.ParseColor((string)p["strokeColor"], Color.black);
                LayaTextStyles.ApplyOutline(tmp, sc, stroke.Value, report, name);
            }

            float? w = LayaRectMath.F(p, "width");
            float? h = LayaRectMath.F(p, "height");
            if (!w.HasValue || !h.HasValue)
            {
                Vector2 pref = tmp.GetPreferredValues(string.IsNullOrEmpty(text) ? "字" : text,
                    w ?? Mathf.Infinity, Mathf.Infinity);
                size = new Vector2(w ?? Mathf.Ceil(pref.x), h ?? Mathf.Ceil(pref.y));
            }
            else
            {
                size = new Vector2(w.Value, h.Value);
            }
            return go;
        }

        private static GameObject BuildTextInput(JObject p, string name, LayaUIReport report, out Vector2 size)
        {
            GameObject go = LayaUITemplates.Spawn("TextInput", null);
            go.name = name;
            TMP_InputField input = go.GetComponent<TMP_InputField>();
            Image bg = go.GetComponent<Image>();

            string skin = (string)p["skin"];
            if (!string.IsNullOrEmpty(skin))
            {
                string sizeGrid = (string)p["sizeGrid"];
                Vector4 border = string.IsNullOrEmpty(sizeGrid) ? Vector4.zero : LayaRectMath.SizeGridToBorder(sizeGrid);
                Sprite sp = LayaSpriteImporter.LoadSprite(LayaSpriteImporter.EnsureSprite(skin, border, report));
                if (sp != null)
                {
                    bg.sprite = sp;
                    bg.type = border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                }
            }
            else
            {
                bg.enabled = false;
            }

            input.text = (string)p["text"] ?? "";
            float fontSize = LayaRectMath.F(p, "fontSize") ?? 24f;
            Color color = LayaRectMath.ParseColor((string)p["color"], Color.white);
            TextMeshProUGUI textComp = input.textComponent as TextMeshProUGUI;
            if (textComp != null)
            {
                textComp.fontSize = fontSize;
                textComp.color = color;
                textComp.alignment = MapAlign((string)p["align"], (string)p["valign"]);
            }
            TextMeshProUGUI ph = input.placeholder as TextMeshProUGUI;
            if (ph != null)
            {
                ph.text = (string)p["prompt"] ?? "";
                ph.fontSize = fontSize;
                ph.color = LayaRectMath.ParseColor((string)p["promptColor"], new Color(0.6f, 0.6f, 0.6f, 0.75f));
                ph.alignment = MapAlign((string)p["align"], (string)p["valign"]);
            }
            float? maxChars = LayaRectMath.F(p, "maxChars");
            if (maxChars.HasValue) input.characterLimit = (int)maxChars.Value;
            if (IsTrue(p, "multiline")) input.lineType = TMP_InputField.LineType.MultiLineNewline;
            if ((string)p["type"] == "password") input.contentType = TMP_InputField.ContentType.Password;

            size = SizeOf(p, 200, 40);
            return go;
        }

        private static GameObject BuildList(JObject p, string name, LayaUIReport report, out Vector2 size, out RectTransform content)
        {
            GameObject go = LayaUITemplates.Spawn("List", null);
            go.name = name;
            ScrollRect sr = go.GetComponent<ScrollRect>();
            content = sr.content;

            float repeatX = LayaRectMath.F(p, "repeatX") ?? 1f;
            float repeatY = LayaRectMath.F(p, "repeatY") ?? 0f;
            // Laya: repeatX=1 纵向列表;repeatY=1 横向列表;都大于 1 是网格
            bool horizontal = repeatY == 1f && repeatX != 1f;
            sr.vertical = !horizontal;
            sr.horizontal = horizontal;
            if (repeatX > 1f && repeatY > 1f)
                report.Approx(name + " 是网格 List(repeatX=" + repeatX + ", repeatY=" + repeatY + "),滚动方向按纵向,需确认");

            float spaceX = LayaRectMath.F(p, "spaceX") ?? LayaRectMath.F(p, "space") ?? 0f;
            float spaceY = LayaRectMath.F(p, "spaceY") ?? LayaRectMath.F(p, "space") ?? 0f;
            if (spaceX != 0f || spaceY != 0f)
                report.Note(name + " List 间距 spaceX=" + spaceX + " spaceY=" + spaceY + "(留给运行时虚拟列表用)");

            size = SizeOf(p, 200, 200);
            return go;
        }

        private static GameObject BuildBoxLayout(JObject p, string name, bool horizontal, LayaUIReport report, out Vector2 size)
        {
            GameObject go = LayaUITemplates.Spawn("Box", null);
            go.name = name;
            HorizontalOrVerticalLayoutGroup lg = horizontal
                ? (HorizontalOrVerticalLayoutGroup)go.AddComponent<HorizontalLayoutGroup>()
                : go.AddComponent<VerticalLayoutGroup>();
            lg.spacing = LayaRectMath.F(p, "space") ?? 0f;
            lg.childControlWidth = false;
            lg.childControlHeight = false;
            lg.childForceExpandWidth = false;
            lg.childForceExpandHeight = false;
            string align = (string)p["align"];
            lg.childAlignment = align == "center" ? TextAnchor.UpperCenter
                : align == "right" || align == "bottom" ? TextAnchor.UpperRight
                : TextAnchor.UpperLeft;
            size = SizeOf(p, 100, 100);
            return go;
        }

        private static Vector2 SizeOf(JObject p, float defW, float defH)
        {
            return new Vector2(LayaRectMath.F(p, "width") ?? defW, LayaRectMath.F(p, "height") ?? defH);
        }

        private static bool IsTrue(JObject p, string key)
        {
            JToken t = p[key];
            return t != null && t.Type == JTokenType.Boolean && (bool)t;
        }

        private static TextAlignmentOptions MapAlign(string align, string valign)
        {
            int h = align == "center" ? 1 : align == "right" ? 2 : 0;
            int v = valign == "middle" ? 1 : valign == "bottom" ? 2 : 0;
            TextAlignmentOptions[,] map =
            {
                { TextAlignmentOptions.TopLeft, TextAlignmentOptions.Left, TextAlignmentOptions.BottomLeft },
                { TextAlignmentOptions.Top, TextAlignmentOptions.Center, TextAlignmentOptions.Bottom },
                { TextAlignmentOptions.TopRight, TextAlignmentOptions.Right, TextAlignmentOptions.BottomRight },
            };
            return map[h, v];
        }

        /// <summary>Laya zOrder 大的在上;按 (zOrder, 原序) 稳定重排兄弟节点。</summary>
        private static void ApplyZOrder(RectTransform parent, JArray children)
        {
            bool any = false;
            foreach (JToken c in children)
            {
                JObject p = c["props"] as JObject;
                if (p != null && p["zOrder"] != null) { any = true; break; }
            }
            if (!any) return;
            List<Transform> order = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++) order.Add(parent.GetChild(i));
            List<float> z = new List<float>();
            for (int i = 0; i < order.Count; i++)
            {
                JObject p = i < children.Count ? children[i]["props"] as JObject : null;
                z.Add(p != null ? (LayaRectMath.F(p, "zOrder") ?? 0f) : 0f);
            }
            // 稳定插入排序
            for (int i = 1; i < order.Count; i++)
            {
                Transform t = order[i]; float zi = z[i]; int j = i - 1;
                while (j >= 0 && z[j] > zi) { order[j + 1] = order[j]; z[j + 1] = z[j]; j--; }
                order[j + 1] = t; z[j + 1] = zi;
            }
            for (int i = 0; i < order.Count; i++) order[i].SetSiblingIndex(i);
        }

        private static string HtmlToTmp(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            string s = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<font[^>]*color\s*=\s*['""]?(#[0-9a-fA-F]{3,8})['""]?[^>]*>", "<color=$1>", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</font>", "</color>", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</?(div|p|span|img|a)[^>]*>", "", RegexOptions.IgnoreCase);
            return s;
        }

        private static void CollectUnknownProps(string type, JObject p, LayaUIReport report)
        {
            foreach (JProperty prop in p.Properties())
            {
                if (!HandledProps.Contains(prop.Name)) report.UnknownProp(type, prop.Name);
            }
        }
    }
}
