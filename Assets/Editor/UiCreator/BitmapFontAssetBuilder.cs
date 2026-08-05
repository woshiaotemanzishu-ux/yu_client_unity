using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>把老端 BMFont XML + 彩色 PNG 转成可直接挂到 TMP 的静态位图字体资产。</summary>
    public static class BitmapFontAssetBuilder
    {
        private const string LegacyFontFolder = "Assets/GameRes/Fonts/Bitmap";

        /// <summary>重建仓库内全部老端 BMFont。可从菜单调用，也可作为 batchmode executeMethod。</summary>
        [MenuItem("神霄/资源/重建老端位图字体")]
        public static void BuildAllLegacyFonts()
        {
            string[] fntPaths = Directory.GetFiles(LegacyFontFolder, "*.fnt", SearchOption.TopDirectoryOnly);
            Array.Sort(fntPaths, StringComparer.OrdinalIgnoreCase);
            int built = 0;
            foreach (string rawPath in fntPaths)
            {
                string path = rawPath.Replace('\\', '/');
                if (BuildOrUpdate(path) != null) built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BitmapFont] 重建完成: {built}/{fntPaths.Length}, folder={LegacyFontFolder}");
            if (built != fntPaths.Length)
                throw new InvalidOperationException($"位图字体重建不完整: {built}/{fntPaths.Length}");
        }

        public static TMP_FontAsset BuildOrUpdate(string fntAssetPath)
        {
            if (string.IsNullOrEmpty(fntAssetPath) || !File.Exists(fntAssetPath))
            {
                Debug.LogError("[UiCreator] 位图字体描述不存在: " + fntAssetPath);
                return null;
            }

            XDocument document;
            try
            {
                document = XDocument.Load(fntAssetPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[UiCreator] 位图字体描述解析失败: " + fntAssetPath + "\n" + e);
                return null;
            }

            XElement font = document.Root;
            XElement common = font?.Element("common");
            XElement page = font?.Element("pages")?.Element("page");
            XElement chars = font?.Element("chars");
            if (common == null || page == null || chars == null)
            {
                Debug.LogError("[UiCreator] 位图字体描述缺少 common/pages/chars: " + fntAssetPath);
                return null;
            }

            string folder = Path.GetDirectoryName(fntAssetPath)?.Replace('\\', '/');
            string fontName = Path.GetFileNameWithoutExtension(fntAssetPath);
            string texturePath = folder + "/" + ReadString(page, "file");
            // 老端运行目录里有一批 FNT 保留了导出工具时期的 page 文件名(test.png/my_0.png/*_0.png)，
            // 真实 CDN 成对文件却统一为 <fontName>.png。Electron「位图字体」工具也按同样规则回退。
            if (!File.Exists(texturePath))
                texturePath = folder + "/" + fontName + ".png";
            string outputPath = folder + "/" + fontName + ".asset";
            if (!File.Exists(texturePath))
            {
                Debug.LogError("[UiCreator] 位图字体贴图不存在: " + texturePath);
                return null;
            }

            ConfigureAtlasImporter(texturePath);
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (atlas == null)
            {
                Debug.LogError("[UiCreator] 位图字体贴图导入失败: " + texturePath);
                return null;
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (fontAsset == null)
            {
                fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
                fontAsset.name = fontName;
                AssetDatabase.CreateAsset(fontAsset, outputPath);
            }

            var definitions = new List<GlyphDefinition>();
            int maxGlyphHeight = 1;
            foreach (XElement node in chars.Elements("char"))
            {
                int unicode = ReadInt(node, "id");
                // BMFont 的 -1 是“缺字占位”，不是可输入 Unicode；TextCore 不接受它。
                if (unicode < 0 || unicode > 0x10FFFF) continue;

                var definition = new GlyphDefinition
                {
                    Unicode = unicode,
                    X = ReadInt(node, "x"),
                    Y = ReadInt(node, "y"),
                    Width = ReadInt(node, "width"),
                    Height = ReadInt(node, "height"),
                    OffsetX = ReadInt(node, "xoffset"),
                    OffsetY = ReadInt(node, "yoffset"),
                    Advance = ReadInt(node, "xadvance"),
                };
                maxGlyphHeight = Math.Max(maxGlyphHeight, definition.Height + Math.Max(0, definition.OffsetY));
                definitions.Add(definition);
            }

            int atlasWidth = ReadInt(common, "scaleW");
            int atlasHeight = ReadInt(common, "scaleH");
            float em = maxGlyphHeight;
            fontAsset.faceInfo = new FaceInfo
            {
                familyName = fontName,
                styleName = "Regular",
                pointSize = em,
                scale = 1f,
                lineHeight = em,
                ascentLine = em,
                capLine = em,
                meanLine = em * 0.75f,
                baseline = 0f,
                descentLine = 0f,
                underlineOffset = -em * 0.1f,
                underlineThickness = 1f,
                strikethroughOffset = em * 0.3f,
                strikethroughThickness = 1f,
                tabWidth = em * 4f,
            };
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.atlasTextures = new[] { atlas };
            fontAsset.glyphTable.Clear();
            fontAsset.characterTable.Clear();

            definitions.Sort((a, b) => a.Unicode.CompareTo(b.Unicode));
            foreach (GlyphDefinition definition in definitions)
            {
                // BMFont 的 y 从贴图左上角向下，TextCore GlyphRect 的 y 从左下角向上。
                int glyphY = atlasHeight - definition.Y - definition.Height;
                var metrics = new GlyphMetrics(
                    definition.Width,
                    definition.Height,
                    definition.OffsetX,
                    definition.Height - definition.OffsetY,
                    definition.Advance);
                var glyph = new Glyph(
                    (uint)definition.Unicode,
                    metrics,
                    new GlyphRect(definition.X, glyphY, definition.Width, definition.Height),
                    1f,
                    0);
                fontAsset.glyphTable.Add(glyph);
                fontAsset.characterTable.Add(new TMP_Character((uint)definition.Unicode, fontAsset, glyph));
            }

            Shader shader = Shader.Find("TextMeshPro/Bitmap Custom Atlas");
            if (shader == null)
            {
                Debug.LogError("[UiCreator] 找不到 TextMeshPro/Bitmap Custom Atlas shader");
                return null;
            }

            Material material = fontAsset.material;
            if (material == null || material.shader != shader)
            {
                if (material != null && AssetDatabase.GetAssetPath(material) == outputPath)
                    UnityEngine.Object.DestroyImmediate(material, true);
                material = new Material(shader) { name = fontName + " Material" };
                AssetDatabase.AddObjectToAsset(material, fontAsset);
                fontAsset.material = material;
            }
            material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);

            // TMP 的部分 atlas 元数据只有 internal setter；通过序列化字段写入，保持静态资产完整。
            var serialized = new SerializedObject(fontAsset);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.FindProperty("m_AtlasWidth").intValue = atlasWidth;
            serialized.FindProperty("m_AtlasHeight").intValue = atlasHeight;
            serialized.FindProperty("m_AtlasPadding").intValue = 0;
            serialized.FindProperty("m_AtlasRenderMode").intValue = (int)GlyphRenderMode.COLOR;
            serialized.FindProperty("m_AtlasTextureIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            fontAsset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(fontAsset);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void ConfigureAtlasImporter(string texturePath)
        {
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(texturePath) is TextureImporter importer)) return;

            bool changed = importer.textureType != TextureImporterType.Default
                           || importer.mipmapEnabled
                           || importer.textureCompression != TextureImporterCompression.Uncompressed
                           || importer.filterMode != FilterMode.Bilinear
                           || importer.wrapMode != TextureWrapMode.Clamp
                           || !importer.alphaIsTransparency
                           || importer.npotScale != TextureImporterNPOTScale.None;
            if (!changed) return;

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static int ReadInt(XElement element, string name)
            => int.Parse(ReadString(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture);

        private static string ReadString(XElement element, string name)
            => element.Attribute(name)?.Value ?? throw new InvalidDataException("缺少属性 " + name);

        private sealed class GlyphDefinition
        {
            public int Unicode;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int OffsetX;
            public int OffsetY;
            public int Advance;
        }
    }
}
