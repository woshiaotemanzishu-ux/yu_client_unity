using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面【底部区】(聊天条 + 底部导航条)纯代码建树生成器。
    ///
    /// 一个 prefab、两个业务子根,互不依赖,分别贴底摆放:
    ///   MainUIChatView —— 聊天/系统消息双滚动条 + 设置/好友/商城入口 + 变强(ActivityIcon "158")。
    ///     底部贴齐、宽 720(横向铺满)、高 254,对标快照 root (0,1026,720,254)——1026+254=1280,
    ///     正好与设计高 1280 贴合,故用「anchorMin(0,0)/anchorMax(1,0)/pivot(0.5,0)/sizeDelta(0,254)」。
    ///   MainUIDownView —— 经验条 + 5 个功能图标(模板克隆) + 翻面转盘。
    ///     底部贴齐、宽 870(比设计宽 720 左右各溢出 75,对应老端 x=-75 的整体偏移)、水平居中,
    ///     故用「anchorMin=anchorMax=pivot=(0.5,0)/sizeDelta(870,DownH)」。
    ///
    /// 换算与取舍(详见各方法注释,汇总写进生成报告):
    /// - ChatView 子树用标准 PlaceTL(Laya 左上原点→Unity 中心锚,parentW=720/parentH=254);
    ///   DownView 老端是「高度≈0、子节点全部用负 y 向上延展」的贴底容器,y=0 是可视条底边而非顶边,
    ///   不能直接套 PlaceTL,改用 PlaceBottom(见其注释的换算推导)。
    /// - _panel_chat/_panel_sys 字段类型是 ScrollRect(业务代码要读 verticalScrollbar/horizontalScrollbar),
    ///   仍挂真实 ScrollRect 组件满足绑定契约,但只按 Assets/Editor/LayaUI/LayaUITemplates.cs 里
    ///   "Panel" 模板的最小写法(自身即 viewport + Content 子节点 + RectMask2D),不搭老端 vScrollBarSkin
    ///   那套 Viewport/Slider/Scrollbar 按钮子树——HideScrollBars 一进 OnInit 就把滚动条隐藏,搭出来也白搭。
    /// - _tpl_ActivityIcon 现搭建为完整 ActivityIcon 子树(对标 Assets/Editor/UiCreator/MainUI/HudActivityCreator.cs
    ///   的 BuildActivityIconTemplate 同款写法,只换成本区代表图 158),不去引用/嵌套已有的
    ///   Assets/Prefabs/UI/MainUI/ActivityIcon.prefab——保持每个 Region prefab 自包含、便于独立复核,
    ///   与 HudActivityCreator 的既有约定一致。_tpl_MainUIStrongerTalkBoard/_tpl_TalkBoard/_tpl_ArrowComponent
    ///   同 HudActivityCreator 一样不建(跨模块模板字段,当前 ActivityIcon.cs 未接线实例化)。
    /// - MainUIDownTips.json(背包已满提示气泡)在 MainUIDownViewBind/MainUIDownView.cs/MainFuncIconItemBind
    ///   均无引用字段,不并入本区,详见生成日志。
    ///
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudBottomBar.prefab,供人工核对后再并入 MainUIModule.prefab。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   ChatView:
    //     _img_bg(ChatView 根下)   -> ChatBackgroundImage
    //     _panel_chat              -> ChatMessagesScroll
    //     _box_chat_con            -> ChatMessagesContent
    //     _panel_sys               -> SystemMessagesScroll
    //     _box_sys_con             -> SystemMessagesContent
    //     _box_setting             -> SettingEntry
    //     _img_setting             -> SettingIcon
    //     _box_friend              -> FriendEntry
    //     _img_friend              -> FriendIcon
    //     _img_friend_red          -> FriendRedDot
    //     _box_shop                -> ShopEntry
    //     _img_shop                -> ShopIcon
    //     _img_shop_red            -> ShopRedDot
    //     _box_shop_effect         -> ShopDiscountEffectSlot
    //     _box_strengthen          -> StrengthenEntrySlot
    //   MainUIChatItem 模板:
    //     _Group1                  -> LegacyMarkerGroup
    //     _gp_title                -> ChannelBadgeGroup
    //     _gp_content              -> MessageContentGroup
    //     _img_trumpet             -> TrumpetIcon
    //   ActivityIcon 模板(与 HudActivityCreator.cs 保持一致):
    //     _box_effect              -> CompletionEffectSlot
    //     _img_icon                -> IconImage
    //     _box_arrow               -> GuideArrowSlot
    //     _img_red                 -> IconRedDot
    //     _img_desc_bg             -> DescriptionBarBg
    //     _img_red_num             -> RedNumberBadgeBg
    //     _lb_num                  -> RedNumberLabel
    //     _lb_desc                 -> DescriptionLabel
    //   DownView:
    //     _box_con                 -> DownViewContent
    //     _img_bg(DownView 根下)   -> BottomBarBackground
    //     _gp_icon_con             -> FuncIconRow
    //     _gp_turn                 -> TurnButtonSlot
    //     _img_turn                -> FlipButtonImage
    //     _img_red(翻面红点)       -> FlipButtonRedDot
    //     _Group1(DownView 内)     -> HiddenProgressGroup
    //     _Image1                  -> HiddenProgressBarFill
    //     _Image2                  -> HiddenProgressDividerA
    //     _Image3                  -> HiddenProgressDividerB
    //     _Image4                  -> ExpBarTrack
    //     _img_exp                 -> ExpBarFill
    //     _box_exp_effect          -> ExpBarSparkleSlot
    //     _lb_exp                  -> ExpLabel
    //   MainFuncIconItem 模板:
    //     _img_icon                -> FuncIconImage
    //     _img_red                 -> FuncIconRedDot
    // 注:_box_effect2 不改名,原样保留——ActivityIcon.EnsureEffectBox 运行时按此字面量名兜底查找
    // (FindDeep(transform,"_box_effect2")),改名会让该兜底失效。
    public static class HudBottomBarCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudBottomBar.prefab";

        // ---- ChatView 源图(均已确认在 Assets/GameRes 下) ----
        private const string IMG_CHAT_BG = "resource/game/mainUI/other/uizjmv3_003.png"; // sizeGrid 10,10,10,10,0(九宫格,见报告)
        private const string IMG_SETTING = "resource/game/mainUI/texture/mainui_set_icon.png";
        private const string IMG_FRIEND = "resource/game/mainUI/texture/mainui_friend_icon.png";
        private const string IMG_RED_DOT_MAINUI = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_RED_DOT_COMMON = "resource/game/common/texture/com_red_point.png";
        private const string IMG_SHOP = "resource/game/icon/texture/22.png";
        private const string IMG_CHAT_WELCOME_ICON = "resource/game/mainUI/texture/mainUI_chat_1.png"; // 系统频道图标(欢迎语),对标 CreateWelcomeSystemMessage
        private const string IMG_ACTIVITY_SAMPLE = "resource/game/icon/texture/158.png"; // 变强 ActivityIcon 恒定 icon_type=158,直接用真实代表图

        // ActivityIcon 内部子结构源图(与 HudActivityCreator 一致)
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_DESC_BG = "resource/game/mainUI/texture/mainui_ui_40.png";
        private const string IMG_RED_NUM_BG = "resource/game/mainUI/texture/ui_bq_03.png";
        private const string IMG_TIP_BG = "resource/game/mainUI/texture/ui_First_16.png";

        // ---- DownView 源图 ----
        // 注:bag/pet/equip/treasure 四张功能图标(均已确认存在于 Assets/GameRes)由业务代码
        // MainUIDownView.BuildFuncIcons 在运行时按 MainUIModel.MainFuncIcons 配置逐个 ResManager.SetImageAsync
        // 换上,模板本身只贴一张代表图(IMG_ROLE),故这里不重复声明常量。
        private const string IMG_DOWN_BG = "resource/game/mainUI/other/uizjmv3_001.png";
        private const string IMG_ROLE = "resource/game/mainUI/texture/role.png";
        private const string IMG_TURN = "resource/game/mainUI/texture/uizjmv3_015.png";
        private const string IMG_PRO_BAR = "resource/game/mainUI/texture/ui_pro_bar_1.png";
        private const string IMG_LINE = "resource/game/mainUI/texture/line.png";
        private const string IMG_EXP = "resource/game/mainUI/texture/exp.png"; // sizeGrid 3,5,3,5(九宫格,见报告)

        // 设计尺寸(对标快照:ChatView root 720x254 贴屏幕底;DownView 870 宽溢出屏幕左右各 75)
        private const float ChatW = 720f, ChatH = 254f;
        private const float DownW = 870f, DownH = 150f; // DownH 是本生成器为「负 y 向上延展」内容选的换算容器高,见 PlaceBottom 注释

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudBottomBar(聊天+底部导航)",
                Note = "MainUIChatView(聊天/系统消息+设置/好友/商城/变强) + MainUIDownView(经验条+功能图标+翻面),两子根各自贴底",
                Order = 50,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再统一激活(与 Login 系列 / HudActivityCreator 一致的安全写法)。
            RectTransform root = UiCreatorKit.NewRoot("HudBottomBar");
            root.gameObject.SetActive(false);

            BuildChatView(root);
            BuildDownView(root);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudBottomBar.prefab 已生成: " + PrefabPath +
                      "(主界面底部区,人工核对后再并入 MainUIModule.prefab)");
        }

        // ================================================================ ChatView

        private static void BuildChatView(Transform parent)
        {
            RectTransform chatRoot = UiCreatorKit.NewNode("MainUIChatView", parent);
            AnchorBottomStretch(chatRoot, ChatH);
            var view = chatRoot.gameObject.AddComponent<MainUIChatView>();

            // 隐藏模板挂载点:聊天项模板 / 变强 ActivityIcon 模板都是纯克隆源,不应裸露在可见容器里。
            RectTransform templates = NewTemplatesWrapper(chatRoot);

            // 全宽底图(ChatBackgroundImage,对标 _img_bg,老端点这块区域会 Fire OPEN_CHAT_VIEW,故开 raycastTarget)
            Image bg = UiCreatorKit.NewImage("ChatBackgroundImage", chatRoot); // 老端: _img_bg
            PlaceTL(bg.rectTransform, 0f, 0f, ChatW, ChatH, ChatW, ChatH);
            bg.raycastTarget = true;
            UiCreatorKit.TrySetSprite(bg, IMG_CHAT_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = bg;

            // 聊天频道滚动区(世界/结社等,当前业务不造假消息,容器建好即可)
            view._panel_chat = BuildMessageScroll("ChatMessagesScroll", "ChatMessagesContent", chatRoot,
                148f, 3f, 396f, 65f, out RectTransform chatCon); // 老端: _panel_chat / _box_chat_con
            view._box_chat_con = chatCon;

            // 系统消息滚动区(欢迎语走这里,唯一实装的静态内容)
            view._panel_sys = BuildMessageScroll("SystemMessagesScroll", "SystemMessagesContent", chatRoot,
                148f, 83f, 397f, 65f, out RectTransform sysCon); // 老端: _panel_sys / _box_sys_con
            view._box_sys_con = sysCon;
            view._tpl_MainUIChatItem = BuildChatItemTemplate(templates);

            // 设置入口(SettingEntry)
            RectTransform boxSetting = UiCreatorKit.NewNode("SettingEntry", chatRoot); // 老端: _box_setting
            PlaceTL(boxSetting, 4f, 25f, 64f, 64f, ChatW, ChatH);
            view._box_setting = boxSetting;

            Image imgSetting = UiCreatorKit.NewImage("SettingIcon", boxSetting); // 老端: _img_setting
            PlaceTL(imgSetting.rectTransform, 5f, -17f, 55f, 63f, 64f, 64f);
            UiCreatorKit.TrySetSprite(imgSetting, IMG_SETTING, UiCreatorKit.Palette.BtnNeutral);
            view._img_setting = imgSetting;

            // 好友入口(FriendEntry,+ 红点,老端声明 visible=false,数据到达前不现身)
            RectTransform boxFriend = UiCreatorKit.NewNode("FriendEntry", chatRoot); // 老端: _box_friend
            PlaceTL(boxFriend, 72f, 22f, 64f, 64f, ChatW, ChatH);
            view._box_friend = boxFriend;

            Image imgFriend = UiCreatorKit.NewImage("FriendIcon", boxFriend); // 老端: _img_friend
            PlaceTL(imgFriend.rectTransform, 5f, -15f, 55f, 64f, 64f, 64f);
            UiCreatorKit.TrySetSprite(imgFriend, IMG_FRIEND, UiCreatorKit.Palette.BtnNeutral);
            view._img_friend = imgFriend;

            Image imgFriendRed = UiCreatorKit.NewImage("FriendRedDot", boxFriend); // 老端: _img_friend_red
            PlaceTL(imgFriendRed.rectTransform, 44f, -20f, 23f, 23f, 64f, 64f);
            UiCreatorKit.TrySetSprite(imgFriendRed, IMG_RED_DOT_MAINUI, UiCreatorKit.Palette.Mark);
            imgFriendRed.gameObject.SetActive(false); // 老端场景声明 visible=false
            view._img_friend_red = imgFriendRed;

            // 商城入口(ShopEntry,+ 红点,同上默认隐藏)
            RectTransform boxShop = UiCreatorKit.NewNode("ShopEntry", chatRoot); // 老端: _box_shop
            PlaceTL(boxShop, 550f, 33f, 64f, 64f, ChatW, ChatH);
            view._box_shop = boxShop;

            Image imgShop = UiCreatorKit.NewImage("ShopIcon", boxShop); // 老端: _img_shop
            PlaceTL(imgShop.rectTransform, 10f, -13f, 55f, 65f, 64f, 64f);
            UiCreatorKit.TrySetSprite(imgShop, IMG_SHOP, UiCreatorKit.Palette.BtnSecond);
            view._img_shop = imgShop;

            Image imgShopRed = UiCreatorKit.NewImage("ShopRedDot", boxShop); // 老端: _img_shop_red
            PlaceTL(imgShopRed.rectTransform, 44f, -20f, 23f, 23f, 64f, 64f);
            UiCreatorKit.TrySetSprite(imgShopRed, IMG_RED_DOT_MAINUI, UiCreatorKit.Palette.Mark);
            imgShopRed.gameObject.SetActive(false); // 老端场景声明 visible=false
            view._img_shop_red = imgShopRed;

            // 限购商城特效盒(ShopDiscountEffectSlot,纯挂点,老端 mouseThrough=true 不接点击;HideUnbackedIndicators 会整体隐藏)
            RectTransform boxShopEffect = UiCreatorKit.NewNode("ShopDiscountEffectSlot", chatRoot); // 老端: _box_shop_effect
            PlaceTL(boxShopEffect, 620f, 15f, 100f, 100f, ChatW, ChatH);
            view._box_shop_effect = boxShopEffect;

            // 变强(StrengthenEntrySlot,ActivityIcon "158" 挂载点)
            RectTransform boxStrengthen = UiCreatorKit.NewNode("StrengthenEntrySlot", chatRoot); // 老端: _box_strengthen
            PlaceTL(boxStrengthen, 634f, 15f, 55f, 55f, ChatW, ChatH);
            view._box_strengthen = boxStrengthen;
            view._tpl_ActivityIcon = BuildActivityIconTemplate(templates);
        }

        /// <summary>
        /// 建聊天/系统消息滚动条:真实 ScrollRect(满足 Bind 字段类型 + 业务代码读 verticalScrollbar)
        /// + RectMask2D 裁剪 + Content 容器(VerticalLayoutGroup 还原老端 VBox 纵向堆叠语义)。
        /// 对标 Assets/Editor/LayaUI/LayaUITemplates.cs 的 "Panel" 模板最小写法(自身即 viewport),
        /// 不搭老端 vScrollBarSkin 驱动出的 Viewport/Slider/Scrollbar 按钮子树——HideScrollBars 一进
        /// OnInit 就把滚动条隐藏,搭出来也是白搭(对应任务里「不必上老那套滚动条 UI」的取舍,详见生成报告)。
        /// </summary>
        private static ScrollRect BuildMessageScroll(string panelName, string contentName, Transform parent,
            float x, float y, float w, float h, out RectTransform content)
        {
            RectTransform panel = UiCreatorKit.NewNode(panelName, parent);
            PlaceTL(panel, x, y, w, h, ChatW, ChatH);
            panel.gameObject.AddComponent<RectMask2D>();

            RectTransform con = UiCreatorKit.NewNode(contentName, panel);
            con.anchorMin = new Vector2(0f, 1f);
            con.anchorMax = new Vector2(0f, 1f);
            con.pivot = new Vector2(0f, 1f);
            con.sizeDelta = new Vector2(w, h);
            con.anchoredPosition = Vector2.zero;

            var vlayout = con.gameObject.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.childControlWidth = true;
            vlayout.childControlHeight = false;
            vlayout.spacing = 2f;

            var fitter = con.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
            // viewport 留空 = 用自身 RectTransform 当视口(对标 LayaUITemplates.Panel 的自视口简化)。
            scroll.content = con;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            content = con;
            return scroll;
        }

        /// <summary>
        /// 建 MainUIChatItem 模板(对标 MainUIChatItem.json + MainUIChatItemBind),挂业务类,建完即禁用。
        /// 只有一份共享模板(_tpl_MainUIChatItem),CreateWelcomeSystemMessage 用它 Instantiate 克隆到 _box_sys_con;
        /// 频道聊天(_box_chat_con)依赖未移植的 ChatModel,当前不生成静态消息,模板留空占位即可。
        /// </summary>
        private static GameObject BuildChatItemTemplate(Transform parent)
        {
            const float itemW = 390f, itemH = 50f;
            RectTransform item = UiCreatorKit.NewNode("MainUIChatItem", parent);
            PlaceTL(item, 0f, 0f, itemW, itemH, itemW, itemH);
            var view = item.gameObject.AddComponent<MainUIChatItem>();

            // LegacyMarkerGroup:老端空标记组,零尺寸,只占层级位不出图形
            RectTransform group1 = UiCreatorKit.NewNode("LegacyMarkerGroup", item); // 老端: _Group1
            PlaceTL(group1, 0f, 0f, 0f, 0f, itemW, itemH);
            view._Group1 = group1;

            RectTransform gpTitle = UiCreatorKit.NewNode("ChannelBadgeGroup", item); // 老端: _gp_title
            PlaceTL(gpTitle, 0f, 0f, 40f, 22f, itemW, itemH);
            view._gp_title = gpTitle;

            Image titleBg = UiCreatorKit.NewImage("titleBg", gpTitle);
            PlaceTL(titleBg.rectTransform, 0f, 0f, 41f, 22f, 40f, 22f);
            titleBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(titleBg, IMG_CHAT_WELCOME_ICON, UiCreatorKit.Palette.Panel);
            view.titleBg = titleBg;

            TextMeshProUGUI title = UiCreatorKit.NewText("title", gpTitle, "系统");
            PlaceTL(title.rectTransform, 2f, 3f, 72f, 36f, 40f, 22f);
            title.fontSize = 18f;
            title.gameObject.SetActive(false); // 老端场景声明 visible=false,有频道名才显示(SetData 控制)
            view.title = title;

            RectTransform gpContent = UiCreatorKit.NewNode("MessageContentGroup", item); // 老端: _gp_content
            PlaceTL(gpContent, 4f, 0f, 381f, 51f, itemW, itemH);
            view._gp_content = gpContent;

            TextMeshProUGUI content = UiCreatorKit.NewText("contentLabel", gpContent, "欢迎踏入九州大荒。");
            UiCreatorKit.Stretch(content.rectTransform);
            content.fontSize = 20f;
            content.alignment = TextAlignmentOptions.Left;
            content.raycastTarget = false;
            view.contentLabel = content;

            // TrumpetIcon:喇叭特殊态未移植,老端场景声明 visible=false,业务代码也恒定隐藏
            Image trumpet = UiCreatorKit.NewImage("TrumpetIcon", item); // 老端: _img_trumpet
            PlaceTL(trumpet.rectTransform, 50f, 0f, 28f, 27f, itemW, itemH);
            trumpet.raycastTarget = false;
            trumpet.gameObject.SetActive(false);
            view._img_trumpet = trumpet;

            item.gameObject.SetActive(false); // 模板默认隐藏,克隆后由业务代码 SetActive(true)
            return item.gameObject;
        }

        /// <summary>
        /// 建 ActivityIcon 模板(对标 ActivityIcon.json + ActivityIconBind),挂业务类。
        /// 写法与 Assets/Editor/UiCreator/MainUI/HudActivityCreator.cs 的 BuildActivityIconTemplate 一致
        /// (保持每个 Region prefab 自包含,不跨 prefab 嵌套引用),仅换成本区固定的 158(变强)代表图。
        /// _tpl_MainUIStrongerTalkBoard/_tpl_TalkBoard/_tpl_ArrowComponent 同 HudActivityCreator 一样不建——
        /// 分别是变强气泡/通用对话气泡/引导箭头的模板字段,当前 ActivityIcon.cs 未接线实例化,跨模块、超出本区范围。
        /// </summary>
        private static GameObject BuildActivityIconTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("ActivityIcon", parent);
            PlaceTL(item, 0f, 0f, 72f, 72f, 55f, 55f); // box_strengthen 是 55x55 命中盒,图标本体 72x72,老端本就溢出
            ActivityIcon icon = item.gameObject.AddComponent<ActivityIcon>();

            RectTransform boxEffect = UiCreatorKit.NewNode("CompletionEffectSlot", item); // 老端: _box_effect
            PlaceTL(boxEffect, 0f, 0f, 72f, 85f, 72f, 72f);
            icon._box_effect = boxEffect;

            Image imgIcon = UiCreatorKit.NewImage("IconImage", item); // 老端: _img_icon
            PlaceTL(imgIcon.rectTransform, 0f, 0f, 72f, 72f, 72f, 72f);
            imgIcon.raycastTarget = true;
            UiCreatorKit.TrySetSprite(imgIcon, IMG_ACTIVITY_SAMPLE, UiCreatorKit.Palette.BtnPrimary);
            icon._img_icon = imgIcon;

            Image boxArrow = UiCreatorKit.NewImage("GuideArrowSlot", item); // 老端: _box_arrow
            PlaceTL(boxArrow.rectTransform, 0f, 0f, 72f, 72f, 72f, 72f);
            boxArrow.color = new Color(1f, 1f, 1f, 0f);
            boxArrow.raycastTarget = false;
            icon._box_arrow = boxArrow;

            Image imgRed = UiCreatorKit.NewImage("IconRedDot", item); // 老端: _img_red
            PlaceTL(imgRed.rectTransform, 50f, -5f, 28f, 29f, 72f, 72f);
            imgRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            imgRed.gameObject.SetActive(false);
            icon._img_red = imgRed;

            Image imgDescBg = UiCreatorKit.NewImage("DescriptionBarBg", item); // 老端: _img_desc_bg
            PlaceTL(imgDescBg.rectTransform, 0f, 71f, 72f, 18f, 72f, 72f);
            imgDescBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgDescBg, IMG_DESC_BG, UiCreatorKit.Palette.Panel);
            imgDescBg.gameObject.SetActive(false);
            icon._img_desc_bg = imgDescBg;

            Image imgRedNum = UiCreatorKit.NewImage("RedNumberBadgeBg", item); // 老端: _img_red_num
            PlaceTL(imgRedNum.rectTransform, 49f, -8f, 32f, 32f, 72f, 72f);
            imgRedNum.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRedNum, IMG_RED_NUM_BG, UiCreatorKit.Palette.Mark);
            imgRedNum.gameObject.SetActive(false);
            icon._img_red_num = imgRedNum;

            TextMeshProUGUI lbNum = UiCreatorKit.NewText("RedNumberLabel", item, "3"); // 老端: _lb_num
            PlaceTL(lbNum.rectTransform, 65f, -1f, 30f, 18f, 72f, 72f);
            lbNum.fontSize = 18f;
            lbNum.gameObject.SetActive(false);
            icon._lb_num = lbNum;

            TextMeshProUGUI lbDesc = UiCreatorKit.NewText("DescriptionLabel", item, ""); // 老端: _lb_desc
            PlaceTL(lbDesc.rectTransform, 36f, 73f, 180f, 32f, 72f, 72f);
            lbDesc.fontSize = 16f;
            lbDesc.gameObject.SetActive(false);
            icon._lb_desc = lbDesc;

            // 白名单不改名:ActivityIcon.EnsureEffectBox 运行时用 FindDeep(transform,"_box_effect2") 按此字面量名兜底查找
            // (烤制会裁掉 inactive 节点导致克隆体字段为 null),改名会让这条兜底路径失效、丢失环特效。
            RectTransform boxEffect2 = UiCreatorKit.NewNode("_box_effect2", item);
            PlaceTL(boxEffect2, -14f, -14f, 100f, 100f, 72f, 72f);
            icon._box_effect2 = boxEffect2;

            // _tpl_TopPlayerTipItem:跟 HudActivityCreator 一样,按 Bind 契约先建好(当前未接线实例化,占位备用)。
            GameObject tplTip = BuildTopPlayerTipItemTemplate(item);
            UiCreatorKit.Place((RectTransform)tplTip.transform, 50f, -15f, 276f, 66f);
            tplTip.SetActive(false);
            icon._tpl_TopPlayerTipItem = tplTip;

            return item.gameObject;
        }

        /// <summary>建 TopPlayerTipItem 模板(对标 TopPlayerTipItem.json:276x66,bg 铺满 + des 文案),挂业务类。</summary>
        private static GameObject BuildTopPlayerTipItemTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("TopPlayerTipItem", parent);
            UiCreatorKit.Place(item, 0f, 0f, 276f, 66f);
            TopPlayerTipItem tip = item.gameObject.AddComponent<TopPlayerTipItem>();

            Image bg = UiCreatorKit.NewImage("bg", item);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_TIP_BG, UiCreatorKit.Palette.Panel);
            tip.bg = bg;

            TextMeshProUGUI des = UiCreatorKit.NewText("des", item, "提示文案");
            PlaceTL(des.rectTransform, 32f, 22f, 200f, 32f, 276f, 66f);
            des.alignment = TextAlignmentOptions.Left;
            des.fontSize = 20f;
            tip.des = des;

            return item.gameObject;
        }

        // ================================================================ DownView

        private static void BuildDownView(Transform parent)
        {
            RectTransform downRoot = UiCreatorKit.NewNode("MainUIDownView", parent);
            AnchorBottomCenter(downRoot, DownW, DownH);
            var view = downRoot.gameObject.AddComponent<MainUIDownView>();

            // 隐藏模板挂载点:功能图标模板是纯克隆源,不应裸露在可见的 FuncIconRow 容器里。
            RectTransform templates = NewTemplatesWrapper(downRoot);

            RectTransform boxCon = UiCreatorKit.NewNode("DownViewContent", downRoot); // 老端: _box_con
            UiCreatorKit.Stretch(boxCon); // box_con 与 downRoot 同尺寸同中心,子节点仍可用 PlaceBottom(DownW,DownH) 换算
            view._box_con = boxCon;

            Image imgBg = UiCreatorKit.NewImage("BottomBarBackground", boxCon); // 老端: _img_bg
            PlaceBottom(imgBg.rectTransform, 75f, -144f, 720f, 140f);
            imgBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgBg, IMG_DOWN_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = imgBg;

            RectTransform gpIconCon = UiCreatorKit.NewNode("FuncIconRow", boxCon); // 老端: _gp_icon_con
            PlaceBottom(gpIconCon, 135f, -110f, 545f, 104f);
            view._gp_icon_con = gpIconCon;
            view._tpl_MainFuncIconItem = BuildFuncIconTemplate(templates);

            RectTransform gpTurn = UiCreatorKit.NewNode("TurnButtonSlot", boxCon); // 老端: _gp_turn
            PlaceBottom(gpTurn, 676f, -127f, 115f, 115f);
            view._gp_turn = gpTurn;

            Image imgTurn = UiCreatorKit.NewImage("FlipButtonImage", gpTurn); // 老端: _img_turn
            PlaceTL(imgTurn.rectTransform, 0f, 0f, 115f, 115f, 115f, 115f);
            UiCreatorKit.TrySetSprite(imgTurn, IMG_TURN, UiCreatorKit.Palette.BtnSecond);
            view._img_turn = imgTurn;

            Image turnRed = UiCreatorKit.NewImage("FlipButtonRedDot", gpTurn); // 老端: _img_red
            PlaceTL(turnRed.rectTransform, 87f, 0f, 28f, 29f, 115f, 115f);
            UiCreatorKit.TrySetSprite(turnRed, IMG_RED_DOT_MAINUI, UiCreatorKit.Palette.Mark);
            view._img_red = turnRed; // 老端场景默认 visible=true;HideUnbackedIndicators 会在 OnInit 立即隐藏

            // HiddenProgressGroup:老端场景声明 visible=false 的旧进度条变体(带两条分隔线),恒定隐藏、无代码引用。
            RectTransform group1 = UiCreatorKit.NewNode("HiddenProgressGroup", boxCon); // 老端: _Group1
            PlaceBottom(group1, 73f, -10f, 603f, 11f);
            group1.gameObject.SetActive(false); // 老端场景声明 visible=false
            view._Group1 = group1;

            Image image1 = UiCreatorKit.NewImage("HiddenProgressBarFill", group1); // 老端: _Image1
            PlaceTL(image1.rectTransform, -58f, 0f, 720f, 12f, 603f, 11f);
            UiCreatorKit.TrySetSprite(image1, IMG_PRO_BAR, UiCreatorKit.Palette.Panel);
            view._Image1 = image1;

            Image image2 = UiCreatorKit.NewImage("HiddenProgressDividerA", group1); // 老端: _Image2
            PlaceTL(image2.rectTransform, 287f, 0f, 146f, 11f, 603f, 11f);
            UiCreatorKit.TrySetSprite(image2, IMG_LINE, UiCreatorKit.Palette.Panel);
            view._Image2 = image2;

            Image image3 = UiCreatorKit.NewImage("HiddenProgressDividerB", group1); // 老端: _Image3
            PlaceTL(image3.rectTransform, 503f, 0f, 146f, 11f, 603f, 11f);
            UiCreatorKit.TrySetSprite(image3, IMG_LINE, UiCreatorKit.Palette.Panel);
            view._Image3 = image3;

            // ExpBarTrack:经验条背景轨道(常显),真正的经验填充在下方 ExpBarFill(_img_exp)上叠加。
            Image image4 = UiCreatorKit.NewImage("ExpBarTrack", boxCon); // 老端: _Image4
            PlaceBottom(image4.rectTransform, 75f, -12f, 720f, 12f);
            UiCreatorKit.TrySetSprite(image4, IMG_PRO_BAR, UiCreatorKit.Palette.Panel);
            view._Image4 = image4;

            // ExpBarFill:经验条:起步按满宽贴(722,对标 EXP_BAR_MAX_WIDTH),OnInit 的 RefreshExp() 会立即按真实 Exp/ExpLim 改宽。
            Image imgExp = UiCreatorKit.NewImage("ExpBarFill", downRoot); // 老端: _img_exp
            PlaceBottom(imgExp.rectTransform, 73f, -12f, 722f, 12f);
            UiCreatorKit.TrySetSprite(imgExp, IMG_EXP, UiCreatorKit.Palette.BtnPrimary);
            view._img_exp = imgExp;

            // ExpBarSparkleSlot:经验条闪光特效挂点,老端用 anchorX=1+right 钉在经验条右端(不随经验条宽度变化),
            // 换算成 Unity 右中锚定,近似复刻(纯特效挂点,像素级位置不影响业务逻辑)。
            RectTransform boxExpEffect = UiCreatorKit.NewNode("ExpBarSparkleSlot", imgExp.transform); // 老端: _box_exp_effect
            PlaceRightMiddle(boxExpEffect, -25f, 50f, 50f);
            view._box_exp_effect = boxExpEffect;

            // ExpLabel:快照里 _lb_exp 的 runtime x=254 疑似设备相关 centerX 布局换算异常(870 宽容器里 724 宽文本框
            // 居中该落在 x≈73,而非 254);改用与 _img_exp 左对齐的设计值 x=73,换算后 cx=0 正好居中,
            // 与经验条视觉对齐,判定更符合设计意图,详见生成报告。
            TextMeshProUGUI lbExp = UiCreatorKit.NewText("ExpLabel", downRoot, "0 / 0"); // 老端: _lb_exp
            PlaceBottom(lbExp.rectTransform, 73f, -12f, 724f, 24f);
            lbExp.fontSize = 20f;
            view._lb_exp = lbExp;
        }

        /// <summary>
        /// _tpl_MainFuncIconItem:单个功能图标模板(默认隐藏),运行时 BuildFuncIcons 按当前开放的功能数
        /// 从这一份模板克隆铺开(105 像素间距),故这里只建一份,不是快照里看到的 5 个运行时克隆实例。
        /// </summary>
        private static GameObject BuildFuncIconTemplate(Transform parent)
        {
            RectTransform item = UiCreatorKit.NewNode("MainFuncIconItem", parent);
            PlaceTL(item, 0f, 0f, 100f, 100f, 545f, 104f); // 起步落在第一格位置,克隆后由 BuildFuncIcons 重新摆位
            var view = item.gameObject.AddComponent<MainFuncIconItem>();

            Image icon = UiCreatorKit.NewImage("FuncIconImage", item); // 老端: _img_icon
            PlaceTL(icon.rectTransform, 8f, 6f, 84f, 88f, 100f, 100f);
            UiCreatorKit.TrySetSprite(icon, IMG_ROLE, UiCreatorKit.Palette.BtnSecond); // 起步贴"角色"代表图,运行时按配置换(role/bag/pet/equip/treasure)
            view._img_icon = icon;

            Image red = UiCreatorKit.NewImage("FuncIconRedDot", item); // 老端: _img_red
            PlaceTL(red.rectTransform, 72f, 0f, 24f, 24f, 100f, 100f);
            UiCreatorKit.TrySetSprite(red, IMG_RED_DOT_COMMON, UiCreatorKit.Palette.Mark);
            red.gameObject.SetActive(false); // SetData 里 SetRedState(false),红点系统未移植
            view._img_red = red;

            item.gameObject.SetActive(false); // 模板默认隐藏
            return item.gameObject;
        }

        // ================================================================ 布局换算 helper(本文件专用)

        /// <summary>
        /// 建一个隐藏的模板挂载容器(__Templates):专收纯克隆源(MainUIChatItem/ActivityIcon/MainFuncIconItem 等模板),
        /// 不让它们裸露在可见的业务容器(ChatMessagesContent/StrengthenEntrySlot/FuncIconRow 等)里当"不可见的子节点"。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>贴底、横向铺满(ChatView 用:宽度由 Stretch 撑满父级,高度固定)。</summary>
        private static void AnchorBottomStretch(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>贴底、水平居中、固定宽高(DownView 用:870 比屏宽 720 左右各溢出 75)。</summary>
        private static void AnchorBottomCenter(RectTransform rt, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Laya 左上原点 (x,y,w,h) → Unity 中心锚 anchoredPosition 换算(对标 HudActivityCreator 的 PlaceTL 约定):
        /// cx = x + w/2 - parentW/2,cy = -(y + h/2 - parentH/2)。用于「y=0 在顶」的常规容器(ChatView 及其子树)。
        /// </summary>
        private static void PlaceTL(RectTransform rt, float x, float y, float w, float h, float parentW, float parentH)
        {
            float cx = x + w * 0.5f - parentW * 0.5f;
            float cy = -(y + h * 0.5f - parentH * 0.5f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>
        /// DownView 专用换算:老端该 view 高度≈0/1、子节点全部用【负 y 向上延展】贴底
        /// (y=0 是可视条底边=屏幕底边,不是常规「y=0 在顶」的容器),不能直接用 PlaceTL。
        /// 等价处理:先把 y 平移 +DownH(换成「0..DownH 从上到下」的常规框架)再套用同一套公式,
        /// 化简得 cy = -(y + h/2 + DownH/2);cx 不受影响,仍是 x + w/2 - DownW/2。
        /// 用于 DownView 及其挂在 _box_con/downRoot 同一坐标系下的子节点。
        /// </summary>
        private static void PlaceBottom(RectTransform rt, float x, float y, float w, float h)
        {
            float cx = x + w * 0.5f - DownW * 0.5f;
            float cy = -(y + h * 0.5f + DownH * 0.5f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        /// <summary>右中锚定(老端 _box_exp_effect 用 anchorX=1+right 钉在经验条右端,不随经验条宽度变化)。</summary>
        private static void PlaceRightMiddle(RectTransform rt, float offsetX, float w, float h)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(offsetX, 0f);
        }

        // ================================================================ 预览

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudBottomBar",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudBottomBar 是并入 MainUIModule 的区域子视图(内含 MainUIChatView + MainUIDownView 两个" +
                    "BaseView),不走 ViewManager.Open<T>()(两个 Bind 类都没有 [UIView] 地址特性);" +
                    "预览直接把最新 prefab 实例化到 Window 层,并对里面每个 BaseView 手动调用 Show(),仅用于看结构/试交互。",
                    "好");
                return;
            }

            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiCreator] 找不到 " + PrefabPath + ",请先点生成");
                return;
            }

            Transform parentLayer = ViewManager.GetLayer(UILayer.Window);
            _previewInstance = Object.Instantiate(prefab, parentLayer);
            BaseView[] views = _previewInstance.GetComponentsInChildren<BaseView>(true);
            if (views.Length == 0)
            {
                Debug.LogError("[UiCreator] HudBottomBar 预览实例找不到任何 BaseView");
                return;
            }
            foreach (BaseView v in views)
            {
                v.Show();
            }
        }
    }
}
