using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Shenxiao.EditorTools.Packaging
{
    /// <summary>
    /// 打包方案的一次性配置迁移(2026-07 定案,幂等可重复执行):
    /// - 开远端 catalog,固定文件名 catalog_live.*(更新靠 .hash 驱动,文件名永不变,老壳不会 404)
    /// - Remote.LoadPath 改运行时变量 {ResCdn.BaseUrl}/[BuildTarget](一次构建、多环境复用,URL 由 AppConfig/版本API 注入)
    /// - Remote_* 组:PackTogetherByLabel(配合 pack_ 标签拆桶) + CRC 仅下载校验 + 重试3 + Timeout 0(它是整请求硬中止,不是空闲超时) + InternalId Dynamic(catalog 减肥)
    /// - App_Local:无 hash 文件名并视为冻结(远端内容依赖它时文件名必须稳定;内容变更必须随壳发版)
    /// - 移除 Launch 场景的 Addressables 双重收录(保留 EditorBuildSettings 一份)
    /// </summary>
    public static class PackagingSetup
    {
        private const string RemoteLoadPathRuntime =
            "{Shenxiao.Framework.Res.ResCdn.BaseUrl}/[BuildTarget]";

        [MenuItem("神霄/打包/① 一次性配置迁移(远端catalog·拆包参数)", priority = 10)]
        public static void MigrateSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[PackagingSetup] Addressable settings not found");
                return;
            }

            var log = new System.Text.StringBuilder("[PackagingSetup] 配置迁移:\n");

            // 1. 远端 catalog:固定名 catalog_live.bin/.hash,输出到 Remote.BuildPath,运行时从 Remote.LoadPath 拉
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.OverridePlayerVersion = "live";
            log.AppendLine("  BuildRemoteCatalog=true, catalog 固定名 catalog_live.*");

            // 2. profile:Remote.LoadPath 换成运行时变量(构建产物内不烘死 URL)
            settings.profileSettings.SetValue(settings.activeProfileId,
                AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPathRuntime);
            log.AppendLine($"  Remote.LoadPath = {RemoteLoadPathRuntime}");

            // 3. 逐组 schema
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null) continue;

                if (group.Name.StartsWith("Remote_"))
                {
                    schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
                    schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
                    schema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.Dynamic;
                    schema.UseAssetBundleCrc = true;                 // 仅远程下载校验
                    schema.UseAssetBundleCrcForCachedBundles = false; // 缓存后不重复校验
                    schema.RetryCount = 3;
                    schema.Timeout = 0; // UnityWebRequest.timeout 是整请求硬中止:大包弱网会被掐死,保持 0
                    EditorUtility.SetDirty(schema);
                    log.AppendLine($"  {group.Name}: PackTogetherByLabel + AppendHash + Dynamic InternalId + CRC(excl. cached) + Retry3");
                }
                else if (group.Name == "App_Local")
                {
                    // 冻结组:远端内容会依赖这里的 shader/字体,文件名必须跨发布稳定。
                    schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                    EditorUtility.SetDirty(schema);
                    log.AppendLine("  App_Local: NoHash 命名(冻结组,内容变更必须随壳发版)");
                }
            }

            // 4. Launch 场景双重收录:EditorBuildSettings 保留,Addressables 条目移除
            var launchGuid = AssetDatabase.AssetPathToGUID("Assets/_App/Scenes/Launch.unity");
            if (!string.IsNullOrEmpty(launchGuid) && settings.FindAssetEntry(launchGuid) != null)
            {
                settings.RemoveAssetEntry(launchGuid);
                log.AppendLine("  移除 App_Local 的 scenes/launch 条目(保留 EditorBuildSettings)");
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            log.AppendLine("完成。下一步:点『神霄/资源/Addressable 自动分组』刷 pack_ 标签,再『打包/② 分组与拆包校验』。");
            Debug.Log(log.ToString());
        }
    }
}
