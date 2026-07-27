using System.IO;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>保证差异表真实存在并可直接在 Inspector 中编辑。</summary>
    [InitializeOnLoad]
    public static class UIEffectProfileCatalogBootstrap
    {
        private const string AssetPath = "Assets/Resources/UIEffectProfileCatalog.asset";

        static UIEffectProfileCatalogBootstrap()
        {
            EditorApplication.delayCall += () => EnsureAsset();
        }

        [MenuItem("神霄/配置/UI 特效差异表", priority = 42)]
        public static void OpenAsset()
        {
            UIEffectProfileCatalog catalog = EnsureAsset();
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        private static UIEffectProfileCatalog EnsureAsset()
        {
            UIEffectProfileCatalog catalog = AssetDatabase.LoadAssetAtPath<UIEffectProfileCatalog>(AssetPath);
            if (catalog != null) return catalog;

            string directory = Path.GetDirectoryName(AssetPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            catalog = ScriptableObject.CreateInstance<UIEffectProfileCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIEffect] Created editable profile catalog: " + AssetPath);
            return catalog;
        }
    }
}
