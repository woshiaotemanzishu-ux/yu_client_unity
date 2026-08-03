using System;
using System.Collections.Generic;
using System.IO;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.Editor.MapTools
{
    public sealed class MapFrameAnimationImportResult
    {
        public int MapResId;
        public int EffectCount;
        public int AssetCount;
        public int CopiedFileCount;
        public int CreatedClipCount;
        public int UpdatedClipCount;
        public int SkippedCount;
        public int AddressableCount;
        public readonly List<string> Errors = new List<string>();
        internal readonly HashSet<string> OutputAssetPaths = new HashSet<string>(StringComparer.Ordinal);

        public bool Succeeded => Errors.Count == 0;

        public string FormatSummary()
        {
            string summary = "地图帧动画: " + EffectCount + " 个实例 / " + AssetCount + " 个共享图集"
                + "\n文件写入 " + CopiedFileCount
                + "，Clip 新建 " + CreatedClipCount
                + "，更新 " + UpdatedClipCount
                + "，跳过 " + SkippedCount
                + "\nAddressables 定向登记 " + AddressableCount;
            if (Errors.Count > 0) summary += "\n失败:\n- " + string.Join("\n- ", Errors);
            return summary;
        }
    }

    /// <summary>把 Electron 地图帧动画图集与实例数据同步为 Unity 可运行资源。</summary>
    public static class MapFrameAnimationImporter
    {
        [Serializable]
        private sealed class LibraryDocument
        {
            public int version = 0;
            public AnimationAssetData[] assets = Array.Empty<AnimationAssetData>();
        }

        [Serializable]
        private sealed class AnimationAssetData
        {
            public string id = "";
            public string name = "";
            public string texturePath = "";
            public int frameCount = 0;
            public int frameWidth = 0;
            public int frameHeight = 0;
            public int atlasWidth = 0;
            public int atlasHeight = 0;
            public float fps = 12f;
            public bool loop = true;
            public float pivotX = 0.5f;
            public float pivotY = 0.5f;
            public FrameData[] frames = Array.Empty<FrameData>();
        }

        [Serializable]
        private sealed class FrameData
        {
            public string name = "";
            public int x = 0;
            public int y = 0;
            public int width = 0;
            public int height = 0;
        }

        [Serializable]
        private sealed class EffectDocument
        {
            public int version = 0;
            public int mapResId = 0;
            public EffectData[] effects = Array.Empty<EffectData>();
        }

        [Serializable]
        private sealed class EffectData
        {
            public string kind = "";
            public string assetId = "";
        }

        private static string LibraryRelative => GameResPath.GetMapFrameAnimationLibrary();
        private static string ManifestRelative => GameResPath.GetSceneMapEffectsManifest();

        public static MapFrameAnimationImportResult ImportForMap(int mapResId, bool overwrite, string clientRoot)
        {
            var result = new MapFrameAnimationImportResult { MapResId = mapResId };
            if (string.IsNullOrWhiteSpace(clientRoot)
                || !Directory.Exists(Path.Combine(clientRoot, "cdn", "resource", "game")))
            {
                result.Errors.Add("yu_client 路径不对(找不到 cdn/resource/game): " + clientRoot);
                return result;
            }

            CopyResourceFile(clientRoot, ManifestRelative, overwrite, result);

            string mapRelative = "resource/game/scene/map/" + mapResId + "/map_effects.json";
            string mapSource = SourcePath(clientRoot, mapRelative);
            if (!File.Exists(mapSource))
            {
                result.SkippedCount++;
                return result;
            }

            EffectDocument mapDocument = ReadJson<EffectDocument>(mapSource, result);
            if (mapDocument == null) return result;
            if (mapDocument.version != 2 || mapDocument.mapResId != mapResId)
            {
                result.Errors.Add("map_effects.json 不是对应 mapResId 的 version 2 文档: " + mapSource);
                return result;
            }

            var usedAssetIds = new HashSet<string>(StringComparer.Ordinal);
            if (mapDocument.effects != null)
            {
                for (int i = 0; i < mapDocument.effects.Length; i++)
                {
                    EffectData effect = mapDocument.effects[i];
                    if (effect == null || effect.kind != "frame_animation") continue;
                    result.EffectCount++;
                    if (IsSafeAssetId(effect.assetId)) usedAssetIds.Add(effect.assetId);
                    else result.Errors.Add("非法 assetId: " + effect.assetId);
                }
            }
            result.AssetCount = usedAssetIds.Count;

            CopyResourceFile(clientRoot, mapRelative, overwrite, result);
            CopyResourceFile(clientRoot, LibraryRelative, overwrite, result);

            LibraryDocument library = ReadJson<LibraryDocument>(SourcePath(clientRoot, LibraryRelative), result);
            if (library == null) return result;

            var assetsById = new Dictionary<string, AnimationAssetData>(StringComparer.Ordinal);
            if (library.assets != null)
            {
                for (int i = 0; i < library.assets.Length; i++)
                {
                    AnimationAssetData asset = library.assets[i];
                    if (asset != null && IsSafeAssetId(asset.id)) assetsById[asset.id] = asset;
                }
            }

            foreach (string assetId in usedAssetIds)
            {
                if (!assetsById.TryGetValue(assetId, out AnimationAssetData asset))
                {
                    result.Errors.Add("中央资源库缺少 assetId=" + assetId);
                    continue;
                }
                ImportAsset(clientRoot, asset, overwrite, result);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        /// <summary>只登记本轮帧动画产物，不扫描或修改其它 GameRes 条目。</summary>
        public static void RegisterAddressables(MapFrameAnimationImportResult result)
        {
            if (result == null || !result.Succeeded || result.OutputAssetPaths.Count == 0) return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                result.Errors.Add("Addressable settings 不存在");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup("Remote_resource");
            if (group == null)
            {
                result.Errors.Add("缺少既有 Addressables Group: Remote_resource");
                return;
            }

            foreach (string assetPath in result.OutputAssetPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    result.Errors.Add("无法登记 Addressable（缺少 GUID）: " + assetPath);
                    continue;
                }
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry == null)
                {
                    result.Errors.Add("无法登记 Addressable: " + assetPath);
                    continue;
                }
                string address = ResourcePath.Normalize(assetPath);
                if (!string.Equals(entry.address, address, StringComparison.Ordinal)) entry.address = address;
                result.AddressableCount++;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void ImportAsset(string clientRoot, AnimationAssetData source, bool overwrite,
            MapFrameAnimationImportResult result)
        {
            string expectedTexture = GameResPath.GetMapFrameAnimationTexture(source.id).Replace('\\', '/');
            if (!string.Equals(source.texturePath.Replace('\\', '/'), expectedTexture, StringComparison.Ordinal))
            {
                result.Errors.Add("texturePath 与固定资源路径不一致 assetId=" + source.id + ": " + source.texturePath);
                return;
            }
            if (!ValidateFrames(source, result)) return;

            CopyResourceFile(clientRoot, expectedTexture, overwrite, result);
            string textureAssetPath = TargetAssetPath(expectedTexture);
            if (!File.Exists(TargetAbsolutePath(expectedTexture)))
            {
                result.Errors.Add("图集纹理未写入: " + textureAssetPath);
                return;
            }

            AssetDatabase.ImportAsset(textureAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureTexture(textureAssetPath, source.atlasWidth, source.atlasHeight);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            if (texture == null)
            {
                result.Errors.Add("Unity 无法加载图集纹理: " + textureAssetPath);
                return;
            }
            if (texture.width != source.atlasWidth || texture.height != source.atlasHeight)
            {
                result.Errors.Add("图集尺寸不一致 assetId=" + source.id + ": JSON="
                    + source.atlasWidth + "x" + source.atlasHeight + " Unity=" + texture.width + "x" + texture.height);
                return;
            }

            string clipAssetPath = TargetAssetPath(
                "resource/effect/map_frame_animations/" + source.id + "/" + source.id + "_clip.asset");
            MapFrameAnimationClipAsset clip = AssetDatabase.LoadAssetAtPath<MapFrameAnimationClipAsset>(clipAssetPath);
            result.OutputAssetPaths.Add(clipAssetPath);
            if (clip != null && !overwrite)
            {
                result.SkippedCount++;
                return;
            }

            bool created = clip == null;
            if (created)
            {
                clip = ScriptableObject.CreateInstance<MapFrameAnimationClipAsset>();
                Directory.CreateDirectory(Path.GetDirectoryName(TargetAbsoluteAssetPath(clipAssetPath)));
                AssetDatabase.CreateAsset(clip, clipAssetPath);
            }

            var frames = new MapFrameAnimationFrame[source.frames.Length];
            for (int i = 0; i < source.frames.Length; i++)
            {
                FrameData frame = source.frames[i];
                frames[i] = new MapFrameAnimationFrame(frame.name, frame.x, frame.y, frame.width, frame.height);
            }
            clip.Configure(source.id, source.name, texture, source.frameWidth, source.frameHeight,
                source.fps, source.loop, source.pivotX, source.pivotY, frames);
            EditorUtility.SetDirty(clip);
            if (created) result.CreatedClipCount++;
            else result.UpdatedClipCount++;
        }

        private static bool ValidateFrames(AnimationAssetData asset, MapFrameAnimationImportResult result)
        {
            if (asset.atlasWidth <= 0 || asset.atlasHeight <= 0 || asset.atlasWidth > 8192 || asset.atlasHeight > 8192
                || asset.frameWidth <= 0 || asset.frameHeight <= 0
                || asset.frames == null || asset.frames.Length == 0 || asset.frameCount != asset.frames.Length)
            {
                result.Errors.Add("动画资源字段不完整 assetId=" + asset.id);
                return false;
            }
            for (int i = 0; i < asset.frames.Length; i++)
            {
                FrameData frame = asset.frames[i];
                if (frame == null || frame.width <= 0 || frame.height <= 0 || frame.x < 0 || frame.y < 0
                    || frame.x + frame.width > asset.atlasWidth || frame.y + frame.height > asset.atlasHeight)
                {
                    result.Errors.Add("帧矩形越界 assetId=" + asset.id + " frame=" + i);
                    return false;
                }
            }
            return true;
        }

        private static void ConfigureTexture(string assetPath, int atlasWidth, int atlasHeight)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            int maxTextureSize = Mathf.Clamp(
                Mathf.NextPowerOfTwo(Mathf.Max(atlasWidth, atlasHeight)), 32, 8192);
            bool dirty = importer.mipmapEnabled || !importer.alphaIsTransparency
                || importer.wrapMode != TextureWrapMode.Clamp || importer.filterMode != FilterMode.Bilinear
                || importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize != maxTextureSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxTextureSize;
            if (dirty) importer.SaveAndReimport();
        }

        private static T ReadJson<T>(string path, MapFrameAnimationImportResult result) where T : class
        {
            if (!File.Exists(path))
            {
                result.Errors.Add("缺少源文件: " + path);
                return null;
            }
            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                result.Errors.Add("解析失败 " + path + ": " + e.Message);
                return null;
            }
        }

        private static void CopyResourceFile(string clientRoot, string relativePath, bool overwrite,
            MapFrameAnimationImportResult result)
        {
            string source = SourcePath(clientRoot, relativePath);
            if (!File.Exists(source))
            {
                result.Errors.Add("缺少源文件: " + source);
                return;
            }

            string target = TargetAbsolutePath(relativePath);
            if (!overwrite && File.Exists(target))
            {
                result.OutputAssetPaths.Add(TargetAssetPath(relativePath));
                result.SkippedCount++;
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target, true);
                string assetPath = TargetAssetPath(relativePath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                result.OutputAssetPaths.Add(assetPath);
                result.CopiedFileCount++;
            }
            catch (Exception e)
            {
                result.Errors.Add("写入失败 " + TargetAssetPath(relativePath) + ": " + e.Message);
            }
        }

        private static bool IsSafeAssetId(string assetId)
        {
            if (string.IsNullOrEmpty(assetId) || assetId.Length > 64
                || assetId[0] < 'a' || assetId[0] > 'z') return false;
            for (int i = 1; i < assetId.Length; i++)
            {
                char c = assetId[i];
                if ((c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '_') return false;
            }
            return true;
        }

        private static string SourcePath(string clientRoot, string relativePath)
        {
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(clientRoot, "cdn", normalized);
        }

        private static string TargetAssetPath(string relativePath)
        {
            return "Assets/GameRes/" + relativePath.Replace('\\', '/');
        }

        private static string TargetAbsolutePath(string relativePath)
        {
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, "GameRes", normalized);
        }

        private static string TargetAbsoluteAssetPath(string assetPath)
        {
            string relative = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Application.dataPath, relative);
        }
    }
}
