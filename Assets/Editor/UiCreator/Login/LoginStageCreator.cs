using Shenxiao.Module.Core.Login;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 新建独立的登录展示舞台,不修改任何现有登录 prefab。
    /// WebBackground 铺满实际屏幕;Viewport 固定 720x1280 居中,承载原有六个登录页面。
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
                Name = "LoginStage(Web 背景 + 居中视口)",
                Note = "新增外壳,不修改现有登录 prefab;背景图当前为占位图",
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
            UiCreatorKit.Place(viewport, 0f, 0f, UiCreatorKit.DesignWidth, UiCreatorKit.DesignHeight);

            stage.webBackground = background;
            stage.backgroundFitter = fitter;
            stage.viewport = viewport;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PREFAB_PATH);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] LoginStage.prefab 已生成: " + PREFAB_PATH
                + "(只新增外部背景/视口,没有重建原登录页)");
        }
    }
}
