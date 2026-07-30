using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 服务器展示 / 踏入仙界页(重构版)纯代码建树生成器。
    ///
    /// 结构对标截图3:全屏立绘背景 + logo「九州神霄录」(上方) + 当前服按钮(状态图标 + 服名 + 提示) +
    /// 底部「踏入仙界」按钮 + 协议勾选行;另含用户协议弹层(默认隐:正文 + 拒绝 + 同意,对标 LoginAlertView)。
    /// 挂 ServerEnterView 并回填全部 public 引用,存 Assets/Prefabs/UI/Login/ServerEnterView.prefab。
    /// 元素已贴老端真实源图(skin 取自 LoginEnterView.scene / LoginAlertView.scene),贴不到回退占位色。
    /// 尺寸/位置为 720×1280 起步值,自行在编辑器调。入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class ServerEnterCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/ServerEnterView.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_BG = "resource/game/login/other/组 1.png";
        private const string IMG_LOGO = "resource/game/login/other/logo.png";                    // 「九州神霄录」logo
        private const string IMG_SERVER_BG = "resource/game/login/other/ui_Login_18.png";         // 当前服条底图
        private const string IMG_SERVER_STATE = "resource/game/login/texture/ui_Login_27.png";   // 当前服状态图标
        private const string IMG_ENTER = "resource/game/login/texture/ui_Login_07.png";          // 踏入仙界按钮
        private const string IMG_CHECK_BG = "resource/game/login/texture/ui_22.png";             // 协议勾选框底
        private const string IMG_CHECK_MARK = "resource/game/login/texture/ui_23.png";           // 协议勾选标记
        // 协议弹层(对标 LoginAlertView.scene)
        private const string IMG_ALERT_FRAME = "resource/game/login/other/bg_03.png";             // 协议弹层框
        private const string IMG_ALERT_TITLE = "resource/game/login/other/uildzdz_008d.png";       // 协议弹层标题图
        private const string IMG_BTN_OK = "resource/game/alert/texture/com_rect_btn12.png";      // 同意
        private const string IMG_BTN_CANCEL = "resource/game/alert/texture/com_rect_btn13.png";  // 拒绝

        // 主页布局(屏幕中心为原点,720×1280)
        private const float LogoW = 506f, LogoH = 166f, LogoY = 430f;
        private const float ServerBtnW = 470f, ServerBtnH = 58f, ServerBtnY = 0f;
        private const float EnterW = 378f, EnterH = 140f, EnterY = -200f;
        private const float AgreementCheckX = -186f, AgreementLabelX = 24f, AgreementBottomY = 60f;
        private const float NoticeBtnX = 275f, NoticeBtnY = 545f, NoticeBtnW = 120f, NoticeBtnH = 58f;

        // 协议弹层布局(弹层中心为原点)
        private const float AlertW = 720f, AlertH = 434f;
        private const float AlertTitleW = 288f, AlertTitleH = 52f, AlertTitleY = 184f;
        private const float AlertContentW = 578.4084f, AlertContentH = 320.3071f;
        private const float AlertContentX = 2.4276f, AlertContentY = -21.773499f;
        private const float AlertBtnW = 175f, AlertBtnH = 70f, AlertBtnY = -230f;
        private const float AlertBtnLeftX = -130f, AlertBtnRightX = 130f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "ServerEnter(踏入仙界)",
                Note = "全屏立绘 + logo + 当前服按钮 + 踏入仙界 + 协议勾选/弹层",
                Order = 30,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再激活(对齐 LoginPanelCreator)。
            RectTransform root = UiCreatorKit.NewRoot("ServerEnterView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<ServerEnterView>();

            // ---------- 全屏立绘背景 ----------
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);

            // ---------- logo「九州神霄录」 ----------
            Image logo = UiCreatorKit.NewImage("Logo", root);
            UiCreatorKit.Place(logo.rectTransform, 0f, LogoY, LogoW, LogoH);
            logo.raycastTarget = false;
            UiCreatorKit.TrySetSprite(logo, IMG_LOGO, UiCreatorKit.Palette.Panel);

            // ---------- 当前服务器按钮(状态图标 + 服名 + 提示) ----------
            UiCreatorKit.ButtonParts server = UiCreatorKit.NewButton("ServerBtn", root, string.Empty);
            UiCreatorKit.Place(server.root, 0f, ServerBtnY, ServerBtnW, ServerBtnH);
            UiCreatorKit.TrySetSprite(server.bg, IMG_SERVER_BG, UiCreatorKit.Palette.Panel);
            server.label.gameObject.SetActive(false);   // 不用按钮自带的整块 Label,改用下面分块布局
            view.serverBtn = server.bg;

            Image stateIcon = UiCreatorKit.NewImage("ServerStateIcon", server.root);
            PlaceAnchored(stateIcon.rectTransform, Vector2.zero, 0f, 0f, 67f, 38f);
            stateIcon.raycastTarget = false;
            UiCreatorKit.TrySetSprite(stateIcon, IMG_SERVER_STATE, UiCreatorKit.Palette.Mark);
            view.serverStateIcon = stateIcon;

            TextMeshProUGUI serverName = UiCreatorKit.NewText("ServerNameLabel", server.root, "A1-妖兽契约");
            PlaceAnchored(serverName.rectTransform, new Vector2(0f, 1f), 188.5f, -29f, 141.01f, 50f);
            serverName.alignment = TextAlignmentOptions.Center;
            view.serverNameLabel = serverName;

            TextMeshProUGUI tip = UiCreatorKit.NewText("TipLabel", server.root, "(点击换区)");
            PlaceAnchored(tip.rectTransform, new Vector2(0f, 1f), 339.005f, -29f, 160f, 50f);
            tip.alignment = TextAlignmentOptions.Left;
            view.tipLabel = tip;

            // ---------- 踏入仙界按钮 ----------
            UiCreatorKit.ButtonParts enter = UiCreatorKit.NewButton("EnterBtn", root, "踏入仙界");
            UiCreatorKit.Place(enter.root, 0f, EnterY, EnterW, EnterH);
            UiCreatorKit.TrySetSprite(enter.bg, IMG_ENTER, UiCreatorKit.Palette.BtnPrimary);
            enter.label.gameObject.SetActive(false);
            view.enterBtn = enter.bg;
            view.enterBtnLabel = enter.label;

            // ---------- 运营公告入口（登录初查与 10207 共用同一 CDN 快照） ----------
            UiCreatorKit.ButtonParts notice = UiCreatorKit.NewButton("NoticeBtn", root, "公告");
            UiCreatorKit.Place(notice.root, NoticeBtnX, NoticeBtnY, NoticeBtnW, NoticeBtnH);
            view.noticeBtn = notice.bg;
            view.noticeBtnLabel = notice.label;

            // ---------- 协议勾选行(勾选框 + 标记 + 文案) ----------
            UiCreatorKit.ButtonParts check = UiCreatorKit.NewButton("AgreementCheckBg", root, string.Empty);
            PlaceAnchored(check.root, new Vector2(0.5f, 0f), AgreementCheckX, AgreementBottomY, 34f, 36f);
            UiCreatorKit.TrySetSprite(check.bg, IMG_CHECK_BG, UiCreatorKit.Palette.Input);
            check.label.gameObject.SetActive(false);
            view.agreementCheckBg = check.bg;

            Image checkMark = UiCreatorKit.NewImage("AgreementCheckMark", check.root);
            UiCreatorKit.Place(checkMark.rectTransform, 2.8f, 4.5f, 30f, 30f);
            checkMark.raycastTarget = false;
            UiCreatorKit.TrySetSprite(checkMark, IMG_CHECK_MARK, UiCreatorKit.Palette.Mark);
            view.agreementCheckMark = checkMark;

            TextMeshProUGUI agreementLabel = UiCreatorKit.NewText(
                "AgreementLabel", root, "我已仔细阅读并同意 思橙用户协议");
            PlaceAnchored(agreementLabel.rectTransform, new Vector2(0.5f, 0f),
                AgreementLabelX, AgreementBottomY, 360f, 36f);
            agreementLabel.alignment = TextAlignmentOptions.Left;
            agreementLabel.fontSize = 24f;
            agreementLabel.raycastTarget = true;   // 文案可点(对标老端 _lb_agrement mouseEnabled)
            view.agreementLabel = agreementLabel;

            // ---------- 用户协议弹层(默认隐) ----------
            BuildAgreementAlert(root, view);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] ServerEnterView.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<ServerEnterView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>建用户协议弹层(对标 LoginAlertView.scene):半透明遮罩 + 框 + 正文 + 拒绝/同意。默认隐藏。</summary>
        private static void BuildAgreementAlert(RectTransform root, ServerEnterView view)
        {
            RectTransform alert = UiCreatorKit.NewNode("AgreementAlert", root);
            UiCreatorKit.Stretch(alert);
            view.agreementAlert = alert.gameObject;

            // 全屏半透明遮罩(挡住下层点击)
            Image mask = UiCreatorKit.NewImage("Mask", alert);
            UiCreatorKit.Stretch(mask.rectTransform);
            mask.color = new Color(0f, 0f, 0f, 0.6f);
            mask.raycastTarget = true;

            // 弹层框
            Image frame = UiCreatorKit.NewImage("Frame", alert);
            UiCreatorKit.Place(frame.rectTransform, 0f, 0f, AlertW, AlertH);
            frame.raycastTarget = true;
            UiCreatorKit.TrySetSprite(frame, IMG_ALERT_FRAME, UiCreatorKit.Palette.Panel);

            // 标题
            TextMeshProUGUI title = UiCreatorKit.NewText("Title", frame.transform, "用户协议和隐私保护指引");
            UiCreatorKit.Place(title.rectTransform, 0f, 185f, AlertW - 199f, 44f);
            title.fontSize = 30f;
            title.gameObject.SetActive(false);

            Image titleImage = UiCreatorKit.NewImage("TitleImg", frame.transform);
            UiCreatorKit.Place(titleImage.rectTransform, 0f, AlertTitleY, AlertTitleW, AlertTitleH);
            titleImage.raycastTarget = true;
            UiCreatorKit.TrySetSprite(titleImage, IMG_ALERT_TITLE, UiCreatorKit.Palette.Panel);

            // 协议正文(对标 LoginAlertView _html_content)
            TextMeshProUGUI content = UiCreatorKit.NewText("Content", frame.transform,
                "请你务必审慎阅读、充分理解“用户协议”和“隐私保护指引”各条款,包括但不限于:" +
                "为了向你提供即时通讯、内容分享等服务,我们需要收集你的设备信息、操作日志等个人信息。" +
                "你可以在“设置”中查看、变更、删除个人信息并管理你的授权。" +
                "你可阅读《用户协议》、《隐私保护指引》了解详细信息。如你同意,请点击“同意”开始接受我们的服务。");
            UiCreatorKit.Place(content.rectTransform, AlertContentX, AlertContentY, AlertContentW, AlertContentH);
            content.alignment = TextAlignmentOptions.TopLeft;
            content.fontSize = 24f;
            view.agreementContent = content;

            // 拒绝(左) | 同意(右)
            UiCreatorKit.ButtonParts cancel = UiCreatorKit.NewButton("CancelBtn", frame.transform, "拒绝");
            UiCreatorKit.Place(cancel.root, AlertBtnLeftX, AlertBtnY, AlertBtnW, AlertBtnH);
            UiCreatorKit.TrySetSprite(cancel.bg, IMG_BTN_CANCEL, UiCreatorKit.Palette.BtnNeutral);
            view.agreementCancelBtn = cancel.bg;

            UiCreatorKit.ButtonParts ok = UiCreatorKit.NewButton("OkBtn", frame.transform, "同意");
            UiCreatorKit.Place(ok.root, AlertBtnRightX, AlertBtnY, AlertBtnW, AlertBtnH);
            UiCreatorKit.TrySetSprite(ok.bg, IMG_BTN_OK, UiCreatorKit.Palette.BtnPrimary);
            view.agreementOkBtn = ok.bg;

            alert.gameObject.SetActive(false);
        }

        private static void PlaceAnchored(RectTransform rect, Vector2 anchor,
            float x, float y, float width, float height)
        {
            UiCreatorKit.Place(rect, x, y, width, height);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = new Vector2(x, y);
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 ServerEnterView",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<ServerEnterView>() 加载新 prefab,仅用于看结构/试交互。",
                    "好");
                return;
            }
            // 先释放上次预览缓存的实例,确保每次都从【最新】prefab 重新实例化。
            ViewManager.Dispose<ServerEnterView>();
            _ = ViewManager.Open<ServerEnterView>();
        }
    }
}
