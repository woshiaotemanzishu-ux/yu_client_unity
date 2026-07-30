using System;
using System.Collections.Generic;
using System.IO;
using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools.AssetHub
{
    /// <summary>
    /// 创角展示「整模 prefab → 交付视频」迁移(2026-07-11,可重跑幂等):
    ///  ① 清 Addressables 里全部 object/role/model_create_* 条目(整模已废弃)
    ///  ② 删 Assets/GameRes/object/role/model_create_* 整模资源夹(1111/1213/1300/1400)
    ///  ③ 登记 object/role/video_create/ 下的创角视频(地址=GameRes 相对路径小写去扩展,
    ///     同 AddressableSetup 约定;视频文件命名 {RoleRes}@create2.mp4 / {RoleRes}@create3.mp4)
    ///  ④ 给 RoleCreateView.prefab 原位补 VideoImage 节点并回填引用——不整树重生成,保住手调布局
    ///     (Creator 已同步加该节点,下次整树重生成也一致)
    /// 批处理:Unity.exe -batchmode -projectPath . -executeMethod
    ///        Shenxiao.EditorTools.AssetHub.CreateRoleVideoMigration.Run -logFile Temp/create_video_migration.log
    /// 这是已完成迁移后的对账/批处理入口,不再占用日常 Unity 菜单。
    /// </summary>
    public static class CreateRoleVideoMigration
    {
        private const string RoleFolder = "Assets/GameRes/object/role";
        private const string VideoFolder = RoleFolder + "/video_create";
        private const string PrefabPath = "Assets/Prefabs/UI/Login/RoleCreateView.prefab";
        private const string StaleAddressPrefix = "object/role/model_create_";

        public static void Run()
        {
            int code;
            try { code = Migrate(); }
            catch (Exception e)
            {
                Debug.LogError("[CreateVideoMig] EXCEPTION " + e);
                code = 1;
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        private static int Migrate()
        {
            AssetDatabase.Refresh();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[CreateVideoMig] Addressable settings 不存在");
                return 1;
            }

            // ① 清整模 Addressables 条目(先收集再删,避免边遍历边改)
            var stale = new List<AddressableAssetEntry>();
            foreach (AddressableAssetGroup g in settings.groups)
            {
                if (g == null) continue;
                foreach (AddressableAssetEntry e in g.entries)
                {
                    if (e != null && e.address != null
                        && e.address.StartsWith(StaleAddressPrefix, StringComparison.Ordinal))
                    {
                        stale.Add(e);
                    }
                }
            }
            foreach (AddressableAssetEntry e in stale) settings.RemoveAssetEntry(e.guid, false);
            Debug.Log("[CreateVideoMig] 清掉整模 Addressables 条目: " + stale.Count);

            // ② 删整模资源夹
            int folders = 0;
            foreach (string dir in AssetDatabase.GetSubFolders(RoleFolder))
            {
                if (!Path.GetFileName(dir).StartsWith("model_create_", StringComparison.Ordinal)) continue;
                if (AssetDatabase.DeleteAsset(dir)) folders++;
                else Debug.LogError("[CreateVideoMig] 删除失败: " + dir);
            }
            Debug.Log("[CreateVideoMig] 删掉整模资源夹: " + folders);

            // ③ 登记创角视频(幂等:CreateOrMoveEntry 已存在则原地更新)
            int videos = 0;
            AddressableAssetGroup group = settings.FindGroup("Remote_object");
            if (group == null)
            {
                Debug.LogError("[CreateVideoMig] Remote_object 组不存在(先跑 神霄/资源/Addressable 自动分组)");
                return 1;
            }
            if (AssetDatabase.IsValidFolder(VideoFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets("", new[] { VideoFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(path)) continue;
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    string address = MakeGameResAddress(path);
                    if (entry != null && entry.address != address) entry.address = address;
                    videos++;
                    Debug.Log("[CreateVideoMig] 视频条目: " + address);
                }
            }
            else
            {
                Debug.LogWarning("[CreateVideoMig] 视频夹不存在(还没拷视频?): " + VideoFolder);
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);

            // ④ prefab 原位补 VideoImage
            bool prefabOk = EnsureVideoImageInPrefab();

            AssetDatabase.SaveAssets();
            Debug.Log("[CreateVideoMig] DONE 清条目=" + stale.Count + " 删夹=" + folders
                + " 视频=" + videos + " prefab=" + prefabOk);
            return prefabOk ? 0 : 3;
        }

        /// <summary>同 AddressableSetup.MakeAddress 约定:GameRes 相对路径、去扩展、小写、正斜杠。</summary>
        private static string MakeGameResAddress(string assetPath)
        {
            string rel = assetPath.StartsWith("Assets/GameRes/", StringComparison.Ordinal)
                ? assetPath.Substring("Assets/GameRes/".Length)
                : assetPath;
            string ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext)) rel = rel.Substring(0, rel.Length - ext.Length);
            return rel.Replace('\\', '/').ToLowerInvariant();
        }

        /// <summary>给现有 prefab 补 VideoImage(紧跟 Bg 之后:盖背景、垫所有 UI 之下)并回填 view.videoImage。
        /// 结构与 RoleCreateCreator 生成的一致;已有节点/引用则跳过(幂等)。</summary>
        private static bool EnsureVideoImageInPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError("[CreateVideoMig] prefab 缺失: " + PrefabPath);
                return false;
            }
            try
            {
                RoleCreateView view = root.GetComponent<RoleCreateView>();
                if (view == null)
                {
                    Debug.LogError("[CreateVideoMig] prefab 根上没有 RoleCreateView");
                    return false;
                }
                if (view.videoImage != null)
                {
                    Debug.Log("[CreateVideoMig] prefab 已有 VideoImage,跳过");
                    return true;
                }

                Transform exist = root.transform.Find("VideoImage");
                RawImage video;
                if (exist != null)
                {
                    video = exist.GetComponent<RawImage>();
                    if (video == null) video = exist.gameObject.AddComponent<RawImage>();
                }
                else
                {
                    var go = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage));
                    go.transform.SetParent(root.transform, false);
                    video = go.GetComponent<RawImage>();
                }

                var rt = (RectTransform)video.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Transform bg = root.transform.Find("Bg");
                video.transform.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);
                video.raycastTarget = false;
                video.enabled = false; // 首帧就绪才由运行时点亮
                view.videoImage = video;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[CreateVideoMig] prefab 已补 VideoImage 并回填引用");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
