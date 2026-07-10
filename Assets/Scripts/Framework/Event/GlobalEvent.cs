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
        /// <summary>使用物品成功(参数: int goods_type_id)。对标老端 GoodsModel.USE_BAG_GOODS_SUCCESS
        /// (On15050 res==1 时 Fire;CongratulationObtainView 等据此刷新)。BagController.On15050 发。</summary>
        public const string EVT_GOODS_USE_SUCCESS = "EVT_GOODS_USE_SUCCESS";
        /// <summary>特殊积分变动(参数: int currency_id;15009 全量重建时为 0)。对标老端
        /// UPDATE_SPECIAL_SCORE / CREATE_SPECIAL_SCORE_FINISH。BagController.On15008/On15009 发。</summary>
        public const string EVT_SPECIAL_SCORE_UPDATE = "EVT_SPECIAL_SCORE_UPDATE";

        // ----- Bag / Goods 协议扩容(自动循环 轮1) -----
        /// <summary>物品详情到达(参数: long goods_id)。15000(自己)/15001(他人,player_id!=自己才落缓存)回包解析完,
        /// 读 GoodsDynamicModel.Peek(goods_id)。BagController.On15000/On15001 发。</summary>
        public const string EVT_GOODS_DETAIL_UPDATE = "EVT_GOODS_DETAIL_UPDATE";
        /// <summary>背包/仓库等容器扩容成功(参数: int pos, int totalCell)。15002 code==1 时发,
        /// 读 BagModel.GetMaxCell(pos)。BagController.On15002 发。</summary>
        public const string EVT_BAG_MAX_CELL = "EVT_BAG_MAX_CELL";
        /// <summary>物品分解成功(参数: List&lt;(long goodsId,long goodsNum)&gt; rewardList,仅展示不落 BagModel)。
        /// 15019(主动分解)与 15090(自动分解提示,老端两号共用同一事件)都发此事件。
        /// BagController.On15019/On15090 发。</summary>
        public const string EVT_GOODS_DECOMPOSE_SUCCESS = "EVT_GOODS_DECOMPOSE_SUCCESS";
        /// <summary>兑换列表变动(参数: int exchangeType)。15026 回包解析完,读 GoodsExchangeModel.GetList(type)。
        /// BagController.On15026 发。</summary>
        public const string EVT_GOODS_EXCHANGE_LIST = "EVT_GOODS_EXCHANGE_LIST";
        /// <summary>兑换/购买/合成(15022)成功(参数: long id,即请求时传入的兑换规则 id)。
        /// BagController.On15022 发。</summary>
        public const string EVT_GOODS_EXCHANGE_DONE = "EVT_GOODS_EXCHANGE_DONE";
        /// <summary>过期物品列表到达(15027 opr==1,无参,读 GoodsExpiredModel.List)。BagController.On15027 发。</summary>
        public const string EVT_GOODS_EXPIRED_LIST = "EVT_GOODS_EXPIRED_LIST";
        /// <summary>场景掉落拾取成功(参数: BagController.DropPickVo vo,15053 res==1)。BagController.On15053 发。</summary>
        public const string EVT_DROP_PICK_SUCCESS = "EVT_DROP_PICK_SUCCESS";
        /// <summary>场景掉落进入拾取计时(参数: BagController.DropPickVo vo,15053 status==1)。BagController.On15053 发。</summary>
        public const string EVT_DROP_PICK_BEGIN = "EVT_DROP_PICK_BEGIN";
        /// <summary>场景掉落包已消失(参数: long dropId,15053 res==1500020)。BagController.On15053 发。</summary>
        public const string EVT_DROP_DISMISS = "EVT_DROP_DISMISS";
        /// <summary>场景掉落拾取失败/不可拾取(参数: long dropId,15053 其余分支)。BagController.On15053 发。</summary>
        public const string EVT_DROP_PICK_FAIL = "EVT_DROP_PICK_FAIL";
        /// <summary>拾取掉落包顺序列表到达(15088,无参,读 DropOrderModel.DropIdList)。BagController.On15088 发。</summary>
        public const string EVT_DROP_ORDER_LIST = "EVT_DROP_ORDER_LIST";
        /// <summary>本人物品 buff 列表变动(15055,无参,读 GoodsBuffModel.List;仅 player_id==自己的回包才更新数据,
        /// 但事件无条件发,对标老端无条件 Fire)。BagController.On15055 发。</summary>
        public const string EVT_GOODS_BUFF_UPDATE = "EVT_GOODS_BUFF_UPDATE";
        /// <summary>礼包等级信息到达(参数: GiftLevelInfo vo,15083)。BagController.On15083 发。</summary>
        public const string EVT_GIFT_LEVEL_INFO = "EVT_GIFT_LEVEL_INFO";
        /// <summary>次数礼包冷却信息变动(参数: long goodsId,15084;读 GoodsCoolingModel.Get(goodsId))。
        /// BagController.On15084 发。</summary>
        public const string EVT_GOODS_COOLING_UPDATE = "EVT_GOODS_COOLING_UPDATE";
        /// <summary>礼包卡兑换结果(参数: bool success, List&lt;(int style,int typeId,int count)&gt; rewards;
        /// 失败时 rewards 为 null)。15087,可能异步再推一次。BagController.On15087 发。</summary>
        public const string EVT_GIFT_CARD_RESULT = "EVT_GIFT_CARD_RESULT";
        /// <summary>物品预览战力到达(参数: int goodsTypeId, long expectPower,15089;幻化 tooltip 用)。
        /// BagController.On15089 发。</summary>
        public const string EVT_GOODS_EXPECT_POWER = "EVT_GOODS_EXPECT_POWER";

        // ----- Partner(剑魄同修) -----
        /// <summary>同修数据变动(14202 全量/14201 单个/14205 培养/14204 激活后)。对标老端 PartnerModel.UPDATE_VIEW。</summary>
        public const string EVT_PARTNER_UPDATE = "EVT_PARTNER_UPDATE";

        // ----- SuitCollect(套装收集)/ RushGift(冲级豪礼)/ OutWard(幻化外观) -----
        /// <summary>套装收集数据变动(15256 全量/15257 激活后)。</summary>
        public const string EVT_SUIT_CLT_UPDATE = "EVT_SUIT_CLT_UPDATE";
        /// <summary>冲级豪礼状态变动(41700 列表/41701 领取后)。</summary>
        public const string EVT_RUSH_GIFT_UPDATE = "EVT_RUSH_GIFT_UPDATE";
        /// <summary>幻化外观数据变动(参数: int type_id;16002 阶星/16023 升星/16028 等级面板/16029 升级后)。</summary>
        public const string EVT_OUTWARD_UPDATE = "EVT_OUTWARD_UPDATE";
        /// <summary>天命觉醒状态变动(42909 前置态/42900 完成后)。</summary>
        public const string EVT_TEMPLE_AWAKEN_UPDATE = "EVT_TEMPLE_AWAKEN_UPDATE";
        /// <summary>装备强化数据变动(15204 查询/15205 强化后)。</summary>
        public const string EVT_EQUIP_STREN_UPDATE = "EVT_EQUIP_STREN_UPDATE";
        /// <summary>古宝数据变动(13320 全量/13321 激活后)。</summary>
        public const string EVT_GUBAO_UPDATE = "EVT_GUBAO_UPDATE";
        /// <summary>副本状态变动(61020 状态/61001 进入回包/61013 结算)。</summary>
        public const string EVT_DUNGEON_UPDATE = "EVT_DUNGEON_UPDATE";
        /// <summary>符文数据变动(16700 全量/16701 镶嵌后)。</summary>
        public const string EVT_RUNE_UPDATE = "EVT_RUNE_UPDATE";
        /// <summary>结社数据变动(40001 列表/40003 申请/40004 创建回包)。</summary>
        public const string EVT_GUILD_UPDATE = "EVT_GUILD_UPDATE";
        /// <summary>神装合成数据变动(15020 合成回包后)。收尾三件套(第20轮工单)。</summary>
        public const string EVT_COMPOSE_UPDATE = "EVT_COMPOSE_UPDATE";
        /// <summary>排位赛数据变动(28001 页面信息/28002 随机对手/28003 挑战回包后)。收尾三件套(第20轮工单)。</summary>
        public const string EVT_JJC_UPDATE = "EVT_JJC_UPDATE";

        // ----- Role -----
        public const string EVT_ROLE_INFO_UPDATE = "EVT_ROLE_INFO_UPDATE";
        /// <summary>主角全量(13001)到齐,可进主城/场景。GameEntryFlow 发。</summary>
        public const string EVT_ROLE_READY = "EVT_ROLE_READY";
        /// <summary>主角战力上升(参数: long 旧战力, long 新战力)。对标老端 mainRoleVo "fighting" 变化 → FightingUpView 弹层。
        /// 由 RoleController.On13033 发,MainUIFlow 监听后弹「战力提升」窗。</summary>
        public const string EVT_COMBAT_POWER_UP = "EVT_COMBAT_POWER_UP";
        /// <summary>主角 PK(战斗)模式变化(进场自块同步/12074 主角广播/13012 切换成功)。读 RoleModel.PkStatus。
        /// 对标老端 mainRoleVo "pk_status" 变化 → MainUITopView.RefreshPkStatus。</summary>
        public const string EVT_PK_STATUS_CHANGED = "EVT_PK_STATUS_CHANGED";
        /// <summary>13012 主动切换成功(区别于被动同步)。FightMode 弹窗据此提示「切换成功」并关闭
        /// (对标老端 PkStatusModel.CHANGE_SUCCESS)。</summary>
        public const string EVT_PK_CHANGE_SUCCESS = "EVT_PK_CHANGE_SUCCESS";

        // ----- Fight 扩容 / Relive(自动循环 队列#2 轮2;200xx,yu_client FightController.ts + commonController/ReliveController.ts) -----
        /// <summary>主角死亡(参数无;FightController.On20013 死亡广播解析完发,20022 主角死亡分支刻意不发——
        /// 对标老端弹窗信号只认 Fire(SHOWRELIVEWINDOW,0) 来自 20013)。ReliveController 订阅:停自动战斗+
        /// 播死亡动作+按场景路由开复活窗。</summary>
        public const string EVT_ROLE_DEAD = "EVT_ROLE_DEAD";
        /// <summary>复活成功(参数: int type,即请求时的 relive_mode 回传)。20004 flag==1 或 12(REVIVE_BOSS/ASHES
        /// 改写)时发。ReliveController.On20004 发,MainUIReliveView 订阅关窗。</summary>
        public const string EVT_RELIVE_SUCCESS = "EVT_RELIVE_SUCCESS";
        /// <summary>复活信息到达(参数: long nextReviveTime,服务器时间戳)。20009 回包解析完发,供服务端强控
        /// 副本(经验本/装备本/龙宫本/心域本)复活面板刷倒计时。ReliveController.On20009 发。</summary>
        public const string EVT_RELIVE_INFO = "EVT_RELIVE_INFO";
        /// <summary>5分钟回城复活次数/疲劳信息(参数: int reviveNum, long endTime)。20017 回包/主动推送解析完发。
        /// ReliveController.On20017 发。</summary>
        public const string EVT_RELIVE_TIRED = "EVT_RELIVE_TIRED";
        /// <summary>buff 技能清理广播(参数: int typeFlag, long roleId, List&lt;(int buffType,int buffSkillId)&gt; list)。
        /// 20007 回包解析完发;消费方(buff UI)未接线,TODO。FightController.On20007 发。</summary>
        public const string EVT_BUFF_CLEARED = "EVT_BUFF_CLEARED";
        /// <summary>清理刚放技能CD(参数: int skillId)。20018 回包解析完发。FightController.On20018 发。</summary>
        public const string EVT_SKILL_CD_CLEAR = "EVT_SKILL_CD_CLEAR";
        /// <summary>技能CD结束时间通知(参数: int skillId, long endTime)。20027 回包解析完发(老端单条,非数组)。
        /// FightController.On20027 发。</summary>
        public const string EVT_SKILL_CD_END = "EVT_SKILL_CD_END";
        /// <summary>触发技能列表(参数: List&lt;int&gt; skillIds,伙伴/联携技能表现)。20028 回包解析完发。
        /// FightController.On20028 发。</summary>
        public const string EVT_TRIGGER_SKILLS = "EVT_TRIGGER_SKILLS";
        /// <summary>战斗能量更新(参数: int energy)。20023 回包解析完发(老端事件名拼写 UPDATE_FIGHT_ENEERGY
        /// 少个R,系老端笔误,本事件不沿用错误拼写)。FightController.On20023 发。</summary>
        public const string EVT_FIGHT_ENERGY = "EVT_FIGHT_ENERGY";
        /// <summary>怪物归属变化(参数: long monId, long ownerRoleId)。20020 抢夺成功 / 20021 查看归属回包解析完发。
        /// FightController.On20020/On20021 发。</summary>
        public const string EVT_MON_OWNER_UPDATE = "EVT_MON_OWNER_UPDATE";
        /// <summary>拾取怪物结果(参数: List&lt;(int errCode,int monId)&gt; results)。20010 回包解析完发。
        /// FightController.On20010 发。</summary>
        public const string EVT_PICK_MON_RESULT = "EVT_PICK_MON_RESULT";
        /// <summary>击杀信息推送(参数: string name, int isShowPkV, int pkValue)。20014 回包解析完发(老端无对应
        /// recv 实现,按服务端权威 pt_200.erl:155-157 write 序解析)。FightController.On20014 发。</summary>
        public const string EVT_KILL_INFO = "EVT_KILL_INFO";
        /// <summary>广播 PK 值(参数: long roleId, int pkValue)。20015 回包解析完发(老端无对应 recv 实现,
        /// 按服务端权威 pt_200.erl:160-161 write 序解析;规格草案假设 "l,i" 与服务端源码 PkValue:16 冲突,
        /// 已按服务端为准改 "l,h",见汇报偏差项)。FightController.On20015 发。</summary>
        public const string EVT_PK_VALUE_UPDATE = "EVT_PK_VALUE_UPDATE";
        /// <summary>模拟战斗死亡广播(参数: long killerId, long diedId)。20022 回包解析完发。
        /// FightController.On20022 发。</summary>
        public const string EVT_SIMULATE_FIGHT = "EVT_SIMULATE_FIGHT";

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

        // ----- 技能成长线(自动循环 轮3;21001/21010-12/13008/13010/12093/18401/20006) -----
        /// <summary>被动技能升级成功(参数: skillId:int)。21001 errcode==1 时发(服务端会自动补推 21002 刷新列表,
        /// 不在此处手动重拉)。SkillController.On21001 发。</summary>
        public const string EVT_SKILL_LEVEL_UP = "EVT_SKILL_LEVEL_UP";
        /// <summary>天赋技能面板全量到达/刷新(无参,读 SkillTalentModel)。21010 回包解析完发。
        /// SkillController.On21010 发。</summary>
        public const string EVT_TALENT_INFO = "EVT_TALENT_INFO";
        /// <summary>天赋技能学习成功(参数: skillId:int, skillLv:int)。21011 errcode==1 时发(成功后老端补发 21010
        /// 刷全量,本端同样补发)。SkillController.On21011 发。</summary>
        public const string EVT_TALENT_LEARNED = "EVT_TALENT_LEARNED";
        /// <summary>天赋技能重置成功(无参)。21012 errcode==1 时发(服务端会主动重放 21010)。
        /// SkillController.On21012 发。</summary>
        public const string EVT_TALENT_RESET = "EVT_TALENT_RESET";
        /// <summary>职业技能给予的 buff 列表到达(无参,读 SkillTalentModel.CareerSkillBuffList)。12093 回包解析完发
        /// (纯被动推送,客户端不主动请求)。SkillController.On12093 发。</summary>
        public const string EVT_CAREER_SKILL_BUFF = "EVT_CAREER_SKILL_BUFF";
        /// <summary>模块加成效果列表到达(无参,读 SkillTalentModel 的 18401 泛用 dict / OnhookExtraSec / LifeSkillAdd)。
        /// 18401 回包解析完发。SkillController.On18401 发。</summary>
        public const string EVT_MODULE_BUFF_LIST = "EVT_MODULE_BUFF_LIST";
        /// <summary>辅助技能广播到达(参数: Scene.Vo.AssistVo vo)。20006 回包解析完发(两段式表现的"广播权威表现"段)。
        /// FightController.On20006 发。</summary>
        public const string EVT_ASSIST_SKILL = "EVT_ASSIST_SKILL";
        /// <summary>快捷栏保存/替换成功(参数: bool isSwap,true=13010替换/false=13008保存)。State==1 时发,随后重拉 13007。
        /// SkillController.On13008/On13010 发。</summary>
        public const string EVT_QUICKBAR_SAVED = "EVT_QUICKBAR_SAVED";

        // ----- MainUI -----
        public const string EVT_MAINUI_ACTIVITY_ICON_ADD = "EVT_MAINUI_ACTIVITY_ICON_ADD";
        public const string EVT_MAINUI_ACTIVITY_ICON_DELETE = "EVT_MAINUI_ACTIVITY_ICON_DELETE";
        public const string EVT_MAINUI_ACTIVITY_ICON_UPDATE = "EVT_MAINUI_ACTIVITY_ICON_UPDATE";
        // 头号玩家主界面数据就绪(对标老端 EventName.UPDATE_TOP_PLAYER_MAIN_DATA),参数:rank_type:int。
        public const string EVT_TOPPLAYER_MAIN_DATA = "EVT_TOPPLAYER_MAIN_DATA";
        // 循环冲榜数据更新(22700 有活动 / 22702 榜单 / 22706 榜首变更)→ 活动视图刷 _box_rank 竞榜展示(3D模型+名次+倒计时)。无参。
        public const string EVT_CYCLEIMP_DATA = "EVT_CYCLEIMP_DATA";
        // 循环冲榜关闭(22700 type/subtype=0)→ 活动视图收起 _box_rank 竞榜展示,放行头号玩家分支。无参。
        public const string EVT_CYCLEIMP_CLOSE = "EVT_CYCLEIMP_CLOSE";
        // 太极收起/展开活动图标(对标老端 MainUIModel.CHANGE_ACTIVITY_STATE):单按钮同时驱动 ActivityView 与
        // SecondaryView 两簇图标收放。参数:bool folded(true=已收起)。
        public const string EVT_MAINUI_ACTIVITY_FOLD = "EVT_MAINUI_ACTIVITY_FOLD";

        // ----- Collect / 采集 -----
        /// <summary>主角在采集中开始移动 → 取消采集(对标老端 CollectBarView 监听 MAINROLE_MOVE_EVENT_IMME →
        /// REQUEST_TO_COLLECT flag=3)。MainRoleAgent 起步时若处于采集态发此事件,CollectController 据此向服务端发取消。无参。</summary>
        public const string EVT_COLLECT_MOVE_CANCEL = "EVT_COLLECT_MOVE_CANCEL";
        /// <summary>一次采集非成功终止(失败/取消/采集物被移除/START 超时)。CollectController 发,TaskModel 据此延时重试
        /// 当前采集任务(对标老端 FindNextOne)。采集成功(flag=2)不发此事件——由服务端 30001 推进任务驱动。无参。</summary>
        public const string EVT_COLLECT_ENDED = "EVT_COLLECT_ENDED";

        // ----- Setting / 设置 -----
        /// <summary>设置数据变化(10202 全量到达 / 10203 写回成功落地),读 SettingModel。
        /// 对标老端 SettingModel.UPDATE_SETTING_INFO / UPDATE_CONTENT。</summary>
        public const string EVT_SETTING_UPDATED = "EVT_SETTING_UPDATED";

        // ----- Res -----
        public const string EVT_RES_UPDATE_PROGRESS = "EVT_RES_UPDATE_PROGRESS";
        public const string EVT_RES_UPDATE_DONE = "EVT_RES_UPDATE_DONE";
    }
}
