using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面「次级浮动层」纯代码建树生成器。
    ///
    /// 对标老端 mainUI 四个独立 view:MainUISecondaryView(经验丸/通知条/协助提示/右侧功能列容器)、
    /// FuncBoardView(功能说明气泡)、MainUIMarriageItem(姻缘挂件)、GiftPushIcon(礼包推送角标)。
    /// 四者在老端各自独立加载,这里合并成一个 prefab、四个子根,方便一次性管理/预览;
    /// FuncBoard/MarriageItem/GiftPush 三个都是事件驱动出现的浮层,默认 inactive。
    ///
    /// 布局关键:MainUISecondaryView 自身在老端是"底锚"view(left=0,right=0,bottom=290,
    /// 自身声明高度仅 1px),子节点的 y 是相对这条底锚线的负偏移(越负越靠上)。这里用
    /// <see cref="PlaceBottom"/> 复刻:子节点锚定在 SecondaryView 自身矩形的左下角(固定点,不随
    /// SecondaryView 的 sizeDelta 缩放),pivot 取自身左上角(对齐老端默认 anchorX=0,anchorY=0),
    /// anchoredPosition = (老端x, -老端y)。SecondaryView 更深的子孙节点则改用常规
    /// UiCreatorKit.Place() 中心锚摆位(老端 x/y/pivot 换算成"相对父矩形中心"的 cx/cy)。
    /// FuncBoardView / MainUIMarriageItem / GiftPushIcon 三个独立子根本身直接用 Place() 摆在
    /// 720×1280 设计画布下(它们不受 SecondaryView 的底锚规则约束)。
    ///
    /// 数据来源:运行时快照 page_snapshot_MainUISecondaryView_*.json / page_snapshot_FuncBoardView_*.json
    /// (57/6 节点,含已解析好的 x/y/globalBounds)+ 老端场景 JSON(resource/game/mainUI/*.json,
    /// 含 MainUISecondaryView 里快照阶段已被业务代码 reparent 掉、快照里看不到的 _box_right 原始位置)。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _box_left                -> LeftIconSlot
    //   _box_auto_effect         -> AutoStateEffectSlot
    //   _box_right               -> RightIconSlot
    //   _box_notice              -> NoticeIconSlot
    //   _box_god                 -> GodSkillIconSlot
    //   _gp_t_map                -> TreasureMapEffectSlot
    //   _img_tt_record           -> TtRecordButton
    //   _box_help                -> GuildHelpButtonBox
    //   _img_help                -> GuildHelpIcon
    //   _box_help_tips           -> GuildHelpTipBubble
    //   _img_help_tips           -> GuildHelpTipBubbleBg
    //   _lb_help                 -> GuildHelpTipLabel
    //   _box_notification_bar    -> NotificationBar
    //   _box_sea                 -> BrightSeaNoticeSlot
    //   _img_sea                 -> BrightSeaNoticeIcon
    //   _box_sea_red             -> BrightSeaRedBadge
    //   _img_sea_red             -> BrightSeaRedBadgeIcon
    //   _lb_sea_red_num          -> BrightSeaRedBadgeCountLabel
    //   _box_firstblood          -> FirstBloodNoticeSlot
    //   _img_first               -> FirstBloodNoticeIcon
    //   _img_first_blood_red     -> FirstBloodRedBadge
    //   _box_team                -> TeamInviteNoticeSlot
    //   _img_team                -> TeamInviteNoticeIcon
    //   _box_red_packet          -> RedPacketNoticeSlot
    //   _img_red_packet          -> RedPacketNoticeIcon
    //   _box_email               -> MailNoticeSlot
    //   _img_email               -> MailNoticeIcon
    //   _box_chat                -> ChatNoticeSlot
    //   _img_chat                -> ChatNoticeIcon
    //   _box_daily_find          -> DailyFindNoticeSlot
    //   _img_daily_find          -> DailyFindNoticeIcon
    //   _box_level_rew           -> LevelRewardNoticeSlot
    //   _img_level_rew           -> LevelRewardNoticeIcon
    //   _box_gift_push           -> GiftPushNoticeSlot
    //   _img_gift_push           -> GiftPushNoticeIcon
    //   _box_outline_exp         -> OnHookExpOrbBox
    //   _img_outline_exp_bg1     -> ExpOrbBaseBg
    //   _img_outline_exp_bg      -> ExpOrbRewardBg
    //   exp_show                 -> ExpRewardEffectSlot
    //   HBox_119                 -> ExpInfoRow
    //   _lb_outline_exp          -> ExpRateLabel
    //   add_btn                  -> ExpBoostButton
    //   add                      -> ExpBoostEffectAnchor
    //   _img_add                 -> ExpBoostIcon
    //   _box_exp_btn             -> ExpRewardButtonBox
    //   exp_btn                  -> ExpRewardButtonIcon
    //   _box_old_outline_exp     -> LegacyExpOrbBox
    //   _img_old_outline_exp_bg  -> LegacyExpOrbBg
    //   _lb_old_outline_exp      -> LegacyExpRateLabel
    //   _box_please              -> MarriageGiftHintBox
    //   _img_please              -> MarriageGiftHintIcon
    //   _gp_pro                  -> RedPacketRainEntryBox
    //   _img_rpr                 -> RedPacketRainIcon
    //   content_bg               -> ContentBackground
    //   _lb_con                  -> DescriptionLabel
    //   _lb_time                 -> DismissCountdownLabel
    //   _img (MainUIMarriageItem)-> MarriageIcon
    //   _gp_effect               -> GiftPushEffectSlot
    //   _img_icon                -> GiftIconImage
    //   _img_desc_bg             -> GiftCountdownBackground
    //   _lb_desc                 -> GiftCountdownLabel
    // 说明:MainUISecondaryView / FuncBoardView / MainUIMarriageItem / GiftPushIcon
    // 四个子根节点名对应业务 View/Bind 类名,保持不变,未在上表列出。
    public static class HudSecondaryCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudSecondary.prefab";

        // ---------------------------------------------------------------- 老端源图(均已 Glob 确认存在)
        private const string IMG_HELP = "resource/game/mainUI/texture/guild_help.png";
        private const string IMG_HELP_TIPS = "resource/game/mainUI/texture/uigh_124.png";           // sizeGrid 26,16,17,27
        private const string IMG_NOTICE_SEA = "resource/game/mainUI/texture/ui_notice_5.png";
        private const string IMG_RED_POINT = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_FIRST_BLOOD = "resource/game/mainUI/texture/UI_bsss_001.png";
        private const string IMG_NOTICE_TEAM = "resource/game/mainUI/texture/ui_notice_4.png";
        private const string IMG_NOTICE_RED_PACKET = "resource/game/mainUI/texture/ui_notice_6.png";
        private const string IMG_NOTICE_EMAIL = "resource/game/mainUI/texture/ui_notice_3.png";
        private const string IMG_NOTICE_CHAT = "resource/game/mainUI/texture/ui_notice_7.png";
        private const string IMG_NOTICE_DAILY_FIND = "resource/game/mainUI/texture/ui_notice_8.png";
        private const string IMG_LEVEL_REW = "resource/game/mainUI/texture/icon_ts_35.png";
        private const string IMG_GIFT_PUSH_ICON = "resource/game/mainUI/texture/icon_ts_34.png";
        private const string IMG_EXP_TITLE_BG = "resource/game/mainUI/texture/com_title_bg1.png";
        private const string IMG_EXP_BG = "resource/game/mainUI/texture/ui_gj_12.png";
        private const string IMG_EXP_ADD = "resource/game/common/texture/ui_gj_27.png";
        private const string IMG_EXP_BTN = "resource/game/mainUI/texture/ui_gj_13.png";
        private const string IMG_PLEASE = "resource/game/mainUI/texture/please.png";
        private const string IMG_RED_PACKET_RAIN = "resource/game/mainUI/texture/uihby_014.png";

        private const string IMG_FUNC_BOARD_BG = "resource/game/mainUI/texture/mainui_ui_41.png";
        private const string IMG_MARRIAGE_ICON = "resource/game/mainUI/texture/marriage_1.png";     // 运行态真实挂件图(老端 json 里 SetIcon 前的默认图是纯透明 com_empty.png,建树期改用这张保证可见)
        private const string IMG_GIFT_DESC_BG = "resource/game/mainUI/texture/mainui_ui_40.png";     // sizeGrid 5,5,5,5

        // 已在项目里的模板 prefab(对标老端 _tpl_ 系列,ActivityIcon/ItemInfoItem/MainUISkillItemGod
        // 三者各自都有独立 prefab 资产,直接引用即可,不在本 Creator 里重新建结构)。
        private const string TPL_ACTIVITY_ICON = "Assets/Prefabs/UI/MainUI/ActivityIcon.prefab";
        private const string TPL_ITEM_INFO_ITEM = "Assets/Prefabs/UI/Common/ItemInfoItem.prefab";
        private const string TPL_SKILL_ITEM_GOD = "Assets/Prefabs/UI/MainUI/MainUISkillItemGod.prefab";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudSecondary(次级浮动层)",
                Note = "SecondaryView(经验丸/通知条/右侧功能列容器) + FuncBoard气泡 + 姻缘挂件 + 礼包推送角标",
                Order = 60,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform hudRoot = UiCreatorKit.NewRoot("HudSecondary");
            hudRoot.gameObject.SetActive(false);

            BuildSecondaryView(hudRoot);
            BuildFuncBoardView(hudRoot);
            BuildMarriageItem(hudRoot);
            BuildGiftPushIcon(hudRoot);

            hudRoot.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(hudRoot.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudSecondary.prefab 已生成: " + PrefabPath +
                      "(四个子根均为普通子节点,非 ViewManager 独立窗口;由 MainUI 模块合并流程整体挂载/Show)");
        }

        // ============================================================ MainUISecondaryView

        private static void BuildSecondaryView(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUISecondaryView", hudRoot);
            // 老端:left=0,right=0,bottom=290 的底锚 view。水平拉伸铺满,垂直方向钉在距父层底边 290 的位置。
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(0f, 600f); // 高度仅供编辑器可视化用;子节点走固定点锚定,不受此值影响
            root.anchoredPosition = new Vector2(0f, 290f);

            var view = root.gameObject.AddComponent<MainUISecondaryView>();

            // ---------- 直接子节点(child 顺序对标老端 json 的 child 数组 = 渲染顺序)----------

            RectTransform boxLeft = UiCreatorKit.NewNode("LeftIconSlot", root); // 老端: _box_left
            PlaceBottom(boxLeft, 7f, -420f, 0f, 0f);
            view._box_left = boxLeft;

            view._box_help = BuildBoxHelp(root, view);

            view._box_notification_bar = BuildNotificationBar(root, view);

            RectTransform boxAutoEffect = UiCreatorKit.NewNode("AutoStateEffectSlot", root); // 老端: _box_auto_effect
            PlaceBottom(boxAutoEffect, 235f, -350f, 250f, 200f);
            view._box_auto_effect = boxAutoEffect; // 纯特效容器,老端无常驻子节点,内容留空

            view._box_outline_exp = BuildOutlineExp(root, view);
            // 老端 json:_box_outline_exp 默认 "visible":false;MainUISecondaryView.RefOutlineExp 三条件
            // 同时满足才置 true:OnHookModel.exp_effect>0(有挂机经验数据) && SceneManager.IsFieldScene()
            // (野外场景) && task_model.newest_finish_task_id >= TaskModel.AfkReceiveTimesTaskId(已完成
            // 领取离线经验任务)。新号/主城初期三者都不满足,老端根本不显示这颗经验丸。
            // OnHookModel/TaskModel 数据尚未移植到 View 层,先默认隐藏兜底,避免新号一进城就看到
            // 占位的"0经验/分"——等数据接入后由 View 按上述三条件点亮。
            view._box_outline_exp.gameObject.SetActive(false);

            view._box_old_outline_exp = BuildOldOutlineExp(root, view);
            view._box_old_outline_exp.gameObject.SetActive(false); // 老端 json 同样默认 visible:false,且与 _box_outline_exp 互斥(RefOutlineExp 里两者手动同步)

            // _box_right:老端场景里紧跟在 _box_old_outline_exp 后面(right=15,y=-68,0x0 标记点)。
            // 运行时 MainUISecondaryView.OnInit 会把它整体 reparent 到 SecondaryView 的父层
            // (right=0,centerY=250),这里只按老端【reparent 前】的原始位置建出来即可。
            RectTransform boxRight = UiCreatorKit.NewNode("RightIconSlot", root); // 老端: _box_right
            PlaceBottom(boxRight, 705f, -68f, 0f, 0f);
            view._box_right = boxRight;

            RectTransform boxNotice = UiCreatorKit.NewNode("NoticeIconSlot", root); // 老端: _box_notice
            PlaceBottom(boxNotice, 705f, -514f, 0f, 0f);
            view._box_notice = boxNotice; // 右列 ActivityIcon 容器,运行时由 ActivityIconManager 动态摆放,内容留空

            RectTransform boxGod = UiCreatorKit.NewNode("GodSkillIconSlot", root); // 老端: _box_god
            PlaceBottom(boxGod, 525f, -170f, 80f, 80f);
            boxGod.gameObject.SetActive(false);
            view._box_god = boxGod;

            view._box_please = BuildPlease(root, view);
            view._box_please.gameObject.SetActive(false);

            RectTransform gpTMap = UiCreatorKit.NewNode("TreasureMapEffectSlot", root); // 老端: _gp_t_map
            PlaceBottom(gpTMap, 152f, -271f, 416f, 100f);
            view._gp_t_map = gpTMap; // 老端亦无常驻子节点(另由藏宝图子组件动态填充),内容留空
            gpTMap.gameObject.SetActive(false); // 对标 MainUISecondaryView.OnInit 里的 _gp_t_map.SetActive(false)

            view._gp_pro = BuildGpPro(root, view);

            Image imgTtRecord = UiCreatorKit.NewImage("TtRecordButton", root); // 老端: _img_tt_record
            PlaceBottom(imgTtRecord.rectTransform, 538f, -21f, 55f, 55f);
            imgTtRecord.color = UiCreatorKit.Palette.BtnNeutral; // 老端该图无固定 skin(运行时动态换图),占位色回退
            imgTtRecord.gameObject.SetActive(false);
            view._img_tt_record = imgTtRecord;

            // ---------- 模板引用(对标老端 _tpl_ 系列;三者在本项目里均已有独立 prefab 资产,直接引用)----------
            view._tpl_ActivityIcon = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_ACTIVITY_ICON);
            view._tpl_ItemInfoItem = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_ITEM_INFO_ITEM);
            view._tpl_MainUISkillItemGod = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_SKILL_ITEM_GOD);
        }

        /// <summary>SecondaryView 专用:子节点锚定在父矩形【左下角固定点】(不随父 sizeDelta 缩放),
        /// pivot 取自身左上角(对齐老端默认 anchorX=0,anchorY=0),anchoredPosition=(老端x, -老端y)。</summary>
        private static void PlaceBottom(RectTransform rt, float x, float layaY, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -layaY);
        }

        // ---- _box_help(168,-21 55x55): 帮助按钮 + 协助提示气泡(默认隐藏) ----
        private static RectTransform BuildBoxHelp(Transform parent, MainUISecondaryView view)
        {
            RectTransform boxHelp = UiCreatorKit.NewNode("GuildHelpButtonBox", parent); // 老端: _box_help
            PlaceBottom(boxHelp, 168f, -21f, 55f, 55f);

            Image imgHelp = UiCreatorKit.NewImage("GuildHelpIcon", boxHelp); // 老端: _img_help
            UiCreatorKit.Place(imgHelp.rectTransform, 0f, 0f, 55f, 55f);
            UiCreatorKit.TrySetSprite(imgHelp, IMG_HELP, UiCreatorKit.Palette.BtnSecond);
            view._img_help = imgHelp;

            RectTransform boxHelpTips = UiCreatorKit.NewNode("GuildHelpTipBubble", boxHelp); // 老端: _box_help_tips
            UiCreatorKit.Place(boxHelpTips, 90f, 18f, 175f, 59f);
            boxHelpTips.gameObject.SetActive(false);
            view._box_help_tips = boxHelpTips;

            Image imgHelpTips = UiCreatorKit.NewImage("GuildHelpTipBubbleBg", boxHelpTips); // 老端: _img_help_tips
            UiCreatorKit.Place(imgHelpTips.rectTransform, 0f, 0f, 175f, 59f);
            UiCreatorKit.TrySetSprite(imgHelpTips, IMG_HELP_TIPS, UiCreatorKit.Palette.Panel);
            imgHelpTips.type = Image.Type.Sliced; // 老端 sizeGrid 26,16,17,27
            view._img_help_tips = imgHelpTips;

            TextMeshProUGUI lbHelp = UiCreatorKit.NewText("GuildHelpTipLabel", boxHelpTips, "收到新的协助请求"); // 老端: _lb_help
            UiCreatorKit.Place(lbHelp.rectTransform, 6.5f, 1.5f, 144f, 18f);
            lbHelp.fontSize = 18f;
            lbHelp.color = new Color32(0x4a, 0x3a, 0x32, 0xff);
            lbHelp.alignment = TextAlignmentOptions.TopLeft;
            view._lb_help = lbHelp;

            return boxHelp;
        }

        // ---- _box_notification_bar(209,-104 300x52): 通知条,9 个功能图标槽位 ----
        private static RectTransform BuildNotificationBar(Transform parent, MainUISecondaryView view)
        {
            RectTransform bar = UiCreatorKit.NewNode("NotificationBar", parent); // 老端: _box_notification_bar
            PlaceBottom(bar, 209f, -104f, 300f, 52f);

            // 老端 _box_sea/_box_team/_box_red_packet/_box_chat 共用同一枚基础槽位(-123,0.5),
            // 同一时刻只会有一个按功能开关显示,当前均为隐藏遗留功能。
            view._box_sea = BuildNoticeSlot(bar, "BrightSeaNoticeSlot", -123f, 0.5f, out Image imgSea, out RectTransform seaRed); // 老端: _box_sea
            UiCreatorKit.TrySetSprite(imgSea, IMG_NOTICE_SEA, UiCreatorKit.Palette.BtnSecond);
            view._img_sea = imgSea;
            view._box_sea.gameObject.SetActive(false);
            view._box_sea_red = seaRed;
            seaRed.gameObject.SetActive(false);
            Image imgSeaRed = UiCreatorKit.NewImage("BrightSeaRedBadgeIcon", seaRed); // 老端: _img_sea_red
            UiCreatorKit.Place(imgSeaRed.rectTransform, 0f, 0f, 28f, 28f);
            UiCreatorKit.TrySetSprite(imgSeaRed, IMG_RED_POINT, UiCreatorKit.Palette.BtnPrimary);
            view._img_sea_red = imgSeaRed;
            TextMeshProUGUI lbSeaRedNum = UiCreatorKit.NewText("BrightSeaRedBadgeCountLabel", seaRed, "2"); // 老端: _lb_sea_red_num
            UiCreatorKit.Place(lbSeaRedNum.rectTransform, 0f, 1f, 28f, 26f);
            lbSeaRedNum.fontSize = 20f;
            view._lb_sea_red_num = lbSeaRedNum;

            view._box_firstblood = BuildNoticeSlot(bar, "FirstBloodNoticeSlot", -85f, 0.5f, out Image imgFirst, out _, rotZ: 8f); // 老端: _box_firstblood
            UiCreatorKit.Place(imgFirst.rectTransform, 0f, 0f, 54f, 55f);
            UiCreatorKit.TrySetSprite(imgFirst, IMG_FIRST_BLOOD, UiCreatorKit.Palette.BtnPrimary);
            view._img_first = imgFirst;
            Image imgFirstBloodRed = UiCreatorKit.NewImage("FirstBloodRedBadge", view._box_firstblood); // 老端: _img_first_blood_red
            UiCreatorKit.Place(imgFirstBloodRed.rectTransform, 23.5f, 13f, 28f, 29f);
            imgFirstBloodRed.rectTransform.localScale = new Vector3(0.7f, 0.7f, 1f);
            UiCreatorKit.TrySetSprite(imgFirstBloodRed, IMG_RED_POINT, UiCreatorKit.Palette.BtnPrimary);
            imgFirstBloodRed.gameObject.SetActive(false);
            view._img_first_blood_red = imgFirstBloodRed;

            view._box_team = BuildNoticeSlot(bar, "TeamInviteNoticeSlot", -123f, 0.5f, out Image imgTeam, out _); // 老端: _box_team
            UiCreatorKit.TrySetSprite(imgTeam, IMG_NOTICE_TEAM, UiCreatorKit.Palette.BtnSecond);
            view._img_team = imgTeam;
            view._box_team.gameObject.SetActive(false);

            view._box_red_packet = BuildNoticeSlot(bar, "RedPacketNoticeSlot", -123f, 0.5f, out Image imgRedPacket, out _); // 老端: _box_red_packet
            UiCreatorKit.TrySetSprite(imgRedPacket, IMG_NOTICE_RED_PACKET, UiCreatorKit.Palette.BtnPrimary);
            view._img_red_packet = imgRedPacket;
            view._box_red_packet.gameObject.SetActive(false);
            imgRedPacket.gameObject.SetActive(false);

            view._box_email = BuildNoticeSlot(bar, "MailNoticeSlot", -28f, 0.5f, out Image imgEmail, out _, rotZ: 8f); // 老端: _box_email
            UiCreatorKit.TrySetSprite(imgEmail, IMG_NOTICE_EMAIL, UiCreatorKit.Palette.BtnSecond);
            view._img_email = imgEmail;

            view._box_chat = BuildNoticeSlot(bar, "ChatNoticeSlot", -123f, 0.5f, out Image imgChat, out _); // 老端: _box_chat
            UiCreatorKit.TrySetSprite(imgChat, IMG_NOTICE_CHAT, UiCreatorKit.Palette.BtnSecond);
            view._img_chat = imgChat;
            view._box_chat.gameObject.SetActive(false);
            imgChat.gameObject.SetActive(false);

            view._box_daily_find = BuildNoticeSlot(bar, "DailyFindNoticeSlot", 29f, 2.5f, out Image imgDailyFind, out _, rotZ: 8f); // 老端: _box_daily_find
            UiCreatorKit.Place(imgDailyFind.rectTransform, 0f, -1f, 55f, 55f);
            UiCreatorKit.TrySetSprite(imgDailyFind, IMG_NOTICE_DAILY_FIND, UiCreatorKit.Palette.BtnSecond);
            view._img_daily_find = imgDailyFind;

            view._box_level_rew = BuildNoticeSlot(bar, "LevelRewardNoticeSlot", 86f, 2.5f, out Image imgLevelRew, out _, rotZ: 8f); // 老端: _box_level_rew
            UiCreatorKit.TrySetSprite(imgLevelRew, IMG_LEVEL_REW, UiCreatorKit.Palette.BtnPrimary);
            view._img_level_rew = imgLevelRew;

            view._box_gift_push = BuildNoticeSlot(bar, "GiftPushNoticeSlot", 57f, 0.5f, out Image imgGiftPush, out _); // 老端: _box_gift_push
            UiCreatorKit.TrySetSprite(imgGiftPush, IMG_GIFT_PUSH_ICON, UiCreatorKit.Palette.BtnPrimary);
            view._img_gift_push = imgGiftPush;

            // 消息驱动槽位默认全部隐藏:老端由服务端推送/系统触发才显示(首杀/邮件/每日寻访/等级奖励/礼包推送),
            // 初始一律不可见,对应系统移植后由 View 侧 SetActive(true) 点亮(2026-07-06 用户验收问题#3)。
            view._box_firstblood.gameObject.SetActive(false);
            view._box_email.gameObject.SetActive(false);
            view._box_daily_find.gameObject.SetActive(false);
            view._box_level_rew.gameObject.SetActive(false);
            view._box_gift_push.gameObject.SetActive(false);

            return bar;
        }

        /// <summary>建一个通知条槽位:55x55 容器 + 同尺寸主图;可选挂一个 28x28 红点子容器(sea 专用)。</summary>
        private static RectTransform BuildNoticeSlot(Transform parent, string name, float cx, float cy,
            out Image mainImage, out RectTransform redBadge, float rotZ = 0f)
        {
            RectTransform box = UiCreatorKit.NewNode(name, parent);
            UiCreatorKit.Place(box, cx, cy, 55f, 55f);
            if (rotZ != 0f) box.localEulerAngles = new Vector3(0f, 0f, rotZ);

            mainImage = UiCreatorKit.NewImage(ImgChildName(name), box);
            UiCreatorKit.Place(mainImage.rectTransform, 0f, 0f, 55f, 55f);

            redBadge = name == "BrightSeaNoticeSlot" ? UiCreatorKit.NewNode("BrightSeaRedBadge", box) : null; // 老端: _box_sea_red
            if (redBadge != null) UiCreatorKit.Place(redBadge, 23.5f, 13.5f, 28f, 28f);

            return box;
        }

        private static string ImgChildName(string boxName)
        {
            // 槽位容器名 -> 内部主图名(老端: "_box_sea"->"_img_sea";"_box_firstblood"->"_img_first",
            // 命名不规则,单独映射;现语义化后按槽位语义各自加 Icon 后缀)。
            switch (boxName)
            {
                case "BrightSeaNoticeSlot": return "BrightSeaNoticeIcon"; // 老端: _box_sea -> _img_sea
                case "FirstBloodNoticeSlot": return "FirstBloodNoticeIcon"; // 老端: _box_firstblood -> _img_first
                case "TeamInviteNoticeSlot": return "TeamInviteNoticeIcon"; // 老端: _box_team -> _img_team
                case "RedPacketNoticeSlot": return "RedPacketNoticeIcon"; // 老端: _box_red_packet -> _img_red_packet
                case "MailNoticeSlot": return "MailNoticeIcon"; // 老端: _box_email -> _img_email
                case "ChatNoticeSlot": return "ChatNoticeIcon"; // 老端: _box_chat -> _img_chat
                case "DailyFindNoticeSlot": return "DailyFindNoticeIcon"; // 老端: _box_daily_find -> _img_daily_find
                case "LevelRewardNoticeSlot": return "LevelRewardNoticeIcon"; // 老端: _box_level_rew -> _img_level_rew
                case "GiftPushNoticeSlot": return "GiftPushNoticeIcon"; // 老端: _box_gift_push -> _img_gift_push
                default: return "NoticeIcon"; // 老端: _img
            }
        }

        // ---- _box_outline_exp(231,-180 257x61): 经验丸 ----
        private static RectTransform BuildOutlineExp(Transform parent, MainUISecondaryView view)
        {
            RectTransform box = UiCreatorKit.NewNode("OnHookExpOrbBox", parent); // 老端: _box_outline_exp
            PlaceBottom(box, 231f, -180f, 257f, 61f);

            Image bg1 = UiCreatorKit.NewImage("ExpOrbBaseBg", box); // 老端: _img_outline_exp_bg1
            UiCreatorKit.Place(bg1.rectTransform, 0f, -3.5f, 351f, 40f);
            bg1.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg1, IMG_EXP_TITLE_BG, UiCreatorKit.Palette.Panel);
            view._img_outline_exp_bg1 = bg1;

            Image bg = UiCreatorKit.NewImage("ExpOrbRewardBg", box); // 老端: _img_outline_exp_bg
            UiCreatorKit.Place(bg.rectTransform, 0.5f, 30.5f, 270f, 118f);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_EXP_BG, UiCreatorKit.Palette.Panel);
            bg.gameObject.SetActive(false);
            view._img_outline_exp_bg = bg;

            RectTransform expShow = UiCreatorKit.NewNode("ExpRewardEffectSlot", box); // 老端: exp_show
            UiCreatorKit.Place(expShow, 0f, 45.5f, 415f, 100f);
            expShow.gameObject.SetActive(false); // 纯特效容器(老端亦无常驻子节点),内容留空
            view.exp_show = expShow;

            RectTransform hbox = UiCreatorKit.NewNode("ExpInfoRow", box); // 老端: HBox_119
            UiCreatorKit.Place(hbox, 0f, 0f, 219f, 31f);

            TextMeshProUGUI lbExp = UiCreatorKit.NewText("ExpRateLabel", hbox, "<color=#00fa64>0</color>经验/分"); // 老端: _lb_outline_exp
            UiCreatorKit.Place(lbExp.rectTransform, -49.5f, -3.5f, 120f, 18f);
            lbExp.fontSize = 24f;
            lbExp.alignment = TextAlignmentOptions.Left;
            view._lb_outline_exp = lbExp;

            RectTransform addBtn = UiCreatorKit.NewNode("ExpBoostButton", hbox); // 老端: add_btn
            UiCreatorKit.Place(addBtn, 75f, 0f, 69f, 31f);
            view.add_btn = addBtn;

            RectTransform add = UiCreatorKit.NewNode("ExpBoostEffectAnchor", addBtn); // 老端: add
            UiCreatorKit.Place(add, 0f, 0f, 69f, 31f);
            // 老端 json 的 add 是空 Box,没有 skin 也没有 Graphics 绘制;ts 里它只是
            // AddUIEffect("UI_tisheng", this.add, ...) / ClearUIEffect(this.add) 的特效挂载点,
            // 自身从不持久渲染任何内容。这里之前挂了一个 Palette 占位色 Image,父级默认可见时
            // 就会露出一块蓝色矩形——现在改为纯空节点(不挂 Image),跟随丸子一起默认不可见,
            // 等挂机加成特效系统接入后再在此处挂真实特效,不在此处贴占位图/占位色。
            view.add = add;

            Image imgAdd = UiCreatorKit.NewImage("ExpBoostIcon", addBtn); // 老端: _img_add
            UiCreatorKit.Place(imgAdd.rectTransform, -9.5f, -2.5f, 50f, 30f);
            UiCreatorKit.TrySetSprite(imgAdd, IMG_EXP_ADD, UiCreatorKit.Palette.BtnPrimary);
            imgAdd.gameObject.SetActive(false);
            view._img_add = imgAdd;

            RectTransform boxExpBtn = UiCreatorKit.NewNode("ExpRewardButtonBox", box); // 老端: _box_exp_btn
            UiCreatorKit.Place(boxExpBtn, 0.5f, 46.5f, 66f, 70f);
            boxExpBtn.gameObject.SetActive(false);
            view._box_exp_btn = boxExpBtn;

            Image expBtn = UiCreatorKit.NewImage("ExpRewardButtonIcon", boxExpBtn); // 老端: exp_btn
            UiCreatorKit.Place(expBtn.rectTransform, 0f, 0f, 66f, 70f);
            expBtn.rectTransform.localEulerAngles = new Vector3(0f, 0f, 7f);
            UiCreatorKit.TrySetSprite(expBtn, IMG_EXP_BTN, UiCreatorKit.Palette.BtnPrimary);
            view.exp_btn = expBtn;

            return box;
        }

        // ---- _box_old_outline_exp(同位,隐藏): 旧版经验丸(整支保留但默认关闭) ----
        private static RectTransform BuildOldOutlineExp(Transform parent, MainUISecondaryView view)
        {
            RectTransform box = UiCreatorKit.NewNode("LegacyExpOrbBox", parent); // 老端: _box_old_outline_exp
            PlaceBottom(box, 231f, -180f, 257f, 61f);

            Image bg = UiCreatorKit.NewImage("LegacyExpOrbBg", box); // 老端: _img_old_outline_exp_bg
            UiCreatorKit.Place(bg.rectTransform, 0f, -0.5f, 351f, 40f);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_EXP_TITLE_BG, UiCreatorKit.Palette.Panel);
            view._img_old_outline_exp_bg = bg;

            TextMeshProUGUI lbOld = UiCreatorKit.NewText("LegacyExpRateLabel", box, ""); // 老端: _lb_old_outline_exp
            UiCreatorKit.Place(lbOld.rectTransform, 0.5f, -0.5f, 200f, 40f);
            lbOld.fontSize = 22f;
            lbOld.color = new Color32(0x71, 0xe1, 0x5b, 0xff);
            view._lb_old_outline_exp = lbOld;

            return box;
        }

        // ---- _box_please(418,-251 70x70,隐藏): 婚恋礼物提示 ----
        private static RectTransform BuildPlease(Transform parent, MainUISecondaryView view)
        {
            RectTransform box = UiCreatorKit.NewNode("MarriageGiftHintBox", parent); // 老端: _box_please
            PlaceBottom(box, 418f, -251f, 70f, 70f);

            Image imgPlease = UiCreatorKit.NewImage("MarriageGiftHintIcon", box); // 老端: _img_please
            UiCreatorKit.Place(imgPlease.rectTransform, 0.5f, 0f, 65f, 58f);
            UiCreatorKit.TrySetSprite(imgPlease, IMG_PLEASE, UiCreatorKit.Palette.BtnPrimary);
            view._img_please = imgPlease;

            return box;
        }

        // ---- _gp_pro(321,-354 78x78): 红包雨/进度入口 ----
        private static RectTransform BuildGpPro(Transform parent, MainUISecondaryView view)
        {
            RectTransform box = UiCreatorKit.NewNode("RedPacketRainEntryBox", parent); // 老端: _gp_pro
            PlaceBottom(box, 321f, -354f, 78f, 78f);

            Image imgRpr = UiCreatorKit.NewImage("RedPacketRainIcon", box); // 老端: _img_rpr
            UiCreatorKit.Place(imgRpr.rectTransform, 0f, 0f, 78f, 78f);
            UiCreatorKit.TrySetSprite(imgRpr, IMG_RED_PACKET_RAIN, UiCreatorKit.Palette.BtnPrimary);
            imgRpr.gameObject.SetActive(false);
            view._img_rpr = imgRpr;

            return box;
        }

        // ============================================================ FuncBoardView(功能说明气泡)

        private static void BuildFuncBoardView(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("FuncBoardView", hudRoot);
            // 老端无固定驻点位置(靠 SetData 时按目标图标屏幕坐标现场摆放),这里给一个居中偏上的起步默认位。
            UiCreatorKit.Place(root, 0f, 300f, 201f, 59f);

            var view = root.gameObject.AddComponent<FuncBoardView>();

            Image contentBg = UiCreatorKit.NewImage("ContentBackground", root); // 老端: content_bg
            UiCreatorKit.Place(contentBg.rectTransform, -7.5f, -0.5f, 201f, 82f);
            contentBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(contentBg, IMG_FUNC_BOARD_BG, UiCreatorKit.Palette.Panel);
            view.content_bg = contentBg;

            TextMeshProUGUI lbCon = UiCreatorKit.NewText("DescriptionLabel", root, "可激活套装"); // 老端: _lb_con
            UiCreatorKit.Place(lbCon.rectTransform, -9.5f, 0f, 142f, 45f);
            lbCon.fontSize = 18f;
            view._lb_con = lbCon;

            TextMeshProUGUI lbTime = UiCreatorKit.NewText("DismissCountdownLabel", root, ""); // 老端: _lb_time
            UiCreatorKit.Place(lbTime.rectTransform, -0.5f, -40f, 142f, 21f);
            lbTime.fontSize = 18f;
            lbTime.color = new Color32(0x00, 0xfa, 0x64, 0xff);
            lbTime.gameObject.SetActive(false);
            view._lb_time = lbTime;

            root.gameObject.SetActive(false); // 事件驱动出现(图标提示时才 SetData + 显示)
        }

        // ============================================================ MainUIMarriageItem(姻缘挂件)

        private static void BuildMarriageItem(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("MainUIMarriageItem", hudRoot);
            // 运行态参考坐标(老端 root 552,478 178x71,720x1280 舞台换算成中心锚)。
            UiCreatorKit.Place(root, 281f, 126.5f, 178f, 71f);

            var view = root.gameObject.AddComponent<MainUIMarriageItem>();

            Image img = UiCreatorKit.NewImage("MarriageIcon", root); // 老端: _img
            UiCreatorKit.Place(img.rectTransform, 0f, 0f, 178f, 71f);
            UiCreatorKit.TrySetSprite(img, IMG_MARRIAGE_ICON, UiCreatorKit.Palette.Panel);
            view._img = img;

            root.gameObject.SetActive(false); // 事件驱动出现(婚恋新手预览开放时才显示)
        }

        // ============================================================ GiftPushIcon(礼包推送角标,无业务类)

        private static void BuildGiftPushIcon(Transform hudRoot)
        {
            RectTransform root = UiCreatorKit.NewNode("GiftPushIcon", hudRoot);
            // 老端里这是被其它功能面板(装备洗练/幻兽/秘轮…)各自的 giftIcon 容器复用的挂件,
            // 本身无固定驻屏坐标;这里放在设计画布中心作为起步默认位,供后续挂到具体面板时再手调。
            UiCreatorKit.Place(root, 0f, 0f, 72f, 72f);

            // GiftPushIcon 没有业务类(纯 Bind);GiftPushIconBind 是具体类、继承自可用的 BaseView,
            // 可以直接挂 Bind 本身做结构承载(runtime 若要用需另写业务子类或直接用 Bind 的 Show()/字段)。
            var bind = root.gameObject.AddComponent<GiftPushIconBind>();

            RectTransform gpEffect = UiCreatorKit.NewNode("GiftPushEffectSlot", root); // 老端: _gp_effect
            UiCreatorKit.Place(gpEffect, 0f, -6.5f, 72f, 85f);
            bind._gp_effect = gpEffect; // 纯特效容器(老端亦无常驻子节点),内容留空

            Image imgIcon = UiCreatorKit.NewImage("GiftIconImage", root); // 老端: _img_icon
            UiCreatorKit.Place(imgIcon.rectTransform, 0f, 0f, 72f, 72f);
            imgIcon.color = UiCreatorKit.Palette.BtnPrimary; // 老端该图无固定 skin(礼包图标运行时按 cfg 动态换),占位色回退
            bind._img_icon = imgIcon;

            Image imgDescBg = UiCreatorKit.NewImage("GiftCountdownBackground", root); // 老端: _img_desc_bg
            UiCreatorKit.Place(imgDescBg.rectTransform, 14f, -50f, 100f, 30f);
            UiCreatorKit.TrySetSprite(imgDescBg, IMG_GIFT_DESC_BG, UiCreatorKit.Palette.Panel);
            imgDescBg.type = Image.Type.Sliced; // 老端 sizeGrid 5,5,5,5
            imgDescBg.gameObject.SetActive(false);
            bind._img_desc_bg = imgDescBg;

            TextMeshProUGUI lbDesc = UiCreatorKit.NewText("GiftCountdownLabel", root, "00:00:00"); // 老端: _lb_desc
            UiCreatorKit.Place(lbDesc.rectTransform, 0f, -45f, 132f, 20f);
            lbDesc.fontSize = 16f;
            lbDesc.color = new Color32(0x88, 0xff, 0x43, 0xff);
            bind._lb_desc = lbDesc;

            root.gameObject.SetActive(false); // 事件驱动出现(挂到具体面板的 giftIcon 容器上才显示)
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudSecondary",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "HudSecondary 是合并区域 prefab(四个子根都未注册 [UIView]/Addressable key,不走 " +
                    "ViewManager.Open<T>()),预览直接把最新 prefab 实例化到 Window 层看结构,并只 Show " +
                    "常驻的 MainUISecondaryView(其余三个事件驱动子根维持 inactive)。",
                    "好");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiCreator] 未找到 " + PrefabPath + ",请先点生成。");
                return;
            }

            Transform parent = ViewManager.GetLayer(UILayer.Window);
            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = "HudSecondary(Preview)";

            var secondary = go.GetComponentInChildren<MainUISecondaryView>(true);
            if (secondary != null) secondary.Show();

            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
