using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    public static class AssetAssemblyProfileStore
    {
        public const string ASSET_PATH = "Assets/GameRes/resource/config/client/asset_assembly_profiles.json";

        public static Dictionary<string, AssetAssemblyEntry> LoadAll()
        {
            EnsureFile();
            string text = File.ReadAllText(ASSET_PATH);
            if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, AssetAssemblyEntry>();
            try
            {
                return AssetAssemblyProfiles.ParseProfiles(text);
            }
            catch (JsonException e)
            {
                Debug.LogError("[AssetAssembly] profile 解析失败: " + e.Message);
                return new Dictionary<string, AssetAssemblyEntry>();
            }
        }

        public static AssetAssemblyEntry Load(string profileId)
        {
            Dictionary<string, AssetAssemblyEntry> all = LoadAll();
            return all.TryGetValue(profileId, out AssetAssemblyEntry entry) ? entry : null;
        }

        public static void Save(string profileId, AssetAssemblyEntry entry)
        {
            Dictionary<string, AssetAssemblyEntry> all = LoadAll();
            if (entry != null && string.IsNullOrEmpty(entry.Id)) entry.Id = profileId;
            all[profileId] = entry;
            string json = Serialize(all);
            File.WriteAllText(ASSET_PATH, json);
            AssetDatabase.ImportAsset(ASSET_PATH);
            AssetDatabase.SaveAssets();
        }

        public static void Delete(string profileId)
        {
            Dictionary<string, AssetAssemblyEntry> all = LoadAll();
            if (!all.Remove(profileId)) return;
            string json = Serialize(all);
            File.WriteAllText(ASSET_PATH, json);
            AssetDatabase.ImportAsset(ASSET_PATH);
            AssetDatabase.SaveAssets();
        }

        private static string Serialize(Dictionary<string, AssetAssemblyEntry> all)
        {
            var root = new AssetAssemblyProfileRoot
            {
                Profiles = all
                    .Where(kv => kv.Value != null)
                    .OrderBy(kv => kv.Key)
                    .Select(kv =>
                    {
                        if (string.IsNullOrEmpty(kv.Value.Id)) kv.Value.Id = kv.Key;
                        return kv.Value;
                    })
                    .ToList(),
            };
            return JsonConvert.SerializeObject(root, Formatting.Indented);
        }

        public static string ProfileIdOf(AssetEntry entry)
        {
            string[] parts = (entry.OutDir ?? string.Empty).Replace('\\', '/').Split('/');
            return parts.Length >= 5 && parts[0] == "Assets" && parts[1] == "GameRes" && parts[2] == "object"
                ? parts[3] + "/" + parts[4]
                : entry.Id;
        }

        public static string AddressOfAsset(string assetPath)
        {
            string rel;
            assetPath = assetPath.Replace('\\', '/');
            if (assetPath.StartsWith("Assets/GameRes/")) rel = assetPath.Substring("Assets/GameRes/".Length);
            else if (assetPath.StartsWith("Assets/_App/")) rel = assetPath.Substring("Assets/_App/".Length);
            else rel = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;

            string ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext)) rel = rel.Substring(0, rel.Length - ext.Length);
            return ResourcePath.Normalize(rel);
        }

        public static string AssetPathOfKey(string key, string ext)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string path = "Assets/GameRes/" + ResourcePath.Normalize(key) + ext;
            return path.Replace('\\', '/');
        }

        public static AssetAssemblyEntry Clone(AssetAssemblyEntry src)
        {
            if (src == null) return null;
            string json = JsonConvert.SerializeObject(src);
            return JsonConvert.DeserializeObject<AssetAssemblyEntry>(json);
        }

        public static void EnsureFile()
        {
            string dir = Path.GetDirectoryName(ASSET_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(ASSET_PATH))
            {
                File.WriteAllText(ASSET_PATH, "{\n  \"profiles\": []\n}\n");
                AssetDatabase.ImportAsset(ASSET_PATH);
            }
        }
    }
}
