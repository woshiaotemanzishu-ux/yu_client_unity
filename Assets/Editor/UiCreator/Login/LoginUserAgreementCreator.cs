using Shenxiao.Module.Core.Login;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 用户协议/隐私保护指引独立详情页生成器。
    /// 几何对标老端 LoginUserAgreementView 运行时快照；正文只在 View 中按渠道配置填充。
    /// </summary>
    public static class LoginUserAgreementCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/LoginUserAgreementView.prefab";
        private const string ImgFrame = "resource/game/login/other/bg_03.png";
        private const string ImgClose = "resource/game/login/texture/uisy_026.png";
        private const string ImgAgreementTitle = "resource/game/login/texture/user_xieyi.png";
        private const string ImgPrivacyTitle = "resource/game/login/texture/user_privacy.png";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "LoginUserAgreement(协议详情)",
                Note = "独立协议/隐私正文面板；滚动正文来自 ConfigAgreement 渠道表",
                Order = 31,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform root = UiCreatorKit.NewRoot("LoginUserAgreementView");
            root.gameObject.SetActive(false);
            LoginUserAgreementView view = root.gameObject.AddComponent<LoginUserAgreementView>();

            Image closeMask = UiCreatorKit.NewImage("CloseMask", root);
            UiCreatorKit.Stretch(closeMask.rectTransform);
            closeMask.color = new Color(0f, 0f, 0f, 0.62f);
            closeMask.raycastTarget = true;
            view.closeMask = closeMask;

            Image frame = UiCreatorKit.NewImage("_img_bg", root);
            UiCreatorKit.Place(frame.rectTransform, 0f, 0f, 650f, 800f);
            frame.raycastTarget = true;
            UiCreatorKit.TrySetSprite(frame, ImgFrame, UiCreatorKit.Palette.Panel);
            view._img_bg = frame;

            Image close = UiCreatorKit.NewImage("_img_close", frame.transform);
            UiCreatorKit.Place(close.rectTransform, 238.5f, 311f, 59f, 58f);
            close.raycastTarget = true;
            UiCreatorKit.TrySetSprite(close, ImgClose, UiCreatorKit.Palette.BtnNeutral);
            view._img_close = close;

            Image agreementTitle = UiCreatorKit.NewImage("_img_xieyi", frame.transform);
            UiCreatorKit.Place(agreementTitle.rectTransform, 0f, 354.5f, 194f, 51f);
            agreementTitle.raycastTarget = false;
            UiCreatorKit.TrySetSprite(agreementTitle, ImgAgreementTitle, UiCreatorKit.Palette.Panel);
            view._img_xieyi = agreementTitle;

            Image privacyTitle = UiCreatorKit.NewImage("_img_privacy", frame.transform);
            UiCreatorKit.Place(privacyTitle.rectTransform, 0f, 354f, 288f, 52f);
            privacyTitle.raycastTarget = false;
            UiCreatorKit.TrySetSprite(privacyTitle, ImgPrivacyTitle, UiCreatorKit.Palette.Panel);
            privacyTitle.gameObject.SetActive(false);
            view._img_privacy = privacyTitle;

            RectTransform panelRect = UiCreatorKit.NewNode("_panel_content", frame.transform);
            UiCreatorKit.Place(panelRect, 0.5f, -2f, 485f, 566f);
            Image panelHit = panelRect.gameObject.AddComponent<Image>();
            panelHit.color = new Color(1f, 1f, 1f, 0f);
            panelHit.raycastTarget = true;
            panelRect.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI content = UiCreatorKit.NewText("_lb_content", panelRect,
                "协议正文加载中…");
            RectTransform contentRect = content.rectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 566f);
            content.fontSize = 22f;
            content.alignment = TextAlignmentOptions.TopLeft;
            content.color = new Color32(0x92, 0x5D, 0x48, 0xFF);
            content.textWrappingMode = TextWrappingModes.Normal;
            content.overflowMode = TextOverflowModes.Overflow;
            content.raycastTarget = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panelRect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = panelRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 30f;
            view._panel_content = scroll;
            view._lb_content = content;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] LoginUserAgreementView.prefab 已生成: " + PrefabPath);
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览协议详情页", "请先进入 Play 模式后再预览。", "好");
                return;
            }
            ViewManager.Dispose<LoginUserAgreementView>();
            _ = ViewManager.Open<LoginUserAgreementView>(new LoginAgreementArgs(2,
                LoginUserAgreementView.TypeAgreement, "shenhai"));
        }
    }
}
