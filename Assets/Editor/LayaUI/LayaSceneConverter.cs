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
    ///
    /// 两种产出粒度:
    ///  - 合并模式(推荐):一个模块(或 ui_groups.json 自定义的大 Panel)= 一个 prefab,
    ///    各窗口是其下的子节点(默认只激活第一个),Bind 组件挂在各窗口子根上。
    ///  - 单窗口模式:一个窗口 scene = 一个 prefab(保留,供共享组件与零散需求)。
    ///
    /// 列表项按 manifest 决策内联进窗口的 __Templates(禁用)节点;
    /// 布局换算全部走 LayaRectMath,皮肤全部走 LayaSpriteImporter。
    /// </summary>
    public static class LayaSceneConverter
    {
        // 当前正在转换的窗口/item 的烘焙皮肤表(节点名 -> 图路径,来自 TS 静态扫描)
        private static Dictionary<string, string> _bakedSkins;

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

        // ---------------------------------------------------------------- 入口

        /// <summary>合并模式:模块(按 ui_groups.json 分组,缺省整模块一组)→ 大 prefab。</summary>
        public static void ConvertModuleCombined(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err)) { Debug.LogError("[LayaUI] " + err); return; }

            LayaSpriteImporter.ResetCache();
            LayaUIReport report = new LayaUIReport(module);
            HashSet<string> stack = new HashSet<string>();
            try
            {
                List<string> leftovers;
                List<LayaUIGroups.Group> groups = LayaUIGroups.ForModule(module, manifest, out leftovers);
                int total = groups.Count + leftovers.Count, idx = 0;
                foreach (LayaUIGroups.Group g in groups)
                {
                    EditorUtility.DisplayProgressBar("LayaUI 合并转换 " + module, g.Name, (float)idx++ / total);
                    BuildGroupPrefab(g, manifest, report, stack);
                }
                foreach (string key in leftovers)
                {
                    EditorUtility.DisplayProgressBar("LayaUI 合并转换 " + module, key, (float)idx++ / total);
                    ConvertOne(key, manifest, report, stack);
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
            Debug.Log("[LayaUI] 模块 " + module + " 合并转换完成。缺图 " + report.MissingCount +
                      " 处,详见报告。编译通过后执行『回填 Bind 引用』。");
        }

        /// <summary>单窗口模式:模块内每个窗口一个 prefab(保留,零散需求用)。</summary>
        public static void ConvertModule(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err)) { Debug.LogError("[LayaUI] " + err); return; }

            LayaSpriteImporter.ResetCache();
            LayaUIReport report = new LayaUIReport(module);
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
            Debug.Log("[LayaUI] 模块 " + module + " 转换完成(单窗口模式)。缺图 " + report.MissingCount + " 处。");
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

        /// <summary>
        /// 组内重转单个窗口:在包含它的合并 prefab 里只替换该窗口的子树,
        /// 不动其他窗口(包括你手调过的)。
        /// </summary>
        public static void ReconvertWindowInGroup(string sceneKey)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            LayaUIManifest.SceneEntry entry = manifest.Get(sceneKey);
            if (entry == null) { Debug.LogError("[LayaUI] manifest 里没有 " + sceneKey); return; }

            string folder = LayaUISettings.PREFAB_ROOT + "/" + manifest.ModuleDir(entry.Module);
            string groupPath = FindGroupPrefabContaining(folder, entry.Name);
            if (groupPath == null)
            {
                Debug.LogError("[LayaUI] " + folder + " 下没有哪个合并 prefab 包含窗口 " + entry.Name +
                               ",先跑一次合并转换");
                return;
            }

            LayaUIReport report = new LayaUIReport(entry.Module);
            LayaSpriteImporter.ResetCache();
            GameObject root = PrefabUtility.LoadPrefabContents(groupPath);
            try
            {
                Transform old = root.transform.Find(entry.Name);
                int siblingIndex = old != null ? old.GetSiblingIndex() : root.transform.childCount;
                bool active = old != null && old.gameObject.activeSelf;
                if (old != null) Object.DestroyImmediate(old.gameObject);

                JObject json = LoadSceneJson(entry);
                if (json == null) { Debug.LogError("[LayaUI] 读不到 " + entry.Json); return; }
                GameObject win = BuildWindow(sceneKey, entry, json, manifest, report, new HashSet<string>());
                win.transform.SetParent(root.transform, false);
                win.transform.SetSiblingIndex(siblingIndex);
                win.SetActive(active);

                LayaBindGenerator.Generate(entry, manifest, win.transform, report);
                PrefabUtility.SaveAsPrefabAsset(root, groupPath);
                Debug.Log("[LayaUI] 已在 " + groupPath + " 内重转窗口 " + entry.Name);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                LayaSpriteImporter.ResetCache();
            }
            report.Save();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- 组装

        private static void BuildGroupPrefab(LayaUIGroups.Group group, LayaUIManifest manifest,
            LayaUIReport report, HashSet<string> stack)
        {
            if (group.Scenes.Count == 0) return;

            GameObject root = new GameObject(group.Name, typeof(RectTransform));
            RectTransform rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(manifest.DesignWidth, manifest.DesignHeight);

            string moduleDir = null;
            bool first = true;
            foreach (string key in group.Scenes)
            {
                LayaUIManifest.SceneEntry entry = manifest.Get(key);
                if (entry == null) continue;
                if (moduleDir == null) moduleDir = manifest.ModuleDir(entry.Module);
                JObject json = LoadSceneJson(entry);
                if (json == null)
                {
                    report.BeginScene(key);
                    report.Note("❌ 运行时 json 读取失败: " + entry.Json);
                    continue;
                }
                GameObject win = BuildWindow(key, entry, json, manifest, report, stack);
                win.transform.SetParent(root.transform, false);
                win.SetActive(first); // 默认只亮第一个窗口,其余在编辑器里手动切
                first = false;

                LayaBindGenerator.Generate(entry, manifest, win.transform, report);

                // 清掉以前单窗口模式留下的同名 prefab,避免新旧两套并存
                string oldPath = PrefabPath(entry, manifest);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(oldPath) != null)
                {
                    AssetDatabase.DeleteAsset(oldPath);
                    report.Note("删除旧单窗口 prefab: " + oldPath + "(已并入 " + group.Name + ")");
                }
            }

            string prefabPath = LayaUISettings.PREFAB_ROOT + "/" + (moduleDir ?? "Unknown") + "/" + group.Name + ".prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>转换一个 scene 为独立 prefab(共享组件、组外窗口、单窗口模式用)。</summary>
        private static string ConvertOne(string sceneKey, LayaUIManifest manifest, LayaUIReport report, HashSet<string> stack)
        {
            LayaUIManifest.SceneEntry entry = manifest.Get(sceneKey);
            if (entry == null) return null;
            string prefabPath = PrefabPath(entry, manifest);
            if (stack.Contains(sceneKey)) return prefabPath; // 防环/防重复
            stack.Add(sceneKey);

            JObject root = LoadSceneJson(entry);
            if (root == null)
            {
                report.BeginScene(sceneKey);
                report.Note("❌ 运行时 json 读取失败: " + entry.Json);
                return null;
            }

            GameObject go = BuildWindow(sceneKey, entry, root, manifest, report, stack);
            LayaBindGenerator.Generate(entry, manifest, go.transform, report);

            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefabPath;
        }

        /// <summary>窗口 = scene 节点树 + __Templates(内联 item + 共享 item 嵌套)。返回未保存的 GO。</summary>
        private static GameObject BuildWindow(string sceneKey, LayaUIManifest.SceneEntry entry, JObject rootJson,
            LayaUIManifest manifest, LayaUIReport report, HashSet<string> stack)
        {
            report.BeginScene(sceneKey);
            _bakedSkins = entry.BakedSkins;
            GameObject go = BuildRoot(entry.Name, rootJson, manifest, report);
            _bakedSkins = null;

            List<GameObject> templates = new List<GameObject>();
            CollectInlineTemplates(entry, manifest, report, templates);

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
            return go;
        }

        private static void CollectInlineTemplates(LayaUIManifest.SceneEntry entry, LayaUIManifest manifest,
            LayaUIReport report, List<GameObject> templates)
        {
            if (entry.InlineItems == null) return;
            foreach (string itemKey in entry.InlineItems)
            {
                LayaUIManifest.SceneEntry ie = manifest.Get(itemKey);
                JObject ij = ie != null ? LoadSceneJson(ie) : null;
                if (ij == null) { report.Note("内联 item 读不到 json: " + itemKey); continue; }
                _bakedSkins = ie.BakedSkins;
                templates.Add(BuildRoot(ie.Name, ij, manifest, report));
                _bakedSkins = null;
                CollectInlineTemplates(ie, manifest, report, templates); // item 套 item
            }
        }

        public static string PrefabPath(LayaUIManifest.SceneEntry entry, LayaUIManifest manifest)
        {
            return LayaUISettings.PREFAB_ROOT + "/" + manifest.ModuleDir(entry.Module) + "/" + entry.Name + ".prefab";
        }

        private static string FindGroupPrefabContaining(string folder, string windowName)
        {
            if (!Directory.Exists(folder)) return null;
            foreach (string file in Directory.GetFiles(folder, "*.prefab", SearchOption.TopDirectoryOnly))
            {
                string path = file.Replace('\\', '/');
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;
                Transform child = asset.transform.Find(windowName);
                if (child != null && child.parent == asset.transform) return path;
            }
            return null;
        }

        private static JObject LoadSceneJson(LayaUIManifest.SceneEntry entry)
        {
            string path = Path.Combine(LayaUISettings.ClientRoot, entry.Json.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return null;
            try { return JObject.Parse(File.ReadAllText(path)); }
            catch (System.Exception e) { Debug.LogError("[LayaUI] 解析失败 " + path + ": " + e.Message); return null; }
        }

        // ---------------------------------------------------------------- 节点树

        private static GameObject BuildRoot(string name, JObject root, LayaUIManifest manifest, LayaUIReport report)
        {
            JObject props = root["props"] as JObject ?? new JObject();
            float w = LayaRectMath.F(props, "width") ?? manifest.DesignWidth;
            float h = LayaRectMath.F(props, "height") ?? manifest.DesignHeight;

            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            // 根节点居中锚定;窗口由 ViewManager 挂到全屏层下,居中即原 is_center 行为
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
            bool autoSizeContainer = false; // 容器没写宽/高:按子节点边界算(Laya 行为)

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
                    autoSizeContainer = p["width"] == null || p["height"] == null;
                    break;
                default:
                    if (type != "Box" && type != "View" && type != "Scene")
                        report.Note("未支持的组件类型 `" + type + "`(节点 " + name + "),按空容器处理");
                    go = LayaUITemplates.Spawn("Box", null);
                    go.name = name;
                    size = SizeOf(p, 0, 0);
                    childContainer = (RectTransform)go.transform;
                    autoSizeContainer = p["width"] == null || p["height"] == null;
                    break;
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            // 先建子节点(子节点锚定容器左上角,不依赖容器尺寸),
            // 容器缺宽/高时按子节点边界补全,再套自身布局——
            // 否则 centerX/right/bottom 定位的自动宽高容器会整体偏移(Laya 是按实际内容宽居中的)。
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
            if (autoSizeContainer)
            {
                Vector2 bounds = ChildBounds(childContainer);
                if (p["width"] == null) size.x = bounds.x;
                if (p["height"] == null) size.y = bounds.y;
            }

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
            CollectUnknownProps(type, p, report);
        }

        /// <summary>子节点内容边界(Laya 自动宽高语义:max(child.x + child.width))。
        /// 只统计左上锚定的子节点,centerX/拉伸子节点跳过(在自动宽高容器里 Laya 同样是病态布局)。</summary>
        private static Vector2 ChildBounds(RectTransform container)
        {
            float w = 0f, h = 0f;
            for (int i = 0; i < container.childCount; i++)
            {
                RectTransform c = container.GetChild(i) as RectTransform;
                if (c == null || !c.gameObject.activeSelf) continue;
                if (c.anchorMin != new Vector2(0f, 1f) || c.anchorMax != new Vector2(0f, 1f)) continue;
                Vector2 sz = c.sizeDelta;
                Vector3 sc = c.localScale;
                float left = c.anchoredPosition.x - c.pivot.x * sz.x * Mathf.Abs(sc.x);
                float top = -c.anchoredPosition.y - (1f - c.pivot.y) * sz.y * Mathf.Abs(sc.y);
                w = Mathf.Max(w, left + sz.x * Mathf.Abs(sc.x));
                h = Mathf.Max(h, top + sz.y * Mathf.Abs(sc.y));
            }
            return new Vector2(w, h);
        }

        private static GameObject BuildImage(JObject p, string name, string type, LayaUIReport report, out Vector2 size)
        {
            GameObject go = LayaUITemplates.Spawn("Image", null);
            go.name = name;
            Image img = go.GetComponent<Image>();

            string skin = (string)p["skin"] ?? (string)p["texture"];
            string sizeGrid = (string)p["sizeGrid"];
            Vector4 border = string.IsNullOrEmpty(sizeGrid) ? Vector4.zero : LayaRectMath.SizeGridToBorder(sizeGrid);

            // scene 里没图,但 TS 静态扫描烘焙出了运行时赋的图
            if (string.IsNullOrEmpty(skin) && _bakedSkins != null)
            {
                string baked;
                if (_bakedSkins.TryGetValue(name, out baked))
                {
                    skin = baked;
                    report.Note("`" + name + "` 烘焙运行时图 ← " + baked + "(来自 TS 静态扫描,真实运行可能换图)");
                }
            }

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
