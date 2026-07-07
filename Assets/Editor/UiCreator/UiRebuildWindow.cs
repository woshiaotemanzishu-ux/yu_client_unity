using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 逐页重构 UI 的生成器面板(取代一堆零散菜单)。
    /// 菜单「神霄/重构UI 生成器」打开;条目来自 UiRebuildRegistry,按模块分组。
    ///
    /// 2026-07-06 改版(条目多了密密麻麻,找东西难):
    /// - 顶部搜索框:按 名字/说明/模块 过滤;搜索时命中模块自动展开。
    /// - 模块折叠(状态存 EditorPrefs),标题带条目数 + 「全部生成」批量按钮。
    /// - 条目压成单行:状态灯(●已生成/○未生成) + 名字(说明进悬浮提示) + ①生成/②预览/定位。
    /// 后续加页面仍只往注册表加,不改本窗口。
    /// </summary>
    public sealed class UiRebuildWindow : EditorWindow
    {
        private const string FoldPrefsPrefix = "UiRebuild.Fold.";

        private Vector2 _scroll;
        private string _search = string.Empty;

        private static readonly Color DotDone = new Color(0.35f, 0.85f, 0.45f);
        private static readonly Color DotMissing = new Color(0.55f, 0.55f, 0.55f);

        [MenuItem("神霄/重构UI 生成器", priority = 8)]
        public static void Open()
        {
            var w = GetWindow<UiRebuildWindow>("重构UI 生成器");
            w.minSize = new Vector2(420f, 300f);
            w.Show();
        }

        private void OnGUI()
        {
            var entries = UiRebuildRegistry.Entries;

            // ---------- 顶栏:标题 + 搜索 + 展开/收起 ----------
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("纯代码建树 → prefab(贴老端源图)→ 编辑器手调", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(180f));
                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20f))) _search = string.Empty;
                if (GUILayout.Button("展开", EditorStyles.toolbarButton, GUILayout.Width(40f))) SetAllFold(entries, true);
                if (GUILayout.Button("收起", EditorStyles.toolbarButton, GUILayout.Width(40f))) SetAllFold(entries, false);
            }

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有注册任何生成器。\n(若刚改完代码,等编译完成后会自动出现。)", MessageType.Info);
                return;
            }

            bool searching = !string.IsNullOrEmpty(_search);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var group in entries.GroupBy(e => e.Module).OrderBy(g => g.Key))
            {
                List<UiCreatorEntry> list = group.OrderBy(x => x.Order).ThenBy(x => x.Name).ToList();
                if (searching)
                {
                    list = list.Where(Match).ToList();
                    if (list.Count == 0) continue;
                }

                // ---------- 模块头:折叠 + 计数 + 批量生成 ----------
                string foldKey = FoldPrefsPrefix + group.Key;
                bool folded = searching || EditorPrefs.GetBool(foldKey, true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool now = EditorGUILayout.Foldout(folded, group.Key + "  (" + list.Count + ")", true, EditorStyles.foldoutHeader);
                    if (!searching && now != folded) EditorPrefs.SetBool(foldKey, now);
                    folded = searching || now;

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("全部生成", GUILayout.Width(68f))
                        && EditorUtility.DisplayDialog("批量生成:" + group.Key,
                            "依次执行本模块 " + list.Count + " 个条目的「生成预制体」,已有的会被覆盖重建。继续?", "生成", "取消"))
                    {
                        foreach (var e in list) e.Generate?.Invoke();
                        AutoFillSlots(group.Key);
                    }
                }

                if (folded)
                {
                    foreach (var e in list) RenderRow(e);
                }
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.EndScrollView();
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

        private static void SetAllFold(IReadOnlyList<UiCreatorEntry> entries, bool open)
        {
            foreach (string m in entries.Select(e => e.Module).Distinct())
            {
                EditorPrefs.SetBool(FoldPrefsPrefix + m, open);
            }
        }

        // ---------- 单行条目:状态灯 + 名字(说明进 tooltip) + 三个按钮 ----------
        private static void RenderRow(UiCreatorEntry e)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16f);

                bool exists = !string.IsNullOrEmpty(e.PrefabPath) && File.Exists(e.PrefabPath);
                Color old = GUI.color;
                GUI.color = exists ? DotDone : DotMissing;
                GUILayout.Label("●", GUILayout.Width(14f));
                GUI.color = old;

                string tip = (e.Note ?? string.Empty)
                    + (string.IsNullOrEmpty(e.PrefabPath) ? "" : "\n" + e.PrefabPath)
                    + (exists ? "\n(已生成)" : "\n(未生成)");
                GUILayout.Label(new GUIContent(e.Name, tip), GUILayout.MinWidth(150f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("①生成", "生成/覆盖重建 prefab(完成后自动回填动态资源槽)"), GUILayout.Width(56f)))
                {
                    e.Generate?.Invoke();
                    AutoFillSlots(e.Module);
                }

                using (new EditorGUI.DisabledScope(e.Preview == null))
                {
                    if (GUILayout.Button(new GUIContent("②预览", "运行时预览(多数需 Play 模式)"), GUILayout.Width(56f)))
                    {
                        e.Preview?.Invoke();
                    }
                }

                using (new EditorGUI.DisabledScope(!exists))
                {
                    if (GUILayout.Button("定位", GUILayout.Width(44f)))
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
}
