using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 新建独立的登录展示舞台,不修改任何现有登录 prefab。
    /// WebBackground 铺满实际屏幕;Viewport 固定宽 720、纵向铺满父级,承载原有六个登录页面。
    /// Expand 画布下,长竖屏会扩展 Viewport 高度;横向宽屏画布高度仍为 1280,只在左右露出背景。
    /// </summary>
    public static class LoginStageCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/Login/LoginStage.prefab";
        private const string PLACEHOLDER_BACKGROUND = "resource/game/login/other/full_screen_bg.jpg";
        private const float FALLBACK_BACKGROUND_ASPECT = 16f / 9f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "LoginStage(宽屏补边 + 长屏铺满)",
                Note = "视口固定宽720并纵向拉伸;宽屏只补左右,长竖屏不再补上下",
                Order = 5,
                Generate = Generate,
                PrefabPath = PREFAB_PATH,
            });
        }

        public static void Generate()
        {
            RectTransform root = UiCreatorKit.NewRoot("LoginStage");
            root.gameObject.SetActive(false);
            LoginStage stage = root.gameObject.AddComponent<LoginStage>();

            Image background = UiCreatorKit.NewImage("WebBackground", root);
            UiCreatorKit.Place(background.rectTransform, 0f, 0f,
                UiCreatorKit.DesignWidth, UiCreatorKit.DesignHeight);
            background.raycastTarget = false;
            UiCreatorKit.TrySetSprite(background, PLACEHOLDER_BACKGROUND, UiCreatorKit.Palette.Bg);

            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.sprite != null && background.sprite.rect.height > 0f
                ? background.sprite.rect.width / background.sprite.rect.height
                : FALLBACK_BACKGROUND_ASPECT;

            RectTransform viewport = UiCreatorKit.NewNode("Viewport720x1280", root);
            viewport.anchorMin = new Vector2(0.5f, 0f);
            viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.anchoredPosition = Vector2.zero;
            viewport.sizeDelta = new Vector2(UiCreatorKit.DesignWidth, 0f);

            stage.webBackground = background;
            stage.backgroundFitter = fitter;
            stage.viewport = viewport;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PREFAB_PATH);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] LoginStage.prefab 已生成: " + PREFAB_PATH
                + "(视口固定宽720、纵向铺满;没有重建原登录页)");
        }
    }
}
