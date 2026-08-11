using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Rune
{
    /// <summary>Rune 固定资源闭包的定向导入/Addressables/Bind 升级；禁止 AssetDatabase.Refresh 和全库扫描。</summary>
    public static class RuneAssetPreflight
    {
        private const string RuneModule = "Assets/Prefabs/UI/Rune/RuneModule.prefab";
        private const string TreasurePrefab = "Assets/Prefabs/UI/RuneTreasure/RuneTreasureMainView.prefab";
        private const string RuneCardDir = "Assets/GameRes/resource/game/runeCard";
        private const string BigBgDir = "Assets/GameRes/resource/game/bigBg";
        private const string ConfigDir = "Assets/GameRes/resource/config/server";
        private const string GroupName = "Remote_resource";

        private static readonly string[] ConfigNames =
        {
            "config_rune_pos", "config_rune_all_show", "config_rune_attr_num",
            "config_rune_attr_coefficient", "config_rune_exchange", "config_rune_wake_up",
            "config_rune_wake_up_exp", "config_rune_wake_up_lv",
            "config_rune_awake_skill", "config_rune_skill",
        };

        private static readonly string[] BigBgNames =
        {
            "ui_rare_bg.jpg", "uilwmb_013a.jpg", "ui_rare_bg3.jpg", "ui_rare_bg4.jpg",
        };

        public static bool Run()
        {
            try
            {
                List<string> paths = BuildFixedClosure();
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) throw new FileNotFoundException("Rune闭包缺文件", path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    if (IsImage(path)) ConfigureTexture(path);
                }

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetGroup group = settings?.FindGroup(GroupName);
                if (settings == null || group == null) throw new InvalidOperationException("Remote_resource 不存在");
                Dictionary<string, EntrySnapshot> before = Snapshot(settings);
                int added = 0;
                foreach (string path in paths)
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("资源无GUID: " + path);
                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    if (entry == null)
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, false, false);
                        added++;
                    }
                    entry.SetAddress(AddressFor(path), false);
                    entry.SetLabel(LabelFor(path), true, true, false);
                }
                VerifyPreExistingUnchanged(settings, before);

                if (!LayaBindFiller.FillPrefab(RuneModule))
                    throw new InvalidOperationException("RuneModule Bind升级失败");
                if (!LayaBindFiller.FillPrefab(TreasurePrefab))
                    throw new InvalidOperationException("RuneTreasureMainView Bind升级失败");

                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Verify(paths, settings);
                Debug.Log("[RuneAssetPreflight] OK exactAssets=" + paths.Count + " added=" + added +
                          " runeCard=133 config=10 bigBg=4");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RuneAssetPreflight] FAILED " + ex);
                return false;
            }
        }

        private static List<string> BuildFixedClosure()
        {
            string[] cards = Directory.GetFiles(RuneCardDir, "*.png", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (cards.Length != 133) throw new InvalidOperationException("RuneCard固定闭包应为133，实际=" + cards.Length);
            var paths = new List<string>(147);
            paths.AddRange(cards);
            paths.AddRange(ConfigNames.Select(name => ConfigDir + "/" + name + ".json"));
            paths.AddRange(BigBgNames.Select(name => BigBgDir + "/" + name));
            if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 147)
                throw new InvalidOperationException("Rune闭包路径不唯一");
            return paths;
        }

        private static void ConfigureTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("不是TextureImporter: " + path);
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (changed) importer.SaveAndReimport();
        }

        private static Dictionary<string, EntrySnapshot> Snapshot(AddressableAssetSettings settings)
        {
            var result = new Dictionary<string, EntrySnapshot>(StringComparer.Ordinal);
            foreach (AddressableAssetGroup group in settings.groups)
            foreach (AddressableAssetEntry entry in group.entries)
                result[entry.guid] = new EntrySnapshot(entry.address, entry.labels);
            return result;
        }

        private static void VerifyPreExistingUnchanged(AddressableAssetSettings settings,
            Dictionary<string, EntrySnapshot> before)
        {
            foreach (KeyValuePair<string, EntrySnapshot> pair in before)
            {
                AddressableAssetEntry current = settings.FindAssetEntry(pair.Key);
                if (current == null || current.address != pair.Value.Address
                    || !current.labels.SetEquals(pair.Value.Labels))
                    throw new InvalidOperationException("覆盖了既有Addressable条目: " + pair.Key);
            }
        }

        private static void Verify(IEnumerable<string> paths, AddressableAssetSettings settings)
        {
            foreach (string path in paths)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                string label = LabelFor(path);
                if (entry == null || entry.address != AddressFor(path) || !entry.labels.Contains(label))
                    throw new InvalidOperationException("Addressable校验失败: " + path);
            }
            GameObject rune = AssetDatabase.LoadAssetAtPath<GameObject>(RuneModule);
            GameObject treasure = AssetDatabase.LoadAssetAtPath<GameObject>(TreasurePrefab);
            if (rune == null || rune.GetComponentInChildren<Shenxiao.Module.Core.Rune.RuneBagView>(true) == null)
                throw new InvalidOperationException("RuneBagView未升级到业务View");
            if (treasure == null || treasure.GetComponentInChildren<Shenxiao.Module.Core.RuneTreasure.RuneTreasureMainView>(true) == null)
                throw new InvalidOperationException("RuneTreasureMainView未升级到业务View");
        }

        private static string AddressFor(string path)
        {
            const string prefix = "Assets/GameRes/";
            string relative = path.StartsWith(prefix, StringComparison.Ordinal) ? path.Substring(prefix.Length) : path;
            return Path.ChangeExtension(relative, null).Replace('\\', '/').ToLowerInvariant();
        }

        private static string LabelFor(string path)
        {
            if (path.StartsWith(RuneCardDir + "/", StringComparison.OrdinalIgnoreCase))
                return "pack_resource_game_runecard";
            if (path.StartsWith(BigBgDir + "/", StringComparison.OrdinalIgnoreCase))
                return "pack_resource_game_bigbg";
            return "pack_resource_config";
        }

        private static bool IsImage(string path) =>
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);

        private sealed class EntrySnapshot
        {
            public string Address { get; }
            public HashSet<string> Labels { get; }
            public EntrySnapshot(string address, IEnumerable<string> labels)
            {
                Address = address;
                Labels = new HashSet<string>(labels ?? Array.Empty<string>(), StringComparer.Ordinal);
            }
        }

        public static void RunBatch()
        {
            bool ok = Run();
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
