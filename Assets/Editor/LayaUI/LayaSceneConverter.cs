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
    public static partial class LayaSceneConverter
    {
        private const string ROOT_LAYOUT_CONFIG_PATH = "Schemas/LayaUI/ui_root_layouts.json";

        /// <summary>UiCreator 产物黑名单(命中的 prefab 路径转换器只读不写),见 IsCreatorOwned。</summary>
        private const string CREATOR_OWNED_PATH = "Schemas/LayaUI/ui_creator_owned.json";

        // 当前正在转换的窗口/item 的烘焙皮肤表(节点名 -> 图路径,来自 TS 静态扫描)
        private static Dictionary<string, string> _bakedSkins;
        private static JObject _rootLayoutConfig;

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

        /// <summary>合并模式:模块(按 ui_groups.json 分组,缺省整模块一组)→ 大 prefab。返回缺图数(-1=失败)。</summary>
        public static int ConvertModuleCombined(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return -1;
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err)) { Debug.LogError("[LayaUI] " + err); return -1; }

            _rootLayoutConfig = null;
            ResetCreatorOwnedCache();   // 每次转换重读黑名单,免得会话里改了 json 不生效
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
            return report.MissingCount;
        }

        /// <summary>单窗口模式:模块内每个窗口一个 prefab(保留,零散需求用)。</summary>
        public static void ConvertModule(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err)) { Debug.LogError("[LayaUI] " + err); return; }

            _rootLayoutConfig = null;
            ResetCreatorOwnedCache();   // 每次转换重读黑名单,免得会话里改了 json 不生效
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
            _rootLayoutConfig = null;
            ResetCreatorOwnedCache();   // 每次转换重读黑名单,免得会话里改了 json 不生效
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
            _rootLayoutConfig = null;
            ResetCreatorOwnedCache();   // 每次转换重读黑名单,免得会话里改了 json 不生效
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
                // 命中黑名单只跳过存盘,不 return——后面的 report.Save() 还要把这次跳过写进报告。
                if (IsCreatorOwned(groupPath, report, "组内重转存盘"))
                {
                    Debug.LogWarning("[LayaUI] " + groupPath + " 归 UiCreator 所有,窗口 " + entry.Name +
                                     " 的重转结果未落盘;要改它请改对应 Creator 代码后点生成。");
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAsset(root, groupPath);
                    Debug.Log("[LayaUI] 已在 " + groupPath + " 内重转窗口 " + entry.Name);
                }
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
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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
                // (UiCreator 产物同样不许删——那不是"旧单窗口残留",是现役事实源)
                string oldPath = PrefabPath(entry, manifest);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(oldPath) != null &&
                    !IsCreatorOwned(oldPath, report, "删除旧单窗口 prefab"))
                {
                    AssetDatabase.DeleteAsset(oldPath);
                    report.Note("删除旧单窗口 prefab: " + oldPath + "(已并入 " + group.Name + ")");
                }
            }

            string prefabPath = LayaUISettings.PREFAB_ROOT + "/" + (moduleDir ?? "Unknown") + "/" + group.Name + ".prefab";
            if (IsCreatorOwned(prefabPath, report, "合并组存盘"))
            {
                Object.DestroyImmediate(root);
                return;
            }
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
            // DFS 栈纪律:务必在出栈时 Remove,使 stack 恰好=当前祖先链(用于 BuildWindow 防环判定)。
            try
            {
                JObject root = LoadSceneJson(entry);
                if (root == null)
                {
                    report.BeginScene(sceneKey);
                    report.Note("❌ 运行时 json 读取失败: " + entry.Json);
                    return null;
                }

                GameObject go = BuildWindow(sceneKey, entry, root, manifest, report, stack);
                if (entry.Decision == "shared-prefab")
                {
                    NormalizeItemRoot(go); // 共享件也是列表项语义,统一左上锚定
                }
                LayaBindGenerator.Generate(entry, manifest, go.transform, report);

                // UiCreator 产物:Bind cs 照常生成(运行时代码要用),但 prefab 不写盘。
                // 仍返回 prefabPath——共享件嵌套等调用方靠它 LoadAssetAtPath 拿现存(UiCreator 版)资产。
                if (IsCreatorOwned(prefabPath, report, "单 prefab 存盘"))
                {
                    Object.DestroyImmediate(go);
                    return prefabPath;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                return prefabPath;
            }
            finally
            {
                stack.Remove(sceneKey);
            }
        }

        /// <summary>窗口 = scene 节点树 + __Templates(内联 item + 共享 item 嵌套)。返回未保存的 GO。</summary>
        private static GameObject BuildWindow(string sceneKey, LayaUIManifest.SceneEntry entry, JObject rootJson,
            LayaUIManifest manifest, LayaUIReport report, HashSet<string> stack)
        {
            report.BeginScene(sceneKey);
            _bakedSkins = entry.BakedSkins;
            GameObject go = BuildRoot(entry.Name, rootJson, manifest, report);
            _bakedSkins = null;
            ApplyRootLayout(sceneKey, entry, rootJson, go, manifest, report);

            var templates = new List<GameObject>();
            var templateEntries = new List<LayaUIManifest.SceneEntry>();
            CollectInlineTemplates(entry, manifest, report, templates, templateEntries);

            // 共享 item:先保证共享 prefab 存在,再嵌套进 __Templates
            if (entry.TsClass != null)
            {
                foreach (KeyValuePair<string, LayaUIManifest.SceneEntry> kv in manifest.Scenes)
                {
                    LayaUIManifest.SceneEntry se = kv.Value;
                    if (se.Decision != "shared-prefab" || se.OwnerClasses == null) continue;
                    if (!se.OwnerClasses.Contains(entry.TsClass)) continue;
                    // 防环:该 shared 件正在当前祖先链上构建(back-edge)→ 不回嵌,否则 SaveAsPrefabAsset 会因
                    // 嵌套自身/祖先的 prefab 实例抛 "Cyclic nesting detected"。互相 own 的 shared 件只保留正向嵌套。
                    if (stack.Contains(kv.Key)) continue;
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

                // 为每个内联模板生成 {ItemName}Bind(业务 Instantiate 后 GetComponent 取字段,
                // 取代 transform.Find);共享件的 Bind 在其独立 prefab 转换时已生成
                for (int i = 0; i < templateEntries.Count; i++)
                {
                    LayaBindGenerator.Generate(templateEntries[i], manifest, templates[i].transform, report);
                }
            }
            return go;
        }

        private static void CollectInlineTemplates(LayaUIManifest.SceneEntry entry, LayaUIManifest manifest,
            LayaUIReport report, List<GameObject> templates, List<LayaUIManifest.SceneEntry> templateEntries)
        {
            if (entry.InlineItems == null) return;
            foreach (string itemKey in entry.InlineItems)
            {
                LayaUIManifest.SceneEntry ie = manifest.Get(itemKey);
                JObject ij = ie != null ? LoadSceneJson(ie) : null;
                if (ij == null) { report.Note("内联 item 读不到 json: " + itemKey); continue; }
                _bakedSkins = ie.BakedSkins;
                GameObject item = BuildRoot(ie.Name, ij, manifest, report);
                _bakedSkins = null;
                NormalizeItemRoot(item);
                templates.Add(item);
                templateEntries.Add(ie);
                CollectInlineTemplates(ie, manifest, report, templates, templateEntries); // item 套 item
            }
        }

        /// <summary>
        /// 列表项模板根归一为左上锚定。BuildRoot 给的是窗口语义(居中锚点+居中 pivot),
        /// 业务 Instantiate 进列表 content(左上)后会把项的"中心"对到 x=0,整行左半被
        /// 视口裁掉(选服列表名字看不见就是这个原因)。Laya 的列表项本来就是左上坐标系。
        /// </summary>
        private static void NormalizeItemRoot(GameObject item)
        {
            RectTransform rt = (RectTransform)item.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
        }

        public static string PrefabPath(LayaUIManifest.SceneEntry entry, LayaUIManifest manifest)
        {
            return LayaUISettings.PREFAB_ROOT + "/" + manifest.ModuleDir(entry.Module) + "/" + entry.Name + ".prefab";
        }

        // ---------------------------------------------------------------- UiCreator 产物黑名单

        /// <summary>
        /// UiCreator 拥有的 prefab 路径(Schemas/LayaUI/ui_creator_owned.json 的 owned 段)。
        /// 转换器命中这些路径时跳过存盘/跳过删除,避免全量重烤覆盖手写脚手架的产物。
        /// 惰性加载 + 进程内缓存,不在每个存盘点重复读盘;json 改完调 <see cref="ResetCreatorOwnedCache"/> 生效。
        /// </summary>
        private static HashSet<string> _creatorOwned;

        /// <summary>owner 说明(路径 -> Creator 类名),只用于往报告里写清是谁拥有。</summary>
        private static Dictionary<string, string> _creatorOwnedBy;

        /// <summary>清掉黑名单缓存,下次判定重新读盘(各转换入口开头调一次)。</summary>
        public static void ResetCreatorOwnedCache()
        {
            _creatorOwned = null;
            _creatorOwnedBy = null;
        }

        private static void EnsureCreatorOwnedLoaded()
        {
            if (_creatorOwned != null) return;

            // 大小写敏感:路径全部由 PREFAB_ROOT + moduleDir + name 拼出,json 里也按同样写法登记。
            _creatorOwned = new HashSet<string>();
            _creatorOwnedBy = new Dictionary<string, string>();

            string path = Path.Combine(Directory.GetCurrentDirectory(), CREATOR_OWNED_PATH);
            if (!File.Exists(path))
            {
                // 没有配置文件 = 不拦截任何路径(老工程/新机首次跑)。
                // 注意:缓存是每个转换入口重置一次,所以全量转换会每个模块各吼一条,不是全程只吼一条。
                Debug.LogWarning("[LayaUI] 找不到 UiCreator 产物黑名单 " + CREATOR_OWNED_PATH +
                                 ",本次转换不做覆盖保护。");
                return;
            }

            try
            {
                JObject root = JObject.Parse(File.ReadAllText(path));
                JArray owned = root["owned"] as JArray;
                if (owned == null) return;
                foreach (JToken t in owned)
                {
                    // 兼容两种写法:纯字符串 "Assets/..." 或对象 { "path": ..., "owner": ... }
                    string p = t.Type == JTokenType.String ? (string)t : (string)t["path"];
                    if (string.IsNullOrEmpty(p)) continue;
                    p = p.Replace('\\', '/');
                    _creatorOwned.Add(p);
                    string owner = t.Type == JTokenType.String ? null : (string)t["owner"];
                    _creatorOwnedBy[p] = string.IsNullOrEmpty(owner) ? "UiCreator" : owner;
                }
                // 载入成功不打日志:全量转换会逐模块重载一次,刷屏没意义;真正命中时 IsCreatorOwned 会逐条吼。
            }
            catch (System.Exception e)
            {
                // 解析失败按"空黑名单"处理,但必须吼出来,不能静默放行覆盖。
                Debug.LogError("[LayaUI] " + CREATOR_OWNED_PATH + " 解析失败,本次转换不做覆盖保护: " + e.Message);
            }
        }

        /// <summary>
        /// 该 prefab 路径是否归 UiCreator 所有(命中则调用方必须跳过存盘/删除)。
        /// 命中时往报告里记一行,绝不静默跳过。
        /// </summary>
        private static bool IsCreatorOwned(string prefabPath, LayaUIReport report, string action)
        {
            if (string.IsNullOrEmpty(prefabPath)) return false;
            EnsureCreatorOwnedLoaded();
            string key = prefabPath.Replace('\\', '/');
            if (!_creatorOwned.Contains(key)) return false;

            string owner;
            if (!_creatorOwnedBy.TryGetValue(key, out owner)) owner = "UiCreator";
            string msg = "⏭ 跳过" + action + "(UiCreator 产物,归 " + owner + "): " + key;
            if (report != null) report.Note(msg);
            Debug.Log("[LayaUI] " + msg);
            return true;
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

        private static JObject LoadRootLayoutConfig()
        {
            if (_rootLayoutConfig != null) return _rootLayoutConfig;

            string path = Path.Combine(Directory.GetCurrentDirectory(), ROOT_LAYOUT_CONFIG_PATH);
            if (!File.Exists(path))
            {
                _rootLayoutConfig = new JObject();
                return _rootLayoutConfig;
            }

            try
            {
                _rootLayoutConfig = JObject.Parse(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LayaUI] ui_root_layouts.json 解析失败: " + e.Message);
                _rootLayoutConfig = new JObject();
            }
            return _rootLayoutConfig;
        }

        /// <summary>
        /// 人工裁决表(推导链最高层)查表,四级依次:
        /// scene key → tsClass → scene 名 → 继承链上的基类名。
        /// 最后一级让一条配置能覆盖一整族(例如配 "BaseWindowComponent" 就作用于全部共用
        /// BaseWindowSkin 的业务大窗,不必逐个窗口写)。
        /// </summary>
        private static JObject GetConfiguredRootLayout(string sceneKey, LayaUIManifest.SceneEntry entry)
        {
            JObject cfg = LoadRootLayoutConfig();
            if (cfg == null) return null;

            JToken token = cfg[sceneKey];
            if (token == null && entry != null && !string.IsNullOrEmpty(entry.TsClass)) token = cfg[entry.TsClass];
            if (token == null && entry != null && !string.IsNullOrEmpty(entry.Name)) token = cfg[entry.Name];
            if (token == null && entry != null && entry.TsChain != null)
            {
                // 顺序是"自身 → 根基类",所以越派生的类越先命中,基类只当兜底。
                foreach (string cls in entry.TsChain)
                {
                    if (string.IsNullOrEmpty(cls)) continue;
                    token = cfg[cls];
                    if (token != null) break;
                }
            }
            return token as JObject;
        }

        // ---------------------------------------------------------------- 根锚定推导链

        /// <summary>水平轴定位键。数组顺序即 Laya Widget 的轴内优先级:centerX > left(+right) > right > x。</summary>
        private static readonly string[] AXIS_X = { "centerX", "left", "right", "x" };

        /// <summary>垂直轴定位键。数组顺序即 Laya Widget 的轴内优先级:centerY > top(+bottom) > bottom > y。</summary>
        private static readonly string[] AXIS_Y = { "centerY", "top", "bottom", "y" };

        /// <summary>节点自身属性:不参与按轴取舍,逐键覆盖后原样带进 clean props
        /// (少了它们 clean props 会把 pivot 打回 (0,1)、丢掉 scale,违反"只改 anchor 不改 pivot")。</summary>
        private static readonly string[] SELF_KEYS = { "anchorX", "anchorY", "pivotX", "pivotY", "scaleX", "scaleY", "rotation" };

        /// <summary>
        /// BaseItem1 / BaseItemRenderer 后代(BaseWindowComponent 的子页 item)要不要也按左上绝对定位修。
        /// 老端这批由父容器 addChild 挂进去、从不参与 is_center,理论上左上才对;但那是另一类缺陷、
        /// 另一套修法(父容器语义尚未核实),本批次先只打标记不改行为,免得把 100 多个子页一起动了。
        /// 核实后把这里改 true 即可放行。
        /// </summary>
        private const bool FIX_ITEM_CHAIN_ROOTS = false;

        /// <summary>
        /// 根锚定推导链(低→高):
        ///   L0 scene json props → L1 基类默认(tsChain)→ L2 manifest.rootLayout(子类 TS 覆写)
        ///   → L3 ui_root_layouts.json(人工最终裁决)
        ///
        /// 为什么需要它:老端 86% 的 view 根锚定写在 TS 运行时基类里(BaseView1 的 is_center、
        /// BaseWindowComponent 的 bottom=0+centerX=0),.scene 数据里根本没有。BuildRoot 只读 scene props,
        /// 于是把这层语义压成二值——有显式锚就换算,否则【无条件居中】。绝大多数 view 落进那条兜底,
        /// 其中相当一批本该保持左上绝对定位却被强行居中。这里把基类语义补回来。
        ///
        /// 合并结果统一走 clean props(只含 width/height + 收敛后的定位键 + 节点自身属性)再喂
        /// LayaRectMath.Apply,不与原 scene props 混着喂——理由见 NormalizeAxis。
        /// </summary>
        private static void ApplyRootLayout(string sceneKey, LayaUIManifest.SceneEntry entry, JObject rootJson,
            GameObject go, LayaUIManifest manifest, LayaUIReport report)
        {
            JObject sceneProps = rootJson["props"] as JObject ?? new JObject();
            float w = LayaRectMath.F(sceneProps, "width") ?? manifest.DesignWidth;
            float h = LayaRectMath.F(sceneProps, "height") ?? manifest.DesignHeight;
            RectTransform rt = (RectTransform)go.transform;

            // 共享件的根随后会被 NormalizeItemRoot 无条件打成左上锚(ConvertOne 里 BuildWindow 之后),
            // 在这儿推导只是白算一遍再被抹掉,还会往报告里灌误导信息。
            if (entry != null && entry.Decision == "shared-prefab")
            {
                report.Tally("根锚定/跳过-共享件(随后被 NormalizeItemRoot 归一)");
                return;
            }

            string baseWhy;
            JObject baseLayer = DeriveBaseLayout(entry, out baseWhy);                        // L1 基类默认
            JObject tsLayer = PickLayoutKeys(entry != null ? entry.RootLayout : null);       // L2 子类 TS 覆写
            JObject manLayer = PickLayoutKeys(GetConfiguredRootLayout(sceneKey, entry));     // L3 人工裁决

            // 一个 .scene 被多个 TS 类共用、各自的根锚定又不一致:manifest 里那份 rootLayout 只是
            // 取值顺序上先到的一个,不是裁决结果。此时【不许】拿它去套——Unity 侧这些类共用同一个
            // prefab,套错等于拿 A 界面的锚渲染 B 界面。维持改前行为并告警,交人工进 ui_root_layouts.json;
            // 人工表一旦写了就以人工表为准(那正是这条路的逃生口)。
            if (entry != null && entry.RootLayoutConflict != null && entry.RootLayoutConflict.Count > 0)
            {
                string who = string.Join(" / ", entry.RootLayoutConflict.ToArray());
                if (CountOf(manLayer) == 0)
                {
                    report.Warn("根锚定 " + sceneKey + " 被多个 TS 类共用且根锚定冲突(" + who + ")," +
                                "已维持改前行为不做推导;需人工在 " + ROOT_LAYOUT_CONFIG_PATH + " 里裁决或拆件");
                    report.Tally("根锚定/⚠共用件冲突(维持现状待裁决)");
                    return;
                }
                report.Note("根锚定 " + sceneKey + " 共用件冲突(" + who + "),按人工表裁决");
                baseLayer = null;
                tsLayer = null;
            }

            bool hasHigher = CountOf(baseLayer) > 0 || CountOf(tsLayer) > 0 || CountOf(manLayer) > 0;
            if (!hasHigher)
            {
                // L1/L2/L3 都没输入,scene props 就是全部语义——BuildRoot 已经算完了,原样保留。
                if (HasExplicitRootLayout(sceneProps))
                {
                    report.Tally("根锚定/scene 显式锚");
                    return;
                }

                // scene 无锚 + 推导链查不到:只能沿用旧的【无条件居中】兜底。
                // 保留它是为了生成器还没产出新字段时不炸掉全库(并行开发的必要条件),
                // 但每命中一次都必须吼出来——否则生成器漏项会被这条兜底永久掩盖。
                if (!CanDeriveFromChain(entry))
                {
                    report.Warn("根锚定 " + sceneKey + " 推导链查不到(tsChain/rootLayout 缺失,或 is_center 是运行时切换)," +
                                "沿用无条件居中兜底;需 analyze_layaui.py 补字段或人工进 " + ROOT_LAYOUT_CONFIG_PATH);
                    report.Tally("根锚定/⚠兜底居中(推导不出)");
                    return;
                }

                if (!IsViewChain(entry) && !FIX_ITEM_CHAIN_ROOTS)
                {
                    // 链上没有 BaseView1:BaseItem1 / BaseItemRenderer 后代是别人的子页 item,
                    // 由父容器 addChild 挂进去,从来不参与 is_center 语义。见 FIX_ITEM_CHAIN_ROOTS。
                    report.Tally("根锚定/子页 item 链(本批次不改行为)");
                    report.Note("根锚定 " + sceneKey + " 链上无 BaseView1(" + ChainText(entry) + "),按子页 item 处理,本批次维持原样");
                    return;
                }

                // 走到这里 = tsChain 在、链上既无 BaseWindowComponent 也无 is_center=true。
                // 老端这类 view 的根压根没被设过锚,保持 .scene 的 x/y 左上绝对定位才是对的
                // ——此前一律居中,正是"反向错"的来源。下面按空的高层继续走合并,
                // 收敛后只剩 L0 的 x/y,LayaRectMath 的兜底分支即左上绝对定位。
            }

            JObject layout = new JObject();
            Accumulate(layout, PickLayoutKeys(sceneProps)); // L0
            Accumulate(layout, baseLayer);                  // L1
            Accumulate(layout, tsLayer);                    // L2
            OverrideByAxis(layout, manLayer);               // L3
            NormalizeAxis(layout, AXIS_X);
            NormalizeAxis(layout, AXIS_Y);

            // 零位移快路径:老端 is_center 的那一大批 view 今天走 BuildRoot 的居中兜底,
            // 推导链算出来的也正是 {centerX:0, centerY:0}——但若过一遍 LayaRectMath,
            // pivot 会被解成 (0,1)、anchoredPosition 解成 (-w/2, h/2):屏幕矩形一样,
            // prefab YAML 里的 m_Pivot / m_AnchoredPosition 却全变,基准档"逐字节零 diff"的
            // 无回归证明就没了。所以这里逐行复刻旧兜底,不经 LayaRectMath。
            // (仅当 scene 自己没有显式锚时才走——scene 带 centerX/centerY 的根今天是过 Apply 的,
            //  pivot 由 anchorX/pivotX 解出,不能被这条快路径打成 0.5。)
            if (!HasExplicitRootLayout(sceneProps) && IsPureCenter(layout))
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = Vector2.zero;
                report.Tally("根锚定/居中(" + (baseWhy ?? "推导") + ",与改前逐字节一致)");
                return;
            }

            // clean props:定位键只认收敛结果,节点自身属性(pivot/scale/rotation)从 scene 搬过来。
            // 绝不把 scene 的定位键一起倒进来——那样陈旧的 top/bottom/center 会活过 TS 赋值。
            JObject p = new JObject
            {
                ["width"] = w,
                ["height"] = h,
            };
            foreach (JProperty prop in layout.Properties())
            {
                p[prop.Name] = prop.Value.DeepClone();
            }

            LayaRectMath.Apply(rt, p, new Vector2(w, h));

            string src = SourceText(baseWhy, tsLayer, manLayer, hasHigher);
            report.Tally("根锚定/" + src);
            report.Note("根锚定 " + sceneKey + " ← " + src + " " + layout.ToString(Newtonsoft.Json.Formatting.None));
            if (entry != null && entry.RootLayout != null && IsTrue(entry.RootLayout, "safeAreaTop"))
            {
                // 老端原文是 top = Util.GetLiuhaiHeight()(硬编码 60 + 静态缓存永不更新,是老端 bug)。
                // 统一折算成 0,安全区交给 Unity 的 SafeAreaRoot,绝不烤成 60px(会叠成双倍内缩)。
                report.Note("根锚定 " + sceneKey + " 的 top 原本取自 Util.GetLiuhaiHeight(),已按手册折算成 0,安全区走 SafeAreaRoot");
            }
        }

        /// <summary>
        /// L1 基类默认层。tsChain 缺失时返回 null —— 这个 null 是"无从推导"与"推导出无锚"的分界线,
        /// 上层据此决定是沿用旧居中兜底还是修成左上绝对定位,不能退化成返回空对象。
        /// </summary>
        private static JObject DeriveBaseLayout(LayaUIManifest.SceneEntry entry, out string why)
        {
            why = null;
            if (entry == null || entry.TsChain == null || entry.TsChain.Count == 0) return null;

            JObject o = null;
            if (entry.TsChain.Contains("BaseWindowComponent"))
            {
                // BaseWindowComponent 的 load_callback:display_obj.bottom = 0;display_obj.centerX = 0
                o = new JObject { ["centerX"] = 0f, ["bottom"] = 0f };
                why = "BaseWindowComponent";
            }
            if (ResolveIsCenter(entry) == true)
            {
                // BaseView1.OnLoadCompleted:if (is_center) display_obj.centerX = display_obj.centerY = 0
                if (o == null) o = new JObject();
                o["centerX"] = 0f;
                o["centerY"] = 0f;
                why = why == null ? "is_center" : why + "+is_center";
            }
            return o;
        }

        /// <summary>is_center 的静态生效值;null = 未提取到,或是运行时切换(不可静态折叠)。</summary>
        private static bool? ResolveIsCenter(LayaUIManifest.SceneEntry entry)
        {
            if (entry == null || entry.IsCenter == null || entry.IsCenter.Type == JTokenType.Null) return null;
            if (entry.IsCenter.Type == JTokenType.Boolean) return (bool)entry.IsCenter;
            return null;    // "dynamic" 之类:构造期一个值、SetData 里另一个值,不许折叠
        }

        /// <summary>推导链是否可用。tsChain 缺失,或 is_center 被标成运行时切换,都算"推导不出"。</summary>
        private static bool CanDeriveFromChain(LayaUIManifest.SceneEntry entry)
        {
            if (entry == null || entry.TsChain == null || entry.TsChain.Count == 0) return false;
            if (entry.IsCenter != null && entry.IsCenter.Type != JTokenType.Null &&
                entry.IsCenter.Type != JTokenType.Boolean) return false;
            return true;
        }

        /// <summary>链上是否有 BaseView1(有 = 真 view,根锚归 is_center 管;没有 = 别人的子页 item)。</summary>
        private static bool IsViewChain(LayaUIManifest.SceneEntry entry)
        {
            return entry != null && entry.TsChain != null && entry.TsChain.Contains("BaseView1");
        }

        private static string ChainText(LayaUIManifest.SceneEntry entry)
        {
            if (entry == null || entry.TsChain == null) return "无链";
            return string.Join(" → ", entry.TsChain.ToArray());
        }

        private static int CountOf(JObject o)
        {
            return o == null ? 0 : o.Count;
        }

        /// <summary>只挑出参与根锚定的键(定位键 + 节点自身属性),safeAreaTop 这类元数据键一律滤掉。</summary>
        private static JObject PickLayoutKeys(JObject src)
        {
            if (src == null) return null;
            JObject o = new JObject();
            foreach (JProperty prop in src.Properties())
            {
                if (!IsLayoutKey(prop.Name)) continue;
                if (prop.Value == null || prop.Value.Type == JTokenType.Null) continue;
                o[prop.Name] = prop.Value.DeepClone();
            }
            return o.Count > 0 ? o : null;
        }

        private static bool IsLayoutKey(string name)
        {
            return System.Array.IndexOf(AXIS_X, name) >= 0
                || System.Array.IndexOf(AXIS_Y, name) >= 0
                || System.Array.IndexOf(SELF_KEYS, name) >= 0;
        }

        /// <summary>
        /// 按 Laya Widget 的真实语义叠一层:逐键覆盖。
        /// 老端就是"scene 属性先加载、TS 赋值再打在同一个 Widget 上",赋了哪个键就只动哪个键,
        /// 同轴其它键仍留在 Widget 上参与 resetLayout(所以 bottom=0 叠在 top 上会变拉伸,不是顶掉 top)。
        /// </summary>
        private static void Accumulate(JObject dst, JObject src)
        {
            if (src == null) return;
            foreach (JProperty prop in src.Properties())
            {
                dst[prop.Name] = prop.Value.DeepClone();
            }
        }

        /// <summary>
        /// 人工裁决层的叠法:按轴整体接管——该轴只要写了一个键,低层这一轴的键全部作废。
        /// 它不是 Laya 语义的一部分,是"人说了算"的最终覆盖,所以 ui_root_layouts.json 里
        /// 写 {x:10} 就真的是 x=10,不会被 scene 里的 left+right 顶成拉伸。
        /// </summary>
        private static void OverrideByAxis(JObject dst, JObject src)
        {
            if (src == null) return;
            if (MentionsAxis(src, AXIS_X)) ClearAxis(dst, AXIS_X);
            if (MentionsAxis(src, AXIS_Y)) ClearAxis(dst, AXIS_Y);
            Accumulate(dst, src);
        }

        private static bool MentionsAxis(JObject o, string[] axis)
        {
            foreach (string k in axis) if (HasProp(o, k)) return true;
            return false;
        }

        private static void ClearAxis(JObject o, string[] axis)
        {
            foreach (string k in axis) o.Remove(k);
        }

        /// <summary>
        /// 把一根轴上累积到的键收敛成【互斥的一种形态】,使 LayaRectMath 与 laya.ui.js 的分支顺序
        /// 差异永远构造不出来。
        ///
        /// Laya(cdn/libs/laya.ui.js 的 Widget.resetLayoutX/Y):centerX > left(+right 才拉伸) > right;
        /// LayaRectMath.Apply:                                  left&&right > centerX > right > left。
        /// 两者只在 centerX 与 left+right 共存时分叉(垂直轴同理)。全库 scene 现在没有这种组合,
        /// 所以一直无感;但推导链会自己造出来——比如 scene 是 left+right+top+bottom 的根,
        /// 叠上基类的 {centerX:0,bottom:0} 就正好合成 centerX+left+right。
        /// 这里按 Laya 的优先级先裁掉败者,只留一种形态喂下去,LayaRectMath 一行都不用改。
        /// </summary>
        private static void NormalizeAxis(JObject o, string[] axis)
        {
            string center = axis[0], near = axis[1], far = axis[2], abs = axis[3];
            bool hasNear = HasProp(o, near), hasFar = HasProp(o, far);
            if (HasProp(o, center)) KeepOnly(o, axis, center, null);
            else if (hasNear && hasFar) KeepOnly(o, axis, near, far);   // 两端都在 = 拉伸
            else if (hasNear) KeepOnly(o, axis, near, null);
            else if (hasFar) KeepOnly(o, axis, far, null);
            else KeepOnly(o, axis, abs, null);                          // 全无 = 绝对坐标兜底
        }

        private static void KeepOnly(JObject o, string[] axis, string a, string b)
        {
            foreach (string k in axis)
            {
                if (k == a || (b != null && k == b)) continue;
                o.Remove(k);
            }
        }

        /// <summary>收敛结果是否恰好是"水平垂直都居中"(即老端 is_center 的等价形态)。</summary>
        private static bool IsPureCenter(JObject o)
        {
            if (!HasProp(o, "centerX") || !HasProp(o, "centerY")) return false;
            if ((LayaRectMath.F(o, "centerX") ?? 1f) != 0f) return false;
            if ((LayaRectMath.F(o, "centerY") ?? 1f) != 0f) return false;
            // 收敛后 centerX/centerY 在场就意味着同轴其它键已被裁掉,这里只兜自身属性:
            // 带 pivot/scale/rotation 的根不能走快路径(快路径是照抄旧兜底,旧兜底会忽略它们)。
            foreach (string k in SELF_KEYS) if (HasProp(o, k)) return false;
            return true;
        }

        private static string SourceText(string baseWhy, JObject tsLayer, JObject manLayer, bool hasHigher)
        {
            if (!hasHigher) return "左上绝对定位(链上无根锚,修正原居中兜底)";
            List<string> parts = new List<string>();
            if (baseWhy != null) parts.Add("基类:" + baseWhy);
            if (CountOf(tsLayer) > 0) parts.Add("TS 覆写");
            if (CountOf(manLayer) > 0) parts.Add("人工表");
            return string.Join("+", parts.ToArray());
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
            if (HasExplicitRootLayout(props))
            {
                // 根节点声明了相对边/居中/缩放定位:走与子节点完全一致的 LayaRectMath 语法,
                // 不再丢弃根级 left/right/top/bottom/centerX/centerY/scale。
                // (centerX/centerY 经 Apply 得到与原居中实现相同的屏幕矩形与尺寸,
                //  仅锚点内部值不同,子节点因落在同一矩形而渲染一致。)
                rt.sizeDelta = new Vector2(w, h);
                LayaRectMath.Apply(rt, props, new Vector2(w, h));
            }
            else
            {
                // 【临时值】无显式定位的窗口先摆居中,真正的根锚由 BuildWindow 里紧随其后的
                // ApplyRootLayout 按推导链(基类默认 / TS 覆写 / 人工表)定夺。
                // 这条居中只有在推导链完全查不到时才会留到最后(那时 ApplyRootLayout 会写 report.Warn)——
                // 老端的锚定语义写在 TS 运行时基类里,scene 数据里没有,不能拿"无锚"当"居中"。
                //
                // 注意 BuildRoot 还有两个不走 BuildWindow 的调用方:CollectInlineTemplates(内联 item)
                // 与 Baker 的 BakeViewTree(快照烤图)。它们随后各有各的归一/绝对几何,
                // 推导链有意不参与——所以新逻辑收在 ApplyRootLayout 里,BuildRoot 保持原样。
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = Vector2.zero;
            }

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

        /// <summary>根 props 是否声明了相对边/居中/缩放定位。有则走 LayaRectMath 语法,
        /// 无则保留 is_center 居中默认(x/y 不计入:顶层窗口的 x=0/y=0 多为占位,运行时靠 is_center)。</summary>
        private static bool HasExplicitRootLayout(JObject p)
        {
            return HasProp(p, "left") || HasProp(p, "right") || HasProp(p, "top") || HasProp(p, "bottom")
                || HasProp(p, "centerX") || HasProp(p, "centerY")
                || HasProp(p, "scaleX") || HasProp(p, "scaleY");
        }

        private static bool HasProp(JObject p, string key)
        {
            JToken t = p[key];
            return t != null && t.Type != JTokenType.Null;
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
                // HBox/VBox 的自动尺寸是「子节点依次排列」语义(Σ显示宽 + 间距),
                // 普通 Box 才是「子节点边界」语义
                Vector2 bounds;
                if (type == "HBox" || type == "VBox")
                {
                    bounds = SumBounds(childContainer, type == "HBox", LayaRectMath.F(p, "space") ?? 0f);
                }
                else
                {
                    bounds = ChildBounds(childContainer);
                }
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

        /// <summary>HBox/VBox 自动尺寸:主轴 = Σ子节点显示尺寸 + 间距,交叉轴 = 最大显示尺寸
        /// (Laya HBox/VBox 会重排子节点,设计时 x/y 无效,不能用边界算法)。</summary>
        private static Vector2 SumBounds(RectTransform container, bool horizontal, float spacing)
        {
            float main = 0f, cross = 0f;
            int count = 0;
            for (int i = 0; i < container.childCount; i++)
            {
                RectTransform c = container.GetChild(i) as RectTransform;
                if (c == null || !c.gameObject.activeSelf) continue;
                Vector2 d = new Vector2(c.sizeDelta.x * Mathf.Abs(c.localScale.x),
                                        c.sizeDelta.y * Mathf.Abs(c.localScale.y));
                main += horizontal ? d.x : d.y;
                cross = Mathf.Max(cross, horizontal ? d.y : d.x);
                count++;
            }
            if (count > 1) main += spacing * (count - 1);
            return horizontal ? new Vector2(main, cross) : new Vector2(cross, main);
        }

        /// <summary>子节点内容边界(Laya 自动宽高语义:max(child.x + child.width))。
        /// 逐轴独立统计:水平只看左锚定子节点,垂直只看顶锚定子节点——
        /// 协议行这类「子节点 centerY 垂直居中」的容器,宽度照样要算
        /// (此前整节点跳过导致 _box_agreement 宽=34,centerX 居中后整行偏右)。</summary>
        private static Vector2 ChildBounds(RectTransform container)
        {
            float w = 0f, h = 0f;
            for (int i = 0; i < container.childCount; i++)
            {
                RectTransform c = container.GetChild(i) as RectTransform;
                if (c == null || !c.gameObject.activeSelf) continue;
                Vector2 sz = c.sizeDelta;
                Vector3 sc = c.localScale;
                float dw = sz.x * Mathf.Abs(sc.x);
                float dh = sz.y * Mathf.Abs(sc.y);
                if (c.anchorMin.x == 0f && c.anchorMax.x == 0f)
                {
                    float left = c.anchoredPosition.x - c.pivot.x * dw;
                    w = Mathf.Max(w, left + dw);
                }
                if (c.anchorMin.y == 1f && c.anchorMax.y == 1f)
                {
                    float top = -c.anchoredPosition.y - (1f - c.pivot.y) * dh;
                    h = Mathf.Max(h, top + dh);
                }
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
                Color c = img.color; c.a = 0f; img.color = c; // 即便点击区运行时把它 enable(无图),也保持透明不画白块
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

            // scene 文本里的换行存的是字面 \n 两个字符,Laya 运行时当换行渲染,这里复刻;
            // 不转的话长文本会按一整行渲染,居中时左右外溢(健康忠告/版权文字就是这么超屏的)
            string text = UnescapeLayaText((string)p["text"]);
            if (html)
            {
                string raw = UnescapeLayaText((string)p["innerHTML"]) ?? text;
                if (string.IsNullOrEmpty(raw)) raw = text;
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

            input.text = UnescapeLayaText((string)p["text"]);
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

            // content 锚定滚动起点(纵向=顶、横向=左):pivot 居中时业务改 sizeDelta 会向
            // 两头同时生长,头部条目被顶出视口(选角页 4 行只见 2 行的根因,通用修)
            if (sr.vertical)
            {
                content.anchorMin = new Vector2(content.anchorMin.x, 1f);
                content.anchorMax = new Vector2(content.anchorMax.x, 1f);
                content.pivot = new Vector2(content.pivot.x, 1f);
            }
            else
            {
                content.anchorMin = new Vector2(0f, content.anchorMin.y);
                content.anchorMax = new Vector2(0f, content.anchorMax.y);
                content.pivot = new Vector2(0f, content.pivot.y);
            }
            content.anchoredPosition = Vector2.zero;

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
            lg.childScaleWidth = true;  // Laya HBox 按显示尺寸(含 scale)排列
            lg.childScaleHeight = true;
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

        /// <summary>Laya scene 文本的字面转义(\n \t)转真实控制符,复刻运行时渲染。</summary>
        private static string UnescapeLayaText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            return s.Replace("\\n", "\n").Replace("\\r", "").Replace("\\t", "\t");
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
