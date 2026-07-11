using System;
using System.IO;
using System.Threading.Tasks;
using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 创角展示「整模→视频」迁移实证(2026-07-11):
    ///  ① 整模清干净:Assets/GameRes/object/role 下无 model_create_* 夹,Addressables 无同前缀条目
    ///  ② 剑士(RoleRes=1111)两段视频在位:VideoClip 资产可加载 + Addressables 地址已登记
    ///     + ResManager 编辑器兜底能按运行时 key(object/role/video_create/1111@create2)拿到 VideoClip
    ///  ③ RoleCreateView.prefab 结构:VideoImage 节点存在、紧跟 Bg、全屏拉伸、RawImage 默认不亮
    ///     不吃点击、view.videoImage 引用已回填
    /// (batch 域 VideoPlayer 不真解码,create2→create3 切段行为由 RoleCreateView 运行时逻辑负责,不在此断言。)
    /// </summary>
    public static class CreateRoleVideoCase
    {
        private const string RoleFolder = "Assets/GameRes/object/role";
        private const string VideoFolder = RoleFolder + "/video_create";
        private const string PrefabPath = "Assets/Prefabs/UI/Login/RoleCreateView.prefab";
        private const string StaleAddressPrefix = "object/role/model_create_";

        public static async Task<int> Run()
        {
            // ① 整模资源/条目确已清除
            int staleFolders = 0;
            foreach (string dir in AssetDatabase.GetSubFolders(RoleFolder))
            {
                if (Path.GetFileName(dir).StartsWith("model_create_", StringComparison.Ordinal)) staleFolders++;
            }
            int staleEntries = 0;
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            bool videoEntry2 = false, videoEntry3 = false;
            if (settings != null)
            {
                foreach (AddressableAssetGroup g in settings.groups)
                {
                    if (g == null) continue;
                    foreach (AddressableAssetEntry e in g.entries)
                    {
                        if (e == null || e.address == null) continue;
                        if (e.address.StartsWith(StaleAddressPrefix, StringComparison.Ordinal)) staleEntries++;
                        if (e.address == "object/role/video_create/1111@create2") videoEntry2 = true;
                        if (e.address == "object/role/video_create/1111@create3") videoEntry3 = true;
                    }
                }
            }
            bool purged = staleFolders == 0 && staleEntries == 0;
            Debug.Log("CLIVERIFY createvideo purge staleFolders=" + staleFolders
                + " staleEntries=" + staleEntries + " ok=" + purged);

            // ② 剑士视频在位:资产 + 条目 + 运行时 key 经编辑器兜底可加载
            var clip2 = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoFolder + "/1111@create2.mp4");
            var clip3 = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoFolder + "/1111@create3.mp4");
            bool assetsOk = clip2 != null && clip3 != null;
            bool sizeOk = clip2 != null && clip2.width == 720 && clip2.height == 1280;

            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true; // batch 域 Addressables 不推进,走兜底
            VideoClip byKey = await Shenxiao.Framework.Res.ResManager
                .LoadOptionalAsync<VideoClip>("object/role/video_create/1111@create2");
            bool keyLoadOk = byKey != null;
            Debug.Log("CLIVERIFY createvideo assets clip2=" + (clip2 != null) + " clip3=" + (clip3 != null)
                + " size720x1280=" + sizeOk + " entry2=" + videoEntry2 + " entry3=" + videoEntry3
                + " keyLoad=" + keyLoadOk);

            // ③ prefab 结构:VideoImage 紧跟 Bg、全屏、默认不亮、引用回填
            bool prefabOk = false, orderOk = false, stretchOk = false, hiddenOk = false;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            RoleCreateView view = prefab != null ? prefab.GetComponent<RoleCreateView>() : null;
            if (view != null && view.videoImage != null)
            {
                prefabOk = true;
                RawImage video = view.videoImage;
                var rt = (RectTransform)video.transform;
                Transform bg = prefab.transform.Find("Bg");
                orderOk = bg != null && video.transform.GetSiblingIndex() == bg.GetSiblingIndex() + 1;
                stretchOk = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                    && rt.offsetMin == Vector2.zero && rt.offsetMax == Vector2.zero;
                hiddenOk = !video.enabled && !video.raycastTarget;
            }
            Debug.Log("CLIVERIFY createvideo prefab bound=" + prefabOk + " orderAfterBg=" + orderOk
                + " stretch=" + stretchOk + " hiddenByDefault=" + hiddenOk);

            bool pass = purged && assetsOk && sizeOk && videoEntry2 && videoEntry3 && keyLoadOk
                && prefabOk && orderOk && stretchOk && hiddenOk;
            Debug.Log("CLIVERIFY createvideo VERDICT purged=" + purged + " assets=" + assetsOk
                + " size=" + sizeOk + " entries=" + (videoEntry2 && videoEntry3) + " keyLoad=" + keyLoadOk
                + " prefab=" + (prefabOk && orderOk && stretchOk && hiddenOk) + " pass=" + pass);
            return pass ? 0 : 3;
        }
    }
}
