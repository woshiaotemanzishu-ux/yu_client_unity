using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 登录连接等待层的重构 UI 生成器。
    /// 只写全新的 WaitforOpenViewLoading.prefab，不会重建或改动任何现有登录页面。
    /// 结构对标老端 login/WaitforOpenViewLoading：屏幕中心 123×123 金色旋转圈 +“加载中”文字。
    /// </summary>
    public static class WaitforOpenViewLoadingCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/WaitforOpenViewLoading.prefab";
        private const string CircleSprite = "resource/game/login/texture/common_loading_circle.png";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "WaitforOpenViewLoading(连接等待层)",
                Note = "只生成新增等待层，不触碰现有登录 Prefab；旋转速度可在组件上手调",
                Order = 15,
                Generate = Generate,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform root = UiCreatorKit.NewRoot("WaitforOpenViewLoading");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<WaitforOpenViewLoading>();

            Image circle = UiCreatorKit.NewImage("Circle", root);
            UiCreatorKit.Place(circle.rectTransform, 0f, 0f, 123f, 123f);
            circle.raycastTarget = false;
            circle.preserveAspect = true;
            UiCreatorKit.TrySetSprite(circle, CircleSprite, new Color(0.72f, 0.50f, 0.12f, 0.82f));
            view._img_circle = circle;

            TextMeshProUGUI label = UiCreatorKit.NewText("LoadingLabel", root, "加载中");
            UiCreatorKit.Place(label.rectTransform, 0f, 0f, 123f, 36f);
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(247, 209, 106, 255);
            view._lb_loading = label;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] WaitforOpenViewLoading.prefab 已生成: " + PrefabPath
                      + "（只新增连接等待层；真机包前记得跑 Addressable 自动分组）");
        }
    }
}
