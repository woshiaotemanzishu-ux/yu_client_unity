using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.EditorTools.AudioMigration
{
    /// <summary>
    /// 锁定 Tools/Audio/sync_legacy_audio.py 生成的 310 个声音的导入策略，并提供 Unity 侧复核。
    /// 资源选择、复制、GUID 和 Addressables 行由 Python 工具维护；此处只防 Inspector/重导入漂移。
    /// </summary>
    public sealed class LegacyAudioImportPostprocessor : AssetPostprocessor
    {
        private const string Root = "Assets/GameRes/resource/sound/";
        private static readonly string[] Categories = { "novice_voice", "npc", "role", "scene", "skill", "ui" };

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) return;
            if (!(assetImporter is AudioImporter importer)) return;
            ApplyExpected(importer, CategoryOf(assetPath));
        }

        [MenuItem("神霄/资源/声音迁移/校验完整性", priority = 241)]
        public static void ValidateMenu()
        {
            bool ok = Validate(out string report);
            if (ok) Debug.Log(report);
            else Debug.LogError(report);
        }

        /// <summary>供 batchmode -executeMethod 调用；失败时抛异常使进程非零退出。</summary>
        public static void ValidateCli()
        {
            if (!Validate(out string report)) throw new InvalidOperationException(report);
            Debug.Log(report);
        }

        public static bool Validate(out string report)
        {
            var errors = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { Root.TrimEnd('/') });
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) errors.Add("Addressable settings 不存在");
            if (guids.Length != 310) errors.Add($"AudioClip 数量={guids.Length}，期望 310");

            var categoryCounts = Categories.ToDictionary(category => category, _ => 0);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string category = CategoryOf(path);
                if (!categoryCounts.ContainsKey(category))
                {
                    errors.Add("未知声音分类: " + path);
                    continue;
                }
                categoryCounts[category]++;

                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                {
                    errors.Add("不是 AudioImporter: " + path);
                    continue;
                }
                if (!MatchesExpected(importer, category)) errors.Add("导入配置漂移: " + path);

                string address = Path.ChangeExtension(path.Substring("Assets/GameRes/".Length), null)
                    .Replace('\\', '/').ToLowerInvariant();
                AddressableAssetEntry entry = settings?.FindAssetEntry(guid);
                string label = "pack_resource_sound_" + category;
                if (entry == null || entry.address != address || !entry.labels.Contains(label))
                    errors.Add($"Addressable 漂移: {path} => {entry?.address ?? "<missing>"} / {label}");
            }

            var expectedCounts = new Dictionary<string, int>
            {
                ["novice_voice"] = 20, ["npc"] = 110, ["role"] = 21,
                ["scene"] = 13, ["skill"] = 94, ["ui"] = 52,
            };
            foreach (var expected in expectedCounts)
                if (categoryCounts[expected.Key] != expected.Value)
                    errors.Add($"{expected.Key} 数量={categoryCounts[expected.Key]}，期望 {expected.Value}");

            const string configPath = "Assets/GameRes/resource/config/client/ConfigSound.json";
            string configGuid = AssetDatabase.AssetPathToGUID(configPath);
            AddressableAssetEntry configEntry = settings?.FindAssetEntry(configGuid);
            if (string.IsNullOrEmpty(configGuid) || configEntry == null || configEntry.address != "resource/config/client/configsound")
                errors.Add("ConfigSound.json 缺失或 Addressable 地址错误");

            string summary = "[LegacyAudio] 资源 310/310，分类 "
                + string.Join("，", Categories.Select(category => category + "=" + categoryCounts[category]));
            report = errors.Count == 0
                ? summary + "；导入配置与 Addressables 校验通过。"
                : summary + $"；异常 {errors.Count} 项：\n" + string.Join("\n", errors.Take(50));
            return errors.Count == 0;
        }

        private static string CategoryOf(string path)
        {
            string relative = path.Substring(Math.Min(path.Length, Root.Length));
            int slash = relative.IndexOf('/');
            return slash >= 0 ? relative.Substring(0, slash).ToLowerInvariant() : relative.ToLowerInvariant();
        }

        private static void ApplyExpected(AudioImporter importer, string category)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.sampleRateOverride = 44100;
            if (category == "scene")
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                settings.preloadAudioData = false;
                importer.loadInBackground = true;
            }
            else if (category == "npc" || category == "novice_voice")
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.75f;
                settings.preloadAudioData = true;
                importer.loadInBackground = true;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.quality = 1f;
                settings.preloadAudioData = true;
                importer.loadInBackground = true;
            }
            importer.forceToMono = false;
            importer.ambisonic = false;
            importer.defaultSampleSettings = settings;
        }

        private static bool MatchesExpected(AudioImporter importer, string category)
        {
            AudioImporterSampleSettings actual = importer.defaultSampleSettings;
            AudioClipLoadType loadType = category == "scene"
                ? AudioClipLoadType.Streaming
                : category == "npc" || category == "novice_voice"
                    ? AudioClipLoadType.CompressedInMemory
                    : AudioClipLoadType.DecompressOnLoad;
            AudioCompressionFormat compression = category == "scene" || category == "npc" || category == "novice_voice"
                ? AudioCompressionFormat.Vorbis
                : AudioCompressionFormat.ADPCM;
            float quality = category == "scene" ? 0.7f : category == "npc" || category == "novice_voice" ? 0.75f : 1f;
            bool preload = category != "scene";
            return actual.loadType == loadType
                && actual.compressionFormat == compression
                && Mathf.Abs(actual.quality - quality) < 0.001f
                && actual.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate
                && actual.preloadAudioData == preload
                && importer.loadInBackground
                && !importer.forceToMono
                && !importer.ambisonic;
        }
    }
}
