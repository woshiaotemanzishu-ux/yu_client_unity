using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 逐页重构 UI 的生成器面板(取代一堆零散菜单)。
    /// 菜单「神霄/重构UI 生成器」打开;条目来自 UiRebuildRegistry。
    ///
    /// 模块用顶部 Tab 隔离;当前模块的生成器按单列卡片展示,
    /// 名称/状态/操作固定分列,避免长列表中错行点击。
    /// 搜索仍覆盖全部模块,结果沿用同一卡片结构。
    /// </summary>
    public sealed class UiRebuildWindow : EditorWindow
    {
        private const string SelectedModulePrefsKey = "UiRebuild.SelectedModule";
        private const float TabMinWidth = 104f;
        private const float StatusColumnWidth = 76f;
        private const float ActionsColumnWidth = 184f;

        private Vector2 _scroll;
        private string _search = string.Empty;
        private string _selectedModule = string.Empty;

        private static readonly Color DotDone = new Color(0.35f, 0.85f, 0.45f);
        private static readonly Color DotMissing = new Color(0.55f, 0.55f, 0.55f);

        [MenuItem("神霄/重构UI 生成器", priority = 8)]
        public static void Open()
        {
            var w = GetWindow<UiRebuildWindow>("重构UI 生成器");
            w.minSize = new Vector2(620f, 360f);
            w.Show();
        }

        private void OnEnable()
        {
            _selectedModule = EditorPrefs.GetString(SelectedModulePrefsKey, string.Empty);
        }

        private void OnGUI()
        {
            var entries = UiRebuildRegistry.Entries;

            // ---------- 顶栏:说明 + 全局搜索 ----------
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("先选模块，再在卡片内操作", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                string search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
                if (search != _search)
                {
                    _search = search;
                    _scroll = Vector2.zero;
                }

                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20f)))
                {
                    _search = string.Empty;
                    _scroll = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有注册任何生成器。\n(若刚改完代码,等编译完成后会自动出现。)", MessageType.Info);
                return;
            }

            string[] modules = entries
                .Select(e => e.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToArray();
            EnsureSelectedModule(modules);

            bool searching = !string.IsNullOrWhiteSpace(_search);
            if (!searching)
            {
                RenderModuleTabs(entries, modules);
            }
            else
            {
                EditorGUILayout.HelpBox("正在跨全部模块搜索；清空搜索后返回当前模块 Tab。", MessageType.None);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (searching)
            {
                RenderSearchResults(entries);
            }
            else
            {
                List<UiCreatorEntry> list = entries
                    .Where(e => e.Module == _selectedModule)
                    .OrderBy(e => e.Order)
                    .ThenBy(e => e.Name)
                    .ToList();
                RenderModule(_selectedModule, list, false);
            }
            EditorGUILayout.EndScrollView();
        }

        private void EnsureSelectedModule(string[] modules)
        {
            if (modules.Length == 0)
            {
                _selectedModule = string.Empty;
                return;
            }

            if (!modules.Contains(_selectedModule))
            {
                _selectedModule = modules[0];
                EditorPrefs.SetString(SelectedModulePrefsKey, _selectedModule);
            }
        }

        private void RenderModuleTabs(IReadOnlyList<UiCreatorEntry> entries, string[] modules)
        {
            int oldIndex = System.Array.IndexOf(modules, _selectedModule);
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 16f) / TabMinWidth));
            string[] labels = modules
                .Select(module => module + " (" + entries.Count(e => e.Module == module) + ")")
                .ToArray();
            int rows = Mathf.CeilToInt(labels.Length / (float)columns);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int newIndex = GUILayout.SelectionGrid(
                    Mathf.Max(0, oldIndex),
                    labels,
                    columns,
                    EditorStyles.toolbarButton,
                    GUILayout.Height(rows * 24f));
                if (newIndex != oldIndex && newIndex >= 0 && newIndex < modules.Length)
                {
                    _selectedModule = modules[newIndex];
                    EditorPrefs.SetString(SelectedModulePrefsKey, _selectedModule);
                    _scroll = Vector2.zero;
                }
            }
        }

        private void RenderSearchResults(IReadOnlyList<UiCreatorEntry> entries)
        {
            List<UiCreatorEntry> matches = entries
                .Where(Match)
                .OrderBy(e => e.Module)
                .ThenBy(e => e.Order)
                .ThenBy(e => e.Name)
                .ToList();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("搜索结果", EditorStyles.boldLabel);
                GUILayout.Label(matches.Count + " 项", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
            }

            if (matches.Count == 0)
            {
                EditorGUILayout.HelpBox("没有匹配的生成器。", MessageType.Info);
                return;
            }

            foreach (var group in matches.GroupBy(e => e.Module))
            {
                List<UiCreatorEntry> list = group.ToList();
                RenderModule(group.Key, list, true);
                EditorGUILayout.Space(6f);
            }
        }

        private static void RenderModule(string module, List<UiCreatorEntry> list, bool searchResult)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(searchResult ? module + "  (" + list.Count + ")" : module, EditorStyles.boldLabel);
                if (!searchResult)
                {
                    GUILayout.Label(list.Count + " 个界面", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("全部重建", GUILayout.Width(82f), GUILayout.Height(26f))
                        && EditorUtility.DisplayDialog("批量重建:" + module,
                            "依次重建本模块 " + list.Count + " 个 prefab。\n\n这会覆盖 prefab 内的人工调整,仅在明确需要从 Creator 重新出厂时使用。", "重建并覆盖", "取消"))
                    {
                        foreach (var entry in list) entry.Generate?.Invoke();
                        AutoFillSlots(module);
                    }
                }
            }

            RenderColumnHeaders();
            foreach (var entry in list)
            {
                RenderCard(entry);
            }
        }

        private bool Match(UiCreatorEntry e)
        {
            return (e.Name != null && e.Name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (e.Note != null && e.Note.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (e.Module != null && e.Module.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>生成后自动回填 UI 动态资源 Slot(特效槽等):Creator 重建 prefab 会冲掉已回填的槽,
        /// 手点菜单容易忘 —— 生成动作收尾自动补。清单里没有该模块条目时是无害空跑。</summary>
        private static void AutoFillSlots(string module)
        {
            string key = module == "MainUI" ? "mainUI" : module == "Login" ? "login" : null;
            if (key == null) return;
            int n = DynamicResources.UIDynamicResourceSlotFiller.FillModules(new[] { key });
            if (n > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[UiCreator] 生成后已自动回填 " + key + " 动态资源 Slot ×" + n);
            }
        }

        private static void RenderColumnHeaders()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Space(10f);
                GUILayout.Label("界面", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Space(10f);
                GUILayout.Label("状态", EditorStyles.miniBoldLabel, GUILayout.Width(StatusColumnWidth));
                GUILayout.Space(10f);
                GUILayout.Label("操作", EditorStyles.miniBoldLabel, GUILayout.Width(ActionsColumnWidth));
            }
        }

        // ---------- 单列卡片:界面信息 / 状态 / 操作固定分列 ----------
        private static void RenderCard(UiCreatorEntry e)
        {
            bool exists = !string.IsNullOrEmpty(e.PrefabPath) && File.Exists(e.PrefabPath);
            string tip = (e.Note ?? string.Empty)
                + (string.IsNullOrEmpty(e.PrefabPath) ? string.Empty : "\n" + e.PrefabPath)
                + (exists ? "\n(已生成)" : "\n(未生成)");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(220f)))
                    {
                        GUILayout.Label(new GUIContent(e.Name, tip), EditorStyles.boldLabel);
                        if (!string.IsNullOrWhiteSpace(e.Note))
                        {
                            GUILayout.Label(e.Note, EditorStyles.wordWrappedMiniLabel);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Space(10f);
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(StatusColumnWidth)))
                    {
                        Color old = GUI.color;
                        GUI.color = exists ? DotDone : DotMissing;
                        GUILayout.Label("●", GUILayout.Width(14f));
                        GUI.color = old;
                        GUILayout.Label(exists ? "已生成" : "未生成", EditorStyles.miniLabel);
                    }

                    GUILayout.Space(10f);
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(ActionsColumnWidth)))
                    {
                        string generateLabel = exists ? "重建" : "生成";
                        string generateTip = exists
                            ? "从 Creator 覆盖重建 prefab,会丢失人工调整"
                            : "从 Creator 生成 prefab";
                        if (GUILayout.Button(new GUIContent(generateLabel, generateTip), GUILayout.Width(56f), GUILayout.Height(28f))
                            && (!exists || EditorUtility.DisplayDialog("覆盖重建:" + e.Name,
                                "这会覆盖现有 prefab 内的人工调整。确定从 Creator 重建?", "重建并覆盖", "取消")))
                        {
                            e.Generate?.Invoke();
                            AutoFillSlots(e.Module);
                        }

                        using (new EditorGUI.DisabledScope(e.Preview == null))
                        {
                            if (GUILayout.Button(new GUIContent("预览", "运行时预览(多数需 Play 模式)"), GUILayout.Width(56f), GUILayout.Height(28f)))
                            {
                                e.Preview?.Invoke();
                            }
                        }

                        using (new EditorGUI.DisabledScope(!exists))
                        {
                            if (GUILayout.Button("定位", GUILayout.Width(56f), GUILayout.Height(28f)))
                            {
                                var obj = AssetDatabase.LoadAssetAtPath<Object>(e.PrefabPath);
                                if (obj != null)
                                {
                                    Selection.activeObject = obj;
                                    EditorGUIUtility.PingObject(obj);
                                }
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space(3f);
        }
    }
}
