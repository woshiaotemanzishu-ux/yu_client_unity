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
        public static string BakeModuleFromManifest(string manifestPath, string snapshotDir, string moduleDir)
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
                    BakeViewTree(tree, name, outp);
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

        /// <summary>快照视图树 → prefab(adapt → 复用 BuildRoot → 写盘)。返回路径。</summary>
        private static string BakeViewTree(JObject tree, string name, string outPrefabPath)
        {
            JObject root = AdaptSnapshotNode(tree);
            var manifest = new LayaUIManifest();      // DesignWidth/Height 默认 720×1280
            var report = new LayaUIReport("bake_" + name);
            _bakedSkins = null;                        // 快照已带运行时解析后的 skin
            GameObject go = BuildRoot(name, root, manifest, report);
            Directory.CreateDirectory(Path.GetDirectoryName(outPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(go, outPrefabPath);
            Object.DestroyImmediate(go);
            return outPrefabPath;
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
