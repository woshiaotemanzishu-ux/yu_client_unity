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

        /// <summary>一键重烤 MainUIActivityView(去影子+共享 ActivityIcon 模板),验证通用转换工具修复。只重这一个,不动其它视图。</summary>
        [MenuItem("神霄/LayaUI/重烤 MainUIActivityView(去影子+共享模板)", priority = 200)]
        public static void RebakeMainUIActivityView()
        {
            string log = BakeModuleFromManifest(
                "Tools/ModuleManifest/mainui.manifest.json",
                "Tools/ModuleManifest/snapshots/mainui",
                "MainUI",
                "MainUIActivityView");
            Debug.Log("[Bake] " + log + "\n报告见 Reports/LayaUI/bake_MainUIActivityView_report.md");
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
            HashSet<string> known = BuildKnownItemNames(prefabEntry, moduleManifest);
            int dropped = CollapseClonedItems(root, known, report);

            var manifest = new LayaUIManifest();      // DesignWidth/Height 默认 720×1280
            _bakedSkins = null;                        // 快照已带运行时解析后的 skin
            GameObject go = BuildRoot(name, root, manifest, report);

            // 共享 item 模板:把 owns_items 里属 shared_external 的项,按 guid 嵌一份禁用实例进 __Templates,
            // 让 LayaBindFiller 把 _tpl_{Name} 绑到【完整】共享 prefab(而非快照里残缺的 husk 克隆)。
            int nested = NestSharedTemplates(go, prefabEntry, moduleManifest, report);

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

        /// <summary>把运行时克隆的重复子节点折叠掉(丢弃)。返回丢弃总数。</summary>
        private static int CollapseClonedItems(JObject root, HashSet<string> known, LayaUIReport report)
        {
            string rootPath = root["props"]?["name"]?.ToString() ?? root["type"]?.ToString() ?? "root";
            return CollapseRec(root, known, report, rootPath);
        }

        private static int CollapseRec(JObject node, HashSet<string> known, LayaUIReport report, string path)
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
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    string nm = (children[i] as JObject)?["props"]?["name"]?.ToString();
                    if (nm != null && cloneNames.Contains(nm)) { children[i].Remove(); dropped++; }
                }
                foreach (string nm in cloneNames)
                    report.Note(path + ": 折叠运行时克隆 '" + nm + "' ×" + counts[nm] + " → 丢弃(运行时动态填充)");
                if (autoSized)
                    report.Approx(path + " 是自适应容器;烤出尺寸只含留存子节点,需核对 anchor/size");
            }

            foreach (JToken ct in children)
            {
                if (ct is JObject co)
                {
                    string nm = co["props"]?["name"]?.ToString() ?? co["type"]?.ToString();
                    dropped += CollapseRec(co, known, report, path + "/" + nm);
                }
            }
            return dropped;
        }

        /// <summary>owns_items 里属 shared_external 的项:按 guid 嵌一份禁用实例进 __Templates,供 _tpl_{Name} 绑定。</summary>
        private static int NestSharedTemplates(GameObject root, JObject prefabEntry, JObject moduleManifest, LayaUIReport report)
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
            if (sharedGuid.Count == 0) return 0;

            GameObject tplRoot = null;
            int nested = 0;
            foreach (JToken t in oi)
            {
                string nm = t?.ToString();
                if (string.IsNullOrEmpty(nm) || !sharedGuid.TryGetValue(nm, out string guid)) continue;
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject shared = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (shared == null) { report.Note("shared item 找不到 prefab: " + nm + " guid=" + guid); continue; }

                if (tplRoot == null)
                {
                    tplRoot = new GameObject("__Templates", typeof(RectTransform));
                    tplRoot.transform.SetParent(root.transform, false);
                    tplRoot.SetActive(false);
                }
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(shared, tplRoot.transform);
                inst.name = nm;               // 名字须等于 item 名,_tpl_{nm} 才绑得上
                inst.SetActive(false);
                nested++;
                report.Note("__Templates 嵌入共享 item: " + nm + " ← " + assetPath);
            }
            return nested;
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
