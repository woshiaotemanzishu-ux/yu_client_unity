using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Shenxiao.EditorTools.AddrSetup
{
    /// <summary>
    /// One-click Addressable group setup.
    /// Convention:
    ///   Assets/_App/...     -> Local group  (built into player)
    ///   Assets/GameRes/...  -> Remote groups (per top-level folder under GameRes)
    /// Address = path relative to Assets/{_App|GameRes}, lowercased, slashes, no extension.
    /// </summary>
    public static class AddressableSetup
    {
        private const string LocalGroupName = "App_Local";
        private const string RemoteGroupPrefix = "Remote_";

        [MenuItem("神霄/资源/Addressable 自动分组", priority = 20)]
        public static void AutoGroupAll()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.Log("[AddressableSetup] Addressable settings not found, creating default settings...");
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                if (settings == null)
                {
                    Debug.LogError("[AddressableSetup] Failed to create Addressable settings.");
                    return;
                }
            }

            // Build sprite atlases first so they get registered as Addressable entries too.
            AutoSpriteAtlas.Build();

            int countLocal = AssignFolderToGroup(settings, "Assets/_App", EnsureLocalGroup(settings));
            int countRemote = 0;
            var remoteRoot = "Assets/GameRes";
            if (AssetDatabase.IsValidFolder(remoteRoot))
            {
                foreach (var sub in AssetDatabase.GetSubFolders(remoteRoot))
                {
                    var subName = Path.GetFileName(sub);
                    var groupName = RemoteGroupPrefix + subName;
                    var group = EnsureRemoteGroup(settings, groupName);
                    countRemote += AssignFolderToGroup(settings, sub, group);
                }
            }

            // UI prefabs live under Assets/Prefabs/ (not GameRes/). Treat them as Remote too.
            if (AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                var prefabGroup = EnsureRemoteGroup(settings, RemoteGroupPrefix + "Prefabs");
                countRemote += AssignFolderToGroup(settings, "Assets/Prefabs", prefabGroup);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(settings);
            Debug.Log($"[AddressableSetup] local entries: {countLocal}, remote entries: {countRemote}");
        }

        private static AddressableAssetGroup EnsureLocalGroup(AddressableAssetSettings settings)
        {
            var g = settings.FindGroup(LocalGroupName);
            if (g != null) return g;
            g = settings.CreateGroup(LocalGroupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = g.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.IncludeInBuild = true;
            return g;
        }

        private static AddressableAssetGroup EnsureRemoteGroup(AddressableAssetSettings settings, string name)
        {
            var g = settings.FindGroup(name);
            if (g != null) return g;
            g = settings.CreateGroup(name, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = g.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.IncludeInBuild = true;
            return g;
        }

        private static int AssignFolderToGroup(AddressableAssetSettings settings, string folder, AddressableAssetGroup group)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return 0;

            int count = 0;
            var guids = AssetDatabase.FindAssets("", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (path.EndsWith(".meta") || path.EndsWith(".cs") || path.EndsWith(".asmdef")) continue;

                var address = MakeAddress(path);
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                if (entry != null && entry.address != address)
                {
                    entry.address = address;
                }
                count++;
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            return count;
        }

        /// <summary>
        /// Assets/GameRes/resource/ui/login/login_bg.png  ->  resource/ui/login/login_bg
        /// Assets/_App/UI/Loading.prefab                  ->  ui/loading
        /// </summary>
        private static string MakeAddress(string assetPath)
        {
            string rel;
            if (assetPath.StartsWith("Assets/GameRes/")) rel = assetPath.Substring("Assets/GameRes/".Length);
            else if (assetPath.StartsWith("Assets/_App/")) rel = assetPath.Substring("Assets/_App/".Length);
            else rel = assetPath.Substring("Assets/".Length);

            var ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext)) rel = rel.Substring(0, rel.Length - ext.Length);
            return rel.Replace('\\', '/').ToLowerInvariant();
        }
    }
}
