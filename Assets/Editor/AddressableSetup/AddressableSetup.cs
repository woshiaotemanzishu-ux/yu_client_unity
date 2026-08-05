using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using Shenxiao.Framework.Res;

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
        private const string RemoteLoadPathDefault = AddressableAssetSettings.kRemoteBuildPathValue;
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
            EnsureRemoteProfileDefaults(settings);

            int countLocal = AssignFolderToGroup(settings, "Assets/_App", EnsureLocalGroup(settings));

            // 自定义特效/通用 shader 必须随构建可用:它们被 Remote 特效材质隐式引用,
            // 若不显式登记成本地 Addressable,bundle 模式下材质可能找不到 shader → 品红(紫块)。
            // 放本地组(随包,不走远端)。
            if (AssetDatabase.IsValidFolder("Assets/Shaders"))
                countLocal += AssignFolderToGroup(settings, "Assets/Shaders", EnsureLocalGroup(settings));

            int countRemote = 0;
            var remoteRoot = "Assets/GameRes";
            if (AssetDatabase.IsValidFolder(remoteRoot))
            {
                foreach (var sub in AssetDatabase.GetSubFolders(remoteRoot))
                {
                    var subName = Path.GetFileName(sub);
                    if (ShouldSkipAddressablePath(sub)) continue;
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

            // 拆包标签:每个 Remote 条目恰好一个 pack_ 标签,配合 PackTogetherByLabel 决定 bundle 粒度。
            int packUnits = Shenxiao.EditorTools.Packaging.PackLabeler.AssignAll(settings);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(settings);
            Debug.Log($"[AddressableSetup] local entries: {countLocal}, remote entries: {countRemote}, pack units: {packUnits}");
        }

        /// <summary>CLI 入口：完成自动分组并以进程码返回，避免 -quit 抢在脚本重载后的方法执行之前。</summary>
        public static void AutoGroupAllBatch()
        {
            try
            {
                AutoGroupAll();
                Debug.Log("[AddressableSetup] AutoGroupAllBatch OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AddressableSetup] AutoGroupAllBatch FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static AddressableAssetGroup EnsureLocalGroup(AddressableAssetSettings settings)
        {
            var g = settings.FindGroup(LocalGroupName);
            if (g == null)
            {
                g = settings.CreateGroup(LocalGroupName, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var schema = g.GetSchema<BundledAssetGroupSchema>() ?? g.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.IncludeInBuild = true;
            return g;
        }

        private static AddressableAssetGroup EnsureRemoteGroup(AddressableAssetSettings settings, string name)
        {
            var g = settings.FindGroup(name);
            if (g == null)
            {
                g = settings.CreateGroup(name, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var schema = g.GetSchema<BundledAssetGroupSchema>() ?? g.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.IncludeInBuild = true;
            return g;
        }

        private static void EnsureRemoteProfileDefaults(AddressableAssetSettings settings)
        {
            var profiles = settings.profileSettings;
            var profileId = settings.activeProfileId;
            if (profiles == null || string.IsNullOrEmpty(profileId)) return;

            var remoteBuildPath = profiles.GetValueByName(profileId, AddressableAssetSettings.kRemoteBuildPath);
            if (string.IsNullOrWhiteSpace(remoteBuildPath))
            {
                profiles.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, AddressableAssetSettings.kRemoteBuildPathValue);
            }

            var remoteLoadPath = profiles.GetValueByName(profileId, AddressableAssetSettings.kRemoteLoadPath);
            if (string.IsNullOrWhiteSpace(remoteLoadPath)
                || remoteLoadPath == AddressableAssetProfileSettings.undefinedEntryValue)
            {
                profiles.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPathDefault);
            }
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
                if (ShouldSkipAddressablePath(path)) continue;
                if (path.EndsWith(".meta") || path.EndsWith(".cs") || path.EndsWith(".asmdef")) continue;
                if (path.EndsWith(".import.json")) continue; // Laya3D 转换元数据,仅编辑器用

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

        private static bool ShouldSkipAddressablePath(string path)
        {
            string normalized = path.Replace('\\', '/');
            return normalized == "Assets/GameRes/_Generated"
                || normalized.StartsWith("Assets/GameRes/_Generated/", System.StringComparison.Ordinal);
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
            string defaultAddress = rel.Replace('\\', '/').ToLowerInvariant();
            return ResourcePath.ApplyAssetAddressAlias(assetPath, defaultAddress);
        }
    }
}
