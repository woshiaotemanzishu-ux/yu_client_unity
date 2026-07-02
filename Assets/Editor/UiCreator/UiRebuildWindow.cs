using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 逐页重构 UI 的生成器面板(取代一堆零散菜单)。
    /// 菜单「神霄/重构UI 生成器」打开;条目来自 UiRebuildRegistry,按模块分组,
    /// 每条提供 ①生成预制体 / ②运行时预览 / 定位 三个按钮。后续加页面只往注册表加,不改本窗口。
    /// </summary>
    public sealed class UiRebuildWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("神霄/重构UI 生成器", priority = 8)]
        public static void Open()
        {
            var w = GetWindow<UiRebuildWindow>("重构UI 生成器");
            w.minSize = new Vector2(380f, 280f);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("逐页重构 UI · 预制体生成器", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("纯代码建树 → 生成 prefab(已贴老端源图/占位色)→ 你在编辑器手调样式。",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);

            var entries = UiRebuildRegistry.Entries;
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有注册任何生成器。\n(若刚改完代码,等编译完成后会自动出现。)", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var group in entries.GroupBy(e => e.Module).OrderBy(g => g.Key))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                foreach (var e in group.OrderBy(x => x.Order).ThenBy(x => x.Name))
                {
                    RenderEntry(e);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void RenderEntry(UiCreatorEntry e)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(e.Name);
                if (!string.IsNullOrEmpty(e.Note))
                {
                    EditorGUILayout.LabelField(e.Note, EditorStyles.miniLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("① 生成预制体"))
                    {
                        e.Generate?.Invoke();
                    }

                    using (new EditorGUI.DisabledScope(e.Preview == null))
                    {
                        if (GUILayout.Button("② 运行时预览"))
                        {
                            e.Preview?.Invoke();
                        }
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(e.PrefabPath)))
                    {
                        if (GUILayout.Button("定位", GUILayout.Width(56f)))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(e.PrefabPath);
                            if (obj != null)
                            {
                                Selection.activeObject = obj;
                                EditorGUIUtility.PingObject(obj);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("定位", "尚未生成:\n" + e.PrefabPath, "好");
                            }
                        }
                    }
                }
            }
        }
    }
}
