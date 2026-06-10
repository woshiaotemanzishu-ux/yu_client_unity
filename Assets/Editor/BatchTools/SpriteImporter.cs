using System.IO;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools.BatchTools
{
    /// <summary>
    /// Auto-applies sprite import settings to textures placed under Assets/GameRes/resource/...
    /// Phase 0: lightweight defaults. Atlas grouping and 9-slice metadata come in Phase 1.
    /// </summary>
    public class SpriteImporter : AssetPostprocessor
    {
        private const string SpriteRoot = "Assets/GameRes/resource";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteRoot)) return;

            var importer = (TextureImporter)assetImporter;
            // Skip non-image textures (e.g. fonts atlas already configured).
            if (importer.textureType == TextureImporterType.Sprite) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = 100;
            importer.SetTextureSettings(settings);
        }
    }
}
