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
        private const string FashionMaterialRoot = "Assets/GameRes/resource/object/fashion/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteRoot)) return;

            var importer = (TextureImporter)assetImporter;
            // 时装染色图是模型材质贴图，不是 UGUI Sprite。这里必须先于通用 resource
            // 规则返回，否则预检改成 Default 后会被本后处理器立刻改回 Sprite，导致
            // 每轮 528 张贴图 SaveAndReimport，既慢又让首次点击验收失去意义。
            if (assetPath.StartsWith(FashionMaterialRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                return;
            }

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
