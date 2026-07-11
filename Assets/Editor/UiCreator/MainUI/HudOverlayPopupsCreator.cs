using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「弹窗类弹层组」纯代码建树生成器(对标老端 yu_client/h5/bin/resource/game/mainUI/*.json)。
    ///
    /// 本文件共用注册两个条目:
    ///  ① MainUIPopups(独立弹窗×6):MainUIBuffView(含 MainUIBuffItem 模板) / MainUIFightModeView
    ///     (含 MainUIFightModeItem 模板) / MainUIIconBoxView(含既有 ActivityIcon.prefab 模板,嵌套实例
    ///     不重建)/ OpenDestinyView / TalkBoard / EyouThAdultItem。
    ///     【捆包已取消】:原 HudOverlayPopups.prefab 捆包是运行时死重——Buff/FightMode(MainUIOverlay.Toggle)
    ///     与 IconBox(MainUIIconBoxView.OpenFor)都按址加载各自的独立 prefab,捆包里的嵌套副本永不激活;
    ///     OpenDestiny/TalkBoard/EyouThAdultItem 三个业务未接线,同样存成独立 prefab 待接线时按址加载。
    ///     现在 GenerateOverlays 只产出 6 个独立 prefab,全部不进总装;三个新 prefab
    ///     (OpenDestinyView/TalkBoard/EyouThAdultItem)打真机包前记得跑一次「Addressable 自动分组」。
    ///  ② ArrowComponent(引导箭头):必须是独立 prefab——MainUIGuideManager.cs 用
    ///     GameResPath.GetUIPrefab("mainUI","ArrowComponent") 按名单独加载,故落在
    ///     Assets/Prefabs/UI/MainUI/ArrowComponent.prefab(Addressable 自动分组后 key 精确等于
    ///     prefabs/ui/mainui/arrowcomponent)。该路径下原有一份旧 LayaSceneConverter
    ///     自动烘焙的 prefab,本生成器点击后会覆盖为纯代码建树版本(取代碎片化镜像结构)。
    ///
    /// 这 7 个业务类均没有 [UIView] 特性(不像 Login 系列走 ViewManager.Open&lt;T&gt; 独立寻址),
    /// 因为它们要么被模块直接持有引用(①),要么按 prefab 名单独加载(②)。所以本文件的 Preview
    /// 不套 Login 样板的 ViewManager.Open&lt;T&gt;,改为直接 AssetDatabase 读最新 prefab + Instantiate +
    /// 手动调用业务方法——精神与 Login 一致(Play 模式检查、每次都从最新 prefab 重新实例化),
    /// 只是加载方式适配了这里没有寻址特性、且 Overlays/ 子目录不参与 Addressable key 拼接的事实。
    ///
    /// 布局换算:Laya 左上原点,parent 中心为 Unity 锚点原点。
    ///   默认(无 anchorX/Y、无 centerX/Y):cx = x + w/2 - parentW/2,cy = -(y + h/2 - parentH/2)。
    ///   若源 JSON 显式给了 anchorX/Y=0.5(节点自身轴心居中):(x,y) 已经是中心点,cx = x - parentW/2,
    ///   cy = parentH/2 - y。若给了 centerX/centerY(相对父中心的偏移):cx = centerX,cy = -centerY。
    ///   数值都是可手改的起步值,自行在编辑器精调。
    /// </summary>
    // 命名对照(Laya风格/序号占位名 -> 语义化英文;模板类节点如 MainUIBuffItem/MainUIFightModeItem/
    // ArrowComponent 保留类名不改):
    //   MainUIBuffView:
    //     image1 -> BuffPanelBg / image2 -> BuffTitleDivider / label1 -> BuffTitleLabel
    //     _list_buff_con -> BuffListScroll / lb_tips -> BuffCloseHintLabel(未接 Bind 字段的纯展示节点)
    //   MainUIBuffItem 模板:
    //     _Group1 -> ItemBody / _Image1 -> ItemBg / _img_buff -> BuffIconImage / _img_mask -> BuffCdMask
    //     _lb_time2 -> BuffIconBadgeLabel / _lb_name -> BuffNameLabel / _img_help -> BuffHelpIcon
    //     _lb_time -> BuffCountdownLabel / _gp_info -> DetailButtonGroup / _Image2 -> DetailButtonBg
    //     _Label1 -> DetailButtonLabel / _lb_desc -> BuffDescLabel / _gp_des -> DescGroup / _html_desc -> DescRichText
    //   MainUIFightModeView:
    //     image1 -> FightModePanelBg / image2 -> FightModeTitleDivider / label1 -> FightModeTitleLabel
    //     _gp_item -> FightModeItemList
    //   MainUIFightModeItem 模板:
    //     _img_bg -> ModeItemBg / _lb_desc -> ModeDescLabel / _img_status -> ModeStatusIcon / _img_select -> ModeSelectedFrame
    //   OpenDestinyView:
    //     _Label1 -> UnsealNoticeLine1Label / _Label2 -> UnsealNoticeLine2Label / _Label3 -> CongratsPrefixLabel
    //     _Label4 -> NewGodRankValueLabel / _img_click -> FullScreenCloseCatcher
    //   EyouThAdultItem:
    //     _box_arrow -> AdultWarningPanelBg / _img_red -> AdultWarningTextImage / _img_icon -> Adult18Icon
    //   ArrowComponent:
    //     _Image1 -> ArrowHeadImage
    public static class HudOverlayPopupsCreator
    {
        private const string ArrowPrefabPath = "Assets/Prefabs/UI/MainUI/ArrowComponent.prefab";
        // buff/fightmode 由 MainUIOverlayBootstrap 经 MainUIOverlay.Toggle("mainUI","MainUIxxxView")
        // 按地址独立加载 —— 必须存成同名独立 prefab 原地覆盖旧转换器版本(地址/GUID 不变)。
        private const string BuffPrefabPath = "Assets/Prefabs/UI/MainUI/MainUIBuffView.prefab";
        private const string FightModePrefabPath = "Assets/Prefabs/UI/MainUI/MainUIFightModeView.prefab";
        // 收纳盒弹窗:同 Buff/FightMode——独立 prefab 原地覆盖,运行时 MainUIIconBoxView.OpenFor 按地址
        // prefabs/ui/mainui/mainuiiconboxview 加载(点活动栏 is_box 图标 100/101 时弹出)。
        private const string IconBoxPrefabPath = "Assets/Prefabs/UI/MainUI/MainUIIconBoxView.prefab";
        // 未接线三件套:业务代码尚无加载入口,存成独立 prefab 待接线时按址加载(不进总装;
        // 打真机包前跑一次「Addressable 自动分组」)。
        private const string OpenDestinyPrefabPath = "Assets/Prefabs/UI/MainUI/OpenDestinyView.prefab";
        private const string TalkBoardPrefabPath = "Assets/Prefabs/UI/MainUI/TalkBoard.prefab";
        private const string EyouThAdultItemPrefabPath = "Assets/Prefabs/UI/MainUI/EyouThAdultItem.prefab";
        private const string ActivityIconPrefabPath = "Assets/Prefabs/UI/MainUI/ActivityIcon.prefab";

        // ---- 老端源图(GameRes 相对路径;已用 Glob 逐个确认存在于 Assets/GameRes 下) ----
        private const string IMG_BG_01 = "resource/game/common4/texture/bg_01.png";                 // 弹窗底板(9宫 20,20,20,20)
        private const string IMG_TITLELINE = "resource/game/common4/texture/com_titleline.png";     // 标题分割线(9宫)
        private const string IMG_MAIN_BUFF_BG = "resource/game/mainUI/texture/main_buff_bg.png";    // buff项底板(9宫 15,15,15,15)
        private const string IMG_COM_EMPTY = "resource/game/common/texture/com_empty.png";          // 占位图(buff图标运行时经 GetIcon 换真图)
        private const string IMG_BUFFT_MASK = "resource/game/mainUI/texture/mainui_bufft_bg.png";   // buff倒计时遮罩(默认隐藏)
        private const string IMG_HELP = "resource/game/common/texture/uity_004.png";                // 蒙面buff帮助图标(默认隐藏)
        private const string IMG_INFO_BTN_BG = "resource/game/common/texture/uian_001b.png";        // 查看详情按钮底
        private const string IMG_FIGHTMODE_SELECT = "resource/game/mainUI/texture/ui_23.png";       // 战斗模式选中框(默认隐藏)
        private const string IMG_ICONBOX_BG = "resource/game/mainUI/texture/uizjmgntc_002.png";     // 图标集底板(9宫 8,8,21,11)
        private const string IMG_ICONBOX_ARROW = "resource/game/mainUI/texture/uizjmgntc_001.png";  // 图标集箭头(scaleY=-1)
        private const string IMG_DESTINY_TITLE = "resource/game/mainUI/texture/uisc_005.png";       // 命运开启标题图
        private const string IMG_DESTINY_ICON1 = "resource/game/mainUI/texture/uisc_008.png";       // 命运图标(默认隐藏,业务未接)
        private const string IMG_DESTINY_INFO = "resource/game/mainUI/texture/ui_info.png";         // 装饰小图标(源 JSON 无 name,不进 Bind)
        private const string IMG_DESTINY_HINT = "resource/game/mainUI/texture/ui_hint.png";         // 装饰小图标(源 JSON 无 name,不进 Bind)
        private const string IMG_TALK_BOARD = "resource/game/common/texture/ui_talk_board.png";     // 对话气泡底(9宫 39,76,52,12)
        private const string IMG_EYOU_BG = "resource/game/mainUI/other/eyou_th_bg.png";             // 18+ 展开框(9宫 10,10,10,10)
        private const string IMG_EYOU_TXT = "resource/game/mainUI/other/eyou_th_txt.png";           // 18+ 警示文字图(默认隐藏)
        private const string IMG_EYOU_18 = "resource/game/mainUI/other/eyou_th_18.png";             // 18+ 图标
        private const string IMG_ARROW_HEAD = "resource/game/mainUI/texture/uixszy_002.png";        // 引导箭头图形
        private const string IMG_ARROW_BG = "resource/game/mainUI/texture/uixszy_001.png";          // 引导气泡底(9宫 27,23,43,23)
        private const string IMG_ARROW_CONTENT_IMG = "resource/game/mainUI/texture/uixszy_003.png"; // 引导气泡配图
        private const string IMG_ARROW_AUTO_BG = "resource/game/mainUI/texture/uity_038.png";       // 自动倒计时底

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIPopups(独立弹窗×6)",
                Note = "六个弹窗各自独立 prefab、按需按址加载(buff/fightmode 走 MainUIOverlay.Toggle,收纳盒走 OpenFor,其余三个待接线);不进总装",
                Order = 80,
                Generate = GenerateOverlays,
                Preview = PreviewOverlays,
                PrefabPath = BuffPrefabPath, // 状态灯代表件(六件各自独立,取 Buff 当代表)
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "ArrowComponent(引导箭头)",
                Note = "独立 prefab,MainUIGuideManager 按名单独加载,文件名必须精确",
                Order = 81,
                Generate = GenerateArrowComponent,
                Preview = PreviewArrow,
                PrefabPath = ArrowPrefabPath,
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIBuffView(buff浮层·独立)",
                Note = "MainUIOverlayBootstrap 按地址加载,原地覆盖旧版;生成 MainUIPopups 时会自动重生成",
                Order = 82,
                Generate = GenerateBuffView,
                PrefabPath = BuffPrefabPath,
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIFightModeView(战斗模式浮层·独立)",
                Note = "MainUIOverlayBootstrap 按地址加载,原地覆盖旧版;生成 MainUIPopups 时会自动重生成",
                Order = 83,
                Generate = GenerateFightModeView,
                PrefabPath = FightModePrefabPath,
            });

            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIIconBoxView(收纳盒弹窗·独立)",
                Note = "点活动栏收纳盒图标(100 福利大厅/101 寻宝)弹出;MainUIIconBoxView.OpenFor 按地址加载。生成后跑一次「Addressable 自动分组」",
                Order = 84,
                Generate = GenerateIconBoxView,
                PrefabPath = IconBoxPrefabPath,
            });
        }

        // ---------------------------------------------------------------- 独立浮层(原地覆盖)

        public static void GenerateBuffView()
        {
            GenerateStandalone(BuildMainUIBuffView, BuffPrefabPath, "MainUIBuffView");
        }

        public static void GenerateFightModeView()
        {
            GenerateStandalone(BuildMainUIFightModeView, FightModePrefabPath, "MainUIFightModeView");
        }

        public static void GenerateIconBoxView()
        {
            GenerateStandalone(BuildMainUIIconBoxView, IconBoxPrefabPath, "MainUIIconBoxView");
        }

        // 未接线三件套:与 Buff/FightMode/IconBox 同一 __StandaloneHolder 套路存独立 prefab
        // (根同样存为激活态,与既有独立弹窗 prefab 现状统一;显隐由接线后的加载方控制)。

        public static void GenerateOpenDestinyView()
        {
            GenerateStandalone(BuildOpenDestinyView, OpenDestinyPrefabPath, "OpenDestinyView");
        }

        public static void GenerateTalkBoard()
        {
            GenerateStandalone(BuildTalkBoard, TalkBoardPrefabPath, "TalkBoard");
        }

        public static void GenerateEyouThAdultItem()
        {
            GenerateStandalone(BuildEyouThAdultItem, EyouThAdultItemPrefabPath, "EyouThAdultItem");
        }

        /// <summary>
        /// 用建树函数产出【独立浮层 prefab】:根节点即视图本体(运行时按地址实例化后直接取根上的
        /// BaseView)。根保持激活(Build* 尾部的 SetActive(false) 在此被统一覆盖,六件口径一致),
        /// 显隐交给加载方(MainUIOverlay.Toggle / OpenFor / 待接线入口)。
        /// </summary>
        private static void GenerateStandalone(System.Action<Transform> build, string prefabPath, string viewName)
        {
            RectTransform holder = UiCreatorKit.NewRoot("__StandaloneHolder");
            holder.gameObject.SetActive(false);
            build(holder);
            Transform viewT = holder.GetChild(0);
            viewT.SetParent(null, false);
            viewT.gameObject.SetActive(true);
            UiCreatorKit.SavePrefab(viewT.gameObject, prefabPath);
            Object.DestroyImmediate(holder.gameObject);
            Debug.Log("[UiCreator] " + viewName + ".prefab 已生成(原地覆盖旧转换器版本,运行时地址不变): " + prefabPath);
        }

        // =====================================================================================
        // ① MainUIPopups(独立弹窗×6,捆包已取消)
        // =====================================================================================

        public static void GenerateOverlays()
        {
            // 捆包已取消:六个弹窗全部各自存独立 prefab,不再建 HudOverlayPopups.prefab、不进总装——
            // Buff/FightMode/IconBox 运行时本就按址加载独立 prefab(捆包副本是永不激活的死重);
            // OpenDestiny/TalkBoard/EyouThAdultItem 未接线,同样存独立 prefab 待接。
            GenerateBuffView();
            GenerateFightModeView();
            GenerateIconBoxView();
            GenerateOpenDestinyView();
            GenerateTalkBoard();
            GenerateEyouThAdultItem();

            Debug.Log("[UiCreator] MainUIPopups:6 个独立弹窗 prefab 已生成(Buff/FightMode/IconBox 原地覆盖运行时地址;" +
                      "OpenDestinyView/TalkBoard/EyouThAdultItem 为待接线新件)。真机包前记得跑 Addressable 自动分组。");
        }

        // ---------------------------------------------------------------- MainUIBuffView

        private static void BuildMainUIBuffView(Transform bundleRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIBuffView", bundleRoot);
            UiCreatorKit.Place(root, 0f, 0f, 358f, 180f);
            var view = root.gameObject.AddComponent<MainUIBuffView>();

            Image image1 = ImgFull("BuffPanelBg", root, IMG_BG_01, UiCreatorKit.Palette.Panel, sliced: true); // 老端: image1
            Image image2 = Img("BuffTitleDivider", root, 0f, 68.5f, 173f, 17f, IMG_TITLELINE, UiCreatorKit.Palette.BtnSecond, sliced: true); // 老端: image2
            TextMeshProUGUI label1 = Txt("BuffTitleLabel", root, 0f, 63f, 220f, 30f, "Buff显示列表", 20f, "#d15e00"); // 老端: label1
            label1.fontStyle = FontStyles.Bold;

            RectTransform content;
            ScrollRect list = NewScrollList("BuffListScroll", root, out content, spacing: 3f); // 老端: _list_buff_con
            UiCreatorKit.Place((RectTransform)list.transform, 0f, 0f, 328f, 100f);

            // 源 JSON 还有个 lb_tips("点击空白处关闭")，但当前 MainUIBuffViewBind 没有对应字段
            // (自动生成器未收录此节点)——保留为纯展示节点，不回填任何 view 字段，不影响 Bind 契约。
            Txt("BuffCloseHintLabel", root, 0f, -67f, 220f, 22f, "点击空白处关闭", 16f, "#a18c80"); // 老端: lb_tips

            RectTransform templates = NewTemplatesWrapper(root);
            GameObject itemTpl = BuildMainUIBuffItemTemplate(templates);

            view.image1 = image1;
            view.image2 = image2;
            view.label1 = label1;
            view._list_buff_con = list;
            view._tpl_MainUIBuffItem = itemTpl;

            root.gameObject.SetActive(false);
        }

        private static GameObject BuildMainUIBuffItemTemplate(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIBuffItem", parent);
            UiCreatorKit.Place(root, 0f, 0f, 328f, 95f);
            var item = root.gameObject.AddComponent<MainUIBuffItem>();

            RectTransform group1 = UiCreatorKit.NewNode("ItemBody", root); // 老端: _Group1
            UiCreatorKit.Stretch(group1);

            Image image1 = ImgFull("ItemBg", group1, IMG_MAIN_BUFF_BG, UiCreatorKit.Palette.Panel, sliced: true); // 老端: _Image1
            Image imgBuff = Img("BuffIconImage", group1, -115.5f, -1f, 75f, 75f, IMG_COM_EMPTY, UiCreatorKit.Palette.BtnSecond); // 老端: _img_buff
            Image imgMask = Img("BuffCdMask", group1, -115.5f, -1f, 75f, 75f, IMG_BUFFT_MASK, UiCreatorKit.Palette.Mark); // 老端: _img_mask
            imgMask.gameObject.SetActive(false); // 源 visible:false(倒计时遮罩,业务未接:待服务器时间+pie动画移植)

            TextMeshProUGUI lbTime2 = Txt("BuffIconBadgeLabel", group1, -115.5f, -1f, 60f, 34f, "", 26f, "#00fa64"); // 老端: _lb_time2

            TextMeshProUGUI lbName = Txt("BuffNameLabel", group1, 3f, 23.5f, 150f, 26f, "Buff名称", 20f, "#663915", TextAlignmentOptions.Left); // 老端: _lb_name
            lbName.fontStyle = FontStyles.Bold;
            Image imgHelp = Img("BuffHelpIcon", lbName.transform, 96f, 0f, 28f, 28f, IMG_HELP, UiCreatorKit.Palette.BtnSecond); // 老端: _img_help
            imgHelp.gameObject.SetActive(false); // 源 visible:false(蒙面buff帮助入口,业务未接:MaskInfoView/说明协议未移植)

            TextMeshProUGUI lbTime = Txt("BuffCountdownLabel", group1, 148.5f, 0f, 40f, 22f, "", 16f, "#0a953e", TextAlignmentOptions.Right); // 老端: _lb_time
            lbTime.fontStyle = FontStyles.Bold;

            RectTransform gpInfo = UiCreatorKit.NewNode("DetailButtonGroup", group1); // 老端: _gp_info
            UiCreatorKit.Place(gpInfo, -34.5f, -31.1f, 75f, 28f);
            Image image2 = ImgFull("DetailButtonBg", gpInfo, IMG_INFO_BTN_BG, UiCreatorKit.Palette.BtnSecond); // 老端: _Image2
            TextMeshProUGUI label1 = Txt("DetailButtonLabel", gpInfo, 0f, 0f, 75f, 28f, "查看详情", 14f, "#ffffff"); // 老端: _Label1
            gpInfo.gameObject.SetActive(false); // 源 visible:false(点击展开详情,业务未接)

            TextMeshProUGUI lbDesc = Txt("BuffDescLabel", root, 8f, -7.5f, 160f, 40f, "", 16f, "#7b5250", TextAlignmentOptions.TopLeft); // 老端: _lb_desc

            RectTransform gpDes = UiCreatorKit.NewNode("DescGroup", root); // 老端: _gp_des
            UiCreatorKit.Place(gpDes, 0.5f, -13f, 145f, 36f);
            VerticalLayoutGroup vlg = gpDes.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            TextMeshProUGUI htmlDesc = Txt("DescRichText", gpDes, 0f, 0f, 145f, 36f, "", 16f, "#7b5250", TextAlignmentOptions.TopLeft); // 老端: _html_desc

            item._Group1 = group1;
            item._Image1 = image1;
            item._img_buff = imgBuff;
            item._img_mask = imgMask;
            item._lb_time2 = lbTime2;
            item._lb_name = lbName;
            item._img_help = imgHelp;
            item._lb_time = lbTime;
            item._gp_info = gpInfo;
            item._Image2 = image2;
            item._Label1 = label1;
            item._lb_desc = lbDesc;
            item._gp_des = gpDes;
            item._html_desc = htmlDesc;

            root.gameObject.SetActive(false); // 模板默认隐藏(运行时克隆出来再显示)
            return root.gameObject;
        }

        // ---------------------------------------------------------------- MainUIFightModeView

        private static void BuildMainUIFightModeView(Transform bundleRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIFightModeView", bundleRoot);
            UiCreatorKit.Place(root, 0f, 0f, 350f, 216f);
            var view = root.gameObject.AddComponent<MainUIFightModeView>();

            Image image1 = ImgFull("FightModePanelBg", root, IMG_BG_01, UiCreatorKit.Palette.Panel, sliced: true); // 老端: image1
            Image image2 = Img("FightModeTitleDivider", root, 0f, 88.5f, 160f, 17f, IMG_TITLELINE, UiCreatorKit.Palette.BtnSecond, sliced: true); // 老端: image2
            TextMeshProUGUI label1 = Txt("FightModeTitleLabel", root, 0f, 85f, 200f, 30f, "战斗模式切换", 18f, "#b94a00"); // 老端: label1

            RectTransform gpItem = UiCreatorKit.NewNode("FightModeItemList", root); // 老端: _gp_item
            gpItem.anchorMin = Vector2.zero;
            gpItem.anchorMax = Vector2.one;
            gpItem.pivot = new Vector2(0.5f, 0.5f);
            gpItem.offsetMin = new Vector2(9f, 9f);
            gpItem.offsetMax = new Vector2(-9f, -36f); // 源 anchor top:36 bottom:9 left:9 right:9
            // 行距归 prefab,原 index*43 代码排版已删(MainUIFightModeView 只按序克隆,布局组按 sibling 顺序排)。
            // spacing = 43(老端行距)− 40(MainUIFightModeItem 模板高)= 3,恰好也等于源 VBox space:3。
            VerticalLayoutGroup vlg = gpItem.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            RectTransform templates = NewTemplatesWrapper(root);
            GameObject itemTpl = BuildMainUIFightModeItemTemplate(templates);

            view.image1 = image1;
            view.image2 = image2;
            view.label1 = label1;
            view._gp_item = gpItem;
            view._tpl_MainUIFightModeItem = itemTpl;

            root.gameObject.SetActive(false);
        }

        private static GameObject BuildMainUIFightModeItemTemplate(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIFightModeItem", parent);
            UiCreatorKit.Place(root, 0f, 0f, 332f, 40f);
            var item = root.gameObject.AddComponent<MainUIFightModeItem>();

            // _img_bg: 横向铺满、固定高度40、顶对齐(源 anchor left:0 right:0);无静态皮肤
            // (运行时经 GetIcon("mainUI", info.BgIcon) 换真图,sizeGrid 15,15,40,20 待贴图配好九宫边框)。
            RectTransform imgBgRt = UiCreatorKit.NewNode("ModeItemBg", root); // 老端: _img_bg
            imgBgRt.anchorMin = new Vector2(0f, 1f);
            imgBgRt.anchorMax = new Vector2(1f, 1f);
            imgBgRt.pivot = new Vector2(0.5f, 1f);
            imgBgRt.sizeDelta = new Vector2(0f, 40f);
            imgBgRt.anchoredPosition = Vector2.zero;
            Image imgBg = imgBgRt.gameObject.AddComponent<Image>();
            imgBg.raycastTarget = false; // 业务 SetData 时会置 true 再绑点击
            imgBg.color = UiCreatorKit.Palette.BtnNeutral;
            imgBg.type = Image.Type.Sliced;

            TextMeshProUGUI lbDesc = Txt("ModeDescLabel", root, -12f, 0f, 150f, 30f, "模式描述", 16f, "#583a40", TextAlignmentOptions.Left); // 老端: _lb_desc

            // ModeStatusIcon(老端 _img_status): 无静态皮肤(运行时经 GetIcon("mainUI", info.StatusIcon) 换真图)。
            Image imgStatus = Img("ModeStatusIcon", root, -127f, 0f, 68f, 26f, null, UiCreatorKit.Palette.BtnSecond); // 老端: _img_status

            Image imgSelect = Img("ModeSelectedFrame", root, 148.5f, 0f, 29f, 29f, IMG_FIGHTMODE_SELECT, UiCreatorKit.Palette.Mark); // 老端: _img_select
            imgSelect.gameObject.SetActive(false); // 源 visible:false(选中态,SetSelect 控制)

            item._img_bg = imgBg;
            item._lb_desc = lbDesc;
            item._img_status = imgStatus;
            item._img_select = imgSelect;

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------------------------------------------------------------- MainUIIconBoxView

        private static void BuildMainUIIconBoxView(Transform bundleRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIIconBoxView", bundleRoot);
            UiCreatorKit.Place(root, 200f, 400f, 189f, 128f);
            var view = root.gameObject.AddComponent<MainUIIconBoxView>();

            Image bg = Img("bg", root, -44.5f, -2f, 100f, 100f, IMG_ICONBOX_BG, UiCreatorKit.Palette.Panel, sliced: true);
            bg.raycastTarget = true; // 源 mouseEnabled:true / mouseThrough:false(挡穿透)

            RectTransform iconCon = UiCreatorKit.NewNode("icon_con", root);
            UiCreatorKit.Place(iconCon, 1.5f, -16.5f, 172f, 105f);
            // 网格归 prefab:成员图标由 GridLayoutGroup 排(固定 6 列、格 72×72=ActivityIcon.WIDTH/HEIGHT),
            // 运行时 MainUIIconBoxView 只按序克隆,原 i%6/floor(i/6) 代码排版已删。
            GridLayoutGroup grid = iconCon.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(ActivityIcon.WIDTH, ActivityIcon.HEIGHT);
            grid.spacing = Vector2.zero;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperLeft;

            Image arrow = Img("arrow", root, -69.5f, 19f, 50f, 30f, IMG_ICONBOX_ARROW, UiCreatorKit.Palette.BtnPrimary);
            arrow.rectTransform.localScale = new Vector3(1f, -1f, 1f); // 源 scaleY:-1

            RectTransform templates = NewTemplatesWrapper(root);
            GameObject activityIconGO = InstantiateExisting(ActivityIconPrefabPath, templates);
            if (activityIconGO != null)
            {
                activityIconGO.name = "ActivityIcon";
                UiCreatorKit.Place((RectTransform)activityIconGO.transform, 0f, 0f, ActivityIcon.WIDTH, ActivityIcon.HEIGHT);
            }

            view.bg = bg;
            view.icon_con = iconCon;
            view.arrow = arrow;
            view._tpl_ActivityIcon = activityIconGO;

            root.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- OpenDestinyView

        private static void BuildOpenDestinyView(Transform bundleRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("OpenDestinyView", bundleRoot);
            // root 收成实际内容占位(中央横幅 734×394 + 标题/文案/底部提示,含 y-345..+175 的散布件):
            // 源画布 867×1321 是出血遗留,可见内容根本不是全屏;真正要盖全屏的只有点击关闭捕获层(见下)。
            UiCreatorKit.Place(root, 0f, 0f, 740f, 700f);
            var view = root.gameObject.AddComponent<OpenDestinyView>();

            // bg_img/icon_img/circule_img 无静态皮肤(bg_img 运行时经 SetOutsideImageSprite 换 uisc_002;
            // icon_img/circule_img 业务代码未赋值,留占位色待后续接特效/装饰)。
            Image bgImg = Img("bg_img", root, 0f, -21.5f, 734f, 394f, null, UiCreatorKit.Palette.Panel);

            Image circuleImg = Img("circule_img", root, -298f, -1f, 85f, 85f, null, UiCreatorKit.Palette.Mark);
            circuleImg.gameObject.SetActive(false); // 源 visible:false

            Image iconImg = Img("icon_img", root, 286f, 0.5f, 250f, 250f, null, UiCreatorKit.Palette.BtnPrimary);
            iconImg.gameObject.SetActive(false); // 源 visible:false

            Image iconImg1 = Img("icon_img1", root, 286f, -6f, 233f, 219f, IMG_DESTINY_ICON1, UiCreatorKit.Palette.BtnPrimary);
            iconImg1.gameObject.SetActive(false); // 源 visible:false

            RectTransform effectGp = UiCreatorKit.NewNode("effect_gp", root);
            UiCreatorKit.Place(effectGp, 2.5f, 0.5f, 10f, 10f); // 特效挂点,未移植特效,先留极小占位盒

            Image titleImg = Img("title_img", root, -195f, 101.5f, 200f, 80f, IMG_DESTINY_TITLE, UiCreatorKit.Palette.BtnSecond);
            titleImg.SetNativeSize(); // 源 JSON 未给宽高,贴到真图后按原图尺寸走

            RectTransform detailGp = UiCreatorKit.NewNode("detail_gp", root);
            UiCreatorKit.Place(detailGp, -151f, -4.25f, 185f, 73.5f);
            TextMeshProUGUI label1 = Txt("UnsealNoticeLine1Label", detailGp, 0f, 25.75f, 185f, 22f, "解封后人物等级将改变成神创等级", 22f, "#ffffff"); // 老端: _Label1
            TextMeshProUGUI label2 = Txt("UnsealNoticeLine2Label", detailGp, 78f, -10.25f, 185f, 22f, "后续每次提升神创等级+1", 22f, "#ffffff"); // 老端: _Label2
            TextMeshProUGUI label3 = Txt("CongratsPrefixLabel", detailGp, -5.5f, -48.25f, 100f, 22f, "恭喜你提升至", 22f, "#ffffff"); // 老端: _Label3
            TextMeshProUGUI label4 = Txt("NewGodRankValueLabel", detailGp, 122.5f, -44.25f, 100f, 30f, "神创1级", 30f, "#fff600"); // 老端: _Label4
            detailGp.gameObject.SetActive(false); // 源 visible:false

            TextMeshProUGUI bottomLabel = Txt("bottom_label", root, 0f, -331.5f, 260f, 28f, "点击空白位置关闭", 24f, "#b3ff48");
            bottomLabel.gameObject.SetActive(false); // 源 visible:false

            // 源 JSON 里另有 2 个未命名 Image(ui_info.png / ui_hint.png)，无 name → Bind 生成器没收录，
            // 保留为纯装饰节点(不进任何 view 字段)。
            Image infoIcon = Img("InfoIcon", root, -323.5f, 9.5f, 40f, 40f, IMG_DESTINY_INFO, UiCreatorKit.Palette.BtnSecond);
            infoIcon.SetNativeSize();
            Image hintIcon = Img("HintIcon", root, -81f, -255.5f, 40f, 40f, IMG_DESTINY_HINT, UiCreatorKit.Palette.BtnSecond);
            hintIcon.SetNativeSize();

            // _img_click: 点击关闭捕获层,无视觉皮肤,透明但可点击(纯命中体,真实视觉由 bg_img/title_img 承担)。
            // 功能上要盖全屏(点空白任意处关闭);root 已收成内容占位 → 不能再 Stretch 跟随 root,
            // 改绝对 720×1280 中心锚(设计画布即全屏,同 FlowerEffectView 的处理)。
            Image imgClick = Img("FullScreenCloseCatcher", root, 0f, 0f, UiCreatorKit.DesignWidth, UiCreatorKit.DesignHeight, null, new Color(1f, 1f, 1f, 0f)); // 老端: _img_click
            imgClick.raycastTarget = true;

            view.bg_img = bgImg;
            view.circule_img = circuleImg;
            view.icon_img = iconImg;
            view.icon_img1 = iconImg1;
            view.effect_gp = effectGp;
            view.title_img = titleImg;
            view.detail_gp = detailGp;
            view._Label1 = label1;
            view._Label2 = label2;
            view._Label3 = label3;
            view._Label4 = label4;
            view.bottom_label = bottomLabel;
            view._img_click = imgClick;

            root.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- TalkBoard

        private static void BuildTalkBoard(Transform bundleRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("TalkBoard", bundleRoot);
            UiCreatorKit.Place(root, -250f, 500f, 200f, 60f); // 场景对话气泡模板,运行时由调用方重新定位
            var view = root.gameObject.AddComponent<TalkBoard>();

            RectTransform contentConta = UiCreatorKit.NewNode("content_conta", root);
            UiCreatorKit.Place(contentConta, -100f, 0.5f, 200f, 59f); // 源 x=-100:气泡本体挂在锚点(气泡尖)左侧

            Image contentBg = ImgFull("content_bg", contentConta, IMG_TALK_BOARD, UiCreatorKit.Palette.Panel, sliced: true);

            TextMeshProUGUI content = UiCreatorKit.NewText("content", contentBg.transform, "对话内容");
            UiCreatorKit.StretchPadding(content.rectTransform, 10f, 6f);
            content.fontSize = 20f;
            if (ColorUtility.TryParseHtmlString("#663519", out Color contentColor)) content.color = contentColor;
            content.alignment = TextAlignmentOptions.Left;
            content.textWrappingMode = TextWrappingModes.Normal; // 源 wordWrap:true

            view.content_conta = contentConta;
            view.content_bg = contentBg;
            view.content = content;

            root.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- EyouThAdultItem

        private static void BuildEyouThAdultItem(Transform bundleRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("EyouThAdultItem", bundleRoot);
            UiCreatorKit.Place(root, -300f, -560f, 80f, 70f); // 泰国 18+ 合规角标,固定屏幕角落起步位置
            var view = root.gameObject.AddComponent<EyouThAdultItem>();

            Image boxArrow = Img("AdultWarningPanelBg", root, 0f, 0f, 69f, 56f, IMG_EYOU_BG, UiCreatorKit.Palette.Panel, sliced: true); // 老端: _box_arrow(Bind 字段名不改)

            Image imgRed = Img("AdultWarningTextImage", root, 67.5f, -14f, 271f, 88f, IMG_EYOU_TXT, UiCreatorKit.Palette.BtnPrimary); // 老端: _img_red
            imgRed.rectTransform.localScale = new Vector3(0.7f, 0.7f, 1f);
            imgRed.gameObject.SetActive(false); // 源 visible:false(展开后才显警示文字)

            Image imgIcon = Img("Adult18Icon", root, 0f, 0f, 173f, 140f, IMG_EYOU_18, UiCreatorKit.Palette.BtnSecond); // 老端: _img_icon
            imgIcon.rectTransform.localScale = new Vector3(0.35f, 0.35f, 1f);
            imgIcon.raycastTarget = true; // 业务 OnInit 也会再置一次,双保险

            view._box_arrow = boxArrow;
            view._img_red = imgRed;
            view._img_icon = imgIcon;

            root.gameObject.SetActive(false);
        }

        // =====================================================================================
        // ② ArrowComponent(独立 prefab)
        // =====================================================================================

        public static void GenerateArrowComponent()
        {
            RectTransform root = UiCreatorKit.NewRoot("ArrowComponent");
            root.gameObject.SetActive(false);
            UiCreatorKit.Place(root, 0f, 0f, 435f, 209f);
            var view = root.gameObject.AddComponent<ArrowComponent>();

            RectTransform aniGp = UiCreatorKit.NewNode("aniGp", root);
            UiCreatorKit.Place(aniGp, 0f, 0f, 435f, 209f);

            RectTransform arrowEffect = UiCreatorKit.NewNode("arrow_effect", aniGp);
            UiCreatorKit.Place(arrowEffect, 6.5f, -62.5f, 0f, 0f);
            Image image1 = Img("ArrowHeadImage", arrowEffect, 0f, 5f, 52f, 52f, IMG_ARROW_HEAD, UiCreatorKit.Palette.BtnPrimary); // 老端: _Image1

            RectTransform rectConta = UiCreatorKit.NewNode("rect_conta", aniGp);
            UiCreatorKit.Place(rectConta, 0f, -1.6f, 325f, 92.13f);

            Image contentBg = Img("content_bg", rectConta, -31.5f, 6.1f, 262f, 80f, IMG_ARROW_BG, UiCreatorKit.Palette.Panel, sliced: true);

            RectTransform contentHBox = UiCreatorKit.NewNode("ContentHBox", contentBg.transform);
            UiCreatorKit.Place(contentHBox, -10f, 0f, 200f, 40f);
            TextMeshProUGUI content = UiCreatorKit.NewText("content", contentHBox, "指引文案");
            UiCreatorKit.Place(content.rectTransform, 0f, 0f, 200f, 40f);
            content.fontSize = 22f;
            content.color = Color.white;
            content.richText = true; // 业务 SetData 每次也会重新置 true

            TextMeshProUGUI content3 = Txt("content3", rectConta, -102.5f, 0f, 80f, 26f, "指引精灵", 22f, "#ffffff");
            content3.gameObject.SetActive(false); // 源 visible:false(纯文本备用展示,业务按需显示)

            Image contentImg = Img("contentImg", rectConta, 98.5f, 13.1f, 122f, 104f, IMG_ARROW_CONTENT_IMG, UiCreatorKit.Palette.BtnSecond);

            Image autoImg = Img("autoImg", rectConta, 0f, -54.9f, 230f, 26f, IMG_ARROW_AUTO_BG, UiCreatorKit.Palette.BtnNeutral);
            RectTransform autoLbHBox = UiCreatorKit.NewNode("AutoLbHBox", autoImg.transform);
            UiCreatorKit.Place(autoLbHBox, 0f, 0f, 206f, 23f);
            TextMeshProUGUI autoLb = UiCreatorKit.NewText("autoLb", autoLbHBox, "");
            UiCreatorKit.Place(autoLb.rectTransform, 0f, 0f, 206f, 23f);
            autoLb.fontSize = 18f;
            autoLb.color = Color.white;
            autoLb.richText = true;

            TextMeshProUGUI autoLb2 = Txt("autoLb2", rectConta, 0f, -55.9f, 120f, 24f, "", 20f, "#0a9f42");

            // circle_effect: 源 JSON 有此节点但 ArrowComponentBind 未收录字段(无引用),保留为空占位盒。
            RectTransform circleEffect = UiCreatorKit.NewNode("circle_effect", root);
            UiCreatorKit.Place(circleEffect, 0f, 0f, 10f, 10f);

            view.aniGp = aniGp;
            view.arrow_effect = arrowEffect;
            view._Image1 = image1;
            view.rect_conta = rectConta;
            view.content_bg = contentBg;
            view.content = content;
            view.content3 = content3;
            view.contentImg = contentImg;
            view.autoImg = autoImg;
            view.autoLb = autoLb;
            view.autoLb2 = autoLb2;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, ArrowPrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] ArrowComponent.prefab 已生成: " + ArrowPrefabPath +
                      "(MainUIGuideManager 用 GameResPath.GetUIPrefab(\"mainUI\",\"ArrowComponent\") 按名加载;" +
                      "真机包前记得跑 Addressable 自动分组)");
        }

        // =====================================================================================
        // 建树小工具(UiCreatorKit 没有的,写在本文件里,不改 UiCreatorKit)
        // =====================================================================================

        /// <summary>中心锚起步值摆位的 Image;skin 为 null 时视为「运行时动态换图」,只上占位色。</summary>
        private static Image Img(string name, Transform parent, float cx, float cy, float w, float h,
            string skin, Color fallback, bool sliced = false)
        {
            Image img = UiCreatorKit.NewImage(name, parent);
            UiCreatorKit.Place(img.rectTransform, cx, cy, w, h);
            img.raycastTarget = false;
            if (!string.IsNullOrEmpty(skin)) UiCreatorKit.TrySetSprite(img, skin, fallback);
            else img.color = fallback;
            if (sliced) img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>铺满父节点的 Image(对标源 JSON 的 top/bottom/left/right=0 或纯背景板)。</summary>
        private static Image ImgFull(string name, Transform parent, string skin, Color fallback, bool sliced = false)
        {
            Image img = UiCreatorKit.NewImage(name, parent);
            UiCreatorKit.Stretch(img.rectTransform);
            img.raycastTarget = false;
            if (!string.IsNullOrEmpty(skin)) UiCreatorKit.TrySetSprite(img, skin, fallback);
            else img.color = fallback;
            if (sliced) img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>中心锚起步值摆位的 TMP 文本。</summary>
        private static TextMeshProUGUI Txt(string name, Transform parent, float cx, float cy, float w, float h,
            string text, float fontSize, string hexColor, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = UiCreatorKit.NewText(name, parent, text);
            UiCreatorKit.Place(t.rectTransform, cx, cy, w, h);
            t.fontSize = fontSize;
            if (ColorUtility.TryParseHtmlString(hexColor, out Color c)) t.color = c;
            t.alignment = align;
            return t;
        }

        /// <summary>
        /// 列表项/图标模板的隐藏容器(对标既有 MainUIIconBoxView.prefab / MainUIBuffView.prefab 里
        /// 已经在用的 "__Templates" 约定):独立于真正承载克隆体的容器(_list_buff_con.content /
        /// _gp_item / icon_con),自身默认 inactive,模板挂在它下面。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>
        /// 建一个 ScrollRect(对标 Laya List:Viewport+RectMask2D+Content,Content 挂
        /// VerticalLayoutGroup+ContentSizeFitter 做自动纵向排列/长度,复刻
        /// Assets/Editor/LayaUI/LayaUITemplates.cs 里 "List" 模板同一套结构)。
        /// </summary>
        private static ScrollRect NewScrollList(string name, Transform parent, out RectTransform content, float spacing)
        {
            RectTransform root = UiCreatorKit.NewNode(name, parent);
            ScrollRect sr = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = UiCreatorKit.NewNode("Viewport", root);
            UiCreatorKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UiCreatorKit.NewNode("Content", viewport);
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = viewport;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            return sr;
        }

        /// <summary>
        /// 嵌套实例化一个【已存在】的 prefab(不重建其内部结构)。用于 MainUIIconBoxView 的
        /// _tpl_ActivityIcon:ActivityIcon 是已经完整迁移好的复杂业务组件(首充气泡/特效盒等),
        /// 不在本次任务范围内,复用既有 Assets/Prefabs/UI/MainUI/ActivityIcon.prefab 最安全,
        /// 对标既有 MainUIIconBoxView.prefab 里同款 PrefabInstance 嵌套写法。
        /// </summary>
        private static GameObject InstantiateExisting(string assetPath, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning("[UiCreator] 找不到既有模板 prefab(应已存在,不在本次生成范围内): " + assetPath);
                return null;
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        // =====================================================================================
        // Preview:这 7 个业务类都没有 [UIView] 特性,不能走 ViewManager.Open<T>(Login 样板)。
        // 改为直接从磁盘读最新 prefab + Instantiate + 手动调用业务方法,精神一致(Play 模式检查、
        // 每次都是最新 prefab 的新实例),只是加载方式适配了这里没有寻址特性的事实。
        // =====================================================================================

        public static void PreviewOverlays()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 MainUIPopups",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "这 6 个视图目前没有 [UIView] 特性(不经 ViewManager.Open<T> 单独寻址;捆包已取消)," +
                    "预览改为把 6 个独立 prefab 各自实例化到 Popup 层 + 手动 Show()。",
                    "好");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Popup);
            InstantiateAndShow<MainUIBuffView>(BuffPrefabPath, layer);
            InstantiateAndShow<MainUIFightModeView>(FightModePrefabPath, layer);
            InstantiateAndShow<MainUIIconBoxView>(IconBoxPrefabPath, layer);
            InstantiateAndShow<OpenDestinyView>(OpenDestinyPrefabPath, layer);
            InstantiateAndShow<TalkBoard>(TalkBoardPrefabPath, layer);
            InstantiateAndShow<EyouThAdultItem>(EyouThAdultItemPrefabPath, layer);

            Debug.Log("[UiCreator] MainUIPopups 预览:6 个独立弹窗已各自实例化到 Popup 层并 Show(),用完手动删除这些实例。");
        }

        private static void InstantiateAndShow<T>(string prefabPath, Transform layer) where T : BaseView
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[UiCreator] 预览跳过,尚未生成: " + prefabPath);
                return;
            }
            GameObject go = Object.Instantiate(prefab, layer);
            go.name = typeof(T).Name + "(Preview)";
            T view = go.GetComponentInChildren<T>(true);
            if (view != null) view.Show();
        }

        public static void PreviewArrow()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 ArrowComponent",
                    "请先进入 Play 模式(UI 层已初始化)再点预览。\n\n" +
                    "ArrowComponent 没有 [UIView] 特性(由 MainUIGuideManager 按 prefab 名单独加载," +
                    "不经 ViewManager.Open<T>),预览改为直接实例化最新 prefab + 手动 SetData 一段演示文案。",
                    "好");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("预览 ArrowComponent", "尚未生成:\n" + ArrowPrefabPath, "好");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Popup);
            GameObject go = Object.Instantiate(prefab, layer);
            go.name = "ArrowComponent(Preview)";

            ArrowComponent arrow = go.GetComponent<ArrowComponent>();
            if (arrow == null)
            {
                Debug.LogError("[UiCreator] ArrowComponent.prefab 缺 ArrowComponent 组件(回填?)");
                return;
            }

            arrow.Show();
            arrow.SetData(new ArrowData
            {
                Content = "这是引导箭头预览文案<br/>第二行示例",
                Direction = ArrowComponent.DIR_DOWN,
                AutoCountdown = false,
            });
        }
    }
}
