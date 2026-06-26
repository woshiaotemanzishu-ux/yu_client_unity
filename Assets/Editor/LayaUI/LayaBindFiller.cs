using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 第二步:Bind 脚本编译通过后,把组件挂上 prefab 并按节点名回填字段引用。
    /// (生成 cs 和回填必须分两步——新脚本要等 Unity 编译一轮才能 AddComponent。)
    /// </summary>
    public static class LayaBindFiller
    {
        public static void FillModule(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) return;
            string moduleDir = manifest.ModuleDir(module);
            string folder = LayaUISettings.PREFAB_ROOT + "/" + moduleDir;
            if (!Directory.Exists(folder))
            {
                Debug.LogError("[LayaUI] 目录不存在: " + folder);
                return;
            }
            int ok = 0, miss = 0;
            foreach (string file in Directory.GetFiles(folder, "*.prefab", SearchOption.TopDirectoryOnly))
            {
                string path = file.Replace('\\', '/');
                if (FillOne(path, moduleDir, manifest)) ok++; else miss++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LayaUI] 回填完成: 成功 " + ok + ",缺 Bind 类 " + miss + "(缺的看 Console 前面的警告)");
        }

        /// <summary>给单个 prefab 挂 Bind 组件并回填字段(烤制器/单页验证用,复用 FillOne)。</summary>
        public static bool FillPrefab(string prefabPath)
        {
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) { Debug.LogError("[LayaUI] manifest 缺失,无法回填"); return false; }
            return FillOne(prefabPath, ModuleDirFromPath(prefabPath), manifest);
        }

        private static bool FillOne(string prefabPath, string moduleDir, LayaUIManifest manifest)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int filled = 0;
                // 单窗口 prefab:Bind 在根上;合并 prefab:Bind 在各窗口子根上
                if (FillWindow(root.transform, root.transform.name, moduleDir, manifest)) filled++;
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    Transform child = root.transform.GetChild(i);
                    if (child.name == "__Templates") continue;
                    if (FillWindow(child, child.name, moduleDir, manifest)) filled++;
                }
                // 烤制器把重复列表项(LoginSelectRoleItem 等)直接烤进容器(不是 __Templates),
                // 上面 FillWindow 只处理窗口根/模板;这里全树递归,给任何"名字匹配 {Name}Bind 类"的
                // 内嵌节点补挂 + 回填(字段相对该 item 根)。item 的 View 靠 GetComponent<{Item}Bind>()
                // 取字段,漏挂会被当 null 跳过 → 烤制快照文字漏出(选角"1 个角色却显示 3 个"的根因)。
                filled += FillNestedItemBinds(root.transform, moduleDir, manifest);
                if (filled == 0)
                {
                    Debug.LogWarning("[LayaUI] " + prefabPath + " 没匹配到任何 Bind 类(还没编译?)");
                    return false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 全树递归:给名字匹配 {Name}Bind 类的内嵌节点(烤进容器的重复列表项,如 LoginSelectRoleItem)
        /// 补挂业务 Bind + 回填字段(字段相对该 item 根)。窗口根/直接子窗口已由 FillWindow 处理,这里
        /// 补的是更深层、烤进容器的 item 节点。EnsureBindOnWindow 名字不匹配时返回 false(纯结构节点跳过)。
        /// 嵌套 prefab 实例从源继承组件,跳过其子树避免在宿主上产生组件覆盖(与 FillAll 同策略)。
        /// </summary>
        private static int FillNestedItemBinds(Transform node, string moduleDir, LayaUIManifest manifest)
        {
            int count = 0;
            for (int i = 0; i < node.childCount; i++)
            {
                Transform c = node.GetChild(i);
                if (c.name == "__Templates") continue;
                if (PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject) != null) continue;
                string discard;
                if (EnsureBindOnWindow(c, c.name, moduleDir, manifest, null, out discard)) count++;
                count += FillNestedItemBinds(c, moduleDir, manifest);
            }
            return count;
        }

        /// <summary>
        /// 给一个窗口根挂 Bind 组件并按节点名回填字段。没有对应类时返回 false。
        /// 优先挂业务子类(如 LoginEnterView : LoginEnterViewBind),业务逻辑才会随 prefab 实例化。
        /// </summary>
        private static bool FillWindow(Transform windowRoot, string windowName, string moduleDir,
            LayaUIManifest manifest, BackfillStats stats = null)
        {
            string resolvedModuleDir;
            bool matched = EnsureBindOnWindow(windowRoot, windowName, moduleDir, manifest, stats, out resolvedModuleDir);
            if (!matched) return false;

            // __Templates 下的列表项模板同样挂 {ItemName}Bind 并回填(字段相对模板根),
            // 业务 Instantiate 模板后 GetComponent 即得字段(克隆自动重映射内部引用)
            Transform tplRoot = windowRoot.Find("__Templates");
            if (tplRoot != null)
            {
                for (int i = 0; i < tplRoot.childCount; i++)
                {
                    Transform tpl = tplRoot.GetChild(i);
                    FillWindow(tpl, tpl.name, resolvedModuleDir, manifest, stats);
                }
            }
            return true;
        }

        /// <summary>
        /// 核心:解析窗口名对应的 {Name}Bind 类型,缺则挂业务子类、基类→子类升级、按节点名回填序列化引用。
        /// 不递归 __Templates(模板策略交调用方)。匹配到 Bind 类型返回 true。
        /// <paramref name="stats"/> 非空时统计补挂/升级/回填数量并标记本 prefab 是否变更(回填工具用)。
        /// </summary>
        private static bool EnsureBindOnWindow(Transform windowRoot, string windowName, string moduleDir,
            LayaUIManifest manifest, BackfillStats stats, out string resolvedModuleDir)
        {
            LayaUIManifest.SceneEntry sceneEntry = ResolveSceneEntry(windowName, moduleDir, manifest, out resolvedModuleDir);
            Type bindType = FindBindType("Shenxiao.Generated.UI." + resolvedModuleDir + "." + windowName + "Bind");
            if (bindType == null) return false;
            Type concreteType = FindSingleSubclass(bindType) ?? bindType;

            Component bind = windowRoot.GetComponent(bindType);
            if (bind != null && concreteType != bind.GetType())
            {
                UnityEngine.Object.DestroyImmediate(bind, true); // 升级成业务子类
                bind = null;
                if (stats != null) { stats.ComponentsUpgraded++; stats.CurrentChanged = true; }
            }
            bool added = false;
            if (bind == null)
            {
                bind = windowRoot.gameObject.AddComponent(concreteType);
                added = true;
                if (stats != null)
                {
                    stats.ComponentsAdded++;
                    stats.CurrentChanged = true;
                    stats.AddedLog.Add(windowName + " : " + concreteType.Name);
                }
            }

            // codeNodes 与生成器同源(manifest),否则无前缀字段收集不到
            HashSet<string> codeNodes = null;
            if (sceneEntry != null)
            {
                codeNodes = new HashSet<string>(sceneEntry.CodeNodes ?? new List<string>());
            }

            List<LayaBindGenerator.FieldInfo> fields = new List<LayaBindGenerator.FieldInfo>();
            LayaUIReport dummy = new LayaUIReport("bindfill");
            LayaBindGenerator.Collect(windowRoot, windowRoot, fields, new HashSet<string>(), dummy, windowName, codeNodes);

            SerializedObject so = new SerializedObject(bind);
            bool anyRefChanged = false;
            foreach (LayaBindGenerator.FieldInfo f in fields)
            {
                SerializedProperty prop = so.FindProperty(f.FieldName);
                if (prop == null) continue;
                Transform t = windowRoot.Find(f.NodePath);
                if (t == null) continue;
                // 用字段【声明类型】解析,而非节点当前组件:烤制后 Box 节点常多挂了 Image
                // (Laya 带 graphics 的 Box→Image),按 f.TypeName(=Image)会把 Image 塞进
                // RectTransform 字段→类型不匹配置空(_box_con/checkBtn 等容器字段漏绑的根因)。
                UnityEngine.Object newRef = ResolveRef(t, TypeFromProp(prop, f.TypeName));
                if (prop.objectReferenceValue != newRef)
                {
                    prop.objectReferenceValue = newRef;
                    anyRefChanged = true;
                    if (stats != null && !added) stats.RefsFilled++;
                }
            }
            if (anyRefChanged) so.ApplyModifiedPropertiesWithoutUndo();
            if (anyRefChanged && stats != null) stats.CurrentChanged = true;
            return true;
        }

        private static LayaUIManifest.SceneEntry ResolveSceneEntry(string windowName, string preferredModuleDir,
            LayaUIManifest manifest, out string resolvedModuleDir)
        {
            resolvedModuleDir = preferredModuleDir;
            if (manifest == null || manifest.Scenes == null) return null;

            LayaUIManifest.SceneEntry fallback = null;
            foreach (KeyValuePair<string, LayaUIManifest.SceneEntry> kv in manifest.Scenes)
            {
                LayaUIManifest.SceneEntry entry = kv.Value;
                if (entry == null || entry.Name != windowName) continue;

                string entryModuleDir = manifest.ModuleDir(entry.Module);
                if (entryModuleDir == preferredModuleDir)
                {
                    resolvedModuleDir = entryModuleDir;
                    return entry;
                }

                if (fallback == null || IsReusableTemplate(entry) && !IsReusableTemplate(fallback))
                {
                    fallback = entry;
                }
            }

            if (fallback != null)
            {
                resolvedModuleDir = manifest.ModuleDir(fallback.Module);
            }
            return fallback;
        }

        private static bool IsReusableTemplate(LayaUIManifest.SceneEntry entry)
        {
            return entry.Decision == "inline" || entry.Decision == "shared-prefab";
        }

        /// <summary>从序列化属性类型 "PPtr&lt;$RectTransform&gt;" 提取声明类型名 RectTransform;失败回退采集类型。</summary>
        private static string TypeFromProp(SerializedProperty prop, string fallback)
        {
            string t = prop != null ? prop.type : null;
            if (string.IsNullOrEmpty(t)) return fallback;
            int s = t.IndexOf('$');
            int e = t.LastIndexOf('>');
            return (s >= 0 && e > s) ? t.Substring(s + 1, e - s - 1) : fallback;
        }

        private static UnityEngine.Object ResolveRef(Transform t, string typeName)
        {
            switch (typeName)
            {
                case "TMP_InputField": return t.GetComponent<TMP_InputField>();
                case "TextMeshProUGUI": return t.GetComponent<TextMeshProUGUI>();
                case "ScrollRect": return t.GetComponent<ScrollRect>();
                case "Image": return t.GetComponent<Image>();
                case "GameObject": return t.gameObject;
                default: return t as RectTransform;
            }
        }

        private static Type FindBindType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(fullName);
                if (t != null) return t;
            }
            // 大小写不敏感兜底:转换器把 Bind 类名首字母大写(如 LonglanguageViewBind),
            // 而 manifest/prefab 场景名可能小写(longlanguageView)→ 精确查不到时按全名忽略大小写再找一次。
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = asm.GetType(fullName, false, true);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        /// <summary>找 Bind 的唯一业务子类;0 个返回 null,多个报错并用基类。</summary>
        private static Type FindSingleSubclass(Type bindType)
        {
            Type found = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                foreach (Type t in types)
                {
                    if (t == null || t.IsAbstract || !bindType.IsAssignableFrom(t) || t == bindType) continue;
                    if (found != null)
                    {
                        Debug.LogError("[LayaUI] " + bindType.Name + " 有多个业务子类(" + found.Name + ", " + t.Name + "),用基类回填");
                        return null;
                    }
                    found = t;
                }
            }
            return found;
        }

        // ===================== 全量回填工具(菜单 神霄/LayaUI/高级/回填 Bind 组件)=====================
        //
        // 背景:第二步回填原按"模块"跑(FillModule),量产期 view-prefab 都覆盖到了;但 shared-prefab
        // /standalone 这类不随某模块流水线重转的共享件(如 common/BaseAwardItem)会漏挂 Bind 组件,
        // 经 ResManager.InstantiateAsync + GetComponent<BaseAwardItem>() 拿到 null。本工具是 FillModule
        // 的全量幂等版:扫 Assets/Prefabs/UI/** 所有 prefab,给缺组件的窗口根/内联模板按 *Bind 节点名补挂 +
        // 回填,仅对真有变更的 prefab 写盘(避免无谓 diff),出报告。可重跑。

        /// <summary>回填统计(报告 + "仅变更才写盘")。</summary>
        public sealed class BackfillStats
        {
            public int PrefabsScanned;
            public int PrefabsChanged;
            public int ComponentsAdded;
            public int ComponentsUpgraded;
            public int RefsFilled;
            public readonly List<string> AddedLog = new List<string>();
            public readonly List<string> ChangedPrefabs = new List<string>();
            internal bool CurrentChanged;
        }

        [MenuItem("神霄/LayaUI/高级/回填 Bind 组件", priority = 100)]
        public static void MenuBackfill()
        {
            BackfillStats s = FillAll(false);
            Debug.Log(string.Format(
                "[LayaUI] 回填 Bind 组件 完成:扫描 {0} prefab,补挂 {1}、升级 {2}、回填引用 {3},改动 {4} 个 prefab。报告:{5}",
                s.PrefabsScanned, s.ComponentsAdded, s.ComponentsUpgraded, s.RefsFilled, s.PrefabsChanged, ReportPath));
        }

        [MenuItem("神霄/LayaUI/高级/回填 Bind 组件(预览不写盘)", priority = 101)]
        public static void MenuBackfillDryRun()
        {
            BackfillStats s = FillAll(true);
            Debug.Log(string.Format(
                "[LayaUI] 回填 Bind 组件 预览(未写盘):扫描 {0} prefab,将补挂 {1}、升级 {2}、回填引用 {3},涉及 {4} 个 prefab。报告:{5}",
                s.PrefabsScanned, s.ComponentsAdded, s.ComponentsUpgraded, s.RefsFilled, s.PrefabsChanged, ReportPath));
        }

        private static string ReportPath { get { return LayaUISettings.REPORT_ROOT + "/bind_backfill_report.md"; } }

        /// <summary>
        /// 扫 <see cref="LayaUISettings.PREFAB_ROOT"/> 下所有 prefab,给缺 Bind 组件的窗口根(及内联模板)按
        /// 节点名补挂业务子类 + 回填序列化引用。一次性、可重跑、幂等;仅对真有变更的 prefab 写盘;出 Markdown 报告。
        /// 嵌套 prefab 实例(共享件,如各宿主里的 BaseAwardItem)不在此重复挂——它们从源 prefab 继承组件。
        /// </summary>
        public static BackfillStats FillAll(bool dryRun)
        {
            BackfillStats stats = new BackfillStats();
            LayaUIManifest manifest = LayaUIManifest.Load(true);
            if (manifest == null) { Debug.LogError("[LayaUI] manifest 缺失,无法回填"); return stats; }
            string root = LayaUISettings.PREFAB_ROOT;
            if (!Directory.Exists(root)) { Debug.LogError("[LayaUI] 目录不存在: " + root); return stats; }

            string[] files = Directory.GetFiles(root, "*.prefab", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                stats.PrefabsScanned++;
                string moduleDir = ModuleDirFromPath(path);
                GameObject go = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    stats.CurrentChanged = false;
                    string discard;
                    EnsureBindOnWindow(go.transform, go.transform.name, moduleDir, manifest, stats, out discard);
                    for (int i = 0; i < go.transform.childCount; i++)
                    {
                        Transform child = go.transform.GetChild(i);
                        if (child.name == "__Templates") continue;
                        EnsureBindOnWindow(child, child.name, moduleDir, manifest, stats, out discard);
                    }
                    // 内联模板(非嵌套 prefab 实例)补组件;嵌套实例从源继承,跳过避免在宿主上产生组件覆盖。
                    FillInlineTemplates(go.transform, moduleDir, manifest, stats);

                    if (stats.CurrentChanged)
                    {
                        stats.PrefabsChanged++;
                        stats.ChangedPrefabs.Add(path);
                        if (!dryRun) PrefabUtility.SaveAsPrefabAsset(go, path);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(go);
                }
            }

            // 每个 prefab 已由 PrefabUtility.SaveAsPrefabAsset 单独持久化;不调用全局 AssetDatabase.SaveAssets(),
            // 避免把会话里其它脏资产(如运行期累积字形的 TMP 动态字体图集)一并刷盘产生无关 diff。
            WriteReport(stats, dryRun);
            return stats;
        }

        /// <summary>递归窗口的 __Templates,给内联模板(非嵌套 prefab 实例)挂 ItemBind 子类。</summary>
        private static void FillInlineTemplates(Transform window, string moduleDir, LayaUIManifest manifest, BackfillStats stats)
        {
            Transform tplRoot = window.Find("__Templates");
            if (tplRoot != null)
            {
                for (int i = 0; i < tplRoot.childCount; i++)
                {
                    Transform tpl = tplRoot.GetChild(i);
                    // 嵌套 prefab 实例从源继承组件,跳过(避免宿主上的 m_AddedComponents 覆盖与重复)
                    if (PrefabUtility.GetCorrespondingObjectFromSource(tpl.gameObject) != null) continue;
                    string discard;
                    EnsureBindOnWindow(tpl, tpl.name, moduleDir, manifest, stats, out discard);
                    FillInlineTemplates(tpl, moduleDir, manifest, stats);
                }
            }
            for (int i = 0; i < window.childCount; i++)
            {
                Transform child = window.GetChild(i);
                if (child.name == "__Templates") continue;
                FillInlineTemplates(child, moduleDir, manifest, stats);
            }
        }

        /// <summary>从 prefab 路径取 PREFAB_ROOT 下第一段子目录作为模块目录(给 ResolveSceneEntry 当首选)。</summary>
        private static string ModuleDirFromPath(string prefabPath)
        {
            string rootPrefix = LayaUISettings.PREFAB_ROOT + "/";
            if (prefabPath.StartsWith(rootPrefix))
            {
                string rest = prefabPath.Substring(rootPrefix.Length);
                int slash = rest.IndexOf('/');
                if (slash > 0) return rest.Substring(0, slash);
            }
            return "";
        }

        private static void WriteReport(BackfillStats stats, bool dryRun)
        {
            try
            {
                Directory.CreateDirectory(LayaUISettings.REPORT_ROOT);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Bind 组件回填报告" + (dryRun ? "(预览,未写盘)" : ""));
                sb.AppendLine();
                sb.AppendLine("- 扫描 prefab: " + stats.PrefabsScanned);
                sb.AppendLine("- 补挂组件: " + stats.ComponentsAdded);
                sb.AppendLine("- 升级基类→子类: " + stats.ComponentsUpgraded);
                sb.AppendLine("- 回填引用: " + stats.RefsFilled);
                sb.AppendLine("- 改动 prefab: " + stats.PrefabsChanged);
                sb.AppendLine();
                if (stats.AddedLog.Count > 0)
                {
                    sb.AppendLine("## 补挂的组件(窗口 : 类型)");
                    foreach (string line in stats.AddedLog) sb.AppendLine("- " + line);
                    sb.AppendLine();
                }
                if (stats.ChangedPrefabs.Count > 0)
                {
                    sb.AppendLine("## 改动的 prefab");
                    foreach (string line in stats.ChangedPrefabs) sb.AppendLine("- " + line);
                }
                File.WriteAllText(ReportPath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LayaUI] 回填报告写入失败: " + e.Message);
            }
        }
    }
}
