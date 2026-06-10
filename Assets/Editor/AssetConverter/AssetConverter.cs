using System.IO;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools.AssetConverter
{
    /// <summary>
    /// Phase 0 placeholder. Recognizes LayaAir 3D asset extensions and routes to per-format parsers.
    /// Real binary parsers (.lh / .lm / .lani / .lmat) implemented incrementally in Phase 1+.
    /// .lani format spec lives in user memory (lani-format-analysis.md).
    /// </summary>
    public static class AssetConverter
    {
        [MenuItem("神霄/资源转换/转单个文件", priority = 50)]
        public static void ConvertSelectedFile()
        {
            string path = EditorUtility.OpenFilePanel("选 LayaAir 资源文件", "", "lh,lm,lani,lmat");
            if (string.IsNullOrEmpty(path)) return;
            ConvertOne(path);
        }

        [MenuItem("神霄/资源转换/批量转文件夹...", priority = 51)]
        public static void ConvertFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("选文件夹", "", "");
            if (string.IsNullOrEmpty(folder)) return;
            string[] exts = { "*.lh", "*.lm", "*.lani", "*.lmat" };
            int total = 0;
            foreach (var ext in exts)
            {
                foreach (var f in Directory.GetFiles(folder, ext, SearchOption.AllDirectories))
                {
                    ConvertOne(f);
                    total++;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[AssetConverter] processed {total} files");
        }

        public static void ConvertOne(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".lh":   LhConverter.Convert(path);   break;
                case ".lm":   LmConverter.Convert(path);   break;
                case ".lani": LaniConverter.Convert(path); break;
                case ".lmat": LmatConverter.Convert(path); break;
                default:
                    Debug.LogWarning($"[AssetConverter] unsupported ext: {ext} ({path})");
                    break;
            }
        }
    }

    /// <summary>.lh: Laya scene/prefab. Parser TODO. See yu_client/h5/laya/Conventional/*.lh for samples.</summary>
    public static class LhConverter
    {
        public static void Convert(string path)
        {
            Debug.LogWarning($"[LhConverter] TODO: parse .lh and emit Unity Prefab + skeleton hierarchy. Input={path}");
        }
    }

    /// <summary>.lm: Laya mesh. Parser TODO.</summary>
    public static class LmConverter
    {
        public static void Convert(string path)
        {
            Debug.LogWarning($"[LmConverter] TODO: parse .lm and emit Unity Mesh asset. Input={path}");
        }
    }

    /// <summary>
    /// .lani: Laya animation clip. Format spec known (LAYAANIMATION:03/04/COMPRESSION_04).
    /// TODO: emit Unity AnimationClip with curves keyed on bone paths.
    /// </summary>
    public static class LaniConverter
    {
        public static void Convert(string path)
        {
            Debug.LogWarning($"[LaniConverter] TODO: parse .lani and emit AnimationClip. Input={path}");
        }
    }

    /// <summary>.lmat: Laya material. Parser TODO. Map BlinnPhong/PBR -> URP/Lit or URP/Unlit.</summary>
    public static class LmatConverter
    {
        public static void Convert(string path)
        {
            Debug.LogWarning($"[LmatConverter] TODO: parse .lmat and emit Material asset. Input={path}");
        }
    }
}
