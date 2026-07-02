using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 登录页(重构版)纯代码建树生成器。
    ///
    /// 结构对标用户给的截图:全屏背景 + 一张卡片(承载表单),
    ///   登录子面板:账号行(标签 + 输入框)/ 密码行 / 记住密码(勾选 + 文字)/ 底部「注册 | 登录」并排;
    ///   注册子面板:账号行 / 密码行 / 底部「确定注册 | 返回」并排。
    /// 两子面板铺满卡片、SetActive 切换。挂 LoginPanelView 并回填全部 public 引用,
    /// 存 Assets/Prefabs/UI/Login/LoginPanel.prefab。元素已贴老端真实源图,贴不到回退占位色。
    /// 尺寸/位置为 720×1280 起步值,自行在编辑器调。入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class LoginPanelCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/LoginPanel.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_BG = "resource/game/login/other/full_screen_bg.jpg";
        private const string IMG_CARD = "resource/game/login/texture/ui_content_bg1.png";
        private const string IMG_INPUT = "resource/game/login/texture/bg_05.png";
        private const string IMG_BTN_PRIMARY = "resource/game/login/texture/ui_Login_btn_2.png"; // 登录/确定注册
        private const string IMG_BTN_SECOND = "resource/game/login/texture/ui_Login_btn_1.png";  // 注册/返回
        private const string IMG_CHECK_ON = "resource/game/alert/texture/com_ui_gx1.png";  // 选中(勾)
        private const string IMG_CHECK_OFF = "resource/game/alert/texture/com_ui_gx.png";  // 未选中(空框)

        // 卡片内表单布局(卡片中心为原点)
        private const float CardW = 660f, CardH = 470f;
        private const float RowAccountY = 110f, RowPasswordY = 28f;
        private const float LabelX = -250f, LabelW = 120f;
        private const float InputX = 55f, InputW = 400f, InputH = 68f;
        private const float BtnW = 230f, BtnH = 88f, BtnLeftX = -135f, BtnRightX = 135f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "LoginPanel(登录 + 注册)",
                Note = "卡片表单:账号/密码 + 记住密码 + 并排按钮,子面板切换",
                Order = 20,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建:避免 TMP_InputField.OnEnable 早于接引用而刷错;建完再激活。
            RectTransform root = UiCreatorKit.NewRoot("LoginPanel");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<LoginPanelView>();

            // 全屏背景
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);

            // 承载表单的卡片
            Image card = UiCreatorKit.NewImage("Card", root);
            UiCreatorKit.Place(card.rectTransform, 0f, 0f, CardW, CardH);
            card.raycastTarget = false;
            UiCreatorKit.TrySetSprite(card, IMG_CARD, UiCreatorKit.Palette.Panel);

            // ---------- 登录子面板(铺满卡片) ----------
            RectTransform loginGroup = UiCreatorKit.NewNode("LoginGroup", card.transform);
            UiCreatorKit.Stretch(loginGroup);
            view.loginGroup = loginGroup.gameObject;

            MakeFormRow(loginGroup, "账号", RowAccountY, "请输入账号", false, out view.loginAccount);
            MakeFormRow(loginGroup, "密码", RowPasswordY, "请输入密码", true, out view.loginPassword);

            // 记住密码:两张状态图叠放(未选中 CheckImg / 选中 CheckImg1),代码只切显隐;+ 文字
            Image checkOff = UiCreatorKit.NewImage("CheckImg", loginGroup);       // 未选中(空框)
            UiCreatorKit.Place(checkOff.rectTransform, -80f, -50f, 40f, 40f);
            UiCreatorKit.TrySetSprite(checkOff, IMG_CHECK_OFF, UiCreatorKit.Palette.Mark);
            checkOff.gameObject.SetActive(false);                                 // 默认记住=true,静态先显选中图
            view.checkImg = checkOff;

            Image checkOn = UiCreatorKit.NewImage("CheckImg1", loginGroup);       // 选中(勾)
            UiCreatorKit.Place(checkOn.rectTransform, -80f, -50f, 40f, 40f);
            UiCreatorKit.TrySetSprite(checkOn, IMG_CHECK_ON, UiCreatorKit.Palette.Mark);
            view.checkImg1 = checkOn;

            TextMeshProUGUI checkLabel = UiCreatorKit.NewText("CheckLabel", loginGroup, "记住密码");
            UiCreatorKit.Place(checkLabel.rectTransform, 40f, -50f, 160f, 40f);
            checkLabel.alignment = TextAlignmentOptions.Left;
            view.rememberLabel = checkLabel;

            // 底部按钮:注册(左) | 登录(右)
            UiCreatorKit.ButtonParts gotoRegister = UiCreatorKit.NewButton("GotoRegisterBtn", loginGroup, "注册");
            UiCreatorKit.Place(gotoRegister.root, BtnLeftX, -155f, BtnW, BtnH);
            UiCreatorKit.TrySetSprite(gotoRegister.bg, IMG_BTN_SECOND, UiCreatorKit.Palette.BtnSecond);
            view.gotoRegisterBtn = gotoRegister.bg;

            UiCreatorKit.ButtonParts loginBtn = UiCreatorKit.NewButton("LoginBtn", loginGroup, "登录");
            UiCreatorKit.Place(loginBtn.root, BtnRightX, -155f, BtnW, BtnH);
            UiCreatorKit.TrySetSprite(loginBtn.bg, IMG_BTN_PRIMARY, UiCreatorKit.Palette.BtnPrimary);
            view.loginBtn = loginBtn.bg;
            view.loginBtnLabel = loginBtn.label;

            // ---------- 注册子面板(铺满卡片,默认隐藏) ----------
            RectTransform registerGroup = UiCreatorKit.NewNode("RegisterGroup", card.transform);
            UiCreatorKit.Stretch(registerGroup);
            view.registerGroup = registerGroup.gameObject;

            MakeFormRow(registerGroup, "账号", RowAccountY, "请输入账号", false, out view.registerAccount);
            MakeFormRow(registerGroup, "密码", RowPasswordY, "请输入密码", true, out view.registerPassword);

            // 底部按钮:确定注册(左) | 返回(右)
            UiCreatorKit.ButtonParts confirmBtn = UiCreatorKit.NewButton("ConfirmBtn", registerGroup, "确定注册");
            UiCreatorKit.Place(confirmBtn.root, BtnLeftX, -120f, BtnW, BtnH);
            UiCreatorKit.TrySetSprite(confirmBtn.bg, IMG_BTN_PRIMARY, UiCreatorKit.Palette.BtnPrimary);
            view.confirmBtn = confirmBtn.bg;

            UiCreatorKit.ButtonParts returnBtn = UiCreatorKit.NewButton("ReturnBtn", registerGroup, "返回");
            UiCreatorKit.Place(returnBtn.root, BtnRightX, -120f, BtnW, BtnH);
            UiCreatorKit.TrySetSprite(returnBtn.bg, IMG_BTN_SECOND, UiCreatorKit.Palette.BtnNeutral);
            view.returnBtn = returnBtn.bg;

            registerGroup.gameObject.SetActive(false);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] LoginPanel.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<LoginPanelView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>建一行表单:左侧标签(右对齐) + 右侧输入框(贴 bg_05)。</summary>
        private static void MakeFormRow(Transform parent, string labelText, float y,
            string placeholder, bool password, out TMP_InputField input)
        {
            TextMeshProUGUI label = UiCreatorKit.NewText(labelText + "Label", parent, labelText);
            UiCreatorKit.Place(label.rectTransform, LabelX, y, LabelW, 50f);
            label.alignment = TextAlignmentOptions.Right;

            input = UiCreatorKit.NewInput(password ? "PasswordInput" : "AccountInput", parent, placeholder, password);
            UiCreatorKit.Place((RectTransform)input.transform, InputX, y, InputW, InputH);
            UiCreatorKit.TrySetSprite(input.GetComponent<Image>(), IMG_INPUT, UiCreatorKit.Palette.Input);
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 LoginPanel",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<LoginPanelView>() 加载新 prefab,仅用于看结构/试交互。",
                    "好");
                return;
            }
            // 先释放上次预览缓存的实例,确保每次都从【最新】prefab 重新实例化(ViewManager.Open 命中缓存只会显示旧实例)。
            ViewManager.Dispose<LoginPanelView>();
            _ = ViewManager.Open<LoginPanelView>();
        }
    }
}
