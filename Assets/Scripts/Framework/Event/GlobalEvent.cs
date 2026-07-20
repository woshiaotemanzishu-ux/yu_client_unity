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

        // ----- Chat 补全(自动循环 轮6;11001私聊/11023/11025/11027/11028/11029/11042/11046/11050) -----
        /// <summary>私聊桶变化(参数 long targetId,即会话对方 role_id):新消息到达或未读数变化。
        /// 对标老端 CHAT_MSG_UPDATE/CHAT_MSG_NUM_UPDATE(本端合并为一个事件)。ChatModel.AddPrivateMessage/
        /// ClearPrivateUnread 发;FriendChatView 等私聊窗口(未移植)消费方 TODO。</summary>
        public const string EVT_CHAT_PRIVATE_UPDATE = "EVT_CHAT_PRIVATE_UPDATE";
        /// <summary>11046 清理:私聊桶被整体清空(参数 long roleId)。对标老端 CLEAR_ROLE_PRIVATE。
        /// ChatModel.ClearRoleChatData 发。</summary>
        public const string EVT_CHAT_PRIVATE_CLEARED = "EVT_CHAT_PRIVATE_CLEARED";
        /// <summary>11046 清理:公共频道里含该玩家的消息已删除(参数 long roleId)。对标老端 CLEAR_ROLE_DATA。
        /// ChatModel.ClearRoleChatData 发(实际刷新走逐频道 <see cref="EVT_CHAT_MESSAGES_UPDATED"/>,此事件仅供
        /// 断言/统计消费)。</summary>
        public const string EVT_CHAT_ROLE_DATA_CLEARED = "EVT_CHAT_ROLE_DATA_CLEARED";
        /// <summary>11028 查看私聊玩家信息到达(参数 ChatModel.PrivatePlayerInfo)。对标老端 PRIVATE_CHAT_PLAYER_INFO。
        /// ChatController.On11028 发;私聊窗口头部(未移植)消费方 TODO。</summary>
        public const string EVT_CHAT_PRIVATE_PLAYER_INFO = "EVT_CHAT_PRIVATE_PLAYER_INFO";
        /// <summary>11029 喇叭广播到达(参数 ChatMessage,已附带写入对应公共频道桶)。ChatController.On11029 发;
        /// 喇叭横幅/跑马灯 UI(ChatTrumpetMenu 等,r6_unity 现状为死窗口)消费方 TODO。</summary>
        public const string EVT_CHAT_HORN_RECEIVED = "EVT_CHAT_HORN_RECEIVED";
        /// <summary>11023 小跨服聊天开关变化(参数 bool isOpen),读 ChatModel.IsZoneOpen。ChatModel.SetZoneOpen 发。</summary>
        public const string EVT_CHAT_ZONE_OPEN_CHANGED = "EVT_CHAT_ZONE_OPEN_CHANGED";
        /// <summary>11025 上传跨服频道物品失败(error_code!=1),对标老端 SHOW_SPECIAL_CHANNEL_GOODS
        /// ("跨服无法查看物品需先上传"提示)。ChatController.On11025 发;提示 UI 消费方 TODO。</summary>
        public const string EVT_CHAT_SPECIAL_GOODS_BLOCKED = "EVT_CHAT_SPECIAL_GOODS_BLOCKED";
        /// <summary>11050 公告全量到达/重建(无参,读 ChatModel.Notices)。ChatModel.SetNoticeList 发。</summary>
        public const string EVT_CHAT_NOTICE_LIST_UPDATED = "EVT_CHAT_NOTICE_LIST_UPDATED";
        /// <summary>某条公告轮到展示(参数 ChatModel.NoticeEntry),对标老端 CheckGongGaoFunc。
        /// ChatModel.PumpNotice 发;跑马灯/传闻 UI 消费方 TODO。</summary>
        public const string EVT_CHAT_NOTICE_TRIGGERED = "EVT_CHAT_NOTICE_TRIGGERED";
        /// <summary>11063 鲜花特效入队(参数 string effectName)。表现层只消费服务端原样下发的资源名。</summary>
        public const string EVT_CHAT_FLOWER_EFFECT = "EVT_CHAT_FLOWER_EFFECT";
        /// <summary>11016 跨系统红点推送到达(轮21 PF 补漏批;参数: int moduleId, int type, int num)。
        /// moduleId==339(红包,需先判断本人已入公会)/moduleId==400&amp;&amp;type==1(公会申请数)。ChatController.On11016
        /// 发;RedPacket 模块与公会红点体系均不在聊天包所有权范围内,本事件只做跨模块通知,真消费方(RedPacketModel/
        /// 公会红点)接线时按 moduleId 分流订阅,TODO。</summary>
        public const string EVT_CHAT_RED_DOT_PUSH = "EVT_CHAT_RED_DOT_PUSH";

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
        /// <summary>幻化外观数据变动(⚠无参,Emit() 零参数——订阅方 OutWardShellView.Rebuild/
        /// OutWardBaseView.OnOutWardUpdate 均为无参 Action,回调里自行按当前 _typeId 重读 Model;
        /// 16002 阶星/16023 升星/16028 等级面板/16029 升级/16024 自动购买切换后触发)。</summary>
        public const string EVT_OUTWARD_UPDATE = "EVT_OUTWARD_UPDATE";
        // ----- OutWard 幻化(Illusion,pt_160,轮24 PI)-----
        /// <summary>幻化家族错误出口(参数: int errcode;16000,errcode!=1600023 的一般错误)。</summary>
        public const string EVT_OUTWARD_ERROR = "EVT_OUTWARD_ERROR";
        /// <summary>幻化激活数量已达上限(参数: int errcode==1600023;16000 特判,对标老端 PET_ACTIVE_LIMIT)。</summary>
        public const string EVT_OUTWARD_ACTIVE_LIMIT = "EVT_OUTWARD_ACTIVE_LIMIT";
        /// <summary>场景外观变化广播落地(参数: int typeId, long roleId;16001,S2C only)。Unity 场景暂无角色
        /// 外观渲染消费方——TODO 消费方:场景角色模型换装/骑乘姿态(对标老端 role_vo.SetFigureId+
        /// SetFigureRideState)。</summary>
        public const string EVT_OUTWARD_SCENE_FIGURE_CHANGE = "EVT_OUTWARD_SCENE_FIGURE_CHANGE";
        /// <summary>幻化穿戴/取消成功(参数: int typeId;16003)。</summary>
        public const string EVT_OUTWARD_ILLUSION_WEAR = "EVT_OUTWARD_ILLUSION_WEAR";
        /// <summary>上/下坐骑结果落地(参数: int typeId, int type[0=下/1=上];16004)。TODO 消费方:坐骑骑乘动画
        /// (对标老端仅 Horse 触发 HorseChange)。</summary>
        public const string EVT_OUTWARD_RIDE_TOGGLE = "EVT_OUTWARD_RIDE_TOGGLE";
        /// <summary>幻化形象列表变动(参数: int typeId;16006 全量/16008 激活后/16009 升阶后/16020 升星后补拉/
        /// 16012 到期删除后补拉)。</summary>
        public const string EVT_OUTWARD_ILLUSION_LIST_UPDATE = "EVT_OUTWARD_ILLUSION_LIST_UPDATE";
        /// <summary>幻化形象详情缓存更新(参数: int typeId, int figureId;16007)。</summary>
        public const string EVT_OUTWARD_FIGURE_DETAIL_UPDATE = "EVT_OUTWARD_FIGURE_DETAIL_UPDATE";
        /// <summary>幻化激活成功(参数: int typeId, int figureId;16008,老端开 OutwardChangedView 庆祝页,
        /// Unity 暂无该 UI,先落数据 Emit 事件)。</summary>
        public const string EVT_OUTWARD_FIGURE_ACTIVATED = "EVT_OUTWARD_FIGURE_ACTIVATED";
        /// <summary>幻化升阶成功(参数: int typeId, int figureId;16009)。</summary>
        public const string EVT_OUTWARD_FIGURE_STAGE_UP = "EVT_OUTWARD_FIGURE_STAGE_UP";
        /// <summary>幻化升星成功(参数: int typeId, int figureId;16020)。</summary>
        public const string EVT_OUTWARD_FIGURE_STAR_UP = "EVT_OUTWARD_FIGURE_STAR_UP";
        /// <summary>幻化到期删除(参数: int typeId, int figureId;16012,S2C only)。</summary>
        public const string EVT_OUTWARD_FIGURE_EXPIRED = "EVT_OUTWARD_FIGURE_EXPIRED";
        /// <summary>魔晶使用/次数变动(参数: int typeId;16010 使用后/16011 次数列表)。</summary>
        public const string EVT_OUTWARD_CRYSTAL_UPDATE = "EVT_OUTWARD_CRYSTAL_UPDATE";
        /// <summary>幻化战力预览(参数: int typeId, int figureId;16022,老端不落任何列表,瞬时值只经事件传递——
        /// 具体数值读 OutWardModel.LastFightPreview)。</summary>
        public const string EVT_OUTWARD_FIGHT_PREVIEW = "EVT_OUTWARD_FIGHT_PREVIEW";
        /// <summary>幻化升星战力预览(参数: int typeId, int figureId;16027,同上瞬时语义,数值读
        /// OutWardModel.LastStarFightPreview)。</summary>
        public const string EVT_OUTWARD_STAR_FIGHT_PREVIEW = "EVT_OUTWARD_STAR_FIGHT_PREVIEW";
        // ----- PetEquip 坐骑/伙伴装备(pt_160,轮25A) -----
        /// <summary>装备数据变化(参数:int typeId；16014全量/16015成功回拉/16016或16017成功更新)。</summary>
        public const string EVT_PET_EQUIP_UPDATE = "EVT_PET_EQUIP_UPDATE";
        /// <summary>宠物装备四容器变化(参数:int pos；15010全量/15017或15018增量，pos=22/32/23/33)。</summary>
        public const string EVT_PET_EQUIP_BAG_UPDATE = "EVT_PET_EQUIP_BAG_UPDATE";
        /// <summary>装备强化跨级成功(无参数；16016 level 真变化时触发，对标老端 UPGRADE_SUCCESS)。</summary>
        public const string EVT_PET_EQUIP_STRENGTH_SUCCESS = "EVT_PET_EQUIP_STRENGTH_SUCCESS";
        /// <summary>装备打磨成功(无参数；16017，对标老端 STAR_SUCCESS)。</summary>
        public const string EVT_PET_EQUIP_STAR_SUCCESS = "EVT_PET_EQUIP_STAR_SUCCESS";
        /// <summary>天命觉醒状态变动(42909 前置态/42900 完成后)。</summary>
        public const string EVT_TEMPLE_AWAKEN_UPDATE = "EVT_TEMPLE_AWAKEN_UPDATE";
        /// <summary>装备强化数据变动(15204 查询/15205 强化后)。</summary>
        public const string EVT_EQUIP_STREN_UPDATE = "EVT_EQUIP_STREN_UPDATE";
        /// <summary>神兵淬炼(精炼)数据变动(15250 查询/15251 精炼后)。自动循环 轮4 队列#4。</summary>
        public const string EVT_EQUIP_SMELT_UPDATE = "EVT_EQUIP_SMELT_UPDATE";
        /// <summary>吞天洗魄数据变动(15212 开槽/15213 洗魄/15214 免费次数/15252 升段后)。自动循环 轮4 队列#4。</summary>
        public const string EVT_EQUIP_WASH_UPDATE = "EVT_EQUIP_WASH_UPDATE";
        /// <summary>神屠九炼(神炼)数据变动(15255 执行后)。自动循环 轮4 队列#4。</summary>
        public const string EVT_EQUIP_REFINEMENT_UPDATE = "EVT_EQUIP_REFINEMENT_UPDATE";
        /// <summary>全身奖励数据变动(15260 激活/15261 列表后;淬炉宗师 type=1 与骸珀镶嵌大师 type=3 共用)。自动循环 轮4 队列#4。</summary>
        public const string EVT_EQUIP_WHOLE_UPDATE = "EVT_EQUIP_WHOLE_UPDATE";
        /// <summary>宝石(骸珀镶嵌)雕刻数据变动(15210 查询/15211 雕刻后)。自动循环 轮4 下半(4b)。
        /// 镶嵌/拆除(15208/09,EquipStoneController)仍复用既有 EVT_EQUIP_STREN_UPDATE,Jewel UI 需同时订阅两个事件。</summary>
        public const string EVT_EQUIP_JEWEL_UPDATE = "EVT_EQUIP_JEWEL_UPDATE";
        /// <summary>古宝数据变动(13320 全量/13321 激活后)。</summary>
        public const string EVT_GUBAO_UPDATE = "EVT_GUBAO_UPDATE";
        /// <summary>副本状态变动(61020 状态/61001 进入回包/61013 结算)。</summary>
        public const string EVT_DUNGEON_UPDATE = "EVT_DUNGEON_UPDATE";

        // ----- 副本家族补全一期(自动循环 轮9) -----
        /// <summary>61004 副本信息推送落地(读 DungeonModel.SceneInfo;对标老端 UPDATE_DUNGEON_INFO)。</summary>
        public const string EVT_DUNGEON_INFO_UPDATE = "EVT_DUNGEON_INFO_UPDATE";
        /// <summary>61018 退出倒计时(参数: int end_time;仅 type==1 才发,对标老端 UPDATE_DUNGEON_END_TIME)。</summary>
        public const string EVT_DUNGEON_END_TIME = "EVT_DUNGEON_END_TIME";
        /// <summary>61030 下一波怪物时间(参数: int wave_num, int time)。</summary>
        public const string EVT_DUNGEON_NEXT_WAVE = "EVT_DUNGEON_NEXT_WAVE";
        /// <summary>61011 助战剩余次数落地(参数: int dun_id;读 DungeonModel.GetHelpCount)。</summary>
        public const string EVT_DUNGEON_HELP_COUNT = "EVT_DUNGEON_HELP_COUNT";
        /// <summary>61021 购买次数成功(参数: int dun_id, int dun_type;对标老端 UPDATE_DUNGEON_TIME)。</summary>
        public const string EVT_DUNGEON_BUY_SUCCESS = "EVT_DUNGEON_BUY_SUCCESS";
        /// <summary>61023 时间评分状态(读 DungeonModel.ScoreState;对标老端 NOW_TIME_SCORE_STATE)。</summary>
        public const string EVT_DUNGEON_SCORE_STATE = "EVT_DUNGEON_SCORE_STATE";
        /// <summary>61025/61026 鼓舞状态变化(参数: bool showToast——61025 成功那侧 true;对标老端 UPDATE_INSPRITE_INFO)。</summary>
        public const string EVT_DUNGEON_INSPIRIT_UPDATE = "EVT_DUNGEON_INSPIRIT_UPDATE";
        /// <summary>61121 资源副本次数落地(参数: int dun_type,0=全量;读 DungeonModel.GetResourceCount)。</summary>
        public const string EVT_DUNGEON_RESOURCE_COUNT = "EVT_DUNGEON_RESOURCE_COUNT";
        /// <summary>61009 剧情触发推送(参数: int story_id, int sub_story_id;对标老端 STORY_PLAY_TRIGGER,
        /// 剧情播放系统未移植,先发事件供后续 Story 通道消费)。</summary>
        public const string EVT_DUNGEON_STORY_TRIGGER = "EVT_DUNGEON_STORY_TRIGGER";
        /// <summary>50801 周本信息落地(读 PolarModel.WeekInfos;对标老端 POLAR_DATA_RETURN)。</summary>
        public const string EVT_POLAR_DATA = "EVT_POLAR_DATA";
        /// <summary>50802 周本榜单落地(参数: int team_dun_id;读 PolarModel.GetRank;对标老端 POLAR_RANK_DATA_RETURN)。</summary>
        public const string EVT_POLAR_RANK_DATA = "EVT_POLAR_RANK_DATA";
        /// <summary>符文数据变动(16700 全量/16701 镶嵌后)。</summary>
        public const string EVT_RUNE_UPDATE = "EVT_RUNE_UPDATE";
        /// <summary>结社数据变动(40001 列表/40003 申请/40004 创建回包)。</summary>
        public const string EVT_GUILD_UPDATE = "EVT_GUILD_UPDATE";
        /// <summary>公会核心一期(轮13a):40005 基础信息落地(读 GuildModel.Info)。</summary>
        public const string EVT_GUILD_INFO_UPDATE = "EVT_GUILD_INFO_UPDATE";
        /// <summary>40006 成员列表落地(读 GuildModel.Members)。</summary>
        public const string EVT_GUILD_MEMBER_UPDATE = "EVT_GUILD_MEMBER_UPDATE";
        /// <summary>40008/40009/40016 申请列表变动(读 GuildModel.Applies)。</summary>
        public const string EVT_GUILD_APPLY_UPDATE = "EVT_GUILD_APPLY_UPDATE";
        /// <summary>40008 由"查看申请"按钮触发(GuildModel.ApplyRequestMark)且列表非空到达——自动开申请弹层
        /// (对标老端 on40008 里 apply_request_mark 分支);为空时不发这个事件,改为 toast"当前没有申请信息"。</summary>
        public const string EVT_GUILD_APPLY_AUTO_OPEN = "EVT_GUILD_APPLY_AUTO_OPEN";
        /// <summary>共享错误壳 40000 到达(参数: int errorCode)。</summary>
        public const string EVT_GUILD_ERROR = "EVT_GUILD_ERROR";
        /// <summary>公会核心一期其余数据变动统称(权限/技能/声望/合并候选/改名信息/捐献/活跃度等,
        /// 本轮未各自建 UI 消费点,统一一个事件供以后按需订阅,避免逐号建事件)。</summary>
        public const string EVT_GUILD_DATA_UPDATE = "EVT_GUILD_DATA_UPDATE";
        /// <summary>公会二期(轮13b):结社仓库数据变动(40101/102/103/104/105/106/107/108/110,读 GuildModel.DepotGoods/DepotScore)。</summary>
        public const string EVT_GUILD_DEPOT_UPDATE = "EVT_GUILD_DEPOT_UPDATE";
        /// <summary>结社宝箱数据变动(40301/302/303/304/305,读 GuildModel.BoxSendList/BoxLog)。</summary>
        public const string EVT_GUILD_BOX_UPDATE = "EVT_GUILD_BOX_UPDATE";
        /// <summary>结社协助数据变动(40401-410,读 GuildModel.AssistList/CurrentMyAssist)。</summary>
        public const string EVT_GUILD_ASSIST_UPDATE = "EVT_GUILD_ASSIST_UPDATE";
        /// <summary>结社武魂/神像数据变动(40500-509,读 GuildModel.GodList/GetGodDetail)。</summary>
        public const string EVT_GUILD_GOD_UPDATE = "EVT_GUILD_GOD_UPDATE";
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

        // ----- Role 成长补全 + 改名 + 转职(自动循环 轮5) -----
        /// <summary>查看他人 Figure 到达(参数: RoleFigureInfo vo)。13013 回包解析完发。
        /// RoleController.On13013 发,消费方(排行榜/记录列表"点开看模型")待补。</summary>
        public const string EVT_ROLE_FIGURE_RETURN = "EVT_ROLE_FIGURE_RETURN";
        /// <summary>头像激活列表变动(无参,读 RoleModel.HeadIdList)。13080 全量到达 / 13081 激活推送后都发。
        /// RoleController.On13080/On13081 发。</summary>
        public const string EVT_ROLE_HEAD_LIST_UPDATE = "EVT_ROLE_HEAD_LIST_UPDATE";
        /// <summary>设置头像成功(无参,13083 code==1)。对标老端 SettingModel.SELECT_ROLE_HEAD_ICON_SUCCESS,
        /// SettingChangeHeadView 监听后关闭自己。RoleController.On13083 发。</summary>
        public const string EVT_ROLE_HEAD_SET_SUCCESS = "EVT_ROLE_HEAD_SET_SUCCESS";
        /// <summary>角色终身次数变动(参数: int module, int sub)。13088 批次/13089 增量到达都发;
        /// 目前无 UI 消费方(TODO)。GameStartController.On13088 / RoleController.On13089 发。</summary>
        public const string EVT_ROLE_LIFELONG_COUNT_UPDATE = "EVT_ROLE_LIFELONG_COUNT_UPDATE";
        /// <summary>改名校验通过(参数: string name, int type)。42604 result==1 时发,对标老端
        /// EventName.CAN_USE_THIS_NAME;SettingChangeNameView 监听后弹二次确认,确定才发 42601。
        /// RoleController.On42604 发。</summary>
        public const string EVT_ROLE_RENAME_CHECK_PASSED = "EVT_ROLE_RENAME_CHECK_PASSED";
        /// <summary>改名提交成功(无参)。42601 result==1 时发;Figure.Name 的更新走既有 12086 广播路径
        /// (SceneController.On12086 自身分流),此事件只负责关窗,勿在此重复改名。RoleController.On42601 发。</summary>
        public const string EVT_ROLE_RENAME_SUCCESS = "EVT_ROLE_RENAME_SUCCESS";
        /// <summary>转职成功(参数: int career, int sex)。13045 error_code==1 时发,对标老端
        /// CHANGE_MAINROLE_CAREER。TransferJobController.On13045 发。</summary>
        public const string EVT_CAREER_CHANGED = "EVT_CAREER_CHANGED";

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
        /// <summary>场景首屏就绪:地图首屏瓦片加载泵空闲(或兜底超时)。切图黑幕据此揭幕。</summary>
        public const string EVT_SCENE_FIRST_SCREEN_READY = "EVT_SCENE_FIRST_SCREEN_READY";
        /// <summary>场景实体就绪:主角+12002快照+首批怪/NPC全部立起(或兜底超时)。首次进世界的加载页据此揭幕。</summary>
        public const string EVT_SCENE_ENTITIES_READY = "EVT_SCENE_ENTITIES_READY";
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

        // ----- Mail 邮件详情/删除/领取(自动循环 轮7) -----
        /// <summary>详情就绪(参数: long mailId),对标老端 OPEN_EMAIL_VIEW——缓存命中或 19002 回包写完缓存后发,
        /// 供 EmailPopView 打开/刷新读 MailModel.GetDetail(mailId)。MailController.RequestMailDetail/On19002 发。</summary>
        public const string EVT_MAIL_DETAIL_READY = "EVT_MAIL_DETAIL_READY";
        /// <summary>批量领取成功(参数: List&lt;(int style,int typeId,int count)&gt; rewards),对标老端 EMAIL_REWARD_UPDATE。
        /// 供 CongratulationObtain 通道或 toast 降级消费。MailController.On19005 发。</summary>
        public const string EVT_MAIL_RECEIVE_REWARD = "EVT_MAIL_RECEIVE_REWARD";
        /// <summary>公会邮件发送结果(参数: bool success),对标老端 On19006。UI 归公会模块消费,TODO。</summary>
        public const string EVT_MAIL_GUILD_SEND_RESULT = "EVT_MAIL_GUILD_SEND_RESULT";
        /// <summary>意见反馈提交结果(参数: bool success),success 时对标老端 SUBMIT_SUCCESS 清空输入框。</summary>
        public const string EVT_MAIL_FEEDBACK_RESULT = "EVT_MAIL_FEEDBACK_RESULT";

        // ----- Friend / 好友(自动循环 轮7;140xx,yu_server pt_140.erl / pp_relationship.erl) -----
        /// <summary>好友/仇人/黑名单分桶数据变化(参数: int type),对标老端 FRIEND_DATA_UPDATE。
        /// 读 FriendModel.GetFriendData(type)。FriendController.On14000/On14013/On14014/On14007 发。</summary>
        public const string EVT_FRIEND_DATA_UPDATE = "EVT_FRIEND_DATA_UPDATE";
        /// <summary>推荐列表变化(无参),对标老端 RECOMMEND_DATA_UPDATE。读 FriendModel.GetRecommendList()。
        /// FriendController.On14001/On14002/申请后标记 发。</summary>
        public const string EVT_FRIEND_RECOMMEND_UPDATE = "EVT_FRIEND_RECOMMEND_UPDATE";
        /// <summary>好友申请列表变化(无参),对标老端 APPLY_DATA_UPDATE。读 FriendModel.ApplyList。
        /// FriendController.On14006/On14008/On14004/On14005 发。</summary>
        public const string EVT_FRIEND_APPLY_UPDATE = "EVT_FRIEND_APPLY_UPDATE";
        /// <summary>好友红点变化(无参),对标老端 FRIEND_REDDOT_UPDATE。读 FriendModel.HaveNewApply。</summary>
        public const string EVT_FRIEND_REDDOT_UPDATE = "EVT_FRIEND_REDDOT_UPDATE";
        /// <summary>好友在线状态变化(参数: FriendModel.FriendVo),对标老端 FRIEND_OLINE_UPDATE。
        /// FriendController.On14009 发。</summary>
        public const string EVT_FRIEND_ONLINE_UPDATE = "EVT_FRIEND_ONLINE_UPDATE";
        /// <summary>亲密度变化(参数: long roleId, int intimacy),对标老端 INTIMACY_UPDATE。
        /// FriendController.On14015 发,只在好友桶命中才触发。</summary>
        public const string EVT_FRIEND_INTIMACY_UPDATE = "EVT_FRIEND_INTIMACY_UPDATE";
        /// <summary>右键菜单数据就绪(参数: long roleId),对标老端 MENU_DATA_UPDTE。读 FriendModel.GetMenuData(roleId)。
        /// FriendController.On14010 发(800ms 节流缓存命中不重发协议,但仍会发本事件用最新缓存刷新菜单)。</summary>
        public const string EVT_FRIEND_MENU_UPDATE = "EVT_FRIEND_MENU_UPDATE";
        /// <summary>140xx 通用错误码兜底(参数: int code),对标老端 On14099。</summary>
        public const string EVT_FRIEND_ERROR = "EVT_FRIEND_ERROR";
        /// <summary>他人资料卡就绪(参数: FriendModel.PlayerCard),对标老端 19501→19502(module_id=1 基础装备)。
        /// FriendController.On19502 发,消费方(资料卡 UI)本轮未接,TODO。</summary>
        public const string EVT_PLAYER_CARD = "EVT_PLAYER_CARD";

        // ----- Team / 组队(自动循环 轮8;24xxx,yu_server pt_240.erl / pp_team.erl) -----
        /// <summary>队伍信息变化(创建/加入/退出/成员增删/队长变更/助战广播/场景/在线,统一走这一条,
        /// 对标老端 TEAM_UPDATE_TERAM_INFO)。无参,读 TeamModel.Instance.Info/HasTeam。
        /// TeamController 多个 handler(24005/10/14/15/34/51/52 等)发。</summary>
        public const string EVT_TEAM_INFO_UPDATE = "EVT_TEAM_INFO_UPDATE";
        /// <summary>组队大厅列表变化(参数: int activityId, int subtype),对标老端 TEAM_UPDATE_TERAM_HALL。
        /// 读 TeamModel.Instance.Hall(已按人数降序)。TeamController.On24012 发。</summary>
        public const string EVT_TEAM_HALL_UPDATE = "EVT_TEAM_HALL_UPDATE";
        /// <summary>申请列表变化(无参),对标老端 TEAM_UPDATE_APPLY_LIST。读 TeamModel.Instance.ApplyList
        /// (已按本地屏蔽表过滤)。TeamController.On24047 发。</summary>
        public const string EVT_TEAM_APPLY_LIST_UPDATE = "EVT_TEAM_APPLY_LIST_UPDATE";
        /// <summary>申请红点变化(无参),对标老端 RedDotManager(ModuleType.TEAM_APPLY)。
        /// 读 TeamModel.Instance.HaveNewApply。TeamController.On24003 发(非屏蔽状态才点亮)。</summary>
        public const string EVT_TEAM_APPLY_REDDOT_UPDATE = "EVT_TEAM_APPLY_REDDOT_UPDATE";
        /// <summary>被邀请列表变化(无参),对标老端 TEAM_UPDATE_BE_INVITED_LIST。读 TeamModel.Instance.BeInvitedList。
        /// TeamController.On24007/RespondInvite 发;本轮走 headless Confirm 队列消费,TeamBeInvitedView(列表弹窗)
        /// 待转换,TODO。</summary>
        public const string EVT_TEAM_BE_INVITED_UPDATE = "EVT_TEAM_BE_INVITED_UPDATE";
        /// <summary>更改组队目标成功(无参),对标老端 TEAM_CHANGE_TARGET_SUCCESS。TeamController.On24017 发。</summary>
        public const string EVT_TEAM_CHANGE_TARGET_SUCCESS = "EVT_TEAM_CHANGE_TARGET_SUCCESS";
        /// <summary>自动同意开关变化(无参),对标老端 TEAM_CHANGE_JOIN_TYPE。TeamController.On24018 发。</summary>
        public const string EVT_TEAM_JOIN_TYPE_UPDATE = "EVT_TEAM_JOIN_TYPE_UPDATE";
        /// <summary>投票相关数据变化(无参,读 TeamModel.Instance.CurrentVote/VoteData),对标老端
        /// TEAM_OPEN_VIEW"TeamVoteView"/TEAM_UPDATE_VOTE_DATA/TEAM_CLOSE_VIEW 三类信号合并。
        /// TeamController.On24035/36/37/40 发;TeamVoteView 未移植,消费方 TODO。</summary>
        public const string EVT_TEAM_VOTE_UPDATE = "EVT_TEAM_VOTE_UPDATE";
        /// <summary>招募列表变化(参数: int type),对标老端 TEAM_UPDATE_ZHAO_MU_DATA。
        /// TeamController.On24060/On24061 发。</summary>
        public const string EVT_TEAM_ZHAO_MU_UPDATE = "EVT_TEAM_ZHAO_MU_UPDATE";
        /// <summary>自动匹配状态变化(参数: bool autoMatch),对标老端 UPDATE_MATCH_STATE。
        /// TeamModel.SetAutoMatch 发;TeamMatchView(匹配中倒计时浮层)未移植,仅状态存储,消费方 TODO。</summary>
        public const string EVT_TEAM_MATCH_STATE_UPDATE = "EVT_TEAM_MATCH_STATE_UPDATE";
        /// <summary>我的助战状态变化(参数: int dunId),对标老端 UPDATE_HELP_STATE。
        /// TeamController.On24033/On24049 发。</summary>
        public const string EVT_TEAM_HELP_STATE_UPDATE = "EVT_TEAM_HELP_STATE_UPDATE";
        /// <summary>附近玩家列表变化(无参),对标老端 TEAM_UPDATE_NEAR_BY_PLAYER。TeamController.On24053 发。</summary>
        public const string EVT_TEAM_NEARBY_PLAYER_UPDATE = "EVT_TEAM_NEARBY_PLAYER_UPDATE";
        /// <summary>创建队伍成功(无参),对标老端 TEAM_BUILD_SUCCESS。TeamController.On24000 发。</summary>
        public const string EVT_TEAM_BUILD_SUCCESS = "EVT_TEAM_BUILD_SUCCESS";
        /// <summary>世界喊话成功(无参),对标老端 TEAM_WORLD_SHOUT_SUCCESS。TeamController.On24055 发。</summary>
        public const string EVT_TEAM_WORLD_SHOUT_SUCCESS = "EVT_TEAM_WORLD_SHOUT_SUCCESS";

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

        // ----- Daily / 日常中心(自动循环 轮10) -----
        /// <summary>15701(act_type=UnLimit)每日任务表落地(读 DailyModel.GetDailyData(1);对标老端 UPDATE_DAILY_DATA)。</summary>
        public const string EVT_DAILY_TASK_UPDATE = "EVT_DAILY_TASK_UPDATE";
        /// <summary>15701(act_type=Limit)限时活动表落地(读 DailyModel.GetDailyData(2))。</summary>
        public const string EVT_DAILY_LIMIT_UPDATE = "EVT_DAILY_LIMIT_UPDATE";
        /// <summary>15703 活跃度宝箱进度落地(对标老端 UPDATE_LIVENESS_REWARD)。</summary>
        public const string EVT_DAILY_LIVENESS_REWARD_UPDATE = "EVT_DAILY_LIVENESS_REWARD_UPDATE";
        /// <summary>15709 活跃度形象信息落地(对标老端 UPDATE_LIVENESS_IMAGE_DATA)。</summary>
        public const string EVT_DAILY_LIVENESS_IMAGE_UPDATE = "EVT_DAILY_LIVENESS_IMAGE_UPDATE";
        /// <summary>15712 他人活跃度形象广播转发(参数: long role_id, int figure_id;场景角色同步消费方未接线,TODO)。</summary>
        public const string EVT_DAILY_FIGURE_PUSH = "EVT_DAILY_FIGURE_PUSH";
        /// <summary>15714 离线挂机时间更新(参数: long time;对标老端 UPDATE_OUTLINE_INFO)。</summary>
        public const string EVT_DAILY_OUTLINE_TIME = "EVT_DAILY_OUTLINE_TIME";
        /// <summary>41900/41903/41904 资源找回表变动(对标老端 UPDATE_RES_FIND_DATA)。</summary>
        public const string EVT_DAILY_RES_FIND_UPDATE = "EVT_DAILY_RES_FIND_UPDATE";
        /// <summary>61801 我要变强状态表落地(对标老端 STRONGER_DATA_RETURN)。</summary>
        public const string EVT_DAILY_STRONGER_UPDATE = "EVT_DAILY_STRONGER_UPDATE";
        /// <summary>15718/15719/15720 预约状态表变动(驱动限时活动页红点/领奖态刷新)。</summary>
        public const string EVT_DAILY_SIGNUP_UPDATE = "EVT_DAILY_SIGNUP_UPDATE";
        /// <summary>15719 报名成功(参数: int module, int module_sub, int ac_sub;状态经 DailyModel.TryGetReservation
        /// 回读,对标老端 DAILY_ORDER_SUCCESS)。</summary>
        public const string EVT_DAILY_SIGNUP_SUCCESS = "EVT_DAILY_SIGNUP_SUCCESS";
        /// <summary>15721 活动开启提醒到达(壳未接线,仅供日志/未来 DailyActTipView 消费)。</summary>
        public const string EVT_DAILY_ACT_REMIND = "EVT_DAILY_ACT_REMIND";
        /// <summary>日常中心红点综合判定变化(参数: bool show;简化版 ComputeRedDot,对标老端 ShowRedDot)。</summary>
        public const string EVT_DAILY_RED_DOT = "EVT_DAILY_RED_DOT";

        // ----- Shop / 商店(自动循环 轮11) -----
        /// <summary>15301 某 shop_type 商品列表落地(参数: int shopType;对标老端 ShopEvent.UPDATE_SHOP_DATA)。</summary>
        public const string EVT_SHOP_DATA_UPDATE = "EVT_SHOP_DATA_UPDATE";
        /// <summary>15302 购买成功后单条 sold_out 原地更新(参数: int keyId;对标老端 UPDATE_SHOP_ONE_DATA)。</summary>
        public const string EVT_SHOP_ONE_UPDATE = "EVT_SHOP_ONE_UPDATE";
        /// <summary>15302 购买成功(参数: int keyId;对标老端 BUY_GOODS_SUCCESS,驱动奖励飞图标——未接线,TODO)。</summary>
        public const string EVT_SHOP_BUY_SUCCESS = "EVT_SHOP_BUY_SUCCESS";
        /// <summary>15305 神秘/神纹商店数据落地(参数: int mysteryType;对标老端 UPDATE_MYSTERY_SHOP_DATA)。</summary>
        public const string EVT_SHOP_MYSTERY_UPDATE = "EVT_SHOP_MYSTERY_UPDATE";
        /// <summary>15305 hit_num 变化 → 刷新特效(参数: int mysteryType;对标老端 SHOW_REFRESH_EFFECT;未接线,TODO)。</summary>
        public const string EVT_SHOP_MYSTERY_REFRESH_EFFECT = "EVT_SHOP_MYSTERY_REFRESH_EFFECT";
        /// <summary>15307 购买神秘/神纹商品成功(参数: int cfgId;对标老端 SHOP_MYSTERY_BUY_SUCCESS)。</summary>
        public const string EVT_SHOP_MYSTERY_BUY_SUCCESS = "EVT_SHOP_MYSTERY_BUY_SUCCESS";
        /// <summary>64000/64001/64002/64003 抢购商城数据落地/变化(对标老端 UPDATE_VIE_SHOP_DATA)。</summary>
        public const string EVT_SHOP_VIE_UPDATE = "EVT_SHOP_VIE_UPDATE";
        /// <summary>64001 抢购购买成功(参数: int id;对标老端 BUY_VIE_GOODS_SUCCESS)。</summary>
        public const string EVT_SHOP_VIE_BUY_SUCCESS = "EVT_SHOP_VIE_BUY_SUCCESS";
        /// <summary>商城红点综合判定变化(参数: bool show;聚合钻石/抢购/神秘首次全新 三路,对标老端
        /// REFRESH_ACTIVITY_ICON_RED_DOT,153——活动图标像素级挂接留 TODO,本轮先给可消费信号)。</summary>
        public const string EVT_SHOP_RED_DOT = "EVT_SHOP_RED_DOT";

        // ----- Rank / 排行榜(自动循环 轮12 #12,纯数据层轮) -----
        /// <summary>22101 某 rank_type 的一页数据落地/续拉完成(参数: int rankType;对标老端
        /// RankModelEvent.RANK_DATA_UPDATE)。本轮无 UI 消费方,先给可订阅信号,供 UI 尾包直接接。</summary>
        public const string EVT_RANK_DATA_UPDATE = "EVT_RANK_DATA_UPDATE";

        // ----- Boss / Boss家族一期·本服核心(自动循环 轮15a;46000段+20025-26+20201-205) -----
        /// <summary>46000 某 boss_type 的列表/状态落地(参数: int bossType)。</summary>
        public const string EVT_BOSS_LIST_UPDATE = "EVT_BOSS_LIST_UPDATE";
        /// <summary>46009 Boss 重生广播(参数: int bossType, int bossId;订正后的显式类型门,对标老端
        /// KILL_BOSS 事件,恒真 bug 已修)。</summary>
        public const string EVT_BOSS_REBORN = "EVT_BOSS_REBORN";
        /// <summary>46016/46008 Boss 击杀/复活提醒落地(参数: int bossType, int bossId)。</summary>
        public const string EVT_BOSS_KILLED_NOTICE = "EVT_BOSS_KILLED_NOTICE";
        /// <summary>46003/46004 进出 Boss 场景结果(参数: bool isEnter, int code)。</summary>
        public const string EVT_BOSS_ENTER_RESULT = "EVT_BOSS_ENTER_RESULT";
        /// <summary>46007 关注/取关操作回执(参数: int bossType, int bossId, bool remind)。</summary>
        public const string EVT_BOSS_REMIND_UPDATE = "EVT_BOSS_REMIND_UPDATE";
        /// <summary>46019/46022 伤害榜落地(自己排名或前3防抖广播,recv 纯被动落表,非拉取)。</summary>
        public const string EVT_BOSS_DAMAGE_RANK_UPDATE = "EVT_BOSS_DAMAGE_RANK_UPDATE";
        /// <summary>46002 全局掉落日志落地(46046 跨服大妖变体本轮不接,详见 Proto.cs 说明)。</summary>
        public const string EVT_BOSS_DROP_LOG_UPDATE = "EVT_BOSS_DROP_LOG_UPDATE";
        /// <summary>46001 击杀日志落地(≤100 条,?BOSS_LOG_LEN 硬顶)。</summary>
        public const string EVT_BOSS_KILL_LOG_UPDATE = "EVT_BOSS_KILL_LOG_UPDATE";
        /// <summary>46041 消耗复活结果(参数: bool success, int bossType, int bossId)。</summary>
        public const string EVT_BOSS_REVIVE_RESULT = "EVT_BOSS_REVIVE_RESULT";
        /// <summary>46044/46043 体力数据更新。</summary>
        public const string EVT_BOSS_VIT_UPDATE = "EVT_BOSS_VIT_UPDATE";
        /// <summary>46045 找回体力结果(参数: bool success;wire 真实字段 code,订正老端 errcode 笔误)。</summary>
        public const string EVT_BOSS_VIT_RECOVER_RESULT = "EVT_BOSS_VIT_RECOVER_RESULT";
        /// <summary>20201-205 免战保护状态更新(信息/使用回执/结束时间/推送/结束回执统一走这一个信号)。</summary>
        public const string EVT_BOSS_WAR_FREE_UPDATE = "EVT_BOSS_WAR_FREE_UPDATE";
        /// <summary>20025/20026 场景采集查询/打断通知(参数: List&lt;long&gt; roleIds 或单 role;场景消费钩子
        /// 未接,TODO,本事件先给可订阅信号)。</summary>
        public const string EVT_BOSS_COLLECT_UPDATE = "EVT_BOSS_COLLECT_UPDATE";

        // ----- KfBoss / Boss家族二期·跨服族(自动循环 轮15b;pt_470+pt_471+pt_619+pt_460内kf_great_demon壳) -----
        /// <summary>pt_470 千幻蜃楼状态变化(47000列表/47005关注/47007-09重生刷新/47017-19宝箱坐标·狩猎
        /// 等级/47021榜单/47023最大疲劳,参数: int clientBossType,均已 +1000)。</summary>
        public const string EVT_KFBOSS_EUDEMONS_UPDATE = "EVT_KFBOSS_EUDEMONS_UPDATE";
        /// <summary>47003/47004 进出千幻蜃楼结果(参数: bool isEnter, int code)。</summary>
        public const string EVT_KFBOSS_EUDEMONS_ENTER_RESULT = "EVT_KFBOSS_EUDEMONS_ENTER_RESULT";
        /// <summary>47006 千幻蜃楼 boss 重生提醒(含服务端 46008-for-type20 误发壳量,参数: int bossType, int bossId)。</summary>
        public const string EVT_KFBOSS_EUDEMONS_REBORN_TIP = "EVT_KFBOSS_EUDEMONS_REBORN_TIP";
        /// <summary>47002 千幻蜃楼掉落日志落地。</summary>
        public const string EVT_KFBOSS_EUDEMONS_DROP_LOG_UPDATE = "EVT_KFBOSS_EUDEMONS_DROP_LOG_UPDATE";
        /// <summary>47015 千幻蜃楼结算奖励落地。</summary>
        public const string EVT_KFBOSS_EUDEMONS_SETTLE_REWARD = "EVT_KFBOSS_EUDEMONS_SETTLE_REWARD";
        /// <summary>47035 复活千幻蜃楼 boss 结果(参数: bool success, int bossType, int bossId)。</summary>
        public const string EVT_KFBOSS_EUDEMONS_REVIVE_RESULT = "EVT_KFBOSS_EUDEMONS_REVIVE_RESULT";
        /// <summary>pt_471 镇煞封魂状态变化(47101主信息/47104购买/47105-06关注/47109-12排名/47114-16场景信息,
        /// 无参数,消费方统一重读 KfBossModel)。</summary>
        public const string EVT_KFBOSS_DECORATION_UPDATE = "EVT_KFBOSS_DECORATION_UPDATE";
        /// <summary>47102/47103/47110 进出/进特殊 boss 结果(参数: int code)。</summary>
        public const string EVT_KFBOSS_DECORATION_ENTER_RESULT = "EVT_KFBOSS_DECORATION_ENTER_RESULT";
        /// <summary>47107 镇煞封魂 boss 复活提醒(参数: int bossId)。</summary>
        public const string EVT_KFBOSS_DECORATION_REVIVE_TIP = "EVT_KFBOSS_DECORATION_REVIVE_TIP";
        /// <summary>47108 镇煞封魂掉落日志落地。</summary>
        public const string EVT_KFBOSS_DECORATION_DROP_LOG_UPDATE = "EVT_KFBOSS_DECORATION_DROP_LOG_UPDATE";
        /// <summary>47113 镇煞封魂 boss 结算落地。</summary>
        public const string EVT_KFBOSS_DECORATION_SETTLE = "EVT_KFBOSS_DECORATION_SETTLE";
        /// <summary>47111 仙宗召援(镇煞封魂场景内,勿与 Guild 40060 混淆)结果(参数: int code)。</summary>
        public const string EVT_KFBOSS_DECORATION_GUILD_HELP_RESULT = "EVT_KFBOSS_DECORATION_GUILD_HELP_RESULT";
        /// <summary>pt_619 论剑恩怨簿更新(61900全量/61901本服增量/61902跨服增量,均复用此信号)。</summary>
        public const string EVT_KFBOSS_KILL_RECORD_UPDATE = "EVT_KFBOSS_KILL_RECORD_UPDATE";
        /// <summary>46037/46038 太古遗凶阶段奖励状态/领取结果落地。</summary>
        public const string EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE = "EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE";
        /// <summary>46039 太古遗凶进场景宝箱信息落地。</summary>
        public const string EVT_KFBOSS_GREAT_DEMON_BOX_UPDATE = "EVT_KFBOSS_GREAT_DEMON_BOX_UPDATE";
        /// <summary>46046 太古遗凶掉落日志落地。</summary>
        public const string EVT_KFBOSS_GREAT_DEMON_DROP_LOG_UPDATE = "EVT_KFBOSS_GREAT_DEMON_DROP_LOG_UPDATE";

        // ----- Marriage / 婚姻(征友/戒指/结婚,自动循环 轮16;pt_172 172xx + 223xx 鲜花) -----
        /// <summary>17200/17201/17202 征友大厅数据变化(参数: int page;-1=非分页专属操作如关注回执)。</summary>
        public const string EVT_MARRIAGE_PERSONALS_UPDATE = "EVT_MARRIAGE_PERSONALS_UPDATE";
        /// <summary>17205 玩家细节(公会)到达,读 MarriageModel.LastRoleDetail。</summary>
        public const string EVT_MARRIAGE_ROLE_DETAIL_UPDATE = "EVT_MARRIAGE_ROLE_DETAIL_UPDATE";
        /// <summary>17210/17211/17213 戒指数据变化(成功分支),读 MarriageModel.Ring。</summary>
        public const string EVT_MARRIAGE_RING_UPDATE = "EVT_MARRIAGE_RING_UPDATE";
        /// <summary>17212 戒指单步提升失败(死号防御recv,对标老端 STOP_RING_UPGRADE)。</summary>
        public const string EVT_MARRIAGE_RING_STOP_UPGRADE = "EVT_MARRIAGE_RING_STOP_UPGRADE";
        /// <summary>17222 求婚/再婚/离婚协商/礼包邀请推送(参数: int type),读 MarriageModel.LastPropose。</summary>
        public const string EVT_MARRIAGE_PROPOSE_PUSH = "EVT_MARRIAGE_PROPOSE_PUSH";
        /// <summary>17231 发送求婚结果(参数: bool success)。</summary>
        public const string EVT_MARRIAGE_PROPOSE_SEND_RESULT = "EVT_MARRIAGE_PROPOSE_SEND_RESULT";
        /// <summary>17223 回应求婚结果(参数: bool success)。</summary>
        public const string EVT_MARRIAGE_PROPOSE_RESPOND_RESULT = "EVT_MARRIAGE_PROPOSE_RESPOND_RESULT";
        /// <summary>17224 回应结果推送(参数: long roleId, int type, int answerType;仅 answerType==1 时发,
        /// 对标老端拒绝分支无任何反馈)。</summary>
        public const string EVT_MARRIAGE_ANSWER_PUSH = "EVT_MARRIAGE_ANSWER_PUSH";
        /// <summary>17226 登录求婚/离婚信息汇总到达,读 MarriageModel.BiaobaiList/BiaobaiAnswerList。</summary>
        public const string EVT_MARRIAGE_BIAOBAI_UPDATE = "EVT_MARRIAGE_BIAOBAI_UPDATE";
        /// <summary>17229 键值推送(参数: int key, long val;key==1 对应恩爱值)。</summary>
        public const string EVT_MARRIAGE_KEY_VALUE_UPDATE = "EVT_MARRIAGE_KEY_VALUE_UPDATE";
        /// <summary>17232 我的伴侣数据变化(三成功码 1/1720012/1012 均发),读 MarriageModel.Mate。</summary>
        public const string EVT_MARRIAGE_MATE_UPDATE = "EVT_MARRIAGE_MATE_UPDATE";
        /// <summary>17234 发送离婚结果(参数: bool success)。</summary>
        public const string EVT_MARRIAGE_DIVORCE_RESULT = "EVT_MARRIAGE_DIVORCE_RESULT";
        /// <summary>17235 回应离婚结果(参数: bool success, int answerType)。</summary>
        public const string EVT_MARRIAGE_DIVORCE_RESPOND_RESULT = "EVT_MARRIAGE_DIVORCE_RESPOND_RESULT";
        /// <summary>17236 领取恩爱称号成功(参数: int id)。</summary>
        public const string EVT_MARRIAGE_DSGT_UPDATE = "EVT_MARRIAGE_DSGT_UPDATE";
        /// <summary>17237 购买真爱礼包结果(参数: bool success)。</summary>
        public const string EVT_MARRIAGE_GIFT_BUY_RESULT = "EVT_MARRIAGE_GIFT_BUY_RESULT";
        /// <summary>17238 真爱礼包信息到达,读 MarriageModel.Gift。</summary>
        public const string EVT_MARRIAGE_GIFT_INFO_UPDATE = "EVT_MARRIAGE_GIFT_INFO_UPDATE";
        /// <summary>17239 领取真爱礼包奖励结果(参数: bool success, int countType),读 MarriageModel.LastGiftReward。</summary>
        public const string EVT_MARRIAGE_GIFT_TAKE_RESULT = "EVT_MARRIAGE_GIFT_TAKE_RESULT";
        /// <summary>17240 请求对方购买礼包结果(参数: bool success)。</summary>
        public const string EVT_MARRIAGE_GIFT_ASK_RESULT = "EVT_MARRIAGE_GIFT_ASK_RESULT";
        /// <summary>17245 进退副本匹配防御接收(参数: int type, int dunId;服务端 handler 已整段注释)。</summary>
        public const string EVT_MARRIAGE_MATCH_RESULT = "EVT_MARRIAGE_MATCH_RESULT";
        /// <summary>17246 匹配结果防御接收(17245 服务端入口封存后无触发源),读 MarriageModel.LastMatchResult。</summary>
        public const string EVT_MARRIAGE_MATCH_PUSH = "EVT_MARRIAGE_MATCH_PUSH";
        /// <summary>17295 邀请伴侣购买副本次数结果(参数: bool success)。</summary>
        public const string EVT_MARRIAGE_DUN_INVITE_BUY_RESULT = "EVT_MARRIAGE_DUN_INVITE_BUY_RESULT";
        /// <summary>17296 收到副本次数购买邀请推送(参数: long roleId, int dunId)。</summary>
        public const string EVT_MARRIAGE_DUN_INVITE_PUSH = "EVT_MARRIAGE_DUN_INVITE_PUSH";
        /// <summary>17297 同意/拒绝购买副本次数推送(参数: int agree, int dunId)。</summary>
        public const string EVT_MARRIAGE_DUN_INVITE_RESPOND_PUSH = "EVT_MARRIAGE_DUN_INVITE_RESPOND_PUSH";
        /// <summary>22300 鲜花错误码专用号到达(参数: int code)。</summary>
        public const string EVT_MARRIAGE_FLOWER_ERROR = "EVT_MARRIAGE_FLOWER_ERROR";
        /// <summary>22301 赠送鲜花结果(参数: bool success, long receiveId, long goodsId)。</summary>
        public const string EVT_MARRIAGE_FLOWER_GIVE_RESULT = "EVT_MARRIAGE_FLOWER_GIVE_RESULT";
        /// <summary>22302 收礼记录到达(一次性全量),读 MarriageModel.FlowerRecords。</summary>
        public const string EVT_MARRIAGE_FLOWER_RECORD_UPDATE = "EVT_MARRIAGE_FLOWER_RECORD_UPDATE";
        /// <summary>22303 鲜花相关信息到达,读 MarriageModel.Flower。</summary>
        public const string EVT_MARRIAGE_FLOWER_INFO_UPDATE = "EVT_MARRIAGE_FLOWER_INFO_UPDATE";
        /// <summary>22304 收到的鲜花通知(参数: long senderId, long goodsId)。</summary>
        public const string EVT_MARRIAGE_FLOWER_RECEIVED = "EVT_MARRIAGE_FLOWER_RECEIVED";
        /// <summary>22305 感谢收花者结果(参数: bool success, long id)。</summary>
        public const string EVT_MARRIAGE_FLOWER_THANKS_RESULT = "EVT_MARRIAGE_FLOWER_THANKS_RESULT";

        // ----- 婚宴 / Banquet 数据层补全(pt_172 172xx,自动循环 轮24 PB;扩既有 172@1/172@2 图标壳) -----
        /// <summary>17250 预约/报名视图数据到达,读 BanquetModel.ApplyView/CanApply。</summary>
        public const string EVT_BANQUET_APPLY_INFO_UPDATE = "EVT_BANQUET_APPLY_INFO_UPDATE";
        /// <summary>17251 预约婚礼结果(参数: bool success;成功码 1/1720034 均算,内部已重发 17249+17250)。</summary>
        public const string EVT_BANQUET_APPLY_RESULT = "EVT_BANQUET_APPLY_RESULT";
        /// <summary>17252 邀请视图数据到达,读 BanquetModel.InviteView/GuestList/AskData。</summary>
        public const string EVT_BANQUET_INVITE_INFO_UPDATE = "EVT_BANQUET_INVITE_INFO_UPDATE";
        /// <summary>17253 邀请宾客结果(参数: bool success;成功码 1/1720033 均算,内部已重发 17252)。</summary>
        public const string EVT_BANQUET_INVITE_SEND_RESULT = "EVT_BANQUET_INVITE_SEND_RESULT";
        /// <summary>17257 索要请柬结果(参数: bool success)。</summary>
        public const string EVT_BANQUET_ASK_INVITE_RESULT = "EVT_BANQUET_ASK_INVITE_RESULT";
        /// <summary>17258 购买请柬/买路进场结果(参数: bool success)。</summary>
        public const string EVT_BANQUET_BUY_INVITE_CARD_RESULT = "EVT_BANQUET_BUY_INVITE_CARD_RESULT";
        /// <summary>17259 购买邀请名额上限结果(参数: bool success;成功已重发 17252,code==1720036 不显码)。</summary>
        public const string EVT_BANQUET_BUY_MAX_RESULT = "EVT_BANQUET_BUY_MAX_RESULT";
        /// <summary>17260 type==1(索要列表)/17252(ask_invite_list) 共享的 AskData 顶层桶更新(参数: bool isNewApply;
        /// 对标老端"比上次更多才算新申请"的 172@2 红点判定)。</summary>
        public const string EVT_BANQUET_ASK_DATA_UPDATE = "EVT_BANQUET_ASK_DATA_UPDATE";
        /// <summary>17260 type==2(宾客列表)/17252(guest_list) 共享的 GuestList 顶层桶更新。</summary>
        public const string EVT_BANQUET_GUEST_LIST_UPDATE = "EVT_BANQUET_GUEST_LIST_UPDATE";
        /// <summary>17261 回应索要请柬结果(参数: bool success;无论成败均已重发 17252)。</summary>
        public const string EVT_BANQUET_ANSWER_ASK_RESULT = "EVT_BANQUET_ANSWER_ASK_RESULT";
        /// <summary>17262 婚礼动画场景信息到达,读 BanquetModel.WeddingRoleList。</summary>
        public const string EVT_BANQUET_SCENE_ANIME_UPDATE = "EVT_BANQUET_SCENE_ANIME_UPDATE";
        /// <summary>17265 婚礼信息到达,读 BanquetModel.BanquetData。</summary>
        public const string EVT_BANQUET_INFO_UPDATE = "EVT_BANQUET_INFO_UPDATE";
        /// <summary>17266 撒喜糖结果(参数: bool success;成功已重发 17272)。</summary>
        public const string EVT_BANQUET_CANDIES_RESULT = "EVT_BANQUET_CANDIES_RESULT";
        /// <summary>17267 放烟花结果(参数: bool success;仅本端角色触发时判 code,config_wedding_fires 未载时
        /// 老端整段跳过不发本事件)。</summary>
        public const string EVT_BANQUET_FIRES_RESULT = "EVT_BANQUET_FIRES_RESULT";
        /// <summary>17270 发弹幕结果(参数: bool success)。</summary>
        public const string EVT_BANQUET_DANMU_RESULT = "EVT_BANQUET_DANMU_RESULT";
        /// <summary>17271 吃桌菜/采集喜糖结果推送(参数: int type, bool success;type==1=桌菜"喜宴"时已重发 17272)。</summary>
        public const string EVT_BANQUET_COLLECT_RESULT = "EVT_BANQUET_COLLECT_RESULT";
        /// <summary>17272 婚礼道具使用信息到达,读 BanquetModel.GoodsInfo。</summary>
        public const string EVT_BANQUET_GOODS_INFO_UPDATE = "EVT_BANQUET_GOODS_INFO_UPDATE";
        /// <summary>17275 婚礼获得总经验推送(参数: long allExp)。</summary>
        public const string EVT_BANQUET_EXP_UPDATE = "EVT_BANQUET_EXP_UPDATE";
        /// <summary>17277 气氛值变化推送(参数: long auraValue;仅 Type==1 时发)。</summary>
        public const string EVT_BANQUET_AURA_UPDATE = "EVT_BANQUET_AURA_UPDATE";
        /// <summary>17278 气氛值奖励推送(参数: long auraNum),读 BanquetModel.LastAuraReward。</summary>
        public const string EVT_BANQUET_AURA_REWARD_PUSH = "EVT_BANQUET_AURA_REWARD_PUSH";
        /// <summary>17279 吃桌菜奖励推送(参数: int type),读 BanquetModel.LastTableReward。</summary>
        public const string EVT_BANQUET_TABLE_REWARD_PUSH = "EVT_BANQUET_TABLE_REWARD_PUSH";
        /// <summary>17298 一键邀请剩余宾客结果(参数: bool success;成功已重发 17252+17260)。</summary>
        public const string EVT_BANQUET_ONE_INVITE_RESULT = "EVT_BANQUET_ONE_INVITE_RESULT";

        // ----- 自定义活动 / CustomActivity(331xx/332xx+225xx补全+224xx+159xx,自动循环 轮17)-----
        // 通用事件为主(P1 定义,事件粒度收敛,不给每个子活动开专用事件;UI 尾包再按需加)。P2-P6 只用这些。
        /// <summary>33101 全量活动列表落地(对标老端 SaveActInfo),读 CustomActivityModel.ActList。</summary>
        public const string EVT_CUSTOMACT_LIST_UPDATE = "EVT_CUSTOMACT_LIST_UPDATE";
        /// <summary>33102 活动增量新开落地(对标老端 AddActInfo)。</summary>
        public const string EVT_CUSTOMACT_LIST_ADD = "EVT_CUSTOMACT_LIST_ADD";
        /// <summary>33103 活动增量关闭落地(对标老端 DeleteActInfo)。</summary>
        public const string EVT_CUSTOMACT_LIST_REMOVE = "EVT_CUSTOMACT_LIST_REMOVE";
        /// <summary>33104 单活动通用详情落地(参数: int baseType, int subType),读 CustomActivityModel.GetDetail。</summary>
        public const string EVT_CUSTOMACT_DETAIL_UPDATE = "EVT_CUSTOMACT_DETAIL_UPDATE";
        /// <summary>33105 通用领取/操作结果(参数: int baseType, int subType, int code;code==1 成功)。</summary>
        public const string EVT_CUSTOMACT_RESULT = "EVT_CUSTOMACT_RESULT";
        /// <summary>33106 全服计数更新(参数: int baseType, int subType)。</summary>
        public const string EVT_CUSTOMACT_ALLCOUNT_UPDATE = "EVT_CUSTOMACT_ALLCOUNT_UPDATE";
        /// <summary>33100 331 家族通用错误码到达(参数: int code)。</summary>
        public const string EVT_CUSTOMACT_ERROR = "EVT_CUSTOMACT_ERROR";
        /// <summary>33158 红包雨波次(RED_PACKET_RAIN=82,P4 使用)。</summary>
        public const string EVT_CUSTOMACT_REDPACKET_WAVE = "EVT_CUSTOMACT_REDPACKET_WAVE";

        // ----- 便宜活批(GodBefall/Halo/FairyWish/RedPacket/FirstBlood/Festival/Welfare/AdReward/Scene散件,
        //        自动循环 轮18)-----
        // 事件粒度收敛:每系统 1-2 个通用事件,不为每个协议号单开;具体协议号交叉见 Proto.cs 对应常量注释。
        // Model 先落数据、事件后 Emit;PK1-PK5 各包按需在事件参数上扩展,不新增事件条目(除非确有必要)。

        /// <summary>GodBefall(440xx)数据到达(<see cref="Shenxiao.Framework.Net.Proto.GODBEFALL_LIST"/> 全量 /
        /// <see cref="Shenxiao.Framework.Net.Proto.GODBEFALL_ITEM_PUSH"/> 单只 / 44006出战 / 44010变身CD /
        /// 44011切变身 落地后发)。参数: long godId(0=全量列表刷新,非0=单只神格局部刷新)。</summary>
        public const string EVT_GODBEFALL_UPDATE = "EVT_GODBEFALL_UPDATE";
        /// <summary>GodBefall 操作结果(激活/升级/升阶/升星/穿脱装/合成/神格强化等,44002-44005/44012-44018)。
        /// 参数: int protoId(触发的请求号), int code(0/1=成功,其余=错误码)。</summary>
        public const string EVT_GODBEFALL_RESULT = "EVT_GODBEFALL_RESULT";

        /// <summary>Halo(514xx)信息/领奖/特权设置变化落地(51400/51401/51402)。参数: int protoId。</summary>
        public const string EVT_HALO_UPDATE = "EVT_HALO_UPDATE";

        /// <summary>FairyWish(513xx)信息/强化节点/点击列表变化落地(51300/51301/51303,51302 send-only 不经此事件)。
        /// 参数: int fairyId(0=批量/未指定单体)。</summary>
        public const string EVT_FAIRYWISH_UPDATE = "EVT_FAIRYWISH_UPDATE";

        /// <summary>RedPacket(339xx)列表/新增/领完推送落地(33900错误码/33901列表/33907新增/33908领完)。
        /// 参数: long redEnvelopesId(0=整表刷新)。</summary>
        public const string EVT_REDPACKET_UPDATE = "EVT_REDPACKET_UPDATE";
        /// <summary>RedPacket 打开/发红包结果(33902/33904/33906)。参数: int protoId, int code。</summary>
        public const string EVT_REDPACKET_RESULT = "EVT_REDPACKET_RESULT";

        /// <summary>FirstBlood(188xx)列表/提醒/红点/详情/领奖结果统一落地(18800-18807,type 96/97/105 三业务
        /// 共用同一事件,消费方按 type/subtype 自行分桶)。参数: int type, int subtype。</summary>
        public const string EVT_FIRSTBLOOD_UPDATE = "EVT_FIRSTBLOOD_UPDATE";

        /// <summary>Festival(194xx)信息/任务列表/领奖结果统一落地(19400-19405,19401 现有 On19401 走独立
        /// FestivalModel 落地,本事件供 PK3 扩展 19402-19405 后统一 Emit,查重确认全仓此前无同名事件)。
        /// 参数: int protoId。</summary>
        public const string EVT_FESTIVAL_UPDATE = "EVT_FESTIVAL_UPDATE";

        /// <summary>Welfare 家族(签到41703-05/静默下载41707-08/在线41715-16/心悦41719/
        /// 战力福利41723-24)信息落地。参数: int protoId(区分子系统)。m9:41722(成长福利)不在此列——
        /// 该号走图标机制(refreshIcon/RefreshIconRed),不 Emit 本事件。</summary>
        public const string EVT_WELFARE_UPDATE = "EVT_WELFARE_UPDATE";
        /// <summary>Welfare 家族操作结果(领取/摇奖/补签等)。参数: int protoId, int code。m9:同上,
        /// 41722 不 Emit 本事件。</summary>
        public const string EVT_WELFARE_RESULT = "EVT_WELFARE_RESULT";

        /// <summary>AdReward(193xx)广告列表/奖励推送/档位变更统一落地(19301-19304)。参数: int protoId。</summary>
        public const string EVT_ADREWARD_UPDATE = "EVT_ADREWARD_UPDATE";

        /// <summary>Scene 散件(120xx 补全,PK5:12015/12017/12022/12023/12025-12028/12030/12036/12043-12045/
        /// 12078/12080/12083/12085/12087/12088/12090/12092)通用落地事件,不与既有 EVT_DROP_*(15053/15088)
        /// 混用。参数: int protoId,消费方按需读 SceneManager 对应字段。</summary>
        public const string EVT_SCENE_MISC_UPDATE = "EVT_SCENE_MISC_UPDATE";

        // ----- 交易行(151xx补全,自动循环 轮19)----- MARKET_ICON_INFO(15121) 既有图标逻辑不经这两个
        // 事件,继续走 ActivityIconManager 直接增删,一行未动。
        /// <summary>Market(151xx补全)列表/信息类落地(15100错误推送/15101/15102/15109/15112/15114/
        /// 15118/15119/15120删除推送)。参数: int protoId。</summary>
        public const string EVT_MARKET_UPDATE = "EVT_MARKET_UPDATE";
        /// <summary>Market 操作结果(上架15106/下架15108/购买15111/发起求购15115/撤销求购15116/
        /// 求购出售15117/喊话15122)。参数: int protoId(触发的请求号), int code(1=成功,其余=错误码)。</summary>
        public const string EVT_MARKET_RESULT = "EVT_MARKET_RESULT";

        // ----- ServerClock(轮20)----- 跨天/整点事件源,由服务端 0点/4点单播 10201 驱动(不是本地 ticker,
        // 见 GameStartController.On10201 → ServerTimeModel.TryFireEvent),spec_serverclock_round20.md §0。
        /// <summary>服务器时间已刷新(无参)。对标老端 ServerTimeModel.REFRESH_SERVER_TIME
        /// (yu_client\h5\src\serverTime\ServerTimeModel.ts:8),每次收到 10201 落地后无条件发
        /// (ServerTimeModel.ts:40 InitServerTime 尾部)。GameStartController.On10201 发;消费方按需订阅
        /// "刚拿到新服务器时钟"信号(P4b LungController 绑的就是这个,非 DAY_CHANGE)。</summary>
        public const string EVT_SERVER_TIME_REFRESH = "EVT_SERVER_TIME_REFRESH";
        /// <summary>跨天(无参)。对标老端 ServerTimeModel.DAY_CHANGE(ServerTimeModel.ts:6),
        /// ServerTimeModel.TryFireEvent 在 GetOpenServerDay() 变化时发(ServerTimeModel.ts:49-51)。
        /// ServerTimeModel.TryFireEvent 发;P3/P4/P4b 多个小户(Marriage/Halo/Chat/Guild/CustomActivity 等)
        /// 订阅做跨天重置/补发。</summary>
        public const string EVT_SERVER_DAY_CHANGE = "EVT_SERVER_DAY_CHANGE";
        /// <summary>整点刷新(参数: int hour)。对标老端 ServerTimeModel.HOUR_REFRESH(ServerTimeModel.ts:9),
        /// ServerTimeModel.TryFireEvent 在命中 RefreshHourList 时发(ServerTimeModel.ts:58-63)。因
        /// refresh_hour_list=[4](ServerTimeModel.ts:10),该参数恒为 4;订阅方照老端写 if(hour==4) 的可保留
        /// (镜像老端恒真冗余,非本端引入)。ServerTimeModel.TryFireEvent 发;P2 三大户(Dungeon/Boss/Shop)+
        /// P4/P4b 多个模块订阅。</summary>
        public const string EVT_SERVER_HOUR_REFRESH = "EVT_SERVER_HOUR_REFRESH";

        // ----- Fashion(时装,第21轮 PA)-----
        /// <summary>时装数据变化(无参;41300/41301/41302/41303/41304/41306/41312/41316/41311 落地后发,
        /// 对标老端 FashionModel.Fire(UPDATEVIEW,...)/Fire(UPDATE_FIGHT,...) 的合并简化版)。</summary>
        public const string EVT_FASHION_UPDATE = "EVT_FASHION_UPDATE";

        // ----- 公会晚宴 GuildActivity(pt_402 主体,自动循环 轮22 PK1)-----
        /// <summary>40200 族错误出口(参数: int errcode)。</summary>
        public const string EVT_GUILDACT_ERROR = "EVT_GUILDACT_ERROR";
        /// <summary>40201 公会BOSS信息落地(无参)。</summary>
        public const string EVT_GUILDACT_BOSS_INFO_UPDATE = "EVT_GUILDACT_BOSS_INFO_UPDATE";
        /// <summary>40203 兽粮被动推送(参数: long add, long total)。</summary>
        public const string EVT_GUILDACT_BOSS_MAT_ADD = "EVT_GUILDACT_BOSS_MAT_ADD";
        /// <summary>40204 召集公会BOSS结果(参数: int errcode)。</summary>
        public const string EVT_GUILDACT_CALL_BOSS_RESULT = "EVT_GUILDACT_CALL_BOSS_RESULT";
        /// <summary>40208 BOSS结算推送(无参,读 GuildActivityModel.LastBossResult)。</summary>
        public const string EVT_GUILDACT_BOSS_RESULT = "EVT_GUILDACT_BOSS_RESULT";
        /// <summary>40209 自动召唤设置结果(参数: int errcode, int isAuto)。</summary>
        public const string EVT_GUILDACT_AUTO_DRUM_RESULT = "EVT_GUILDACT_AUTO_DRUM_RESULT";
        /// <summary>40211 晚宴活动信息落地(核心驱动号,无参)。</summary>
        public const string EVT_GUILDACT_ACT_INFO_UPDATE = "EVT_GUILDACT_ACT_INFO_UPDATE";
        /// <summary>40212 进入晚宴场景结果(参数: int errcode)。</summary>
        public const string EVT_GUILDACT_ENTER_SCENE_RESULT = "EVT_GUILDACT_ENTER_SCENE_RESULT";
        /// <summary>40214 积分排行榜落地(无参)。</summary>
        public const string EVT_GUILDACT_RANK_INFO_UPDATE = "EVT_GUILDACT_RANK_INFO_UPDATE";
        /// <summary>40217 答题信息落地(无参)。</summary>
        public const string EVT_GUILDACT_QUEST_INFO_UPDATE = "EVT_GUILDACT_QUEST_INFO_UPDATE";
        /// <summary>40220 个人积分排行落地(无参)。</summary>
        public const string EVT_GUILDACT_MY_RANK_UPDATE = "EVT_GUILDACT_MY_RANK_UPDATE";
        /// <summary>40221 小游戏完成状态(参数: bool finished)。</summary>
        public const string EVT_GUILDACT_MINI_GAME_STATUS = "EVT_GUILDACT_MINI_GAME_STATUS";
        /// <summary>40222 当日轮换小游戏类型(参数: int gameType)。</summary>
        public const string EVT_GUILDACT_GAME_TYPE_UPDATE = "EVT_GUILDACT_GAME_TYPE_UPDATE";
        /// <summary>40255 经验/贡献推送(参数: int type, long exp)。</summary>
        public const string EVT_GUILDACT_EXP_PUSH = "EVT_GUILDACT_EXP_PUSH";
        /// <summary>40256 火苗信息落地(无参)。</summary>
        public const string EVT_GUILDACT_FIRE_INFO_UPDATE = "EVT_GUILDACT_FIRE_INFO_UPDATE";
        /// <summary>40257 采集火苗奖励推送(无参,读 GuildActivityModel.LastFireReward)。</summary>
        public const string EVT_GUILDACT_FIRE_REWARD_PUSH = "EVT_GUILDACT_FIRE_REWARD_PUSH";
        /// <summary>40258 阶段推送(参数: int stage, int time)。</summary>
        public const string EVT_GUILDACT_STAGE_PUSH = "EVT_GUILDACT_STAGE_PUSH";
        /// <summary>40259 答题状态推送(参数: int status)。</summary>
        public const string EVT_GUILDACT_QUESTION_STATUS = "EVT_GUILDACT_QUESTION_STATUS";
        /// <summary>40260 龙魂信息落地(无参)。</summary>
        public const string EVT_GUILDACT_DRAGON_INFO_UPDATE = "EVT_GUILDACT_DRAGON_INFO_UPDATE";
        /// <summary>40262 战斗结果推送(无参,读 GuildActivityModel.LastResult)。</summary>
        public const string EVT_GUILDACT_RESULT_INFO_UPDATE = "EVT_GUILDACT_RESULT_INFO_UPDATE";
        /// <summary>40264 购买菜肴结果(参数: bool ok, int code)。</summary>
        public const string EVT_GUILDACT_FOOD_BUY_RESULT = "EVT_GUILDACT_FOOD_BUY_RESULT";
        /// <summary>40265 菜肴状态落地(无参)。</summary>
        public const string EVT_GUILDACT_FOOD_STATUS_UPDATE = "EVT_GUILDACT_FOOD_STATUS_UPDATE";
        /// <summary>40266 答题积分排名奖励推送(无参,读 GuildActivityModel.LastRankReward)。</summary>
        public const string EVT_GUILDACT_RANK_REWARD_PUSH = "EVT_GUILDACT_RANK_REWARD_PUSH";
        /// <summary>40267 经验加成状态落地(参数: long ratio)。</summary>
        public const string EVT_GUILDACT_EXP_BUFF_UPDATE = "EVT_GUILDACT_EXP_BUFF_UPDATE";

        // ----- StarEquip(星宿核心 pp_constellation_equip,pt_232 直接处理段 23200-23209/23250-23257,
        // 轮23 PK1;23204 按裁决1 killlist 不建对应事件) -----
        /// <summary>23200 族错误出口(参数: int errorCode, string errorCodeArgs)。老端特判 errorCode==1500081
        /// 触发 <see cref="EVT_STAREQUIP_COMPOSE_FAIL"/>(见 Proto.cs STAREQUIP_ERROR 注释:当前服务端实现下
        /// 是死分支,镜像不删)。</summary>
        public const string EVT_STAREQUIP_ERROR = "EVT_STAREQUIP_ERROR";
        /// <summary>23201 总览落地(无参,读 StarEquipModel.PageInfo/TotalStar)。</summary>
        public const string EVT_STAREQUIP_OVERVIEW_UPDATE = "EVT_STAREQUIP_OVERVIEW_UPDATE";
        /// <summary>23202 穿戴成功(参数: long goodsAutoId, long goodsTypeId;失败经 EVT_STAREQUIP_ERROR)。</summary>
        public const string EVT_STAREQUIP_WEAR_RESULT = "EVT_STAREQUIP_WEAR_RESULT";
        /// <summary>23203 卸下成功(参数: long goodsAutoId, long goodsTypeId;失败经 EVT_STAREQUIP_ERROR)。</summary>
        public const string EVT_STAREQUIP_UNWEAR_RESULT = "EVT_STAREQUIP_UNWEAR_RESULT";
        /// <summary>23205 星级大师界面信息落地(无参,读 StarEquipModel.StarMaster)。</summary>
        public const string EVT_STAREQUIP_STAR_MASTER_INFO_UPDATE = "EVT_STAREQUIP_STAR_MASTER_INFO_UPDATE";
        /// <summary>23206 星级大师升级结果(参数: bool ok, int code)。</summary>
        public const string EVT_STAREQUIP_STAR_MASTER_UP_RESULT = "EVT_STAREQUIP_STAR_MASTER_UP_RESULT";
        /// <summary>23207 吞噬界面信息落地(无参,读 StarEquipModel.Devour)。</summary>
        public const string EVT_STAREQUIP_DEVOUR_INFO_UPDATE = "EVT_STAREQUIP_DEVOUR_INFO_UPDATE";
        /// <summary>23208 吞噬筛选结果(无参,读 StarEquipModel.Devour.Color/Star;失败经 EVT_STAREQUIP_ERROR)。</summary>
        public const string EVT_STAREQUIP_DEVOUR_TAB_RESULT = "EVT_STAREQUIP_DEVOUR_TAB_RESULT";
        /// <summary>23209 吞噬执行成功(无参,读 StarEquipModel.Devour;失败经 EVT_STAREQUIP_ERROR)。</summary>
        public const string EVT_STAREQUIP_DEVOUR_RESULT = "EVT_STAREQUIP_DEVOUR_RESULT";
        /// <summary>23250 装备属性预览落地(无参,读 StarEquipModel.LastPreview)。</summary>
        public const string EVT_STAREQUIP_PREVIEW_UPDATE = "EVT_STAREQUIP_PREVIEW_UPDATE";
        /// <summary>23251 星数被动推送落地(无参,读 StarEquipModel.StarPush)。</summary>
        public const string EVT_STAREQUIP_STAR_PUSH_UPDATE = "EVT_STAREQUIP_STAR_PUSH_UPDATE";
        /// <summary>23252 合成成功(参数: int ruleId;读 StarEquipModel.LastComposeReward)。</summary>
        public const string EVT_STAREQUIP_COMPOSE_SUCCESS = "EVT_STAREQUIP_COMPOSE_SUCCESS";
        /// <summary>23252 合成失败(code==err150_compose_fail=1500081;无参)且共 23200 死分支同名复用
        /// (见 EVT_STAREQUIP_ERROR 注释)。</summary>
        public const string EVT_STAREQUIP_COMPOSE_FAIL = "EVT_STAREQUIP_COMPOSE_FAIL";
        /// <summary>23253 解锁星宿页成功(参数: int page;失败经 EVT_STAREQUIP_ERROR)。</summary>
        public const string EVT_STAREQUIP_UNLOCK_PAGE_RESULT = "EVT_STAREQUIP_UNLOCK_PAGE_RESULT";
        /// <summary>23254 蜕变/属性转移预览落地(无参,读 StarEquipModel.LastTransformPreview)。</summary>
        public const string EVT_STAREQUIP_TRANSFORM_PREVIEW_UPDATE = "EVT_STAREQUIP_TRANSFORM_PREVIEW_UPDATE";
        /// <summary>23255 类型 tips 预览落地(参数: long goodsTypeId;读 StarEquipModel.TypePreviewCache[id])。</summary>
        public const string EVT_STAREQUIP_TYPE_PREVIEW_UPDATE = "EVT_STAREQUIP_TYPE_PREVIEW_UPDATE";
        /// <summary>23256 合成次数信息落地(参数: int composeId;读 StarEquipModel.ComposeTime[id])。</summary>
        public const string EVT_STAREQUIP_COMPOSE_TIME_UPDATE = "EVT_STAREQUIP_COMPOSE_TIME_UPDATE";
        /// <summary>23257 蜕变/属性转移执行结果(参数: bool ok)。老端 on23257 是空 if 块未接任何动作,本端
        /// 仍补发事件供尾包消费(比老端多做但无害)。</summary>
        public const string EVT_STAREQUIP_TRANSFORM_RESULT = "EVT_STAREQUIP_TRANSFORM_RESULT";

        // ----- StarForge(星宿锻造 chc,pt_232 兜底转发段 23210-23241,轮23 PK2) -----
        /// <summary>23210/23220/23230/23240 某子系统某"页"入口数据落地(参数: int chcType[1强化/2进化/
        /// 3附魔显示"觉醒"/4启灵], int stype[星宿页])。数据本体读 StarForgeModel.GetInfo。</summary>
        public const string EVT_STARFORGE_INFO_UPDATE = "EVT_STARFORGE_INFO_UPDATE";
        /// <summary>23212/23232 大师列表落地(参数: int chcType[仅1/3有意义], int stype)。
        /// 数据本体读 StarForgeModel.GetMaster。</summary>
        public const string EVT_STARFORGE_MASTER_UPDATE = "EVT_STARFORGE_MASTER_UPDATE";
        /// <summary>23211/23221/23231/23241 动作结果(参数: int chcType, int code[原始错误码,1=成功],
        /// bool applied[数据是否真的变化——STREN/MAGIC/SOUL 即 code==1;EVO 是 code==1 且 is_success==1])。</summary>
        public const string EVT_STARFORGE_ACTION_RESULT = "EVT_STARFORGE_ACTION_RESULT";
        /// <summary>23213/23233 点亮大师结果(参数: int chcType[1/3], int code[1=成功])。</summary>
        public const string EVT_STARFORGE_MASTER_RESULT = "EVT_STARFORGE_MASTER_RESULT";
    }
}
