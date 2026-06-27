namespace Shenxiao.Framework.Event
{
    /// <summary>
    /// Global event constants. Append new entries here, do not scatter strings.
    /// Naming: EVT_{MODULE}_{ACTION}.
    /// </summary>
    public static class GlobalEvent
    {
        // ----- Boot -----
        public const string EVT_FRAMEWORK_READY = "EVT_FRAMEWORK_READY";
        public const string EVT_BASE_WINDOW_OPENED = "EVT_BASE_WINDOW_OPENED";
        public const string EVT_BASE_WINDOW_CLOSED = "EVT_BASE_WINDOW_CLOSED";

        // ----- Net -----
        public const string EVT_NET_CONNECTED = "EVT_NET_CONNECTED";
        public const string EVT_NET_DISCONNECTED = "EVT_NET_DISCONNECTED";
        public const string EVT_NET_ERROR = "EVT_NET_ERROR";

        // ----- Login -----
        public const string EVT_LOGIN_SUCCESS = "EVT_LOGIN_SUCCESS";
        public const string EVT_LOGIN_FAIL = "EVT_LOGIN_FAIL";
        public const string EVT_LOGIN_SERVER_SELECTED = "EVT_LOGIN_SERVER_SELECTED";
        /// <summary>游戏服 10000 回包解析完成,参数: roleCount (int)。</summary>
        public const string EVT_GAME_ROLE_LIST = "EVT_GAME_ROLE_LIST";
        /// <summary>创角结果(10003),参数: result (int,1=成功;3 重名/4 敏感字/5 长度/6 已有角色)。</summary>
        public const string EVT_GAME_CREATE_ROLE_RESULT = "EVT_GAME_CREATE_ROLE_RESULT";

        /// <summary>GM 秘籍清单到达(GmCheatController,11100 回包解析完)。</summary>
        public const string EVT_GM_CHEAT_LIST = "EVT_GM_CHEAT_LIST";
        /// <summary>进入游戏成功(10004 result=1)。</summary>
        public const string EVT_GAME_ENTERED = "EVT_GAME_ENTERED";
        public const string EVT_GAME_START_FLAG_READY = "EVT_GAME_START_FLAG_READY";
        public const string EVT_GAME_START = "EVT_GAME_START";

        // ----- Chat -----
        public const string EVT_CHAT_MESSAGES_UPDATED = "EVT_CHAT_MESSAGES_UPDATED";

        // ----- Bag -----
        public const string EVT_BAG_UPDATE = "EVT_BAG_UPDATE";

        // ----- Role -----
        public const string EVT_ROLE_INFO_UPDATE = "EVT_ROLE_INFO_UPDATE";
        /// <summary>主角全量(13001)到齐,可进主城/场景。GameEntryFlow 发。</summary>
        public const string EVT_ROLE_READY = "EVT_ROLE_READY";
        /// <summary>主角战力上升(参数: long 旧战力, long 新战力)。对标老端 mainRoleVo "fighting" 变化 → FightingUpView 弹层。
        /// 由 RoleController.On13033 发,MainUIFlow 监听后弹「战力提升」窗。</summary>
        public const string EVT_COMBAT_POWER_UP = "EVT_COMBAT_POWER_UP";

        // ----- Scene -----
        public const string EVT_SCENE_MAP_READY = "EVT_SCENE_MAP_READY";
        /// <summary>12002 场景快照解析完成、场景对象表(SceneManager)已就绪。携带数据用 SceneManager 的强类型事件。</summary>
        public const string EVT_SCENE_SNAPSHOT_READY = "EVT_SCENE_SNAPSHOT_READY";
        /// <summary>切场景/登出:场景对象表已清空。</summary>
        public const string EVT_SCENE_OBJECTS_CLEARED = "EVT_SCENE_OBJECTS_CLEARED";
        /// <summary>某场景角色的状态字段(隐身/幽灵/分组/PK)变化(12070/12071/12072/12074),渲染层据此刷新表现。</summary>
        public const string EVT_SCENE_ROLE_STATE = "EVT_SCENE_ROLE_STATE";

        // ----- Notice / 公告 -----
        /// <summary>系统公告(11020)到达,读 NoticeModel.LastSysNotice。</summary>
        public const string EVT_SYS_NOTICE = "EVT_SYS_NOTICE";
        /// <summary>传闻广播(11015/11018)到达,读 NoticeModel.RecentChuanwen。</summary>
        public const string EVT_CHUANWEN = "EVT_CHUANWEN";

        // ----- Mail / 邮件 -----
        /// <summary>邮件列表/新邮件(19001/19007)变化,读 MailModel.Mails。</summary>
        public const string EVT_MAIL_LIST_UPDATE = "EVT_MAIL_LIST_UPDATE";
        /// <summary>邮件未读标记(19008)变化,读 MailModel.HasUnread。</summary>
        public const string EVT_MAIL_UNREAD_UPDATE = "EVT_MAIL_UNREAD_UPDATE";

        // ----- FunctionOpen / 功能开放达成奖励 -----
        /// <summary>功能开放达成列表/状态(13800/13801/13802)变化,读 FunctionOpenModel.FinishState。</summary>
        public const string EVT_FUNC_OPEN_UPDATE = "EVT_FUNC_OPEN_UPDATE";

        // ----- FirstRecharge / 首充 -----
        /// <summary>首充信息/状态(15905/15906/15908)变化,读 FirstRechargeModel。</summary>
        public const string EVT_FIRST_RECHARGE_UPDATE = "EVT_FIRST_RECHARGE_UPDATE";

        // ----- WeekCard / 周卡 -----
        /// <summary>周卡信息/奖励(45201/45202/45203)变化,读 WeekCardModel。</summary>
        public const string EVT_WEEK_CARD_UPDATE = "EVT_WEEK_CARD_UPDATE";

        // ----- SurpriseGift / 惊喜礼包 -----
        /// <summary>惊喜礼包信息/抽奖/购买(49000-49004)变化,读 SurpriseGiftModel。</summary>
        public const string EVT_SURPRISE_GIFT_UPDATE = "EVT_SURPRISE_GIFT_UPDATE";

        // ----- Task -----
        public const string EVT_TASK_LIST_UPDATED = "EVT_TASK_LIST_UPDATED";
        public const string EVT_TASK_ONE_UPDATED = "EVT_TASK_ONE_UPDATED";
        /// <summary>点任务项后选中任务变化(携带 taskId),刷新任务栏选中态。对标老端 CLICK_DO_TASK。</summary>
        public const string EVT_TASK_SELECT_CHANGED = "EVT_TASK_SELECT_CHANGED";

        // ----- AutoBrush -----
        public const string EVT_AUTOBRUSH_INFO_UPDATED = "EVT_AUTOBRUSH_INFO_UPDATED";
        public const string EVT_AUTOBRUSH_LEVEL_UPDATED = "EVT_AUTOBRUSH_LEVEL_UPDATED";
        public const string EVT_AUTOBRUSH_STATE_UPDATED = "EVT_AUTOBRUSH_STATE_UPDATED";

        // ----- Skill / 技能(对标老端 SkillManager 的 UPDATE_SKILL_LIST / UPDATE_SKILL_BAR_INFO +
        //        EventName.UPDATE_AUTO_FIGHT_STATE + FightEvent.SKILL_SHORTCUT_CLICK) -----
        /// <summary>技能总表(21002)解析完、shortcutList 重建,读 SkillManager.ShortcutList。</summary>
        public const string EVT_SKILL_LIST_UPDATED = "EVT_SKILL_LIST_UPDATED";
        /// <summary>快捷栏配置(13007)到达、skill_bar_info 更新。</summary>
        public const string EVT_SKILL_BAR_UPDATED = "EVT_SKILL_BAR_UPDATED";
        /// <summary>自动战斗开关变化(参数: bool 是否自动)。对标老端 EventName.UPDATE_AUTO_FIGHT_STATE。注意与自动闯关 AUTOBRUSH 区分。</summary>
        public const string EVT_AUTO_FIGHT_STATE = "EVT_AUTO_FIGHT_STATE";
        /// <summary>自动战斗临时手动模式变化(对标老端 EventName.AUTO_FIGHT_TEMP_MODE)。
        /// 切第三态皮肤 uizjmgj_001a1;触发源(场景拖拽 1.5s)属场景系统,本轮只暴露 setter,差异见报告。</summary>
        public const string EVT_AUTO_FIGHT_TEMP_MODE = "EVT_AUTO_FIGHT_TEMP_MODE";
        /// <summary>点击技能槽派发(参数: skillId:int, attackType:int)。对标老端 FightEvent.SKILL_SHORTCUT_CLICK。
        /// SkillController.PressSkillHandler 据此走 career/obj 三分支;目标型技能进 SceneCombat.MainRoleAttackTarget。</summary>
        public const string EVT_SKILL_SHORTCUT_CLICK = "EVT_SKILL_SHORTCUT_CLICK";
        /// <summary>主角技能释放边界(参数: skillId:int, targetInstanceId:int)。对标老端 FightEvent.RELEASE_MAIN_SKILL。
        /// SceneCombat 在取到真实怪物目标、命中攻击范围、朝向后发此本地等价事件(对标老端 Fire(RELEASE_MAIN_SKILL,...,compress_id))。
        /// 真实服务端攻击请求 20001(h+i×N 怪 + h+l×N 人 + ihhh skill/x/y/angle)经 fight-movie/AOE 碰撞收集链构建,
        /// 本轮不发(不猜格式),只到本地边界 → 下一轮 blocker。</summary>
        public const string EVT_RELEASE_MAIN_SKILL = "EVT_RELEASE_MAIN_SKILL";

        // ----- MainUI -----
        public const string EVT_MAINUI_ACTIVITY_ICON_ADD = "EVT_MAINUI_ACTIVITY_ICON_ADD";
        public const string EVT_MAINUI_ACTIVITY_ICON_DELETE = "EVT_MAINUI_ACTIVITY_ICON_DELETE";
        public const string EVT_MAINUI_ACTIVITY_ICON_UPDATE = "EVT_MAINUI_ACTIVITY_ICON_UPDATE";

        // ----- Res -----
        public const string EVT_RES_UPDATE_PROGRESS = "EVT_RES_UPDATE_PROGRESS";
        public const string EVT_RES_UPDATE_DONE = "EVT_RES_UPDATE_DONE";
    }
}
