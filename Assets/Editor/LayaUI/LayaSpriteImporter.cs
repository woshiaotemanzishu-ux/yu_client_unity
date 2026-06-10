using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 皮肤(skin 路径)-> Unity Sprite 资产。三级解析:
    ///   1. h5/laya/assets/ 散图直接拷贝;
    ///   2. 散图没有但在模块图集里 -> 按 UIConfig.json 的 frame 从 cdn texture.png 反裁;
    ///   3. 都没有 -> 记入报告,绝不静默。
    /// 拷贝后统一设 TextureImporter(Sprite/FullRect/无 mipmap),sizeGrid 写进 spriteBorder。
    /// </summary>
    public static class LayaSpriteImporter
    {
        private static JObject _uiConfig;
        private static readonly Dictionary<string, Texture2D> _atlasTexCache = new Dictionary<string, Texture2D>();

        public static void ResetCache()
        {
            _uiConfig = null;
            foreach (Texture2D t in _atlasTexCache.Values)
            {
                if (t != null) Object.DestroyImmediate(t);
            }
            _atlasTexCache.Clear();
        }

        /// <summary>
        /// 确保 skin 对应的 Sprite 资产存在并配置好,返回资产路径;失败返回 null 并把原因写进 report。
        /// border 为 Vector4.zero 表示不带九宫格。
        /// </summary>
        public static string EnsureSprite(string skin, Vector4 border, LayaUIReport report)
        {
            if (string.IsNullOrEmpty(skin)) return null;
            string assetPath = LayaUISettings.GAMERES_ROOT + "/" + skin;
            if (!File.Exists(assetPath))
            {
                if (!MaterializePng(skin, assetPath, report)) return null;
                AssetDatabase.ImportAsset(assetPath);
            }
            ConfigureImporter(assetPath, border, report, skin);
            return assetPath;
        }

        public static Sprite LoadSprite(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static bool MaterializePng(string skin, string assetPath, LayaUIReport report)
        {
            // 1) 散图
            string loose = Path.Combine(LayaUISettings.LayaAssetsRoot, skin.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(loose))
            {
                if (IsLfsPlaceholder(loose))
                {
                    report.MissingSkin(skin, "散图是 git-lfs 占位文件,请在 yu_client 里执行 git lfs pull");
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                File.Copy(loose, assetPath, true);
                return true;
            }
            // 2) 图集反裁
            if (ExtractFromAtlas(skin, assetPath, report)) return true;
            report.MissingSkin(skin, "散图与图集都找不到");
            return false;
        }

        private static bool ExtractFromAtlas(string skin, string assetPath, LayaUIReport report)
        {
            // skin: resource/game/{module}/texture/{name}.png
            string[] parts = skin.Split('/');
            if (parts.Length < 4 || parts[0] != "resource" || parts[1] != "game") return false;
            string module = parts[2];
            string frameName = parts[parts.Length - 1];

            JObject uiCfg = LoadUIConfig(report);
            if (uiCfg == null) return false;
            JObject atlas = uiCfg[module + "/texture.atlas"] as JObject;
            JObject frame = atlas? ["frames"]? [frameName] as JObject;
            if (frame == null) return false;

            Texture2D tex = LoadAtlasTexture(module, report);
            if (tex == null) return false;

            JObject f = (JObject)frame["frame"];
            int fx = (int)f["x"], fy = (int)f["y"], fw = (int)f["w"], fh = (int)f["h"];
            JObject src = frame["sourceSize"] as JObject;
            JObject off = frame["spriteSourceSize"] as JObject;
            int sw = src != null ? (int)src["w"] : fw;
            int sh = src != null ? (int)src["h"] : fh;
            int ox = off != null ? (int)off["x"] : 0;
            int oy = off != null ? (int)off["y"] : 0;

            if (fx + fw > tex.width || fy + fh > tex.height)
            {
                report.MissingSkin(skin, "图集 frame 越界(UIConfig 与 texture.png 不一致)");
                return false;
            }

            // 图集坐标自顶向下,GetPixels 自底向上
            Color[] pixels = tex.GetPixels(fx, tex.height - fy - fh, fw, fh);
            Texture2D outTex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            Color[] blank = new Color[sw * sh];
            outTex.SetPixels(blank);
            outTex.SetPixels(ox, sh - oy - fh, fw, fh, pixels);
            outTex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            report.Note(skin + " 由图集 " + module + "/texture.png 反裁得到");
            return true;
        }

        private static JObject LoadUIConfig(LayaUIReport report)
        {
            if (_uiConfig != null) return _uiConfig;
            string path = Path.Combine(LayaUISettings.CdnResourceRoot, "UIConfig.json");
            if (!File.Exists(path))
            {
                report.Note("UIConfig.json 不存在,图集反裁不可用: " + path);
                return null;
            }
            _uiConfig = JObject.Parse(File.ReadAllText(path));
            return _uiConfig;
        }

        private static Texture2D LoadAtlasTexture(string module, LayaUIReport report)
        {
            Texture2D tex;
            if (_atlasTexCache.TryGetValue(module, out tex)) return tex;
            string path = Path.Combine(LayaUISettings.CdnResourceRoot, "game", module, "texture.png");
            if (!File.Exists(path))
            {
                report.Note("模块图集不存在: " + path);
                _atlasTexCache[module] = null;
                return null;
            }
            if (IsLfsPlaceholder(path))
            {
                report.Note("模块图集是 git-lfs 占位文件,请先 git lfs pull: " + path);
                _atlasTexCache[module] = null;
                return null;
            }
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                Object.DestroyImmediate(tex);
                tex = null;
            }
            _atlasTexCache[module] = tex;
            return tex;
        }

        private static void ConfigureImporter(string assetPath, Vector4 border, LayaUIReport report, string skin)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                TextureImporterSettings s = new TextureImporterSettings();
                importer.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(s);
                dirty = true;
            }
            if (border != Vector4.zero && importer.spriteBorder != border)
            {
                if (importer.spriteBorder != Vector4.zero)
                {
                    report.Note(skin + " 的 sizeGrid 在不同界面不一致,保留先到的 " + importer.spriteBorder + ",忽略 " + border);
                }
                else
                {
                    importer.spriteBorder = border;
                    dirty = true;
                }
            }
            if (dirty) importer.SaveAndReimport();
        }

        private static bool IsLfsPlaceholder(string path)
        {
            FileInfo fi = new FileInfo(path);
            if (fi.Length > 512) return false;
            byte[] head = new byte[64];
            using (FileStream fs = File.OpenRead(path))
            {
                int n = fs.Read(head, 0, head.Length);
                return Encoding.ASCII.GetString(head, 0, n).StartsWith("version https://git-lfs");
            }
        }
    }
}
