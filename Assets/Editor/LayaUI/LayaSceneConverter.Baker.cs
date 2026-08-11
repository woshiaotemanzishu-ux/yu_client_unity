using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// Phase 2 烤制器:把【运行时快照】(page_snapshot_*.json,老端运行时解析完的完整视图树,
    /// 含运行时新建/改位的节点)当转换源,复用现有 BuildRoot/BuildNode/LayaRectMath/散图导入/
    /// prefab 写出,烤成【节点全在、可视化可编辑】的 UGUI prefab。
    ///
    /// 关键:快照 serializeNode 已把节点的 Laya 布局属性(x/y/width/anchorX/pivotX/left/right/
    /// centerX/scale/rotation/skin/sizeGrid + textProps)按运行时值抓下来了,这正是转换器吃的方言,
    /// 所以烤制器只是把快照节点适配成 {type,props,child},不是另起炉灶。
    /// </summary>
    public static partial class LayaSceneConverter
    {
        // 直接透传给转换器 props 的布局属性(快照里已是运行时解析后的值)
        private static readonly string[] BAKE_PASS_PROPS =
        {
            "x", "y", "width", "height", "anchorX", "anchorY", "pivotX", "pivotY",
            "left", "right", "top", "bottom", "centerX", "centerY",
            "scaleX", "scaleY", "rotation", "alpha", "zOrder", "sizeGrid", "skin", "visible",
        };

        /// <summary>从快照烤一个视图成 prefab(只烤,不挂组件/不注册)。返回 prefab 路径(失败 null)。</summary>
        public static string BakeViewFromSnapshot(string snapshotPath, string viewName, string outPrefabPath)
        {
            if (!File.Exists(snapshotPath))
            {
                Debug.LogError("[Bake] 快照不存在: " + snapshotPath);
                return null;
            }
            JObject view = FindView(JObject.Parse(File.ReadAllText(snapshotPath)), viewName);
            JObject tree = view?["nodeTree"] as JObject;
            if (tree == null)
            {
                Debug.LogError("[Bake] 快照里找不到视图/无 nodeTree: " + viewName);
                return null;
            }
            string p = BakeViewTree(tree, viewName, outPrefabPath);
            AssetDatabase.Refresh();
            Debug.Log("[Bake] 完成 → " + p);
            return p;
        }

        /// <summary>
        /// 运行态转换实验专用入口：输出只能位于 Assets/__RuntimeConversionExperiment，
        /// 不注册 Addressables、不回填业务组件、不全局 Refresh，也不物化/改写共享图片资源。
        /// </summary>
        public static string BakeViewFromSnapshotIsolated(string snapshotPath, string viewName, string outPrefabPath)
        {
            string normalized = (outPrefabPath ?? "").Replace('\\', '/');
            if (!normalized.StartsWith("Assets/__RuntimeConversionExperiment/"))
                throw new System.InvalidOperationException("ISOLATED_BAKE_OUTPUT_REQUIRED: " + outPrefabPath);
            if (!File.Exists(snapshotPath))
                throw new FileNotFoundException("runtime snapshot missing", snapshotPath);
            JObject view = FindView(JObject.Parse(File.ReadAllText(snapshotPath)), viewName);
            JObject tree = view?["nodeTree"] as JObject;
            if (tree == null) throw new System.InvalidOperationException("runtime view missing: " + viewName);
            LayaSpriteImporter.ExistingAssetsOnly = true;
            try
            {
                string result = BakeViewTree(tree, viewName, normalized);
                AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                return result;
            }
            finally
            {
                LayaSpriteImporter.ExistingAssetsOnly = false;
            }
        }

        /// <summary>BatchMode: bake isolated runtime snapshot candidate and render it in a real 720x1280 Canvas.</summary>
        public static void BakeRuntimeSnapshotExperimentCli()
        {
            string snapshot = CommandLineValue("-runtimeSnapshot");
            string viewName = CommandLineValue("-runtimeViewName");
            string prefab = CommandLineValue("-runtimeOutPrefab");
            string png = CommandLineValue("-runtimeRenderPng");
            string report = CommandLineValue("-runtimeRenderReport");
            if (string.IsNullOrEmpty(snapshot) || string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(prefab)
                || string.IsNullOrEmpty(png) || string.IsNullOrEmpty(report))
                throw new System.InvalidOperationException("runtime snapshot experiment arguments missing");
            string baked = BakeViewFromSnapshotIsolated(snapshot, viewName, prefab);
            RenderIsolatedCandidate(baked, png, report, 720, 1280);
            Debug.Log("[RuntimeSnapshotExperiment] OK prefab=" + baked + " png=" + png + " report=" + report);
        }

        /// <summary>
        /// Existing-editor entry for the isolated experiment. The request file is deliberately
        /// outside Assets so opening the project does not register or import experiment inputs.
        /// </summary>
        [MenuItem("神霄/实验/运行态通用转换隔离候选", priority = 900)]
        public static void BakeRuntimeSnapshotExperimentMenu()
        {
            const string requestPath = "Temp/RuntimeSnapshotExperiment/request.json";
            if (!File.Exists(requestPath))
                throw new FileNotFoundException("runtime snapshot experiment request missing", requestPath);
            JObject request = JObject.Parse(File.ReadAllText(requestPath));
            string snapshot = request["snapshot"]?.ToString();
            string viewName = request["viewName"]?.ToString();
            string prefab = request["prefab"]?.ToString();
            string png = request["png"]?.ToString();
            string report = request["report"]?.ToString();
            if (string.IsNullOrEmpty(snapshot) || string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(prefab)
                || string.IsNullOrEmpty(png) || string.IsNullOrEmpty(report))
                throw new System.InvalidOperationException("runtime snapshot experiment request fields missing");
            if (File.Exists(png) || File.Exists(report))
                throw new System.InvalidOperationException("IMMUTABLE_EXPERIMENT_OUTPUT_EXISTS");
            string baked = BakeViewFromSnapshotIsolated(snapshot, viewName, prefab);
            RenderIsolatedCandidate(baked, png, report, 720, 1280);
            Debug.Log("[RuntimeSnapshotExperiment] OK prefab=" + baked + " png=" + png + " report=" + report);
        }

        private static string CommandLineValue(string key)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return null;
        }

        private static void RenderIsolatedCandidate(string prefabPath, string pngPath, string reportPath, int width, int height)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null) throw new System.InvalidOperationException("isolated candidate prefab failed to import: " + prefabPath);
            GameObject cameraGo = null, canvasGo = null, instance = null;
            RenderTexture rt = null;
            Texture2D capture = null;
            try
            {
                cameraGo = new GameObject("RuntimeCandidateCamera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = height * 0.5f;
                camera.transform.position = new Vector3(0f, 0f, -100f);

                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = rt;

                canvasGo = new GameObject("RuntimeCandidateCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                UnityEngine.UI.CanvasScaler scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(width, height);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;

                instance = Object.Instantiate(asset, canvasGo.transform, false);
                instance.name = asset.name;
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                capture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capture.Apply();
                RenderTexture.active = previous;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pngPath)));
                File.WriteAllBytes(pngPath, capture.EncodeToPNG());

                var nodes = new JArray();
                int imageNodes = 0, resolvedImages = 0, textNodes = 0, renderedTexts = 0;
                foreach (RectTransform rect in instance.GetComponentsInChildren<RectTransform>(true))
                {
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                    foreach (Vector3 corner in corners)
                    {
                        Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                        min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                    }
                    UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>();
                    TMPro.TMP_Text text = rect.GetComponent<TMPro.TMP_Text>();
                    if (image != null) { imageNodes++; if (image.enabled && image.sprite != null) resolvedImages++; }
                    if (text != null) { textNodes++; if (text.enabled && !string.IsNullOrEmpty(text.text)) renderedTexts++; }
                    nodes.Add(new JObject {
                        ["path"] = CandidatePath(rect, instance.transform), ["name"] = rect.name,
                        ["x"] = min.x, ["y"] = height - max.y, ["width"] = max.x - min.x, ["height"] = max.y - min.y,
                        ["active"] = rect.gameObject.activeInHierarchy,
                        ["image"] = image != null, ["spriteResolved"] = image != null && image.sprite != null,
                        ["text"] = text != null ? text.text : ""
                    });
                }
                JObject result = new JObject {
                    ["schema"] = 1, ["prefab"] = prefabPath, ["width"] = width, ["height"] = height,
                    ["nodes"] = nodes,
                    ["metrics"] = new JObject {
                        ["generatedNodes"] = nodes.Count, ["imageNodes"] = imageNodes, ["resolvedImages"] = resolvedImages,
                        ["textNodes"] = textNodes, ["renderedTexts"] = renderedTexts
                    }
                };
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
                File.WriteAllText(reportPath, result.ToString());
            }
            finally
            {
                if (capture != null) Object.DestroyImmediate(capture);
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                if (instance != null) Object.DestroyImmediate(instance);
                if (canvasGo != null) Object.DestroyImmediate(canvasGo);
                if (cameraGo != null) Object.DestroyImmediate(cameraGo);
            }
        }

        private static string CandidatePath(Transform current, Transform root)
        {
            var parts = new List<string>();
            Transform value = current;
            while (value != null)
            {
                parts.Add(value.name);
                if (value == root) break;
                value = value.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        /// <summary>
        /// 批量烤整模块(给 conversion 流水线/skill 用):扫 manifest 的每个 prefab,
        /// 在 snapshotDir 的快照里找对应视图 → 烤 + 挂组件回填 + 注册 addressable。一次 MCP 调用跑完。
        /// Unity 单编辑器,资产操作必须串行,所以批量做成一个调用而不是多 agent 并行碰 Unity。
        /// 返回状态报告文本。
        /// </summary>
        public static string BakeModuleFromManifest(string manifestPath, string snapshotDir, string moduleDir,
            string onlyPrefab = null)
        {
            if (!File.Exists(manifestPath)) return "[BakeModule] manifest 不存在: " + manifestPath;

            // 索引快照里所有视图: name -> view
            var viewMap = new Dictionary<string, JObject>();
            if (Directory.Exists(snapshotDir))
            {
                foreach (string f in Directory.GetFiles(snapshotDir, "page_snapshot*.json"))
                {
                    JObject snap;
                    try { snap = JObject.Parse(File.ReadAllText(f)); }
                    catch { continue; }
                    foreach (JToken vt in snap["views"] as JArray ?? new JArray())
                    {
                        JObject v = vt as JObject;
                        string nm = (v?["meta"] as JObject)?["name"]?.ToString();
                        if (string.IsNullOrEmpty(nm)) continue;
                        int nc = v["nodeCount"] != null ? (int)v["nodeCount"] : 0;
                        // 同名多份快照(数据驱动屏冷采空壳 + 带数据版)→ 取节点最多的那份(数据最全)。
                        // 同时按基名登记(快照对重复实例加了 #N 后缀,如 LoginAlertView#1)。
                        int hash = nm.IndexOf('#');
                        string baseNm = hash > 0 ? nm.Substring(0, hash) : nm;
                        foreach (string key in new[] { nm, baseNm })
                        {
                            int exc = viewMap.TryGetValue(key, out JObject ex) && ex["nodeCount"] != null ? (int)ex["nodeCount"] : -1;
                            if (nc > exc) viewMap[key] = v;
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            int baked = 0, skipped = 0, failed = 0;
            string prefabRoot = LayaUISettings.PREFAB_ROOT + "/" + moduleDir;
            JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));

            foreach (JToken pt in manifest["prefabs"] as JArray ?? new JArray())
            {
                JObject pf = pt as JObject;
                string name = pf?["name"]?.ToString();
                if (string.IsNullOrEmpty(name)) continue;
                if (onlyPrefab != null && name != onlyPrefab) continue; // 只重烤指定 prefab,免动其它已手修视图
                JArray roots = pf["view_roots"] as JArray;
                string viewName = (roots != null && roots.Count > 0) ? roots[0].ToString() : name;

                if (!viewMap.TryGetValue(viewName, out JObject view))
                {
                    skipped++;
                    sb.AppendLine("SKIP  " + name + "  (无快照)");
                    continue;
                }
                JObject tree = view["nodeTree"] as JObject;
                if (tree == null)
                {
                    failed++;
                    sb.AppendLine("FAIL  " + name + "  (快照无 nodeTree)");
                    continue;
                }
                try
                {
                    string outp = prefabRoot + "/" + name + ".prefab";
                    BakeViewTree(tree, name, outp, pf, manifest); // pf=该 prefab 条目(owns_items),manifest=模块 manifest(shared_external)
                    LayaBindFiller.FillPrefab(outp);             // 挂业务组件 + 按节点名回填字段
                    RegisterAddressable(outp, AddressFromPath(outp));
                    baked++;
                    sb.AppendLine("BAKE  " + name + "  → " + outp);
                }
                catch (System.Exception e)
                {
                    failed++;
                    sb.AppendLine("FAIL  " + name + "  " + e.Message);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return string.Format("[BakeModule] {0}: baked={1} skipped={2} failed={3}\n", moduleDir, baked, skipped, failed)
                   + sb;
        }

        // ===== Phase 2 快照烤制:通用重烤(从 manifest 自动派生快照/模块目录)=====
        // UI 在「神霄/LayaUI/转换器」窗口的『快照烤制』折叠区;以下是给窗口/脚本调的纯逻辑,
        // 取代原来写死的「重烤 MainUIActivityView/DownView/脏HUD」单视图菜单。

        private const string ManifestRoot = "Tools/ModuleManifest";

        /// <summary>Tools/ModuleManifest 下所有 *.manifest.json(快照烤制可选的模块清单)。</summary>
        public static string[] ListBakeManifests()
        {
            if (!Directory.Exists(ManifestRoot)) return new string[0];
            string[] files = Directory.GetFiles(ManifestRoot, "*.manifest.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++) files[i] = files[i].Replace('\\', '/');
            return files;
        }

        /// <summary>该 manifest 的视图名清单(= prefabs[].name),给烤制下拉用。</summary>
        public static string[] ListManifestViews(string manifestPath)
        {
            var list = new List<string>();
            if (!File.Exists(manifestPath)) return list.ToArray();
            JObject m = JObject.Parse(File.ReadAllText(manifestPath));
            foreach (JToken pt in m["prefabs"] as JArray ?? new JArray())
            {
                string nm = (pt as JObject)?["name"]?.ToString();
                if (!string.IsNullOrEmpty(nm)) list.Add(nm);
            }
            return list.ToArray();
        }

        /// <summary>
        /// 通用重烤:从 manifest 派生快照目录(snapshots/&lt;manifest基名&gt;)与模块目录(source_prefab 所在文件夹),
        /// 重烤指定视图(viewName=null → 整模块),可选把烤出的视图重嵌回 monolith(source_prefab)。返回状态文本。
        /// </summary>
        public static string RebakeFromManifest(string manifestPath, string viewName, bool reembed)
        {
            if (!File.Exists(manifestPath)) return "[Bake] manifest 不存在: " + manifestPath;
            JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
            string snapshotDir = SnapshotDirFor(manifestPath);
            string moduleDir = ModuleDirFor(manifest);

            var sb = new StringBuilder();
            sb.Append(BakeModuleFromManifest(manifestPath, snapshotDir, moduleDir, viewName));

            if (reembed)
            {
                string monolith = manifest["source_prefab"]?.ToString();
                if (string.IsNullOrEmpty(monolith) || !File.Exists(monolith))
                {
                    sb.AppendLine("[Bake] 跳过重嵌:manifest 无 source_prefab 或文件不存在。");
                }
                else
                {
                    string prefabRoot = LayaUISettings.PREFAB_ROOT + "/" + moduleDir;
                    string[] views = viewName != null ? new[] { viewName } : ListManifestViews(manifestPath);
                    foreach (string v in views)
                    {
                        string baked = prefabRoot + "/" + v + ".prefab";
                        if (File.Exists(baked)) sb.AppendLine(ReplaceModuleSubviewWithBaked(monolith, v, baked));
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sb.ToString();
        }

        /// <summary>mainui.manifest.json → Tools/ModuleManifest/snapshots/mainui。</summary>
        private static string SnapshotDirFor(string manifestPath)
        {
            string dir = (Path.GetDirectoryName(manifestPath) ?? ManifestRoot).Replace('\\', '/');
            string baseName = Path.GetFileName(manifestPath);
            int dot = baseName.IndexOf('.');
            if (dot > 0) baseName = baseName.Substring(0, dot);
            return dir + "/snapshots/" + baseName;
        }

        /// <summary>模块目录(PREFAB_ROOT 下,如 MainUI)= source_prefab 所在文件夹名;回退 manifest.module。</summary>
        private static string ModuleDirFor(JObject manifest)
        {
            string src = manifest["source_prefab"]?.ToString();
            if (!string.IsNullOrEmpty(src))
            {
                string folder = Path.GetFileName(Path.GetDirectoryName(src));
                if (!string.IsNullOrEmpty(folder)) return folder;
            }
            return manifest["module"]?.ToString() ?? "";
        }

        // ——— 内部 ———

        private static JObject FindView(JObject snap, string viewName)
        {
            foreach (JToken vt in snap["views"] as JArray ?? new JArray())
            {
                JObject v = vt as JObject;
                if ((v?["meta"] as JObject)?["name"]?.ToString() == viewName) return v;
            }
            return null;
        }

        /// <summary>快照视图树 → prefab(adapt → 去影子 → 复用 BuildRoot → 嵌共享模板 → 写盘)。返回路径。</summary>
        private static string BakeViewTree(JObject tree, string name, string outPrefabPath,
            JObject prefabEntry = null, JObject moduleManifest = null)
        {
            JObject root = AdaptSnapshotNode(tree);
            var report = new LayaUIReport("bake_" + name);

            // 数据驱动容器去影子:把运行时克隆的重复子节点(活动图标等)从快照里折叠掉,
            // 否则烤成一堆静态影子,盖住业务 View 运行时实例化的动态项(特效就挂在动态项上)。
            // shared_external 项整组丢弃(由完整共享 prefab 当模板);inline-owned 项【留一份】当模板,免 _tpl_X 变 null。
            HashSet<string> known = BuildKnownItemNames(prefabEntry, moduleManifest);
            HashSet<string> sharedNames = BuildSharedNames(moduleManifest);
            int dropped = CollapseClonedItems(root, known, sharedNames, report);

            var manifest = new LayaUIManifest();      // DesignWidth/Height 默认 720×1280
            _bakedSkins = null;                        // 快照已带运行时解析后的 skin
            GameObject go = BuildRoot(name, root, manifest, report);

            // item 模板进 __Templates(禁用),供 LayaBindFiller 绑 _tpl_{Name}:
            //  · shared_external:按 guid 嵌一份【完整】共享 prefab 实例;
            //  · inline-owned:把 CollapseClonedItems 留下的那份克隆挪进来(快照 husk,够 _tpl 绑,免重烤丢模板)。
            int nested = BuildItemTemplates(go, prefabEntry, moduleManifest, report);

            Directory.CreateDirectory(Path.GetDirectoryName(outPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(go, outPrefabPath);
            Object.DestroyImmediate(go);
            if (dropped > 0 || nested > 0) report.Save();
            return outPrefabPath;
        }

        // ——— 数据驱动容器去影子 + 共享模板嵌入 ———

        private const int KNOWN_ITEM_THRESHOLD = 2;     // manifest 确认的 item:≥2 即折叠
        private const int HEURISTIC_THRESHOLD = 6;      // 未确认:≥6 才折叠(避开 3~5 个的合法静态重复)

        /// <summary>本视图认得的 item 名集合 = 该 prefab 的 owns_items ∪ 全模块 shared_external 名。</summary>
        private static HashSet<string> BuildKnownItemNames(JObject prefabEntry, JObject moduleManifest)
        {
            var set = new HashSet<string>();
            if (prefabEntry?["owns_items"] is JArray oi)
                foreach (JToken t in oi)
                {
                    string s = t?.ToString();
                    if (!string.IsNullOrEmpty(s)) set.Add(s);
                }
            if (moduleManifest?["shared_external"] is JArray se)
                foreach (JToken t in se)
                {
                    string s = (t as JObject)?["name"]?.ToString();
                    if (!string.IsNullOrEmpty(s)) set.Add(s);
                }
            return set;
        }

        private static int EffectiveThreshold(string name, HashSet<string> known)
            => (known != null && known.Contains(name)) ? KNOWN_ITEM_THRESHOLD : HEURISTIC_THRESHOLD;

        /// <summary>自适应容器:丢子节点会改变算出的尺寸(HBox/VBox,或没显式宽高的 Box)。</summary>
        private static bool IsAutoSizedContainer(JObject node)
        {
            string type = (string)node["type"];
            if (type == "HBox" || type == "VBox") return true;
            if (type == "Box")
            {
                JObject p = node["props"] as JObject;
                bool hasW = p?["width"] != null && p["width"].Type != JTokenType.Null;
                bool hasH = p?["height"] != null && p["height"].Type != JTokenType.Null;
                if (!hasW || !hasH) return true;
            }
            return false;
        }

        /// <summary>把运行时克隆的重复子节点折叠掉。shared/启发式整组丢弃;inline-owned(known 但非 shared)留首份当模板。返回丢弃总数。</summary>
        private static int CollapseClonedItems(JObject root, HashSet<string> known, HashSet<string> sharedNames, LayaUIReport report)
        {
            string rootPath = root["props"]?["name"]?.ToString() ?? root["type"]?.ToString() ?? "root";
            return CollapseRec(root, known, sharedNames, report, rootPath);
        }

        private static int CollapseRec(JObject node, HashSet<string> known, HashSet<string> sharedNames, LayaUIReport report, string path)
        {
            JArray children = node["child"] as JArray;
            if (children == null || children.Count == 0) return 0;

            var counts = new Dictionary<string, int>();
            foreach (JToken ct in children)
            {
                string nm = (ct as JObject)?["props"]?["name"]?.ToString();
                if (string.IsNullOrEmpty(nm)) continue;
                counts.TryGetValue(nm, out int c);
                counts[nm] = c + 1;
            }

            bool autoSized = IsAutoSizedContainer(node);
            var cloneNames = new HashSet<string>();
            foreach (KeyValuePair<string, int> kv in counts)
            {
                if (kv.Value < EffectiveThreshold(kv.Key, known)) continue;
                if (autoSized && !known.Contains(kv.Key))
                {
                    report.Note(path + ": 跳过启发式折叠 '" + kv.Key + "' ×" + kv.Value + "(自适应容器,未经 manifest 确认)");
                    continue;
                }
                cloneNames.Add(kv.Key);
            }

            int dropped = 0;
            if (cloneNames.Count > 0)
            {
                // inline-owned(known 但不在 shared_external):留【首份】当模板(BuildItemTemplates 挪进 __Templates);shared/启发式整组丢。
                var keepFirst = new HashSet<string>();
                foreach (string nm in cloneNames)
                    if (known.Contains(nm) && (sharedNames == null || !sharedNames.Contains(nm))) keepFirst.Add(nm);

                var seenKept = new HashSet<string>(); // 已为该名留过首份
                for (int i = 0; i < children.Count; i++)
                {
                    string nm = (children[i] as JObject)?["props"]?["name"]?.ToString();
                    if (nm == null || !cloneNames.Contains(nm)) continue;
                    if (keepFirst.Contains(nm) && seenKept.Add(nm)) continue; // 该名首次遇到=首份,留
                    children[i].Remove();
                    dropped++;
                    i--; // 删了一个,索引补偿
                }

                foreach (string nm in cloneNames)
                    report.Note(path + ": 折叠运行时克隆 '" + nm + "' ×" + counts[nm] +
                                (keepFirst.Contains(nm) ? " → 留1份当模板、其余丢" : " → 整组丢弃(运行时动态填充)"));
                if (autoSized)
                    report.Approx(path + " 是自适应容器;烤出尺寸只含留存子节点,需核对 anchor/size");
            }

            foreach (JToken ct in children)
            {
                if (ct is JObject co)
                {
                    string nm = co["props"]?["name"]?.ToString() ?? co["type"]?.ToString();
                    dropped += CollapseRec(co, known, sharedNames, report, path + "/" + nm);
                }
            }
            return dropped;
        }

        /// <summary>全模块 shared_external 的 item 名集合(这些项整组丢、由完整共享 prefab 当模板)。</summary>
        private static HashSet<string> BuildSharedNames(JObject moduleManifest)
        {
            var set = new HashSet<string>();
            if (moduleManifest?["shared_external"] is JArray se)
                foreach (JToken t in se)
                {
                    string s = (t as JObject)?["name"]?.ToString();
                    if (!string.IsNullOrEmpty(s)) set.Add(s);
                }
            return set;
        }

        /// <summary>
        /// 把 owns_items 的模板收进禁用的 __Templates,供 LayaBindFiller 绑 _tpl_{Name}:
        ///  · shared_external:按 guid 嵌一份【完整】共享 prefab 实例;
        ///  · inline-owned:把 CollapseClonedItems 留下的那份克隆(快照 husk)挪进来。
        /// 返回收纳的模板数。
        /// </summary>
        private static int BuildItemTemplates(GameObject root, JObject prefabEntry, JObject moduleManifest, LayaUIReport report)
        {
            if (prefabEntry == null || moduleManifest == null) return 0;
            if (!(prefabEntry["owns_items"] is JArray oi)) return 0;

            var sharedGuid = new Dictionary<string, string>();
            if (moduleManifest["shared_external"] is JArray se)
                foreach (JToken t in se)
                {
                    JObject o = t as JObject;
                    string nm = o?["name"]?.ToString();
                    string guid = o?["guid"]?.ToString();
                    if (!string.IsNullOrEmpty(nm) && !string.IsNullOrEmpty(guid)) sharedGuid[nm] = guid;
                }

            GameObject tplRoot = null;
            int built = 0;
            foreach (JToken t in oi)
            {
                string nm = t?.ToString();
                if (string.IsNullOrEmpty(nm)) continue;

                if (sharedGuid.TryGetValue(nm, out string guid))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject shared = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (shared == null) { report.Note("shared item 找不到 prefab: " + nm + " guid=" + guid); continue; }
                    EnsureTplRoot(root, ref tplRoot);
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(shared, tplRoot.transform);
                    inst.name = nm;               // 名字须等于 item 名,_tpl_{nm} 才绑得上
                    inst.SetActive(false);
                    built++;
                    report.Note("__Templates 嵌入共享 item: " + nm + " ← " + assetPath);
                }
                else
                {
                    // inline-owned:CollapseClonedItems 留了首份在容器里,挪进 __Templates(禁用)。
                    Transform kept = FindByNameOutsideTemplates(root.transform, nm);
                    if (kept == null) { report.Note("inline item 没留下克隆(本视图未实例化它,跳过): " + nm); continue; }
                    EnsureTplRoot(root, ref tplRoot);
                    kept.SetParent(tplRoot.transform, false);
                    kept.gameObject.SetActive(false);
                    built++;
                    report.Note("__Templates 收纳 inline item 模板: " + nm + "(快照 husk,够 _tpl 绑;完整结构待该 item 自身 prefab 化)");
                }
            }
            return built;
        }

        private static void EnsureTplRoot(GameObject root, ref GameObject tplRoot)
        {
            if (tplRoot != null) return;
            Transform existing = root.transform.Find("__Templates");
            if (existing != null) { tplRoot = existing.gameObject; tplRoot.SetActive(false); return; }
            tplRoot = new GameObject("__Templates", typeof(RectTransform));
            tplRoot.transform.SetParent(root.transform, false);
            tplRoot.SetActive(false);
        }

        /// <summary>在树里找第一个叫 name、且不在 __Templates 子树下的节点(取留存的 inline 克隆)。</summary>
        private static Transform FindByNameOutsideTemplates(Transform root, string name)
        {
            foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
            {
                if (c.name != name) continue;
                bool underTpl = false;
                for (Transform p = c.parent; p != null; p = p.parent)
                    if (p.name == "__Templates") { underTpl = true; break; }
                if (!underTpl) return c;
            }
            return null;
        }

        /// <summary>快照节点 → 转换器的 {type, props, child}。props 透传布局属性 + 展平 textProps。</summary>
        private static JObject AdaptSnapshotNode(JObject snap)
        {
            var props = new JObject();
            string name = (string)snap["name"];
            if (!string.IsNullOrEmpty(name)) props["name"] = name;

            foreach (string k in BAKE_PASS_PROPS)
            {
                JToken t = snap[k];
                if (t != null && t.Type != JTokenType.Null) props[k] = t;
            }

            JObject tp = snap["textProps"] as JObject;
            if (tp != null)
            {
                foreach (JProperty pr in tp.Properties())
                    if (props[pr.Name] == null) props[pr.Name] = pr.Value;
            }

            string type = (string)snap["type"] ?? "Box";
            JArray children = snap["children"] as JArray;

            // 文字类节点(TextInput/Label/Text/FontClip/HTMLDivElement)的内部子节点是 Laya 的文字渲染节点,
            // 与转换器的 TMP 模板(Text Area/Text/Placeholder)重复,会叠在一起(输入框提示字盖住输入值)→ 不烤子节点。
            // TextInput 的 prompt(提示文字)藏在子节点里,提上来当 placeholder(否则 BuildTextInput 的占位为空)。
            bool textLike = type == "TextInput" || type == "Label" || type == "Text"
                || type == "FontClip" || type == "HTMLDivElement";
            if (type == "TextInput" && props["prompt"] == null)
            {
                string prompt = FindFirstText(children);
                if (!string.IsNullOrEmpty(prompt)) props["prompt"] = prompt;
            }

            var node = new JObject { ["type"] = type, ["props"] = props };
            if (!textLike && children != null && children.Count > 0)
            {
                var arr = new JArray();
                foreach (JToken c in children)
                    if (c is JObject co) arr.Add(AdaptSnapshotNode(co));
                node["child"] = arr;
            }
            return node;
        }

        /// <summary>递归找子节点里第一段文字(给 TextInput 提 prompt 用)。</summary>
        private static string FindFirstText(JArray children)
        {
            if (children == null) return null;
            foreach (JToken ct in children)
            {
                JObject c = ct as JObject;
                if (c == null) continue;
                string t = (c["textProps"] as JObject)?["text"]?.ToString();
                if (!string.IsNullOrEmpty(t)) return t;
                string sub = FindFirstText(c["children"] as JArray);
                if (sub != null) return sub;
            }
            return null;
        }

        /// <summary>注册成 addressable。postEvent:false 全程不发修改事件,避开 MCP 的交互守卫。</summary>
        private static void RegisterAddressable(string prefabPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid)) return;
            var group = settings.FindGroup("Remote_Prefabs") ?? settings.DefaultGroup;
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null) entry.SetAddress(address, false);
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// Path B:把 monolith(如 LoginModule)里某个子视图,替换成对应烤制 prefab 的【嵌套实例】。
        /// 烤制 prefab 仍是可编辑源(改它会更新 monolith 里的嵌套实例);monolith 只是装配,LoginFlow 不用改。
        /// 子视图的业务组件 + 已回填字段随嵌套实例带进来。返回状态。
        /// </summary>
        public static string ReplaceModuleSubviewWithBaked(string modulePrefabPath, string viewName, string bakedPrefabPath)
        {
            GameObject baked = AssetDatabase.LoadAssetAtPath<GameObject>(bakedPrefabPath);
            if (baked == null) return "[B] 烤制 prefab 不存在: " + bakedPrefabPath;

            GameObject module = PrefabUtility.LoadPrefabContents(modulePrefabPath);
            try
            {
                Transform old = module.transform.Find(viewName);
                bool hadOld = old != null;   // 在 DestroyImmediate 前判,否则 Unity 把已销毁对象当 null
                int idx = hadOld ? old.GetSiblingIndex() : module.transform.childCount;
                bool wasActive = hadOld && old.gameObject.activeSelf;
                if (hadOld) Object.DestroyImmediate(old.gameObject);

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(baked, module.transform);
                inst.name = viewName;
                inst.transform.SetSiblingIndex(idx);
                inst.SetActive(wasActive);

                PrefabUtility.SaveAsPrefabAsset(module, modulePrefabPath);
                return "[B] OK " + (hadOld ? "替换" : "新增") + " " + viewName + " → 嵌套 " + bakedPrefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(module);
            }
        }

        /// <summary>Assets/Prefabs/UI/Login/LoginCreateRoleView.prefab → prefabs/ui/login/logincreateroleview</summary>
        private static string AddressFromPath(string assetPath)
        {
            string rel = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;
            int dot = rel.LastIndexOf('.');
            if (dot > 0) rel = rel.Substring(0, dot);
            return rel.Replace('\\', '/').ToLowerInvariant();
        }
    }
}
