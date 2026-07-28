using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 从 HudBottomBarCreator 拆出的聊天条区域;root 收成实际占位(底部贴齐、横向铺满、高 254);
    /// 布局数值全部来自运行时快照,拆分未改动。
    ///
    ///   MainUIChatView —— 聊天/系统消息双滚动条 + 设置/好友/商城入口 + 变强(ActivityIcon "158")。
    ///     底部贴齐、宽 720(横向铺满)、高 254,对标快照 root (0,1026,720,254)——1026+254=1280,
    ///     正好与设计高 1280 贴合,故用「anchorMin(0,0)/anchorMax(1,0)/pivot(0.5,0)/sizeDelta(0,254)」
    ///     (拆分后这组锚定收在区域 root 上,视图子根 Stretch 填满 root)。
    ///
    /// 换算与取舍(详见各方法注释):
    /// - ChatView 子树用标准 PlaceTL(Laya 左上原点→Unity 中心锚,parentW=720/parentH=254)。
    /// - _panel_chat/_panel_sys 字段类型是 ScrollRect(业务代码要读 verticalScrollbar/horizontalScrollbar),
    ///   仍挂真实 ScrollRect 组件满足绑定契约,但只按 Assets/Editor/LayaUI/LayaUITemplates.cs 里
    ///   "Panel" 模板的最小写法(自身即 viewport + Content 子节点 + RectMask2D),不搭老端 vScrollBarSkin
    ///   那套 Viewport/Slider/Scrollbar 按钮子树——HideScrollBars 一进 OnInit 就把滚动条隐藏,搭出来也白搭。
    /// - _tpl_ActivityIcon 现搭建为完整 ActivityIcon 子树(对标 Assets/Editor/UiCreator/MainUI/HudActivityCreator.cs
    ///   的 BuildActivityIconTemplate 同款写法,只换成本区代表图 158),不去引用/嵌套已有的
    ///   Assets/Prefabs/UI/MainUI/ActivityIcon.prefab——保持每个 Region prefab 自包含、便于独立复核,
    ///   与 HudActivityCreator 的既有约定一致。_tpl_MainUIStrongerTalkBoard/_tpl_TalkBoard/_tpl_ArrowComponent
    ///   同 HudActivityCreator 一样不建(跨模块模板字段,当前 ActivityIcon.cs 未接线实例化)。
    ///
    /// 存 Assets/Prefabs/UI/MainUI/Regions/HudChatBar.prefab,供人工核对后再并入 MainUIModule.prefab。
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
    // 注:_box_effect2 不改名,原样保留——ActivityIcon.EnsureEffectBox 运行时按此字面量名兜底查找
    // (FindDeep(transform,"_box_effect2")),改名会让该兜底失效。
    public static class HudChatBarCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudChatBar.prefab";

        // ---- ChatView 源图(均已确认在 Assets/GameRes 下) ----
        private const string IMG_CHAT_BG = "resource/game/mainUI/other/uizjmv3_003.png"; // sizeGrid 10,10,10,10,0(九宫格,见报告)
        private const string IMG_SETTING = "resource/game/mainUI/texture/mainui_set_icon.png";
        private const string IMG_FRIEND = "resource/game/mainUI/texture/mainui_friend_icon.png";
        private const string IMG_RED_DOT_MAINUI = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_SHOP = "resource/game/icon/texture/22.png";
        private const string IMG_CHAT_WELCOME_ICON = "resource/game/mainUI/texture/mainUI_chat_1.png"; // 系统频道图标(欢迎语),对标 ChatModel.EnsureWelcomeSystemMessage
        private const string IMG_ACTIVITY_SAMPLE = "resource/game/icon/texture/158.png"; // 变强 ActivityIcon 恒定 icon_type=158,直接用真实代表图

        // ActivityIcon 内部子结构源图(与 HudActivityCreator 一致)
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_DESC_BG = "resource/game/mainUI/texture/mainui_ui_40.png";
        private const string IMG_RED_NUM_BG = "resource/game/mainUI/texture/ui_bq_03.png";
        private const string IMG_TIP_BG = "resource/game/mainUI/texture/ui_First_16.png";

        // 设计尺寸(对标快照:ChatView root 720x254 贴屏幕底)
        private const float ChatW = 720f, ChatH = 254f;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudChatBar(聊天条)",
                Note = "底部聊天/系统消息条+设置/好友/商城/变强入口,有界 root 底贴横铺×254",
                Order = 50,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再统一激活(与 Login 系列 / HudActivityCreator 一致的安全写法)。
            RectTransform root = UiCreatorKit.NewRoot("HudChatBar");
            AnchorBottomCenter(root, ChatW, ChatH);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUIChatView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUIChatView>();

            // 隐藏模板挂载点:聊天项模板 / 变强 ActivityIcon 模板都是纯克隆源,不应裸露在可见容器里。
            RectTransform templates = NewTemplatesWrapper(viewRoot);

            // 全宽底图(ChatBackgroundImage,对标 _img_bg,老端点这块区域会 Fire OPEN_CHAT_VIEW,故开 raycastTarget)
            Image bg = UiCreatorKit.NewImage("ChatBackgroundImage", viewRoot); // 老端: _img_bg
            PlaceTL(bg.rectTransform, 0f, 0f, ChatW, ChatH, ChatW, ChatH);
            bg.raycastTarget = true;
            UiCreatorKit.TrySetSprite(bg, IMG_CHAT_BG, UiCreatorKit.Palette.Panel);
            view._img_bg = bg;

            // 聊天频道滚动区(世界/仙宗/跨服等由 MainUIChatView 从 ChatModel 实时填充)
            view._panel_chat = BuildMessageScroll("ChatMessagesScroll", "ChatMessagesContent", viewRoot,
                148f, 3f, 396f, 65f, out RectTransform chatCon); // 老端: _panel_chat / _box_chat_con
            view._box_chat_con = chatCon;

            // 系统消息滚动区(欢迎语与 11001 系统频道消息统一走 ChatModel)
            view._panel_sys = BuildMessageScroll("SystemMessagesScroll", "SystemMessagesContent", viewRoot,
                148f, 83f, 397f, 65f, out RectTransform sysCon); // 老端: _panel_sys / _box_sys_con
            view._box_sys_con = sysCon;
            view._tpl_MainUIChatItem = BuildChatItemTemplate(templates);

            // 设置入口(SettingEntry)
            RectTransform boxSetting = UiCreatorKit.NewNode("SettingEntry", viewRoot); // 老端: _box_setting
            PlaceTL(boxSetting, 4f, 25f, 64f, 64f, ChatW, ChatH);
            view._box_setting = boxSetting;

            Image imgSetting = UiCreatorKit.NewImage("SettingIcon", boxSetting); // 老端: _img_setting
            PlaceTL(imgSetting.rectTransform, 5f, -17f, 55f, 63f, 64f, 64f);
            UiCreatorKit.TrySetSprite(imgSetting, IMG_SETTING, UiCreatorKit.Palette.BtnNeutral);
            view._img_setting = imgSetting;

            // 好友入口(FriendEntry,+ 红点,老端声明 visible=false,数据到达前不现身)
            RectTransform boxFriend = UiCreatorKit.NewNode("FriendEntry", viewRoot); // 老端: _box_friend
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
            RectTransform boxShop = UiCreatorKit.NewNode("ShopEntry", viewRoot); // 老端: _box_shop
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
            RectTransform boxShopEffect = UiCreatorKit.NewNode("ShopDiscountEffectSlot", viewRoot); // 老端: _box_shop_effect
            PlaceTL(boxShopEffect, 620f, 15f, 100f, 100f, ChatW, ChatH);
            view._box_shop_effect = boxShopEffect;

            // 变强(StrengthenEntrySlot,ActivityIcon "158" 挂载点)
            RectTransform boxStrengthen = UiCreatorKit.NewNode("StrengthenEntrySlot", viewRoot); // 老端: _box_strengthen
            PlaceTL(boxStrengthen, 634f, 15f, 55f, 55f, ChatW, ChatH);
            view._box_strengthen = boxStrengthen;
            view._tpl_ActivityIcon = BuildActivityIconTemplate(templates);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudChatBar.prefab 已生成: " + PrefabPath +
                      "(聊天条区域,人工核对后再并入 MainUIModule.prefab)");
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
        /// 只有一份共享模板(_tpl_MainUIChatItem),MainUIChatView 按真实 ChatModel 消息克隆到聊天/系统容器。
        /// 老端根高 29；多行消息由 MainUIChatItem 根据 TMP preferred height 扩高。
        /// </summary>
        private static GameObject BuildChatItemTemplate(Transform parent)
        {
            const float itemW = 390f, itemH = MainUIChatItem.SingleLineHeight;
            RectTransform item = UiCreatorKit.NewNode("MainUIChatItem", parent);
            PlaceTL(item, 0f, 0f, itemW, itemH, itemW, itemH);
            var view = item.gameObject.AddComponent<MainUIChatItem>();
            var itemLayout = item.gameObject.AddComponent<LayoutElement>();
            itemLayout.minHeight = itemH;
            itemLayout.preferredHeight = itemH;

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
            UiCreatorKit.Stretch(title.rectTransform);
            title.fontSize = 15f;
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.SetActive(false); // 老端场景声明 visible=false,有频道名才显示(SetData 控制)
            view.title = title;

            RectTransform gpContent = UiCreatorKit.NewNode("MessageContentGroup", item); // 老端: _gp_content
            PlaceTL(gpContent, 4f, 0f, 381f, itemH, itemW, itemH);
            view._gp_content = gpContent;

            TextMeshProUGUI content = UiCreatorKit.NewText("contentLabel", gpContent, "欢迎踏入九州大荒。");
            UiCreatorKit.Stretch(content.rectTransform);
            content.fontSize = 18f;
            content.alignment = TextAlignmentOptions.TopLeft;
            content.overflowMode = TextOverflowModes.Overflow;
            content.textWrappingMode = TextWrappingModes.Normal;
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

            // 内部件锚边角/撑满:模板根被槽拉伸时,图撑满、角标钉角、文字条贴底(槽位式的尺寸传导链)。
            Image imgIcon = UiCreatorKit.NewImage("IconImage", item); // 老端: _img_icon
            UiCreatorKit.Stretch(imgIcon.rectTransform);
            imgIcon.raycastTarget = true;
            UiCreatorKit.TrySetSprite(imgIcon, IMG_ACTIVITY_SAMPLE, UiCreatorKit.Palette.BtnPrimary);
            icon._img_icon = imgIcon;

            Image boxArrow = UiCreatorKit.NewImage("GuideArrowSlot", item); // 老端: _box_arrow
            UiCreatorKit.Stretch(boxArrow.rectTransform);
            boxArrow.color = new Color(1f, 1f, 1f, 0f);
            boxArrow.raycastTarget = false;
            icon._box_arrow = boxArrow;

            // 钉右上角:x=原左上锚 50−72=−22,y 不变
            Image imgRed = UiCreatorKit.NewImage("IconRedDot", item); // 老端: _img_red
            PinTopRight(imgRed.rectTransform, -22f, 5f, 28f, 29f);
            imgRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRed, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            imgRed.gameObject.SetActive(false);
            icon._img_red = imgRed;

            // 底部横铺:宽随模板根,条顶钉在根底上方 1(原 TL(0,71,72,18) 于 72 高根 → 72−71=1)
            Image imgDescBg = UiCreatorKit.NewImage("DescriptionBarBg", item); // 老端: _img_desc_bg
            imgDescBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            imgDescBg.rectTransform.anchorMax = new Vector2(1f, 0f);
            imgDescBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            imgDescBg.rectTransform.sizeDelta = new Vector2(0f, 18f);
            imgDescBg.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            imgDescBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgDescBg, IMG_DESC_BG, UiCreatorKit.Palette.Panel);
            imgDescBg.gameObject.SetActive(false);
            icon._img_desc_bg = imgDescBg;

            // 钉右上角 49−72=−23
            Image imgRedNum = UiCreatorKit.NewImage("RedNumberBadgeBg", item); // 老端: _img_red_num
            PinTopRight(imgRedNum.rectTransform, -23f, 8f, 32f, 32f);
            imgRedNum.raycastTarget = false;
            UiCreatorKit.TrySetSprite(imgRedNum, IMG_RED_NUM_BG, UiCreatorKit.Palette.Mark);
            imgRedNum.gameObject.SetActive(false);
            icon._img_red_num = imgRedNum;

            // 钉右上角 65−72=−7
            TextMeshProUGUI lbNum = UiCreatorKit.NewText("RedNumberLabel", item, "3"); // 老端: _lb_num
            PinTopRight(lbNum.rectTransform, -7f, 1f, 30f, 18f);
            lbNum.fontSize = 18f;
            lbNum.gameObject.SetActive(false);
            icon._lb_num = lbNum;

            // 锚底中(0.5,0),x=0 水平居中:老端 x=36 是【中心枢轴】坐标(=72 宽根正中,ActivityIcon.prefab 可证),
            // 此前 PlaceTL 把它误当左上角换出 +90 右偏,是移植笔误,按居中修正。
            TextMeshProUGUI lbDesc = UiCreatorKit.NewText("DescriptionLabel", item, ""); // 老端: _lb_desc
            lbDesc.rectTransform.anchorMin = lbDesc.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            lbDesc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lbDesc.rectTransform.sizeDelta = new Vector2(180f, 32f);
            lbDesc.rectTransform.anchoredPosition = new Vector2(0f, -17f);
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

        // ================================================================ 布局换算 helper(本文件专用)

        /// <summary>
        /// 建一个隐藏的模板挂载容器(__Templates):专收纯克隆源(MainUIChatItem/ActivityIcon 等模板),
        /// 不让它们裸露在可见的业务容器(ChatMessagesContent/StrengthenEntrySlot 等)里当"不可见的子节点"。
        /// </summary>
        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        /// <summary>贴底、横向铺满(区域 root 用:宽度由 Stretch 撑满父级,高度固定)。</summary>
        /// <summary>底边贴齐 + 水平【固定宽居中】(老端 MainUIChatView 是 centerX=0、.scene 根固定 720×254)。
        /// 原先用的是横向 Stretch(anchorMax.x=1、sizeDelta.x=0):在 720 宽下与固定 720 等价,
        /// 但宽屏下 root 会跟着铺满整屏 —— 内部底图仍是 720 中心锚所以看不出来,可 root 的 raycast 区域
        /// 铺满了,会吃掉两侧本该穿透到 3D 场景的点击。改成固定宽居中后与老端一致。</summary>
        private static void AnchorBottomCenter(RectTransform rt, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
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

        /// <summary>钉右上角(anchor(1,1)/pivot(0,1)):x=原左上锚 x−根宽,y 不变(取正上)。角标类内部件用,
        /// 模板根被槽拉伸时角标始终贴右上角(与 ActivityIcon.prefab 的同款重锚一致)。</summary>
        private static void PinTopRight(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ================================================================ 预览

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudChatBar",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudChatBar 是并入 MainUIModule 的区域子视图,不走 ViewManager.Open<T>()" +
                    "(MainUIChatViewBind 没有 [UIView] 地址特性);预览直接把最新 prefab 实例化到 " +
                    "Window 层并手动调用 view.Show(),仅用于看结构/试交互。",
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
            var view = _previewInstance.GetComponentInChildren<MainUIChatView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] HudChatBar 预览实例缺少 MainUIChatView 组件");
                return;
            }
            view.Show();
        }
    }
}
