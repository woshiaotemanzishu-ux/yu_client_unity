using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
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
    /// root 已收成【底部散布带】(全宽×830,不再全屏);FuncBoardView / MainUIMarriageItem / GiftPushIcon
    /// 三个独立子根改用 PlaceAboveBottom 锚屏幕底边(屏幕位置与原 720×1280 画布 Place() 完全一致)。
    ///
    /// 数据来源:运行时快照 page_snapshot_MainUISecondaryView_*.json / page_snapshot_FuncBoardView_*.json
    /// (57/6 节点,含已解析好的 x/y/globalBounds)+ 老端场景 JSON(resource/game/mainUI/*.json)。
    /// _box_right 不再按"reparent 前原始位置"建:老端运行时会把它 reparent 到父层(right=0,centerY=250),
    /// 现直接按这个【运行时终态】建成 MainUISecondaryView 的兄弟节点(挂 HudSecondary 根下),
    /// MainUISecondaryView.OnInit 的搬家代码已删。
    /// 左/右图标簇改【槽位式】:_box_left 下 4 槽、_box_right 下 8 槽,
    /// 槽位位置=旧运行时 GetVisiblePos 公式的等价终态、全烤 prefab 可拖,运行时按序填。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _box_left                -> LeftIconSlot
    //   _box_auto_effect         -> AutoStateEffectSlot
    //   _box_right               -> RightIconSlot
    //   _box_notice              -> (活动入口已统一归 HudActivity.prefab,本区不再建)
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

        // 槽位样例图候选(仅设计期可视,运行时被 ClearDesignTimeSampleIcons 清掉;
        // 生成时按顺序取第一个存在于 Assets/GameRes/resource/game/icon/texture/ 的)。
        private static readonly string[] RightSlotSampleCandidates = { "3301.png", "2820.png", "6210.png", "137.png", "621.png" };
        private static readonly string[] LeftSlotSampleCandidates = { "159.png", "158.png", "151_1.png" };

        // 已退役：HudSecondary 不再注册到重构 UI 生成器，也不再进入 MainUIModule。
        // 保留旧生成代码只用于对照拆分前的几何来源；新入口见 HudAuxiliaryCreator/HudActivityCreator。
        private static void RegisterLegacy()
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
            // root 改回【全屏 Stretch】:原本是全宽×830 的钉底带,但那样 root 的垂直中线是"距屏幕底 415"的
            // 固定线,而不是真正的屏幕中线 —— 老端 _box_right(右侧功能列)用的是 centerY=250,即相对
            // 【屏幕】垂直中线偏移,必须有一个高度随屏幕变化的父矩形才能复刻。
            // 改全屏是安全的:原 root 本就 pivot(0.5,0)+anchoredPosition(0,0)+锚底,底边就在屏幕底;
            // 全屏后底边仍在屏幕底,而所有子根(MainUISecondaryView / 三个 PlaceAboveBottom 子根)都锚 root 底边,
            // 位置一律不变。720×1280 基准档逐像素零位移。
            UiCreatorKit.Stretch(hudRoot);
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

            // 容器收成实际矩形(老端是 0×0 标记点、槽位悬挂在原点外——编辑器里选不中/看不到框):
            // 4 槽横排(72+5 间距)→ 303×72,屏幕位 (7..310, 498..570),与旧 0×0 悬挂时的槽位完全重合。
            RectTransform boxLeft = UiCreatorKit.NewNode("LeftIconSlot", root); // 老端: _box_left
            PlaceBottom(boxLeft, 7f, -492f, 303f, 72f);
            view._box_left = boxLeft;
            // 【槽位基线】左簇 4 个空槽(容器内部坐标,左上锚):横排、间距 72+5。槽位置全在 prefab 可拖,
            // 运行时 MainUISecondaryView 按序填。
            for (int i = 0; i < 4; i++)
            {
                BuildIconSlot(boxLeft, i, i * (72f + 5f), 0f, new Vector2(0f, 1f), i == 0 ? LeftSlotSampleCandidates : null);
            }

            view._box_help = BuildBoxHelp(root, view);

            view._box_notification_bar = BuildNotificationBar(root, view);

            RectTransform boxAutoEffect = UiCreatorKit.NewNode("AutoStateEffectSlot", root); // 老端: _box_auto_effect
            PlaceBottomCenterX(boxAutoEffect, 235f, -350f, 250f, 200f); // 老端 centerX=0(实测中心 235+125=360)
            view._box_auto_effect = boxAutoEffect;

            // 老端 UpdateAutoStateEffect:寻路优先于自动战斗,二者互斥挂到同一 250x200 宿主。
            // 静态 offset/scale 归 prefab/Creator,运行时只按 slotId 选择并维护 Handle。
            RectTransform autoDynamicResources = UiCreatorKit.NewNode("__DynamicResources", boxAutoEffect);
            UiCreatorKit.Stretch(autoDynamicResources);
            BuildAutoStateEffectSlot(autoDynamicResources, MainUISecondaryView.AUTO_PATHING_EFFECT_SLOT_ID,
                "ui_zidongxunluzhong", "存在未完成寻路时优先显示;与自动战斗态互斥");
            BuildAutoStateEffectSlot(autoDynamicResources, MainUISecondaryView.AUTO_FIGHTING_EFFECT_SLOT_ID,
                "ui_zidongzhandouzhong", "无寻路且自动战斗开启时显示;与寻路态互斥");

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

            // _box_right:老端场景里是 SecondaryView 的子节点(right=15,y=-68,0x0 标记点),运行时
            // MainUISecondaryView.OnInit 曾把它整体 reparent 到 SecondaryView 的父层(right=0,centerY=250)。
            // 现直接按【运行时终态】烤进 prefab:建成 MainUISecondaryView 的兄弟节点(挂 hudRoot 下),
            // 右缘锚定、centerY-250(Laya centerY 向下为正 → Unity y=-250),运行时不再搬家/改锚;
            // Bind 字段照常回填(EnsureBound 只查引用非空,不管层级)。
            RectTransform boxRight = UiCreatorKit.NewNode("RightIconSlot", hudRoot); // 老端: _box_right
            // 容器收成实际矩形(老端是 0×0 标记点):右列入口图原图是 114×64 横条(大战/副本/竞技…,老端对
            // 右列专门按原图尺寸显示,一刀切 72×72 会把横条压扁——这就是"进游戏被压缩"的根因),
            // 槽按真实横条尺寸建:2 列×6 行 = 228×384,右缘锚、底距屏幕底 390(首图标底边=老终态 y890)。
            // 垂直方向锚【屏幕中线】而非钉底:老端是 centerY=250(相对满屏 Main 层的垂直中线下移 250),
            // 原先烤成"距屏幕底 390"在 720×1280 下等价,但屏幕一变高就分叉(老端跟中线走、Unity 钉死离底)。
            // pivot 保持 (1,0) 不动 —— 老端 _box_right 是 0×0 标记点,那个原点对应 Unity 这里的【底边】,
            // 连 pivot 一起改会让整簇上移半个高度(192px)。
            // 校验:720×1280 中线距底 640,底边 = 640-250 = 390,与原值一致(零位移);
            //       1080×2400(Expand 逻辑高 1600)中线距底 800,底边 = 550,老端 1600-(800+250) = 550,一致。
            boxRight.anchorMin = new Vector2(1f, 0.5f);
            boxRight.anchorMax = new Vector2(1f, 0.5f);
            boxRight.pivot = new Vector2(1f, 0f);
            boxRight.sizeDelta = new Vector2(228f, 384f);
            boxRight.anchoredPosition = new Vector2(0f, -250f);
            view._box_right = boxRight;
            // 【槽位基线】右簇 8 个 114×64 空槽(容器内部坐标,右下锚):第一列 6 个从容器底往上排,
            // 第 7、8 个在左边第二列。槽位置/尺寸全在 prefab 可拖(图标克隆体撑满所在槽,槽多大图多大)。
            for (int i = 0; i < 8; i++)
            {
                BuildIconSlot(boxRight, i,
                    -(Mathf.Floor(i / 6f) * 114f),
                    (i % 6) * 64f,
                    new Vector2(1f, 0f),
                    i == 0 ? RightSlotSampleCandidates : null,
                    114f, 64f);
            }

            // 老端 _box_notice「通知位」活动入口已统一归 HudActivity.prefab,
            // 本区不再建 NoticeIconSlot 节点或承接任何活动位置组。

            RectTransform boxGod = UiCreatorKit.NewNode("GodSkillIconSlot", root); // 老端: _box_god
            PlaceBottomRight(boxGod, 525f, -170f, 80f, 80f); // 老端 right=115(实测右缘 720-(525+80)=115)
            boxGod.gameObject.SetActive(false);
            view._box_god = boxGod;

            view._box_please = BuildPlease(root, view);
            view._box_please.gameObject.SetActive(false);

            RectTransform gpTMap = UiCreatorKit.NewNode("TreasureMapEffectSlot", root); // 老端: _gp_t_map
            PlaceBottomCenterX(gpTMap, 152f, -271f, 416f, 100f); // 老端 centerX=0(实测中心 152+208=360)
            view._gp_t_map = gpTMap; // 老端亦无常驻子节点(另由藏宝图子组件动态填充),内容留空
            gpTMap.gameObject.SetActive(false); // 对标 MainUISecondaryView.OnInit 里的 _gp_t_map.SetActive(false)

            view._gp_pro = BuildGpPro(root, view);

            Image imgTtRecord = UiCreatorKit.NewImage("TtRecordButton", root); // 老端: _img_tt_record
            PlaceBottomCenterX(imgTtRecord.rectTransform, 538f, -21f, 55f, 55f); // 老端 centerX=205(实测中心 538+27.5=565.5)
            imgTtRecord.color = UiCreatorKit.Palette.BtnNeutral; // 老端该图无固定 skin(运行时动态换图),占位色回退
            imgTtRecord.gameObject.SetActive(false);
            view._img_tt_record = imgTtRecord;

            // ---------- 模板引用(对标老端 _tpl_ 系列;三者在本项目里均已有独立 prefab 资产,直接引用)----------
            view._tpl_ActivityIcon = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_ACTIVITY_ICON);
            view._tpl_ItemInfoItem = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_ITEM_INFO_ITEM);
            view._tpl_MainUISkillItemGod = AssetDatabase.LoadAssetAtPath<GameObject>(TPL_SKILL_ITEM_GOD);
        }

        /// <summary>浮层子根专用:锚屏幕底边(=root 底部散布带的底边)、中心轴心摆位——x=相对水平中线的偏移,
        /// bottomUp=节点中心距底边距离。统一锚底后,root 高度可随意调而不影响任何子件屏幕位置。</summary>
        private static void PlaceAboveBottom(RectTransform rt, float x, float bottomUp, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, bottomUp);
        }

        /// <summary>同 PlaceAboveBottom,但水平方向锚【屏幕右缘】(老端 right 型)。
        /// x 仍传相对水平中线的偏移(与 PlaceAboveBottom 同参数),方法内换算成相对右缘的偏移;
        /// pivot 保持中心不变,基准档零位移。</summary>
        private static void PlaceAboveBottomRight(RectTransform rt, float x, float bottomUp, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x - DesignWidth * 0.5f, bottomUp);
        }

        /// <summary>SecondaryView 专用:子节点锚定在父矩形【左下角固定点】(不随父 sizeDelta 缩放),
        /// pivot 取自身左上角(对齐老端默认 anchorX=0,anchorY=0),anchoredPosition=(老端x, -老端y)。
        /// 仅给老端真·左锚的子件用(当前只有 _box_left);其余一律走 PlaceBottomCenterX / PlaceBottomRight。</summary>
        private static void PlaceBottom(RectTransform rt, float x, float layaY, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -layaY);
        }

        // ---------------------------------------------------------------------------------------
        // 老端 Laya 的 centerX / right 相对布局语义 → Unity 锚点。
        //
        // 为什么需要这两个方法:老端 MainUISecondaryView 的子件绝大多数用 Laya Widget 的 centerX(水平居中偏移)
        // 或 right(距右边缘)定位,屏幕变宽时会自动跟随中线/右缘。但这层语义写在老端 .scene 数据与 TS 里,
        // 而本 Creator 的几何源是 720×1280 的【单点运行时快照】——在这一个采样点上 "centerX=-1" 与 "left=209"
        // 完全等价、快照分辨不出,于是过去一律烤成了 PlaceBottom 的左锚绝对坐标。父节点是水平拉伸的,
        // 结果宽屏下这批子件整体左漂 (实际宽-720)/2,这正是"改宽高比界面就乱"的直接根因之一。
        //
        // 换算故意【只改 anchor、不改 pivot】:pivot 保持左上 (0,1) 与 PlaceBottom 一致,这样调用点无需改动
        // 任何数值,只换方法名即可,720×1280 基准档保证逐像素零位移(硬性验收标准)。
        // 也【不直接抄老端的 centerX/right 字面量】:快照实测的 x 精度更高(例如 _box_help 实测中心 195.5,
        // 老端 centerX=-165 折合 195,直接抄会引入 0.5px 位移)。
        // ---------------------------------------------------------------------------------------

        /// <summary>老端设计宽度;快照 x 是在这个宽度下采的,换算基准。</summary>
        private const float DesignWidth = 720f;

        /// <summary>老端 centerX 型子件:锚父矩形【水平中线】,屏幕变宽时跟随中线走。
        /// x 仍传快照实测的左上原点 x(与 PlaceBottom 同参数),方法内换算成相对中线的偏移。</summary>
        private static void PlaceBottomCenterX(RectTransform rt, float x, float layaY, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x - DesignWidth * 0.5f, -layaY);
        }

        /// <summary>老端 right 型子件:锚父矩形【右边缘】,屏幕变宽时贴右走。
        /// x 仍传快照实测的左上原点 x,方法内换算成相对右缘的偏移。</summary>
        private static void PlaceBottomRight(RectTransform rt, float x, float layaY, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x - DesignWidth, -layaY);
        }

        /// <summary>建一个 72×72 空槽(左上锚/左上枢轴,anchoredPosition 直接取旧运行时公式的等价终态值;
        /// 槽位置全在 prefab 可拖,运行时 MainUISecondaryView 按序填)。sampleCandidates 非空时给槽放一个
        /// Stretch 填满的 Sample 样例图(仅设计期可视,运行时被 ClearDesignTimeSampleIcons 清掉);
        /// 候选图都不存在则不建 Sample(槽保持空,不影响运行时填充)。</summary>
        private static void BuildIconSlot(RectTransform parent, int index, float x, float y, Vector2 anchorPivot,
            string[] sampleCandidates, float w = 72f, float h = 72f)
        {
            RectTransform slot = UiCreatorKit.NewNode("Slot_" + index, parent);
            slot.anchorMin = slot.anchorMax = slot.pivot = anchorPivot; // 左簇左上锚、右簇右下锚(容器内部坐标)
            slot.sizeDelta = new Vector2(w, h);
            slot.anchoredPosition = new Vector2(x, y);
            if (sampleCandidates == null) return;

            foreach (string candidate in sampleCandidates)
            {
                string rel = "resource/game/icon/texture/" + candidate;
                if (!System.IO.File.Exists("Assets/GameRes/" + rel)) continue;
                Image sample = UiCreatorKit.NewImage("Sample", slot);
                UiCreatorKit.Stretch(sample.rectTransform);
                UiCreatorKit.TrySetSprite(sample, rel, UiCreatorKit.Palette.BtnNeutral);
                return;
            }
        }

        // ---- _box_help(168,-21 55x55): 帮助按钮 + 协助提示气泡(默认隐藏) ----
        private static RectTransform BuildBoxHelp(Transform parent, MainUISecondaryView view)
        {
            RectTransform boxHelp = UiCreatorKit.NewNode("GuildHelpButtonBox", parent); // 老端: _box_help
            PlaceBottomCenterX(boxHelp, 168f, -21f, 55f, 55f); // 老端 centerX=-165(实测中心 168+27.5=195.5)

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
            PlaceBottomCenterX(bar, 209f, -104f, 300f, 52f); // 老端 centerX=-1(实测中心 209+150=359)

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

        private static void BuildAutoStateEffectSlot(Transform parent, string slotId, string effectName, string note)
        {
            RectTransform holder = UiCreatorKit.NewNode(slotId, parent);
            UiCreatorKit.Place(holder, 0f, 0f, 32f, 32f);
            UIEffectSlot slot = holder.gameObject.AddComponent<UIEffectSlot>();
            slot.ConfigureEffect(slotId, effectName, GameResPath.GetUIEffectPrefabPath(effectName),
                "yu_client mainUI/MainUISecondaryView.ts:1943-1969", note,
                new Vector2(6.8f, -4f), Vector3.one * 6.4f, 0f);
        }

        // ---- _box_outline_exp(231,-180 257x61): 经验丸 ----
        private static RectTransform BuildOutlineExp(Transform parent, MainUISecondaryView view)
        {
            RectTransform box = UiCreatorKit.NewNode("OnHookExpOrbBox", parent); // 老端: _box_outline_exp
            PlaceBottomCenterX(box, 231f, -180f, 257f, 61f); // 老端 centerX=-1(实测中心 231+128.5=359.5)

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
            PlaceBottomCenterX(box, 231f, -180f, 257f, 61f); // 老端 centerX=-1(与 _box_outline_exp 同位)

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
            PlaceBottomRight(box, 418f, -251f, 70f, 70f); // 老端 right=232(实测右缘 720-(418+70)=232)

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
            PlaceBottomCenterX(box, 321f, -354f, 78f, 78f); // 老端 centerX=0(实测中心 321+39=360)

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
            // 老端无固定驻点位置(靠 SetData 时按目标图标屏幕坐标现场摆放),这里给一个居中偏上的起步默认位
            //(root 已收成底部散布带 → 改锚带底,距底 940 = 原屏幕 y340,位置不变)。
            PlaceAboveBottom(root, 0f, 940f, 201f, 59f);

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
            // 运行态参考坐标(老端 root 552,478 178x71;root 已收成底部散布带 → 锚带底,距底 766.5 = 原屏幕 y513.5)。
            // 老端 right=-10(贴右缘并向右溢出 10):实测中心 360+281=641、右缘 641+89=730,距右 720-730=-10。
            // 原先烤成相对中线 +281 的居中锚,宽屏下会跟着中线走而离开右缘,故改锚右缘。
            // 校验:720 宽时锚点 720、中心 = 720-79 = 641,与原值一致(零位移)。
            PlaceAboveBottomRight(root, 281f, 766.5f, 178f, 71f);

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
            // 本身无固定驻屏坐标;起步默认位=屏幕中心(root 已收成底部散布带 → 锚带底,距底 640)。
            PlaceAboveBottom(root, 0f, 640f, 72f, 72f);

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
