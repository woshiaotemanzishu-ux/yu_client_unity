using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    public sealed class AssetAssemblyCandidate
    {
        public string Name;
        public string Key;
        public string AssetPath;
    }

    public sealed class AssetAssemblyPickerWindow : EditorWindow
    {
        private List<AssetAssemblyCandidate> _candidates = new List<AssetAssemblyCandidate>();
        private Action<AssetAssemblyCandidate> _onPick;
        private string _search = "";
        private Vector2 _scroll;

        public static void Open(string title, List<AssetAssemblyCandidate> candidates,
            Action<AssetAssemblyCandidate> onPick)
        {
            var w = GetWindow<AssetAssemblyPickerWindow>(true, title);
            w.minSize = new Vector2(560f, 440f);
            w._candidates = candidates ?? new List<AssetAssemblyCandidate>();
            w._onPick = onPick;
            w._search = "";
            w._scroll = Vector2.zero;
            w.ShowUtility();
        }

        public static List<AssetAssemblyCandidate> ScanModels()
        {
            return Scan("t:Prefab", new[] { "Assets/GameRes/object" });
        }

        public static List<AssetAssemblyCandidate> ScanActions()
        {
            return Scan("t:AnimationClip", new[] { "Assets/GameRes/object" });
        }

        public static List<AssetAssemblyCandidate> ScanActions(string actionGroup)
        {
            string folder = string.IsNullOrEmpty(actionGroup)
                ? "Assets/GameRes/object/role/action"
                : "Assets/GameRes/object/role/action/" + actionGroup;
            return Scan("t:AnimationClip", new[] { folder });
        }

        public static List<AssetAssemblyCandidate> ScanEffects()
        {
            return Scan("t:Prefab", new[] { "Assets/GameRes/effect/objs" });
        }

        private static List<AssetAssemblyCandidate> Scan(string filter, string[] folders)
        {
            var list = new List<AssetAssemblyCandidate>();
            foreach (string guid in AssetDatabase.FindAssets(filter, folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                string key = AssetAssemblyProfileStore.AddressOfAsset(path);
                list.Add(new AssetAssemblyCandidate
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    Key = key,
                    AssetPath = path,
                });
            }
            return list.OrderBy(c => c.Key).ToList();
        }

        private void OnGUI()
        {
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            string needle = (_search ?? string.Empty).Trim().ToLowerInvariant();
            List<AssetAssemblyCandidate> shown = string.IsNullOrEmpty(needle)
                ? _candidates
                : _candidates.Where(c =>
                    c.Name.ToLowerInvariant().Contains(needle)
                    || c.Key.ToLowerInvariant().Contains(needle)).ToList();

            EditorGUILayout.LabelField($"候选 {shown.Count}/{_candidates.Count}", EditorStyles.miniLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (AssetAssemblyCandidate c in shown)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(c.Name, GUILayout.Width(170f), GUILayout.Height(22f)))
                    {
                        _onPick?.Invoke(c);
                        Close();
                    }
                    EditorGUILayout.SelectableLabel(c.Key, EditorStyles.miniLabel, GUILayout.Height(22f));
                    if (GUILayout.Button("定位", GUILayout.Width(44f), GUILayout.Height(20f)))
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(c.AssetPath);
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
