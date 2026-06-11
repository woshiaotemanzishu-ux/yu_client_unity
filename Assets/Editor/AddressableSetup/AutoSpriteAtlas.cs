using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Shenxiao.EditorTools.AddrSetup
{
    /// <summary>
    /// Auto-generates one SpriteAtlas per "texture" folder under Assets/GameRes/resource/.
    /// Convention:
    ///   Assets/GameRes/resource/{module}/.../texture/   -> {parent}_texture.spriteatlas (sibling)
    /// Idempotent: if the .spriteatlas already exists, refreshes its packables.
    /// IncludeInBuild=false -> bundled into the same Addressable group as the textures.
    /// </summary>
    public static class AutoSpriteAtlas
    {
        private const string ResourceRoot = "Assets/GameRes/resource";
        private const string TextureFolderName = "texture";

        [MenuItem("神霄/资源/自动建 SpriteAtlas", priority = 21)]
        public static void Build()
        {
            int created = 0, refreshed = 0;
            if (!AssetDatabase.IsValidFolder(ResourceRoot))
            {
                Debug.LogWarning($"[AutoSpriteAtlas] {ResourceRoot} not found.");
                return;
            }
            foreach (var folder in CollectTextureFolders(ResourceRoot))
            {
                bool isNew;
                EnsureAtlas(folder, out isNew);
                if (isNew) created++; else refreshed++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AutoSpriteAtlas] created={created}, refreshed={refreshed}");
        }

        private static IEnumerable<string> CollectTextureFolders(string root)
        {
            // find every folder named "texture" under root recursively
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                foreach (var sub in AssetDatabase.GetSubFolders(cur))
                {
                    if (Path.GetFileName(sub).Equals(TextureFolderName, System.StringComparison.OrdinalIgnoreCase))
                        yield return sub;
                    stack.Push(sub);
                }
            }
        }

        private static void EnsureAtlas(string textureFolder, out bool isNew)
        {
            // sibling atlas: .../{module}/texture -> .../{module}/{module}_texture.spriteatlas
            var parent = Path.GetDirectoryName(textureFolder).Replace('\\', '/');
            var moduleName = Path.GetFileName(parent);
            var atlasPath = $"{parent}/{moduleName}_texture.spriteatlas";

            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            isNew = atlas == null;
            if (isNew)
            {
                atlas = new SpriteAtlas();
                atlas.SetIncludeInBuild(false);
                var settings = atlas.GetPackingSettings();
                settings.enableRotation = false;
                settings.enableTightPacking = false;
                atlas.SetPackingSettings(settings);
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            // Reset packables to just this folder (idempotent).
            var folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(textureFolder);
            if (folderObj == null) return;

            var current = atlas.GetPackables();
            atlas.Remove(current);
            atlas.Add(new Object[] { folderObj });
            EditorUtility.SetDirty(atlas);
        }
    }
}
