using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Shenxiao.Editor.DynamicResources
{
    [CustomEditor(typeof(UIDynamicResourceSlot), true)]
    public sealed class UIDynamicResourceSlotEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var slot = (UIDynamicResourceSlot)target;
            string assetPath = ResolveAssetPath(slot.AddressKey);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Asset", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(assetPath) ? "(missing) " + slot.AddressKey : assetPath,
                EditorStyles.textField, GUILayout.Height(20f));

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(assetPath)))
            {
                if (GUILayout.Button("Open Runtime Asset"))
                {
                    Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset != null) AssetDatabase.OpenAsset(asset);
                }
                if (GUILayout.Button("Ping Runtime Asset"))
                {
                    Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
            }

            string addressableState = GetAddressableState(assetPath);
            MessageType messageType = addressableState.StartsWith("OK")
                ? MessageType.Info
                : MessageType.Warning;
            EditorGUILayout.HelpBox(addressableState, messageType);
        }

        private static string ResolveAssetPath(string addressKey)
        {
            if (string.IsNullOrEmpty(addressKey)) return "";
            string stem = "Assets/GameRes/" + addressKey.Trim().Replace('\\', '/');
            string[] candidates =
            {
                stem + ".prefab",
                stem + ".png",
                stem + ".jpg",
                stem + ".asset",
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(candidates[i])))
                    return candidates[i];
            }
            return "";
        }

        private static string GetAddressableState(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "Missing runtime asset. Run conversion/import first.";
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return "Addressables settings missing.";
            var entry = settings.FindAssetEntry(guid);
            if (entry == null) return "Runtime asset is not Addressable. Run 神霄/资源/Addressable 自动分组.";
            return "OK Addressable: " + entry.address + " (" + entry.parentGroup.Name + ")";
        }
    }
}
