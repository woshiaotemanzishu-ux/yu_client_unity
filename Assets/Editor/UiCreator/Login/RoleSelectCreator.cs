using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Login
{
    /// <summary>
    /// 选角页(重构版)纯代码建树生成器(对标老端 LoginSelectRoleView.scene + LoginSelectRoleItem.scene)。
    ///
    /// 结构(对标参考截图,竖屏 720×1280,上→下):
    ///   顶部:4 张固定角色卡 RoleCard_0..3(直接建在 RoleCards 容器下,位置各自手调,不用模板克隆)。
    ///     每张卡内:Bg 底图 / SelectedFrame 选中胶囊 / Head 头像 / NameLabel / LevelRow{ TurnLabel /
    ///     AscendMark 升仙角标 / LevelLabel }。View 直接持有这些子控件引用(roleCards),运行时不按名查找。
    ///     超出角色数的卡=空槽「+」创建入口(运行时贴 ui_Login_04)。
    ///   顶部右:玩家头像占位组 Portrait(PortraitIcon 头像框 + PortraitName 名字)——纯装饰占位,未接数据,自行删/接。
    ///   正中:3D 模型容器 ModelCon(固定居中矩形,非铺满;UIModelStage 把 RawImage 贴进来)。
    ///     内含可见占位 ModelPlaceholder(半透明色块 + 说明字)——【运行时由 View 隐藏】,只为在编辑器里
    ///     看得见角色显示区、方便直接改 ModelCon 的位置/大小(容器 rect 直接决定角色上屏位置与大小,不拉伸)。
    ///   底部中:踏入仙界 EnterBtn(ui_Login_07);顶部左角:返回 ReturnBtn(ui_Login_01)。
    /// 挂 RoleSelectView 回填全部 public 引用,存 Assets/Prefabs/UI/Login/RoleSelectView.prefab。
    /// 元素已贴老端真实源图,贴不到回退占位色。尺寸/位置为 720×1280 起步值,自行在编辑器调。
    /// 入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class RoleSelectCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Login/RoleSelectView.prefab";

        // 老端源图(GameRes 相对路径;均已确认在 Assets/GameRes 下)
        private const string IMG_BG = "resource/game/login/other/full_screen_bg.jpg";
        private const string IMG_CARD_BG = "resource/game/login/texture/ui_Login_03.png";   // 未选底图(头像框)
        private const string IMG_CARD_BG2 = "resource/game/login/texture/ui_Login_06.png";  // 未选名牌胶囊
        private const string IMG_SC = "resource/game/login/texture/uisc_006.png";           // 升仙角标
        private const string IMG_ENTER = "resource/game/login/texture/ui_Login_07.png";      // 踏入仙界
        private const string IMG_RETURN = "resource/game/login/texture/ui_Login_01.png";     // 返回

        // 卡片尺寸(对标 LoginSelectRoleItem.scene:124×150,内部 _img_bg 102×102 头像框、_img_bg2 82×126 名牌)
        private const float CardW = 150f, CardH = 230f;
        private const float HeadFrameW = 110f, HeadFrameH = 110f;
        private const float NamePlateW = 96f, NamePlateH = 140f;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Login",
                Name = "RoleSelect(选角)",
                Note = "角色卡列表(模板克隆)+ 3D 模型 + 踏入仙界/返回",
                Order = 50,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再激活(与 LoginPanelCreator 一致)。
            RectTransform root = UiCreatorKit.NewRoot("RoleSelectView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<RoleSelectView>();

            // 全屏背景
            Image bg = UiCreatorKit.NewImage("Bg", root);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_BG, UiCreatorKit.Palette.Bg);

            // 3D 模型容器:正中固定矩形(非铺满)。容器 rect 直接决定角色上屏位置/大小(不拉伸),
            // 内含可见占位 ModelPlaceholder,运行时由 View 隐藏,只为在编辑器里看得见并直接调这颗节点。
            RectTransform model = UiCreatorKit.NewNode("ModelCon", root);
            UiCreatorKit.Place(model, 0f, -30f, 520f, 900f);
            view.modelCon = model;
            view.modelPlaceholder = BuildModelPlaceholder(model);

            // 4 张固定角色卡(顶部横排起步,位置各自手调)。直接建卡、回填引用,不用模板克隆。
            RectTransform cardsRoot = UiCreatorKit.NewNode("RoleCards", root);
            UiCreatorKit.Stretch(cardsRoot);   // 纯分组容器,卡自身用中心锚摆位
            float[] cardX = { -252f, -84f, 84f, 252f };  // 4 张横排起步 x(约 168 间距);自行在编辑器挪
            const float cardY = 470f;
            view.roleCards = new RoleSelectView.RoleCard[cardX.Length];
            for (int i = 0; i < cardX.Length; i++)
            {
                view.roleCards[i] = BuildCard(cardsRoot, i, cardX[i], cardY);
            }

            // 顶部右:玩家头像占位组(纯装饰,未接数据;自行删/接)。
            BuildPortraitPlaceholder(root);

            // 踏入仙界(底部居中,对标截图底部大按钮)
            Image enter = UiCreatorKit.NewImage("EnterBtn", root);
            UiCreatorKit.Place(enter.rectTransform, 0f, -560f, 378f, 140f);
            UiCreatorKit.TrySetSprite(enter, IMG_ENTER, UiCreatorKit.Palette.BtnPrimary);
            view.enterBtn = enter;

            // 返回(左上角,对标老端 centerX=-375)
            Image ret = UiCreatorKit.NewImage("ReturnBtn", root);
            UiCreatorKit.Place(ret.rectTransform, -320f, 575f, 96f, 72f);
            UiCreatorKit.TrySetSprite(ret, IMG_RETURN, UiCreatorKit.Palette.BtnSecond);
            view.returnBtn = ret;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] RoleSelectView.prefab 已生成: " + PrefabPath +
                      "(可经 ViewManager.Open<RoleSelectView>() 加载;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>
        /// 建一张固定角色卡(可见、可各自手调位置),回填 RoleSelectView.RoleCard 的直接引用(不再按名查找)。
        /// 空槽 vs 角色态的换图由运行时 RoleSelectView 负责,这里只搭结构 + 贴未选态起步图。
        /// </summary>
        private static RoleSelectView.RoleCard BuildCard(Transform parent, int index, float x, float y)
        {
            RectTransform card = UiCreatorKit.NewNode("RoleCard_" + index, parent);
            UiCreatorKit.Place(card, x, y, CardW, CardH);

            // 头像框底图(未选 ui_Login_03;点击命中体,raycastTarget 默认开)
            Image bg = UiCreatorKit.NewImage("Bg", card);
            UiCreatorKit.Place(bg.rectTransform, 0f, 50f, HeadFrameW, HeadFrameH);
            UiCreatorKit.TrySetSprite(bg, IMG_CARD_BG, UiCreatorKit.Palette.Panel);

            // 头像(贴在头像框内,运行时换图)
            Image head = UiCreatorKit.NewImage("Head", bg.transform);
            UiCreatorKit.Place(head.rectTransform, 0f, 0f, 90f, 90f);
            head.raycastTarget = false;
            head.color = UiCreatorKit.Palette.Mark;   // 占位:运行时换成真头像

            // 名牌胶囊(未选 ui_Login_06,承载名字/等级)
            Image selectedFrame = UiCreatorKit.NewImage("SelectedFrame", card);
            UiCreatorKit.Place(selectedFrame.rectTransform, 0f, -65f, NamePlateW, NamePlateH);
            selectedFrame.raycastTarget = false;
            UiCreatorKit.TrySetSprite(selectedFrame, IMG_CARD_BG2, UiCreatorKit.Palette.BtnSecond);

            // 名字
            TextMeshProUGUI name = UiCreatorKit.NewText("NameLabel", card, "角色名");
            UiCreatorKit.Place(name.rectTransform, 0f, -42f, NamePlateW, 30f);
            name.fontSize = 22f;

            // 等级行:转生 + 升仙角标 + 等级(横排)
            RectTransform hbox = UiCreatorKit.NewNode("LevelRow", card);
            UiCreatorKit.Place(hbox, 0f, -118f, NamePlateW, 26f);
            var lvLayout = hbox.gameObject.AddComponent<HorizontalLayoutGroup>();
            lvLayout.childAlignment = TextAnchor.MiddleCenter;
            lvLayout.spacing = 2f;
            lvLayout.childForceExpandWidth = false;
            lvLayout.childForceExpandHeight = false;
            lvLayout.childControlWidth = true;
            lvLayout.childControlHeight = false;

            TextMeshProUGUI turn = UiCreatorKit.NewText("TurnLabel", hbox, "1转");
            UiCreatorKit.Place(turn.rectTransform, 0f, 0f, 36f, 24f);
            turn.fontSize = 20f;

            Image sc = UiCreatorKit.NewImage("AscendMark", hbox);
            UiCreatorKit.Place(sc.rectTransform, 0f, 0f, 22f, 24f);
            sc.raycastTarget = false;
            UiCreatorKit.TrySetSprite(sc, IMG_SC, UiCreatorKit.Palette.Mark);
            sc.enabled = false;   // 默认不显示升仙角标

            TextMeshProUGUI lv = UiCreatorKit.NewText("LevelLabel", hbox, "1级");
            UiCreatorKit.Place(lv.rectTransform, 0f, 0f, 50f, 24f);
            lv.fontSize = 20f;

            return new RoleSelectView.RoleCard
            {
                bg = bg,
                selectedFrame = selectedFrame,
                ascendMark = sc,
                head = head,
                nameLabel = name,
                turnLabel = turn,
                levelLabel = lv,
            };
        }

        /// <summary>
        /// 角色模型占位:铺满 ModelCon 的半透明色块 + 说明字。仅编辑期可见,运行时由 RoleSelectView 隐藏。
        /// 用途:让开发者在编辑器里看得见角色显示区,直接改 ModelCon 的位置/大小即可挪角色/改大小。
        /// </summary>
        private static Image BuildModelPlaceholder(Transform model)
        {
            Image ph = UiCreatorKit.NewImage("ModelPlaceholder", model);
            UiCreatorKit.Stretch(ph.rectTransform);
            ph.raycastTarget = false;
            ph.sprite = null;
            ph.color = new Color(0.30f, 0.80f, 0.45f, 0.30f);   // 半透明绿:一眼看出是占位

            TextMeshProUGUI hint = UiCreatorKit.NewText("Hint", ph.transform,
                "角色模型占位\n(改 ModelCon 的位置/大小)");
            UiCreatorKit.Stretch(hint.rectTransform);
            hint.fontSize = 26f;
            hint.color = new Color(1f, 1f, 1f, 0.85f);
            return ph;
        }

        /// <summary>顶部右玩家头像占位组(装饰,未接数据):圆头像框 PortraitIcon + 名字 PortraitName。</summary>
        private static void BuildPortraitPlaceholder(Transform root)
        {
            RectTransform group = UiCreatorKit.NewNode("Portrait", root);
            UiCreatorKit.Place(group, 300f, 555f, 110f, 150f);

            Image portrait = UiCreatorKit.NewImage("PortraitIcon", group);
            UiCreatorKit.Place(portrait.rectTransform, 0f, 20f, 100f, 100f);
            portrait.raycastTarget = false;
            UiCreatorKit.TrySetSprite(portrait, IMG_CARD_BG2, UiCreatorKit.Palette.Panel);

            TextMeshProUGUI name = UiCreatorKit.NewText("PortraitName", group, "玩家名");
            UiCreatorKit.Place(name.rectTransform, 0f, -55f, 110f, 30f);
            name.fontSize = 22f;
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 RoleSelectView",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览经 ViewManager.Open<RoleSelectView>() 加载新 prefab,仅用于看结构/试交互。\n" +
                    "注意:需先有角色列表数据(走过 10000 回包)才能看到角色卡;否则只显示空槽创建入口。",
                    "好");
                return;
            }
            ViewManager.Dispose<RoleSelectView>();
            _ = ViewManager.Open<RoleSelectView>();
        }
    }
}
