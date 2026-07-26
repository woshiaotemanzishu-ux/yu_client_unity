namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// 协议号常量,与 yu_client/yu_server 一致(请求与回包同号,注册回调即收该号回包)。
    /// 出处:yu_client h5/src/login/LoginController.ts。新协议号统一加在这里,不要散落。
    /// </summary>
    public static class Proto
    {
        /// <summary>龙语秘境主面板快照。C2S 严格空包；S2C 为剩余次数、总次数及地图/怪物列表。</summary>
        public const int DRAGON_WHISPER_INFO = 65101;
        /// <summary>龙语秘境掉落记录快照。C2S 严格空包；S2C 为完整掉落记录列表。</summary>
        public const int DRAGON_WHISPER_DROP_LOG = 65106;
        /// <summary>藏宝图开奖记录快照。C2S 严格空包；S2C 为完整记录及奖励列表。</summary>
        public const int TREASURE_MAP_DRAW_LOG = 20303;
        /// <summary>伙伴副本章节扫荡信息。C2S: level:u8；S2C: level:u8,sweep_count:u16,dun_list:u16×{dun_id:u32,score:u8}。</summary>
        public const int DUNGEON_PARTNER_DUNGEONS = 61105;
        /// <summary>伙伴副本章节星级奖励。C2S: level:u8；S2C: level:u8,stage_reward:u16×{score:u16,status:u8}。</summary>
        public const int DUNGEON_PARTNER_STAGE_REWARDS = 61106;
        /// <summary>尘世门活动状态。C2S 空包；S2C state:u8,end_time:u32,mod:u32,group_id:u32,next_start_time:u32,servers:u16×{server_id:u64,server_num:u64,name:string,world_lv:u64},avg_lv:u64。</summary>
        public const int SENTIENT_ACT_INFO = 24101;
        /// <summary>尘世门门户快照。C2S 空包；S2C portals:u16×{portal_id:u64,x:u32,y:u32}。</summary>
        public const int SENTIENT_ACT_PORTALS = 24102;
        /// <summary>尘世门人数快照。C2S 空包；S2C assist_num:u32,enter_num:u32。</summary>
        public const int SENTIENT_ACT_COUNTS = 24107;

        // ----- 登录链(1xxxx) -----
        /// <summary>账号登录游戏服。发 "iiss"(pid, 时间戳秒, account_id, plat_name);
        /// 回包 "clihi"(career, 服务器时间l, 开服时间i, 角色数h, 注册数i)+ 逐角色数据。</summary>
        public const int ACCOUNT_LOGIN = 10000;

        /// <summary>创角。发 "cccsslsscscc",见 LoginController.ts TRY_CREATE_ROLE。</summary>
        public const int CREATE_ROLE = 10003;

        /// <summary>选角进入游戏。发 "lsisisscscsh",见 TRY_LOGIN_GAME。</summary>
        public const int ENTER_GAME = 10004;

        /// <summary>心跳(无字段)。</summary>
        public const int HEARTBEAT = 10006;

        /// <summary>角色名验证(创角前查重)。发 "s" 名字;回包 "c" 结果
        /// (1成功/2失败/4已使用/5非法字符/6长度1-5,对标老客户端 On10007)。
        /// 注:此前误标为「踢下线通知」,已按 yu_server pt_100.erl 纠正。</summary>
        public const int NAME_VERIFY = 10007;
        public const int SERVER_TIME = 10201;
        public const int SETTING_LIST = 10202;
        /// <summary>批量写设置(对标老端 SETTING_REQUEST_PROTO_10203):发 h 条数 + 每条 c type/c subtype/c is_open;
        /// 回包 error_code:i(==1 成功后客户端把缓存列表落地 SettingModel)。</summary>
        public const int SETTING_WRITE = 10203;
        /// <summary>脱离卡死(对标老端 confirm_flee):发 "i"(scene_id);回包 code:i(!=1 显错误码,==1 服务端拉人切场景)。</summary>
        public const int SETTING_FLEE = 10210;
        public const int SETTING_WX_SUBSCRIPTION_SWITCH = 11307; // 微信订阅总开关：空请求，回包 res:u8。
        /// <summary>发言(各频道通用,含喇叭)。发 "csslssis"(channel, province, city, receive_id, msg, args, tktime=0, ticket="");
        /// 对标老端 ChatController.ts send_msg,与 pt_110.erl read(11001) 字段序逐一核对一致。receive_id 语义:
        /// 私聊(channel=6)=对方 role_id;喇叭(channel=2)=范围选择(1本服/2小跨服/3全服,TRUMPET_TYPE);其余频道传0。
        /// 回包同号 11001 用于世界/公会/队伍等公共频道广播;**私聊(channel=6)真正回包走 11002,喇叭走 11029,
        /// 不会原样回 11001**(pp_chat.erl handle(11001,...) 内按 Channel 分三路 write(11001/11002/11029,...))。</summary>
        /// <summary>聊天家族统一错误码出口(对标老端 ChatController.ts:412-417 On11000:
        /// Util.ErrorCodeShow(scmd.error_code, scmd.args),无其它副作用)。轮21 覆盖率审计发现的跨系统统一
        /// 错误码出口漏号之一(同批 15200/40505/40507,见 r21_coverage_governance.md;40505/40507 经复核为
        /// 服务端死号,已在 GuildController.cs 注释说明,不需要新注册)。回包 error_code:i, args:s。</summary>
        public const int CHAT_ERROR = 11000;
        public const int CHAT_MESSAGE = 11001;
        /// <summary>私聊消息推送(S2C,双方各收一份完全相同的包)。回包(pt_110.erl write(11002,...)):
        /// Channel:8, ServerNum:16, SerId:16, SerName:s, PlayerList[u16 len]{PlayerId:64, Figure}(固定2项:[发送者,接收者]),
        /// Msg:s, Args:s, Result:8, Time:32。目标 id 取"非自己"的那个 PlayerList 项(对标老端 setChatData)。</summary>
        public const int CHAT_PRIVATE_MESSAGE = 11002;
        public const int CHAT_CACHE = 11010;

        // ----- 聊天/公告(110xx,yu_server pt_110.erl) -----
        /// <summary>传闻广播(全服)。回包 "hhs"(moduleId, id, content)。</summary>
        public const int SC_CHUANWEN = 11015;
        /// <summary>传闻广播(带发送者形象)。回包 serId:h, playerId:l, figure 块, moduleId:h, id:h, content:s。</summary>
        public const int SC_CHUANWEN_FIGURE = 11018;
        /// <summary>系统公告/提示(跑马灯)。回包 "s"(content)。</summary>
        public const int SC_SYS_NOTICE = 11020;

        // ----- 聊天补全(自动循环 轮6;yu_server pt_110.erl/pp_chat.erl 权威字段序,与 yu_client
        //        ChatController.ts/ChatModel.ts 交叉核对,冲突处按服务端为准) -----
        /// <summary>是否开启小跨服聊天(GAME_START 非开服第1天 + 老端另有 DAY_CHANGE 复触发,本端未接日切事件,
        /// 仅 GAME_START 查一次)。发空;回包 "c"(is_open,0/1)。</summary>
        public const int CHAT_ZONE_OPEN = 11023;
        /// <summary>上传跨服频道物品数据(跨服频道发言带物品链接时,先发此包)。发 "c"+"h"+n×"l"(channel, 数量, 逐个 goods_id);
        /// 回包 "i"(error_code,!=1 → Emit 「跨服无法查看物品需先上传」提示)。</summary>
        public const int CHAT_UPLOAD_ZONE_GOODS = 11025;
        /// <summary>跨服查看物品(点击跨服频道物品链接时发)。发 "cl"(channel, goods_id);回包 "i"(error_code)。
        /// **recv 是空壳,照老端静默丢弃**——真正的物品面板由 mod_kf_chat 异步转发的另一条推送负责,11026 只是查询回执。</summary>
        public const int CHAT_CHECK_ZONE_GOODS = 11026;
        /// <summary>点击聊天缓存(消私聊未读红点)。发 "cl"(channel 固定=PRIVATE, role_id);回包 "i"(error_code)。
        /// 对标老端 FriendChatView:发送同时本地立即清红点(reSetPrivateNum),不等回包。</summary>
        public const int CHAT_CLICK_CACHE = 11027;
        /// <summary>查看私聊玩家信息(打开私聊窗口时发一次)。发 "l"(role_id);
        /// 回包(pt_110.erl write(11028,...)):error_code:32, role_id:64, Figure, combat_power:64, online_flag:8, intimacy:32。</summary>
        public const int CHAT_PRIVATE_PLAYER_INFO = 11028;
        /// <summary>喇叭广播推送(S2C 专用,客户端严禁发;真正的喇叭消耗/发送走 <see cref="CHAT_MESSAGE"/> channel=HORN(2))。
        /// 回包(pt_110.erl write(11029,...)):Channel:8, ServerNum:16, SerId:16, SerName:s, Province:s, City:s,
        /// HornType:8(1本服/2小跨服/3全服), PlayerId:64, Figure, Msg:s, Args:s, Result:8, Time:32。</summary>
        public const int CHAT_HORN_PUSH = 11029;
        /// <summary>被禁言通知(S2C 主动推送,老端漏接,本端补齐)。回包 "i"(距解禁剩余秒数)。
        /// ⚠r6_server 实证:服务端唯一生产者 lib_chat:be_lim_talk/1 当前全仓库无调用点,是彻底死代码——
        /// 注册此 handler 纯粹是"对老端遗漏的补齐"以防将来恢复调用,现状永远收不到此包,无害。</summary>
        public const int CHAT_BANNED_NOTICE = 11042;
        /// <summary>黑名单/清理玩家消息(S2C 推送)。回包 role_id 列表[u16 len]{role_id:64}。
        /// 遍历清理该玩家在公共频道的消息 + 私聊 dict 条目,**跳过自己**(role_id==自身时不清理)。
        /// 不在本轮目标号内,但它是私聊清理(11046)唯一入口,随 11027/11028 一并接。</summary>
        public const int CHAT_BLACKLIST_CLEAR = 11046;
        /// <summary>跨系统红点推送(对标老端 ChatController.ts:640-654 On11016):module_id==339(红包)时,
        /// 若本人未加入公会(guild_id==0)老端直接 return 不置位,否则置红包红点;module_id==400 且 type==1 时
        /// 是公会申请数红点(RedDotController.up(GUILD_APPLY,num) + 刷新主界面功能图标)。回包
        /// module_id:h, type:h, num:h。⚠Unity 侧 RedPacket 模块与公会红点体系(GuildController.cs 已注明
        /// "本仓 Guild 红点体系未建")均不在聊天包所有权范围,本轮只解包 + Emit 通用事件,真消费方接入时
        /// 按 module_id 分流(见 GlobalEvent.EVT_CHAT_RED_DOT_PUSH 注释)。</summary>
        public const int CHAT_RED_DOT_PUSH = 11016;
        /// <summary>系统公告/跑马灯(GAME_START 空参发一次;服务端后台改公告表时会主动全服重推,幂等重建)。
        /// 回包 notice_list[u16 len]{Source:s, Type:8, Color:s, Content:s, Url:s, SendCount:32, SendGap:16,
        /// StartTime:32, EndTime:32, State:8}。⚠与"喇叭"是两套系统(11050 纯只读零消耗,喇叭消耗广播在 11001/11029),
        /// 客户端收到后自跑本地每秒轮询定时器按 send_gap 循环触发展示(对标老端 StartGongGaoList,定时器实现见
        /// <see cref="Shenxiao.Module.Core.Chat.ChatModel.PumpNotice"/>,勿用 MonoBehaviour.Update 直接承载判定逻辑)。</summary>
        public const int CHAT_NOTICE = 11050;
        /// <summary>通用掉落飘字推送(S2C)。回包:Type:8 + GoodsList[u16×{GoodsTypeId:32,Num:32}]。</summary>
        public const int CHAT_GOODS_GAIN = 11060;
        /// <summary>标准 ObjectList 掉落飘字推送(S2C)。回包:ObjectList[u16×{Style:8,TypeId:32,Count:32}]。</summary>
        public const int CHAT_OBJECT_GAIN = 11061;
        /// <summary>全局鲜花特效推送(S2C)。回包:Effect:string；资源名由服务端下发，客户端不得硬编码。</summary>
        public const int CHAT_FLOWER_EFFECT = 11063;
        /// <summary>假人聊天触发(GAME_START 空参发一次;策划要求仅该账号下第一个角色会收到,服务端 mod_counter 已把关)。
        /// 回包 "c"(type)。⚠降级:老端据此拼假人击杀/获得道具消息需要 config_jjc_robot + ClientRobotLv 两张配置表
        /// (伪造角色外观/装备/战力用于营造"新服热闹"假象),两表均未迁移入 Unity——本端只记录收到的 type,
        /// 不生成任何假人消息(不臆造经济/形象数据),TODO 待配置迁移后补齐。</summary>
        public const int CHAT_ROBOT = 11064;
        /// <summary>聊天监控动态包编号(C2S 单向,微信小游戏分包场景专用,回包 ack 可忽略)。发 "s"(package_code)。
        /// Unity 构建无微信小游戏分包概念,本端无自动触发源,仅留 API(<see cref="Shenxiao.Module.Core.Chat.ChatController.SendMonitorPackageCode"/>)。</summary>
        public const int CHAT_MONITOR_PKG = 11065;

        // 以下号跳过(仅存说明,不写代码;逐号裁决见规格 §0 及本轮汇报"裁决表"):
        // 11003/11004/11005/11006(语音聊天全链路):老端录音入口(VoiceChatView.SendVoice 调用被注释)、SDK回调触发
        //   (ON_RESULT_OF_SPEECH/ON_END_OF_SPEECH 全仓库唯一 Fire 点也被注释)、发送触发三层均不可达(dead);
        //   11004 点击播放绑定的 REQUEST_CCMD_EVENT 全仓库无存活监听,也是死接线。11005 服务端回包因参数形状 bug
        //   (mod_chat_voice.erl:138 传裸整数不匹配 write(11005,[ErrorCode]) 子句)恒落兜底发空 cmd0。11006 双端
        //   UNUSED(客户端零引用,老端未注册 handler)。11010 缓存(CHAT_CACHE)里历史语音条目仍会被现有
        //   ReadCacheMessage 兼容解析(voice_id/voice_time 字段照读,不崩,只是不再产生新语音)。不移植语音收发。
        // 11011(世界频道剩余发言次数):双端 UNUSED——客户端不发送、不注册 recv,老端"次数"UI 已移除,
        //   服务端 lib_chat:send_world_channel_left_count 因无人调用永不触发。
        // 11022(帮派求助/击杀Boss广播):h5/src 全仓库零引用(仅协议定义),该"一键喊帮派"功能未接入 UI,跳过。
        // 11024(小跨服聊天状态推送,幻兽区域版):服务端 pp_chat.erl 无 handle 子句、唯一生产者调用点被注释、
        //   push 逻辑硬编码返回 0——双向彻底死代码,与 11023 是"两套小跨服开关"历史遗留,只用 11023 一套。
        // 11040/11041(GM禁言/解禁言):服务端仅 figure.gm==1 才生效,h5/src 玩家客户端零引用,纯 GM 工具通道。
        // 11043(获取禁言信息)/11044(聊天举报)/11045(聊天举报带内容):h5/src 玩家客户端全部零引用——
        //   11043 虽服务端有越权查询缺口(无需校验目标是否自己)但客户端从不发起;11044/11045 真正的"举报头像"
        //   入口(ChatMenuView.ts"举报头像"按钮)走的是好友/关系模块另一套协议,不是这两个号。三者均 UNUSED。
        // 11082/11083(聊天图片,分片实验版):pt_110.erl 全文件无编解码定义,pp_chat.erl 对应 handle 整段注释
        //   "暂时不开启,开启要测试"——三层(读/处理/写)全死,是被现役图片协议 11007/11008/11009 取代的废弃分支,
        //   与礼包码 15087(pt_150/goods 模块)毫无关系。彻底不实现。

        // ----- GM 秘籍(111xx,yu_server pt_111.erl / pp_gm.erl) -----
        /// <summary>请求 GM 秘籍清单(无字段)。回包:u16 分类数 × { s 分类名,
        /// u16 命令数 × { s 命令, s 中文名, u16×s 参数描述, u16×s 默认值 } }。</summary>
        public const int GM_CHEAT_LIST = 11100;

        /// <summary>执行 GM 秘籍。发 "s" 命令串(命令_参数_参数,如 "lv_100"、"goods_36010001_10")。
        /// 鉴权:服务端 gm_password 为空则全放行;否则先发 "setgmpassword_密码"。</summary>
        public const int GM_CHEAT_EXEC = 11101;

        // ----- Scene / map (120xx, yu_client h5/src/scene/SceneController.ts) -----
        /// <summary>主角移动上报(对标 SceneController.ts:1042 moveRequestHandler)。
        /// 发 "ihhchhhh"(scene_id, 当前x, 当前y, move_type, 目标x, 目标y, 起飞x, 起飞y);
        /// 摇杆普通移动时 move_type=0(NORMOL_MOVE),目标=当前坐标,起飞=0,约每 0.5s 上报一次。</summary>
        public const int SC_MOVE = 12001;
        /// <summary>Scene load complete / request scene snapshot. Send empty; reply is the full scene snapshot.</summary>
        public const int SC_LOAD_SCENE = 12002;
        /// <summary>其他玩家进入视野(单条,RoleVo)。回包体同 12002 内的玩家块,见 pt_120 binary_to_12003。</summary>
        public const int SC_ROLE_ADD = 12003;
        /// <summary>玩家离开视野。回包 "l"(role_id)。</summary>
        public const int SC_ROLE_REMOVE = 12004;
        /// <summary>删除场景对象(怪物/其他)。回包 "i"(instance_id)。</summary>
        public const int SC_ENTITY_DELETE = 12006;
        /// <summary>怪物/采集物进入视野(单条,MonsterVo)。回包体同 12002 内的怪物块,见 pt_120 binary_12007。</summary>
        public const int SC_MONSTER_ADD = 12007;
        /// <summary>场景对象通用位置同步。回包 "hhi"(x, y, instance_id)。</summary>
        public const int SC_SCENE_MOVE = 12008;
        /// <summary>场景对象血量更新。回包 "lll"(obj_id, hp, hpLim)。</summary>
        public const int SC_HP_UPDATE = 12009;
        /// <summary>隐身字段广播。回包 "clc"(sign, id, hide)。</summary>
        public const int SC_HIDE = 12070;
        /// <summary>幽灵字段广播。回包 "clc"(sign, id, ghost)。</summary>
        public const int SC_GHOST = 12071;
        /// <summary>分组字段广播。回包 "cll"(sign, id, group)。</summary>
        public const int SC_GROUP = 12072;
        /// <summary>PK 状态字段广播。回包 "clc"(sign, id, pkStatus)。</summary>
        public const int SC_PK_STATUS = 12074;
        /// <summary>展示状态字段广播。回包 "clc"(sign, id, show)。</summary>
        public const int SC_SHOW = 12075;
        /// <summary>移动速度变更广播。回包 "clh"(sign, playerId, speed)。</summary>
        public const int SC_SPEED = 12082;
        /// <summary>玩家改名通知。回包 "ls"(playerId, name)，无 sign 前缀。</summary>
        public const int SC_RENAME = 12086;
        /// <summary>九宫格玩家增删:h+12003×N(加) + h+l×N(删)。</summary>
        public const int SC_VIEW_ROLE_REFRESH = 12011;
        /// <summary>九宫格对象增删:怪物/伙伴/其他/假人(加) + i×N(删)。</summary>
        public const int SC_VIEW_OBJ_REFRESH = 12012;
        /// <summary>Change/enter scene. Send "iicchh"; reply "ihhiicc".</summary>
        public const int SC_CHANGE_SCENE = 12005;
        /// <summary>Request current scene drop list. Send empty; reply starts with u16 count.</summary>
        public const int SC_DROP_LIST = 12018;
        /// <summary>NPC task icon refresh. Send empty or receive pushed u16 count + {u32 npcId, u8 iconFlag}.</summary>
        public const int SC_NPC_ICON_REFRESH = 12020;

        // ----- Fight / 战斗 (200xx, yu_client h5/src/scene/fight/FightController.ts) -----
        /// <summary>主角技能攻击请求(C2S)。对标 FightController.ts:800 WriteBegin(20001):
        /// h+怪物实例id(i)×N + h+玩家roleId(l)×N + ihhh(skill_id, attack_x, attack_y, attack_angle)。
        /// 单体目标技能:怪列表=[目标实例id],人列表=[],x/y=目标坐标,angle=0(老端硬编码,见 FightController.ts:1238)。
        /// 服务端同号广播(S2C)=攻击结果(攻击者信息+防御者列表+伤害,FightVo),本期只记录原始响应取证(完整解析=P4)。</summary>
        public const int CS_FIGHT_ATTACK = 20001;

        // 20003(怪打玩家广播)跳过:服务端 pt_200.erl/pp_battle.erl 均无对应子句(空号,catch-all no_match);
        // 老端虽有 handler20003 实现,判定是对旧服务端协议的遗留,当前权威服务端不会下发,客户端严禁发。

        /// <summary>进/出战斗态(C2S)。对标 FightController.ts:889 SendFmtToGame(20024,"c",1/2):1=进战斗态 2=出战斗态。
        /// 老端由 CHANGE_FIGHTING_STATE 驱动(受 ConfigClientScene.fighting_state_invalidate 限制)。</summary>
        public const int CS_FIGHTING_STATE = 20024;
        /// <summary>采集请求/回包(同号 C2S+S2C)。对标 FightController.ts:867 SendFmtToGame(20008,"iic",ins_id,type_id,flag)
        /// + handler20008(ts:583)。发 "iic"(采集物实例 id, 采集物 type_id, flag:1=请求开始/2=请求完成/3=取消);
        /// 回包 "c"(flag:1=开始成功 START→播蹲下采集动作+进度条、2=完成成功 COMPLETE→收尾删采集物、≥3=各类失败/取消)。</summary>
        public const int CS_COLLECT = 20008;

        // ----- Fight 扩容(自动循环 队列#2 轮2;字段序=服务端 yu_server src/pt/pt_200.erl 权威实测,
        //        与老端 yu_client FightController.ts ReadFmt 冲突处已在注释标注、以服务端 write 为准) -----
        /// <summary>攻击失败返回(S2C 专用推送,客户端严禁发)。对标 pt_200.erl:106-110 write(20005,...):
        /// ErrCode:c, Sign1:c, User1:l(64位), Hp1:l, X1:h, Y1:h, Sign2:c, User2:l, Hp2:l, X2:h, Y2:h,
        /// InexistenceList[u16×{Id:i}]。⚠与老端 FightController.ts ReadFmt("ccishlhhcishlhh") 冲突
        /// (老端把 role_id 读成 i 且多读 plat_name/server_id,服务端源码里根本没有这两个字段)——老端该处理体
        /// 整段是注释掉的死代码,只剩打日志,判定其 ReadFmt 早已与服务端不符;按服务端 write 顺序为准,仅供日志,
        /// 不做任何状态处理(老端亦然)。error_flag 语义(老端注释):1=对方没血 2=出手太快 3=自己没血 4=距离太远
        /// 5=技能cd未到(41=移除已死怪物列表/10=怒气不足/30=重置三连击,均系老端死代码注释,未接配置表)。</summary>
        public const int CS_FIGHT_ATTACK_FAIL = 20005;

        /// <summary>辅助技能释放(双向,预表现+服务端广播确认的两段式协议,对标 FightController.ts SendAssistSkill/AssistVo)。
        /// 发 "li"(role_id, skill_id;对标 FightController.ts:1748);回包(pt_200.erl:113-117 write(20006,[Sign,Id,SkillId,SkillLv,_Act,AssList])
        /// 的 Data1 部分,字段序=<see cref="Scene.Vo.AssistVo"/>.ReadFromProtocal,与老端 AssistVo.ts:19-23 ReadFmt("lcic") 一致):
        /// RoleId:64(攻击者), AttackerType:8, SkillId:32, SkillLevel:8,
        /// 随后 DefenseNum:16 + {TypeFlag:8, RoleId:64, Hp:64, BuffNum:16 + {IconType:16,BuffEffectId:16,Id:32,Level:8,Diejia:8,
        /// Integer:32,Decimals:32,Period:64}×BuffNum}×DefenseNum(对标 pt_200.erl:226-233 assist_list/1,无 anger/damage/pos/moveAnim,
        /// 比 20001 的 FightVo 精简得多,不要混用 FightVo 的字段表)。两段式表现语义:发送前尽力复用现有攻击表现通道预播出手动作,
        /// 广播回来后再按真实 defense_list 结算血量——两段都保留,不去重(对标老端 SendAssistSkill 本地 PlayActions 预播 +
        /// handler20006 广播权威表现)。</summary>
        public const int CS_ASSIST_SKILL = 20006;

        /// <summary>buff 技能清理(S2C 专用推送,客户端严禁发)。对标 pt_200.erl:120-123 write(20007,[Sign,Id,Dels]):
        /// Sign:c, Id:l, Dels[u16×{BuffType:h, BuffSkillId:i}]。服务端强制清 buff 广播(驱散/combo 中断等)。</summary>
        public const int CS_BUFF_CLEAR = 20007;

        /// <summary>拾取怪物(同号 C2S+S2C)。对标 FightController.ts:896 onCollideMonsterHandler:
        /// 发 "h"+n×"i"(count, 逐个 instance_id,动态 fmt);回包(pt_200.erl:138-143 write(20010,ResList))
        /// "h"+循环{Res:c, SrcId:i}。Res==1→toast「拾取成功」,其余按老端一律清待拾取标记(失败文案老端已死代码化)。</summary>
        public const int CS_PICK_MONSTER = 20010;

        // 20011(与赏金怪物对话)跳过:服务端 handle(20011,...) 整段被注释,命中 catch-all,客户端严禁发。
        // 20012(客户端申请扣血)跳过:仅 ?SCENE_TYPE_KF_TEMPLE(幻兽之域)场景生效,该系统未移植。

        /// <summary>被杀信息(同号 C2S+S2C:主用途是 S2C 死亡广播;C2S 空包查询仅登录死亡恢复用——服务端
        /// pp_battle.erl:208-220 仅当 hp&lt;=0 且有 LastBeKill 记录才回,其余静默 skip,发了无害)。对标
        /// FightController.ts:506-541 + pt_200.erl:150-152 write(20013,[AttSign,Name,PkValue,BGold,Lv,Turn,AttId]):
        /// AttSign:c(killerType,1=怪/2=玩家), Name:s(killerName,可能与 config_mon 不一致),
        /// PkValue:h(现在的罪恶值,读弃), BGold:c(扣除的元宝,读弃,服务端恒传0), Lv:h(玩家等级,读弃),
        /// Turn:c(几转,读弃), AttId:l(killerId,怪为 instance_id 非模板 id,需 3 级 fallback 查真名)。
        /// 死亡→复活弹窗的唯一触发信号(老端 Fire(SHOWRELIVEWINDOW,0))。</summary>
        public const int CS_KILLER_INFO = 20013;

        /// <summary>击杀信息(S2C 专用推送,客户端严禁发)。老端 FightController.ts 无对应 recv 实现(全仓库搜索
        /// 20014 零命中),本轮按服务端权威 pt_200.erl:155-157 write(20014,[Name,IsShowPkV,PkValue]) 解析:
        /// Name:s, IsShowPkV:c, PkValue:h。</summary>
        public const int CS_KILL_INFO = 20014;

        /// <summary>广播 PK 值(S2C 专用推送,客户端严禁发)。老端无对应 recv 实现,按服务端权威
        /// pt_200.erl:160-161 write(20015,[RoleId,PkValue]) 解析:RoleId:l, PkValue:h。</summary>
        public const int CS_PK_VALUE = 20015;

        // 20016 跳过:pt_200.erl/FightController.ts 均无任何相关子句,纯空号。

        /// <summary>清理刚放技能CD(S2C 专用推送,客户端严禁发)。老端无对应 recv 实现,按服务端权威
        /// pt_200.erl:168-169 write(20018,[SkillId]) 解析:SkillId:i。</summary>
        public const int CS_SKILL_CD_CLEAR = 20018;

        // 20019(圣灵特殊技能释放通知)跳过:圣灵系统未移植。

        /// <summary>抢夺归属(同号 C2S+S2C)。对标 FightController.ts:875 SendFmtToGame(20020,"i",instance_id);
        /// 回包(pt_200.erl:175-176 write(20020,[ErrCode,MonId])):ErrCode:i, MonId:i。ErrCode==1→toast「抢夺成功」
        /// +归属事件;否则错误码。⚠老端触发源(SNATCHING_OWNERSHIP 事件)全仓库无 UI Fire,发送侧当前孤立,
        /// 本轮只留 API,交互点未来另补。</summary>
        public const int CS_SNATCH_OWNERSHIP = 20020;

        /// <summary>查看归属(同号 C2S+S2C)。对标 FightController.ts:879 SendFmtToGame(20021,"i",instance_id);
        /// 回包(pt_200.erl:179-180 write(20021,[MonId,FirstId])):MonId:i, FirstId:l。⚠老端触发源
        /// (CHECK_OWNERSHIP 事件)同样全仓库无 UI Fire,发送侧孤立,本轮只留 API。</summary>
        public const int CS_CHECK_OWNERSHIP = 20021;

        /// <summary>模拟战斗结果/强制死亡广播(S2C 专用推送,客户端严禁发)。对标 FightController.ts:556-569
        /// + pt_200.erl:183-184 write(20022,[KillerId,PlayerId,Hp,HpLim]):KillerId:l, PlayerId:l(即 died_id),
        /// Hp:l, HpLim:l。died_id==主角时仅记录 killer(不 Fire 复活弹窗信号,老端如此,弹窗只认 20013)。</summary>
        public const int CS_SIMULATE_FIGHT = 20022;

        /// <summary>战斗能量更新(同号 C2S+S2C)。对标 FightController.ts:570-573;发空查询,
        /// 回包(pt_200.erl:187-188 write(20023,[Energy])):Energy:h。老端事件名拼写 UPDATE_FIGHT_ENEERGY
        /// (少个 R,老端本身笔误,Unity 侧不沿用错误拼写,仅在此注释存档)。</summary>
        public const int CS_FIGHT_ENERGY = 20023;

        /// <summary>技能CD结束时间通知(S2C 专用推送,客户端严禁发)。对标 FightController.ts:683-690
        /// + pt_200.erl:204-205 write(20027,[SkillId,EndTime]):SkillId:i, EndTime:l(64位)。⚠老端读取是
        /// **单条**(变量名 skill_list 但无 count 前缀/无循环,只 push 一个元素),不要脑补成数组循环。</summary>
        public const int CS_SKILL_CD_END = 20027;

        /// <summary>触发技能列表(S2C 专用推送,客户端严禁发,伙伴/联携技能表现)。对标 FightController.ts:692-703
        /// + pt_200.erl:207-210 write(20028,[SkillIdL]):SkillNum:h + 循环 SkillId:i。</summary>
        public const int CS_TRIGGER_SKILLS = 20028;

        // 20025/20026(圣域Boss采集/采集被打断)跳过:归 Boss 包(实际归属 BossController.ts,非 FightController)。
        // 20201-20205(免战保护 pp_protect)跳过:归 Boss 包。

        // ----- Relive / 复活(200xx 续,yu_client h5/src/commonController/ReliveController.ts) -----
        /// <summary>复活请求/结果(同号 C2S+S2C,一号双向)。对标 ReliveController.ts:66-127(recv)/200-213(send);
        /// 发 "c"(relive_mode,服务端 guard 见 pt_200.erl:29-30 + pp_battle.erl:82-91 白名单);
        /// 回包(pt_200.erl:101-103 write(20004,[Type,Res])):Type:c(回传请求方式), Res:c(结果码,全表见
        /// ReliveController 注释)。⚠REVIVE_BOSS/REVIVE_ASHES 类型复活成功时服务端把 Res 强改成 12
        /// (pp_battle.erl:102-107),12 按成功路径处理。</summary>
        public const int RELIVE_REQUEST = 20004;

        /// <summary>复活时间戳查询(同号 C2S+S2C)。对标 ReliveController.ts:60-64 + FightController.ts:870-873
        /// GAME_START 发空包;回包(pt_200.erl:41-42 读体空 / 134-135 write(20009,[IsRevive,ReviveTime])):
        /// IsRevive:c(can_relive), ReviveTime:i(next_relive_time,服务器时间戳)。副本内答复来自
        /// lib_dungeon:send_reveive_info,野外答复来自玩家自身 revive_status,纯查询无副作用。</summary>
        public const int RELIVE_INFO = 20009;

        /// <summary>5分钟回城复活次数查询(同号 C2S+S2C,亦有服务端主动推送)。对标
        /// pt_200.erl:62-63(读体空)/164-165 write(20017,[ReviveNum,EndTime]):ReviveNum:h, EndTime:i。
        /// lib_revive.erl:442-463 add_revive_tired 会在 boss/幻兽boss 场景死亡复活时主动维护并主动推送同号。</summary>
        public const int RELIVE_TIRED = 20017;

        // ----- Task (300xx, yu_client h5/src/commonController/TaskController.ts) -----
        /// <summary>Task full list. Send empty; reply h + task list, then h + received task list.
        /// Task item format: i task_id, h tip_count, then each tip "c s c i i i i i h h c".</summary>
        public const int TASK_LIST = 30000;

        /// <summary>Single task update. Reply has the same ReadTaskVo payload as TASK_LIST entries.</summary>
        public const int TASK_UPDATE_ONE = 30001;
        public const int TASK_LATEST_FINISHED = 30005;

        /// <summary>接受任务(对话 TRIGGER 节点点击)。发 "i"(task_id)。对标老端
        /// DialogueController.AcceptTask → TaskModel.Fire(REQUEST_CCMD_EVENT, 30003) → SendFmtToGame(30003,"i",task_id)。
        /// 成功后服务端推 30001 刷新该任务,客户端无需解 30003 回包即可见状态变化。</summary>
        public const int CC_TASK_ACCEPT = 30003;
        /// <summary>提交/完成任务(对话 FINISH/FINISH_AND_TRIGGER 节点点击)。发 "i"(task_id)。对标老端
        /// DialogueController.FinishTask → 30004。成功后服务端推 30001/新任务,客户端据此刷新。</summary>
        public const int CC_TASK_FINISH = 30004;
        /// <summary>对话事件(对话 TALK_EVENT 节点点击)。发 "i"(npc_id,注意传的是 npc_id 不是 task_id)。
        /// 对标老端 DialogueController.TalkToNPC → 30007。</summary>
        public const int CC_TASK_TALK_EVENT = 30007;

        // ----- 技能(21xxx + 13007,yu_client h5/src/skill/SkillController.ts)进游戏 GAME_START 后请求 -----
        /// <summary>技能总表。请求无参;回包(On21002 → SkillManager.CreateSkillList):
        /// len:h + {skill_id:i, skill_lv:h}×len。建 mySkillList,据 ConfigSkillUI.carrerSkillList 刷 shortcutList。</summary>
        public const int SKILL_LIST = 21002;
        /// <summary>技能快捷栏。请求无参;回包(on13007):len:h + {pos:c, type:c, skill_id:i, is_auto:c}×len,按 pos 升序。
        /// type==2 的项可覆盖默认 shortcutList 顺序(GetSkillBarAutoFightSkillOrder)。</summary>
        public const int SKILL_SHORTCUT_BAR = 13007;

        // ----- 技能成长线(自动循环 轮3;yu_server src/pt/pt_210.erl 权威字段序,逐条与 yu_client
        //        h5/src/skill|innateSkill/*.ts 交叉核对;21010/21011/21012 turn&lt;4 静默 skip,非 errcode) -----
        /// <summary>职业(被动)技能升级。发 "i"(skill_id);回包(pt_210.erl:29-37 write(21001,[Errcode,SkillId])):
        /// Errcode:32, SkillId:32。发送前材料预校验(config_skill 该级 condition 里的 {goods,TypeId,Count} 项,
        /// 对标 SkillPassiveSubItem.ts:94 的意图——但老端该处 next_vo.condition.goods 缺 ErlangParser 解析步骤,
        /// 实测恒 undefined,是死代码;本端按 InnateUpInfoItem.ts:194 的正确用法用 ErlangParser 修正实现,见 SkillConfigs.TryGetGoodsCost)。
        /// errcode==1 → Emit EVT_SKILL_LEVEL_UP(服务端会自动补推 21002,勿手动重拉);否则显码降级 toast。</summary>
        public const int SKILL_UPGRADE = 21001;
        /// <summary>天赋技能面板信息。请求空包(GAME_START 追加发,对标老端无条件;服务端 turn&lt;4 静默不回,非 errcode)。
        /// 回包(pt_210.erl:104-119 write(21010,[LessPoint,TalentSkills])):
        /// LessPoint:16, Len:16, {SkillType:8, Point:16, Len:16, {SkillId:32, SkillLv:16}×N}×N。</summary>
        public const int TALENT_INFO = 21010;
        /// <summary>学习/加点天赋技能。发 "i"(skill_id);回包(pt_210.erl:121-133 write(21011,[Errcode,SkillId,SkillLv,LessPoint])):
        /// Errcode:32, SkillId:32, SkillLv:16, LessPoint:16。发送前前置校验(SkillTalentModel.CanLearn,对标
        /// InnateUpInfoItem.ts:126 满级/less_point/point分支/pre_skill(2));成功→补发 21010 刷全量 + Emit EVT_TALENT_LEARNED。</summary>
        public const int TALENT_LEARN = 21011;
        /// <summary>重置天赋技能。请求空包(老端**不做**客户端拦截,道具够不够都发,errcode 兜底,对标
        /// InnateSkillView.ts:114-135)。回包(pt_210.erl:135-143 write(21012,[Errcode,AllPoint])):Errcode:32, AllPoint:16。
        /// 成功(==1)→ toast「天赋重置成功」,服务端会主动重放 21010。</summary>
        public const int TALENT_RESET = 21012;

        /// <summary>保存快捷栏。发 "ccic"(pos,type,skill_id,is_auto);回包(pt_130.erl:199-200 write(13008,State)):
        /// State:8(1成功/0失败,**非 32 位 errcode 宏**)。==1 → toast「保存成功」+ 重拉 13007。老端/权威协议表均无 UI 触发源
        /// (13008/13010 在 h5/src 里只有 SkillController 自注册,无任何 View Fire),本轮只提供协议 API,无 UI 触发对标老端现状。</summary>
        public const int QUICKBAR_SAVE = 13008;
        /// <summary>替换(交换)快捷栏两个槽位。发 "cc"(pos1,pos2);回包(pt_130.erl:207-208 write(13010,State)):
        /// State:8(1成功/0失败,非 errcode)。==1 → toast「替换成功」+ 重拉 13007。同上无 UI 触发源。</summary>
        public const int QUICKBAR_SWAP = 13010;

        /// <summary>职业技能给予的 buff 列表(纯 S→C 异步推送,客户端严禁发;服务端 pp_scene.erl:276-280 cast 到场景进程,
        /// mod_scene_agent_cast.erl:537-546 异步回,查无用户静默不回)。回包(pt_120.erl:448-451 write(12093,[SkillL])):
        /// Len:16, {SkillId:32, SkillLv:16}×N。存 SkillTalentModel.CareerSkillBuffList + Emit EVT_CAREER_SKILL_BUFF;
        /// HUD buff 图标行现成通道 MainUIBuffView.RefreshBuffList 需要 buff_cfgs 等价配置表(未加载)+ 挂载点 MainUIFlow.cs
        /// 当前基线脏(并行会话在改),本轮只落数据 + log,不接 HUD(见汇报)。</summary>
        public const int CAREER_SKILL_BUFF = 12093;

        /// <summary>模块加成效果列表(GAME_START 延迟2帧追加发空包,对标老端)。回包(pt_184.erl:12-25 write(18401,[BuffList])):
        /// Len:16, {Key:32, ValuesStr(pt:write_string)}×N。key==2:Values 是 Erlang term 串(如 "[{onhook_time,N}]"),
        /// 解出 onhook_time 后 OnhookExtraSec=20*3600+onhook_time(对标老端 OutLineModel.max_outline_time),写入
        /// OnHookController.MaxOnlineTimeSec(消费方 13216 领取挂机收益处关联);key==6:Values 是裸数字串,
        /// LifeSkillAdd=Number(values)(对标老端 CompositeModel.lifeSkillAdd,本端未有对应 Compose 模块字段,存 SkillTalentModel 同 dict)。
        /// 全量存 SkillTalentModel 泛用 dict + Emit EVT_MODULE_BUFF_LIST。</summary>
        public const int MODULE_BUFF_LIST = 18401;

        // 以下号跳过(仅存说明,不写代码;主控三路侦察定案,详见规格 §0):
        // 21003/21004/21005(技能强化):双端死——老端 SkillController.ts on21003/on21004/on21005 读了就丢弃/整段函数体空,
        //   服务端 pp_skill.erl:29-39 对应处理分支被注释,请求落兜底静默(不回包)。config_skill_stren 表配套模型方法
        //   (SkillUIModel.GetSkillStrenInfo 等)全仓库零调用。不移植。
        // 21101/21102/21103/21104(远古奥术):老端 tabStrList 第4档"远古奥术"tab 被注释(SkillSubView.ts:43),
        //   forbbiden_skill_info 恒 null 无赋值语句,ForbiddenSkillView 视图类全仓库不存在;服务端模块
        //   pp_arcana.erl:7 标 @deprecated(但协议路径本身存活,注意不要被注释误导为服务端死链——纯粹是客户端视图层缺失)。
        //   GAME_START 也不发 21101(老端那发是空耗,不复刻)。不移植。

        // ----- AutoBrush / main-line guard (133xx, yu_client h5/src/commonController/AutoBrushController.ts) -----
        /// <summary>Auto-brush monster progress. Send empty; reply "iiill".</summary>
        public const int AUTOBRUSH_INFO = 13300;

        /// <summary>Auto-brush rank/basic level info. Send empty; reply starts "cii" + rank list.</summary>
        public const int AUTOBRUSH_RANK = 13301;

        /// <summary>Enter/exit main-line auto-brush dungeon. Send "c"; reply "i".</summary>
        public const int AUTOBRUSH_ENTER_EXIT = 13305;

        /// <summary>Main-line auto-brush pass result. Reply "icii" + reward_array.</summary>
        public const int AUTOBRUSH_RESULT = 13306;

        /// <summary>Toggle auto-brush. Send "c"; reply "ic".</summary>
        public const int AUTOBRUSH_TOGGLE = 13307;

        /// <summary>退出副本(通用,对标老端 BaseDungeonController 61002)。发空;回包 error_code:i(==1 成功)。
        /// 主线副本(AutoBrush)结算/失败后由客户端主动发此包退副本,服务端再用 12005 把玩家切回野外野场景。</summary>
        public const int DUNGEON_EXIT = 61002;

        // ----- 功能开放达成奖励(138xx,yu_server pt_138.erl) -----
        /// <summary>已完成功能列表。请求无参;回包 h + {Id:h, State:c}×N。</summary>
        public const int FUNC_OPEN_LIST = 13800;
        /// <summary>领取功能开放奖励。发 "hc"(id, state);回包 Code:i, Id:h, State:c。</summary>
        public const int FUNC_OPEN_CLAIM = 13801;
        /// <summary>新功能开放推送(S2C)。h + {Id:h}×N。</summary>
        public const int FUNC_OPEN_NEW = 13802;

        // ----- 玩家信息(130xx,yu_server pt_130.erl)进游戏后服务端主动推送 -----
        /// <summary>主角全量信息(进游戏首推)。回包见 pt_130 write(13001):
        /// Id:l, 平台:s, 服数:h, 跨服消息:s, 服id:h, 服名:s, Figure块, BattleAttr块,
        /// 场景:i, X:h, Y:h, 副本:i, 经验:l, 经验上限:l, 元宝:i, 绑元:i, 铜币:l, 帮贡:i,
        /// 战力:l, 帮派id:l, 帮派名:s, pk变更时间:h, pk值:h, 队伍id:l, 配偶id:l, ip:s, 阵营:h, 注册时间:i。</summary>
        public const int ROLE_INFO = 13001;
        /// <summary>经验变更。回包 "l"(exp)。</summary>
        public const int ROLE_EXP = 13002;
        /// <summary>升级。回包 "hll"(level, exp, expLim)。</summary>
        public const int ROLE_LEVEL = 13003;
        /// <summary>货币。回包 "liii"(铜币 coin, 元宝 gold, 绑元 bgold, 帮贡 gcoin)。</summary>
        public const int ROLE_CURRENCY = 13006;
        /// <summary>战斗属性/战力更新(攻防血等重算后服务端推,对标老端 RoleController.On13033 → ReadFrom13033)。
        /// 回包首 "l"=战力,后接战斗属性块;本端目前只取战力驱动「战力提升」弹层(后续属性块按需扩展)。</summary>
        public const int ROLE_BATTLE_UPDATE = 13033;
        /// <summary>切换 PK(战斗)模式(对标老端 PkStatusController.ts:29 SendFmtToGame(13012,"c",type))。
        /// 发 "c"(目标 pk_status);回包 "ici"(error_code, pk_status, remain_time):
        /// code==1 且 remain==0 → 切换成功;code==1 且 remain>0 → 进入和平切换冷却(peace_cd_time);其余为错误码。</summary>
        public const int PK_STATUS_CHANGE = 13012;
        public const int ROLE_LIFELONG_COUNT = 13088;

        // ----- 角色成长补全 + 改名 + 转职(自动循环 轮5;yu_server pt_130.erl/pt_426.erl 权威字段序,
        //        与 yu_client RoleController.ts 交叉核对,冲突处已按服务端为准改正并在注释标注) -----
        /// <summary>世界等级(GAME_START 裸发 + 服务端同号推送更新)。回包 "Hh"(⚠反直觉,对标 pt_130.erl:211-212
        /// write(13011,[ExpAdd,ServerLv])→&lt;&lt;ExpAdd:16/signed, ServerLv:16&gt;&gt;):
        /// worldLvExp:H(16位有符号,经验加成%), worldLv:h(16位无符号,世界等级)。</summary>
        public const int ROLE_WORLD_LEVEL = 13011;
        /// <summary>查看他人 Figure(通用"拉人物模型"通道,被排行榜/记录列表等多处复用,非自身面板)。
        /// 发 "hlh"(server_id, role_id, module_id;module_id 是调用方自定义来源标签)。
        /// 回包(pt_130.erl:218-221 write(13013,[ServerId,ServerNum,Id,ModId,Power,Figure,ServerName])):
        /// server_id:h, player_num:h, player_id:l, module_id:h, fighting:l, +FigureProto 块, platform:s
        /// (服务端字段名 ServerName,老端变量名 platform,同一尾串字段,不是两个字段)。</summary>
        public const int ROLE_FIGURE_QUERY = 13013;
        /// <summary>玩家托管(自动战斗)状态(GAME_START 裸发)。回包 "c"(1=托管中)。
        /// 战斗表现门控用(老端 Scene/FightMovie 多处消费跳过本地预表现/动画插值),本端仅落 RoleModel,
        /// 门控消费未接(TODO)。</summary>
        public const int ROLE_DEPOSIT_STATE = 13017;
        /// <summary>被动技能解锁通知(S2C 专用推送,客户端严禁发;服务端 pt_130.erl 无 read 子句)。
        /// 回包 len:h + skill_id:i×len。逐个按 config_skill[id].type==2(被动)静默并入
        /// SkillManager 技能列表(level=1,对标老端 AddSkillToSkillList),无事件Fire、无toast。</summary>
        public const int ROLE_SKILL_PASSIVE_UNLOCK = 13020;
        /// <summary>经验获得飘字(S2C 专用推送,客户端严禁发;服务端 pt_130.erl 无 read 子句)。
        /// 回包 "clh"(expType:c, exp:l, percent:h)。expType 枚举:0无/1GM/2任务/3个人杀怪/4副本/5队伍杀怪/
        /// 6物品用加经验/7离线经验找回/8帮派宴会/9挂机经验物品找回/14(未注释)。分支对标老端 RoleController.ts:305-367
        /// (expType==3 副本刷条分支不 return,继续走底部通用飘字;6/8/14 提前 return;"6||8||2"实际只剩2可达)。</summary>
        public const int ROLE_EXP_FLOAT = 13036;
        /// <summary>转职确认(道具"转职卡" type38/subtype39 使用触发)。发 "cc"(career, sex)。
        /// 回包(pt_130.erl:281-283 write(13045,[ErrorCode,ErrorCodeArgs,NewCareer,NewSex])):
        /// error_code:i, args:s, career:c, sex:c。==1 → Emit EVT_CAREER_CHANGED + 级联重拉 13080/13046/21002
        /// (对标老端 MainRoleVo.changeCareer);OutwardChangedView(外观变更通用展示窗)未移植,TODO log。
        /// 挂 <see cref="Shenxiao.Module.Core.TransferJob.TransferJobController"/>(新模块,不归 RoleController)。</summary>
        public const int TRANSFER_JOB_CHANGE = 13045;
        /// <summary>转职冷却时间(GAME_START 裸发 + 转职成功后重拉)。回包 "i"=change_career_time,
        /// **绝对服务器时间戳,不是剩余秒**(与 <see cref="Bag.BagModel"/> 等"剩余秒转绝对时间"存法相反,
        /// 存 RoleModel.ChangeCareerTime,勿复用同一转换 helper)。消费方(道具tooltip冷却展示)未接,TODO。</summary>
        public const int TRANSFER_JOB_COOLDOWN = 13046;
        /// <summary>头像三件套 1/3——激活头像列表(GAME_START + 开窗时拉)。回包 "h"+i×len(head_id 列表)。
        /// 存 RoleModel.HeadIdList;id==1/3 恒视为已激活(硬编码照抄老端 RoleManager.HaveActiviteThisHead)。</summary>
        public const int ROLE_HEAD_LIST = 13080;
        /// <summary>头像三件套 2/3——激活头像(S2C 推送)。⚠字段序按服务端权威改正:pt_130.erl:302-303
        /// write(13081,[Res,Id]) → &lt;&lt;Res:32, Id:64&gt;&gt;(规格草案假设"recv c,i"抄自老端 TS 形状,
        /// 但老端客户端请求侧本就是空转 pp_player.erl:331-332 handle(13081,_,[_Id])->{ok,Status},从无真实回包验证过;
        /// 真正触发源是服务端 use_picture_goods 内部广播,与客户端发送无因果)。code==1 成功/2无此头像/3已激活/
        /// 4物品不足/5性别不符。请求侧对标老端仅绑在废弃的"自定义头像上传"(13082)成功回调,该半成品不移植,
        /// 本端只注册 recv,不提供 Send 封装。</summary>
        public const int ROLE_HEAD_ACTIVATE_PUSH = 13081;
        /// <summary>头像三件套 3/3——设置玩家头像。发 "l"(head_id)。回包(pt_130.erl:310-312
        /// write(13083,[Res,PictureVer,String])):code:i, head_ver:i, head_id_str:s。1成功→改
        /// Figure.Raw["picture"] + Emit EVT_ROLE_HEAD_SET_SUCCESS;2管理员禁止;4无该头像;else 错误码。</summary>
        public const int ROLE_HEAD_SET = 13083;
        /// <summary>查看玩家指定数据(GAME_START 裸发;双端语义标签不一致但字节序一致,以服务端权威 byte
        /// 布局实现——老端 TS 认为是"渠道播放时长统计"埋点(role_platform_times_data[style]=times),
        /// 服务端 pt_130.erl:388-392 实际取 ExpDunCount/VipBossCount/Gate 三项计数;两者 shape 同为
        /// len:h+{u8,u32}×len,仅字段命名假设不同)。回包 "h"+{type:c,value:i}×len,落 RoleModel 泛用字典,
        /// 老端亦仅 console.warn 打日志、无任何消费方,本端同样不 Emit 事件。</summary>
        public const int ROLE_MISC_COUNTERS = 13086;
        /// <summary>角色终身次数信息+1(S2C 推送,老端无客户端主动发送观测到;服务端函数头 guard
        /// 强约束 ModuleId==300&amp;&amp;SubModule==1,其余静默丢弃不回包)。回包 "hhhh"
        /// (ModuleId, SubModule, Type, Count)。落 RoleModel 通用终身计数字典(与 13088 共用存储,
        /// 见 <see cref="Role.RoleModel.SetLifelongCount"/>),Emit EVT_ROLE_LIFELONG_COUNT_UPDATE(module,sub),
        /// 无 UI 消费方(TODO)。</summary>
        public const int ROLE_LIFELONG_INCREMENT = 13089;

        // 以下号跳过(仅存说明,不写代码;逐号裁决见规格 §0 及本轮汇报"裁决表"):
        // 13082(校验能否上传头像):服务端 ALIVE(pt_130.erl:106-107 有 guard),但其唯一下游
        //   SettingUploadHeadView.ts 整个类体被注释成空壳,老端"自定义头像上传"功能半成品未完工,
        //   不移植(与其绑定的 13081 请求侧同样不建)。
        // 13084(设置 GPS 经纬度):服务端 ALIVE(pt_130.erl:115-116,guard `is_integer` 对二进制解出的整型
        //   恒真形同虚设),但老端 h5/src 全仓库找不到任何调用点,且本游戏 Unity 端无地理位置/GPS 功能,
        //   无触发源可对接,不写代码。
        // 13085:服务端 pt_130.erl 无 read/write 子句、老端全仓库零引用,双端真实不存在,不写代码。
        // 13087(挂后台切回游戏通知):服务端 ALIVE 但单向请求无回包(pt_130.erl 无 write(13087,...)子句,
        //   仅取消复活计时器副作用);老端无调用点,Unity 亦无 App 生命周期(OnApplicationPause/focusChanged)
        //   钩子系统可挂载,超出本轮"角色面板补全"范围,不写代码(留待后续 App 生命周期专项接入)。

        // ----- 改名(426xx,yu_server pt_426.erl / pp_rename.erl)-----
        /// <summary>改角色名(提交)。发 "si"(name, type;1免费/2钻石/3改名卡)。回包(pt_426.erl:24-34
        /// write(42601,[Result,Name])):result:i, name:s。⚠错误码取值以服务端为准:老端 TS On42601
        /// 硬编码假设 result 2/3/4/5/6 小整数枚举,但服务端 <c>data_error_code</c> 运行期表实际下发
        /// 1001(勾玉不足)/1008(长度不合法,提示"4-12个字符",非老端假设的"2~6汉字")/1009(重名)/
        /// 1010(非法字符)/1450002(敏感词)/4260001(今日已改)/4260002(系统升级中),两套编码冲突,
        /// 本端按服务端实测为准(见 <see cref="Role.RoleController"/> FormatRenameMsg)。成功(result==1)→
        /// toast「改名成功」+ Emit EVT_ROLE_RENAME_SUCCESS;Figure.Name 更新走既有 12086 广播路径(勿双改)。</summary>
        public const int RENAME_SUBMIT = 42601;
        /// <summary>查询是否免费改名(改名入口按钮点击发,裸请求)。回包(pt_426.erl:36-42
        /// write(42602,[Result])):result:i(1免费/2否)。收到后打开 SettingChangeNameView(result 作为
        /// is_free 参数),对标老端 SettingModel.Fire(SETTING_OPEN_VIEW,"SettingChangeNameView",scmd.result)。</summary>
        public const int RENAME_FREE_CHECK = 42602;
        /// <summary>判断是否满足改名条件(提交前预检,同 42601 消耗/长度/敏感词校验链但**不落库不扣道具**,
        /// 42601 有 <c>lib_game:is_ban_rename()</c> 系统封禁拦截而 42604 没有)。发 "si"(name, type),
        /// 回包(pt_426.erl:59-69 write(42604,[Result,Name])):result:i, name:s(格式/错误码表同 42601)。
        /// result==1 → Emit EVT_ROLE_RENAME_CHECK_PASSED(name,type),供二次确认弹窗后再发 42601。</summary>
        public const int RENAME_CHECK = 42604;
        // 42603(查看曾用名):协议契约完整但服务端 handle 子句整段被注释(pp_rename.erl:116-141),
        //   老端全仓库零 UI 入口/零调用,双端均 DEAD,不写代码(若要做需从零设计 UI,超出本轮范围)。

        // ----- Login/common kick notice (590xx, yu_server pt_590.erl) -----
        /// <summary>Server-side forced logout / kick reason. Reply payload: u16 code.</summary>
        public const int LOGIN_KICK_REASON = 59004;

        // ----- NPC (121xx, yu_client h5/src/scene/SceneController.ts) -----
        /// <summary>Request scene NPC list. Send "i" sceneId; reply sceneId + npc list.</summary>
        public const int SC_NPC_LIST = 12100;
        /// <summary>Dynamic NPC add/remove push. Reply starts with u16 count.</summary>
        public const int SC_NPC_DYNAMIC = 12103;

        // ----- 对话(121xx,yu_client h5/src/commonController/DialogueController.ts)-----
        /// <summary>NPC 关联任务/打开 NPC 对话。发 "i"(npc_id);
        /// 回包(ClientProtocol.json 12101):npc_id:i + task_list[ u16 count × {task_id:i, task_state:c, task_name:s, task_type:c} ]。
        /// task_state: 0无/1可接/2接了未完成/3完成可提交/4有任务对话。</summary>
        public const int CC_NPC_TASK_LIST = 12101;
        /// <summary>获取某任务的对话。发 "ii"(npc_id, task_id);回包(ClientProtocol.json 12102):npc_id:i, task_id:i, talk_id:i。
        /// talk_id 查 config_talk 取真实对话内容。</summary>
        public const int CC_NPC_TASK_TALK = 12102;

        // ----- 邮件(190xx,yu_server pt_190.erl / pp_mail.erl) -----
        /// <summary>请求/回:邮件列表。回包 h + {MailId:l,Type:c,State:c,Title:s,IsAttach:c,Time:i,EffectEt:i}×N。</summary>
        public const int MAIL_LIST = 19001;
        /// <summary>详情(自动循环 轮7)。发 "l"(mail_id);回包 MailId:l,Sender:s,Title:s,Content:s,
        /// Attachment[h+{ObjectType:c,TypeId:i,Num:i,ExtraAttr[h+{Color:c,TypeId:c,AttrId:h,AttrVal:i,PlusInterval:c,PlusUnit:i}]}],
        /// Time:i,IsReceive:c。老端 request_email_info 先查本地 emailInfoDic 缓存,命中不发协议——MailModel 缓存优先复刻同一行为。</summary>
        public const int MAIL_DETAIL = 19002;
        /// <summary>批量删除(自动循环 轮7)。**手写变长包**:WriteBegin+"h"(count)+"l"×count(mail_id 逐个);
        /// 回包 ErrorCode:i + MailIds[h+{MailId:l}]。服务端对"有未领附件/当日未读"的 id 静默跳过不删(lib_mail.erl:274-287),
        /// 前端也复刻 GetNoGetRewardEmailList 过滤(只删无附件或已领附件的邮件)。</summary>
        public const int MAIL_DELETE = 19003;
        /// <summary>新邮件到达增量推送(S2C 主动,轮21 PF 补漏批;对标老端 FriendController.ts:546-551
        /// On19004 `_model.addEmail(scmd.mail_list)`——**与 19001 全量列表不同,是追加/upsert 语义**,新邮件
        /// 到达时(如 GM 邮件、任务完成邮件)服务端立即推送本号,不追加就永远进不了列表。回包 mail_list
        /// [h + {MailId:l,Type:c,State:c,Title:s,IsAttach:c,Time:i,EffectEt:i}](字段同 19001/<see cref="MAIL_NEW"/>)。
        /// 服务端唯一发送点 lib_mail.erl:172-186 `add_mail/2`,发完本号必紧跟着发一次 19008(HasUnread=true)。
        /// 轮7 已发现此缺口并留 TODO(见 <see cref="MAIL_NEW"/> 旧注),本轮补齐。</summary>
        public const int MAIL_ADD_PUSH = 19004;
        /// <summary>批量领取附件(自动循环 轮7)。**手写变长包**同 <see cref="MAIL_DELETE"/> 结构;
        /// 回包 ErrorCode:i + MailIds[h+{MailId:l}] + Reward(ObjectList,老端 CongratulationObtainView 展示用)。
        /// 服务端顺序处理、遇首个失败即整体中止(已成功的 id 仍在 MailIds 里)。前端背包容量预检
        /// (对标老端 GoodsModel.CheckEquipNum)在此号发送前拦截。</summary>
        public const int MAIL_RECEIVE = 19005;
        /// <summary>发公会邮件(自动循环 轮7)。发 "ss"(title,content);回包 ErrorCode:i。
        /// 服务端 check_send_guild_mail_on_server 当前版本硬编码恒返回 not_open(lib_mail.erl:741-742),
        /// 功能实际不可用;UI 归属公会模块(GuildMailView),本轮只补 API,TODO 见汇报。</summary>
        public const int MAIL_GUILD_SEND = 19006;
        /// <summary>⚠命名历史遗留,非推送:服务端 19007 实为"取单条邮件基本信息"C2S 请求/回(read MailId:l,
        /// pp_mail.erl:83-92 `handle(19007,PS,[MailId])`),回包字段同列表项(同 <see cref="MAIL_NEW"/> 自身
        /// 即字段,非推送触发)。**老端从未发送该号**(FriendController.ts 全仓库零 SendFmtToGame(19007,...)
        /// 调用点,只注册了空 On19007),故对老端而言恒不可达。真正的"新邮件到达"推送号是
        /// <see cref="MAIL_ADD_PUSH"/>(19004),轮21 已补齐。本号既有 handler 保留(防御性,若未来真被请求触发
        /// 仍可正确落地单条数据),不提供发送 API。</summary>
        public const int MAIL_NEW = 19007;
        /// <summary>是否有未读邮件(S2C "c")。</summary>
        public const int MAIL_UNREAD = 19008;
        /// <summary>可发邮件剩余次数(S2C "c")。服务端 pp_mail.erl:98-104 handle(19009,...) 整段被注释——
        /// **DEAD**:客户端若发送该号会落服务端兜底(无回包),既有 handler 保留但实际收不到回包。</summary>
        public const int MAIL_LEFT_NUM = 19009;
        /// <summary>意见反馈/工单提交(自动循环 轮7,非"联系客服"聊天)。发 "s"(content,≤400字符);
        /// 回包 ErrorCode:i(==1 成功清空输入框,老端无论成功失败都先弹 ErrorCodeShow)。
        /// 服务端 30 秒硬编码 CD(进程字典,非通用 CD 表)。</summary>
        public const int MAIL_FEEDBACK = 19010;

        // ----- 他人资料卡(195xx,yu_server pt_195.erl / pp_look_over.erl,自动循环 轮7) -----
        /// <summary>查看资料卡(申请信号,本身不回数据)。发 "hlh"(server_id,role_id,module_id;
        /// module_id=1 基础装备/2龙珠/3影装/4神祭/5幻化/6天启/7谪仙临凡/8灵饰/9神纹/10蜃妖/11神巫妖灵/12御魂,
        /// 定义于 goods.hrl LOOK_OVER_MODULE_LIST)。回包仅 Code:i(成功路径不回包,只有失败才回错误码);
        /// 真正数据由服务器随后经对应 195xx 号推送。自己查自己/role_id=0/module_id 非法 → 服务端静默跳过或回错误码。</summary>
        public const int LOOKOVER_REQUEST = 19501;
        /// <summary>module_id=1 基础装备资料卡(本轮唯一完整解析的模块,"完整角色卡"字段)。回包
        /// ServerId:h,RoleId:l,Combat:l,AchvStage:h,Figure(FigureProto),
        /// EquipList[h+{GoodsId:l,TypeId:i,Cell:h,Color:c,Stren:h,Star:c,Stage:h,Level:h,GodLevel:h}],
        /// MagicCircle[h+{Lv:c,IsOpen:c,FreeFlag:c,EndTime:i}],FairyList[h+{Type:h,IsActive:c}]。</summary>
        public const int LOOKOVER_BASE_EQUIP = 19502;
        /// <summary>module_id=2 龙珠资料:SumPower:l,IsActive:c,BallList[],FigureList[]；轮28 已完整解析并接资料卡。</summary>
        public const int LOOKOVER_DRAGONBALL = 19503;
        /// <summary>module_id=3/4 共用号,靠首字段 SysType 分流(3=影装/4=神祭,两个完全不同 UI)——
        /// 不能按协议号一一映射 View，回调按 SysType 分发；轮28 已完整解析两分支。</summary>
        public const int LOOKOVER_SEAL_OR_DRACONIC = 19504;
        /// <summary>号与 module_id 顺序错位:19505 实为 module_id=6 天启资料(非 5)，轮28 已完整解析。</summary>
        public const int LOOKOVER_REVELATION = 19505;
        /// <summary>号与 module_id 顺序错位:19506 实为 module_id=5 幻化资料(非 6)，轮28 已完整解析。</summary>
        public const int LOOKOVER_ILLUSION = 19506;
        /// <summary>module_id=7 谪仙临凡(降神)资料，轮28 已完整解析。</summary>
        public const int LOOKOVER_GODBEFALL = 19507;
        /// <summary>module_id=8 灵饰资料，轮28 已完整解析。</summary>
        public const int LOOKOVER_UNREAL = 19508;
        /// <summary>module_id=9 神纹资料，轮28 已完整解析。</summary>
        public const int LOOKOVER_LUNG = 19509;
        /// <summary>module_id=10 蜃妖资料(历史命名 GODBEAST,与“降神”无关)，轮28 已完整解析。</summary>
        public const int LOOKOVER_GODBEAST = 19510;
        /// <summary>module_id=11 神巫+妖灵双数据，轮28 已完整解析。</summary>
        public const int LOOKOVER_PET = 19511;
        /// <summary>module_id=12 御魂资料，轮28 已完整解析。</summary>
        public const int LOOKOVER_RUNE = 19512;

        // ----- 好友(140xx,yu_server pt_140.erl / pp_relationship.erl,自动循环 轮7) -----
        // 守卫总门槛(pp_relationship.erl:22-35):除 14000+Data=[3](查黑名单)/14010(查看菜单)/
        // 14007+Type∈{2,3}(拉黑/取消拉黑)三种特例外,其余 140xx 请求都要求主角等级达到好友模块开放等级,否则服务端静默丢包不回。
        // 跳过(按规格§0):14011(单点查双方关系,老端未实现)、14012(号未分配,DEAD)、14016/14017(劲敌模块,服务端无 handler 路由)。
        /// <summary>好友/仇人/黑名单列表分桶。发 "c"(type:1好友/2仇人/3黑名单);回包
        /// Type:c,RelaList[h+{RoleId:l,Name:s,Career:c,Sex:c,Turn:c,Lv:h,Vip:c,VipHide:c,Picture:s,PicVer:i,
        /// Combat:l,OnlineFlag:c,Intimacy:i,MarriageType:c,BlockId:i,HouseId:i,HouseLv:h,IsSupvip:c,
        /// LastChatTime:i,OfflineTime:i,AddTime:i,DressList[h+{DressType:c,DressId:i}]}]。GAME_START 拉 type=1。</summary>
        public const int FRIEND_LIST = 14000;
        /// <summary>好友推荐列表。发 "c"(type:0默认/1换一批,服务端 10s CD 手写非通用 CD 表);回包
        /// Code:i,RecommendedList[h+{RoleId:l,Name:s,Career:c,Sex:c,Turn:c,Lv:h,Vip:c,VipHide:c,Picture:s,PicVer:i,
        /// Combat:l,OnlineFlag:c,IsSupvip:c}](无 intimacy/dress_list)。</summary>
        public const int FRIEND_RECOMMEND = 14001;
        /// <summary>按昵称搜索玩家。发 "s"(role_name);回包 Code:i,RoleId:l,Name:s,Career:c,Sex:c,Turn:c,Lv:h,
        /// Vip:c,VipHide:c,Picture:s,PicVer:i,Combat:l,OnlineFlag:c(单个对象,非数组;对方不在线仍带资料+err14_not_online)。</summary>
        public const int FRIEND_SEARCH = 14002;
        /// <summary>发送加好友申请。发 "l"(be_ask_id;假人推荐位老端强制传0仍照发,服务端按普通 id 处理);
        /// 回包 Code:i。**无冷却**(服务端 CheckList 不含时间校验)。</summary>
        public const int FRIEND_ADD_APPLY = 14003;
        /// <summary>一键处理好友申请(0拒绝/1接受)。发 "c"(response_type);回包 Code:i。
        /// 老端**无视 code**收到回包即清空整份本地申请列表;accept 时联动重拉好友列表(type=1)。</summary>
        public const int FRIEND_APPLY_ONE_CLICK = 14004;
        /// <summary>单条处理好友申请(0拒绝/1接受/2拉黑)。发 "lc"(ask_id,response_type);回包 Code:i。
        /// 成功后按 ask_id 从本地列表逐条移除;accept 时联动重拉好友列表(type=1)。</summary>
        public const int FRIEND_APPLY_ONE = 14005;
        /// <summary>拉取待处理好友申请列表。发 null(无参);回包
        /// AskList[h+{RoleId:l,Name:s,Career:c,Turn:c,Lv:h,Picture:s,PicVer:i,Combat:l,AddTime:i}](无 sex/vip)。
        /// GAME_START 拉一次;打开申请弹窗时也会重拉。</summary>
        public const int FRIEND_APPLY_LIST = 14006;
        /// <summary>好友关系操作。发 "cl"(type:1删好友/2拉黑/3取消拉黑/4加仇人/5移除仇人,role_id);回包 Type:c,Code:i。
        /// 服务端"命令+被动刷新"模式:先无条件重拉对应 type 完整列表(flushOperationtView),再按 code 弹提示。
        /// type=4/5(仇人)前端无 UI,只留 API(照规格保留)。情侣关系保护:对方是配偶时 type=1/2 会报错。</summary>
        public const int FRIEND_OPERATE = 14007;
        /// <summary>S2C 推送:收到新的好友申请。回包 RoleId:l,Name:s,Career:c,Turn:c,Lv:h,Picture:s,PicVer:i,
        /// Combat:l,AddTime:i(单个,无 sex)。去重后插入本地申请列表。</summary>
        public const int FRIEND_APPLY_PUSH = 14008;
        /// <summary>S2C 推送:好友/仇人上下线通知。回包 RoleId:l,Name:s,RelaType:c(1好友/2仇人),
        /// OnlineFlag:c,Timestamp:i(下线时用此时间戳换算 offline_time = 服务器现在时间 - Timestamp)。</summary>
        public const int FRIEND_ONLINE_PUSH = 14009;
        /// <summary>查看玩家交互菜单(右键头像)。发 "l"(role_id;role_id==0 或 ==自己本地拦截不发,
        /// 800ms 内重复请求同一人只更新缓存不重发);回包 Code:i,RoleId:l,Figure(FigureProto),
        /// Rela:c(0无/1好友/2仇人/3黑名单/4仇人且黑名单/5仇人且好友),TeamId:i。</summary>
        public const int FRIEND_MENU_DATA = 14010;
        /// <summary>S2C 推送:社交列表增量新增/更新。回包 UpdateList[h+{Type:c,RoleList[同 <see cref="FRIEND_LIST"/> 单项结构]}]。
        /// 按 role_id 存在则覆盖、不存在则插入对应 type 桶。</summary>
        public const int FRIEND_LIST_DELTA_UPSERT = 14013;
        /// <summary>S2C 推送:社交列表增量移除。回包 UpdateList[h+{Type:c,RoleIds[h+{RoleId:l}]}]。</summary>
        public const int FRIEND_LIST_DELTA_REMOVE = 14014;
        /// <summary>S2C 推送:好友亲密度变化。回包 RoleId:l,Intimacy:i。只在好友桶(非仇人/黑名单)命中才更新。</summary>
        public const int FRIEND_INTIMACY_PUSH = 14015;
        /// <summary>S2C 推送/兜底:140xx 系列通用错误码。回包 Code:i,非0/真值才弹提示。</summary>
        public const int FRIEND_ERROR = 14099;

        // ----- 首充(159xx 子集,yu_server pt_159.erl) -----
        /// <summary>首充信息。请求无参;回包 h+{Open:c,Index:c}×N + ProductId:i + IsNotify:c。</summary>
        // ----- Recharge/VIP (158xx, yu_server pt_158.erl / pp_recharge.erl) -----
        /// <summary>充值商品列表。请求无参;回包 h + {ProductId:i, ReturnType:c} x N。</summary>
        public const int RECHARGE_PRODUCT_LIST = 15800;
        /// <summary>充值商品返利状态变更。回包 ProductId:i, ReturnType:c。</summary>
        public const int RECHARGE_PRODUCT_UPDATE = 15801;

        public const int FIRST_RECHARGE_INFO = 15905;
        /// <summary>领取首充奖励。发 "c"(index);回包 Errcode:i, Index:c。</summary>
        public const int FIRST_RECHARGE_CLAIM = 15906;
        /// <summary>首充新号横幅展示期结束通知。请求无参、无回包；服务端据此把 15905.IsNotify 置为 1。</summary>
        public const int FIRST_RECHARGE_NOTIFY = 15907;
        /// <summary>是否已购首充。请求无参;回包 "c"(isBuy)。</summary>
        public const int FIRST_RECHARGE_ISBUY = 15908;

        // ----- Custom activity (331xx, yu_client h5/src/commonController/CustomActivityController.ts) -----
        /// <summary>Open custom activity list. Send empty; reply h + {base_type:h, sub_type:h, act_type:c, show_id:h, wlv:h, name:s, desc:s, condition:s, stime:i, etime:i}.</summary>
        public const int CUSTOM_ACTIVITY_LIST = 33101;

        // ----- 头号玩家 / 冲榜 (225xx, yu_client CustomActivityController On22501/On22502) -----
        /// <summary>头号玩家某榜单信息。发 "ih"(rank_type, sub_type=1);回包
        /// rank_type:i, sel_rank:i, sel_val:l, sum:i, max_len:h, rank_limit:i, status:c, end_time:l, is_combat:c,
        /// rank_list[u16×{player_id:l, name:s, first_value:l, rank:i}]。</summary>
        public const int TOP_PLAYER_RANK_INFO = 22501;
        /// <summary>头号玩家目标奖励信息(红点用)。发 "h"(sub_type);回包
        /// goal_list[u16×{rank_type:i, goal[u16×{goalId:l, status:c}]}]。</summary>
        public const int TOP_PLAYER_GOAL_INFO = 22502;

        // ----- 循环冲榜 / 竞榜 (227xx, yu_server pt_227, yu_client CycleimpActlistController) -----
        /// <summary>获取正在开启的竞榜活动(GAME_START 拉一次)。发无参;回包
        /// type:h, subtype:h, start_time:i, end_time:i, upon_end_time:i。type&&subtype 非0=有活动开启。</summary>
        public const int CYCLE_RANK_OPENING = 22700;
        /// <summary>竞榜界面信息(个人)。发 "hh"(type,subtype);回包
        /// type:h, subtype:h, is_open:c, score:i, rank:h, id:h, got_type:c。</summary>
        public const int CYCLE_RANK_PANEL = 22701;
        /// <summary>竞榜榜单(主面板榜首取这里)。发 "hh"(type,subtype);回包
        /// type:h, subtype:h, score:i, rank:h, reward_id:h, rank_list[u16×{rank:h, server_id:i, role_id:l, role_name:s, role_score:i}]。</summary>
        public const int CYCLE_RANK_LIST = 22702;
        /// <summary>昨日竞榜榜单。发无参;回包
        /// type:h, subtype:h, score:i, rank:h, push_type:c, rank_list[u16×{rank:h, server_id:i, role_id:l, role_name:s, role_score:i}]。</summary>
        public const int CYCLE_RANK_YESTERDAY = 22703;
        /// <summary>竞榜第一名变化服务端主动推送。回包
        /// rank_type:h, rank_subtype:h, server_id:i, role_id:l, role_name:s, role_score:i。</summary>
        public const int CYCLE_RANK_FIRST_CHANGE = 22706;

        // ----- 至尊VIP / SVIP (45120, yu_client SvipMainController) -----
        /// <summary>SVIP 信息。请求无参;回包 open_act_id:c, list[u16×{type:h, content_list[u16×{content_id:c}]}]。
        /// open_act_id>0 显示主界面图标(45120),=0 删除。</summary>
        public const int SVIP_INFO = 45120;

        // ----- 祭典/宝录(194xx,yu_server pt_194.erl / 老端 FestivalController) -----
        /// <summary>宝录基础信息。请求无参;回包 uid:h, act_id:c, type:c, lv:h, exp:i, expired_time:i,
        /// reward_list[u16×{lv:h, status1:c, status2:c}]。uid>0 显示主界面图标(223),=0 删除。</summary>
        public const int FESTIVAL_INFO = 19401;
        public const int CUSTOM_ACTIVITY_FTVINVEST = 33211; // 节日投资(FTVINVEST=62)信息。请求 "hh"(base_type,sub_t
        public const int CUSTOM_ACTIVITY_RED_ENVELOPE_REBATE = 33255; // 红包返利(RED_ENVELOPE_REBATE=117)信息。请求 "hh"(type
        public const int COMPETE_ACT_LIST = 33800; // 竞榜/赛事活动正在开启列表(模块338,驱动图标 338@type@subtype 家族
        public const int COMPETE_ACT_LIST_DUOBAO_DRAW = 33803; // 连服夺宝抽奖结果(type/subtype/times/today_score/error/reward_list)
        public const int MARKET_ICON_INFO = 15121; // 市场跨服开放时间(图标151/151@1切换)。请求无参(read(15121,_)->{ok,
        public const int LIMITLEVELSHOP_LIST = 61200; // 限时等级抢购礼包列表(模块612,驱动图标61201)。请求无参(read(61200,_)->
        public const int ACTIVITYFORESHOW_SNATCH_TIME = 65208; // 领地夺宝时间信息(预告图标 652@31@0 用)。请求无字段(read(65208,_)->{
        public const int SNATCH_TREASURE_ENTRY_INFO = 65201; // 领地夺宝入口全量只读快照；严格空请求，65208 保留给 ActivityForeshow。
        public const int BANQUET_WEDDING_STATE = 17249; // 婚礼状态(→172@2 宾客管理图标)。read(17249,_)->{ok,[]} 裸请求;w(与婚姻172xx同属pt_172号段,归Banquet占用,自动循环轮16婚姻段不重复定义,交叉见下方"婚姻"段头注释)
        public const int BANQUET_CALL = 17256; // 婚礼召集/婚礼列表(→172@1 婚礼图标)。read(17256,_)->{ok,[]} 裸请(与婚姻172xx同属pt_172号段,归Banquet占用,自动循环轮16婚姻段不重复定义,交叉见下方"婚姻"段头注释)

        // ----- 婚宴数据层补全(自动循环 轮24 PB,pt_172 172xx;扩 BANQUET_WEDDING_STATE/BANQUET_CALL 既有壳,不重建) -----
        //        22 个接收活号新增,逐条核对 pp_marriage.erl 原文(17249/56 已在上方；17263/64 的回包归
        //        Marriage 接收，Banquet 仅提供进入/离开场景的发送 API,不重复定义常量)。
        //        NowWeddingState==2 门:17252(open_invite_guest)/17259(buy_max)/17260(open_ask_invite)。
        //        婚礼场景(?WeddingScene)门:17262/17265/17266/17267/17270/17272/17275(Unity 无婚礼场景,
        //        发送方法照建,场景门禁本端不预检,服务端权威拦截,调用留 UI/场景轮)。AskInviteLv=130 门:
        //        17257(pp_marriage.erl:1843)**与 17258(:1860,同一常量,侦察稿未提及,本代理原文核实补全)**。
        //        成功复发链(照 BanquetController.ts 原文):17251(code∈{1,1720034})→17249+17250;
        //        17253(code∈{1,1720033})/17259(code==1)/17261(无条件)→17252;17266(code==1)/
        //        17267(code∈{1,1720071},且配置已载)→17272;
        //        17271(type==1 时)→17272;17276(无条件,S2C-only 推送)→17249+17250;17298(error_code==1)→17252+17260(type=[2])。
        //        无 read 的纯推送号(S2C only,C2S 不可达):17271/17276/17277/17278/17279(pt_172.erl 无对应
        //        read 子句,逐号核对确认)。17273 虽有服务端推送,但老端 handler 为空,已按玩家行为事实源列入 killlist。 -----

        /// <summary>婚宴预约/报名视图数据(C2S 空包)。S2C Code:32,NowWeddingState:8,
        /// MyWeddingTimes[u16计数]{WeddingType:8,UseTimes:16,MaxTimes:16,OrderToday:8},
        /// DayList[u16计数]{OrderUnixDate:32,TimeList[u16计数]{TimeId:8,
        /// OrderList[u16计数]{RoleIdM:64,RoleIdW:64,WeddingType:8,IfOwn:8}}}(三层嵌套)。
        /// 老端特例 code!=1720012 才显码(1720012=err172_couple_single,data_error_code.erl:3034,原样镜像)。</summary>
        public const int BANQUET_APPLY_INFO = 17250;
        /// <summary>预约婚礼(C2S "ccc" day_id,time_id,wedding_type)。S2C Code:32,Time:32,WeddingType:8,
        /// ManList[u16计数]×bin_16,WomanList[u16计数]×bin_17(bin_16/17:RoleId:64,Name:s,Lv:16,
        /// CombatPower:64,Sex:8,Vip:32,Career:8,Turn:8,**无 Picture 字段**——勿与 17256 bin_24/25/26
        /// 混淆)。成功码 1 或 1720034(err172_wedding_order_success,data_error_code.erl:3122,配偶侧回执)
        /// 均触发重发 17249+17250。</summary>
        public const int BANQUET_APPLY_SEND = 17251;
        /// <summary>婚宴邀请视图数据(C2S 空包)。服务端 NowWeddingState==2 才放行(pp_marriage.erl:1755),
        /// 否则回错误壳(字段占位全 0/"")。S2C Code:32,MyRoleId:64,MyName:s,MyPicture:s,MyPictureVer:32,
        /// LoverRoleId:64,LoverName:s,LoverPicture:s,LoverPictureVer:32,WeddingType:8,WeddingTime:32,
        /// IfOrderAgain:8,LessInviteNum:8,GuestNum:8,GuestList[u16计数]{RoleId:64,AnswerType:8,Name:s},
        /// AskInviteList[u16计数]{RoleId:64,Name:s}(**无 AnswerType,与 GuestList 形状不同**)。</summary>
        public const int BANQUET_INVITE_INFO = 17252;
        /// <summary>邀请宾客(C2S "h"+N×"l" count,role_id...;InviteList 变长数组)。服务端校验:不可邀请自己/
        /// 不可邀请配偶/NowWeddingState==2/每个被邀请人 Lv&gt;=130(err172_marriage_ask_lv_limit,
        /// pp_marriage.erl:1788)。S2C Code:32,InviteList[u16计数]{RoleId:64}(**纯 RoleId,无 Name/Type 包装**)。
        /// 成功码 1 或 1720033(err172_wedding_invite_success,data_error_code.erl:3118)均触发重发 17252。</summary>
        public const int BANQUET_INVITE_SEND = 17253;
        /// <summary>索要请柬(C2S "l" role_id_m)。服务端 Lv&gt;=130 门(AskInviteLv,pp_marriage.erl:1843
        /// err172_marriage_ask_lv_limit)。S2C Code:32(无其它字段)。</summary>
        public const int BANQUET_ASK_INVITE = 17257;
        /// <summary>购买请柬/买路进场(C2S "l" role_id_m)。服务端同 17257 Lv&gt;=130 门(pp_marriage.erl:1860,
        /// 本代理原文核实补全,侦察稿未提及)+ ?WeddingGuestMaxNumPrice 消耗校验。S2C Code:32,RoleIdM:64。</summary>
        public const int BANQUET_BUY_INVITE_CARD = 17258;
        /// <summary>购买邀请名额上限(C2S "c" buy_num,buy_num&gt;0)。服务端 NowWeddingState==2 门。
        /// S2C Code:32,LessInviteNum:8,GuestNum:8。成功(code==1)重发 17252;code==1720036
        /// (err172_wedding_buy_max_num_success,data_error_code.erl:3130,已是成功语义但老端仍归入 else 分支)
        /// 时不显码但也不触发重发。</summary>
        public const int BANQUET_BUY_INVITE_MAX = 17259;
        /// <summary>打开索要/邀请列表(C2S "h"+N×"c" count,type...;TypeList 变长数组,老端固定传 [2])。
        /// 服务端 NowWeddingState==2 门。S2C Code:32,LessInviteNum:8,
        /// List[u16计数]{Type:8,InfoList[u16计数]{RoleId:64,AnswerType:8,Name:s}}。**双 type 分流**:
        /// Type==1→AskData(索要请柬列表,老端按"是否比上次更多"判定 172@2 红点是否为"新申请");
        /// Type==2→GuestList(与 17252 的 GuestList 字段共用同一顶层桶)。</summary>
        public const int BANQUET_OPEN_ASK_INVITE = 17260;
        /// <summary>回应索要请柬(C2S "h"+N×"lc" count,(role_id,answer_type)...;AnswerAskList 变长数组)。
        /// 服务端要求 wedding_pid 存活(err172_wedding_not_start)。S2C Code:32(无论成功失败,老端无条件
        /// 重发 17252 刷新邀请视图)。</summary>
        public const int BANQUET_ANSWER_ASK_INVITE = 17261;
        /// <summary>婚礼动画场景信息(C2S 空包)。服务端要求 SceneId==?WeddingScene 且对方 wedding_pid 存活,
        /// 否则回错误壳(err172_wedding_not_start/err172_wedding_not_scene)。S2C Code:32,
        /// ManList[u16计数]{RoleIdM:64,FigureM(<see cref="Shenxiao.Common.Proto.FigureProto"/>,pt:write_figure)},
        /// WomanList[u16计数]{RoleIdW:64,FigureW},GuestPositionList[u16计数]{PosId:8,GuestRoleId:64,IfEnter:8}。</summary>
        public const int BANQUET_SCENE_ANIME_INFO = 17262;
        /// <summary>婚礼信息(C2S 空包)。服务端要求 SceneId==?WeddingScene,否则回错误壳(字段占位全 0)。
        /// S2C Code:32,StageId:8,StageEndTime:32,Aura:32,LessNormalCandies:32,LessSpecialCandies:32,
        /// GuestsNum:8。</summary>
        public const int BANQUET_WEDDING_INFO = 17265;
        /// <summary>撒喜糖(C2S "c" candies_type;1=普通/2=特殊；8002003/8002004 是对应配置物品 ID,
        /// 不能上行)。服务端要求婚礼场景+wedding_pid 存活+RoleIdM∈{自己,配偶}(err172_wedding_not_owner)。
        /// S2C Code:32(成功后老端重发 17272)。</summary>
        public const int BANQUET_SPRINKLE_CANDIES = 17266;
        /// <summary>放烟花(C2S "c" fires_type)。服务端要求婚礼场景+wedding_pid 存活+config_wedding_fires
        /// 命中+可发奖校验。S2C Code:32(老端本地读 config_wedding_fires 取 charact 播场景特效,配置已载时
        /// 无条件重发 17272,配置未载时整段跳过——本端镜像该门禁,详见 BanquetController.On17267 注释)。</summary>
        public const int BANQUET_SET_OFF_FIRES = 17267;
        /// <summary>发弹幕(C2S "si" msg,tk_time;tk_time 老端固定传 0)。内部转发 pp_chat:handle(11001,场景频道)。
        /// S2C Code:32(仅 ?SUCCESS/?FAIL 两态,无其它字段)。</summary>
        public const int BANQUET_SEND_DANMU = 17270;
        /// <summary>吃桌菜/采集喜糖结果推送(**无 read,S2C only**,由场景采集完成触发,非本模块协议号驱动——
        /// 老端经通用 COMPLETE_TO_COLLECT 场景事件本地乐观更新 BanquetModel.list_table_num/BanquetData 计数,
        /// 未走本号请求;本轮无婚礼场景,只镜像接收解析+发事件,场景采集联动留尾包)。S2C Code:32,
        /// ErrorCodeArgs:s,Type:8(1=桌菜"喜宴"/2=普通喜糖/其它=特殊喜糖)。Type==1 时老端额外
        /// SendFmtToGame(17272) 且 toast"获得喜宴"。</summary>
        public const int BANQUET_COLLECT_RESULT = 17271;
        /// <summary>婚礼道具使用信息/桌菜采集状态(C2S 空包)。服务端要求婚礼场景,否则回错误壳。S2C Code:32,
        /// IfMaster:8,FreeCandies:8,FreeFires:8,CollectTableList[u16计数]{TableMonOnlyId:32}
        /// (**纯 u32,无字段包装**)。</summary>
        public const int BANQUET_GOODS_INFO = 17272;
        /// <summary>婚礼获得总经验(C2S 空包)。服务端要求婚礼场景,否则静默 skip(不回包,老端也无错误壳分支)。
        /// S2C AllExp:64(**无 Code 前缀,唯一字段**)。</summary>
        public const int BANQUET_EXP_INFO = 17275;
        /// <summary>婚礼开始推送(**无 read,S2C only**,对标 mod_marriage_wedding_mgr.erl:694 定时扫描
        /// wedding_order_list 到点触发,双向单播新人)。S2C RoleIdM:64,RoleIdW:64(**无 Code 前缀**)。
        /// 老端收到无条件重发 17249+17250(刷新预约/图标状态,字段本身不消费)。</summary>
        public const int BANQUET_WEDDING_START_PUSH = 17276;
        /// <summary>气氛值变化推送(**无 read,S2C only**)。S2C InfoList[u16计数]{Type:8,Values:32}
        /// (**无 Code 前缀**)。老端仅 Type==1 时 Fire(AURA,values),其余 Type 现无分支但仍需读完整个数组
        /// 保游标。</summary>
        public const int BANQUET_AURA_PUSH = 17277;
        /// <summary>气氛值奖励推送(**无 read,S2C only**,达到 config_wedding_aura 阈值时按在场宾客逐个 cast)。
        /// S2C AuraNum:32,Reward:ObjectList(u16计数{Type:8,TypeId:32,Num:32})(**无 Code 前缀**)。
        /// 老端弹 BanquetRewardView 领奖动画,本轮数据层only,发事件供尾包 UI 消费。</summary>
        public const int BANQUET_AURA_REWARD_PUSH = 17278;
        /// <summary>吃桌菜奖励推送(**无 read,S2C only**)。S2C Type:8,Reward:ObjectList(**无 Code 前缀**)。
        /// 老端 Util.ShowCongratulationView(reward,10) 弹祝贺动画,本轮数据层only,发事件供尾包 UI 消费。</summary>
        public const int BANQUET_TABLE_REWARD_PUSH = 17279;
        /// <summary>一键邀请剩余宾客(C2S 空包,对标 lib_marriage:one_invite_role)。S2C ErrorCode:32
        /// (**字段名 ErrorCode 非 Code,语义相同**)。成功(==1)时老端 toast"一键邀请成功！"+重发
        /// 17252+17260(TypeList=[2])。</summary>
        public const int BANQUET_ONE_INVITE = 17298;
        public const int KAIFU_INVEST_OPEN = 42004; // 开服投资活动开启列表(驱动 4205 巅峰投资 / 1112 超值投资图标;裸请求)
        /// <summary>开服投资按类型状态。C2S: type:u8；S2C: type:u8,cur_lv:u16,buy_time:u32,get_time:u32,login_days:u16,rewards:u16×{id:u8,got_lv:u16}。</summary>
        public const int KAIFU_INVEST_INFO = 42001;
        public const int KAIFU_BOOK_INFO = 42401; // 契约之书章节信息(驱动 424 / 424@1 图标;裸请求)
        /// <summary>开服投资(pt_420)家族统一错误出口(轮22 族错误出口批;对标老端 KaifuActivityController.ts:161-164
        /// On42000:无条件 ErrorCodeShow(code,args)。服务端 send_error/2(lib_investment.erl:413-417)是投资
        /// 相关多处失败分支共享的错误壳,回包恒为错误码)。回包(pt_420.erl write(42000,[Code,Args])):code:i, args:s。</summary>
        public const int KAIFU_INVEST_ERROR = 42000;
        public const int DIAMONDFIGHT_INFO = 13700; // 灵玉/勾玉大战活动状态(war_state 驱动图标137);请求裸发 read(13700,_
        /// <summary>灵玉大战"进入准备场景"错误出口(轮22 族错误出口批;对标老端 DiamondFightController.ts:298-303
        /// On13704:code!=1→ErrorCodeShow,无其它副作用)。回包(pt_137.erl write(13704,[Code])):code:i。</summary>
        public const int DIAMONDFIGHT_ENTER_ERROR = 13704;
        public const int KF1VN_STAGE_INFO = 62101; // 诸天王者(跨服1vn)活动阶段。请求无字段裸发;回包 stage:c, turn:h, edti
        /// <summary>诸天王者错误出口(轮22 族错误出口批;对标老端 Kf1vnController.ts:242-245 Handler62103:
        /// 无条件 ErrorCodeShow(error_code),无其它副作用)。回包(pt_621.erl write(62103,[ErrorCode])):code:i。</summary>
        public const int KF1VN_ERROR = 62103;
        /// <summary>诸天王者竞猜/匹配相关错误出口(轮22 族错误出口批;对标老端 Kf1vnController.ts:444-447
        /// Handler62132:无条件 ErrorCodeShow(error_code),忽略 error_args,无其它副作用)。
        /// 回包(pt_621.erl write(62132,[ErrorCode,ErrorArgs])):code:i, args:s。</summary>
        public const int KF1VN_QUIZ_ERROR = 62132;
        public const int SEAHEGEMONY_INFO = 18600; // 四海争霸基础信息(阵营/报名态)。请求无参 read(18600,_)->{ok,[]};回包
        public const int SEAHEGEMONY_SIGNUP = 18625; // 四海争霸报名结束时间。请求无参 read(18625,_)->{ok,[]};回包 end_ti
        /// <summary>四海争霸(舰船)错误出口(轮22 族错误出口批;对标老端 SeaHegemonyController.ts:301-308,
        /// scmd&amp;&amp;code!=1→ErrorCodeShow,无其它副作用)。回包(pp_seacraft.erl:261;pt_186.erl
        /// write(18614,[Code])):code:i。</summary>
        public const int SEACRAFT_ERROR_18614 = 18614;
        /// <summary>四海争霸(舰船职务/分配)错误出口(轮22 族错误出口批;对标老端 SeaHegemonyController.ts:318-325,
        /// scmd&amp;&amp;code!=1→ErrorCodeShow,无其它副作用)。回包(lib_seacraft_mod.erl:1468/1472/1476/1487/1492,
        /// pp_seacraft.erl:280;pt_186.erl write(18616,[Code])):code:i。</summary>
        public const int SEACRAFT_ERROR_18616 = 18616;
        /// <summary>四海争霸日常(pt_187)家族统一错误出口(轮22 族错误出口批;老端也挂在 SeaHegemonyController.ts:590-595,
        /// 与186共用UI控制器,不新建 Controller;无条件 ErrorCodeShow(code),服务端 send_error/2 是多处
        /// do_handle 共享的错误壳,回包恒为错误码——对标老端无 if 守卫直接显码)。回包(pp_seacraft_daily.erl:375
        /// send_error/2;pt_187.erl write(18700,[Code])):code:i。</summary>
        public const int SEACRAFT_DAILY_ERROR = 18700;
        public const int KFHOLYAREA_ACT_STATE = 28410; // 神陨禁区(跨服圣域)活动状态/时间窗——驱动主界面图标284。请求裸发 read(28410,_
        /// <summary>神陨禁区"退出"错误出口(轮22 族错误出口批;对标老端 KfHolyAreaController.ts:272-275,
        /// 无条件 ErrorCodeShow(code),无其它副作用)。回包(pp_c_sanctuary.erl:147-165;pt_284.erl
        /// write(28407,[ErrCode])):code:i(成功/失败均回此号,老端也是无条件显码)。</summary>
        public const int KFHOLYAREA_EXIT_ERROR = 28407;
        /// <summary>神陨禁区(pt_284)家族统一错误出口(轮22 族错误出口批;对标老端 KfHolyAreaController.ts:354-357
        /// "错误返回",无条件 ErrorCodeShow(code)。服务端 send_error/2(pp_c_sanctuary.erl:16-31,等级不足时
        /// 触发)+ lib_sanctuary_cluster_util.erl:162/166 共用此号,回包恒为错误码)。
        /// 回包(pt_284.erl write(28414,[Code])):code:i。</summary>
        public const int KFHOLYAREA_ERROR = 28414;
        /// <summary>神纹/龙纹基础快照。C2S 严格空包；S2C 为属性、部位与战力的全量替换快照。</summary>
        public const int LUNG_INFO = 18100;
        public const int LUNG_STOVE_INFO = 18105; // 神纹熔炉数据(stove_data);回包驱动主界面图标181显隐;请求 read(18105,
        /// <summary>神纹熔炉下一次开启状态。C2S 严格空包；S2C: crucible_id:u16,start_time:u32。</summary>
        public const int LUNG_STOVE_OPEN_STATE = 18112;
        public const int BASEDUNGEON_TOWER_INFO = 61117; // 限时爬塔状态(round/over_time/reward_mode)——驱动限时塔图标 331
        public const int GROWTHBENEFITS_INFO = 41720;      // 成长福利信息/任务态
        public const int GROWTHBENEFITS_TASK_UPDATE = 41721; // 成长福利任务进度推送
        public const int FRIENDINVITE_INFO = 34001;        // 好友邀请/分享信息
        /// <summary>好友邀请升级角色全量快照。C2S 空包；S2C pos_list:u16×{invitee_id:u64,pos:u8,name:s,lv:u16,career:u8,status:u8}。</summary>
        public const int FRIENDINVITE_LEVEL_INFO = 34006;
        /// <summary>好友邀请帮助信息完整快照。C2S 空包；S2C count:u16,reward_list:u16×{reward_id:u8,status:u8},pos_list:u16×升级邀请角色。</summary>
        public const int FRIENDINVITE_HELP_INFO = 34005;
        public const int FRIENDINVITE_BOOST_INFO = 34008;
        public const int FRIENDINVITE_WELFARE_INFO = 34012;
        /// <summary>好友邀请(pt_340)家族统一错误出口(轮22 族错误出口批;对标老端 FriendInviteController.ts:160-163
        /// On34000:无条件 ErrorCodeShow(code,args)。服务端 send_error_code/3(lib_invite.erl:436-441)是
        /// 多处失败分支共享的错误壳,首字段 Pt 标识触发协议号,老端不消费该字段,本端同样只读不透出)。
        /// 回包(pt_340.erl write(34000,[Pt,Code,Args])):pt:h, code:i, args:s。</summary>
        public const int FRIENDINVITE_ERROR = 34000;
        public const int TOPVIP_INFO = 45101;              // 至尊VIP基础信息
        /// <summary>龙珠雕像总览。C2S 空包；S2C: status:u8,power:u64。</summary>
        public const int DRAGONBALL_STATUE_OVERVIEW = 14310;
        /// <summary>龙珠套装概览。C2S 空包；S2C: wear_type:u8,items:u16×{type:u8,lv:u8,power:u64,next_power:u64}。</summary>
        public const int DRAGONBALL_SUIT_INFO = 14303;
        /// <summary>龙珠本体列表/状态刷新。C2S 空包；S2C: items:u16×{dragon_id:u32,dragon_lv:u16,power:u64,next_power:u64}。</summary>
        public const int DRAGONBALL_LIST = 14300;
        /// <summary>龙珠系统总战力。C2S 严格空包；S2C: total_power:u64。</summary>
        public const int DRAGONBALL_TOTAL_POWER = 14306;
        /// <summary>不朽圣骸基础快照。C2S: stage:u8,type:u8；S2C 为阶段/类型/部位全量树。</summary>
        public const int ARMOR_INFO = 14401;
        /// <summary>勋章基础快照。C2S 空包；S2C: id,stren_lv,stren_exp,honour,power,pass_layers。</summary>
        public const int MEDAL_INFO = 13401;
        /// <summary>勋章称号全量状态。C2S 空包；S2C: titles:u16×{id:u32,level:u16,power:u32,is_equip:u8}。</summary>
        public const int MEDAL_TITLE_SNAPSHOT = 13405;
        /// <summary>跨服分组基础快照。C2S 空包；S2C 为服务器与模块分组全量数据。</summary>
        public const int KF_STAGE_INFO = 10200;
        /// <summary>天命觉醒激活列表快照。C2S 空包；S2C: ids:u16×u32。</summary>
        public const int REINCARNATION_AWAKEN_INFO = 16400;
        /// <summary>幻兽总览快照。C2S 空包；S2C 为战斗次数和幻兽嵌套列表。</summary>
        public const int GODBEAST_OVERVIEW = 17301;
        /// <summary>称号列表快照。C2S 空包；S2C: current_id:u32,items:u16×{id:u32,order:u8,end_time:u32}。</summary>
        public const int DESIGNATION_LIST = 41101;
        /// <summary>面具状态快照。C2S 空包；S2C: mask_id:u8,end_time:u32。</summary>
        public const int MASK_INFO = 51101;
        /// <summary>使魔实体核心快照。C2S 空包；S2C 为开放状态和完整实体列表。</summary>
        public const int DEMON_INFO = 18301;
        /// <summary>使魔单体真实战力快照。C2S: demons_id:u32；S2C: demons_id:u32,power:u32。</summary>
        public const int DEMON_POWER = 18302;
        /// <summary>使魔天赋商店完整快照。C2S 空包；S2C: refresh_time:u32,refresh_num:u16,cost:ObjectList,shop:u16×{id:u32,goods_id:u32,price:u32,num:u16,cost_num:u16,discount:u8,can_buy_num:u16,buy_num:u16}。</summary>
        public const int DEMON_TALENT_SHOP = 18311;
        /// <summary>使魔天赋真实战力查询。C2S: demons_id:u32,sign:u8,id:u32,skill_lv:u16；S2C: power:u32,demons_id:u32,sign:u8,skill_id:u32,skill_lv:u16,code:u32。</summary>
        public const int DEMON_TALENT_POWER = 18314;
        /// <summary>使魔羁绊全量快照。C2S 空包；S2C: fetters:u16×fetter_id:u32。</summary>
        public const int DEMON_FETTERS = 18303;
        /// <summary>使魔上卷/绘卷全量快照。C2S 空包；S2C: paintings:u16×painting_id:u8。</summary>
        public const int DEMON_PAINTINGS = 18307;
        /// <summary>使魔转盘祝福值快照。C2S 空包；S2C: bless_value:u32。</summary>
        public const int DEMON_BLESSING = 50901;
        /// <summary>天启主状态快照。C2S 空包；S2C 为标量与三类全量列表。</summary>
        public const int REVELATION_INFO = 28606;
        /// <summary>天启装备战力刷新。C2S 空包；S2C: power:u64，仅在已有28606快照时覆盖。</summary>
        public const int REVELATION_POWER = 28609;
        /// <summary>怪物图鉴已激活 PicId 全量快照。C2S 空包；S2C: pic_list:u16×pic_id:u32。</summary>
        public const int MON_BOOK_ACTIVATED_PICS = 44205;
        /// <summary>怪物图鉴单项首级战力预览。C2S: pic_id:u32；S2C: pic_id:u32,next_power:u64。</summary>
        public const int MON_BOOK_PREVIEW_POWER = 44207;
        /// <summary>怪物图鉴按类型全量快照。C2S: type:u16；S2C: type+分组表+图鉴表+总战力。</summary>
        public const int MON_BOOK_TYPE_INFO = 44201;
        /// <summary>跨服单人排行副本个人状态。C2S 空包；S2C: start_level:u8,reward_state:u8,levels:u16×{u8,u32}。</summary>
        public const int KF_SINGLE_RANK_INFO = 50701;
        /// <summary>跨服单人排行副本指定区域榜单。C2S: area_id:u8；S2C: area_id+完整排行表。</summary>
        public const int KF_SINGLE_RANK_AREA_TOP = 50703;
        /// <summary>跨服单人排行副本指定区域可挑战擂主表。C2S: area_id:u8；S2C: area_id:u8,entries:u16×{level:u8,role_id:u64,role_name:string,server_id:u16,server_num:u16,lv:u16,career:u8,sex:u8,turn:u8,picture:string,picture_ver:u8,go_time:u32}。</summary>
        public const int KF_SINGLE_RANK_AREA_TOWERS = 50702;
        public const int ACHIEVEMENT_STAGE = 40901;
        public const int ACHIEVEMENT_ENTRIES = 40903;
        public const int ACHIEVEMENT_STAR = 40906;
        public const int ACHIEVEMENT_TYPES = 40908;
        public const int GUARD_INFO = 21601;
        public const int NINE_SKY_INFO = 13500;
        /// <summary>九魂圣殿战斗小面板。C2S 空包；S2C: cur_floor:u8,max_floor:u8,left_time:u32,kill_num:u16,score:u32,first_server_num:u16,first_player:string。</summary>
        public const int NINE_SKY_BATTLE_INFO = 13503;
        /// <summary>九魂圣殿秘宝持有者主动快照。S2C: index:u8,server_num:u16,role_id:u64,role_name:string,left_time:u32。</summary>
        public const int NINE_SKY_FLAG_INFO = 13504;
        public const int GHOST_WALK_INFO = 20601;
        /// <summary>时空圣痕跨服世界列表。C2S 空包；S2C: status:u8,servers:u16×{server_num:u32,name:s,level:u16}。</summary>
        public const int TS_CRACK_WORLD_INFO = 20411;
        /// <summary>永恒圣殿活动时间。C2S 空包；S2C: open_time:u32,enter_time:u32,end_time:u32。</summary>
        public const int ETERNITY_TIME_INFO = 27900;
        /// <summary>永恒圣殿参与人数与资格快照。C2S 空包；S2C: can_enter_scene:u8,join_list:u16×{scene:u32,self_server_num:u16,scene_num:u16}。</summary>
        public const int ETERNITY_JOIN_INFO = 27901;
        /// <summary>永恒圣殿怪物信息。C2S: scene:u16；S2C: scene:u16,monsters:u16×{mon_id:u32,mon_lv:u16,mon_type:u8,bl_server:u32,bl_server_name:s,bl_server_num:u32,reborn_time:u32}。</summary>
        public const int ETERNITY_MONSTER_INFO = 27904;
        /// <summary>永恒圣殿怪物伤害排行。C2S: scene:u16,mon_id:u32；S2C: scene:u16,mon_id:u32,hurt_list:u16×{server_id:u32,server_num:u16,server_name:s,player_id:u32,player_name:s,damage:u16}。</summary>
        public const int ETERNITY_DAMAGE_RANK = 27905;
        /// <summary>永恒圣殿怪物复活推送。仅 S2C：mon_id:u32。</summary>
        public const int ETERNITY_MONSTER_REBORN = 27907;
        /// <summary>永恒圣殿 Boss 状态推送。仅 S2C：mon_id:u32,reborn_time:u32,bl_server:u32,bl_server_num:u32,bl_server_name:s。</summary>
        public const int ETERNITY_BOSS_STATE = 27908;
        /// <summary>永恒圣殿进入前置条件拒绝。仅 S2C：code:u32；code==1 不表示本端可确认的进入成功。</summary>
        public const int ETERNITY_ERROR = 27909;
        /// <summary>永恒圣殿复活状态快照。显式 C2S 空包；S2C: die_times:u16,time:u32,die_time:u32,safe_time:u32。</summary>
        public const int ETERNITY_RELIVE_INFO = 27906;
        /// <summary>圣灵战场世界信息。C2S 空包；S2C: mod:u8,status:u8,end_time:u32,servers:u16×{id:u32,num:u32,name:s,level:u32}。</summary>
        public const int HOLY_BATTLE_INFO = 21801;
        /// <summary>圣灵战场等待场景累计经验。C2S 空包；S2C: all_exp:u64（每包为当前累计总值）。</summary>
        public const int HOLY_BATTLE_EXPERIENCE = 21804;
        /// <summary>圣灵战场个人积分与阶段奖励。C2S 空包；S2C: point:u32,rewards:u16×{stage:u16,status:u8}。</summary>
        public const int HOLY_BATTLE_SCORE = 21805;
        /// <summary>圣灵战场战场统计。C2S 空包；S2C: groups:u16×{group_id:u8,tower_num:u8,point:u32,rank:u8,roles:u16×{role_id:u64,rank:u8,server_id:u32,server_num:u32,name:string,point:u32,kill:u16,assists:u16}}。</summary>
        public const int HOLY_BATTLE_RECORD_STATS = 21808;
        /// <summary>圣灵战场阶段状态与绝对截止时间。C2S 空包；S2C: status:u8,end_time:u32。</summary>
        public const int HOLY_BATTLE_PHASE_TIME = 21811;
        /// <summary>圣灵战场个人状态推送。仅 S2C: point:u16,single_rank:u16,group_rank:u8,anger:u8,anger_end:u32,buffs:u16×{attr_id:u16,value:u32}。</summary>
        public const int HOLY_BATTLE_FIGHT_STATE = 21807;
        /// <summary>圣灵战场怪物增量。C2S 空包；S2C: count:u16×{mon_auto:u32,mon_cfg_id:u32,hp:u32,hp_all:u32,group_id:u8}。</summary>
        public const int HOLY_BATTLE_MONSTER_INFO = 21813;
        /// <summary>圣灵战场死亡信息主动推送。S2C: role_name:string,role_id:u64,lv:u16,power:u64,picture_ver:u32,picture:string,anger:u32,server_id:u32,career:u8,turn:u8。</summary>
        public const int HOLY_BATTLE_DEATH_INFO = 21809;
        /// <summary>圣灵战场结算信息主动推送。仅 S2C: res:u8,groups:u16×{group_id:u8,tower_num:u8,point:u32},my_group_id:u8,my_rank:u8。</summary>
        public const int HOLY_BATTLE_RESULT_INFO = 21810;
        /// <summary>周一嘉礼任务状态。C2S 空包；S2C: task_state:u16×{task_id:u16,state:u8}。</summary>
        public const int MONDAYS_AWARD_TASK_STATE = 17904;
        /// <summary>周一嘉礼跨服开奖记录。C2S 空包；S2C: count:u16×{server_id:u32,server_num:u16,role_id:u64,role_name:string,type:u8,pool_id:u16,utime:u32,picture:string,picture_ver:u32,career:u16,turn:u16}。</summary>
        public const int MONDAYS_AWARD_RECORDS = 17905;
        /// <summary>周一嘉礼当前奖池。C2S 空包；S2C: pool_count:u16×{id:u16,rid_count:u16,rid:u16×rid_count}。</summary>
        public const int MONDAYS_AWARD_POOLS = 17908;
        /// <summary>周一嘉礼抽奖窗口与累计次数。C2S 空包；S2C: code:u8,draw_times:u16。</summary>
        public const int MONDAYS_AWARD_DRAW_STATE = 17907;
        /// <summary>午时狂欢累计经验。C2S 空包；S2C: exp:u32（每包为累计绝对值）。</summary>
        public const int NOON_PARTY_TOTAL_EXP = 28503;
        /// <summary>午时狂欢普通/高级宝箱已采集绝对计数。C2S 空包；S2C: low_box:u32,high_box:u32。</summary>
        public const int NOON_PARTY_BOX_COUNTS = 28504;
        /// <summary>午时狂欢 Boss/宝箱怪复活的 Unix 绝对截止时刻。C2S 空包；S2C: time:u32。</summary>
        public const int NOON_PARTY_REBORN_DEADLINE = 28505;
        /// <summary>午时狂欢活动结束的 Unix 绝对截止时刻。C2S 空包；S2C: time:u32。</summary>
        public const int NOON_PARTY_END_DEADLINE = 28506;
        /// <summary>托管选择快照。C2S 空包；S2C: day_coin:u32,onhook_coin:u32,activities:u16×嵌套行为列表。</summary>
        public const int DEPOSIT_ACTIVITY_ONHOOK = 19201;
        /// <summary>托管双币主动推送。S2C: day_coin:u32,onhook_coin:u32；无 C2S。</summary>
        public const int DEPOSIT_COINS_PUSH = 19208;
        /// <summary>托管历史记录全量快照。C2S 空包；S2C: records:u16×{u16,u16,u32,u32,u16,u32}。</summary>
        public const int DEPOSIT_RECORDS = 19206;
        /// <summary>装扮类型快照。C2S: dress_type:u8；S2C 为该类型已启用装扮全量列表。</summary>
        public const int DRESS_INFO = 11200;
        public const int DRAGONBALL_GIFT_INFO = 14311;     // 龙玉礼包信息(图标143)
        public const int SEVENDAY_OPEN_INFO = 17500;       // 七天登录信息
        public const int SEVENDAY_MERGE_INFO = 17502;      // 合服七天信息
        public const int PUSHGIFT_LIST = 19101;            // 礼包推送列表
        /// <summary>礼包推送单礼包详情。C2S: gift_id:u16,sub_id:u16；S2C: 名称、结束时间、条件及档位奖励完整快照。</summary>
        public const int PUSHGIFT_DETAIL = 19102;
        public const int PUSHGIFT_OFFLINE = 19104;         // 礼包推送-离线过期领取
        public const int ADVENTURE_INFO = 42700;           // 天天冒险活动时间窗
        /// <summary>天天冒险主状态。C2S 空包；S2C: circle:u16,location:u16,left_times:u16,throw_times:u16,free_reset_times:u16,free_throw_times:u16。</summary>
        public const int ADVENTURE_BOARD_STATE = 42701;
        /// <summary>天天冒险商店快照。C2S 空包；S2C: times:u32,refresh_cost:ObjectList,goods:u16×{id:u16,type:u8,reward:ObjectList,show_price:u32,price:u32,over:u8,state:u8}。</summary>
        public const int ADVENTURE_SHOP_SNAPSHOT = 42704;
        // 属性药剂(pt_217): 21702 成功不回本号，服务端随后推 21701；21700 是唯一错误出口。
        public const int ATTRIBUTE_POTION_ERROR = 21700;
        public const int ATTRIBUTE_POTION_LEVEL_COUNT = 21701;
        public const int ATTRIBUTE_POTION_USE = 21702;
        public const int ATTRIBUTE_POTION_ALL_COUNT = 21703;

        // ----- 周卡(452xx,yu_server pt_452.erl) -----
        /// <summary>周卡信息。请求无参;回包 Lv:h, Exp:i, IsActivity:c, GiftBagNum:h, CanReceiveGift:h, ExpiredTime:i。</summary>
        public const int WEEK_CARD_INFO = 45201;
        /// <summary>领取周卡奖励。请求无参;回包 Code:i + 奖励[h+{Style:c,TypeId:i,Count:i}×N]。</summary>
        public const int WEEK_CARD_CLAIM = 45202;
        /// <summary>周卡奖励推送(S2C)。Type:c + 奖励[…]。</summary>
        public const int WEEK_CARD_REWARD = 45203;

        // ----- 物品/背包(150xx,yu_client h5/src/commonController/GoodsController.ts + commonModel/BagModel.ts) -----
        /// <summary>物品容器全量(满背包/装备/仓库…)。发 "h"(pos;背包 pos=4=GoodsModel.GOODS_POS_TYPE.bag,见
        /// GoodsController.ts GAME_START 批量 SendFmtToGame(15010,"h",pos));回包(ClientProtocol.json "15010"):
        /// pos:h, cell_num:h, max_cell:h, cell_gold:c,
        /// goods_list[u16 × {goods_id:l, type_id:i, sub_pos:c, cell:h, goods_num:i, bind:c, trade:c, sell:c, is_drop:c,
        /// color:c, expire_time:i, combat_power:i, stren:h, level:h, rating:i, overall_rating:i,
        /// addition_attrlist[u16×{attr_type:c,attr_value:i,color:c,combat_power:i}],
        /// equip_extra_attr[u16×{color:c,type_id:c,attr_id:h,attr_val:i,plus_interval:c,plus_unit:i}],
        /// equipStage:c, equipStar:c, skill_id:i, skill_lv:c, awake_list[u16×{attr_type:h,awake_lv:i,awake_exp:i}]}]。
        /// 显示只取 type_id/goods_num/color/cell,但每项须按序读完(含 3 嵌套数组)否则错位。每个回包对应一个 pos。</summary>
        public const int GOODS_CONTAINER_INFO = 15010;

        /// <summary>特殊积分单条变动(对标 GoodsController.On15008 → GoodsModel.UpdateSpecialScore)。
        /// 回包:currency_id:i, num:i。主货币金/铜走 13xxx,不在此。</summary>
        public const int SPECIAL_SCORE_UPDATE = 15008;

        /// <summary>特殊积分全量(对标 On15009 → CreateSpecialScoreList 清空重建)。
        /// 回包:currency_list[u16 × {currency_id:i, num:i}]。</summary>
        public const int SPECIAL_SCORE_LIST = 15009;

        /// <summary>物品容器增量·全字段(对标 GoodsController.On15017:pos==bag → 逐项 UpdateBagGoods
        /// num&lt;=0 删/有则替换/新增;pos==equip → UpdateEquipGoods 未移植)。
        /// 回包:pos:h + goods_list[u16 × 同 15010 单项 schema(含 3 嵌套数组)]。</summary>
        public const int GOODS_LIST_UPDATE = 15017;

        /// <summary>物品容器增量·数量(使用/出售等数量变化;对标 On15018,另有 TRY_SHOW_ITEM_USE_VIEW
        /// 获得展示 flow 未移植)。回包:pos:h + goods_list[u16 × {goods_id:l, goods_num:i, type_id:i}]。</summary>
        public const int GOODS_NUM_UPDATE = 15018;

        /// <summary>出售物品(对标 GoodsController.OnSellGoodsHandler:WriteBegin(15021) + h count + 逐项 l goods_id/i num)。
        /// 回包(ClientProtocol.json "15021"):res:i + type_id_list[u16 × {type_id:i, num:i}];res==1「出售成功」,
        /// 否则老端走 Util.ErrorCodeShow 错误码表(未移植 → 显码降级)。数量变化随 15018 推送。</summary>
        public const int SELL_GOODS = 15021;

        /// <summary>使用背包物品(GoodsController.ts UseHandler:USE_BAG_GOODS → SendFmtToGame(15050,"li",goods_id,number))。
        /// 发 "li"(goods_id:l, num:i);回包(ClientProtocol.json "15050"):res:i, args:s, goods_id:l, goods_type_id:i,
        /// goods_num:i, hp:i, num:i, show_goods[u16 × {gid:l, type:c, goodid:i, gnum:i}]。
        /// res==1 使用成功(type==35 冷却物不弹「使用成功」);show_goods=礼包开出物品(经 GetMappingTypeId 还原展示)。</summary>
        public const int USE_GOODS = 15050;

        // ----- Goods 协议扩容(自动循环 轮1;老端 GoodsController.ts + commonModel/GoodsModel.ts,
        //        字段顺序以 ClientProtocol.json "15000"…"15090" 为准) -----
        /// <summary>物品详情(对标 GoodsController.On15000 → goodsModel.AddDynamic)。发 "l"(goods_id);
        /// 回包(ClientProtocol.json "15000")全量装备实例字段:goods_id:l, type_id:i, sub_pos:c, cell:h, num:i,
        /// bind:c, trade:c, sell:c, color:c, expire_time:i, combat_power:i, equip_type:c, price_type:c, sell_price:i,
        /// stren:h, stren_exp:i, rating:i, overall_rating:i, division:c, wash_rating:i,
        /// addition_attrlist[u16×{attr_type:c,attr_value:i,color:c,combat_power:i}],
        /// stone_list[u16×{pos:c,type_id:i}], magic_list[u16×{goods_id:i,end_time:i}],
        /// equip_extra_attr[u16×{color:c,type_id:c,attr_id:h,attr_val:i,plus_interval:c,plus_unit:i}],
        /// wash_attr[u16×{index:c,color:c,attr_id:h,attr_val:i}], suit_list[u16×{suit_lv:c,suit_slv:c,suit_count:c}],
        /// cspirit_stage:h, cspirit_lv:h, awakening_lv:c, equip_skill_id:i, equip_skill_lv:c, mount_equip_skill_id:i,
        /// mount_equip_skill_lv:c, pet_equip_stage:h, pet_equip_star:h, level:h,
        /// awake_list[u16×{attr_type:h,awake_lv:i,awake_exp:i}], refinement_lv:h。
        /// 落 <see cref="Bag.GoodsDynamicModel"/> 缓存(3 秒同 goods_id 节流 + 一次性回调),Emit EVT_GOODS_DETAIL_UPDATE。</summary>
        public const int GOODS_DETAIL = 15000;

        /// <summary>查看他人物品详情(对标 On15001)。发 "ll"(player_id, goods_id);回包同 <see cref="GOODS_DETAIL"/>
        /// 但首字段多 player_id:l,且**没有 stren_exp/wash_rating**(逐字段核对 ClientProtocol.json "15001")。
        /// type_id==0 → toast 错误码 1500001;player_id 不等于自己才落缓存(防串数据,对标老端 If vo.player_id != mainRoleId)。</summary>
        public const int GOODS_DETAIL_OTHERS = 15001;

        /// <summary>玩家开启背包/仓库格子(对标 On15002 → CHANGE_BAG_MAX_CELL)。发 "hh"(pos, 要开的格数);
        /// 回包(ClientProtocol.json "15002"):code:i, pos:h, cell_num:h(开启后**总**格数,字段名虽是 cell_num 但语义=总容量)。
        /// code==1 → toast「扩容成功」+ <see cref="Bag.BagModel.SetMaxCell"/> + Emit EVT_BAG_MAX_CELL(pos,total)。</summary>
        public const int BAG_EXPAND = 15002;

        /// <summary>物品转移格子位置(对标 On15003)。发 "lhh"(goods_id, from_pos, to_pos);回包 code:i,
        /// code!=1 显错误码;成功不本地改状态,等 15017 增量推送(对标老端 On15003 只在失败时 ErrorCodeShow)。</summary>
        public const int GOODS_MOVE_POS = 15003;

        /// <summary>物品分解(对标 On15019 + ResolveGoods 动态发包:WriteBegin(15019)+h 计数+逐项 l goods_id/i num,
        /// 无固定 sendFmt)。回包(ClientProtocol.json "15019"):code:i, reward_list[u16×{goods_id:l,goods_num:i}]。
        /// code==1 → toast「分解成功」+ Emit EVT_GOODS_DECOMPOSE_SUCCESS(reward_list);reward_list 只作展示,
        /// **不写入 BagModel**(数量变化仍走 15017/15018)。</summary>
        public const int GOODS_DECOMPOSE = 15019;

        /// <summary>物品兑换(购买/兑换/合成共用同号,对标 On15022)。发 "li"(id, num;服务端 guard num&gt;0)。
        /// 回包(ClientProtocol.json "15022"):errcode:i, id:l, type:c。errcode==1 时按 type 分文案:
        /// 2/3/4→「购买成功」且自动补发 <see cref="GOODS_EXCHANGE_LIST"/>(type) 刷新列表;5/7→「兑换成功」;6→「合成成功」;
        /// 随后 Emit EVT_GOODS_EXCHANGE_DONE(id);errcode!=1 显错误码。</summary>
        public const int GOODS_EXCHANGE = 15022;

        /// <summary>物品兑换列表(对标 On15026)。发 "h"(exchange_type);回包(ClientProtocol.json "15026"):
        /// type:h, exchange_list[u16×{id:i,count:h,can_exchange:c}]。按 id 升序排序后按 type 分桶存
        /// <see cref="Bag.GoodsExchangeModel"/> + Emit EVT_GOODS_EXCHANGE_LIST(type)。跨系统共享通道
        /// (伙伴商店 type=7/龙语/跨服1v1 等均走它),通用存取不绑定具体玩法。</summary>
        public const int GOODS_EXCHANGE_LIST = 15026;

        /// <summary>过期物品查看/回收(对标 On15027 → GoodsExpiredView)。发 "c"(opr:1查/2回收)。
        /// 回包(ClientProtocol.json "15027"):opr:c, goods_list[u16×{goods_id:l,type_id:i,goods_num:h}]。
        /// opr==1 → 存 <see cref="Bag.GoodsExpiredModel"/> + Emit EVT_GOODS_EXPIRED_LIST + 简易确认弹窗
        /// (对标 GoodsExpiredView.close_time=15,每秒-1,&lt;0 自动确认,共 16 次 tick ≈16 秒;仅 UI 实际弹出时计时);
        /// 确认/超时 → 发 opr=2;opr==2 回执老端不处理,仅 log。GAME_START 后延时 2.5 秒自动查看一次(对标老端
        /// setTimeout(delay_fun,2.5) 尾部 SendFmtToGame(15027,"c",1))。</summary>
        public const int GOODS_EXPIRED = 15027;

        /// <summary>背包已满改邮件发放通知(S2C 主动推送,禁止客户端发送;对标老端 BagController.ts:147-167
        /// On15029)。回包(yu_server pt_150.erl write(15029):799-807,全仓库唯一发送点
        /// lib_goods_api.erl:2111 `send_mail_when_no_cell`,state 恒为1):state:c, location:h(掉落物所属
        /// bag_location,老端据此弹二次确认框跳转对应背包/星装页签)。物品本体已经落进系统邮件,此包只是
        /// "背包满了改邮件发,要不要去清一下"的提醒;Unity 暂无星装(232星座装备)模块与"打开指定背包位置"
        /// 事件通道,降级为纯 toast 提示,不复刻老端按 location 跳转 OpenFun(105)/(170) 的二次确认框,TODO。</summary>
        public const int BAG_FULL_MAIL_NOTICE = 15029;

        /// <summary>服务端通知客户端重新拉取物品背包数据(对标 On15030,老端空桩 //OnGameStart())。禁止客户端发送,
        /// 空包(无字段);收到后重新走一次 <see cref="GOODS_CONTAINER_INFO"/>(pos=bag)流程。</summary>
        public const int GOODS_RELOAD_NOTICE = 15030;

        /// <summary>拾取场景掉落包(对标 On15053;老端注释:无拾取时间的发一次,有拾取时间的发两次)。发 "l"(drop_id);
        /// 回包(ClientProtocol.json "15053"):res:i, args:s, status:c, drop_id:l。三态状态机(判断顺序照老端):
        /// res==1→拾取成功;否则 status==1→进入拾取计时;否则 res==1500020→掉落包已消失;否则→失败(toast 错误码,带 args)。</summary>
        public const int DROP_PICK = 15053;

        /// <summary>获取物品 buff 列表(对标 On15055,无参请求)。回包(ClientProtocol.json "15055"):player_id:l,
        /// buff_list[u16×{goods_id:i,buff_type:c,effect_list:s,time:i,single_time:i}]。仅 player_id==自己才落
        /// <see cref="Bag.GoodsBuffModel"/>(对标老端 If scmd.player_id==mainRoleId);无条件 Emit EVT_GOODS_BUFF_UPDATE。</summary>
        public const int GOODS_BUFF_LIST = 15055;

        /// <summary>礼包等级信息(对标 On15083 + GetGiftBagDynamic 单槽回调)。发 "li"(goods_id, type_id);
        /// 回包(ClientProtocol.json "15083"):goods_id:l, type_id:i, gift_level:h。广播 Emit EVT_GIFT_LEVEL_INFO(vo)
        /// + 一次性回调(<see cref="Bag.GoodsDynamicModel.RequestGiftLevel"/> 发起时注册,回包触发后清空)。</summary>
        public const int GIFT_LEVEL_INFO = 15083;

        /// <summary>次数礼包使用次数/冷却(对标 On15084)。发 "l"(goods_id);回包(ClientProtocol.json "15084"):
        /// goods_id:l, use_count:c, total_count:c, freeze_endtime:i。⚠老端此链路已断(GoodsModel.setGoodCoolingData
        /// 函数体整段注释),本轮补齐收发 + <see cref="Bag.GoodsCoolingModel"/> 缓存 + Emit EVT_GOODS_COOLING_UPDATE(goods_id);
        /// 触发预取(红点系统)暂不做。</summary>
        public const int GOODS_COOLING_INFO = 15084;

        /// <summary>领取自选礼包物品内容(对标 On15086 + optional_gift 动态发包)。发 "l"+"h"+n×("c"+"i")
        /// (gift_id, 选项数, {slot序号:u8, num:u32}…;**slot 序号是 1 字节 c,不是 h/i**)。
        /// 回包(ClientProtocol.json "15086"):code:i。code==1→toast「兑换成功」,否则显错误码。
        /// UI(SelectGiftView)未接线,本轮只留 <see cref="Bag.BagController.SendOptionalGift"/> 发送封装。</summary>
        public const int GIFT_OPTIONAL_RECEIVE = 15086;

        /// <summary>领取礼包卡奖励(对标 On15087)。发 "s"(card_no)。回包(ClientProtocol.json "15087"):
        /// res:i, reward_list:ObjectList(u16×{style:c,typeId:i,count:i},解析先例见 41701/RushGiftController)。
        /// 服务端有 5 秒中央 CD,结果可能异步再推一次本号。reward_list 非空 → 视为成功,经 GetMappingTypeId
        /// 逐项还原「获得X」toast + Emit EVT_GIFT_CARD_RESULT(true,list);为空 → 失败,查错误码 toast(res) +
        /// Emit EVT_GIFT_CARD_RESULT(false,null)。</summary>
        public const int GIFT_CARD_RECEIVE = 15087;

        /// <summary>拾取掉落包顺序列表(对标 On15088 → Scene.Instance.SetDropIndexList,S2C 推送)。
        /// 禁止客户端发送;回包 drop_id_list[u16×{drop_id:i}]。存 <see cref="Bag.DropOrderModel"/> +
        /// Emit EVT_DROP_ORDER_LIST;场景层消费方待补。</summary>
        public const int DROP_ORDER_LIST = 15088;

        /// <summary>查看物品预览战力(对标 On15089,幻化 tooltip 用)。发 "i"(goods_type_id,**4 字节类型 id,
        /// 非物品实例 id**)。回包(ClientProtocol.json "15089"):goods_type_id:i, expect_power:i。
        /// Emit EVT_GOODS_EXPECT_POWER(typeId,power);消费方(幻化 tooltip)待补。</summary>
        public const int GOODS_EXPECT_POWER = 15089;

        /// <summary>物品自动分解提示(对标 On15090,S2C 推送)。禁止客户端发送;回包(ClientProtocol.json "15090"):
        /// code:i, reward_list[u16×{goods_id:l,goods_num:i}], goods_bag_type:c(11=符文/15=源力/43=龙语),
        /// under_color:c(某颜色以下自动分解,2=蓝色/3=紫色/0=无颜色限制)。code==1 → 按 bag_type/under_color 组合
        /// toast(文案逐字对标老端 GoodsController.ts:1000-1024)+ Emit EVT_GOODS_DECOMPOSE_SUCCESS(复用
        /// <see cref="GOODS_DECOMPOSE"/> 同一事件,对标老端两号共用 GOODS_DECOMPOSE_SUCCESS);否则显错误码。
        /// reward_list 同样不写 BagModel。</summary>
        public const int GOODS_AUTO_DECOMPOSE_NOTICE = 15090;

        // 以下号跳过(仅存说明,不写代码;主控三路侦察定案):
        // 15004/15005/15006:服务端 pt_150 对应 handle 子句整段注释掉,死号(服务端不会下发,客户端也无对应发送口)。
        // 15023(更改物品子位置/神装武器放入保护箱):服务端 check_good_change_sub_pos 已硬编码 {fail,?FAIL} 永远失败,
        //   老端 On15023 回包处理体也是空函数,双端皆废,不移植。
        // 15085(礼包每天使用次数):老端 h5/src 全仓库零引用(UNUSED,无 SendFmtToGame(15085,...) 调用点),
        //   且服务端该号缺 count 条件分支时静默不回包,不移植。

        // ----- 套装收集(pt_152 段内 15256-15259,yu_server goods/suit_collect;老端 SuitActivityController.ts) -----
        /// <summary>套装收集全量(请求无参,老端 GAME_START 发;回包:clt_list[u16×{suit_id:c, cur_stage:c,
        /// cur_pos_list[u16×{equip_type:c}]}] + suit_id:c 当前时装)。主线 100391=套装1 cur_stage≥4。</summary>
        public const int SUIT_CLT_INFO = 15256;
        /// <summary>激活套装阶段(发 "cc" suit_id,stage;回包:code:i, suit_id:c, cur_stage:c,
        /// cur_pos_list[u16×{equip_type:c}];code==1 成功——服务端需背包有对应部位装备)。</summary>
        public const int SUIT_CLT_ACTIVE = 15257;
        /// <summary>穿装自动点亮广播(S2C only,客户端禁止发送空包)。回包:
        /// list[u16×{suit_id:c,equip_type:c}]。</summary>
        public const int SUIT_CLT_AUTO_LIGHT = 15258;
        /// <summary>套装时装穿脱。发 "cc" suit_id,is_wear(0/1);回包:code:i,suit_id:c,is_wear:c。</summary>
        public const int SUIT_CLT_FASHION_WEAR = 15259;

        // ----- 冲级豪礼/等级礼包(pt_417,yu_server welfare/pp_welfare;老端 WelfareController.ts) -----
        /// <summary>礼包状态列表(请求无参;回包:giftbag_state[u16×{lv:h, received:c, end_time:l, remain_num:i}];
        /// received 0未达/1可领/2已领/4被领完)。主线 100420=领取 lv35。</summary>
        public const int RUSH_GIFT_STATE = 41700;
        /// <summary>领取礼包(发 "h" lv;回包:lv:h, code:i, rewards[u16×{type:c, type_id:i, num:i}](ObjectList);
        /// code==1 成功后老端再发 41700 刷新)。</summary>
        public const int RUSH_GIFT_RECEIVE = 41701;

        // ----- 幻化外观 OutWard(pt_160,yu_server mount/;老端 OutWardController.ts。type_id:1=坐骑 2=剑魄同修 3/4/5=翼影/圣器/神兵) -----
        /// <summary>外观对象信息·系统A阶星(发 "c" type_id;回包:type_id:c, stage:c, star:h, blessing:i,
        /// figure_stage:c, combat:i, etime:l, auto_buy:c, attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×{skill_id:i}])。</summary>
        public const int OUTWARD_INFO = 16002;
        /// <summary>坐骑/同修专用一键升星·系统A(发 "ccc" type_id,auto_buy,gold_type;回包:errcode:i, type_id:c,
        /// stage:c, star:h, blessing:i, blessing_plus:i, etime:l, auto_buy:c, ratio_list[u16×{rate:c,rate_num:h}])。
        /// 主线 100330=坐骑(type_id=1)1阶2星(ctype23:id=阶/need=星,Stage&gt;Id 或同阶 Star&gt;=Need 即完成)。</summary>
        public const int OUTWARD_STAR_UP = 16023;
        /// <summary>外观等级线·系统B面板(发 "c" type_id;回包:type_id:c, level:h, cur_exp:i, combat:i,
        /// attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×{skill_id:i,skill_level:c}])。</summary>
        public const int OUTWARD_LV_PANEL = 16028;
        /// <summary>外观等级线·系统B升级(发 "c" type_id;回包:errcode:i, type_id:c, level:h, cur_exp:i,
        /// add_exp:i, combat:i, skill_list[u16×{skill_id:i,skill_level:c}], ratio_list[u16×{rate:c,rate_num:h}])。
        /// 主线 100521=同修(type_id=2)到2级、100901=坐骑(type_id=1)到2级(ctype90:id=type_id/need=等级)。</summary>
        public const int OUTWARD_LV_UP = 16029;

        // ----- 幻化 OutWard Illusion 全链补齐(pt_160,轮24 PI;老端 OutWardController.ts:71-355)-----
        // 服务端总闸复述(pp_mount.erl:26-45 handle/3):TypeId 必须 ∈ ?APPERENCE(=data_mount:get_constant_cfg(20),
        // 现网值 [1,2,3,4,5,12],与 OutWardController.AllTypeIds 完全一致)且角色等级达到该 type 的开放等级,
        // 否则整包 skip 零回包——TypeId∈{6精灵Sprite,7飞骑Pet,8法阵MagicArr} 在协议层不可达,老端 UI 遗留的
        // 对应分支(ShowIlluRed/GetRedType/CanIllusionUP 等)是死代码,本轮严禁移植,发送侧沿用既有 AllTypeIds。
        /// <summary>族错误出口(无请求,S2C only;回包 errcode:i)。errcode==1600023 时老端特判"激活数量已达上限"
        /// (Fire PET_ACTIVE_LIMIT),其余显码降级。对标 OutWardController.ts:71-78 On16000,
        /// pt_160.erl write(16000,[Errcode])(唯一字段)。</summary>
        public const int OUTWARD_ERROR = 16000;
        /// <summary>场景外观变化广播(S2C only,read/2 未定义;回包 type_id:c, role_id:l, is_ride:c, figure_id:i,
        /// speed:h)。触发方:场景内任意角色幻化(16003)/骑乘(16004)变化时 lib_mount:change_ride_status/
        /// broadcast_to_scene_1 用 send_to_area_scene 广播给场景内所有人(非仅操作者自己)。对标
        /// OutWardController.ts:80-91 On16001(role_vo.SetFigureId+SetFigureRideState+改速度)——Unity 场景
        /// 暂无角色外观渲染消费方,本轮只落数据 + Emit 事件留 TODO 消费方。</summary>
        public const int OUTWARD_SCENE_FIGURE_CHANGE = 16001;
        /// <summary>幻化穿戴/取消(发 "ccii" type_id,type[1=基础/2=幻化],args,color;回包 errcode:i, type_id:c,
        /// type:c, args:i, color:i——type==2 时 args 回显穿戴的 figure_id,type==1 时 args 回显 figure_stage,
        /// 两种语义共用同一字段,对标 OutWardBaseModel.UpdateOutWardFigure 的 type 分支)。对标
        /// OutWardController.ts:98-106 On16003 / :431-434 发送(ILLUSION_OUTWARD)。</summary>
        public const int OUTWARD_ILLUSION_WEAR = 16003;
        /// <summary>上/下坐骑(发 "cc" type_id,type[0=下/1=上];回包 errcode:i, type_id:c, type:c)。对标
        /// OutWardController.ts:108-117 On16004(仅 errcode==1 且 type_id==Horse 触发骑乘动画)/
        /// :726-731 发送(CHANGE_HORSE_STATE)。老端未记录"是否骑乘中"到 Model,本轮同样不落该状态,
        /// 只 Emit 事件供未来场景动画消费。</summary>
        public const int OUTWARD_RIDE_TOGGLE = 16004;
        /// <summary>幻化形象列表(发 "c" type_id;回包 errcode:i, type_id:c, illusion_id:i(当前穿戴的 figure id,
        /// 0=未穿戴/仅基础形象), color_id:h, figure_list[u16×{id:i, stage:c, star:h, end_time:i}])。对标
        /// OutWardController.ts:128-140 On16006,pp_mount.erl:152-165 do_handle(16006)。</summary>
        public const int OUTWARD_ILLUSION_LIST = 16006;
        /// <summary>幻化形象详情(发 "ci" type_id,id;回包 errcode:i, type_id:c, id:i, stage:c, star:h,
        /// blessing:i, combat:i, star_combat:i, end_time:i, attr_list[u16×{attr_id:c,attr_val:i}],
        /// skill_list[u16×skill_id:i](⚠仅 id,无 level,区别于 16002/16028 的 skill_list),
        /// color_list[u16×{color_id:h,color_lv:i}], next_star_power:l)。该 id 未激活时服务端直接
        /// skip 不回包(pp_mount.erl:187-188 "未激活不处理",非 bug)。对标 On16007:142-149。</summary>
        public const int OUTWARD_FIGURE_DETAIL = 16007;
        /// <summary>激活形象(发 "ci" type_id,id;回包 errcode:i, type_id:c, id:i, combat:i)。服务端实测
        /// 4 条失败分支全部改走 16000 通用错误出口(lib_mount.erl:1676/1680/1684/1687),16008 本身只在
        /// 成功时出现,但老端仍防御式判 errcode,照抄。对标 On16008:151-176(成功后无条件补拉 16006)。</summary>
        public const int OUTWARD_FIGURE_ACTIVATE = 16008;
        /// <summary>幻化升阶(发 "cii" type_id,id,goods_id;回包 errcode:i, type_id:c, id:i, stage:c,
        /// blessing:i, blessing_plus:i, ratio_list[u16×{rate:c,rate_num:h}], goods_id:i)。服务端
        /// type_id∈{Horse,Partner} 走 figure_upgrade_stage_sp,其余走 figure_upgrade_stage
        /// (pp_mount.erl:197-203);失败同样改走 16000。对标 On16009:178-194(成功后无条件补拉 16006)。</summary>
        public const int OUTWARD_FIGURE_STAGE_UP = 16009;
        /// <summary>使用魔晶(发 "ci" type_id,goods_id;回包 errcode:i, type_id:c, goods_id:i)。对标
        /// On16010:196-206(成功后补拉 16011+16002)。</summary>
        public const int OUTWARD_CRYSTAL_USE = 16010;
        /// <summary>魔晶使用次数(发 "c" type_id;回包 type_id:c, counter_list[u16×{goods_id:i,times:i,times_lim:i}])。
        /// 对标 On16011:208-211。</summary>
        public const int OUTWARD_CRYSTAL_COUNTER = 16011;
        /// <summary>幻化到期删除推送(S2C only,read/2 未定义;⚠write 用 Id:8,与 16007/16008 的 Id:32 不同,
        /// 不可复用同一读函数;回包 type_id:c, id:c)。触发:lib_mount:clear_figure/3 由
        /// check_figure_time 定时器在幻化真到期时真删库 + 真推包。对标 On16012:213-223
        /// (删本地缓存 + Fire CANCEL_ACTIVE + 补拉 16006)。</summary>
        public const int OUTWARD_FIGURE_EXPIRED = 16012;
        /// <summary>幻化升星(发 "ci" type_id,id;回包 errcode:i, type_id:c, id:i, star:h)。失败同样改走
        /// 16000。对标 On16020:230-250(成功后原地патch 缓存 star 字段 + 补拉 16006 + 补拉 16007)。</summary>
        public const int OUTWARD_FIGURE_STAR_UP = 16020;
        /// <summary>幻化战力预览(发 "cc" type_id,id;⚠id 与请求方均是 8 位[c],非 16007/16008 的 32 位[i];
        /// 回包(无 errcode 包装)type_id:c, id:c, power:l, star_combat:l, next_star_power:l)。老端仅在该
        /// figure 尚无 16007 详情缓存时才发起本请求,已缓存直接读缓存的 combat/star_combat/next_star_power——
        /// "选中未缓存才请求"。对标 On16022:260-263 Fire REAL_FIGHT / IllusionBaseView.SetFightValue:1273-1295。</summary>
        public const int OUTWARD_FIGHT_PREVIEW = 16022;
        /// <summary>坐骑/同修一键升星自动购买开关(发 "cc" type_id,auto_buy;回包 errcode:i, type_id:c, auto_buy:c)。
        /// 服务端 guard type_id∈{Horse,Partner}(pp_mount.erl:960-961);服务端实测恒发 ?SUCCESS
        /// (lib_mount.erl:1341),老端 On16024 也确实不判 errcode 直接套值——本号照抄不加 errcode 判断。
        /// 对标 On16024:277-284。</summary>
        public const int OUTWARD_AUTO_BUY = 16024;
        /// <summary>幻化升星战力预览(发 "cc" type_id,id[⚠均 8 位];回包(无 errcode)type_id:c, id:c, power:l,
        /// next_star_power:l)。老端仅在该 figure 的 star_combat 未缓存(取自 16007 缓存)时才发起本请求
        /// (OutwardStarView.SelectItem:311-329)。对标 On16027:286-289 Fire UPDATE_STAR_FIGHT。</summary>
        public const int OUTWARD_STAR_FIGHT_PREVIEW = 16027;

        // ----- 坐骑/伙伴装备 PetEquip(pt_160,自动循环轮25A;老端 PetEquipController.ts) -----
        /// <summary>装备面板全量(发 "c" type_id,仅 1=坐骑/2=伙伴；回包 type_id:c,errcode:i,
        /// combat_power:i,pet_equip_list[u16×{pos_id:c,pos_lv:i,stage:i,star:h,pos_point:i,
        /// goods_id:l,goods_type_id:i}])。仅 errcode==1 可替换模型。</summary>
        public const int PET_EQUIP_INFO = 16014;
        /// <summary>穿戴/替换装备(发 "ccl" type_id,pos_id,goods_id；回包 type_id:c,code:i,pos_id:c,
        /// new_goods_id:l,old_goods_id:l,new_goods_type_id:i,combat_power:i)。成功后重拉 16014；货品容器
        /// 增删由同次服务端操作另发 15017/15018,不可在本号伪造。</summary>
        public const int PET_EQUIP_WEAR = 16015;
        /// <summary>装备强化(自定义动态帧: type_id:c,goods_id:l,cost_list[h×cost_goods_id:l]；回包
        /// type_id:c,code:i,exp:i,level:h,goods_id:l,combat_power:i)。仅 code==1 且 goods_id 命中时更新。</summary>
        public const int PET_EQUIP_STRENGTHEN = 16016;
        /// <summary>装备打磨/升星进阶(发 "cll" type_id,goods_id,cost_goods_id；回包 type_id:c,code:i,
        /// stage:h,star:h,goods_id:l,cost_goods_id:l,combat_power:i,exp:i,level:h)。成功时同步模型和已穿戴
        /// 货品的阶段/星级/评分。</summary>
        public const int PET_EQUIP_POLISH = 16017;

        // ----- 宝宝 Baby(pt_182，当前已接 23 个老端数据/操作协议；仅 18202/18212 保持死号) -----
        /// <summary>宝宝通用错误：cmd:h,error_code:I,args:s。</summary>
        public const int BABY_ERROR = 18200;
        /// <summary>宝宝基础信息：active_time:I,baby_id:I,baby_name:s,is_change_name:c；请求体为空。</summary>
        public const int BABY_BASIC_INFO = 18201;
        /// <summary>宝宝抚养信息：raise_lv:h,raise_exp:I,task_list[h×{task_id:h,finish_num:h,finish_state:c}],power:I；请求体为空。</summary>
        public const int BABY_RAISE_INFO = 18203;
        /// <summary>宝宝阶段信息：stage:h,stage_lv:c,stage_exp:I,power:I；请求体为空。</summary>
        public const int BABY_STAGE_INFO = 18204;
        /// <summary>宝宝装备：equip_list[h×{pos_id:c,id:l,goods_type_id:I,estage:h,estage_lv:h,estage_exp:I,skill_id:I}],power:I；请求体为空。</summary>
        public const int BABY_EQUIP_INFO = 18205;
        /// <summary>宝宝形象：active_list[h×{baby_id:I,baby_star:h}]；请求体为空。</summary>
        public const int BABY_FIGURE_INFO = 18206;
        /// <summary>宝宝家族信息（含 attr_info/attr_list 双层数组）；请求体为空。</summary>
        public const int BABY_FAMILY_INFO = 18207;
        /// <summary>宝宝点赞榜：请求体为空；回包 role_id:l,praise_list[h×{role_id:l,name:s,baby_power:I,praise_num:I}]。</summary>
        public const int BABY_LIKE_RANK = 18208;
        /// <summary>宝宝获赞记录：请求体为空；回包 record_list[h×{praiser_id:l,name:s,is_praise_back:c}]。</summary>
        public const int BABY_LIKE_RECORDS = 18209;
        /// <summary>激活宝宝：请求体为空；回包 code:I。</summary>
        public const int BABY_ACTIVATE = 18210;
        /// <summary>宝宝阶段提升：请求体为空；回包 code:I,stage:h,stage_lv:c,stage_exp:I,power:I。</summary>
        public const int BABY_STAGE_UP = 18211;
        /// <summary>宝宝形象升星：请求 baby_id:I；回包 code:I,baby_id:I,baby_star:h,power:l,next_power:l。</summary>
        public const int BABY_FIGURE_STAR_UP = 18213;
        /// <summary>宝宝形象穿脱：请求 type:c,baby_id:I；回包 code:I,type:c,baby_id:I，type 仅 1/2。</summary>
        public const int BABY_FIGURE_WEAR = 18214;
        /// <summary>宝宝改名：请求 name:s；回包 code:I,name:s。</summary>
        public const int BABY_RENAME = 18215;
        /// <summary>展示宝宝到世界频道：请求体为空；pt_182.erl read(18216, _)；服务端当前不回包。</summary>
        public const int BABY_SHOW = 18216;
        /// <summary>宝宝点赞/回赞：请求 role_id:l,opr:c；回包 code:I,role_id:l,opr:c,rewards[h×{type:c,type_id:I,num:I}]。</summary>
        public const int BABY_PRAISE = 18217;
        /// <summary>宝宝装备穿戴：请求 pos:c,goods_id:l；回包 code:I,pos:c,goods_id:l,goods_type_id:I,skill_id:I,power:I。</summary>
        public const int BABY_EQUIP_WEAR = 18218;
        /// <summary>宝宝装备强化：请求 pos:c；回包 code:I,pos:c,goods_id:l,goods_type_id:I,stage:h,stage_lv:h,stage_exp:I,power:I。</summary>
        public const int BABY_EQUIP_UPGRADE = 18219;
        /// <summary>宝宝装备铭刻：请求 pos:c,count:h,type_id:i×count；回包 code:I,pos:c,goods_id:l,goods_type_id:I,skill_id:I,power:I。</summary>
        public const int BABY_EQUIP_IMPRINT = 18220;
        /// <summary>宝宝任务进度推送：task_id:h,finish_num:h,finish_state:c。</summary>
        public const int BABY_TASK_UPDATE = 18221;
        /// <summary>领取宝宝任务奖励：请求 task_id:h；回包 code:I,task_id:h,finish_num:h,finish_state:c。</summary>
        public const int BABY_TASK_REWARD = 18222;
        /// <summary>宝宝形象战力推送：请求 baby_id:I；回包 baby_id:I,baby_star:h,power:l,next_power:l（无 code）。</summary>
        public const int BABY_FIGURE_POWER = 18223;
        /// <summary>他人点赞推送：praiser_id:l（仅服务端主动下发）。</summary>
        public const int BABY_PRAISE_PUSH = 18224;

        // ----- 通用副本(pt_610,yu_server dungeon;老端 BaseDungeonController.ts。御魂本 type=12,dun_id 12001~) -----
        /// <summary>通用副本(pt_610)家族统一错误出口(轮22 族错误出口批;对标老端 BaseDungeonController.ts:668-673
        /// "通用错误返回",无条件 ErrorCodeShow(error_code)。服务端 send_dungeon_msg/2(lib_dungeon.erl:1341-1345)
        /// 是副本大量失败分支共享的错误壳,回包恒为错误码,老端忽略 error_code_args 字段)。
        /// 回包(pt_610.erl write(61000,[ErrorCode,ErrorCodeArgs])):code:i, args:s。</summary>
        public const int DUNGEON_ERROR = 61000;
        /// <summary>进入副本(发 "i" dun_id;回包 dun_id:i, scene_id:i, error_code:i, error_code_args:s)。
        /// 主线 100980(ctype9 id=12)=通关御魂本1层一次、101522(ctype57)=到3层。</summary>
        public const int DUNGEON_ENTER = 61001;
        /// <summary>副本结算 UI 推送——实证真正的"通用结算界面"(对标老端 BaseDungeonController.ts:767 起注册,
        /// 御魂本 dun_type=Rune 走第 911/976 行分支)。字段(ClientProtocol.json "61003"):
        /// result:c, result_subtype:c, dun_id:i, grade:c, scene_id:i,
        /// reward_list[u16×{style:c,typeId:i,count:l,goods_id:l}],
        /// other_reward[u16×{reward_type:c, other_reward_list[u16×{style1:c,type_id1:i,count1:l,goods_id1:l}]}],
        /// ex_data[u16×{key:h,val:i}], count:c。result==1 成功(同 61001/61002"1=成功"约定)。</summary>
        public const int DUNGEON_SETTLE_UI = 61003;
        /// <summary>⚠侦察实证(第18轮 A1 票):字段声明存在(result:c, help_type:c, score:h, pass_time:h,
        /// rela_list[u16×{role_id:l,rela_type:c,intimacy:i,is_ask_add:c,guild_id:l}], drop_reward:ObjectList,
        /// reward_list:ObjectList),但老端 h5/src 源码树里从未注册任何处理器;proto610.d.ts 类型声明写明
        /// desc="结算界面加好友,邀请加入公会,积分展示"——是结算面板的社交附加协议(好友/公会邀请,配合
        /// help_type 标志"助战类"副本),不是副本结算本体。御魂本结算实际走 <see cref="DUNGEON_SETTLE_UI"/>(61003)。
        /// 常量保留供后续如实测服务端确有下发再接;当前 DungeonController 不注册。</summary>
        public const int DUNGEON_SETTLE = 61013;
        /// <summary>副本状态/次数(请求体仅 dun_type:c 一个字段,对标 pt_610 read(61020);回包:dun_type:c +
        /// dun_list[u16×{dun_id:i, daily_count:h, weekly_count:h, permanent_count:h, reset_count:h, vip_count:h,
        /// add_count:h, is_sweep:c, rec_data[u16×{key:h,val:i}]}])。</summary>
        public const int DUNGEON_STATE = 61020;

        // ----- 通用副本扩容(自动循环 轮9;老端同一份 BaseDungeonController.ts/BaseDungeonModel.ts,
        //        字段序以 r9_server 侦察(pt_610.erl/pt_611.erl bit-width 实测)为准) -----
        /// <summary>副本信息(当前波次/开始结束时间)。纯推送壳,无 send 字段(read(61004,_)->{ok,[]});
        /// 回包:start_time:i, start_time_ms:l, end_time:i, level:h, level_end_time:i, owner_id:l, wave_num:i。
        /// 触发:①IsLoadingDunType 白名单类型(Rune/Partner/Dragon/Heart/Equip/Polar/SingleRank)服务端进副本后主动推;
        /// ②非白名单类型 61001 成功回调客户端显式补发本号(空参);③进副本场景后固定与 61018/61030 重发三连
        /// (对标老端 DungeonFightSceneView.LoadCustomLogic)。</summary>
        public const int DUNGEON_INFO = 61004;
        /// <summary>副本波次/事件推送(S2C,lib_dungeon_common_event 发)。禁止客户端发送;
        /// 回包 dun_id:i, scene_id:i, type:h, time:i, wave_num:i(老端存 curr_wave_type/curr_wave_num
        /// 并驱动 RefreshMonster/寻路——Unity 刷怪渲染由场景协议(12007/12012)承担,本端只落波次数据)。</summary>
        public const int DUNGEON_WAVE_PUSH = 61005;
        /// <summary>剧情触发推送(S2C,61010 的"预告"配对号)。禁止客户端发送;
        /// 回包 story_id:i, sub_sotry_id:i(⚠字段名 sotry 是老端/服务端一致的历史拼写)。</summary>
        public const int DUNGEON_STORY_PUSH = 61009;
        /// <summary>坐标事件(对标老端 TriggerFlushMonster,新主线/装备本"走到某坐标刷一批怪"机制)。
        /// 发 "hh"(x,y);回包原样回显 x:h,y:h,驱动 role_pos_event_list 状态机(命中范围内 trigger_state 置3完成,
        /// 曾触发中(2)未命中回退1)。</summary>
        public const int DUNGEON_POS_EVENT = 61007;
        /// <summary>剧情事件(⚠字段序陷阱,r9 侦察实证):真实发送点是老端 StoryController.ts:600 直发,
        /// fmt="iic"(story_id:i, sub_story_id:i, is_end:c)——不是 BaseDungeonController.ts 里那个从未被触发的
        /// "ilc"死分支。服务端(pp_dungeon.erl)本号无 write,纯 ack 无回包,本端不注册接收。</summary>
        public const int DUNGEON_STORY_EVENT = 61010;
        /// <summary>助战剩余次数(神纹/装备本"组队助战"型入口用)。发 "i"(dun_id);
        /// 回包 dun_id:i, left_help_count:c。</summary>
        public const int DUNGEON_HELP_COUNT = 61011;
        /// <summary>退出副本时间(副本内倒计时唯一数据源)。无 send 字段,裸发;
        /// 回包 type:c, end_time:i——仅 type==1 才有意义(0=该副本无倒计时配置)。</summary>
        public const int DUNGEON_EXIT_TIME = 61018;
        /// <summary>坐标触发情况表(61007 的断线重连/重进场景对账配对协议)。发 "i"(scene_id),每次进副本场景发一次;
        /// 回包 xy_list[u16×{x:i,y:i}](⚠与 61007 的 x/y 是 16 位不同,这里是 32 位)——命中的
        /// role_pos_event_list 项直接置 trigger_state=3,避免重进场景重复触发。</summary>
        public const int DUNGEON_POS_EVENT_LIST = 61019;
        /// <summary>下一波怪物生成时间(进副本场景固定三连之一,对标老端 mod_dungeon:get_next_wave_time)。
        /// 无 send 字段,裸发;回包 wave_num:i, time:i。</summary>
        public const int DUNGEON_NEXT_WAVE_TIME = 61030;
        /// <summary>购买副本次数。发 "ih"(dun_id, count;UI 恒传 count=1,无批量购买入口);
        /// 回包 error_code:i, dun_id:i, buy_count:h。成功后按 dun_type 分支:NEW_*/Material_*/Unreal/Soul/
        /// AdvancedExp 全组共享一个 vip_count 广播给同 type 所有条目,其余类型仅更新对应 dun_id 那条;
        /// dun_id==姻缘本(13001)且 error_code==6100043 → 专文案"购买次数已达上限"。</summary>
        public const int DUNGEON_BUY_COUNT = 61021;
        /// <summary>扫荡。发 "ih"(dun_id, auto_num);回包 error_code:i, dun_id:i, grade:c, left_count:h,
        /// auto_num:h, sweep_list[u16×{reward_list[u16×{style:c,typeId:i,count:i,goods_id:l}],
        /// other_reward[u16×{reward_type:c,other_reward_list[u16×{style1:c,typeId1:i,count1:i,goods_id1:l}]}]}]
        /// (⚠count 字段是 32 位,不同于 61003 的 64 位)。展示复用既有 DungeonResultView 通道。</summary>
        public const int DUNGEON_SWEEP = 61022;
        /// <summary>当前时间评分状态(装备本场景"星级评分随时间变化"倒计时用)。无 send 字段,裸发
        /// (⚠陷阱:老端调用方传了 dun_id 但该号走 default 分支会被静默丢弃,发送侧不要编码任何参数);
        /// 回包 cur_score:i, next_score:i, change_time:i。</summary>
        public const int DUNGEON_SCORE_STATE = 61023;
        /// <summary>鼓舞(经验副本消费加成)。发 "c"(cost_type;1=铜币,2=元宝);
        /// 回包 error_code:i, coin_count:c, gold_count:c。</summary>
        public const int DUNGEON_INSPIRIT = 61025;
        /// <summary>鼓舞状态数据(进经验本战斗界面/打开鼓舞面板各查一次)。无 send 字段,裸发;
        /// 回包 coin_count:c, gold_count:c。</summary>
        public const int DUNGEON_INSPIRIT_STATE = 61026;
        /// <summary>资源副本一键操作(对标老端 RequestDungeonChallenge;61028"批量扫荡"已死,本号+61121是替代版)。
        /// 发 "c"(oper_type;1=一键挑战,2=一键扫荡);回包 code:i, oper_type:c,
        /// sweep_list[u16×{reward_list[...],other_reward[...]}](与 61022 同款 reward item 形状,无 dun_id 逐项)。</summary>
        public const int DUNGEON_RESOURCE_ONEKEY = 61120;
        /// <summary>资源副本次数信息(对标老端 RequestDungeonNum;61020 处理完资源副本类型后补发本号)。
        /// 发 "c"(dun_type;0=查全部资源副本类型);回包 count_list[u16×{dun_type:c,sweep_count:h,challenge_count:h}]。</summary>
        public const int DUNGEON_RESOURCE_COUNT = 61121;
        /// <summary>回应邀请进入副本(轮22 族错误出口批;对标老端 BaseDungeonController.ts:1593-1601 内联
        /// handler:code==1 空分支/否则 ErrorCodeShow(code),无其它副作用)。回包(lib_dungeon.erl:2988
        /// offline_answer_invite_dun/1;pt_610.erl write(61047,[Code,Answer])):code:i, answer:c
        /// (answer 老端未读,本端同样只消费不透出)。</summary>
        public const int DUNGEON_INVITE_RESPOND = 61047;
        /// <summary>异兽入侵 领取阶段奖励(轮22 族错误出口批;对标老端 BaseDungeonController.ts:1848-1857
        /// 内联handler:error_code==1 分支 setMonsterInvasionReward 调用**已被老端注释**[纯死代码,运行时
        /// 无副作用],否则 ErrorCodeShow(error_code)——本端如实镜像"成功也不做事",不臆造奖励消费)。
        /// 回包(pp_dungeon.erl:631/643/648/652;pt_610.erl write(61092,[DunId,ErrorCode,RewardStatus,RewardList])):
        /// dun_id:i, code:i, reward_status:c, reward_list[u16×{style:c,typeId:i,num:i}]。</summary>
        public const int DUNGEON_MONSTER_INVASION_REWARD = 61092;

        // 以下号跳过(仅存说明,不写代码;轮9 双端侦察定案,见 r9_olddungeon/r9_server):
        // 61006(事件触发)/61014(剧情播放列表)/61016(结算界面2关卡)/61017(跳过副本)/61024(副本可用性)/
        //   61027(副本重置):服务端全活,但老端 h5/src 全树零引用(无注册无发送),UNUSED 不移植。
        //   61024 被 61121+61042+前端本地算取代;61014 被单条 61009 取代;61016 配套的 61015 同样零引用。
        // 61028(按类型批量扫荡):老端 registered 但无任何 UI 触达路径,被 61120+61121 取代的死协议,不移植。
        // 61012/61029/61057/61060/61099(610段)+61119(611段):服务端 DEAD(write 调用点全被注释/handle 直接 skip,
        //   照 r9_server §DEAD 清单),双向皆死不移植。
        // 61031-61041:守卫公会本(GuildGuard)专属(击杀数/伤害榜/怪物血量/波数/摘要),老端注册在
        //   GuildController.ts 非 BaseDungeonController——归公会包,本轮不碰。
        // 61112-61116:灵魄本(Rune)专属奖励系统(通用奖励领取/列表 61112-13+符文每日奖励/状态/解锁 61114-16),
        //   非"塔"——归灵魄奖励包(老端 InitDunData Rune 分支的 61113/61115 触发一并留待该包)。
        // 61117 已接(BaseDungeonController 限时爬塔图标);61118(限时爬塔大奖领取)塔二期。
        // 50805(周本专属结算推送,不复用 61003):DungeonPolarBalance 结算面板未移植,周本二期。

        // ----- 周常副本(pt_508,yu_server week_dungeon;老端 DungeonPolarView.ts/DungeonPolarRankView.ts。
        //        周本(Polar,dun_type=36)是独立于 61xxx 通用副本的数据线,不挂 DungeonModel.DunStatesByType) -----
        /// <summary>玩家的周常副本信息。无 send 字段,裸发(老端周本大厅加载完成时查一次);
        /// 回包 dun_list[u16×{week_dun_id:i, dun_score:h, single_succ:c, team_succ:c, help_times:h,
        /// boss_reward[u16×{boss_id:i, reward_st:c}]}]。</summary>
        public const int POLAR_WEEK_INFO = 50801;
        /// <summary>周本榜单。发 "icc"(team_dun_id, rank1, rank2;老端固定查第1~10名);
        /// 回包 team_dun_id:i, self_rank:c, self_pass_time:h,
        /// rank_list[u16×{pass_time:h,time:i,rank:c,role_list[u16×{role_id:l,role_name:s,server_id:h,server_num:h}]}]。</summary>
        public const int POLAR_RANK = 50802;

        // ----- 灵魄/符文(pt_167,yu_server rune;老端 RuneBagItem.ts/SecretTreasureMainView) -----
        /// <summary>符文全量(请求无参;回包 rune_point:i, rune_chip:i, skill_lv:h, rune_list[u16×{pos_id:c, if_open:c,
        /// goods_id:l, goods_type_id:i, color:c, lv:h, attr_list[u16×{attr_id:i, attr_num:i, awake_lv:i, awake_exp:i,
        /// next_power:l, cur_power:l}]}], rune_sum_power:l)。</summary>
        public const int RUNE_INFO = 16700;
        /// <summary>镶嵌符文(发 "cl" pos_id, goods_id;回包 code:i, pos_id:c, new_goods_id:l, old_goods_id:l,
        /// new_goods_type_id:i)。主线 100990(ctype33)=镶嵌一次(孔位1无条件开放)。</summary>
        public const int RUNE_WEAR = 16701;

        // ----- 结社/公会(pt_400,yu_server guild;老端 GuildJoinView.ts/GuildBuildView.ts) -----
        /// <summary>结社列表(发 "shh" name,pageSize,pageNo;回包 page_total:h, page_no:h, guild_list[u16×{guild_id:l,
        /// guild_name:s, guild_lv:h, guild_exp:i, chief_id:l, chief_name:s, member_num:h, member_capacity:h,
        /// is_apply:c, auto_approve_power:i, combat_power:l, merge_status:c, is_master:c}])。</summary>
        public const int GUILD_LIST = 40001;
        /// <summary>一键批量申请加入(无参;回包 error_code:i, guild_id:l, apply_type:c)。</summary>
        public const int GUILD_APPLY_ALL = 40003;
        /// <summary>创建结社(发 "ls" cfgId=2, name;回包 error_code:i, guild_id:l)。空服最短路径:建社成功即
        /// join_guild 事件 → 主线 101080(ctype14)完成。</summary>
        public const int GUILD_CREATE = 40004;
        /// <summary>任务系统补触发加入结社判定(C2S 无参,对标老端 GuildJoinView 打开时发 30008)。</summary>
        public const int CC_TASK_JOIN_GUILD = 30008;
        /// <summary>申请加入指定公会(发 "l" guild_id;回包 error_code:i, guild_id:l, apply_type:c——
        /// 与 40003 同结构。GuildListItem/TopItem 逐行"申请"按钮用,区别于 40003 一键批量)。</summary>
        public const int GUILD_APPLY_ONE = 40002;

        // ----- 公会核心一期(自动循环 轮13a;pt_400 第1组33活号,GuildController 新控制器;
        //        wire 权威=r13_server_pt400.md §字段序,老端 GuildController.ts/GuildModel.ts 格式串交叉) -----
        /// <summary>公会协议家族共享错误壳(仅 write,无 read;error_code:i)。前置粗校验(40013任命互斥/
        /// 40029自嘲/40042未入会/40043改名checklist)专用,自号回错的号不走这里。**补全**:40002/40003
        /// 加入申请的前置三校验(已在公会/等级不足/圣域中)失败也走这里(pp_guild.erl 40002/40003 分支);
        /// 40004 建会失败同样先走这里(lib_guild.erl create_guild 失败分支),只有真正建会成功才回 40004
        /// 自己的号(该失败分支在服务端现状下理论不可达,Unity On40004 的失败处理防御性无害)。</summary>
        public const int GUILD_ERROR = 40000;
        /// <summary>公会基础信息(C2S 无参;回包 guild_id:l, guild_name:s, announce:s,
        /// position_list[u16×{position:c, role_id:l, figure}], guild_lv:h, gfunds:i, growth_val:i, gactivity:i,
        /// member_num:h, member_capacity:h, combat_power:l(**实为前十战力和 combat_power_ten**), online_num:h,
        /// disband_warnning_time:i, salary_status:c, division:c, join_time:i, is_in_merge:c)。</summary>
        public const int GUILD_BASE_INFO = 40005;
        /// <summary>公会成员列表(C2S 无参,服务端无分页,规模=member_capacity;回包
        /// member_list[u16×{role_id:l, figure, position:c, title_id:i, combat_power:l, online_flag:c,
        /// offline_time:i, create_time:i}])。</summary>
        public const int GUILD_MEMBER_LIST = 40006;
        /// <summary>退出结社(C2S 无参;回包 error_code:i)。前置 mutex(晚宴/领地战/结社副本/圣域/协助/怒海每日),
        /// 失败走自己的号。</summary>
        public const int GUILD_QUIT = 40007;
        /// <summary>申请列表(C2S 无参,上限20(仅手动审批公会生效);回包
        /// apply_list[u16×{role_id:l, figure, combat_power:l}])。</summary>
        public const int GUILD_APPLY_LIST = 40008;
        /// <summary>审批单条申请(发 "lc" role_id, type[1同意/0拒绝];回包 error_code:i, type:c, role_id:l)。
        /// 审批人不存在/无权限/申请记录不存在=静默不回包。</summary>
        public const int GUILD_APPLY_APPROVE = 40009;
        /// <summary>查审批设置(C2S 无参;回包 approve_type:c, auto_approve_lv:h, auto_approve_power:i)。</summary>
        public const int GUILD_APPLY_SETTING_INFO = 40010;
        /// <summary>设置审批规则(发 "chi" approve_type, auto_approve_lv, auto_approve_power;
        /// 回包 error_code:i)。**订正**:pp_guild 前置层 ErrorCode==nothing 时确实 skip 不发,但已 cast
        /// 出去的业务层(mod_guild_cast.erl 'setting_approve')结尾无条件回包,成功时 error_code==1 一样
        /// 会到达——并非"成功静默,收到即失败"(此前注释系对前置层的误读)。</summary>
        public const int GUILD_APPLY_SETTING_SET = 40011;
        /// <summary>编辑公告(发 "cs" save_type[1保存/2保存并通知], announce;回包 error_code:i)。
        /// **订正(同40011)**:'modify_announce' 结尾无条件回包,error_code==1 为真成功,并非静默。
        /// 唯一纯等级门:公会等级&lt;4 拒。</summary>
        public const int GUILD_ANNOUNCE_SET = 40012;
        /// <summary>任命职位(发 "lc" role_id, position;回包 error_code:i, role_id:l, position:c)。
        /// 转会长分支互斥锁前置失败走共享 40000(与 40007/14 不同),业务层失败回自己的号。</summary>
        public const int GUILD_APPOINT_POSITION = 40013;
        /// <summary>踢出成员(发 "l" role_id;回包 error_code:i, role_id:l)。</summary>
        public const int GUILD_KICK = 40014;
        /// <summary>玩家自身公会信息(C2S 无参,被动补发点极多——入会/改名/合并/职位变更;回包
        /// guild_id:l, guild_name:s, guild_lv:h, position:c, position_name:s)。落 RoleModel 主角VO。</summary>
        public const int GUILD_SELF_INFO = 40015;
        /// <summary>全部批准/拒绝申请(发 "c" type[1同意/2拒绝];回包 error_code:i, type:c)。
        /// Type∉{1,2} 服务端子句不匹配=静默丢弃,严禁发其它值。</summary>
        public const int GUILD_APPLY_BULK_HANDLE = 40016;
        /// <summary>场景广播(纯推送,无 C2S;role_id:l, guild_id:l, guild_name:s, position:c, position_name:s)。
        /// 按当前地图区域池广播(非公会广播),用于更新他人头顶公会名牌;Common/UI3D 红线内不接场景消费,仅解析+事件。</summary>
        public const int GUILD_SCENE_BROADCAST = 40017;
        /// <summary>公会升级(**老端从未真实发送**,"升级仙宗"按钮只弹提示,本轮不做真实发送 API;
        /// 回包 error_code:i——**必须接 recv**:操作者私有确认 + 等级真变化时公会全员广播[固定1],
        /// 同一操作可能收到两份,按"收到即刷新"处理,不辨来源)。</summary>
        public const int GUILD_UPGRADE = 40018;
        /// <summary>公告编辑界面信息(**纯死号,老端 handler 函数体为空且从无主动请求点**;回包
        /// remain_times:c, free_times:c)。本轮仅注册防御 no-op handler,不发送、不消费。</summary>
        public const int GUILD_ANNOUNCE_INFO = 40019;
        /// <summary>领取公会工资(C2S 无参,每日一次;回包 error_code:i)。</summary>
        public const int GUILD_SALARY = 40020;
        /// <summary>权限列表(C2S 无参,不在公会时回空列表非静默;回包
        /// permission_type_list[u16×{permission_type:c}])。</summary>
        public const int GUILD_PERMISSION_LIST = 40021;
        /// <summary>捐献信息(C2S 无参,混在批量拉取里仍活跃请求,UI 不建;回包 gactivity:i, donate_times:c,
        /// self_gift_list[u16×{gift_id:h, gift_status:c}], donate_record[u16×{donate_id:i, role_id:l,
        /// role_name:s, donate_type:c, times:c, donate_add:h, gfunds_add:h, guild_activity:h, time:i}]
        /// (item_to_bin_6 字段序按 40026 同名"捐献记录"结构类推,报告未逐字段列出,已标注假设))。</summary>
        public const int GUILD_DONATE_INFO = 40023;
        /// <summary>解散公会(会长专属,C2S 无参;回包 error_code:i)。圣域场景内禁止解散走自己的号。</summary>
        public const int GUILD_DISBAND = 40027;
        /// <summary>公会活跃度查询/推送(C2S 无参;回包 gactivity:i)。</summary>
        public const int GUILD_ACTIVITY = 40028;
        /// <summary>调戏(发 "l" role_id;**recv:null,服务端无 write 调用点,纯发**;自娱自乐/不同公会走共享
        /// 40000 静默或回错,正常路径只触发公会聊天频道飘字)。</summary>
        public const int GUILD_TEASE = 40029;
        /// <summary>玩家声望信息(C2S 无参;回包 all_prestige:i, title_id:i, prestige_week:i, prestige_limit:i)。</summary>
        public const int GUILD_PRESTIGE_INFO = 40030;
        /// <summary>今日声望推送/查询(C2S 无参;回包 all_prestige:i, prestige_day:i, prestige_day_limit:i)。</summary>
        public const int GUILD_PRESTIGE_DAILY = 40031;
        /// <summary>贡献值变化推送(纯推送,无 C2S;new_donate:i)。仅被动获得贡献(任务奖励等)时触发,
        /// 不伴随死掉的 40024 主动捐献 UI。</summary>
        public const int GUILD_DONATE_PUSH = 40039;
        /// <summary>公会技能列表(发 "c" type[1基础/2高级];回包 donate:i,
        /// skill_list[u16×{skill_id:i, learn_lv:c, research_lv:c, cur_power:l, next_power:l}])。</summary>
        public const int GUILD_SKILL_LIST = 40040;
        /// <summary>学习公会技能(发 "i" skill_id;回包 error_code:i, skill_id:i, learn_lv:c, donate:i(**学习后剩余
        /// 贡献值,非本次消耗**), cur_power:l, next_power:l)。未入会前置走共享 40000,深层业务失败回自己的号。</summary>
        public const int GUILD_SKILL_LEARN = 40042;
        /// <summary>公会改名(发 "s" new_name;回包 error_code:i, new_name:s——**深层9项checklist失败一律走
        /// 共享40000,只有真正扣费成功才回自己的号**)。</summary>
        public const int GUILD_RENAME = 40043;
        /// <summary>改名信息(C2S 无参;回包 is_free:c, next_rename_time:i)。</summary>
        public const int GUILD_RENAME_INFO = 40044;
        /// <summary>仙宗召援(C2S 无参;回包 role_id:l, role_name:s, role_lv:h, role_career:c, role_sex:c,
        /// role_pic:s, role_pic_ver:i, boss_type:h, boss_type_name:s, boss_id:i, layer:c, scene_id:i, x:h, y:h)。
        /// 真公会广播(send_to_guild),收到时非自己发起才提示。</summary>
        public const int GUILD_BOSS_CALL = 40060;
        /// <summary>公会合并候选列表(C2S 无参;回包 guild_list[u16×{同 40001 item_to_bin_0 结构}])。</summary>
        public const int GUILD_MERGE_LIST = 40061;
        /// <summary>申请合并指定公会(发 "l" guild_id;回包 error_code:i, guild_id:l)。成功后联动推给对方会长
        /// 一份新的 40061。</summary>
        public const int GUILD_MERGE_APPLY = 40062;
        /// <summary>响应合并申请(发 "cl" op_type[1同意/2拒绝], guild_id;回包 error_code:i, guild_id:l)。</summary>
        public const int GUILD_MERGE_RESPONSE = 40063;

        // ----- 公会二期:结社仓库(自动循环 轮13b;pt_401,wire 权威=yu_server src/pt/pt_401.erl 源码逐字节读出,
        //        非报告转述;老端 GuildDepotView.ts/GuildDepotItem.ts 交叉) -----
        /// <summary>仓库家族共享错误壳(仅 write,error_code:i)。40102/40103 静默陷阱(Num&lt;=0 且非任务装备id/
        /// 40104 空数组)不经这里,是真无回包,不要等它。</summary>
        public const int GUILD_DEPOT_ERROR = 40100;
        /// <summary>仓库信息(C2S 无参;回包 depot_score:i, exchange_records[u16×{id:i,role_name:s,exchange_type:c,
        /// goods_id:l,type_id:i,color:c,rating:i,overall_rating:i,addition_attrlist[u16×{attr_type:c,attr_value:i,
        /// color:c,combat_power:i}],equip_extra_attr[u16×{color:c,type_id:c,attr_id:h,attr_val:i,plus_interval:c,
        /// plus_unit:i}],stone_list[u16×{pos:c,type_id:i}],wash_attr[u16×{index:c,color:c,attr_id:h,attr_val:i}],
        /// suit_lv:c,suit_slv:h,suit_count:c,time:i}], depot_goods[u16×{同上 12 字段(无 id/role_name/exchange_type/
        /// time),多一个 goods_num:i}])。列表头部可能有一条虚构任务装备条目(goods_id=1,不对应真实仓库记录)。</summary>
        public const int GUILD_DEPOT_INFO = 40101;
        /// <summary>捐献装备入仓库(自定义变长数组,非固定 fmt:发 "h"+count,逐条 "li" goods_id,num;
        /// 回包 error_code:i, depot_score:i——**该号 ErrorCode 恒为成功,失败改走共享 40100**)。空列表本地拦截不发。</summary>
        public const int GUILD_DEPOT_DONATE = 40102;
        /// <summary>积分兑换仓库物品(发 "lii" goods_id,type_id,num;回包 error_code:i, depot_score:i)。
        /// **静默陷阱**:Num&lt;=0 且 goods_id≠任务装备id(=1)时服务端两个 do_handle 子句都不匹配,真无回包
        /// (发送侧本地锁死 Num&gt;0);任务装备兑换必须 Num 精确=1(≠1 会被错误路由到通用兑换分支,
        /// 大概率回"物品不在仓库",发送侧本地锁死任务装备 Num=1)。经验道具兑换失败改走共享 40100 而非本号
        /// (三条兑换路径里唯一不同的一条,与老端一致)。</summary>
        public const int GUILD_DEPOT_EXCHANGE = 40103;
        /// <summary>销毁仓库物品(自定义变长数组:发 "h"+count,逐条 "l" goods_id;回包 error_code:i, op_type:c
        /// [3手动/4自动], depot_num:i)。空列表本地拦截不发(服务端同样静默,不依赖它兜底)。</summary>
        public const int GUILD_DEPOT_DESTROY = 40104;
        /// <summary>仓库物品新增推送(纯推送,无 C2S;depot_goods[u16×{同 40101 depot_goods 单条结构,13 字段}])。</summary>
        public const int GUILD_DEPOT_GOODS_ADD = 40105;
        /// <summary>仓库物品数量增量推送(纯推送;depot_goods[u16×{goods_id:l, num:i}](num=0=删除,精简结构非
        /// 完整物品,任务装备兑换后传的是虚构条目清零 {1,0}))。</summary>
        public const int GUILD_DEPOT_GOODS_NUM = 40106;
        /// <summary>兑换记录新增推送(纯推送;exchange_records[u16×{同 40101 exchange_records 单条结构,16 字段}])。</summary>
        public const int GUILD_DEPOT_RECORD_PUSH = 40107;
        /// <summary>仓库更新广播(公会全员,send_to_guild;change:c,四处调用点硬编码恒为 1)。任务装备/经验道具
        /// 兑换两条路径不触发此号(只影响个人虚拟条目,不动公会共享物品池,设计如此非遗漏)。</summary>
        public const int GUILD_DEPOT_CHANGE = 40108;
        /// <summary>按条件批量销毁设置(发 "ccc" stage,color,star;**recv:null,服务端无 write(40109,...) 子句,
        /// 响应借道 40104**——严禁按老端"复制粘贴读40108"的 handler bug 照抄,不要为本号注册接收器)。</summary>
        public const int GUILD_DEPOT_AUTO_DESTROY_SET = 40109;
        /// <summary>查当前自动清理条件(C2S 无参;回包 stage:c,color:c,star:c)。</summary>
        public const int GUILD_DEPOT_AUTO_DESTROY_INFO = 40110;

        // ----- 公会二期:结社宝箱(自动循环 轮13b;pt_403,wire 权威=pt_403.erl 源码) -----
        /// <summary>宝箱家族共享错误壳(仅 write,error_code:i;仅1处真实调用点但确认存活)。</summary>
        public const int GUILD_BOX_ERROR = 40300;
        /// <summary>宝箱信息(C2S 无参;回包 num:h, max_num:h, send_list[u16×{auto_id:l,role_name:s,role_id:l,
        /// task_id:i,status:c,reward:ObjectList,time:i}], log[u16×{role_name:s,role_id:l,task_id:i,time:i}],
        /// info[u16×{task_id:i,send_num:c}])。</summary>
        public const int GUILD_BOX_INFO = 40301;
        /// <summary>领取宝箱(发 "l" auto_id——**64 位!服务端 `AutoId:64` 源码原文,老端 r13_oldguild 文档写的
        /// `h` gift_id 是文档命名混淆(与短id宝箱语义混淆),16 位是老端 bug,严禁照抄**;auto_id=0=一键领取;
        /// 回包 code:i, send_list[u16×{auto_id:l, reward:ObjectList}])。</summary>
        public const int GUILD_BOX_RECEIVE = 40302;
        /// <summary>新宝箱记录推送(公会全员广播;send_list[u16×{同 40301 send_list 结构}], log[u16×{同 40301
        /// log 结构}])。</summary>
        public const int GUILD_BOX_NEW_PUSH = 40303;
        /// <summary>宝箱记录失效推送(公会全员广播,GM清空/过期自动清理;auto_id:l——按 id 移除单条)。</summary>
        public const int GUILD_BOX_REMOVE_PUSH = 40304;
        /// <summary>任务发放次数状态推送(info[u16×{task_id:i,send_num:c}])。**三种触发范围混用**:①单人完成
        /// 任务后仅发给操作者本人(增量1条);②day_clear 每日重置 / gm_clear GM清空——走 `send_to_all/1`
        /// **全服广播,不分公会**(不带 GuildId 过滤信息)。recv 端**严禁假设收到即代表自己有公会**,必须容忍
        /// 无公会/未加载 GuildModel.Info 场景下也收到本号(纯按 TaskInfoList 内容更新,不触发红点)。</summary>
        public const int GUILD_BOX_TASK_INFO_PUSH = 40305;

        // ----- 公会二期:结社协助(自动循环 轮13b;pt_404,wire 权威=pt_404.erl 源码) -----
        /// <summary>发起协助请求(发 "chil" type[1boss/2副本/3璀璨之海/4主线本],sub_type,target_cfg_id,target_id;
        /// 回包 error_code:i, assist_id:l(早期拒绝分支恒 0,成功分支为服务端分配 id), type:c, sub_type:h,
        /// target_cfg_id:i, target_id:l)。无独立错误壳,首字段即 ErrorCode。</summary>
        public const int GUILD_ASSIST_LAUNCH = 40401;
        /// <summary>协助他人(发 "lc" assist_id,type——**服务端业务层丢弃客户端 Type,只用 AssistId**;
        /// 回包 error_code:i, assist_id:l, type:c(深层分支回显服务端权威 LaunchAssist.type,早期失败分支才回显
        /// 客户端原始值))。</summary>
        public const int GUILD_ASSIST_HELP = 40402;
        /// <summary>取消协助/求助(发 "l" assist_id;回包 error_code:i, cancel_type:c[1主动/2璀璨之海结算触发],
        /// assist_id:l, ask_id:l——按 ask_id 是否是自己区分"取消成功"vs"对方取消了对我的协助")。</summary>
        public const int GUILD_ASSIST_CANCEL = 40403;
        /// <summary>今日协助成功次数(C2S 无参;回包 assist_count:c——**8位!非常见的 i/h**)。**静默陷阱**:
        /// `AssistId&gt;0 andalso AssistProcess==1` 条件不满足时(纯查询无进行中协助)服务端直接 ok 不回包,
        /// 发送侧不能假设必有响应。</summary>
        public const int GUILD_ASSIST_COUNT = 40404;
        /// <summary>求助列表(C2S 无参,**服务端全局 map 靠 GuildId 过滤,无任何长度上限**;回包
        /// assist_list[u16×{同 40406 单条结构,14 字段}])。</summary>
        public const int GUILD_ASSIST_LIST = 40405;
        /// <summary>新求助推送(公会全员广播;assist_id:l,type:c,sub_type:h,target_cfg_id:i,target_id:l,
        /// role_id:l,name:s,level:h,career:c,sex:c,pic:s,pic_ver:i,is_assist:c(广播时刻恒0,还没人应助),
        /// extra:ObjectList嵌套变长(仅 type==3 璀璨之海非空,字段 ser_id:i,ser_num:h,rober_id:l,rober_name:s,
        /// rober_power:i,rober_reward:ObjectList,back_reward:ObjectList,共7字段))。</summary>
        public const int GUILD_ASSIST_NEW_PUSH = 40406;
        /// <summary>求助结束/失效推送(公会全员广播;assist_id:l)。**扇出模式**:发起者"取消全部协助"场景是
        /// "1次本号广播(全公会,告知求助消失)+ N次 40403 单播(每个正在协助中的协助者各一份,通知其协助被取消)"
        /// 的组合,recv 端必须按条处理,不能当全量刷新——收到一条只移除这一条 assist_id。</summary>
        public const int GUILD_ASSIST_REMOVE_PUSH = 40407;
        /// <summary>查当前正在协助的对象(C2S 无参;回包 assist_id:l,type:c,sub_type:h,target_cfg_id:i,
        /// target_id:l,role_id:l,name:s,level:h,career:c,sex:c,pic:s,pic_ver:i——12字段,比40406/40405单条
        /// 少 is_assist+extra 两项)。</summary>
        public const int GUILD_ASSIST_MY_INFO = 40408;
        /// <summary>协助成功通知(纯推送,面向协助者;assist_id:l)。区别于 40407(面向全公会/求助者)。</summary>
        public const int GUILD_ASSIST_SUCCESS_PUSH = 40409;
        /// <summary>有人接受协助通知(纯推送,面向求助者;assist_id:l, role_id:l(协助者), name:s(协助者名))。</summary>
        public const int GUILD_ASSIST_ACCEPTED_PUSH = 40410;

        // ----- 公会二期:结社武魂/神像(自动循环 轮13b;pt_405,wire 权威=pt_405.erl 源码;
        //        神像进度是 per-player 存储(SQL 按 RoleId 查/存),与 GuildId 无存储层绑定,仅解锁门槛依赖公会
        //        等级/头衔——不做全公会广播,全部 send_to_uid 仅操作者本人) -----
        /// <summary>神像家族共享错误壳(errcode:i;顶层门槛[开服天数/角色等级]+几乎全部业务失败分支共用,
        /// 是四族里调用最密集的错误壳)。</summary>
        public const int GUILD_GOD_ERROR = 40500;
        /// <summary>神像总览(C2S 无参,仅 GuildIdol 功能开放才发;回包 guild_title_lv:h, god_list[u16×{god_id:h,
        /// color:c,lv:h,god_power:l}]——遍历配置里**全部**神像id(未激活的以{Id,0,0,0}占位),非"已拥有"列表)。</summary>
        public const int GUILD_GOD_INFO = 40501;
        /// <summary>单神像铭文详情(发 "h" god_id;回包 god_id:h, rune_list[u16×{pos:c,goods_id:l,goods_type_id:i}]
        /// (至多6条,槽位上限=?pos_list), combo_id:c(每次查询重新校验有效性,不满足强制清零), achievement_lvs
        /// [u16×{lv:h}](同样重新过滤), god_power:l)。**事实上的"万能刷新推送号"**——40505/506/507/508/509
        /// 五个操作成功后统一补发本号刷新,不是各自独立确认。</summary>
        public const int GUILD_GOD_RUNE_INFO = 40502;
        /// <summary>神像升品(发 "h" god_id;回包 god_id:h, color:c[升品后新品质], lv:h, god_power:l)。</summary>
        public const int GUILD_GOD_COLOR_UP = 40503;
        /// <summary>神像觉醒(发 "h" god_id;回包 god_id:h, color:c[本次未变], lv:h[觉醒后新等级], god_power:l)。
        /// **同一字段位置在40503/40504语义不同**(是否为本次变更值),消费方不要弄反。</summary>
        public const int GUILD_GOD_AWAKE = 40504;
        /// <summary>穿戴铭文(发 "hcl" god_id,pos_id,goods_id)。**DEAD——全仓库排除四参遮蔽后确认无任何
        /// write(40505,...) 调用点,协议格式完整但业务代码统一改用 40502 全量刷新代替**;发送侧照常发送
        /// (真实用户操作),**接收侧严禁注册 handler**(永远收不到,注册了也是死代码),结果只能靠 40502 到达判断。</summary>
        public const int GUILD_GOD_WEAR = 40505;
        /// <summary>激活铭文组合(发 "hc" god_id,combo_id)。**协议层设计上就没有 write 方向**(pt_405.erl 的
        /// write 子句列表里 40505 后面直接跳到 40507,40506 连定义都没有——不同于40505/507"定义了但弃用",
        /// 本号是从设计上就单向),响应同样借道 40502/40500,**接收侧不注册**。</summary>
        public const int GUILD_GOD_COMBO_ACTIVATE = 40506;
        /// <summary>脱下铭文(发 "hc" god_id,pos)。**DEAD,与40505同一模式**(协议格式已定义 code:i,业务代码
        /// 从未调用),发送侧照常发送,**接收侧严禁注册 handler**。</summary>
        public const int GUILD_GOD_TAKE_OFF = 40507;
        /// <summary>升级铭文(发 "hc" god_id,pos;回包 code:i——真实存活,与40505/507对照组,证明该模式本可以
        /// 有独立确认号,只是这两个是真被弃用)。成功先补发40502再回本号。</summary>
        public const int GUILD_GOD_RUNE_UPGRADE = 40508;
        /// <summary>激活铭文大师等级(发 "ch" god_id,lv——**注意此号 GodId 是 8 位,是本族唯一独例,其余 8 个
        /// 神像号 GodId 均为 16 位,不可类推复用同一套解析函数**;回包 code:i)。成功先补发40502再回本号,
        /// 隐式门槛:6个铭文槽位须全部插满(check_achievement_lv 要求 RuneList 长度≥6)。</summary>
        public const int GUILD_GOD_ACHIEVEMENT_ACTIVATE = 40509;

        // ----- 薄增量六件套(第20轮工单;详见 Docs/工单-薄增量六件套.md) -----
        /// <summary>OutWard 通用一键升星(type_id∉{1,2}:3翼影/4圣器/5神兵;发 "c" type_id;
        /// 回包=16023 少 etime/auto_buy:errcode:i, type_id:c, stage:c, star:h, blessing:i, blessing_plus:i,
        /// ratio_list[u16×{rate:c,rate_num:h}])。解主线 100665/101045/101345(ctype24/92/41)。</summary>
        public const int OUTWARD_STAR_UP_GENERIC = 16005;
        // ----- 第21轮 OutWard 全类型补漏(pt_160.erl:122-125/703-714;老端 OutWardController.ts:317-330/354) -----
        /// <summary>外观等级线·系统B技能升级(发 "ci" type_id,skill_id;回包:errcode:i, type_id:c, skill_id:i, level:c)。
        /// ⚠侦察订正(第21轮):系统B(16028/16029/本号)对全部 6 个 type_id{1,2,3,4,5,12}都活,并非仅坐骑/同修专属
        /// (config_mount_level 每 type_id 各 750 条;lib_mount_upgrade_sys.erl:33-43 send_panel_info 不含 type_id guard)。
        /// errcode==1 成功后老端另拉一次 16002(REQUEST_PROTO)联动刷新,同 16023/16029 惯例。</summary>
        public const int OUTWARD_LV_SKILL_UP = 16030;

        /// <summary>宝石镶嵌(发 "ccl" equipPos,stonePos,goodsId;回包 res:i, equip_type:c, pos:c, type_id:i)。主线 101175(ctype48)。</summary>
        public const int EQUIP_STONE_SET = 15208;
        /// <summary>宝石拆除(发 "cc";回包 res:i, equip_type:c, pos:c)。</summary>
        public const int EQUIP_STONE_UNSET = 15209;
        /// <summary>挂机到点更新(C2S 空包；S2C error_code:i,next_time:i,had_afk_time:i)。</summary>
        public const int ONHOOK_TICK = 13211;
        /// <summary>挂机收益信息(C2S 空包;S2C login_type:c,off_lv:h,cost_afk_time:i,reward:ObjectList,
        /// back_count:i,back_exp:l,afk_time:i,next_time:i,exp_effect:l,had_afk_time:i)。</summary>
        public const int ONHOOK_INFO = 13212;
        /// <summary>挂机时长更新(C2S 空包；S2C afk_time:i,next_time:i)。</summary>
        public const int ONHOOK_TIME_UPDATE = 13214;
        /// <summary>挂机经验效率推送（C2S 空包但老端无主动发送入口；S2C exp_effect:l/u64）。仅被动接收，禁止轮询。</summary>
        public const int ONHOOK_EXP_EFFECT = 13215;
        /// <summary>领取挂机收益(C2S 空包；S2C errcode:i,old_lv:h,old_lv_ratio:h,goods_list[u16×{style:c,typeId:i,count:l}])。
        /// 主线 101211(ctype91,唯一事件计数型:领一次即完成)。</summary>
        public const int ONHOOK_RECEIVE = 13216;
        /// <summary>挂机经验加成完整列表。C2S 空包；S2C: count:u16×{type:u32,ratio:u64,end_time:u32}。</summary>
        public const int ONHOOK_EXP_ADDITIONS = 13217;
        /// <summary>物品自动熔炼经验服务端主动推送：exp_list[u16 × {add_exp:u16,ratio:u8}]。</summary>
        public const int ONHOOK_AUTO_SMELT_EXP = 13218;
        /// <summary>装备家族统一错误码出口(对标老端 EquipController.ts:274-282 On15200:
        /// Util.ErrorCodeShow(scmd.res);res==1520090/1520091 两个分支老端均为空/已注释,无额外副作用——
        /// 满足断言E族错误出口收紧规则)。轮21 覆盖率审计发现的跨系统统一错误码出口漏号之一(同批
        /// 11000/40505/40507,见 r21_coverage_governance.md)。回包只有 res:i。</summary>
        public const int EQUIP_ERROR = 15200;
        /// <summary>穿戴装备(发 "l" goods_id 实例id;回包 res:i, goods_id:l, old_goods_id:l, type_id:i, cell_pos:c)。
        /// 主线 101205(ctype93 穿3件3阶橙装,状态快照自动判定)。</summary>
        public const int EQUIP_WEAR = 15201;
        /// <summary>背包熔炼信息(无参;回包 level:h, exp:i)。</summary>
        public const int BAG_FUSION_INFO = 15024;
        /// <summary>背包熔炼(发 h count + 逐项 l goods_id/i num,对标 OnDevourEquipment WriteBegin(15025);
        /// 回包 code:i + exp_list[u16×{add_exp:h, ratio:c}],随后服务端另推 15024)。主线 101285(ctype18)。</summary>
        public const int BAG_FUSION = 15025;

        // ----- 天命觉醒(pt_429,yu_server temple_awaken;老端 TempleAwakenEnterView.ts) -----
        /// <summary>完成觉醒之路初始任务(C2S 无参;回包 error_code:i,==1 成功 → 服务端 open_temple_awaken 推进
        /// 主线 100590(ctype81);前置=任务 100580 完成,KV(6) 等级门槛服务端校验)。</summary>
        public const int TEMPLE_AWAKEN_FINISH_INITIAL = 42900;
        /// <summary>天命觉醒全量状态树。C2S 空包；S2C 为章节/子章/阶段进度。</summary>
        public const int TEMPLE_AWAKEN_INFO = 42901;
        /// <summary>觉醒之路前置任务完成态推送(is_finish:c)。</summary>
        public const int TEMPLE_AWAKEN_PRE_STATE = 42909;

        // ----- 装备强化(pt_152 段内 15204/15205;老端 EquipController.ts + EquipStrenView.ts) -----
        /// <summary>查询槽位强化信息(发 "c" equip_type;回包 res:i, equip_type:c, stren:h)。</summary>
        public const int EQUIP_STREN_INFO = 15204;
        /// <summary>执行强化(发 "cc" equip_type,type[1单次/2一键(equip_type 传 0)];回包 res:i, res1:c, type:c,
        /// stren_info[u16×{equip_type:c, stren:h}])。主线 100720(ctype31)=全身强化总和≥8,服务端 equip_sum 事件推进。</summary>
        public const int EQUIP_STREN_DO = 15205;

        // ----- 装备成长四件套(自动循环 轮4 队列#4;pt_152 段内 15212/13/14/50/51/52/55/60/61;
        // 老端 EquipController.ts + EquipSmeltView/EquipWashView/EquipRefinementView/EquipStrenMasterView.ts) -----
        /// <summary>神兵淬炼(精炼)信息查询(发 "c" equip_type;回包 res:i, equip_type:c, refine:h, refine_high:h)。
        /// UI 挂 EquipView tab1"神兵淬炼",老端底层变量/事件全叫 Smelt——与 15255"神炼"是两套独立系统,勿混。</summary>
        public const int EQUIP_SMELT_INFO = 15250;
        /// <summary>神兵淬炼执行(发 "cc" equip_type,type[1单件/2一键(equip_type 传 0)];回包 res:i, res1:c, type:c,
        /// refine_info[u16×{equip_type:c, refine_high:h}])。</summary>
        public const int EQUIP_SMELT_DO = 15251;
        /// <summary>开启洗魄槽(发 "cc" equip_type, index+1[老端如此];回包 res:i, goods_id:l, index:c)。
        /// UI 挂 EquipView tab3"吞天洗魄"。</summary>
        public const int EQUIP_WASH_OPEN_SLOT = 15212;
        /// <summary>洗魄执行——**手写序列,非简单 fmt**(对标 EquipController.ts:59-71):c(equip_type) + h(锁定槽数量) +
        /// c[](锁定槽下标+1,变长) + c(ratio_plus);回包 res:i, goods_id:l, attr_list[u16×{index:c}]。</summary>
        public const int EQUIP_WASH_DO = 15213;
        /// <summary>洗魄免费次数查询(无参;回包 free_times:c,无 res 字段)。GAME_START 发一次。</summary>
        public const int EQUIP_WASH_FREE_TIMES = 15214;
        /// <summary>洗魄升段(发 "cc" equip_type, is_buy[0/1];回包 res:i, goods_id:l;新段位未打包进协议,
        /// 客户端拿不到新值,只能靠详情重拉间接得知)。</summary>
        public const int EQUIP_WASH_DIVISION = 15252;
        /// <summary>神屠九炼(神炼)执行(发 "l" goods_id 装备实例id,不是 equip_type;回包 code:i, goods_id:l,
        /// refine_lv:i)。UI 挂 EquipView tab4"神屠九炼"。无专用查询协议,展示读 15000/15001 GoodsDetailVo.RefinementLv。</summary>
        public const int EQUIP_REFINEMENT_DO = 15255;
        /// <summary>全身奖励激活(发 "c" type[1强化/3宝石,本轮只用1];回包 errcode:i, type:c, whole_lv:h)。
        /// 淬炉宗师(EquipStrenMasterView,type=1)与骸珀镶嵌大师(type=3,4b 另单)共用基建。</summary>
        public const int EQUIP_WHOLE_ACTIVE = 15260;
        /// <summary>全身奖励列表查询(无参;回包 list[u16×{type:c, whole_lv:h}])。</summary>
        public const int EQUIP_WHOLE_LIST = 15261;
        // 跳过(规格 §0 本轮不加常量不写代码,只在 Runbook 由主控记队列):
        // 15202 卸下装备:老端无发送入口(UI 已砍),跳过。
        // 15206/15207 进阶装备/进阶属性预览:老端零 UI 参照,跳过。
        // 15242/15243 唤魔信息(旧):pp_equip.erl 整段注释、pt_152 无 read/write,服务端 DEAD,跳过。
        // 15253 神装升阶重复号:协议 read/write 都在但 pp_equip.erl 无 handle 分支,服务端 DEAD,跳过。
        // 15217-15219 神装信息/升阶/升阶预览:独立合成窗(RedEnterView 神兵铸造页签),与本模块无关,归后续包。
        // 15220-15223/15262 共鸣套装:独立窗(EquipSuitBaseView),归后续包。
        // 15230-15233(铸灵/护灵)、15241/15244/15245(觉醒/唤魔技能):归后续包,本轮不加常量。

        // ----- 宝石(骸珀镶嵌,自动循环 轮4 下半/4b;pt_152 段内 15210/15211/15215/15216;
        // 老端 EquipController.ts + jewel/EquipJewelView.ts + jewel/EquipJewelCraveView.ts -----
        /// <summary>宝石雕刻信息查询(发 "c" equip_pos;回包 res:i, equip_pos:c, refine_lv:c, exp:i,
        /// attr_list[u16×{attr_id:c, attr_val:i}])。**refine_lv 是 1 字节**(服务端 pt_152.erl:281-302 item_to_bin_2
        /// 实证,勿按规格草稿字面 h 解读——字段名 refine_lv 勿与 15250 的 refine 混)。GAME_START 循环 equip_pos=1..10
        /// 预拉(对标 EquipController.ts:224)。</summary>
        public const int EQUIP_JEWEL_CRAVE_INFO = 15210;
        /// <summary>宝石雕刻执行(发 "cic" equip_pos, 材料type_id, one_key[0/1];回包 res:i, equip_pos:c, is_up:c,
        /// one_key:c)。成功自动重发 15210 刷新(对标老端 on15211)。</summary>
        public const int EQUIP_JEWEL_CRAVE_DO = 15211;
        /// <summary>宝石升级(镶嵌位上,发 "ccc" equip_pos, stone_pos, upgrade_type[0普通/1一键低级宝石/2直升丹];
        /// 回包 res:i, equip_pos:c, pos:c, type_id:i)。无 OpenLv 门。</summary>
        public const int EQUIP_JEWEL_STONE_UPGRADE = 15215;
        /// <summary>宝石合成(发 "ic" type_id, is_one_key[0/1];回包 res:i, type_id:i, is_one_key:c)。
        /// 成功且 is_one_key==1 时服务端语义要求客户端自循环续发(对标老端 on15216);老端 UI 首发入口已被砍
        /// (全仓库找不到手动首次发起调用点,只留自循环续发),Unity 同步只留 API <see cref="EquipJewelController.CombineStone"/>
        /// 供未来入口调用,本轮不建 UI 触发按钮。</summary>
        public const int EQUIP_JEWEL_STONE_COMBINE = 15216;
        /// <summary>子功能战力查询(轮21 PF 补漏批;发/回同号 "c" sub_mod → 回包 sub_mod:c, power:i)。
        /// 对标老端 EquipController.ts:713-716 On15254 → model.Fire(EquipEvent.SUBTYPE_POWER,scmd);
        /// 唯一真实调用点 jewel/EquipJewelView.ts:463-465 `GetPowerOnProto`(视图打开/刷新时发 sub_mod=1)。
        /// 服务端 pp_equip.erl:571-574 `get_equip_sub_mod_power/2` 目前**只认 sub_mod==1(?EQUIP_STONE_POWER,
        /// 宝石/骸珀镶嵌)**,其余取值恒回 power=0(def_goods.hrl:122)——本号事实上是"宝石镶嵌战力"专用号,
        /// 不是通用子系统战力查询。Unity 暂无 EquipJewelView 主战力展示位(仅有 CraveView 子窗),落
        /// EquipJewelModel 数据层 + 复用既有 EVT_EQUIP_JEWEL_UPDATE 事件,消费方 TODO。</summary>
        public const int EQUIP_JEWEL_SUB_MOD_POWER = 15254;

        // ----- 古宝/妖物(pt_133 段内 13320/13321,yu_server enchantment_guard soap;老端 MonsterController.ts/guBao) -----
        /// <summary>古宝全量状态(请求无参;回包 combat:l + soap_list[u16×{soap_id:h, debris_list[u16×{debris_id:h}]}])。</summary>
        public const int GUBAO_INFO = 13320;
        /// <summary>激活古宝碎片(发 "hh" soap_id,debris_id;回包 errcode:i, soap_id:h, debris_list[u16×{debris_id:h}], combat:l)。
        /// 主线 100811(ctype89)=soap 10001 幽瞳 2 碎片全激活(消耗 1105010011/12,刷图掉落)。</summary>
        public const int GUBAO_ACTIVE = 13321;

        // ----- 剑魄同修(142xx,yu_server pt_142.erl + pp_partner;老端 PartnerController.ts) -----
        /// <summary>同修单个信息(请求发 "i" companion_id;推送/回包:sum_attr[u16×{attr_id:c,attr_val:i}],
        /// companion_id:i, stage:h, star:h, is_active:c, blessing:i, train_num:i, attr[同上], combat:l, fight_id:i)。</summary>
        public const int PARTNER_INFO = 14201;
        /// <summary>同修全量列表(请求无参;回包:fight_id:i + sum_attr[] + companion_list[u16×{companion_id:i,
        /// stage:h, star:h, biog_list[u16×{lv:c}], is_active:c, is_fight:c, figure_id:i, blessing:i, train_num:i,
        /// attr[], combat:l}])。进游戏(EVT_GAME_START)请求。</summary>
        public const int PARTNER_LIST = 14202;
        /// <summary>激活同修(发 "i";回包 errcode:i, companion_id:i, combat:l;errcode==1 置激活)。</summary>
        public const int PARTNER_ACTIVE = 14204;
        /// <summary>培养同修(发 "i";回包 errcode:i, companion_id:i, stage:h, star:h, blessing:i;
        /// 主线 100190 要求 1阶2星;消耗服务端结算,背包变化走 15017/15018)。</summary>
        public const int PARTNER_TRAIN = 14205;

        // ----- 惊喜礼包(490xx,yu_server pt_490.erl) -----
        /// <summary>惊喜礼包信息。请求无参;回包见 pt_490 write(49000)。</summary>
        public const int SURPRISE_GIFT_INFO = 49000;
        /// <summary>抽奖。发 "i"(giftId);回包 Code:i, GiftId:i。</summary>
        public const int SURPRISE_GIFT_DRAW = 49001;
        /// <summary>翻牌。请求无参;回包 Code:i, TurnId:h, GiftId:h, UseFreeTimes:i。</summary>
        public const int SURPRISE_GIFT_TURN = 49002;
        /// <summary>购买礼包。发 "i"(giftId);回包 Code:i, GiftId:i。</summary>
        public const int SURPRISE_GIFT_BUY = 49003;
        /// <summary>刷新推送(S2C)。次数:i×3 + DayTaskList[h+{TaskId:c,State:c}×N]。</summary>
        public const int SURPRISE_GIFT_REFRESH = 49004;

        // ----- 收尾三件套(第20轮工单;详见 Docs/工单-收尾三件套.md) -----
        /// <summary>灵魄强化(发 "l" goods_id 已穿戴符文实例;回包 code:i, rune_point:i, goods_id:l;
        /// code==1 成功 → 消耗 rune_point 更新,随后再拉一次 16700 刷新)。解主线 101525(ctype50)。</summary>
        public const int RUNE_UPGRADE = 16702;
        /// <summary>神装合成(通用装备合成 COMPOSE_EQUIP type=2;对标 CompositeController.ts:107 WriteBegin(15020):
        /// 发 "i" rule_id + "h"+n×"l" regular_glist(固定材料) + "h"+m×"l" specify_glist(指定材料);
        /// 回包 code:i, compose_type:c, rule_id:i, goods_id:l;code==1 成功)。解主线 101725(ctype73)。</summary>
        public const int GOODS_COMPOSE = 15020;
        /// <summary>排位赛(竞技场)页面信息(请求无参;回包 rank:i, history_rank:i, reward_rank:i, combat:l, hp:i,
        /// num:h, num_refresh:i, honour:i, is_reward:c, pet_id:i, break_id_list[u16×{break_id:i}])。
        /// ⚠服务端计数断链(mod_jjc_cast.erl:87),挑战不推进主线 101465(ctype35)任务,待服务端修复。</summary>
        public const int JJC_INFO = 28001;
        /// <summary>排位赛随机对手(请求无参;回包 role_list[u16×{rank:i, role_id:l, combat:l, hp:i, pet_id:i,
        /// figure:RecFigure}])。</summary>
        public const int JJC_RIVALS = 28002;
        /// <summary>排位赛挑战(发 "ilic" selfRank, rivalId, rivalRank, challengeType=0;回包
        /// role_list[u16×{role_id:l, figure:RecFigure, before_rank:h, rank:h, combat:l}], result:c,
        /// reward_list[u16×{type:c, type_id:i, num:l}], break_reward_list:ObjectList)。</summary>
        public const int JJC_CHALLENGE = 28003;
        /// <summary>排位赛挑战次数完整快照(请求无参;回包 errcode:i32(32-bit wire 由 ReadU32 后 unchecked 转 int),
        /// left_num:u16, num_refresh:u32, can_buy_num:u16)。num_refresh 为服务器绝对时间原值，独立于 28001 的页面字段。</summary>
        public const int JJC_TIMES_INFO = 28004;
        /// <summary>被挑战记录完整快照(C2S 空包;S2C errcode:u32(落库时 unchecked 转 int),
        /// record_list:u16×{role_id:u64,picture:s,picture_ver:u32,name:s,career:u8,sex:u8,turn:u8,vip_lv:u8,
        /// lv:u16,combat_power:u64,result:u8,state:u8,rank_range:u32,time:u32})。</summary>
        public const int JJC_CHALLENGE_RECORDS = 28009;

        /// <summary>无尽之海主快照(C2S 严格空包；S2C pic:s,pic_ver:u32,reward_times:u8,total_reward_times:u8,
        /// rob_times:u8,total_rob_times:u8,auto_id:u64,status:u8,send_list:u16×完整航运项)。</summary>
        public const int BRIGHT_SEA_INFO = 18900;
        /// <summary>无尽之海巡航/掠夺记录快照(C2S 严格空包；S2C log_list:u16×完整日志及三个 ObjectList)。</summary>
        public const int BRIGHT_SEA_CRUISE_LOGS = 18901;
        /// <summary>无尽之海巡航船只页状态快照(C2S 严格空包；S2C shipping_id:u8,luckey_value:u16,
        /// reward_times:u8,total_reward_times:u8,up_times:u8,total_up_times:u8)。</summary>
        public const int BRIGHT_SEA_SHIP_INFO = 18902;
        /// <summary>无尽之海巡航结算详情(C2S auto_id:u64；S2C 掠夺者、奖励及掠夺奖励快照)。</summary>
        public const int BRIGHT_SEA_CRUISE_DETAIL = 18904;
        /// <summary>无尽之海跨服信息快照(C2S 严格空包；S2C 模式/世界等级及 enemy、un_satisfy 服务器列表)。</summary>
        public const int BRIGHT_SEA_SERVER_INFO = 18915;
        /// <summary>无尽之海协助绑元次数快照(C2S 严格空包；S2C daily_num:u16,max_bgold_num:u16)。</summary>
        public const int BRIGHT_SEA_ASSIST_BGOLD_INFO = 18916;
        /// <summary>无尽之海本人巡航状态快照(C2S 严格空包；S2C auto_id:u64,status:u8,reward_times:u8,total_reward_times:u8)。</summary>
        public const int BRIGHT_SEA_SHIP_STATUS = 18917;

        // ----- 组队(24xxx,yu_server pt_240.erl / pp_team.erl;老端 commonController/TeamController.ts,
        // 自动循环 轮8) -----
        // 范围裁决(按规格§0):桶1核心 25 个(可发起+有实质处理)+ 桶2推送 14 个,共 39 号在此声明常量。
        // 跳过(不加常量):24011(委任队长,UI 四层链路全断的僵尸协议)/24042(获取活动剩余次数,老端 handler
        // 函数体为空且从未真发)/proto240 里定义但 h5 全仓库零引用的 16 个 UNUSED 号(24016/22/24/25/26/32/39/
        // 41/43/44/45/46/50/54/56/58/59)/服务端 DEAD(24022/32/41/45/46,与上重叠)/区间内未分配号
        // (24001/19/27/28/29/31)。
        /// <summary>创建队伍。发 "ccihhi"(activity_id,subtype,scene_id,min_lv,max_lv,join_con_value 恒传0);
        /// 回包 Res:i,ErrCodeArgs:s(仅错误分支有意义,成功不弹本号,队伍数据靠随后的 24010 广播)。
        /// 预校验:<see cref="Shenxiao.Module.Core.Team.TeamModel.IsOpenTeam"/>(主线 101260 前禁组队)。</summary>
        public const int TEAM_CREATE = 24000;
        /// <summary>申请入队(已知 team_id,组队大厅列表场景)。发 "lcc"(team_id,activity_id,subtype);
        /// 回包 Res:i,ErrCodeArgs:s。</summary>
        public const int TEAM_APPLY_JOIN = 24002;
        /// <summary>S2C 推送:队长收到入队申请。回包 ServerId:h,PlayerId:l,Figure。老端行为:非屏蔽状态下点亮
        /// 申请红点 + 补拉 <see cref="TEAM_APPLY_LIST"/>(24047)。</summary>
        public const int TEAM_APPLY_PUSH = 24003;
        /// <summary>队长回应加入队伍请求。**手写自定义序**(非固定 fmt 字符串,照老端 TeamController.ts:452-463
        /// WriteFMT 实测):h(list.length) + 每项 c(res 0/1) h(server_id) l(player_id);一键清空传空数组
        /// (list.length=0)。回包仅 Res:i(无 error_code_args,与 24008 不同)。</summary>
        public const int TEAM_APPLY_RESPONSE = 24004;
        /// <summary>离开队伍(含队长解散,服务端按角色区分,客户端发送逻辑相同)。发:无参;回包 Res:i。
        /// 成功连锁(对标老端 Handler24005):清本地队伍信息 + 重拉 <see cref="TEAM_INFO"/>(24010)/
        /// <see cref="TEAM_HALL"/>(24012,用当前目标)+ 若在自动匹配中追加取消匹配(24048 state=0)。</summary>
        public const int TEAM_QUIT = 24005;
        /// <summary>邀请别人加入队伍(同服)。**手写自定义序**(照老端 TeamController.ts:464-477):
        /// c(activity_id) c(subtype) i(scene_id) h(min_lv) h(max_lv) h(invite_list.length) + 每项 l(role_id)。
        /// 回包仅 Res:i。分流:server_id 与自己不同服 → 走 <see cref="TEAM_INVITE_CROSS"/>(24057)。</summary>
        public const int TEAM_INVITE = 24006;
        /// <summary>S2C 推送:被邀请者收到邀请信息。回包 TeamId:l,Num:c,ActivityId:i,Subtype:c,SceneId:i,
        /// InviterId:l,Figure,InviteSceneId:i,InviteType:c(0普通/1退副本重邀)。同一 team_id/inviter_id 覆盖去重。</summary>
        public const int TEAM_INVITE_PUSH = 24007;
        /// <summary>被邀请者回应邀请请求。**手写自定义序**(照老端 TeamController.ts:488-497 实测;
        /// ⚠️与 24004 顺序相反——先 team_id 后 res):h(list.length) + 每项 l(team_id) c(agree 0/1)。
        /// 回包 Res:i,ErrCodeArgs:s;拒绝时本地额外调 DeleteBeInvited。</summary>
        public const int TEAM_INVITE_RESPONSE = 24008;
        /// <summary>踢出队伍。发 "l"(kick_id);回包 Res:i。</summary>
        public const int TEAM_KICK = 24009;
        /// <summary>队伍信息(团队全量快照,双重触发:推送 + 主动拉,发送本身无参数)。回包
        /// TeamId:l,ActivityId:c,Subtype:c,SceneId:i,PreNumFull:c,AutoMatching:c,MatchSt:i,MinLv:h,MaxLv:h,
        /// JoinConValue:i,AutoStart:c,JoinType:c,Members[h+{Id:l,TeamPosition:c,Figure,HelpType:c,SceneId:i,
        /// JoinTime:i,Power:l,Online:c,ServerId:h,ServerNum:h,JoinValue:i}](按 team_position 升序排序)。
        /// GAME_START 拉一次;断线清空。</summary>
        public const int TEAM_INFO = 24010;
        /// <summary>查看队伍招募面板(组队大厅列表,按目标筛选)。发 "cci"(activity_id,subtype,scene_id);
        /// 回包 ActivityId:c,Subtype:c,SceneId:i,Teams[h+{TeamId:l,Num:c,JoinConValue:i,Members[h+{Id:l,
        /// TeamPosition:c,Figure,HelpType:c,SceneId:i,Online:c,ServerId:h,ServerNum:h,JoinValue:i,Power:l}]}]
        /// (成员项字段序与 <see cref="TEAM_INFO"/> 不同:无 JoinTime,Power 挪到末尾;大厅列表按人数降序排序)。</summary>
        public const int TEAM_HALL = 24012;
        /// <summary>S2C 推送:广播场景中玩家的组队属性(驱动场景内头顶队长/队员标记)。回包
        /// Id:l,TeamId:l,Position:c(0/1/2)。落地到 <see cref="Shenxiao.Module.Core.Scene.Vo.RoleVo"/> 的
        /// TeamId/TeamPos 字段(场景渲染层本轮未接,数据先备好)。</summary>
        public const int TEAM_ROLE_SCENE_TAG_PUSH = 24013;
        /// <summary>S2C 推送:离开队员信息(id==自己表示被踢/解散/退出广播给自己)。回包 Id:l。
        /// id==自己 → 清空本地队伍;否则从成员列表按 id 移除。</summary>
        public const int TEAM_MEMBER_LEAVE_PUSH = 24014;
        /// <summary>S2C 推送:队长变更信息。回包 Id:l(新队长 id)。老端行为:该 id 成员置 team_position=1,
        /// 其余全部置 0(老端原样如此,会抹掉"假人3"区分,不纠正)。</summary>
        public const int TEAM_LEADER_CHANGE_PUSH = 24015;
        /// <summary>更改组队目标。发 "ccihhi"(activity_id,subtype,scene_id,min_lv,max_lv,join_con_value);
        /// 回包 Res:i,ActivityId:c,Subtype:c,SceneId:i,MinLv:h,MaxLv:h,JoinConValue:i。成功后本端自动
        /// 重新拉 <see cref="TEAM_HALL"/>(24012,用新 activity/subtype)。</summary>
        public const int TEAM_CHANGE_TARGET = 24017;
        /// <summary>更改申请自动进入类型。发 "c"(join_type:1不自动/2自动同意);回包 Res:i,JoinType:c。</summary>
        public const int TEAM_CHANGE_JOIN_TYPE = 24018;
        /// <summary>发起投票(仲裁)。发 "ic"(activity_id **32位**,subtype;同模块内位宽不统一,勿假设8位);
        /// 回包 ErrorCode:i,ErrCodeArgs:s,ActivityId:i,Subtype:c,SceneId:i,ArbitrateId:h。仅处理失败分支,
        /// 真正打开投票面板靠配套推送 <see cref="TEAM_VOTE_OPEN_PUSH"/>(24035)。服务端 CD 3000ms/1次
        /// (240 段唯一有 CD 的号)。</summary>
        public const int TEAM_VOTE_START = 24020;
        /// <summary>队员投票。发 "hc"(arbitrate_id,res 0反对/1赞同);回包 ErrorCode:i,ErrCodeArgs:s,Res:c。
        /// 仅处理失败分支。</summary>
        public const int TEAM_VOTE = 24021;
        /// <summary>匹配队伍(把自己塞进已有同类队伍/匹配池的信令,非拉列表)。发 "cc"(activity_id,subtype);
        /// 回包 Res:i,ActivityId:c,Subtype:c。成功无本地状态变更(老端该分支代码已注释掉),真正"匹配中"
        /// UI 状态由 <see cref="TEAM_AUTO_MATCH"/>(24048)驱动。</summary>
        public const int TEAM_MATCH_JOIN = 24023;
        /// <summary>S2C 推送:自身提示类通知(离线/踢出/满员/非队长等自身单播)。回包 Res:i。
        /// res==2400022 时老端在"当前无大界面打开"情况下自动弹 TeamView(本轮 TeamView 未移植,跳过该分支)。</summary>
        public const int TEAM_SELF_TIP_PUSH = 24030;
        /// <summary>助战开关。发 "ic"(dun_id,help_type 0/1);回包 ErrorCode:i,DunId:i,HelpType:c。</summary>
        public const int TEAM_HELP_TYPE = 24033;
        /// <summary>S2C 推送:广播助战状态(队友的 help_type 变化)。回包 Members[h+{RoleId:l,HelpType:c}]。</summary>
        public const int TEAM_HELP_TYPE_PUSH = 24034;
        /// <summary>S2C 推送:广播发起投票(真正打开 TeamVoteView 的入口,老端同时关闭 TeamMatchView/TeamView;
        /// 两窗口本轮均未移植,仅存数据)。回包 ActivityId:i,Subtype:c,SceneId:i,ArbitrateId:h,EndTime:i。</summary>
        public const int TEAM_VOTE_OPEN_PUSH = 24035;
        /// <summary>S2C 推送:广播队员投票(驱动已投票头像标记)。回包 RoleId:l,ArbitrateId:h,Res:c。</summary>
        public const int TEAM_VOTE_MEMBER_PUSH = 24036;
        /// <summary>S2C 推送:广播投票结果(常用于"投票未通过"等提示)。回包 ErrorCode:i,ErrCodeArgs:s。</summary>
        public const int TEAM_VOTE_RESULT_PUSH = 24037;
        /// <summary>S2C 推送:给其他队员的通用带参提示(邀请结果/费用错误/仲裁拒绝等,几乎全模块共用信道)。
        /// 回包 ErrorCode:i,ErrCodeArgs:s。</summary>
        public const int TEAM_TIP_PUSH = 24038;
        /// <summary>S2C 推送:取消仲裁(内部流程推送,无字段)。</summary>
        public const int TEAM_VOTE_CANCEL_PUSH = 24040;
        /// <summary>查询申请列表(仅队长能拉到有效数据,非队长服务端静默不回)。发:无参;回包
        /// Applicants[h+{ServerId:h,PlayerId:l,Figure,CombatPower:l,ServerNum:h}]。本地按
        /// <see cref="Shenxiao.Module.Core.Team.TeamModel.IsInShieldState"/>(10 分钟本地屏蔽表)过滤。</summary>
        public const int TEAM_APPLY_LIST = 24047;
        /// <summary>设置队伍自动匹配状态(驱动"匹配中"浮层的核心状态源)。发 "c"(state:0取消/1开始);
        /// 回包 Res:i,ErrCodeArgs:s,State:c,MatchSt:i,ActivityId:c,Subtype:c,RoleId:l。state==2(匹配成功)
        /// 老端无专门分支,落入"非1"统一按取消处理(对标保留,不额外精确化)。</summary>
        public const int TEAM_AUTO_MATCH = 24048;
        /// <summary>获取我的助战状态。发 "i"(dun_id);回包 DunId:i,State:c(与 24033 落地同一份数据)。</summary>
        public const int TEAM_HELP_STATE = 24049;
        /// <summary>S2C 推送:队员切换场景(粗粒度,仅场景号无坐标)。回包 RoleId:l,SceneId:i。</summary>
        public const int TEAM_MEMBER_SCENE_PUSH = 24051;
        /// <summary>S2C 推送:队员上下线状态变化。回包 RoleId:l,Online:c。</summary>
        public const int TEAM_MEMBER_ONLINE_PUSH = 24052;
        /// <summary>获取附近的玩家(邀请面板"附近玩家"tab 用)。发 "i"(scene_id);回包 SceneId:i,
        /// Users[h+{RoleId:l,Platform:s,ServNum:h,ServId:h,Figure}](整体替换,非增量)。</summary>
        public const int TEAM_NEARBY_PLAYERS = 24053;
        /// <summary>世界喊话(招募喊话)。发:无参;回包:无字段(空包即成功信号)。仅队长可发,客户端本地
        /// 5 秒冷却(<see cref="Shenxiao.Module.Core.Team.TeamModel.WORLD_SHOUT_COOL_TIME"/>)。</summary>
        public const int TEAM_WORLD_SHOUT = 24055;
        /// <summary>邀请别人加入队伍(带服务器 id,跨服邀请)。**手写自定义序**(照老端 TeamController.ts:478-487
        /// 实测):h(list.length) + 每项 h(server_id) l(role_id)。回包 Res:i——⚠️r8_server 实证该 write 子句
        /// 全仓库零调用,真实 ack 走 <see cref="TEAM_INVITE"/>(24006);本端仍防御性注册 recv,便于服务端未来改动。</summary>
        public const int TEAM_INVITE_CROSS = 24057;
        /// <summary>招募列表(副本专用,带次数信息)。发 "ci"(type:1推荐/2公会/3好友,dun_id);回包
        /// Type:c,DunId:i,List[h+{RoleId:l,Figure,Count:c,MaxCount:c,CombatPower:l}]。</summary>
        public const int TEAM_RECRUIT_LIST = 24060;
        /// <summary>队员招募列表(无队伍限定/在线,通用邀请面板用,无 count 字段区别于 24060)。发 "c"(type:2公会/3好友);
        /// 回包 Type:c,List[h+{RoleId:l,Figure,CombatPower:l}]。</summary>
        public const int TEAM_RECRUIT_MEMBER_LIST = 24061;
        /// <summary>催促开启活动(副本人数不足时"催促队友"按钮)。发:无参;**老端未注册任何 recv handler**,
        /// 纯 fire-and-forget(服务端只是给队友群发聊天提示),本端也不 RegisterProtocal。</summary>
        public const int TEAM_URGE = 24062;
        /// <summary>一键同意入队(sentientAct 专属 UI 共用协议)。发:无参;回包 ErrorCode:i。无论成败都补拉
        /// <see cref="TEAM_APPLY_LIST"/>(24047)。</summary>
        public const int TEAM_APPLY_ALL = 24063;

        // ----- 日常中心(157xx + 41900系 + 61801,自动循环 轮10;yu_server src/pt/pt_157.erl·pt_419.erl·
        //        pt_618.erl 权威字段序,与 yu_client commonController/DailyController.ts + commonModel/
        //        DailyModel.ts 交叉核对;冲突处按服务端 write 为准。⚠轮10交叉验收 blocker 订正:15700"跨系统
        //        共享错误码壳不重复注册"的裁决前提不成立——全仓 grep 无任何人注册它,已改为在 DailyController
        //        补注册(老端就是 DailyController 唯一注册方,GapMap 风险#5 结论仍成立:不存在双注册冲突)。
        //        跳过:15702/15704/15707/15708(号段空洞,双端均无 read/write/handle)、
        //        15713(read/write 骨架在,handle 已注释,DEAD,老端也无 UI 接线)。⚠勘误(r10 侦察定案):
        //        签到/补签实际协议是 41704/41705,归 WelfareController,不在本簇——本文件不加那两个号。) -----

        /// <summary>15700 通用错误码(纯推送,pt_157.erl:56-62 write(15700,[Errcode:32])):15701/15705/15710/
        /// 15715/15716/15717/15719/15720 等失败分支的服务端 guard 全部经此号回包,不是独立请求。</summary>
        public const int DAILY_ERROR = 15700;
        /// <summary>查询活跃度次数(每日任务/限时活动共用一张读表,按 act_type 分槽)。发 "c"(act_type:1非限时/2限时);
        /// 回包(pt_157.erl:64-81 write(15701,...)):ActType:8, Time:64(离线挂机时间搭车带出),
        /// AcList[u16×{Module:32,ModuleSub:32,AcSub:32,Num:32,MaxNum:32,Live:32,MaxLive:32,CanGetLive:32,State:8}]。
        /// 老端 GAME_START 对两种 act_type 各发一次,DailyTaskView/DailyLimitActivityView 开页各自再发一次。</summary>
        public const int DAILY_ACTIVITY_LIST = 15701;
        /// <summary>查询活跃度奖励(每日任务底栏宝箱进度条)。发:无参;回包(pt_157.erl:83-100 write(15703,...)):
        /// Live:32, LiveMax:32, RewardList[u16×{Id:32,State:8}](对标老端按 Id 升序展示)。
        /// 同号还被服务端 <c>lib_liveness:refresh_live_reward/1</c> 在活跃度变化时主动推送复用。</summary>
        public const int DAILY_LIVENESS_REWARD = 15703;
        /// <summary>领取活跃度宝箱奖励。发 "i"(id);回包(pt_157.erl:102-110 write(15705,...)):Errcode:32, Id:32。
        /// guard:已领(1570002)/活跃度不够(1570001)/背包不足(兜底 err150_no_cell)。成功→toast+重拉
        /// <see cref="DAILY_LIVENESS_REWARD"/>+GetBoxRewardListById 奖励预览(周卡翻倍假条目未接线,TODO)。</summary>
        public const int DAILY_LIVENESS_REWARD_GET = 15705;
        /// <summary>活动状态变更主动推送(S2C 专用)。回包(pt_157.erl:112-124 write(15706,...)):
        /// Module:32, ModuleSub:32, ActType:8, Status:8。原地改 daily_data[ActType] 里对应条目 state
        /// (对标老端 UpdateDailyData;650@1 CSPVP 联动分支老端已注释,不抄)。</summary>
        public const int DAILY_ACTIVITY_STATE_PUSH = 15706;
        /// <summary>查询玩家活跃度形象信息。发:无参;回包(pt_157.erl:126-138 write(15709,...)):
        /// Lv:32, Liveness:32, Id:32, Display:8。挂在每日任务底栏"活跃度形象"入口(DailyLivenessMsgView,
        /// 现仅 Bind 无具体类,r10_unity 结论,UI 未接壳)。</summary>
        public const int DAILY_LIVENESS_INFO = 15709;
        /// <summary>活跃度升级。发:无参;回包(pt_157.erl:140-150 write(15710,...)):Errcode:32, Lv:32, Liveness:32。
        /// guard:活跃度不够(1570006)/已满级(1570007)/配置错(1570005)。成功→toast「升级成功」+重拉
        /// <see cref="DAILY_LIVENESS_INFO"/>+UseNewImage(按新等级自动挑一个新解锁形象换上)。</summary>
        public const int DAILY_LIVENESS_LEVEL_UP = 15710;
        /// <summary>更换活跃度形象。发 "i"(figure id;⚠仅被 UseNewImage 自动触发,老端手动选择 UI 按钮代码已整段
        /// 注释,无玩家可操作入口)。回包(pt_157.erl:152-160 write(15711,...)):Errcode:32, Id:32。⚠r10_server
        /// 实证:服务端 <c>pp_activitycalen.erl</c> 对应 handle 子句已注释(DEAD),本端仍防御性注册 recv
        /// (同 <see cref="CHAT_BANNED_NOTICE"/> 先例),现状发送后恒收不到回包,无害。</summary>
        public const int DAILY_LIVENESS_CHANGE_FIGURE = 15711;
        /// <summary>广播他人活跃度形象变更(S2C 专用推送)。回包(pt_157.erl:162-170 write(15712,...)):
        /// RoleId:64, FigureId:32。⚠r10_server 实证:触发主体已从"活跃度换形象"整体迁移给龙珠模块
        /// (<c>lib_dragon_ball.erl</c>),协议本身仍活。场景内角色形象同步消费方未接线(仅转发事件,TODO)。</summary>
        public const int DAILY_LIVENESS_FIGURE_PUSH = 15712;
        /// <summary>离线挂机时间更新推送(S2C 专用)。回包(pt_157.erl:182-188 write(15714,...)):Time:64。
        /// 刷每日任务底栏"离线挂机时间"文案(UI 未接壳,先落 DailyModel.OutlineTime)。</summary>
        public const int DAILY_ONHOOK_TIME_PUSH = 15714;
        /// <summary>查询活跃度找回信息(50 级开)。发:无参;回包(pt_157.erl:190-203 write(15715,...)):
        /// ResAct[u16×{ActId:32,ActSub:16,Lefttimes:16,BackTimes:16}]。guard:开启等级不够(1570012)。
        /// ⚠老端 <c>LivenessCanFind()</c> 已硬编码 return false=功能下线;按规格协议接收保留但**不建 UI**
        /// (GAME_START 时等级达标才发,照老端)。</summary>
        public const int DAILY_LIVENESS_FIND_INFO = 15715;
        /// <summary>活跃度找回(消耗绑钻换活跃度)。发 "ihh"(act_id, act_sub, times;⚠老端 h5/src 全仓库无发送
        /// 调用点,功能已随 15715 一并下线,本端仅按协议声明防御性注册 recv,不提供 UI)。
        /// 回包(pt_157.erl:205-215 write(15716,...)):ActId:32, ActSub:16, Lefttimes:16。</summary>
        public const int DAILY_LIVENESS_FIND = 15716;
        /// <summary>领取活跃度(每日任务单条 item 完成后领取)。发 "ih"(module, module_sub)。
        /// ⚠r10_server 静态证据:read 只解 2 字段但 handle pattern 要 3 字段,疑似历史 arity 不匹配 bug、
        /// 大概率不可达(需网关派发代码交叉确认,本仓无法 100% 定论)——按规格 §0 与老端行为原样实现:
        /// 回包(pt_157.erl:217-227 write(15717,...)):ActId:32, ActSub:16, AddLive:32(字段名历史遗留,
        /// 语义已是 LeftTimes,不影响编码)。成功→toast「领取成功」+重拉 <see cref="DAILY_ACTIVITY_LIST"/>
        /// (UnLimit)+<see cref="DAILY_LIVENESS_REWARD"/> 联动。</summary>
        public const int DAILY_TASK_LIVENESS_CLAIM = 15717;
        /// <summary>活动报名情况查询/推送(限时活动预约状态表)。发:无参;回包(pt_157.erl:229-242 write(15718,...)):
        /// ActList[u16×{Module:32,ModuleSub:32,AcSub:32,Status:8,Join:8}](过滤 module!=500,驱动预约红点计数)。
        /// 同号还被服务端每日刷新/公会战状态变化主动推送复用。DailyView 打开时统一拉一次。</summary>
        public const int DAILY_SIGNUP_LIST = 15718;
        /// <summary>活动报名/预约。发 "iii"(module, module_sub, ac_sub);回包(pt_157.erl:244-260 write(15719,...)):
        /// Code:32, Module:32, ModuleSub:32, AcSub:32, Status:8, Join:8。guard 链见 r10_server(活动开启中不能报名/
        /// 公会战资格/未入会/未到今日开启/等级或类型不符/已报名等 6 支)。成功且 Status!=2→弹预约成功小窗
        /// (DailyReservationView,现仅 Bind 无具体类,toast 兜底)。⚠老端夹带微信小游戏订阅检查,Unity 非微信
        /// 渠道整体跳过,不移植。</summary>
        public const int DAILY_SIGNUP = 15719;
        /// <summary>领取报名奖励(活动结束后)。发 "iii"(module, module_sub, ac_sub);
        /// 回包(pt_157.erl:262-274 write(15720,...)):Code:32, Module:32, ModuleSub:32, AcSub:32。
        /// guard:未到今日开启(1570010)/状态不符(?FAIL)。成功→按 config_ac.sign_up_reward 展示奖励(降级 toast)+
        /// 该条目预约状态置 2(已领)+预约红点 -1。</summary>
        public const int DAILY_SIGNUP_REWARD = 15720;
        /// <summary>限时活动开启提醒(主动推送+GAME_START/升级时主动查一次)。发:无参;
        /// 回包(pt_157.erl:276-291 write(15721,...)):IsRemind:8, ActList[u16×{Module:32,ModuleSub:32,AcSub:32,
        /// State:8,Time:32,SignState:8}]。老端弹窗簇 DailyActTipView 逻辑最绕(未弹→判断是否弹新窗;已弹→原地
        /// 刷新/跳tab/关闭),现仅 Bind 无具体类(r10_unity 结论)——按规格"有壳接壳,无壳 toast 降级+TODO"实现。</summary>
        public const int DAILY_ACT_REMIND = 15721;
        /// <summary>设置"今日不再提醒"开关(DailyActTipView 弹窗里的复选框)。发 "c"(open:0/1)。
        /// ⚠<c>pt_157.erl</c> 全文无 write(15722,...) 子句——服务端仅写内存 map,纯 fire-and-forget,
        /// 无需(也无法)注册 recv。</summary>
        public const int DAILY_ACT_REMIND_SET = 15722;

        /// <summary>资源找回界面信息。发:无参;回包(pt_419.erl:22-37 write(41900,...)):
        /// Errcode:32, ResAct[u16×{ActId:32,ActSub:16,Lefttimes:16,LefttimesVip:16,RewardLv:32}]。
        /// 触发点:GAME_START/DailyResFindView 开页/41903 回错 4190001 兜底重拉。同号亦被凌晨4点刷新主动推送复用。</summary>
        public const int DAILY_RES_FIND_INFO = 41900;
        /// <summary>资源找回(单条,可选额外次数)。发 "ihchh"(act_id, act_sub, type:1绑钻/2金币, times:正常次数,
        /// times_others:额外/vip次数);回包(pt_419.erl 对应 write(41903,...)):Errcode:32, Type:8, ActId:32,
        /// ActSub:16, Lefttimes:16, LefttimesVip:16, RewardLv:32。guard 链见 r10_server(公会战资格/等级/次数为0/
        /// 类型非法/配置缺失(4190003)/次数不足(4190001/4190002)/背包满(1500011))。成功→toast「找回成功」+
        /// 只更新该条目 lefttimes/lefttimes_vip(对标 UpdateFindData)。UI 简化:滑杆简化为"全额找回"一键。</summary>
        public const int DAILY_RES_FIND = 41903;
        /// <summary>一键找回。发 "c"(type);回包(pt_419.erl 对应 write(41904,...)):Errcode:32, Type:8,
        /// ActList[u16×{ActId:32,ActSub:16,Lefttimes:16,LefttimesVip:16,RewardLv:32}](覆盖式整表刷新,对标
        /// SetAllResFindData)。成功后主动重拉一次 <see cref="DAILY_RES_FIND_INFO"/> 兜底(对标老端)。</summary>
        public const int DAILY_RES_FIND_ONEKEY = 41904;

        /// <summary>我要变强(100级开)状态列表查询。发:无参;回包(pt_618.erl:12-25 write(61801,...)):
        /// StateList[u16×{Id:32,State:8,Time:64}]。⚠等级不足时服务端**静默不回包**(无 errcode,r10_server
        /// 实证,与 157/419 家族"总有个错误码回包"风格不同)。该页本身不再发起任何"变强"动作协议,
        /// 只是状态汇总+跳转(真正操作分散在各外部系统,Unity 端 jump_id→具体系统映射表未移植,TODO log)。</summary>
        public const int DAILY_STRONGER_LIST = 61801;

        // ----- 商店(15301-15307 + 64000-64003,自动循环 轮11;yu_server src/pt/pt_153.erl·pt_640.erl 权威字段序,
        //        与 yu_client commonController/ShopController.ts:448-458 实注册清单 + commonModel/ShopModel.ts
        //        交叉核对;冲突处按服务端 write 为准。边界勘误:LimitLevelShop(模块612,61200-03)是已完工独立
        //        系统,协议号/数据模型不共享,勿混。
        //        跳过:15303(双端定义齐全但老端 RegisterProtocal 从未注册、全仓零 Fire 调用,彻底死号,
        //        被 15304 取代——r11_server §存活判定证实 15303 本身"可达"[pp_shop.erl 有 handle 子句],
        //        但客户端从未发送过,故本端不注册/不提供发送 API)。 -----

        /// <summary>商店列表(按 shop_type 查询)。发 "c"(type:u8,ShopType 枚举 1-18)。
        /// 回包(pt_153.erl write(15301,...)):Type:8, GoodsList[u16×{KeyId:32, SubtypeList:s(先去 "%["/"%]" 包裹
        /// 再按逗号切成 series_list), Rank:32, GoodsId:32, Num:32, MoneyType:32, Price:32, Discount:16,
        /// QuotaType:8, QuotaNum:16(真实限购上限), "SoldOut":16(⚠字段名具误导性,真实语义=已购次数 UsedTime,
        /// 非售罄布尔), Condition:s(Erlang term,如 [{lv,120}]), TriggerTaskId:32, Bind:8}。
        /// guard:Type 不在 18 种合法值内 → 服务端静默 skip 不回包(r11_server §Guard)。
        /// ⚠type==TopVipShop(10)老端整包劫持转发给独立 TopVipModel(不进主 ShopModel 表)——Unity 现状
        /// TopVip 模块无对应接收方(无 SetSupremeVipShopGoodsList 等价物),本端落 ShopModel 专槽
        /// (TopVipShopGoodsList)+TODO,不双注册/转发 45102(该号属 TopVipController 自己的协议,与本簇无关)。</summary>
        public const int SHOP_GOODS_LIST = 15301;
        /// <summary>购买商品(按 key_id,全商城通用购买入口)。发 "ii"(key_id, num)。
        /// 回包(pt_153.erl write(15302,...)):Result:32, KeyId:32, Num:32。成功(Result==1)→ sold_out(已购次数)
        /// 累加 num;quota_type==3(终生限购)额外整表重排。发货/扣钱走既有 pt_150(15017/15018/15008),
        /// 本协议只处理结果码,不重复实现入库/扣币(r11_server §购买链路副作用)。</summary>
        public const int SHOP_BUY = 15302;
        /// <summary>快速购买(按 goods_id,专供速购弹窗 QuickBuyView,与 15301/15302 体系平行)。
        /// 发 "iic"(goods_id, num, buy_type:1钻石/2绑钻)。回包(pt_153.erl write(15304,...)):
        /// Res:32, GoodsId:32, Num:32, BuyType:8。UI 未接壳(QuickBuyView 未移植),仅留发送/接收 API。</summary>
        public const int SHOP_QUICK_BUY = 15304;
        /// <summary>神秘/神纹商店主页查询。发 "h"(type:u16,MysteryShopType:1神秘/2神纹)。
        /// 回包(pt_153.erl write(15305,...)):Type:16, RefreshTime:32, HitNum:16,
        /// GoodList[u16×{CfgId:16, Discount:8, Price:32, "BuyType":8(⚠字段名具误导性,真实语义=购买状态
        /// 1未买/2已买,非货币类型), BuyNum:8}]。guard:Type 不在 ?SHOP_TYPE_CAREER(仅2种)内 → 静默 skip。</summary>
        public const int SHOP_MYSTERY_LIST = 15305;
        /// <summary>手动刷新神秘/神纹商店。发 "h"(type)。回包(pt_153.erl write(15306,...)):Errcode:32。
        /// errcode==1 时服务端会自动补推 15305,本端不重复重拉(对标老端协议注释)。</summary>
        public const int SHOP_MYSTERY_REFRESH = 15306;
        /// <summary>购买神秘/神纹商店商品。发 "hhi"(type, cfg_id, price——折扣价客户端算好回传做防篡改校验)。
        /// 回包(pt_153.erl write(15307,...)):Errcode:32, Type:16, CfgId:16。
        /// ⚠r11_server 证实服务端失败分支实参错位:成功给 [Errcode,Type,CfgId],失败给 [Errcode,Id,0]
        /// (Id 顶替 Type 位、CfgId 位恒 0)——但老端 Handler15307 失败分支只调 Util.ErrorCodeShow(errcode),
        /// 从不读第2/3字段,故该错位不影响实际消费;本端同样只读 errcode 做提示,第2/3字段照位宽消耗但不作为
        /// Type/CfgId 语义使用(按老端行为原样实现,注释存档,不强行"修正"一个从未被读取的错位字段)。</summary>
        public const int SHOP_MYSTERY_BUY = 15307;
        /// <summary>抢购(限购)商城列表推送。send:null(协议表标注服务端主动推),但老端仍在 GAME_START/
        /// 每日4点/开抢购tab 时主动裸发一个无参帧拉取,照抄。发:无参。
        /// 回包(pt_640.erl write(64000,...)):IdList[u16×{Id:32, GoodId:32, DefaultNum:32, PriceType:8,
        /// OldPrice:32, NewPrice:32, TotalLimitNum:32, LeftLimitNum:32, DailyLimitNum:32, BuyNum:32}]。
        /// guard:进程字典节流,距上次响应 &lt;200 秒的重复请求静默 skip(r11_server §Guard)。
        /// ⚠left_time 字段协议表无此项——是客户端收到后自算"下一个游戏日0点"接上去的本地展示字段,
        /// 不是服务端下发,倒计时纯前端算(须用服务器墙钟 SERVER_ZONE_HOURS=8,轮10 血训,勿裸 UTC)。</summary>
        public const int SHOP_VIE_LIST = 64000;
        /// <summary>购买抢购商品。发 "ii"(id, num——购买次数固定传1)。
        /// 回包(pt_640.erl write(64001,...)):Errcode:32, Id:32, BuyNum:32(⚠=本次购买数量回显,服务端字段名
        /// SelfNum=Num,非累计已购数;失败路径恒 0。老端 Handler64001 同款直接当 buy_num 赋值=忠实复刻,
        /// 累计数以下一次 64000 全量为准), LeftLimitNum:32
        /// (全服剩余限购)。⚠双编码体系:errcode 正常业务分支=0-7 自定义提示码(老端专用文案表
        /// 0失败/1成功/2已下架/3金额不足/4达到限购/5售罄/6剩余不足/7未上架),守卫失败分支=全局
        /// ERRCODE 大数值(如 err640_goods_not_on_sale=6400000);按量级分流:≥100000 走显码降级 toast,
        /// 0-7 走老端文案。成功后原地 patch 本地 vie_info 对应条目(不重拉整张 64000 列表)。</summary>
        public const int SHOP_VIE_BUY = 64001;
        /// <summary>抢购商品库存变化广播(纯推送,S2C only)。回包(pt_640.erl write(64002,...)):
        /// ChangeList[u16×{Id:32, LeftLimitNum:32}]。逐条 patch 本地 vie_info 对应条目 left_limit_num。</summary>
        public const int SHOP_VIE_UPDATE = 64002;
        /// <summary>抢购商品下架广播(纯推送,S2C only)。回包(pt_640.erl write(64003,...)):
        /// DelList[u16×{Id:32}]。⚠老端 vinfo.id_list.slice(i,1) 是 Array.slice 误当 splice 的假删除 bug
        /// (slice 不改原数组,老端这条广播实际从未真删过);本端按显然意图实现为真删,注释订正存档
        /// (同轮10 rule10 先例:64003 真删)。</summary>
        public const int SHOP_VIE_DELETE = 64003;

        // ----- 排行榜(22100-22105,自动循环 轮12 #12,纯数据层轮;yu_server src/pt/pt_221.erl 权威字段序,
        //        与 yu_client commonController/RankController.ts:88-124 实注册清单交叉核对;冲突处按服务端 write 为准。
        //        存活裁决(r12_server §存活判定,DEAD占比4/6全家族最高):22100 从建库起从未被调用(孤儿壳,非"曾用现废");
        //        22102(公会榜)/22103(点赞信息)/22104(膜拜)handle 整段被注释(pp_common_rank.erl:95 注释块标题
        //        "公会排行榜和膜拜被剥离出排行榜！！！"自证已废),对应 write 侧代码虽在但唯一调用入口不可达——
        //        彻底不可达且无替代迁移(22102 底层数据被圣域 pt_283/28302 接管,22103/22104 纯功能下线)。
        //        跳过(§0/规格纪律 5,严禁实现发送与业务):22102/22103/22104。
        //        22105("我要变强"/末位信息):服务端活(pp_common_rank.erl:133-135 恒定回包)但老端 RankController.ts
        //        从未 RegisterProtocal(22105,...),全仓零引用——老端行为优先,跳过不移植(r12_oldrank §RegisterProtocal
        //        实注册清单/r12_server §结论1)。 -----

        /// <summary>排行榜通用错误码壳。回包(pt_221.erl write(22100,[Errcode])):Errcode:32。
        /// **孤儿协议**:r12_server 证实全仓库 `pt_221:write(22100` 零调用点(从建库起就没接入,非"曾用现废");
        /// 老端 RankController.ts 仍 RegisterProtocal(22100,On22100) 只做 Util.ErrorCodeShow——本端照抄注册防御 recv
        /// (显码 toast),避免真出现时无 handler 报"unhandled proto"噪音,注释存档"服务端从未发"。</summary>
        public const int RANK_ERROR = 22100;
        /// <summary>查询个人排行榜(14 种 rank_type,战力/等级/成就/竞技/坐骑/飞骑/翅膀/精灵×2/圣器/神兵/装备/爬塔/挂机)。
        /// 发 "iii"(type, start, len;均 u32)。回包(pt_221.erl write(22101,...)):
        /// RankType:32, Start:32, Len:32, RoleRank:32, <b>SelVal:64</b>(⚠位宽陷阱!22102 同名字段是 32位,
        /// 别按 22102 的宽度表照抄), SelSecVal:32, Sum:32, RankList[u16×{PlayerId:64, PraiseNum:32,
        /// Figure(变长,复用 <see cref="Shenxiao.Common.Proto.FigureProto"/>,与 10+ 处协议共用同一份"玩家外观快照"
        /// schema,非本协议专属), SelCombat:64, FirstValue:64, SecondValue:32, ThirdValue:32, Rank:32}]。
        /// guard(r12_server §Guard lib_common_rank_mod.erl:1187):Start≤0 或 Len≤0 服务端**静默 skip**(不回任何包,
        /// 连 22100 都不发)——本端发送侧本地拦截,不发废包。RankType 无白名单校验,未知值落服务端 combat_power
        /// 兜底语义,仍正常回包(不报错)。
        /// ⚠**Sum 字段更正(轮12 blocker 修复,原注释仅提越界分支是误记)**:lib_common_rank_mod.erl **正常分支
        /// (:1220)与越界分支(:1190)都**把 Sum 字段位置填成客户端请求的 Len,不是 :1183 算出的真实
        /// Sum=length(RankList)——wire sum 恒为请求 len 的回声,不可用于判断"是否越界"或"是否还有下一页"
        /// (旧的"只增不减/越界终止"防御建立在误读上,已废弃,<see cref="Rank.RankModel.ApplySum"/> 现仅存档展示)。
        /// 分页续拉改为 config 驱动(对标老端 RankModel.ts:128-160 用 config_ranking.rank_max 预排页数,与
        /// wire sum 无关):续拉条件是 received&lt;RankConfigs.GetByType(type).RankMax,落在
        /// <see cref="Rank.RankModel.RankTypeData.ConfiguredMax"/>。真实数据不足 Len 条时服务端用全 0
        /// (PlayerId=0)占位项凑满整页,与老端一致照样入库(渲染"虚位以待"留 UI 尾包)。
        /// 分页节流:老端 20 条/帧(oneMax)由 <c>reqFun()</c> 按帧续拉;本端把"帧"替换成"收到响应后立即续发下一页"
        /// (无 Update 依赖,轮1"Update 驱动行为需走非 Update 通道"教训——续拉逻辑落在 On22101 handler 内,可被
        /// CliVerify 反射直接喂包驱动,无需真实等待/tick)。自身排名(RoleRank/SelVal/SelSecVal)随每包自带,
        /// 无需独立协议查询。</summary>
        public const int RANK_QUERY = 22101;

        // ----- Boss 家族一期·本服核心(自动循环 轮15a/15a修复轮;46000-46046=pt_460,20025-26=采集,20201-205=免战)。
        //        范围铁律:47000-47035/47101-47117/61900-61902 跨服族全部不实现(留15b)。
        //        存活裁决(逐号与 yu_server src/boss/{pp_boss,lib_boss,lib_boss_mod,mod_boss}.erl 直接核对调用点,
        //        不采信侦察子报告的推断结论——过程中修正了子报告 3 处误判,详见各号注释):
        //        46037/46038/46039/46046 虽号段在 46000-46046 内,但 pp_boss.erl 对应 handle 子句**无条件**转发
        //        mod_great_demon_local:*(跨服秘境大妖,不看 boss_type 取值)——判定为跨服族复用本号段的壳,
        //        本轮不实现(与47xxx同等对待)。46017/46018/46020/46021/46023/46032 服务端 handle 仍在但对应
        //        C2S 老端已弃用(zero SCMD_REQUEST 调用)且均非自主推送(仅同步应答自己的请求),我方不发起
        //        请求则永不可达,不实现;46030 write 调用点已被注释(lib_boss.erl:802 `%`开头),真死,不注册。
        //        **修复轮订正**:46013/46031 初审误判为"非自主推送不可达"——直接核实 mod_boss.erl:1795-1798
        //        (46013 神庙怪刷新 send_to_scene)、lib_boss.erl:1833-1852(46031 秘境宝箱归属 send_to_scene/
        //        send_to_uid)均为服务端**无条件主动推送**,且落点场景(TEMPLE=5/FAIRYLAND=9)本轮 46003 EnterBoss
        //        即可到达——照 46006 先例登记防御 recv(数据只落日志,不建 UI)。46026-46029/46033(节日boss场景
        //        内推送)+46040(血条百分比,请求驱动)同样登记防御 recv/数据层——46003 Feast(11)进场即可触达
        //        前者,46040 消费方留战斗HUD轮 TODO。 -----

        /// <summary>查采集怪当前采集对象(20025/26 无 ClientProtocol.json 定义,手工 ReadFmt,对标老端
        /// BossController.ts:648-677)。S2C:h(count)+循环 l(role_id)。C2S(BossController.ts:522,
        /// `BROADCAST_COLLECT_RESULT` flag==13 且 `IsHolyBossScene()` 时触发):"ii"(monster_ins_id, monster_type_id)。
        /// 老端消费方是千幻蜃楼(跨服 holy)场景内打断判定——Unity 无场景采集钩子(BossSceneManager 等价物未接),
        /// 本轮只接 recv/send 数据层,消费方以事件形式暴露,TODO 场景钩子。</summary>
        public const int BOSS_COLLECT_QUERY = 20025;
        /// <summary>玩家采集被打断通知(S2C 单 l:role_id)。同上,场景消费钩子 TODO。</summary>
        public const int BOSS_COLLECT_INTERRUPT = 20026;

        /// <summary>免战保护信息查询(C2S 空包;S2C protect_list[{scene_type,protect_time,use_count}])。</summary>
        public const int WAR_FREE_INFO = 20201;
        /// <summary>使用免战保护(C2S "i" scene_type;S2C error_code,scene_type,protect_time,use_count)。</summary>
        public const int WAR_FREE_USE = 20202;
        /// <summary>免战保护结束时间查询(进场景请求,C2S 空包;S2C end_time,按服务器时间戳算剩余)。</summary>
        public const int WAR_FREE_END_TIME = 20203;
        /// <summary>免战保护时间更新推送(S2C scene_type,protect_time,use_count,纯推送)。</summary>
        public const int WAR_FREE_UPDATE = 20204;
        /// <summary>结束免战保护(C2S "i" scene_type;S2C error_code,scene_type)。</summary>
        public const int WAR_FREE_END = 20205;

        /// <summary>本服 Boss 列表/状态查询(多路复用,靠 boss_type 分派)。C2S "c" boss_type。S2C:BossType:8,
        /// AllCount:8, Count:8, Tired:16, AllTired:16, Vit:16, LastVitTime:32, CollectTimes:8, AllCollectTimes:8,
        /// BossInfo[u16 计数]{BossId:32,Num:8,RebornTime:32,IsRemind:8,AutoRemind:8}。</summary>
        public const int BOSS_LIST = 46000;
        /// <summary>击杀日志查询(pp_boss.erl:103 handle 真实存在,老端 46001✅注册但 SCMD_REQUEST 从未 Fire——
        /// 本轮按 wire 权威补齐发送侧,CliVerify 断言 100 条硬顶`?BOSS_LOG_LEN`)。C2S "ci" boss_type,boss_id。
        /// S2C KillLog[u16计数]{Time:32,RoleId:64,Name:s}。</summary>
        public const int BOSS_KILL_LOG = 46001;
        /// <summary>全局掉落日志查询(C2S 空包)。S2C DropLog[u16计数]{Time:32,RoleId:64,Name:s,BossType:8,
        /// BossId:32,GoodsId:32,Num:32,Rating:32,EquipExtraAttr[u16计数]{Color:8,TypeId:8,AttrId:16,AttrVal:32,
        /// PlusInterval:8,PlusUnit:32},IsTop:8}。</summary>
        public const int BOSS_DROP_LOG = 46002;
        /// <summary>进入 Boss 场景(C2S "ci" boss_type,boss_id)。S2C Code:32(同号回声,失败即错误码)。</summary>
        public const int BOSS_ENTER = 46003;
        /// <summary>离开 Boss 场景(C2S "c" boss_type)。S2C Code:32。</summary>
        public const int BOSS_LEAVE = 46004;
        /// <summary>蛮荒禁地/跨服大妖怒气值(纯服务端推送,老端从未 SCMD_REQUEST 主动请求,本端同样只 recv 不
        /// 提供发送方法)。S2C Anger:16,MaxAnger:16。</summary>
        public const int BOSS_ANGER = 46005;
        /// <summary>蛮荒禁地退出倒计时(Type=1雷神之怒预警30s/2踢出倒计时10s,timer 驱动纯推送)。老端
        /// zero-ref(遗弃号)但 pp_boss.erl:233/mod_boss.erl 多处仍在推(r15_server §存活互证:服务端还活着在推)——
        /// 登记防御 recv,只落地不建 UI。S2C Type:8,TickoutTime:8。</summary>
        public const int BOSS_ANGER_TIME = 46006;
        /// <summary>关注/取关操作(C2S "cicc" boss_type,boss_id,remind,auto_state)。S2C Code:32,BossType:8,
        /// BossId:32,Remind:8,IsAuto:8。</summary>
        public const int BOSS_REMIND = 46007;
        /// <summary>Boss 重生提醒单播(仅发给关注了该 Boss 的人,S2C 字段结构与 46016 完全相同,复用同一读取
        /// 函数——老端 On46008 直接 GetSCMD(46016))。S2C BossType:8,BossId:32。</summary>
        public const int BOSS_REVIVE_REMIND = 46008;
        /// <summary>Boss 重生广播(场景广播,通常与 46036 成对发出)。**老端真 bug(轮15a rule10 订正)**:
        /// `On46009` 判断写成 `boss_type==suit || secret || eudaemon || ...`,`||` 后面全是裸常量非零恒真,
        /// KILL_BOSS 事件对任意 boss_type 无条件触发——本端订正为显式逐项 `==` 比较(同轮13 权限 truthy 同款笔误)。
        /// S2C BossType:8,BossId:32,RebornTime:32,Num:8。</summary>
        public const int BOSS_REBORN = 46009;
        /// <summary>Boss 疲劳值广播(联动补发 46044 刷新完整体力信息,对标老端注释)。S2C BossTired:8
        /// (⚠字段名 boss_tired 不是 tired)。</summary>
        public const int BOSS_TIRED = 46011;
        /// <summary>幻兽领(Eudaemon)采集次数广播(登记防御 recv,同 46006 先例:mod_boss.erl:1795-1798
        /// 神庙怪刷新分支无条件 send_to_scene,老端 TS 侧零引用但服务端仍在推;Temple(5) 场景本轮 46003
        /// EnterBoss 即可到达,原自述"非自主推送不可达"结论有误,修复轮订正)。S2C BossType:8,BossId:32,Num:8。</summary>
        public const int BOSS_COLLECT_TIMES = 46013;
        /// <summary>每日 boss 重置广播(mod_boss.erl:1072 send_to_all 全服广播,空包无字段;与 46013/46031
        /// 同类"服务端在推、老端零消费"——登记防御 recv 保持同类一致,主控轮15a收尾补齐)。S2C 空。</summary>
        public const int BOSS_DAILY_RESET = 46014;
        /// <summary>结算奖励推送(与 47015 共用结构;C2S 读侧 pp_boss.erl 无 handle 子句,发了静默丢——
        /// 本端只 recv 不提供发送方法)。r15_server 直接核实 write 调用点真活(lib_boss_api.erl:176 /
        /// lib_boss_mod.erl:1290),订正子报告"死配置"误判为"C2S 死/S2C 活"。S2C RewardType:8,
        /// RewardList[u16计数]{Type:8,GoodsTypeId:32,Num:32,Id:64}。</summary>
        public const int BOSS_SETTLE_REWARD = 46015;
        /// <summary>Boss 被击杀/复活提醒通知(fieldspecial/field_infinite 不弹提示;abyss 180级以下不弹;
        /// field 体力为0不弹——UI 层判断留 TODO,本轮只落数据)。S2C BossType:8,BossId:32。</summary>
        public const int BOSS_KILLED_NOTICE = 46016;
        /// <summary>世界boss伤害榜前3名防抖广播(轮15a 订正:r15_server 子报告注3称"从未 send"为误判——
        /// 直接核实 mod_boss.erl:2052-2062 `do_handle_info({'send_rank',...})` 确有 `send_to_scene`,
        /// 500ms 防抖后真广播;老端 TS 侧虽已被新一代榜单取代零引用,但服务端仍在推,登记防御 recv)。
        /// S2C Rank[u16计数]{RoleName:s,Damage:32}。</summary>
        public const int BOSS_DAMAGE_RANK_TOP3 = 46019;
        /// <summary>世界boss伤害排名(自己,非拉取——每次伤害发生后服务端由 `lib_boss_api:be_hurted`→
        /// `rank_damage` 自动触发即时回给攻击者本人,recv 纯被动落表,不提供拉取方法)。
        /// S2C SelfRank:8,SelfDamage:32,SelfName:s,Distance:32。</summary>
        public const int BOSS_DAMAGE_RANK_SELF = 46022;
        /// <summary>连杀通知场景广播(轮15a 订正:r15_server 子报告注4称"全仓库无调用点"为误判——直接核实
        /// `lib_boss_mod:dkill_notice/2`(46024)确被 :765 行 `apply_cast` 调用且真 send_to_scene;
        /// dkill&gt;2 且是自己连杀才带 index,他人连杀按5的倍数才播报——UI 播报节流留 TODO,本轮落数据)。
        /// S2C RoleId:64,Figure(<see cref="Shenxiao.Common.Proto.FigureProto"/>),Dkill:16。</summary>
        public const int BOSS_DKILL_NOTICE = 46024;
        /// <summary>世界boss广播role信息壳(老端 `switch(vo.key)` case 体为空,未来扩展占位,本端同样只落
        /// 原始 key/val 不做业务分支)。S2C InfoList[u16计数]{Key:8,Val:32}。</summary>
        public const int BOSS_ROLE_INFO = 46025;
        /// <summary>节日boss隐藏宝箱列表推送(登记防御 recv:C2S 无 handle 子句/纯 server→client 单向,
        /// lib_boss_mod.erl:1713 write 侧真实调用;46003 Feast(11) 进场即可触达)。
        /// S2C BoxList[u16计数]{BoxId:32}。</summary>
        public const int BOSS_FEAST_HIDE_BOX = 46026;
        /// <summary>节日boss宝箱刷新广播(场景内怪物快照,28字段复用全场景怪物结构非boss专属item,
        /// lib_boss.erl:1723-1728 `send_to_scene(?FEAST_BOSS_SCENE,...)`,登记防御 recv)。
        /// S2C BossId:32,BossX:32,BossY:32,BoxList[u16计数]{X:16,Y:16,AutoId:32,MonCfgId:32,Hp:64,HpLim:64,
        /// Lv:16,Name:s,Sp:16,MonResource:32,MonRes:s,ImagId:32,WeaponId:32,AttType:8,Kind:8,Color:8,OnHook:8,
        /// Boss:8,CollectTime:32,IsBeClicked:8,IsBeAtted:8,Hide:8,Ghost:8,MonGroup:16,GuildId:64,Angel:16,
        /// AttrType:8,Title:32}。</summary>
        public const int BOSS_FEAST_BOX_REFRESH = 46027;
        /// <summary>节日boss采集结算结果(登记防御 recv)。S2C Code:8,RewardList(write_object_list,
        /// u16计数×{Type:8,GoodsTypeId:32,Num:32})。</summary>
        public const int BOSS_FEAST_COLLECT_RESULT = 46028;
        /// <summary>节日boss全部击杀空回执(登记防御 recv)。S2C 空包。</summary>
        public const int BOSS_FEAST_ALL_KILLED = 46029;
        /// <summary>秘境宝箱归属信息广播(登记防御 recv,同 46006 先例:lib_boss.erl:1833-1852
        /// `send_draw_data`/`send_to_scene`+`send_to_uid` 秘境boss被击杀时无条件广播;Secret(9) 场景本轮 46003
        /// EnterBoss 即可到达,原自述"非自主推送不可达"结论有误,修复轮订正)。S2C BossId:32,RoleId:64,Name:s,
        /// Career:8,Lv:16,Combat:64,Picture:s,PictureVer:32,Time:32,Curtimes:16,LimitTimes:16。</summary>
        public const int BOSS_DOMAIN_BOX_OWNER = 46031;
        /// <summary>节日boss下一波倒计时(登记防御 recv)。S2C NextWave:32,Time:32。</summary>
        public const int BOSS_FEAST_NEXT_WAVE = 46033;
        /// <summary>新野外boss死亡debuff状态查询(C2S 空包)。S2C DieTimes:16,Time:32,DebuffTime:32,SafeTime:32——
        /// 转发 <see cref="Shenxiao.Module.Core.Relive.ReliveModel"/> 死亡次数槽位(spec 明示接线点)。</summary>
        public const int BOSS_DEATH_DEBUFF = 46034;
        /// <summary>秘境领域层数广播(纯推送)。S2C BossType:8,Layer:8。</summary>
        public const int BOSS_DOMAIN_LAYER = 46035;
        /// <summary>Boss/大妖复活坐标点位广播(通常与 46009 成对发出)。S2C BossId:32,Xylist[u16计数]{X:16,Y:16}。</summary>
        public const int BOSS_REBORN_POS = 46036;
        /// <summary>boss血量百分比显示(登记防御 recv+补发送方法,C2S 空包,老端 BossModel.StartUpdateBossHp
        /// 每5s 轮询;config_boss_show_hp 门控哪些场景显示血条,战斗HUD消费方留TODO)。
        /// S2C List[u16计数]{MonId:32,AutoId:64,Hp:64,HpMax:64}。</summary>
        public const int BOSS_HP_SHOW = 46040;
        /// <summary>消耗复活(C2S "ci" boss_type,boss_id)。S2C Errcode:32,BossType:8,BossId:32。</summary>
        public const int BOSS_REVIVE_CONSUME = 46041;
        /// <summary>Boss 进出/复活成功广播通知(无 read,纯推送;`46041` 消耗复活成功分支联动触发,eudemons_land
        /// 系统也复用此号广播,与本轮无关不处理其分支)。S2C BossType:8,BossId:32。</summary>
        public const int BOSS_REVIVE_NOTICE = 46042;
        /// <summary>体力查询 ack(仅 NEW_OUTSIDE/SPECIAL 类型响应,真实数据走 46044;C2S "c" boss_type)。
        /// S2C 空包(纯触发信号)。</summary>
        public const int BOSS_VIT_ACK = 46043;
        /// <summary>体力详情查询(C2S "c" boss_type)。S2C Vit:16,MaxVit:16,AddVit:16,BackVit:16,LastVitTime:32。</summary>
        public const int BOSS_VIT_DETAIL = 46044;
        /// <summary>找回体力(C2S "ch" boss_type,vit_back_num)。**老端真 bug(轮15a rule9 订正)**:S2C 定义字段名
        /// 是 `code`,老端失败分支误读成不存在的 `scmd.errcode`(恒 undefined)——本端一律按 wire 真实字段
        /// `code` 实现,不照抄老端笔误。S2C Code:32。</summary>
        public const int BOSS_VIT_RECOVER = 46045;

        // ----- Boss 家族二期·跨服族(自动循环 轮15b;pt_470=千幻蜃楼/圣兽岭 47000-47035,pt_471=镇煞封魂/
        //        幻域Boss 47101-47117,pt_619=论剑恩怨簿 61900-61902,+ pt_460 内 kf_great_demon 壳
        //        46037/46038/46039/46046)。落点 KfBossController.cs/KfBossModel.cs,与 15a 的
        //        BossController.cs/BossModel.cs(本服 46000 段)并列,不改后者结构。
        //        死号裁决(与 yu_server 源码直接核对调用点;15b 服务端镜头复验):47008 r15b 侦察报告称"服务端无
        //        发送调用点",经 mod_eudemons_land.erl:1158/1188(write+send_to_scene)证伪,确为活号(服务端镜头
        //        复验通过);47117 同判活号防御 recv(lib_decoration_boss_local.erl:435 write+send_to_all,15b 未再独立复验):
        //        真死跳过:47001(发送侧 C2S 死号,老端从未 Fire,与 46032 同款;服务端 read/response 链其实完整,仅老端不发);
        //        47011(整链路死——pp_eudemons_land.erl 无 handle(47011,..)子句,write(47011) 组包在 zone_local.erl:154
        //        虽有唯一调用点,但其宿主触发函数 mod_eudemons_land_zone_local:get_same_zone_servers/1 全仓库零调用者,永不执行)。 -----

        /// <summary>千幻蜃楼/圣兽岭 boss 列表(C2S "c" boss_type,服务端裸值,如 holy=1)。S2C BossType:8(裸值,
        /// 客户端侧按老端 cross_boss_base_index=1000 自行 +1000 换算 UI 类型)、ActStatus:8、ResetEtime:32、
        /// Tired:8、MaxTired:8、CollectList[u16计数]{Type:8,CollectTimes:8,TotalCollectTimes:8}、
        /// BossInfo[u16计数]{BossId:32,Num:8,RebornTime:32,IsRemind:8}。§广播语义:sync==NO 时先回 47010
        /// 占位错误码(kf_server_allot),异步触发跨服同步,客户端需重新发起本请求才能吃到 sync==YES 后的真数据
        /// (r15b 时序结论:失败优先占位、无中间"已受理"包,不做自动轮询,严禁死等)。</summary>
        public const int KFBOSS_EUDEMONS_LIST = 47000;
        /// <summary>千幻蜃楼全局掉落日志查询(C2S 空包)。S2C DropLog[u16计数]{RoleId:64,ServerId:16,ServerNum:16,
        /// Name:s,BossId:32,Layers:8,GoodsId:32,Rating:32,EquipExtraAttr[...],Time:32}(与 46046 同形态,
        /// 共用 KfBossModel.CrossDropLogEntry)。</summary>
        public const int KFBOSS_EUDEMONS_DROP_LOG = 47002;
        /// <summary>进入千幻蜃楼(C2S "ci" boss_type,boss_id)。S2C Code:32。§跨服时序(r15b 权威):pp 层不等
        /// 跨服回执直接 apply_cast 转发;跨服侧失败显式回 47003 定向发送,**成功无任何包**,靠场景切换事件隐式
        /// 确认(严禁死等专属成功回包)。</summary>
        public const int KFBOSS_EUDEMONS_ENTER = 47003;
        /// <summary>离开千幻蜃楼(C2S "c" boss_type)。S2C Code:32。</summary>
        public const int KFBOSS_EUDEMONS_LEAVE = 47004;
        /// <summary>千幻蜃楼 boss 关注/取关(跨服变体,比 46007 少 AutoRemind 字段;C2S "cic" boss_type,boss_id,
        /// remind)。S2C Code:32,BossType:8,BossId:32,Remind:8。</summary>
        public const int KFBOSS_EUDEMONS_REMIND = 47005;
        /// <summary>千幻蜃楼 boss 重生提醒单播(仅关注者收到,无 read 纯推送)。S2C BossType:8,BossId:32。
        /// ⚠服务端在产 bug(r15b 实证,lib_great_demon_local.erl:571-580 send_remind_msg_role 复制粘贴遗留):
        /// 跨服大妖(KfGreatDemon=20)重生关注提醒本应走 pt_460:46008,却误写成 pt_470:write(47006,[20,BossId])
        /// 发出——本端不特殊处理,老端"歪打正着"地在这里正常收到即可(此 handler 本就不校验 BossType 取值),
        /// 对应的 46008-for-type20 路径不用补,注释存档到此为止。</summary>
        public const int KFBOSS_EUDEMONS_REBORN_TIP = 47006;
        /// <summary>千幻蜃楼 boss 被击杀信息(无 read 纯推送)。S2C BossType:8,BossId:32,RebornTime:32,Num:8。
        /// boss_type==holy(1+1000)时才触发 KILL_BOSS 类通知(对标老端 On47007),其余仅落地 RebornTime。</summary>
        public const int KFBOSS_EUDEMONS_KILLED_NOTICE = 47007;
        /// <summary>千幻蜃楼怪物重生刷新信息(无 read 纯推送)。S2C BossType:8,BossId:32,RebornTime:32,Num:8
        /// (与 47007 结构相同)。老端 RegisteredHandler 函数体为空(收到即弃),但服务端确认真会发送
        /// (mod_eudemons_land.erl:1158/1188,write+send_to_scene)——本端按活号防御 recv(数据落地,不额外弹窗)。</summary>
        public const int KFBOSS_EUDEMONS_REBORN_REFRESH = 47008;
        /// <summary>千幻蜃楼疲劳值广播(无 read 纯推送)。S2C BossTired:8。</summary>
        public const int KFBOSS_EUDEMONS_TIRED = 47009;
        /// <summary>千幻蜃楼错误提示码专用号(无 read 纯推送,"同步中"占位/通用错误码复用同一个号)。
        /// S2C Code:32——code==1031(kf_server_allot)时是老端特殊文案"正在获取千幻蜃楼信息 请稍候再试",
        /// 其余走通用错误码展示。不做自动重试轮询(r15b:老端本身也只是提示语,无计时器,严禁死等)。</summary>
        public const int KFBOSS_EUDEMONS_SYNC_CODE = 47010;
        /// <summary>千幻蜃楼结算奖励(C2S 空包)。S2C RewardType:8,RewardList[u16计数]{Type:8,GoodsTypeId:32,
        /// Num:32,Id:64}(与 46015 共用结构)。reward_type==3 时老端走通用弹窗,本轮只落数据不分弹窗。</summary>
        public const int KFBOSS_EUDEMONS_SETTLE_REWARD = 47015;
        /// <summary>千幻蜃楼个人信息推送壳(C2S 空包)。S2C InfoList[u16计数]{Key:8,Val:32}(老端 switch(key){}
        /// 空 case,占位壳,本端同样只落原始 key/val)。</summary>
        public const int KFBOSS_EUDEMONS_ROLE_INFO = 47016;
        /// <summary>千幻蜃楼进场景宝箱信息全量(C2S 空包)。S2C Info[u16计数]{BossId:32,Xylist[u16计数]{X:16,Y:16}}。</summary>
        public const int KFBOSS_EUDEMONS_BOX_POS = 47017;
        /// <summary>千幻蜃楼宝箱信息单条更新(无 read 纯推送)。S2C BossId:32,Xylist[u16计数]{X:16,Y:16}。</summary>
        public const int KFBOSS_EUDEMONS_BOX_POS_UPDATE = 47018;
        /// <summary>千幻蜃楼狩猎等级信息(C2S 空包,独有子系统)。S2C Level:16,Exp:32,AddExp:32。</summary>
        public const int KFBOSS_EUDEMONS_HUNT_LEVEL = 47019;
        /// <summary>圣兽领榜单(C2S 空包)。S2C PlayerList[u16计数]{RoleId:64,RoleName:s,ServerId:16,ServerNum:16,
        /// Score:32,SortKey1:32,KillNum:16,SortKey2:32,TotalScore:32,SortKey3:32}(一次性全量下发)。</summary>
        public const int KFBOSS_EUDEMONS_RANK = 47021;
        /// <summary>玩家获得积分推送(C2S 空包,无 read 但 wire 定义;老端 Handler 函数体为空,纯推送未消费)。
        /// S2C ScoreType:8,ScoreAdd:16。</summary>
        public const int KFBOSS_EUDEMONS_SCORE = 47022;
        /// <summary>最大疲劳值变化刷新(C2S 空包)。S2C MaxTired:8。</summary>
        public const int KFBOSS_EUDEMONS_MAX_TIRED = 47023;
        /// <summary>千幻蜃楼玩家死亡次数(C2S 空包)。S2C DieTimes:16,Time:32,DieTime:32,SafeTime:32——转发
        /// ReliveModel.HolyBoss 槽位(对标老端 SetReliveTimeData(...,BossSpecialReliveType.HolyBoss),与 46034
        /// 的 WorldBoss 槽位并列)。</summary>
        public const int KFBOSS_EUDEMONS_DEATH_DEBUFF = 47034;
        /// <summary>复活千幻蜃楼 boss(C2S "ci" boss_type,boss_id)。S2C Errcode:32,BossType:8,BossId:32。
        /// 成功后老端补发 47000(对标本端 EnterBoss 系"隐式成功"惯例的例外——此号本身即显式结果包,
        /// 成功分支仍需重拉列表刷新数据)。</summary>
        public const int KFBOSS_EUDEMONS_REVIVE = 47035;

        /// <summary>镇煞封魂主界面数据(C2S 空包)。S2C ActStatus:8,Count:8,AssistCount:8,BuyCount:8,AddCount:8,
        /// InBuff:8,KillCount:16,IsAlive:8,SbossRoleNum:8,BossList[u16计数]{BossId:32,RebornTime:32,RoleNum:8,
        /// IsHadAssist:8}。</summary>
        public const int KFBOSS_DECORATION_INFO = 47101;
        /// <summary>进入镇煞封魂 boss(C2S "ic" boss_id,type[1=普通/2=协助])。S2C ErrorCode:32,BossId:32,Type:8。
        /// §双路径(r15b):CLS_TYPE_GAME 纯本服同步处理,成败都显式回包;CLS_TYPE_CENTER 两段 cast 转发跨服,
        /// 全程无本服占位 ack,最终成败由跨服 relay 回一个显式包——两条路径客户端表现一致,均不用等待额外包。</summary>
        public const int KFBOSS_DECORATION_ENTER = 47102;
        /// <summary>退出镇煞封魂(C2S 空包)。S2C ErrorCode:32。</summary>
        public const int KFBOSS_DECORATION_LEAVE = 47103;
        /// <summary>购买进入次数(C2S 空包)。S2C ErrorCode:32——成功后老端本地 buy_count+1,不重查 47101。</summary>
        public const int KFBOSS_DECORATION_BUY_COUNT = 47104;
        /// <summary>取消关注列表(C2S 空包,初始化批量落地)。S2C UnfollowList[u16计数]{BossId:32}(数组元素是裸
        /// BossId,非结构体)。</summary>
        public const int KFBOSS_DECORATION_UNFOLLOW_LIST = 47105;
        /// <summary>单个关注/取关(C2S "ic" boss_id,is_follow)。S2C ErrorCode:32,BossId:32,IsFollow:8。</summary>
        public const int KFBOSS_DECORATION_FOLLOW = 47106;
        /// <summary>boss/特殊 boss 复活通知(无 read 纯推送)。S2C BossId:32——联动 config_decoration_boss
        /// 生成提示文案(老端消费方,本轮数据层只透出事件)。</summary>
        public const int KFBOSS_DECORATION_REBORN = 47107;
        /// <summary>镇煞封魂掉落记录(C2S 空包)。S2C DropLog[u16计数]{RoleId:64,ServerId:16,ServerNum:16,Name:s,
        /// BossId:32,GoodsId:32,Num:32,Rating:32,EquipExtraAttr[...],Time:32}——注意比 46002/47002/46046 少
        /// Layers 字段、多 Num 字段,独立形态(KfBossModel.DecorationDropLogEntry)。</summary>
        public const int KFBOSS_DECORATION_DROP_LOG = 47108;
        /// <summary>特殊 boss 个人伤害排名全量(C2S 空包)。S2C RankList[u16计数]{RoleId:64,Name:s,ServerId:16,
        /// ServerNum:16,ServerName:s,Hurt:64}。</summary>
        public const int KFBOSS_DECORATION_RANK = 47109;
        /// <summary>进入特殊 boss(C2S 空包)。S2C ErrorCode:32。</summary>
        public const int KFBOSS_DECORATION_ENTER_SPECIAL = 47110;
        /// <summary>仙宗召援(C2S 空包)。S2C ErrorCode:32——注意与 Guild 家族 40060"仙宗召援"同名不同协议号,
        /// 是镇煞封魂场景内独立的呼叫入口,不要混淆(轮13 报告已提醒)。</summary>
        public const int KFBOSS_DECORATION_GUILD_HELP = 47111;
        /// <summary>特殊 boss 个人伤害单条推送(无 read 纯推送,增量 patch 排行榜)。S2C RoleId:64,Name:s,
        /// ServerId:16,ServerNum:16,ServerName:s,Hurt:64——按 RoleId 命中则更新伤害,否则追加。</summary>
        public const int KFBOSS_DECORATION_DAMAGE_PUSH = 47112;
        /// <summary>镇煞封魂 boss 结算(无 read 纯推送)。S2C IsBelong:8,IsDouble:8,RewardTypeList[u16计数]
        /// {RewardType:8,RewardList[u16计数]{Style1:8,TypeId1:32,Count1:32,GoodsId1:64}},RewardTypeList2
        /// [同构](双层嵌套数组,两套奖励表)。</summary>
        public const int KFBOSS_DECORATION_SETTLE = 47113;
        /// <summary>战斗场景信息(C2S 空包,进场景即推)。S2C EnterType:8,QuitTime:32,ReviveTime:32。</summary>
        public const int KFBOSS_DECORATION_SCENE_INFO = 47114;
        /// <summary>退出时间单独刷新(无 read 纯推送)。S2C QuitTime:32。</summary>
        public const int KFBOSS_DECORATION_QUIT_TIME = 47115;
        /// <summary>复活时间单独刷新(无 read 纯推送)。S2C ReviveTime:32。</summary>
        public const int KFBOSS_DECORATION_REVIVE_TIME = 47116;
        /// <summary>boss/特殊 boss 死亡广播(无 read;老端 RegisteredHandler 函数体为空,收到即弃)。
        /// S2C BossId:32,RebornTime:32。服务端确认真会发送且是全服广播(lib_decoration_boss_local.erl:435-436
        /// `write(47117,...)+send_to_all`——本代理直接核对调用点证伪了 r15b 报告"零调用点"的结论),
        /// 按活号防御 recv(数据只落日志,不建 UI)。</summary>
        public const int KFBOSS_DECORATION_DEATH = 47117;

        /// <summary>论剑恩怨簿界面协议(C2S 空包)。S2C SendList[u16计数]{Sign:8,Time:32,SceneName:s,AttrName:s,
        /// AttrId:64}(本服)+ KfSendList[u16计数]{Sign:8,Time:32,SceneName:s,ServerId:32,ServerNum:32,
        /// AttrName:s,AttrId:64}(跨服,注意 ServerId/ServerNum 是 32 位,与 47xxx/46xxx 系普遍 16 位不同)。
        /// §scope quirk(r15b+r15_oldboss#10):服务端 is_in_kf_pk_scene 只覆盖 EUDEMONS_BOSS/KF_SANCTUARY/
        /// SANCTUM 三类场景,镇煞封魂/跨服大妖场景死亡不产恩怨记录,照实接收不额外过滤。§凌晨清理 quirk:
        /// pt_619 变量名 BeforeOneMounth 实际赋值=NowTime(近全清,非 30 天窗口)+ DB 持久化代码整段注释死,
        /// 纯会话内存态——客户端照收推送即可,不做本地跨会话缓存假设。</summary>
        public const int KFBOSS_KILL_RECORD_LIST = 61900;
        /// <summary>本服新击杀记录推送(无 read;单条,与 61900 SendList 单条 item 结构相同)。
        /// S2C Sign:8,Time:32,SceneName:s,AttrName:s,AttrId:64。</summary>
        public const int KFBOSS_KILL_RECORD_NEW = 61901;
        /// <summary>跨服击杀记录推送(无 read;单条,与 61900 KfSendList 单条 item 结构相同)。
        /// S2C Sign:8,Time:32,SceneName:s,ServerId:32,ServerNum:32,AttrName:s,AttrId:64。</summary>
        public const int KFBOSS_KILL_RECORD_KF_NEW = 61902;

        /// <summary>跨服秘境大妖(太古遗凶)阶段奖励状态(C2S 空包)。S2C KillNum:32,HadRewardList[u16计数]
        /// {Stage:16}——pt_460 内 kf_great_demon 专属壳,pp_boss.erl 无条件转发 mod_great_demon_local,
        /// 与 boss_type 取值无关(15a 曾判定死号,15b 订正实现)。</summary>
        public const int KFBOSS_GREAT_DEMON_REWARD_STATE = 46037;
        /// <summary>领取太古遗凶阶段奖励(C2S "i" reward_id)。S2C RewardId:32,Code:32。成功后老端补发本号
        /// 重拉(对标 SendFmtToGame(46037))。</summary>
        public const int KFBOSS_GREAT_DEMON_REWARD_TAKE = 46038;
        /// <summary>太古遗凶进场景宝箱+特殊 boss 信息(C2S 空包)。S2C Info[u16计数]{BossId:32,Xylist[u16计数]
        /// {X:16,Y:16}}——只收集 mon_type∈{宝箱2/高级宝箱3/特殊大妖1}三种,普通怪(0)不进列表。</summary>
        public const int KFBOSS_GREAT_DEMON_BOX_INFO = 46039;
        /// <summary>太古遗凶掉落记录(C2S "h" boss_type,固定传 KfGreatDemon=20)。S2C BossType:16,DropLog
        /// [u16计数]{RoleId:64,ServerId:16,ServerNum:16,Name:s,BossId:32,Layers:8,GoodsId:32,Rating:32,
        /// EquipExtraAttr[...],Time:32}(与 47002 同形态,共用 KfBossModel.CrossDropLogEntry)。</summary>
        public const int KFBOSS_GREAT_DEMON_DROP_LOG = 46046;

        // ----- 婚姻(172xx+223xx,pt_172 pp_marriage,征友/戒指/结婚;老端 MarriageController.ts,自动循环 轮16) -----
        //        本包=MarriageController 注册 33 号(纯数据层,UI View 绑定留尾包)。排除:marriage2=Banquet
        //        (17249/17256 已在上方由 BANQUET_WEDDING_STATE/BANQUET_CALL 占用,勿重复定义)/dungeonMarriage=
        //        BaseDungeon(61020/61021)/baby=BabyController(18xxx,17280-94 宝宝幻形整体悬空)/遗留死号
        //        (17203/04/20/21/25/30/33/41-44/54/55/80-94/98)。位宽独例(逐号勿套模板):17222 CombatPower=u32;
        //        17226(bin_6/bin_8)·17232=u64。无 Code 前导帧:17205/17222/17224/17226/17229/17238/17244/17296/
        //        17297(纯推送/通知);17246 与 r16 报告"无Code"结论不同——ClientProtocol.json 与老端 on17246 实读
        //        `scmd.code` 逐字核验后订正为**带 Code**(本代理直接核对原文覆盖侦察报告的误判)。17212 戒指单步
        //        升级为死号(老端注册 handler 但零发送点+成功分支全注释),本段只注册防御 recv 不提供发送方法。 -----

        /// <summary>征友大厅列表分页(C2S "c" page)。S2C Code:32,Page:8,OwnPopularity:32,AskFollowTime:32,
        /// AskFlowerTime:32,LessFreeTimes:8,PlayerList[u16计数]{RoleId:64,Name:s,Lv:16,Sex:8,Vip:32,Career:8,
        /// Turn:8,IfMarriage:8,Picture:s,PictureVer:32,IfOnline:8,Popularity:32,Msg:s,Type:8,Time:32,IfFollow:8,
        /// IfFriend:8,Intimacy:32,TagList[u16计数]{TagId:8,TagSubid:8},VipExp:32,VipHide:8,IsSupvip:8}
        /// (**无 CombatPower 字段**,勿多读)。Page:1=大厅/2=我的关注/3=粉丝(关注/粉丝页硬截断100条,大厅不截断)。</summary>
        public const int MARRIAGE_PERSONALS_LIST = 17200;
        /// <summary>关注/取消关注玩家(C2S "lc" follow_role_id,type)。S2C Code:32,FollowRoleId:64,Type:8。</summary>
        public const int MARRIAGE_PERSONALS_FOLLOW = 17201;
        /// <summary>发布征友信息(C2S 变长:Msg:str,Type:8,TagList[u16计数]{TagId:8,TagSubid:8})。
        /// S2C Code:32,Type:8。</summary>
        public const int MARRIAGE_PERSONALS_ISSUE = 17202;
        /// <summary>玩家细节(公会)信息(C2S "l" role_id)。S2C RoleId:64,GuildId:64,GuildName:s(**无 Code 前缀,
        /// 独例**)。</summary>
        public const int MARRIAGE_ROLE_DETAIL = 17205;

        /// <summary>戒指信息(C2S 空包)。S2C Code:32,Stage:8,Star:8,PrayNum:32,RingCombatPower:32,
        /// PolishList[u16计数]{GoodsTypeId:32,UseNum:16},AttrList[u16计数]{AttrType:32,AttrNum:32}。
        /// 老端用 config_ring_star 自算 ring_combat_power **覆盖**服务端字段——本轮先如实落地服务端值,
        /// 自算覆盖逻辑留 TODO(见 MarriageController 注释)。</summary>
        public const int MARRIAGE_RING_INFO = 17210;
        /// <summary>解锁戒指(C2S 空包)。S2C Code:32,Stage:8,Star:8,PrayNum:32。</summary>
        public const int MARRIAGE_RING_UNLOCK = 17211;
        /// <summary>戒指单步提升——**死号**(老端注册 handler 但零发送点+成功分支全注释,实际升级走 17213
        /// 一键提升)。C2S "i" goods_type_id(GoodsTypeId:32,虽 wire 定义完整但本端不提供发送方法)。
        /// S2C Code:32,GoodsTypeId:32,Stage:8,Star:8,PrayNum:32——本端只注册防御 recv(解析不消费,失败发
        /// EVT_MARRIAGE_RING_STOP_UPGRADE 对齐老端失败分支)。</summary>
        public const int MARRIAGE_RING_UPGRADE_STEP = 17212;
        /// <summary>一键提升戒指(C2S 空包)。S2C Code:32,Stage:8,Star:8,PrayNum:32。</summary>
        public const int MARRIAGE_RING_UPGRADE_ALL = 17213;

        /// <summary>求婚/再婚/离婚协商/礼包邀请推送(无 read,纯推送)。S2C RoleId:64,Name:s,Lv:16,
        /// CombatPower:32(**u32 独例**,勿套 17226/17232 的 u64),Sex:8,Vip:32,Career:8,Turn:8,Picture:s,
        /// PictureVer:32,Type:8,ProposeType:8,Msg:s,IfAa:8,CostList[u16计数]{GoodsType:32,GoodsTypeId:32,
        /// GoodsNum:32}(**无 Code 前缀**)。Type:2=求婚/4=离婚协商/5=请求购买礼包。</summary>
        public const int MARRIAGE_PROPOSE_PUSH = 17222;
        /// <summary>回应求婚(C2S "lc" role_id,type;type:1=答应/2=拒绝)。S2C Code:32,RoleId:64,Type:8。</summary>
        public const int MARRIAGE_PROPOSE_RESPOND = 17223;
        /// <summary>回应求婚/离婚结果推送(无 read,双向单播,纯推送)。S2C RoleId:64,Type:8,AnswerType:8
        /// (**无 Code 前缀**)。AnswerType:1=答应/2=拒绝——老端仅 answer_type==1 时处理(清亲密度/开成功窗/
        /// 重拉伴侣礼包戒指信息),==2 拒绝无任何反馈分支,本端镜像:仅 answer_type==1 时落地并发事件。</summary>
        public const int MARRIAGE_ANSWER_PUSH = 17224;
        /// <summary>登录求婚/离婚信息汇总(C2S 空包,无 Code 前缀)。S2C BiaobaiList[u16计数]×bin_6,
        /// BiaobaiAnswerList[u16计数]×bin_8。bin_6: RoleId:64,Name:s,Lv:16,CombatPower:**64**(独例,与
        /// 17222 的 u32 不同),Sex:8,Vip:32,Career:8,Turn:8,Type:8,ProposeType:8,Msg:s,IfAa:8,
        /// CostList[u16计数]{u32,u32,u32}。bin_8: RoleId:64,Name:s,Lv:16,CombatPower:64,Sex:8,Vip:32,
        /// Career:8,Turn:8,Type:8,AnswerType:8。老端只读 biaobai_list、**不读 answer_list**——本端两个数组
        /// 都必须 ReadArray 读完保游标(answer_list 落地但老端未消费,比老端完整无害)。</summary>
        public const int MARRIAGE_BIAOBAI_LIST = 17226;
        /// <summary>其他信息推送(键值,如恩爱值,无 read,无 Code 前缀)。S2C List[u16计数]{Key:8,Val:32}。
        /// Key==1 时对应恩爱值(老端 SetLoveNum)。</summary>
        public const int MARRIAGE_KEY_VALUE_PUSH = 17229;
        /// <summary>发送求婚(C2S "lcsc" role_id,wedding_type,msg,if_aa)。S2C Code:32,RoleId:64
        /// (成功后对方收到 17222/17224 推送)。AA制分支服务端已注释,IfAa 读入即弃,不影响本端如实发送。</summary>
        public const int MARRIAGE_PROPOSE_SEND = 17231;
        /// <summary>我的伴侣(C2S 空包)。S2C Code:32,RoleId:64,CombatPower:**64**,
        /// Figure(<see cref="Shenxiao.Common.Proto.FigureProto"/>,write_figure 全字段含 is_marriage/
        /// marriage_id/marriage_name),Type:8,NowWeddingState:8,AnniversaryTime:32,LoveNum:32,
        /// FirstMarriage:8。老端 code∈{1,1720012(单身),1012} 都当成功刷新伴侣(有意逻辑非bug),本端三码
        /// 同镜像落地。</summary>
        public const int MARRIAGE_MATE_INFO = 17232;
        /// <summary>发送离婚(C2S "c" divorce_type)。S2C Code:32(成功后对标老端重拉 17232)。</summary>
        public const int MARRIAGE_DIVORCE_SEND = 17234;
        /// <summary>回应离婚(C2S "c" answer_type)。S2C Code:32,AnswerType:8。</summary>
        public const int MARRIAGE_DIVORCE_RESPOND = 17235;
        /// <summary>领取恩爱称号奖励(C2S "c" id)。S2C Code:32,Id:8。</summary>
        public const int MARRIAGE_DSGT_TAKE = 17236;
        /// <summary>购买真爱礼包(C2S 空包)。S2C Code:32(老端成功分支额外经 ChatModel 发情侣公告私信
        /// BoardMarriager,跨模块社交联动,本轮数据层不接,留 TODO)。</summary>
        public const int MARRIAGE_GIFT_BUY = 17237;
        /// <summary>真爱礼包信息(C2S 空包,无 Code 前缀)。S2C LoveGiftTimeS:32,LoveGiftTimeO:32,
        /// GiftState[u16计数]{CountType:8,State:8,Time:32}。</summary>
        public const int MARRIAGE_GIFT_INFO = 17238;
        /// <summary>领取真爱礼包奖励(C2S "c" count_type;1=购买礼包/2=登录礼包)。S2C Code:32,CountType:8,
        /// Reward:ObjectList(u16计数{Type:8,TypeId:32,Num:32})。</summary>
        public const int MARRIAGE_GIFT_TAKE = 17239;
        /// <summary>请求对方购买礼包(C2S 空包)。S2C Code:32(成功后对方收到 17222 type=5 推送)。</summary>
        public const int MARRIAGE_GIFT_ASK_BUY = 17240;

        /// <summary>进入/退出伴侣副本匹配的历史线格式(C2S "ci" type,dun_id;type:1=进入/2=退出;S2C
        /// Code:32,Type:8,DunId:32)。服务端 pp_marriage:handle(17245) 已整段注释,本端不提供发送 API;
        /// 常量和接收注册只用于防御尾包。</summary>
        public const int MARRIAGE_DUN_MATCH = 17245;
        /// <summary>匹配结果(无 read,纯推送)。S2C Code:32,List[u16计数]{Type:8,RoleId:64,
        /// Figure(<see cref="Shenxiao.Common.Proto.FigureProto"/>),Power:64},EnterTime:8。⚠与 r16 报告
        /// "无Code"结论不同——ClientProtocol.json 定义 code:i 为首字段且老端 on17246 实读 scmd.code,直接
        /// 核对原文订正为带 Code(本代理 §1 裁决)。由于 17245 服务端入口已封存,17246 推送链无触发源;
        /// 本端只保留防御接收、解析落地和事件派发。</summary>
        public const int MARRIAGE_DUN_MATCH_RESULT = 17246;

        /// <summary>进入婚礼场景(轮22 族错误出口批;老端挂在 BanquetController.ts:202-207 On17263,
        /// code!=1→ErrorCodeShow,无其它副作用;协议属 pt_172 婚姻族)。回包(pp_marriage.erl:1979/1985,
        /// mod_marriage_wedding_mgr.erl:1424;pt_172.erl write(17263,[Code])):code:i。
        /// ⚠成功分支(check_all 通过)不回本号,由 mod_marriage_wedding_mgr:enter_wedding 走场景切换,
        /// 本端只镜像老端"失败才提示"的错误壳,不臆造成功回调。</summary>
        public const int MARRIAGE_BANQUET_ENTER_SCENE = 17263;
        /// <summary>离开婚礼场景(轮22 族错误出口批;老端 BanquetController.ts:209-214 On17264,
        /// code!=1→ErrorCodeShow,无其它副作用)。回包(pp_marriage.erl:1992-2005,
        /// pt_172.erl write(17264,[Code])):code:i(成功/失败均回此号)。</summary>
        public const int MARRIAGE_BANQUET_LEAVE_SCENE = 17264;

        /// <summary>邀请伴侣购买副本次数(C2S "i" dun_id)。S2C Code:32。</summary>
        public const int MARRIAGE_DUN_INVITE_BUY = 17295;
        /// <summary>收到伴侣购买副本次数邀请推送(无 read,无 Code 前缀)。S2C RoleId:64,RoleName:s,DunId:32。</summary>
        public const int MARRIAGE_DUN_INVITE_PUSH = 17296;
        /// <summary>同意/拒绝购买副本次数(C2S "ci" agree,dun_id;agree:1=同意/2=拒绝)。S2C Agree:8,DunId:32,
        /// RoleId:64,RoleName:s(**无 Code 前缀**,回执字段即请求回声)。</summary>
        public const int MARRIAGE_DUN_INVITE_RESPOND = 17297;

        /// <summary>鲜花错误码专用号(无 read,纯推送)。S2C Code:32。</summary>
        public const int MARRIAGE_FLOWER_ERROR = 22300;
        /// <summary>赠送鲜花(C2S "lhihc" role_id,server_id,goods_type_id,num,anonymous)。S2C Code:32,
        /// ReceiveId:64,ReceiveServerId:16,GoodsId:32,GoodsNum:16。</summary>
        public const int MARRIAGE_FLOWER_GIVE = 22301;
        /// <summary>收礼记录(C2S 空包,无 Code 前缀;一次性全量下发,无分页)。S2C RecordList[u16计数]
        /// {Id:64,SenderId:64,SenderName:s,ServerId:16,ServerNum:16,GoodsId:32,GoodsNum:16,Anonymous:8,
        /// IsThanks:8,Time:32}。</summary>
        public const int MARRIAGE_FLOWER_RECORD = 22302;
        /// <summary>鲜花相关信息(C2S 空包,无 Code 前缀)。S2C FlowerNum:32,Charm:32,Fame:32。</summary>
        public const int MARRIAGE_FLOWER_INFO = 22303;
        /// <summary>收到的鲜花通知(无 read,无 Code 前缀)。S2C SenderId:64,
        /// SenderFigure(<see cref="Shenxiao.Common.Proto.FigureProto"/>),ServerId:16,ServerNum:16,
        /// GoodsId:32,GoodsNum:16。</summary>
        public const int MARRIAGE_FLOWER_RECEIVED = 22304;
        /// <summary>感谢收花者(C2S "l" id;老端两处调用点分别传 role_id 与记录 id,字段语义按上下文由调用方
        /// 决定,wire 侧仅回声该值)。S2C Code:32,Id:64。</summary>
        public const int MARRIAGE_FLOWER_THANKS = 22305;

        // ===================================================================================================
        // ----- 自定义活动(331xx/332xx+225xx补全+224xx+159xx,pp_custom_act/pp_custom_act_list/pp_rush_rank,
        // 自动循环 轮17)P1-P6 全部活号常量。331xx/332xx 已存在:CUSTOM_ACTIVITY_LIST=33101(:683)/
        // CUSTOM_ACTIVITY_FTVINVEST=33211(:720)/CUSTOM_ACTIVITY_RED_ENVELOPE_REBATE=33255(:721);225xx 已存在:
        // TOP_PLAYER_RANK_INFO=22501(:689)/TOP_PLAYER_GOAL_INFO=22502(:692)——原位保留不重复定义,本段仅补全。
        // 命名:CUSTOM_ACT_*(331xx/332xx新号)/TOP_PLAYER_*(225xx补全)/KF_FLOWER_RANK_*·CONSUME_RANK_*(224xx)/RECHARGE_STAT_*(159xx)。
        // 死号(§1,老端未注册或服务端/客户端任一侧全死)严禁在本段出现:33107/33110/33111/33112-33114/33116
        // (33115 完美情缘活)/33118/33120-33123/33126/33127/33143/33145/33146/33148/33149/33151-33154/
        // 33160-33164/33170-33178/33180-33189(33179 活)/33198/33199/33201-33208/33218-33220/33223/33249/
        // 33252-33254(LIST_DUOBAO 独立包不碰)/33261/22601-22603。
        // ===================================================================================================

        // ---- P1 框架核心(pt_331 33100-33108,33101 已存在于上方;字段序逐个回 pt_331.erl 原文+item_to_bin_N
        // 核对,发现 33104 recon 表初稿"Type:8,Value:32"有误,已用 pt_331.erl:2236 item_to_bin_3 订正为
        // 8字段,详见常量注释) ----
        /// <summary>331 家族通用错误码出口(纯推送,S2C only)。回包 ErrorCode:32。老端仅 ErrorCode!=1012 时
        /// 弹窗显错(pt_331.erl:347-353,ClientProtocol.json "33100")。</summary>
        public const int CUSTOM_ACT_ERROR = 33100;
        /// <summary>活动增量新开(S2C only,推送点 lib_custom_act.erl:2247)。回包同 33101 结构:List[u16计数]×
        /// {BaseType:16,SubType:16,ActType:8,ShowId:16,Wlv:16,Name:s,Desc:s,Condition:s,Stime:32,Etime:32}
        /// (pt_331.erl:370-383 item_to_bin_1,字段序与 item_to_bin_0/33101 完全一致)。</summary>
        public const int CUSTOM_ACT_ADD = 33102;
        /// <summary>活动增量关闭(S2C only,推送点 lib_custom_act.erl:2274/280、mod_hi_point.erl:204)。
        /// 回包 List[u16计数]×{BaseType:16,SubType:16}(pt_331.erl:385-398)。</summary>
        public const int CUSTOM_ACT_REMOVE = 33103;
        /// <summary>单活动通用详情(默认兜底号,RequireActInfo 分发表末尾兜底)。C2S "hh" BaseType,SubType。
        /// S2C BaseType:16,SubType:16,RewardList[u16计数]×{Grade:16,FormType:8,Status:8,ReceiveTimes:16,
        /// Name:s,Desc:s,Condition:s,Reward:s}——**订正**:pt_331.erl:400-417 的 write 子句只列了变量名,
        /// 真实字段序在 item_to_bin_3(pt_331.erl:2236-2262)且与 ClientProtocol.json "33104" 完全一致
        /// (8字段,非早期侦察表误记的 Type:8,Value:32 两字段/33107 的结构)。</summary>
        public const int CUSTOM_ACT_DETAIL = 33104;
        /// <summary>通用领取/操作结果回执(近20个子活动共用)。C2S "hhh" BaseType,SubType,Grade。
        /// S2C ErrorCode:32,BaseType:16,SubType:16,Grade:16(pt_331.erl:419-431)。</summary>
        public const int CUSTOM_ACT_CLAIM = 33105;
        /// <summary>全服计数(FtvCollectionExchange/FtvShop/AtListPurchase 共用)。C2S 老端 args.length>=5 时才发
        /// "hhhhh" BaseType,SubType,ModId,CounterId,Grade(不足5参不发,ts:359-365)。S2C BaseType:16,SubType:16,
        /// ModId:16,CounterId:16,Count:16,Grade:16(pt_331.erl:433-449)。</summary>
        public const int CUSTOM_ACT_ALLCOUNT = 33106;
        /// <summary>活动刷新批量指令(S2C only,推送点 lib_custom_act.erl:437 RefreshActList)。回包
        /// Values[u16计数]×{BaseType:16,SubType:16}(pt_331.erl:470-483)。老端收到后遍历逐条 RequireActInfo,
        /// 本端镜像遍历调 RequestActDetail(见 CustomActivityController.Core.cs On33108)。</summary>
        public const int CUSTOM_ACT_REFRESH = 33108;

        // ---- P1 头号玩家/开服冲榜补全(pt_225,22501/22502 已存在于上方;本段仅补 22500/22503-05,
        // 实现归 P6 独占 TopPlayerController.cs) ----
        /// <summary>头号玩家通用错误码(S2C only)。回包 ErrorCode:32。老端仅 ErrorCode!=1012 弹窗
        /// (ClientProtocol.json "22500","error_code":"i")。</summary>
        public const int TOP_PLAYER_ERROR = 22500;
        /// <summary>领取目标奖励。C2S "ihc" 实参序=Type,SubType(恒=1),Goal——轮17收口订正:早期注释写
        /// "Type,Goal,SubType"与原文不符,已按 pt_225.erl:15-19 read 定义+老端 TopPlayerItem.ts:50-52 调用
        /// 实参交叉核实。S2C ErrorCode:32,Type:32,Goal:8,SubType:16(ClientProtocol.json "22503")。
        /// 成功后老端重拉 22502。</summary>
        public const int TOP_PLAYER_GOAL_CLAIM = 22503;
        /// <summary>领取排名奖励。C2S "ihc" 实参序=Type,SubType(恒=1),RewardId(同上轮17收口订正,
        /// pt_225.erl:20-24)。S2C ErrorCode:32,RewardId:8,SubType:16,Type:32(ClientProtocol.json "22504")。
        /// 成功后老端重拉 22502。</summary>
        public const int TOP_PLAYER_RANK_CLAIM = 22504;
        /// <summary>头号玩家获取途径信息。C2S "i" RushId。S2C RushId:32,Res[u16计数]×{JumpId:32,Label:32,
        /// EndTime:64}(ClientProtocol.json "22505")。</summary>
        public const int TOP_PLAYER_GET_WAY = 22505;

        // ---- P1 鲜花榜/消费榜补全(224xx,与轮12 221xx 竞榜/pt_225 无交集;实现归 P6 独占。轮17收口定名:
        // P6 已裁决 22400/22403=跨服鲜花榜(老端注册处注释"//鲜花榜" ts:2878-2880,On22403 联动
        // FlowerrankModel.SetFlowerRankData ts:1911-1915;本服鲜花榜走 22401 不在本轮号段),
        // 22405=首发充值消费排行(ts:2921-2922)。Unity 无 FlowerrankModel,数据落 P6 Model+TODO) ----
        /// <summary>跨服鲜花榜通用错误码(S2C only)。回包 Code:32(ClientProtocol.json "22400")。</summary>
        public const int KF_FLOWER_RANK_ERROR = 22400;
        /// <summary>跨服鲜花榜数据(含 figure_list)。C2S "ih" rankType,subType(老端 FlowerRankView.ts:63-84
        /// 仅 base_type==2 跨服时发)。S2C Type:32,SubType:16,SelRank:32,SelVal:32,SelZone:8,Sum:32,MaxLen:16,
        /// RankLimit:32,RankList[u16计数]×{RoleId:64,ServerId:16,Zone:8,ServerNum:16,Name:s,FirstValue:32,
        /// Rank:32},FigureList[u16计数]×{RoleId:64,Figure:RecFigure}(ClientProtocol.json "22403")。</summary>
        public const int KF_FLOWER_RANK_INFO = 22403;
        /// <summary>首发充值消费排行。S2C Code:32,Type:16,SubType:16,RankType:32,SelRank:32,SelVal:32,Sum:32,
        /// MaxLen:16,RankLimit:32,RankList[u16计数]×{RoleId:64,Name:s,FirstValue:32,Rank:32}
        /// (ClientProtocol.json "22405")。</summary>
        public const int CONSUME_RANK_INFO = 22405;

        // ---- P1 充值统计补全(159xx,15908 归 AddVipService 与本段无冲突;实现归 P5) ----
        /// <summary>每日累充信息。S2C SubType:16,Num:32,RewardInfos[u16计数]×{Id:16,State:8,Val:32,Max:32,
        /// RewardList:ObjectList,Condition:s,Desc:s}(ClientProtocol.json "15955";老端注册注释"每日累充")。</summary>
        public const int RECHARGE_STAT_DAILY_ACCUM_INFO = 15955;
        /// <summary>每日累充奖励列表。S2C SubType:16,RewardList[u16计数]×{Id:16,State:8,Val:32,Max:32,
        /// GoldNum:64,RewardList:ObjectList,Condition:s,Desc:s}(ClientProtocol.json "15956")。</summary>
        public const int RECHARGE_STAT_DAILY_ACCUM_REWARD = 15956;
        /// <summary>某活动类型充值总额(老端注释"某个活动类型的充值总额")。S2C Type:16,SubType:16,TotalGold:32
        /// (ClientProtocol.json "15957")。老端 On15957→SetActRecharge(type,subtype,total_gold)。</summary>
        public const int RECHARGE_STAT_ACT_RECHARGE = 15957;
        /// <summary>节日活动·充值有礼充值金额(老端注释原文)。S2C Type:16,SubType:16,TotalGold:32
        /// (ClientProtocol.json "15958")。</summary>
        public const int RECHARGE_STAT_POLITE_RECHARGE = 15958;
        /// <summary>当天充值金额(老端注释原文)。S2C TotalGold:32(ClientProtocol.json "15959")。
        /// 老端收到后追发 RequireActInfo(CON_RECHARGE,1)。</summary>
        public const int RECHARGE_STAT_TODAY = 15959;
        /// <summary>几天前的充值金额列表(老端注释原文)。S2C Lists[u16计数]×{Time:32,TotalGold:32}
        /// (ClientProtocol.json "15960")。</summary>
        public const int RECHARGE_STAT_HISTORY = 15960;

        // ---- P2 抽奖A:OPTIONALLOTTO=76/WISH_POOL=79/DESTINY_TURNTABLE=99/TURNTABLE_100=100 ----
        public const int CUSTOM_ACT_LOTTO_PANEL = 33128;   // OPTIONALLOTTO 界面
        public const int CUSTOM_ACT_LOTTO_LOCK = 33129;    // 锁定奖池;**变长发送特例**(老端 WriteBegin/WriteFMT 手写,非固定 fmt)
        public const int CUSTOM_ACT_LOTTO_RESET = 33133;   // 重置
        public const int CUSTOM_ACT_LOTTO_DRAW = 33134;    // 抽奖
        public const int CUSTOM_ACT_LOTTO_STAGE = 33135;   // 阶段奖
        public const int CUSTOM_ACT_LOTTO_POOL = 33139;    // 奖池
        public const int CUSTOM_ACT_WISHPOOL_POOL = 33141;  // WISH_POOL 奖池
        public const int CUSTOM_ACT_WISHPOOL_CLAIM = 33142; // 取奖池奖励(老端 fmt 表 33142 命中"hhh"死分支,实发参数待 P2 回调用点核实)
        public const int CUSTOM_ACT_WISHPOOL_RESET = 33144; // 重置
        public const int CUSTOM_ACT_DESTINY_PANEL = 33238;  // DESTINY_TURNTABLE 界面
        public const int CUSTOM_ACT_DESTINY_PUSH = 33239;   // **recv-only**(C2S 死,S2C 抽奖后积分推送)
        public const int CUSTOM_ACT_DESTINY_DRAW = 33240;   // 开抽;Reward 走 write_string 非 object_list(pt_332.erl:1042)
        public const int CUSTOM_ACT_TURN100_PANEL = 33241;  // TURNTABLE_100 界面
        public const int CUSTOM_ACT_TURN100_PUSH = 33242;   // **recv-only** 推送

        // ---- P3 抽奖B:GASHAPON=103/LUC_TREA_TWO=102/ONLINE_DRAW=81/LUC_TREA=80/FORTUNECAT=87/
        // BIND_JAGE_WISH=127 ----
        public const int CUSTOM_ACT_GASHAPON_INFO = 33245;        // 通用抽奖信息
        public const int CUSTOM_ACT_GASHAPON_DRAW = 33246;        // 开抽
        public const int CUSTOM_ACT_LUCTREA2_PANEL = 33243;       // 幸运鉴宝2 界面;GradeInfo 嵌套
        public const int CUSTOM_ACT_LUCTREA2_DRAW = 33244;
        public const int CUSTOM_ACT_ONLINEDRAW_PANEL = 33217;     // WinnerList 含 write_figure→复用 FigureProto
        public const int CUSTOM_ACT_ONLINEDRAW_GOODS_POWER = 33266; // 物品期望战力,read GoodsId:64
        public const int CUSTOM_ACT_LUCTREA_PANEL = 33213;        // Pool=Obj[],ErrorCode 在**末尾**
        public const int CUSTOM_ACT_LUCTREA_DRAW = 33214;
        public const int CUSTOM_ACT_FORTUNECAT_INFO = 33224;
        public const int CUSTOM_ACT_FORTUNECAT_DRAW = 33225;
        public const int CUSTOM_ACT_FORTUNECAT_RECORD = 33226;
        public const int CUSTOM_ACT_BINDJAGE_INFO = 33260;        // 心愿单信息
        public const int CUSTOM_ACT_BINDJAGE_DRAW = 33262;        // Errcode 末尾
        public const int CUSTOM_ACT_BINDJAGE_FREEGIFT = 33263;    // Errcode 末尾

        // ---- P4 节日族:摇钱树 MONEYTREE=50/MOUNT_TURNTABLE=54/MONEYTREE_SHOP=89/FTVACTIVENESS=56/
        // SAIBOTREASURE=58/绑钻转盘 TURNTABLE=28/RED_PACKET_RAIN=82/HOLYCALL=67 ----
        public const int CUSTOM_ACT_MONEYTREE_PANEL = 33190;    // 三嵌套 ShowList/CumulateReward/Shop
        public const int CUSTOM_ACT_MONEYTREE_DRAW = 33191;     // 服务端同号双子句(HOLY_SUMMON 精确+通用兜底)
        public const int CUSTOM_ACT_MONEYTREE_CUMULATE = 33192;
        public const int CUSTOM_ACT_MONEYTREE_SHOP = 33168;     // 树商店兑换
        public const int CUSTOM_ACT_MONEYTREE_CURRENCY = 33231; // 契约点/货币展示
        public const int CUSTOM_ACT_FTVACTIVE_PANEL = 33193;
        public const int CUSTOM_ACT_FTVACTIVE_SUBMIT = 33194;
        public const int CUSTOM_ACT_FTVACTIVE_SERVER_CLAIM = 33195;
        public const int CUSTOM_ACT_FTVACTIVE_TRIGGER_PUSH = 33196; // **recv-only** 广播
        public const int CUSTOM_ACT_SAIBO_PANEL = 33165;
        public const int CUSTOM_ACT_SAIBO_STAGE = 33166;   // ErrorCode 开头但含 Buy 尾字段
        public const int CUSTOM_ACT_SAIBO_DRAW = 33167;
        public const int CUSTOM_ACT_BINDDIAMOND_PANEL = 33130;
        public const int CUSTOM_ACT_BINDDIAMOND_DRAW = 33131;
        public const int CUSTOM_ACT_BINDDIAMOND_RECORD = 33132;
        public const int CUSTOM_ACT_REDRAIN_PANEL = 33155;      // WaveReceive 嵌套;C2S 仅 "h" SubType(无 BaseType)
        public const int CUSTOM_ACT_REDRAIN_GRAB = 33157;
        public const int CUSTOM_ACT_REDRAIN_WAVE_PUSH = 33158;  // **recv-only** 3字段;服务端 lib_red_envelopes_mod.erl:302 误用16字段调用(应33902)是已知线上bug,与本号定义无关
        public const int CUSTOM_ACT_HOLYCALL_PANEL = 33221;     // 四嵌套+RareDrawTimes
        public const int CUSTOM_ACT_HOLYCALL_RARE_DRAW = 33222;

        // ---- P5 商业礼包族:ZERO_MALL=36/FTVINVEST=62/VIPGIFT=71/DAILYSUPPLY=61/NAMEVERIFY=69/批量兑换/
        // QUESTIONNAIRE=90/MANY_RECHARGE=107/冲级/ADVERTISEMENT=111/RED_ENVELOPE_REBATE=117/CARNIVAL=118/
        // TIRED_CHARGE_POLITE=121/OVER_VIEW=126/RARE_SURFACE=128/通用号/HOTPOINT/actMarriage=25/BETA_ACT=77 ----
        public const int CUSTOM_ACT_ZEROMALL_PANEL = 33136;
        public const int CUSTOM_ACT_ZEROMALL_BUY = 33137;
        public const int CUSTOM_ACT_ZEROMALL_REBATE = 33138;
        public const int CUSTOM_ACT_FTVINVEST_BUY = 33212; // 购买;同时升级现有 On33211(见 CustomActivityController.cs)
        public const int CUSTOM_ACT_VIPGIFT_SET_GRADE = 33215;
        public const int CUSTOM_ACT_DAILYSUPPLY_LIVENESS = 33209;
        public const int CUSTOM_ACT_NAMEVERIFY_CONFIRM = 33169; // read/write 均空包
        public const int CUSTOM_ACT_BATCH_EXCHANGE = 33179;     // FTVSHOP/FTVEXCHANGE/ATLISTPURCHASE 共用;ErrorCode,Num,BaseType,SubType,Grade 序注意
        public const int CUSTOM_ACT_QUESTIONNAIRE_SUBMIT = 33236;
        public const int CUSTOM_ACT_MANYRECHARGE_PANEL = 33247;
        public const int CUSTOM_ACT_LEVEL_RUSH_GIFT = 33248;    // 冲级挑战
        public const int CUSTOM_ACT_AD_CD_LIST = 33250;         // ADVERTISEMENT 冷却列表
        public const int CUSTOM_ACT_RUSH_RANK_TOP_PLAYER_PUSH = 33251; // 头号玩家提示(331家族内部冲榜上报,与225xx pp_rush_rank 是两套,勿混淆)
        /// <summary>LIST_DUOBAO=116 夺宝积分墙阶段信息(轮21 PF 补漏批;老端独立 ListDuobaoController.ts+
        /// ListDuobaoModel.ts,非主 CustomActivityController.ts 一部分)。发/回 "hh" type,subtype。
        /// 回包(pt_332.erl write(33252):1325-1361):Type:h,Subtype:h,Score:i,TodayScore:i,Condition:s,
        /// RewardList[h+{GradeId:h,IsRare:c,Reward:ObjectList}],StageList[h+{Id:h,GotType:c}],WorldLv:i。</summary>
        public const int CUSTOM_ACT_LISTDUOBAO_STAGE = 33252;
        /// <summary>LIST_DUOBAO 排行榜(轮21 PF 补漏批)。发/回 "hh" type,subtype。
        /// 回包(pt_332.erl write(33253):1363-1397):Type:h,Subtype:h,Score:i,Rank:h,
        /// RankList[h+{Rank:h,ServerId:i,RoleId:l,RoleName:s,RoleScore:i}],SeverScore:i,ServerRank:h,
        /// ServerRankList[h+{Rank:h,ServerId:i,ServerName:s,ServerScore:i}]。</summary>
        public const int CUSTOM_ACT_LISTDUOBAO_RANK = 33253;
        /// <summary>LIST_DUOBAO 阶段奖励领取(轮21 PF 补漏批)。发 "hhh" type,subtype,reward_id。
        /// 回包(pt_332.erl write(33254):1399-1411):Type:h,Subtype:h,RewardId:h,ErrorCode:i。
        /// 对标老端 On33254:领取后**无条件**(不看 error_code)追发 33252 刷新阶段信息。</summary>
        public const int CUSTOM_ACT_LISTDUOBAO_CLAIM = 33254;
        public const int CUSTOM_ACT_REDENVELOPE_WITHDRAW = 33256; // 提现;同时升级现有 On33255(见 CustomActivityController.cs)
        public const int CUSTOM_ACT_CARNIVAL_TASK = 33258;
        /// <summary>累充有礼(TIRED_CHARGE_POLITE=121)奖励状态。On33101 扫描到 BaseType==121 的条目会追发本号
        /// (镜像老端 On33101:950-952 双追发之二,见 CustomActivityController.cs On33101)。C2S "hh" BaseType,
        /// SubType。S2C BaseType:16,SubType:16,RechargeNum:16,IsRecharge:16,List[u16计数]×{Grade:16,
        /// Condition:s,Name:s,Desc:s,RewardList[u16计数]×{FormType:8,Status:8,Reward:s}}(pt_332.erl:1503,
        /// guard Type==RECHARGE_POLITE)。</summary>
        public const int CUSTOM_ACT_TIRED_CHARGE_POLITE = 33259;
        public const int CUSTOM_ACT_OVERVIEW_REWARD = 33264;     // +镜像老端 RequireOverViewRew 遍历补拉(Model.ts:410,归 P5)
        public const int CUSTOM_ACT_RARESURFACE_CLAIM = 33265;   // Errcode 末尾;被 wxOneMoney 复用=通用分档领取
        public const int CUSTOM_ACT_REWARD_LIST_PUSH = 33257;    // **recv-only** 通用奖励列表推送,被≥3个活动模块复用
        public const int CUSTOM_ACT_WIN_LOG = 33197;             // 活动通用获奖记录,LogList+SelfList 三层嵌套
        /// <summary>嗨点(HOTPOINT,SPECIAL_ID.HOTPOINT=23)。老端注册但服务端 handle 空转({ok,Player},
        /// pp_custom_act.erl:632-639)且 33101 列表层整体过滤 HI_POINT(lib_custom_act.erl:2314 起)——
        /// **P5 只注册防御 recv,不提供发送方法**。</summary>
        public const int CUSTOM_ACT_HI_POINT_INFO = 33140;
        public const int CUSTOM_ACT_MARRIAGE_ACT_INFO = 33115;   // 完美情缘(actMarriage=25):Code开头+WeddingTypeList;C2S "hc" SubType,Opr(固定1)
        public const int CUSTOM_ACT_BETA_RECHARGE_RETURN = 33216; // 封测充值返还(BETA_ACT=77);C2S 空包

        // ---- P6 跨服+榜:KFGROUPBUY=88(改 .Kf.cs)/TopPlayer补全(改 TopPlayerController.cs)/消费鲜花榜(见上方 224xx) ----
        public const int CUSTOM_ACT_KFGROUPBUY_INFO = 33227;
        public const int CUSTOM_ACT_KFGROUPBUY_RECORD = 33228;   // FirstBuy/TailBuy 双子数组嵌套
        public const int CUSTOM_ACT_KFGROUPBUY_BUY = 33229;
        public const int CUSTOM_ACT_KFGROUPBUY_COUNT_PUSH = 33230; // **recv-only** 购买数广播
        public const int CUSTOM_ACT_KFGROUPBUY_SHOUT = 33267;      // 喊话,Code开头

        // ----- 便宜活批(440/514/339/188/513/194补全/417补全/193xx/120散件,自动循环 轮18) -----
        // 骨架轮(P0)只落协议常量+wire摘要,业务/UI留 PK1-PK5 各包实现;侦察材料见
        // scratchpad spec_cheapwins_round18.md + r18_oldclient_cheapwins.md + r18_unity_cheapwins.md,
        // erl 行号已按本轮实读 yu_server/src/pt/pt_{440,514,513,339,188,194,417,193,120}.erl 原文核对
        // (非抄侦察稿估读)。§1 死号(33903/33905/41702/41706/41710-41714/41717/41718/15104/15105/
        // 12089/12091/12024消费侧空转/18802 type96&&subtype==2 禁发/51303 禁发/51302 无回包/19405 无回执)
        // 严禁在此新增常量或改动既有寄存;交叉见各常量注释断言。

        // ---- PK1 GodBefall 谪仙临凡(440xx,yu_server pt_440.erl;16 号全活,44007-44009 空号) ----
        /// <summary>神格总列表(S2C 主推送,GAME_START/CHANGE_LEVEL 发空触发)。pt_440.erl:8,73-86:
        /// 发空;回包 GodList[u16×{IsBattle:8,GodId:32,Lv:16,Exp:32,Grade:16,Star:32,Power:64,
        /// NextLvPower:64,NextGradePower:64,NextStarPower:64,EquipList[u16×{Pos:8,GoodsId:64}]}]
        /// (二层嵌套,item_to_bin_0/item_to_bin_1)。</summary>
        public const int GODBEFALL_LIST = 44000;
        /// <summary>单只神格详情推送(44002/44005 成功后老端自动补发,老端 ts:213,250 镜像)。pt_440.erl:10-12,
        /// 88-121:发 "i"(GodId);回包同 <see cref="GODBEFALL_LIST"/> 单元素结构(11 字段,IsBattle 起
        /// EquipList 止,无 Errcode)。</summary>
        public const int GODBEFALL_ITEM_PUSH = 44001;
        /// <summary>激活神格(首只激活服务端自动上阵,44006 推送镜像)。pt_440.erl:13-15,123-133:
        /// 发 "i"(GodId);回包 Errcode:32, Power:64, GodId:32。</summary>
        public const int GODBEFALL_ACTIVATE = 44002;
        /// <summary>升级。pt_440.erl:16-18,135-155:发 "i"(GodId);回包 Errcode:32, GodId:32, Lv:16, Exp:32,
        /// Power:64, NextLvPower:64, NextGradePower:64, NextStarPower:64。</summary>
        public const int GODBEFALL_LEVEL_UP = 44003;
        /// <summary>升阶。pt_440.erl:19-21,157-175:发 "i"(GodId);回包 Errcode:32, GodId:32, Grade:16,
        /// Power:64, NextLvPower:64, NextGradePower:64, NextStarPower:64。</summary>
        public const int GODBEFALL_GRADE_UP = 44004;
        /// <summary>升星。pt_440.erl:22-24,177-195:发 "i"(GodId);回包 Errcode:32, GodId:32, Star:32,
        /// Power:64, NextLvPower:64, NextGradePower:64, NextStarPower:64。</summary>
        public const int GODBEFALL_STAR_UP = 44005;
        /// <summary>出战/放入出战槽位。pt_440.erl:25-28,197-205:发 "ci"(Pos:8, GodId:32);
        /// 回包 Errcode:32, GodId:32。成功后服务端推送 <see cref="GODBEFALL_LIST"/> 全量刷新
        /// (老端首只神格激活即自动出战,亦经此号镜像)。</summary>
        public const int GODBEFALL_SET_BATTLE = 44006;
        /// <summary>变身冷却状态查询/推送(GAME_START + SceneManager.START 发空;死亡事件亦推送)。
        /// pt_440.erl:29-30,207-215:发空;回包 SwitchCd:32, EndTime:32。⚠切场景 CD 推送服务端已注释
        /// (仅死亡事件触发),存档不当 bug 追。</summary>
        public const int GODBEFALL_SWITCH_CD = 44010;
        /// <summary>切换出战神格变身。pt_440.erl:31-32,217-225:发空;回包 Errcode:32, GodId:32
        /// (老端调用点 MainUISkillItemGod.ts:116,303)。</summary>
        public const int GODBEFALL_SWITCH = 44011;
        /// <summary>穿戴神装。pt_440.erl:33-36,227-233:发 "li"(GoodsId:64, Pos/Id:32);
        /// 回包仅 Code:32(成功无 ack,只回 <see cref="GODBEFALL_ITEM_PUSH"/> 推送——与 44013 不对称,注释存档)。</summary>
        public const int GODBEFALL_EQUIP_WEAR = 44012;
        /// <summary>脱下神装。pt_440.erl:37-40,235-241:发 "ic"(Id:32, Pos:8);回包 Code:32
        /// (成功=ack+<see cref="GODBEFALL_ITEM_PUSH"/> 双反馈,与 44012 不对称)。</summary>
        public const int GODBEFALL_EQUIP_TAKEOFF = 44013;
        /// <summary>快速合成(按规则)。pt_440.erl:41-44,243-253:发 "il"(RuleId:32, GoodsId:64);
        /// 回包 Code:32, RuleId:32, GoodsId:64。</summary>
        public const int GODBEFALL_QUICK_SYNTHESIS = 44014;
        /// <summary>战力预览。pt_440.erl:45-47,255-263:发 "i"(GodId);回包 GodId:32, Power:64(无 Code)。</summary>
        public const int GODBEFALL_POWER_PREVIEW = 44015;
        /// <summary>智能合成(GodBefallSynthesisView.ts:110,老端**自定义 WriteFmt**,勿套通用解析模板)。
        /// pt_440.erl:48-56,265-280:发 u16 计数 + {RuleId:32,Count:8}×N(m5订正:此前"变长数组[u8+...]"
        /// 计数宽度写错,GodBefallController.RequestSmartSynthesis 实际用 "h" 即 16 位);回包 Code:32,
        /// GoodsList[u16×{GoodsType:8,GoodsTypeId:64,GoodsNum:8}]。</summary>
        public const int GODBEFALL_SMART_SYNTHESIS = 44016;
        /// <summary>神格强化界面(god_type 分流,GAME_START 对 god_type 3~6 循环发)。pt_440.erl:57-59,282-292:
        /// 发 "c"(GodType:8);回包 GodType:8, CurrentLv:16, CurrentExp:32(无 Code)。</summary>
        public const int GODBEFALL_TYPE_PANEL = 44017;
        /// <summary>神格强化提交(老端**自定义 WriteFmt**)。pt_440.erl:60-70,294-312:
        /// 发 GodType:8 + u16 计数 + {GoodsTypeId:32,GoodsNum:16}×N + IsDivide:8(m5订正:计数宽度同上,
        /// GodBefallController.RequestTypeStrengthen 实际用 "h");
        /// 回包 Code:32, Args:string, GodType:8, CurrentLv:16, CurrentExp:32, IsDivide:8。</summary>
        public const int GODBEFALL_TYPE_STRENGTHEN = 44018;

        // ---- PK2 三小合包:Halo 光环(514xx,pt_514.erl,3 号全活) ----
        /// <summary>光环信息(GAME_START/DAY_CHANGE 发空)。pt_514.erl:8,20-44:发空;回包 EndTime:32,
        /// Rewards[u16×{Id:32,State:8}], SettingList[u16×{HaloId:16,Type:16,State:8}]。
        /// ⚠业务层字段名 Type 实为 wire 上的 HaloId 槽位(item_to_bin_1 命名与业务含义错位),照 erl 原名注释存档。
        /// 0 点批量刷新服务端已注释=死,存档不当 bug 追。</summary>
        public const int HALO_INFO = 51400;
        /// <summary>领取光环奖励(HaloItem.ts:55)。pt_514.erl:10-12,46-56:发 "i"(Id:32);
        /// 回包 Id:32, State:8, **Errcode:32 在末尾**(与常见 Errcode 开头习惯相反,逐号核实存档)。</summary>
        public const int HALO_REWARD_RECEIVE = 51401;
        /// <summary>光环/自动扫荡特权设置(同号双向,C2S+S2C)。pt_514.erl:13-17,58-70:发 "hhc"(Id:16, Type:16,
        /// State:8);回包同结构 + **Errcode:32 在末尾**。⚠发送点全在外系统(arena/ArenaEnterView.ts:195,199、
        /// dungeonEquip/DungeonEquipEnterView.ts:222,226、dungeonDragon/DungeonDragonEnterView.ts:190,194、
        /// godBeast/GodBeastComView.ts:215,219),HaloController 内部调用是注释掉的死代码(:39-41);
        /// 本轮只落数据层收发,4 处外系统入口 UI 闭环留尾包。</summary>
        public const int HALO_SETTING_UPDATE = 51402;

        // ---- PK2 三小合包:FairyWish 仙灵祝福(513xx,pt_513.erl,4 号全活) ----
        /// <summary>某仙灵全部信息(上线/重连/充值服务端主动推,老端对 5 个 FairyId 各推一次)。pt_513.erl:8-10,22-39:
        /// 发 "i"(FairyId);回包 FairyId:32, IsBuy:8, NodeList[u16×{NodeId:32,IsActivate:8,Combat:32}]。</summary>
        public const int FAIRYWISH_INFO = 51300;
        /// <summary>强化节点(FairyWishView.ts:228)。pt_513.erl:11-14,41-51:发 "ii"(FairyId, NodeId);
        /// 回包 FairyId:32, NodeId:32, **Code:32 在末尾**。成功后联动 OutWardBaseModel.UpdateOutWardStrongerRed
        /// (fairy_id-1000)刷新红点,红点耦合 Pet/OutWard 系统,注释存档。</summary>
        public const int FAIRYWISH_NODE_ACTIVATE = 51301;
        /// <summary>购买仙灵(pet/OutWardBaseView.ts:411 发送点)。pt_513.erl:15-17,53-57:发 "i"(FairyId);
        /// **send-only,服务端 write 子句体为空(无字段),发后不等待回包**(fire-and-forget,回执改走后续
        /// <see cref="FAIRYWISH_INFO"/> 主动推送)。⚠严禁按通用模式阻塞等待本号 ack。</summary>
        public const int FAIRYWISH_BUY = 51302;
        /// <summary>点击次数推送(recv-only,全仓无发送点)。pt_513.erl:18-19,59-72:客户端严禁发;
        /// 回包 ClickList[u16×{FairyId:32,Times:8}]。</summary>
        public const int FAIRYWISH_CLICK_PUSH = 51303;

        // ---- PK2 三小合包:RedPacket 公会红包(339xx,pt_339.erl;7 号活,33903/33905 死号不注册
        //        [老端零调用+handler 读体存在但业务空转,r18_oldclient_cheapwins §3 实证]) ----
        /// <summary>339 通用错误码(纯推送)。pt_339.erl:33-39:客户端严禁发;回包 Errcode:32。</summary>
        public const int REDPACKET_ERROR = 33900;
        /// <summary>红包列表(RedPacketMainView.ts:94;33904/33906 成功后回补)。pt_339.erl:8,41-63:发空;
        /// 回包 RedEnvelopesList[u16×item_to_bin_0(16字段:Id:64,RoleId:64,RoleName:s,Career:8,Sex:8,Turn:8,
        /// Picture:s,PictureVer:32,Type:8,Extra:32,Status:8,ReceiveStatus:8,TotalNum:16,RecipientsNum:16,
        /// Msg:s,Stime:32)] + RecordList[u16×item_to_bin_1(4字段:Id:32,RoleName:s,CfgId:32,Time:32)]。</summary>
        public const int REDPACKET_LIST = 33901;
        /// <summary>打开红包(MainItem.ts:59,64)。pt_339.erl:10-12,65-112:发 "l"(RedEnvelopesId:64);
        /// 回包 16 字段(Id..Extra 同 <see cref="REDPACKET_LIST"/> 单项前 10 字段序,ReceiveMoney 替换
        /// ReceiveStatus 位)+ RecipientList[u16×item_to_bin_2(9字段:RoleId:64,RoleName:s,Career:8,Sex:8,
        /// Turn:8,Picture:s,PictureVer:32,ReceiveMoney:32,Time:32)]。位宽已按 pt_339.erl 原文核对。</summary>
        public const int REDPACKET_OPEN = 33902;
        /// <summary>发系统/物品红包(CtrlView.ts:121 物品红包分支)。pt_339.erl:17-20,128-134:
        /// 发 "lh"(Id:64, SplitNum:16);回包仅 Errcode:32。</summary>
        public const int REDPACKET_SEND = 33904;
        /// <summary>发 VIP 红包(CtrlView.ts:119,type==100 分支)。pt_339.erl:26-30,144-154:
        /// 发 "ihs"(Money:32, SplitNum:16, Msg:string);回包 Errcode:32, Args:string。</summary>
        public const int REDPACKET_SEND_VIP = 33906;
        /// <summary>红包新增推送(S2C,与 <see cref="REDPACKET_LIST"/> 的 RedEnvelopesList 单元素同结构)。
        /// pt_339.erl:156-169:客户端严禁发;回包 RedEnvelopesList[u16×item_to_bin_3(16字段,同33901)]。</summary>
        public const int REDPACKET_NEW_PUSH = 33907;
        /// <summary>红包已领完推送(公会广播)。pt_339.erl:171-177:客户端严禁发;回包 Id:64。</summary>
        public const int REDPACKET_TAKEN_PUSH = 33908;

        // ---- PK3 FirstBlood 首杀/首通(188xx,pt_188.erl;8 号全活,18800-18807;type 收口分发:
        //        96=Boss首杀/97=副本首通[UI归DungeonPartner]/105=神符本首通[UI归DungeonRune]) ----
        /// <summary>188 通用错误码(纯推送,无 read 子句)。pt_188.erl:42-48:客户端严禁发;回包 Code:32。</summary>
        public const int FIRSTBLOOD_ERROR = 18800;
        /// <summary>首杀/首通列表(GAME_START 发 (96,1)(97,1))。pt_188.erl:8-11,50-67:发 "cc"(Type:8, Subtype:8);
        /// 回包 Type:8, Subtype:8, FirstBloodList[u16×item_to_bin_0(11字段:ShowFirstBlood:8,BossId:32,
        /// FirstBloodRoleId:64,FirstBloodRoleName:s,RoleLv:16,RoleSex:8,RoleCarrer:8,Picture:s,PictureVer:32,
        /// DressList[u16×{DressType:8,DressId:32}],RewardState:8)]。二层嵌套。</summary>
        public const int FIRSTBLOOD_LIST = 18801;
        /// <summary>领取首杀/首通奖励(MainView.ts:169)。pt_188.erl:12-16,69-85:发 "cci"(Type:8, Subtype:8,
        /// BossId:32);回包 Type:8, Subtype:8, Code:32, BossId:32, RewardList(ObjectList)。
        /// ⚠Type==96(Boss)&&Subtype==2 分支服务端 handle 注释(pp_boss_first_blood_plus.erl:47-56),
        /// **该组合严禁发送**。</summary>
        public const int FIRSTBLOOD_REWARD_CLAIM = 18802;
        /// <summary>首杀提醒推送。pt_188.erl:17-20,87-103:回包 Type:8, Subtype:8,
        /// FirstBloodRoleName:string, BossName:string。</summary>
        public const int FIRSTBLOOD_NOTICE_PUSH = 18803;
        /// <summary>神符本(type=105)专属领奖(老端发送点 dungeonRune/DungeonRuneFirstView.ts:133)。
        /// pt_188.erl:21-25,105-126:发 "cci"(Type:8, Subtype:8, DunId:32);回包 Type:8, Subtype:8, DunId:32,
        /// RewardState:8, PassRoleList[u16×item_to_bin_2(10字段:RoleId:64,RoleName:s,Rank:8,RoleLv:16,
        /// RoleSex:8,RoleCarrer:8,Picture:s,PictureVer:32,DressList[u16×{DressType:8,DressId:32}],Time:64)]。
        /// 二层嵌套。</summary>
        public const int FIRSTBLOOD_RUNE_REWARD_CLAIM = 18804;
        /// <summary>红点列表(GAME_START 发 (105,1))。pt_188.erl:26-29,128-145:发 "cc"(Type:8, Subtype:8);
        /// 回包 Type:8, Subtype:8, RedPointList[u16×{DunId:32,ShowPoint:8}]。</summary>
        public const int FIRSTBLOOD_REDPOINT_LIST = 18805;
        /// <summary>逐条详情查询(收到 <see cref="FIRSTBLOOD_LIST"/> 后按列表逐条发,老端 Controller:138 镜像)。
        /// pt_188.erl:30-34,147-159:发 "cci"(Type:8, Subtype:8, BossId:32);
        /// 回包 Type:8, Subtype:8, BossId:32, SharedStatus:8。</summary>
        public const int FIRSTBLOOD_DETAIL_QUERY = 18806;
        /// <summary>领全服归属奖(MainView.ts:152)。pt_188.erl:35-39,161-178:发 "cci"(Type:8, Subtype:8,
        /// BossId:32);回包结构同 <see cref="FIRSTBLOOD_REWARD_CLAIM"/>(Type,Subtype,Code,BossId,RewardList)。</summary>
        public const int FIRSTBLOOD_GUILD_REWARD_CLAIM = 18807;

        // ---- PK3 Festival 祭典/宝录补全(194xx,pt_194.erl;19401=FESTIVAL_INFO 已在 :719 注册,勿重复定义;
        //        本段补 19400/19402-19405) ----
        /// <summary>194 通用返回码(纯推送,无 read 子句)。pt_194.erl:25-35:客户端严禁发;
        /// 回包 Code:32, Args:string。</summary>
        public const int FESTIVAL_ERROR = 19400;
        /// <summary>领取等级奖励(AwardListItem:181/LevelAwardView:120,lv=0 代表全部)。pt_194.erl:10-12,64-72:
        /// 发 "h"(Lv:16,m5订正:此前误写"c"即8位,FestivalController.RequestLevelAward 实际用 "h");
        /// 回包 RewardList(ObjectList,无独立 Code 字段——非空即成功,对齐老端读法)。</summary>
        public const int FESTIVAL_LEVEL_AWARD_CLAIM = 19402;
        /// <summary>任务列表(type=0 代表三类全部,收到 <see cref="FESTIVAL_INFO"/>(19401)后老端自动发 type=0,
        /// Controller:140 镜像)。pt_194.erl:13-15,74-87:发 "c"(Type:8);回包 TypeList[u16×item_to_bin_1(3字段:
        /// Type:8, TaskList[u16×item_to_bin_2{TaskId:16,FinishTimes:8,CurNum:32,Status:8}], RefreshTime:32)]。
        /// 二层嵌套。</summary>
        public const int FESTIVAL_TASK_LIST = 19403;
        /// <summary>领取任务经验(TaskView:235/TaskListItem:100)。pt_194.erl:16-19,89-95:
        /// 发 "ch"(Type:8, TaskId:16);回包 Exp:32(无 Code,捎带随后 <see cref="FESTIVAL_INFO"/>+
        /// <see cref="FESTIVAL_TASK_LIST"/> 刷新)。</summary>
        public const int FESTIVAL_TASK_EXP_CLAIM = 19404;
        /// <summary>购买高阶宝录(1=豪华/2=至尊,CommodityView:586/GetRewardView:267)。pt_194.erl:20-22:
        /// 发 "c"(Type:8);**pt_194.erl 无对应 write 子句,服务端不回执**——成功与否只能等
        /// <see cref="FESTIVAL_INFO"/>(19401)刷新态,发送侧禁止阻塞等待本号 ack。</summary>
        public const int FESTIVAL_PURCHASE = 19405;

        // ---- PK4 Welfare 福利余量(417xx,pt_417.erl;签到/静默下载/在线/心悦,死号 41702/41706/
        //        41710-41714/41717/41718 不注册[服务端 handler 活但老端零消费,配置对齐铁律以客户端为准]) ----
        /// <summary>签到基础信息(DAY_CHANGE 延时 5ms 重发)。pt_417.erl:16-17,105-141:发空;回包
        /// TotalDays:8, TotalType:16, TotalState[u16×item_to_bin_1{Sum:32,Receive:8}],
        /// AccState[u16×item_to_bin_2{CheckDay:8,Receive:8}], CheckType:16, RetroTimes:8, DaysFresh:8,
        /// RemainTimes:8, CheckDay:8(9字段双平行数组)。</summary>
        public const int WELFARE_CHECKIN_INFO = 41703;
        /// <summary>签到领取(老端**自定义 ReadFmt 裸读非 GetSCMD**,勿套通用模板)。pt_417.erl:18-21,143-167:
        /// 发 "cc"(Day:8, Retroactive:8);回包 Code:32, Rewads[u16×{Style:32,TypeId:32,Count:32}],
        /// ExtraRewads[u16×同结构]。⚠位宽已按 pt_417.erl:143-167 原文核实为 Style:32(非侦察稿存疑的
        /// "Style:8?"),item_to_bin_3/item_to_bin_4 三字段均 32 位。</summary>
        public const int WELFARE_CHECKIN_CLAIM = 41704;
        /// <summary>签到补签。pt_417.erl:22-24,169-184:发 "c"(Day:8);
        /// 回包 Code:32, Rewads[u16×{Style:32,TypeId:32,Count:32}]。</summary>
        public const int WELFARE_CHECKIN_RETROACTIVE = 41705;
        /// <summary>静默下载奖励信息(GAME_START 发空)。pt_417.erl:28-29,194-204:发空;
        /// 回包 Code:32, Rewads(ObjectList)。</summary>
        public const int WELFARE_DOWNLOAD_INFO = 41707;
        /// <summary>领取静默下载奖励。pt_417.erl:30-31,206-212:发空;回包 Code:32。</summary>
        public const int WELFARE_DOWNLOAD_CLAIM = 41708;
        /// <summary>在线福利信息(GAME_START/升级到 KV 门槛时发空)。pt_417.erl:42-43,266-283:发空;
        /// 回包 Time:16, LoginTime:32, List[u16×{Id:32,State:8}]。</summary>
        public const int WELFARE_ONLINE_INFO = 41715;
        /// <summary>领取在线福利(含月卡额外档)。pt_417.erl:44-46,285-300:发 "i"(Id:32);
        /// 回包 Code:32, SendList[u16×item_to_bin_7{RewardId:32,Rewards(ObjectList),
        /// OtherRewards(ObjectList)}]。二层嵌套。</summary>
        public const int WELFARE_ONLINE_CLAIM = 41716;
        /// <summary>心悦礼包(GAME_START 按 GetWelfareWelcomeOpenState 条件发,opr=3/4 分支)。
        /// pt_417.erl:52-54,326-340:发 "c"(Opr:8);回包 Code:32, Opr:8, GiftSt:8, Reward(ObjectList)。</summary>
        public const int WELFARE_XINYUE_GIFT = 41719;

        // ---- PK4 成长福利补全(41722,延续既有 GROWTHBENEFITS_INFO=41720/GROWTHBENEFITS_TASK_UPDATE=41721 家族) ----
        /// <summary>领取成长福利任务奖励(GrowthBenefitTaskItem.ts:67)。pt_417.erl:57-59,372-382:
        /// 发 "h"(TaskId:16);回包 Errcode:32, TaskId:16, Status:8。</summary>
        public const int GROWTHBENEFITS_TASK_CLAIM = 41722;

        // ---- PK4 战力福利 CombatWelfare(41723/41724,老端独立 GrowthForceModel,新建 CombatWelfareController) ----
        /// <summary>战力福利面板(开界面/CheckFightWelfareOpen)。pt_417.erl:60-61,384-405:发空;
        /// 回包 Round:8, Times:8, Combat:64, NextCombat:64, List[u16×裸整数 RewardId:16](item_to_bin_10 单字段)。</summary>
        public const int COMBAT_WELFARE_INFO = 41723;
        /// <summary>战力福利摇奖(GrowthForceModel.FightWelfareSend)。pt_417.erl:62-63,407-421:发空;
        /// 回包 Code:32, Round:8, Times:8, RewardId:16, NextCombat:64。</summary>
        public const int COMBAT_WELFARE_DRAW = 41724;

        // ---- PK4 广告奖励 AdReward(193xx,pt_193.erl;独立 AdRewardController,不塞 Welfare;
        //        ClientProtocol.json L2102-2109) ----
        /// <summary>广告奖励推送(S2C,pt_193.erl 无对应 read 子句,客户端严禁发)。pt_193.erl:17-25:
        /// 回包 Reward(ObjectList)。</summary>
        public const int ADREWARD_REWARD_PUSH = 19301;
        /// <summary>广告冷却/开放列表(GAME_START 按 GetAdOpenState 发空)。pt_193.erl:8-9,27-40:发空;
        /// 回包 AdList[u16×{ModId:32,SubId:32,Count:8}]。</summary>
        public const int ADREWARD_LIST = 19302;
        /// <summary>上报广告观看完成/领取。pt_193.erl:10-14,42-54:发 "iii"(ModId:32, SubId:32, GradeId:32);
        /// 回包 ModId:32, SubId:32, GradeId:32, Code:32。</summary>
        public const int ADREWARD_WATCH_CLAIM = 19303;
        /// <summary>广告档位变更推送(老端 On19304 逻辑全注释=占位;pt_193.erl 无对应 read 子句,S2C only)。
        /// pt_193.erl:56-66:回包 ModId:32, SubId:32, GradeId:32。**仅注册防御 recv,不提供发送方法**。</summary>
        public const int ADREWARD_GRADE_PUSH = 19304;

        // ---- PK5 场景散件(120xx,pt_120.erl;SceneController.cs 追加分支,复用既有 SC_ 前缀。
        //        死号不注册:12089[真死,RegisterProtocal 老端已注释]/12091[服务端 wire 层无 write 定义,
        //        仅会发 cmd=0 空包]。12024 特殊:注册但自空——读完 3 字段不消费,镜像老端处理体空转) ----
        /// <summary>假人进场(单条推送)。pt_120.erl:185-186,553-564(binary_12015):回包
        /// Id:32, 0:16(保留位/占位), ServerId:16, ServerNum:16, Figure(FigureProto), X:16, Y:16, Hp:64,
        /// HpLim:64, Speed:16, Hide:8, Ghost:8, Group:64。</summary>
        public const int SC_DUMMY_ENTER = 12015;
        /// <summary>掉落包生成(触发 DEAL_WITH_SCENE_DROP_LIST_VO)。pt_120.erl:189-193:回包
        /// MonId:32, Time:16, Scene:32, DropList[u16×...](与 <see cref="SC_DROP_LIST"/>(12018) 的 DropBin
        /// 同源 17 字段结构,复用既有 DropVo 解析), X:16, Y:16, Boss:8。</summary>
        public const int SC_DROP_SPAWN = 12017;
        /// <summary>开始拾取掉落确认(注册但**自空处理**——只按序读完 3 字段保游标,不落 Model,
        /// 镜像老端处理体空转)。pt_120.erl:231-232:回包 DropId:64, RoleId:64, DropEndTime:64。</summary>
        public const int SC_DROP_PICK_CONFIRM = 12024;
        /// <summary>Boss 归属变更(按伤害最高)。pt_120.erl:222-223:回包 PlayerId:64, BossFlag:8。</summary>
        public const int SC_BOSS_OWNER = 12022;
        /// <summary>怪物喊话气泡。pt_120.erl:226-228:回包 AutoId:32, Msg:string。</summary>
        public const int SC_MONSTER_TALK = 12023;
        /// <summary>Boss 伤害榜初始全量(C2S 查询)。pt_120.erl:46-47(read "i" AutoId),235-242(write):
        /// 发 "i"(AutoId:32);回包 AutoId:32, ConfigId:32, List[u16×{RoleId:64,Name:s,ServerId:16,
        /// ServerNum:16,ServerName:s,TeamId:64,TeamPos:8,Hurt:64,AssistId:64}]。</summary>
        public const int SC_BOSS_HURT_LIST = 12025;
        /// <summary>Boss 伤害榜增量新增(S2C only)。pt_120.erl:245-248:回包 AutoId:32, ConfigId:32,
        /// RoleId:64, Name:string, ServerId:16, ServerNum:16, ServerName:string, TeamId:64, TeamPos:8,
        /// Hurt:64, AssistId:64(单条,非数组)。</summary>
        public const int SC_BOSS_HURT_ADD = 12026;
        /// <summary>Boss 伤害榜移除(S2C only)。pt_120.erl:251-253:回包 AutoId:32, ConfigId:32,
        /// RoleIdList[u16×{RoleId:64}]。</summary>
        public const int SC_BOSS_HURT_REMOVE = 12027;
        /// <summary>玩家协助 id 更改(S2C only)。pt_120.erl:256-258:回包 AutoId:32, ConfigId:32,
        /// ChangeIds[u16×{RoleId:64,AssistId:64}]。</summary>
        public const int SC_BOSS_ASSIST_CHANGE = 12028;
        /// <summary>动态区域标记(S2C only)。pt_120.erl:261-263,546-550(pack_area_mark):回包
        /// AreaMarkList[u16×{AreaId:8,ClientType:8}]。</summary>
        public const int SC_AREA_MARK = 12030;
        /// <summary>血量变化广播(战斗表现核心,含 can_receive_scene_protocal 门控)。pt_120.erl:288-291
        /// (7 参重载补 SourceSign=0,SourceId=0 后统一落 9 字段):回包 Sign:8, Id:64, Hp:64, HpLim:64,
        /// IsMinus:8(是否扣血), Change:64, BuffId:16, SourceSign:8, SourceId:64(吸血反弹流血特效来源)。</summary>
        public const int SC_HP_CHANGE = 12036;
        /// <summary>怪物:玩家求助列表全量(C2S 查询)。pt_120.erl:78-79(read "i" AutoId),316-323(write):
        /// 发 "i"(AutoId:32);回包 AutoId:32, ConfigId:32, List[u16×{AssistId:64,RoleId:64,Name:s,
        /// ServerId:16,ServerNum:16,ServerName:s}]。</summary>
        public const int SC_ASSIST_LIST = 12043;
        /// <summary>玩家求助增量新增(S2C only)。pt_120.erl:326-329:回包 AutoId:32, ConfigId:32,
        /// AssistId:64, RoleId:64, Name:string, ServerId:16, ServerNum:16, ServerName:string(单条)。</summary>
        public const int SC_ASSIST_ADD = 12044;
        /// <summary>玩家求助删除(S2C only)。pt_120.erl:332-333:回包 AutoId:32, ConfigId:32,
        /// DelAssistId:64。</summary>
        public const int SC_ASSIST_REMOVE = 12045;
        /// <summary>婚姻名/转职等 Figure 变更广播(含主角自身分支)。pt_120.erl:363-365:回包
        /// Id:64, Figure(FigureProto,pt:write_figure)。</summary>
        public const int SC_FIGURE_CHANGE = 12078;
        /// <summary>怪物 can_attack 等属性变更广播(S2C only)。pt_120.erl:371-373:回包
        /// Id:32, Attrs[u16×{Type:8,Value:32}]。</summary>
        public const int SC_MONSTER_ATTR_UPDATE = 12080;
        /// <summary>复活完成(触发 RELIVE_COMPLETE + 请求剩余复活次数,与 Relive 模块[20009/20017]联动)。
        /// pt_120.erl:383-385:回包 ReviveType:8, ScenceId:32, X:16, Y:16, ScenceName:string, Hp:64,
        /// Gold:32, BGold:32, AttProtectedTime:16(9 字段)。</summary>
        public const int SC_REVIVE_COMPLETE = 12083;
        /// <summary>安全区状态(**GapMap"小飞鞋"标注订正**:实为区域安全状态广播,非小飞鞋;小飞鞋归
        /// 12033/AutoFight 13300 家族。老端发送点绑定 SAFE_AREA_CHANGE 事件,recv 更新 role_vo.safe_area_state)。
        /// pt_120.erl:82-83(read "c" Type),392-393(write):发 "c"(Type:8);回包 PlayerId:64, Type:8。</summary>
        public const int SC_SAFE_AREA_STATE = 12085;
        /// <summary>场景玩家计数(老端发送点绑定 SCENE_PALYER_COUNT 事件,recv 更新 BossModel)。
        /// pt_120.erl:85-86(read "h" SceneId),400-401(write):发 "h"(SceneId:16);回包
        /// SceneId:16, Num:16。</summary>
        public const int SC_PLAYER_COUNT = 12087;
        /// <summary>场景内简单用户列表(C2S 裸查询)。pt_120.erl:88-89(read 裸),403-406(write),
        /// 703-709(pack_simple_user):发空;回包 Users[u16×{Platform:string,ServerNum:16,Id:64,Sex:8,
        /// Realm:8,Career:8,Lv:16,Name:string,Picture:string,PictureVer:32}]。</summary>
        public const int SC_SIMPLE_USER_LIST = 12088;
        /// <summary>公会 id 字段广播(S2C only)。pt_120.erl:439-440:回包 Sign:8, Id:64, GuildId:64。</summary>
        public const int SC_GUILD_ID_CHANGE = 12090;
        /// <summary>怪物 Buff 批量请求(老端发送点绑定 REQUEST_MONSTER_BUFF 事件)。pt_120.erl:94-100
        /// (read 变长数组),442-446(write):发变长数组[u16×{GoodsId:64}];回包
        /// List[u16×{Id:64,AerBuffList:预编码二进制(来源另一模块,原样透传)}]。</summary>
        public const int SC_MONSTER_BUFF_BATCH = 12092;

        // ----- 交易行补全(151xx,yu_server pt_151.erl / 老端 MarketController.ts,自动循环 轮19) -----
        // MARKET_ICON_INFO=15121 已在上方"便宜活批"段定义(轮18),本段补齐老端活号 17 个中除 15121 外的
        // 其余 16 个。死号 15103(协议 read/write 双缺但业务层活,发了也拿垃圾响应;read no_match 后更
        // 可能网关静默丢弃——pt_151:read 全仓无调用点[全仓 grep pt_151: 仅命中 :write,无一处 :read],
        // 网关是独立二进制,不在本仓库源码树内,无法在此继续追踪确证)/15104(搜索,老端零调用已自砍)/
        // 15105(推荐价,老端零调用已自砍,服务端该错误分支本身还在产 bug:1元素vs3字段定义)/
        // 15107(P2P上架,do_handle 整段注释)/15110(P2P列表,注释+write缺)/15113(P2P红点,触发链依赖已
        // 注释的15107)严禁在此新增常量。服务端统一 open_lv=90 门槛(pp_sell.erl:22-29),90 级以下静默
        // 丢包不回。wire 已逐字段核对 pt_151.erl 原文(行号见各常量注释),详见 r18_server_market.md 台账。
        /// <summary>15100 通用错误码推送(S2C only)。pt_151.erl:81-91:回包 Errcode:32, Args:string。</summary>
        public const int MARKET_ERROR_PUSH = 15100;
        /// <summary>15101 一级分类挂单数量。pt_151.erl:8-10(read "i" Type),93-108(write):
        /// 发 "i"(Type:32);回包 Type:32, SellList[u16×{Subtype:32,SellNum:32}]。</summary>
        public const int MARKET_LEVEL1_LIST = 15101;
        /// <summary>15102 二级列表商品(9字段,EquipExtraAttr 二层嵌套)。pt_151.erl:11-17(read "iiccc"
        /// Type,Subtype,Stage,Star,Color;99=不筛选),110-127(write):发 "iiccc";回包 Type:32,Subtype:32,
        /// GoodsList[u16×{Id:64,PlayerId:64,TypeId:32,GoodsNum:32,Rating:32,OverallRating:32,UnitPrice:32,
        /// SellType:8,EquipExtraAttr[u16×{Color:8,TypeId:8,AttrId:16,AttrVal:32,PlusInterval:8,
        /// PlusUnit:32}]}]。</summary>
        public const int MARKET_GOODS_LIST = 15102;
        /// <summary>15106 上架。pt_151.erl:25-30(read "liic" GoodsId,GoodsNum,Price,IsShout),156-162
        /// (write):发 "liic";回包 Errcode:32。成功后老端重发 <see cref="MARKET_SHELF_LIST"/> 刷新
        /// (ts:163)。</summary>
        public const int MARKET_SELL_UP = 15106;
        /// <summary>15108 下架。pt_151.erl:31-36(read "clii" SellType,Id,TypeId,GoodsNum),164-170
        /// (write):发 "clii"(SellType 老端恒传1);回包 Errcode:32。成功后老端重发
        /// <see cref="MARKET_SHELF_LIST"/> 刷新(ts:177)。</summary>
        public const int MARKET_SELL_DOWN = 15108;
        /// <summary>15109 我的上架列表。pt_151.erl:37-38(read 裸),172-185(write):发空;回包
        /// GoodsList[u16×9字段](同 <see cref="MARKET_GOODS_LIST"/> 元素结构,item_to_bin_5)。
        /// 15106/15108 成功后老端回补。</summary>
        public const int MARKET_SHELF_LIST = 15109;
        /// <summary>15111 购买。pt_151.erl:39-48(read "cliiliii" SellType,Id,Type,Subtype,SellerId,
        /// TypeId,GoodsNum,UnitPrice),187-201(write):发 "cliiliii"(SellType 老端恒传1);回包
        /// Errcode:32,SellType:8,Id:64,Type:32,Subtype:32(5字段,无 write_string)。成功后老端重发
        /// <see cref="MARKET_BUY_TIMES"/> 刷新(ts:199)。</summary>
        public const int MARKET_BUY = 15111;
        /// <summary>15112 交易记录。pt_151.erl:49-50(read 裸),203-216(write):发空;回包
        /// RecordList[u16×{TypeId:32,GoodsNum:32,Rating:32,OverallRating:32,Type:8,Tax:32,Price:32,
        /// Time:32,EquipExtraAttr[u16×同 15102 六字段]}]。</summary>
        public const int MARKET_RECORD_LIST = 15112;
        /// <summary>15114 购买次数。pt_151.erl:51-52(read 裸),218-231(write):发空;回包
        /// TimesList[u16×{Type:8,Times:8,TimesLimit:8}]。</summary>
        public const int MARKET_BUY_TIMES = 15114;
        /// <summary>15115 发起求购。pt_151.erl:53-57(read "iii" TypeId,GoodsNum,UnitPrice),233-255
        /// (write):发 "iii";回包 Errcode:32,Id:64,PlayerId:64,RoleName:string,TypeId:32,GoodsNum:16
        /// (与 read 侧 32 位不同,write 子句原文核实),UnitPrice:32,Time:32。</summary>
        public const int MARKET_PLZ_CREATE = 15115;
        /// <summary>15116 撤销求购。pt_151.erl:58-60(read "l" Id),257-265(write):发 "l";回包
        /// Errcode:32,Id:64。</summary>
        public const int MARKET_PLZ_CANCEL = 15116;
        /// <summary>15117 出售给求购单。pt_151.erl:61-67(read "lliii" Id,BuyerId,TypeId,GoodsNum,Price),
        /// 267-277(write):发 "lliii";回包 Errcode:32,Id:64,GoodsNum:32。</summary>
        public const int MARKET_PLZ_SELL = 15117;
        /// <summary>15118 求购列表(全服,分页)。pt_151.erl:68-71(read "hh" PageNo,PageSize),279-298
        /// (write):发 "hh";回包 PageTotal:16,PageNo:16,PageSize:16,SeekList[u16×{Id:64,SerId:64,
        /// ServerNum:64(独例),PlayerId:64,RoleName:string,TypeId:32,GoodsNum:16,UnitPrice:32,
        /// Time:32}]。</summary>
        public const int MARKET_PLZ_LIST_ALL = 15118;
        /// <summary>15119 我的求购列表。pt_151.erl:72-73(read 裸),300-313(write):发空;回包
        /// SeekList[u16×{Id:64,TypeId:32,GoodsNum:16,UnitPrice:32,Time:32}](比 15118 少 SerId/
        /// ServerNum/RoleName)。</summary>
        public const int MARKET_PLZ_LIST_MINE = 15119;
        /// <summary>15120 挂单/求购删除推送(S2C only)。pt_151.erl:315-327:回包 SellType:8,Type:32,
        /// Subtype:32,Id:64。SellType==1(挂单)/3(求购)分流,2(P2P)死路径。</summary>
        public const int MARKET_SELL_DELETE_PUSH = 15120;
        /// <summary>15122 喊话。pt_151.erl:76-78(read "l" SellId),337-347(write):发 "l";回包
        /// Errcode:32,SellId:64,CdTime:32。老端成功分支为空(ts:299-307),只在失败分支显码。</summary>
        public const int MARKET_SHOUT = 15122;

        // ----- 时装 Fashion(pt_413,yu_server src/fashion/;老端 commonController/FashionController.ts。
        // 第21轮 PA/PD:接管时装主体及部位升级/套装协议。pos:1=衣服 3=头饰(data_fashion.erl:19275
        // get_pos_id_list()->[1,3];pos2武器/pos4足部已死,config_fashion_model 无对应数据佐证)。
        // ⚠死号严禁发:41307 全死(pp_fashion.erl 无 41307 handle 子句,落 catch-all 只 ?PRINT 不回包;
        // 唯一 write 调用点 lib_fashion.erl:375 已注释;老端 FashionController.ts:44-45 有 send 分支但
        // 全仓零 Fire 且 RegisterProtocal 列表:419-431 不含 41307)。
        // 41310 客户端侧死(服务端 pp_fashion.erl:298-317 会回,但老端零发包点且 RegisterProtocal 列表
        // 不含 41310,收到也丢弃)。
        // 41311 上行死、仅活下行:FashionController.ts:46-47 的 send 分支全仓零 Fire(不发);但
        // RegisterProtocal(41311,On41311)(:426)存在且有实体——服务端会在穿脱/激活/染色/神殿觉醒后
        // 主动广播(lib_fashion_event.erl:22、lib_fashion.erl:177、lib_temple_awaken.erl:1471),
        // 必须注册接收并处理形象变更,只是本端永不主动请求它。 -----
        /// <summary>41300 全量拉取(发空;老端由 GoodsModel.CREATE_BAG_LIST_FINISH 触发 Fire(SCMD_REQUEST,41300),
        /// FashionController.ts:97;本端简化为 EVT_GAME_START 后拉取,配置就绪即可,无需等背包)。
        /// 回包 pt_413.erl:83-87 + item_to_bin_0/1/2(:310-360):
        /// Code:i, PosList[u16×{PosId:c, WearFashionId:i, PosLv:h, PosUpgradeNum:i,
        /// FashionList[u16×{FashionId:i, FashionStarLv:h, NowColorId:c,
        /// ColorList[u16×{ColorId:c, FashionStarLv:h}]}]}]。</summary>
        public const int FASHION_INFO_ALL = 41300;
        /// <summary>41301 染色解锁(发 "cicc" PosId,FashionId,ColorId,Type;老端 FashionMainView.ts:94 **只发 Type=2**
        /// =解锁颜色——Type=1 染色分支服务端未用,pp_fashion.erl:64 注释原文「%% 染色（未用）」,严禁发 Type=1);
        /// 回包 Code:i, PosId:c, FashionId:i, ColorId:c, Type:c。Code==1 成功后 color_list 追加 {ColorId,1}
        /// (pp_fashion.erl:133)。</summary>
        public const int FASHION_UNLOCK_COLOR = 41301;
        /// <summary>41302 穿戴(发 "cic" PosId,FashionId,ColorId;41304 激活成功后老端也会自动补发这个 ColorId=0,
        /// FashionController.ts:288);回包 Code:i, PosId:c, FashionId:i, ColorId:c。</summary>
        public const int FASHION_WEAR = 41302;
        /// <summary>41303 卸下(发 "ci" PosId,FashionId;⚠也会被动收到——穿神殿/套装收集/天启会顶掉时装,
        /// 服务端 lib_fashion_api.erl:48 对 pos∈[1,3] 各主动推一个非本人请求的 41303,Model 须能处理"被动卸下");
        /// 回包 Code:i, PosId:c, FashionId:i。</summary>
        public const int FASHION_TAKE_OFF = 41303;
        /// <summary>41304 激活(发 "ci" PosId,FashionId;成功后老端自动 Fire(SCMD_REQUEST,41302,PosId,FashionId,0)
        /// 补穿,FashionController.ts:288);回包 Code:i, PosId:c, FashionId:i。</summary>
        public const int FASHION_ACTIVE = 41304;
        /// <summary>41305 衣服部位升级(变长请求:PosId:c,GoodsCount:h,
        /// GoodsList[N×{GoodsInstanceId:l,GoodsNum:h}];GoodsInstanceId 是64位背包实例id,不是物品类型id)。
        /// 回包 Code:i,PosId:c,PosLv:h,PosUpgradeNum:i。</summary>
        public const int FASHION_POSITION_UPGRADE = 41305;
        /// <summary>41306 基础色(color 0)进阶(发 "cic" PosId,FashionId,ColorId;ColorId 恒传当前 now_color_id;
        /// ⚠服务端 lib_fashion_check.erl:141 对未解锁颜色 keyfind 会 badmatch 崩进程——只对已在 color_list
        /// 里的颜色发);回包 Code:i, PosId:c, FashionId:i, ColorId:c, FashionStarLv:h。</summary>
        public const int FASHION_UPGRADE_BASE = 41306;
        /// <summary>41312 时装战力(发 "ci" PosId,FashionId;41304/41306/41316 成功后服务端会自动内部再调一次也推这个,
        /// pp_fashion.erl:238/:294);回包(⚠**无 Code 首位**,与其余 413xx 惯例相反):
        /// PosId:c, FashionId:i, ColorPowerList[u16×{ColorId:c, ColorPower:l, NextColorPower:l}]。</summary>
        public const int FASHION_POWER = 41312;
        /// <summary>41313 套装全量信息(发空;服务端在套装符合数量变化时也会主动推送)。回包无 Code:
        /// FashionSuit[u16×{SuitId:c,Lv:c,ActiveNum:c,ConformNum:c,Power:i,NextPower:i}]。</summary>
        public const int FASHION_SUIT_INFO = 41313;
        /// <summary>41314 激活套装档位(发 "cc" SuitId,ActiveNum;老端 ActiveNum 只发2/4)。回包注意 Code 在第三位:
        /// SuitId:c,ActiveNum:c,Code:i,Power:i,NextPower:i。</summary>
        public const int FASHION_SUIT_ACTIVATE = 41314;
        /// <summary>41315 套装升阶(发 "c" SuitId)。回包注意 Code 在第三位:
        /// SuitId:c,Lv:c,Code:i,Power:i,NextPower:i。</summary>
        public const int FASHION_SUIT_UPGRADE = 41315;
        /// <summary>41316 彩色(非 0 色)进阶(发 "cic" PosId,FashionId,ColorId;与 41306 同结构不同协议号/字段位置);
        /// 回包 PosId:c, FashionId:i, ColorId:c, Lv:c(⚠8位,41306 对应字段 FashionStarLv 是 16位), Code:i
        /// (⚠**Code 在最后**,与 41300-41306 惯例相反)。</summary>
        public const int FASHION_UPGRADE_COLOR = 41316;
        /// <summary>41311 外观形象增量广播(⚠仅活下行,严禁发上行——见上方族注释);
        /// 回包 RoleId:l, FashionEquip[u16×{PartPos:c, FashionModelId:i, FashionChartletId:c}]
        /// (item_to_bin_4,pt_413.erl:368)。对标老端 On41311(FashionController.ts:337-344)
        /// role_vo.ChangeVar("fashion_model_list", scmd.fashion_equip)。</summary>
        public const int FASHION_FIGURE_PUSH = 41311;

        // ----- 公会晚宴 GuildActivity(pt_402 主体,yu_server src/guild_act/;老端 commonController/
        // GuildActivityController.ts。自动循环 轮22 PK1:26 号(公会BOSS 40201/03/04/08/09 + 晚宴主流程
        // 40211/12/14/17/20/21/22 + 篝火/答题/龙魂/菜肴 40255/56/57/58/59/60/62/64/65/66/67 + 族错误出口
        // 40200)。结社守卫(40230-32)按主控裁决2 全部 killlist,不在此列;40263(召唤远古巨龙)三层死透
        // (c2s pp_guild_act.erl:624-643 整段注释,S2C 唯一调用链 lib_guild_feast.erl:1096-1127→
        // mod_guild_feast_mgr.erl:240-241,1240-1247 均只被该已注释 c2s 触达,kf 模块 mod_kf_guild_feast_topic
        // 核实与 dragon/fire 无关——全仓 grep 零命中,无接管),发送/接收均不实现,不建常量,归 PK3 killlist。 -----
        /// <summary>40200 族错误出口(Errcode:32,通用错误码包,pp_guild_act.erl send_error_code/2 到处调用)。
        /// 老端 on40200→Util.ErrorCodeShow。纯 S-only,无 c2s。</summary>
        public const int GUILDFEAST_ERROR = 40200;
        /// <summary>40201 公会BOSS信息(Etime:32,AutoDrumupTime:32,DunId:32,GbossMat:32,RemainTimes:8,
        /// IsAuto:8,IsDrumToday:8,MonState:8)。C2S 空包,GAME_START 后老端恒发(ts:83)。
        /// server handle(40201,_)(pp_guild_act.erl:27)→mod_guild_boss:send_gboss_info。</summary>
        public const int GUILDFEAST_BOSS_INFO = 40201;
        /// <summary>40203 兽粮被动推送(AddGbossMat:32,GbossMat:32)。**c2s"上交兽粮"已注释**
        /// (pp_guild_act.erl:47-74),纯内部触发(mod_guild_boss.erl:311,由 lib_gift_new.erl:541/
        /// lib_goods_api.erl:670,674 拾取/使用神兽诱饵物料时调用),本端只接收不发送。</summary>
        public const int GUILDFEAST_BOSS_MAT_ADD = 40203;
        /// <summary>40204 召集公会BOSS(Errcode:32,RoleId:64)。C2S 空包(会长/副会长权限)。
        /// server handle(40204,_)(pp_guild_act.erl:77+)→mod_guild_boss:drum_up。</summary>
        public const int GUILDFEAST_BOSS_CALL = 40204;
        /// <summary>40208 BOSS结算推送(GbossResult:8,FixReward[Gtype:8,GtypeId:32,Gnum:**16**独例],
        /// AuctionReward[同结构])。无独立 c2s(战斗结算内部推送,mod_guild_boss.erl:971)。</summary>
        public const int GUILDFEAST_BOSS_RESULT = 40208;
        /// <summary>40209 设置自动召唤(Errcode:32,IsAuto:8)。C2S 带 IsAuto:8。
        /// server handle(40209,[IsAuto])(pp_guild_act.erl:137)。</summary>
        public const int GUILDFEAST_BOSS_AUTO = 40209;
        /// <summary>40211 晚宴活动信息(核心驱动号:Status:8,ActEndTime:32,Etime:32,Stage:8)。C2S 空包,
        /// GAME_START 后老端恒发+切场景重发(ts:85,109,161)。server handle(40211,_)(L152)→
        /// mod_guild_feast_mgr:send_act_info。是驱动整个晚宴 UI 状态机的核心号(老端 CheckOpenView 按
        /// Stage 决定弹哪个面板,本轮 UI 不接,数据先落地)。</summary>
        public const int GUILDFEAST_ACT_INFO = 40211;
        /// <summary>40212 进入晚宴场景(Errcode:32)。C2S 空包。server handle(40212,_)(L157)多重校验
        /// (GM关闭/场景/等级/公会等级)。errcode==1 老端重发 40211(ts:160-161)。</summary>
        public const int GUILDFEAST_ENTER_SCENE = 40212;
        /// <summary>40214 积分排行榜(IsKf:8,GuildList[u16×{GuildId:64,ServerNum:32,GuildName,GuildScore:32,
        /// GuildRank:16}],RankList[u16×{SerId:32,SerNum:32,Rank:16,Name,Score:32}])。C2S 空包。
        /// server handle(40214,_)(L200)→send_quiz_rank。</summary>
        public const int GUILDFEAST_RANK_INFO = 40214;
        /// <summary>40217 答题信息(Status:8,Etime:32,No:32,Id:64)。C2S 空包(须在晚宴场景)。
        /// server handle(40217,_)(L231)。</summary>
        public const int GUILDFEAST_QUEST_INFO = 40217;
        /// <summary>40218 退出晚宴场景。**仅 C2S 空包(退出场景请求,有真实副作用:
        /// lib_scene:player_change_scene+mod_guild_feast_mgr:exit_scene,pp_guild_act.erl:242-251),
        /// 严禁注册接收**——全仓 grep "write(40218" 只命中 pt_402.erl:288-294 的函数定义本身,
        /// 无任何调用点(mod_guild_feast_mgr.erl:416-433 的 exit_scene 内部实现里没有回写 40218);
        /// 老端 GuildActivityController.ts:177-184 注册的 on40218 是永远不会触发的死接收(镜像
        /// "40054 单向生效无回执"先例,主控裁决3)。</summary>
        public const int GUILDFEAST_EXIT_SCENE = 40218;
        /// <summary>40220 个人积分排行(Rank:16,Point:64)。C2S 空包。server handle(40220,_)(L268)。</summary>
        public const int GUILDFEAST_MY_RANK = 40220;
        /// <summary>40221 小游戏是否已完成(IsFinish:8)。C2S 空包。server handle(40221,_)(L274)。</summary>
        public const int GUILDFEAST_MINI_GAME_STATUS = 40221;
        /// <summary>40222 当日轮换小游戏类型(GameType:8,1=答题/2=消消乐)。C2S 空包。
        /// server handle(40222,_)(L279)。</summary>
        public const int GUILDFEAST_GAME_TYPE = 40222;
        /// <summary>40255 经验/贡献推送(Type:8,Exp:64)。**纯被动收,老端从未主动发送**(全仓 zero
        /// Fire(REQUEST_PROTO,40255)/SendFmtToGame(40255)),真正驱动号是玩家在晚宴场景获得经验时
        /// player/lib_player.erl:460 内部直调 mod_guild_feast_mgr:send_exp_by_cast,不经 c2s。
        /// ⚠pp_guild_act.erl 有两条 handle(40255,...)子句(L463 无判别性 [_Type] 在前,L522 篝火经验
        /// [Type==1] 在后)——Erlang 子句顺序匹配,第二条永久不可达(r22 侦察已证实,轮13a 疑点坐实),
        /// 但因老端根本不发起 c2s 40255,该遮蔽不影响现网表现,本端按"只会推 Type=1"实现接收即可。
        /// 不提供发送方法。</summary>
        public const int GUILDFEAST_EXP_PUSH = 40255;
        /// <summary>40256 火苗信息(Wave:32,NextTime:64)。C2S 空包。server handle(40256,[])(L474)。</summary>
        public const int GUILDFEAST_FIRE_INFO = 40256;
        /// <summary>40257 采集火苗奖励推送(RewardList,pt:write_object_list 标准格式 u16计数+
        /// {Type:8,TypeId:32,Num:32})。**c2s"点击火苗采集"已死**——pp_guild_act.erl:487-496 整段注释,
        /// 唯一 c2s 触发链 mod_guild_feast_mgr:collect_fire(RoleId,FireId,GuildId)(L164-165)→cast→
        /// do_handle_event(L702-739)无任何存活调用点(唯一调用方就是已注释的 c2s)。**但 S2C 推送本身另有
        /// 存活触发链**:lib_mon_event.erl:110,239(通用怪物击杀事件)→mod_guild_feast_mgr:kill_boss→
        /// lib_guild_feast.erl:905 collect_fire(MonId,AtterId,State)→pt_402:write(40257,...)(:938)——
        /// 即当前版本"采集火苗"已改为"在场景里击杀火苗怪"触发,不再是点击 c2s;老端
        /// GuildActivityController.ts 也只注册 on40257 接收,全仓 zero 主动发送,与此吻合。本端按纯被动
        /// 推送实现接收,不提供发送方法(主控裁决4 核实结论)。</summary>
        public const int GUILDFEAST_FIRE_REWARD = 40257;
        /// <summary>40258 阶段推送(Stage:8,Time:16)。无对应 c2s(pt_402.erl 无 read(40258)子句,纯推送)。
        /// 老端 on40258 取出即弃(占位),本端如实落地供尾包消费。</summary>
        public const int GUILDFEAST_STAGE_PUSH = 40258;
        /// <summary>40259 答题(推送 Status:8;C2S 带 Answer:8)。server handle(40259,[Answer])(L500)→
        /// mod_guild_feast_mgr:quiz_answer,与 pp_chat.erl:115,147 共享同一 quiz_answer 函数(答题也可经
        /// 聊天频道触发,老端未走该路径,本端不复刻聊天侧触发)。</summary>
        public const int GUILDFEAST_ANSWER = 40259;
        /// <summary>40260 龙魂信息(DragonSpirit:64)。C2S 空包。server handle(40260,[])(L509)。
        /// 40261 购买成功后由 add_dragon_spirit 内部广播本号刷新(mod_guild_feast_mgr.erl:855-858)。</summary>
        public const int GUILDFEAST_DRAGON_INFO = 40260;
        /// <summary>40261 购买龙魂(**仅 C2S**,字段名沿用 pt_402.erl 的"DragonSpirit:64"但语义是购买数量
        /// Num——lib_guild_feast.erl:561-579 buy_dragon_spirit/4 签名即为 Num)。pt_402.erl 全仓无
        /// write(40261 子句,老端 on40261 也是空函数占位(ts:217-219),**严禁注册接收**:失败走 40200
        /// 通用错误包(:578),成功由 mod_guild_feast_mgr:add_dragon_spirit 内部广播 40260 刷新
        /// (:575→mod_guild_feast_mgr.erl:855-858),不是遗漏。</summary>
        public const int GUILDFEAST_BUY_DRAGON_SPIRIT = 40261;
        /// <summary>40262 战斗结果推送(Status:8,RewardList 标准ObjectList)。无独立 c2s(战斗结算内部推送)。
        /// 老端对应弹窗分支已被注释(ts:224-228),仅 model.SetResultInfo 落地,UI 未接。</summary>
        public const int GUILDFEAST_RESULT_INFO = 40262;
        /// <summary>40264 购买菜肴(Code:32,FoodList[u16×{Type:8,Status:8}])。C2S 带 Type:8。
        /// server handle(40264,[Type])(L532)含高级菜肴每公会限购一次校验。</summary>
        public const int GUILDFEAST_FOOD_BUY = 40264;
        /// <summary>40265 菜肴状态(FoodList[u16×{Type:8,Status:8}])。C2S 空包。
        /// server handle(40265,[])(L599)。</summary>
        public const int GUILDFEAST_FOOD_STATUS = 40265;
        /// <summary>40266 答题积分排名奖励(Rank:32,Reward 标准ObjectList)。**纯 S-only 推送,无对应 c2s**
        /// (pt_402.erl 无 read(40266)子句)。触发链:mod_guild_feast_mgr.erl:797-814 阶段切换时
        /// {'pre_enter_dragon',...}内部事件(非kf分支)→lib_guild_feast:quest_calc_reward(L390-)→
        /// lib_player:apply_cast(...,send_topic_reward_in_ps,...)(L398-399)→
        /// send_topic_reward_in_ps/3(L1430-1443)→pt_402:write(40266,...)(:1437),全链存活确认;
        /// kf(跨服)分支改走 mod_kf_guild_feast_topic:end_act(不影响本号非kf路径的存活判定)。</summary>
        public const int GUILDFEAST_RANK_REWARD = 40266;
        /// <summary>40267 经验加成状态(Ratio:32)。C2S 空包。server handle(40267,[])(L612)。</summary>
        public const int GUILDFEAST_EXP_BUFF = 40267;

        // ===================================================================================
        // 232 星宿核心(pp_constellation_equip,pt_232.erl 直接处理段;族路由 mod_server.erl:720
        // "232"→pp_constellation_equip:handle/3。开放门禁:OpenDay&gt;=open_day_limit(=0) 且
        // Figure.lv&gt;=open_lv(config_constellation_kv,=560),不满足静默 skip——**唯二例外 23250/23255**
        // 门禁外也放行(pp_constellation_equip.erl:40 `Cmd==23250 orelse Cmd==23255`)。
        // 23204(单星宿总属性查询)按主控裁决1 killlist:老端注册 On23204 但全仓零发送点(请求方向死),
        // 响应永不触发——不发不收,本段不建常量(同 40218/40263 先例"永不触发的接收严禁注册")。
        // 星宿锻造(chc,PK2 段)分段追加于本段之后,见 <see cref="STARFORGE_STREN_INFO"/> 起。
        // ===================================================================================

        /// <summary>23200 族错误出口(ErrorCode:32,ErrorCodeArgs:string)。全家族 send_error 统一出口
        /// (pp_constellation_equip.erl:764-768 send_error/2),23202/23203/23205/23207(隐含)/23209/23250/
        /// 23254/23255/23257 等"自身无 Code 字段"的号失败时均走这里。⚠老端 On23200 特判
        /// error_code==1500081(err150_compose_fail)时 Fire COM_FAIL 而非通用 ErrorCodeShow——但全仓 grep
        /// pp_constellation_equip.erl 未发现任何 send_error(...,err150_compose_fail) 调用点(该错误码两处
        /// 用例都直接 pt_232:write(23252,...)自己的号,见 <see cref="STAREQUIP_COMPOSE"/>),故此特判在当前
        /// 服务端实现下是死分支,本端仍照抄镜像(不删,防止以后服务端改动激活)。</summary>
        public const int STAREQUIP_ERROR = 23200;

        /// <summary>23201 总览(C2S 空包;S2C 无 Code)。响应 write(23201,[TotalStar:16,ItemList_Len:16+
        /// ItemList[item_to_bin_0:{Page:32,Power:64,NormalNum:8,SpecialNum:8,Attr(attr_list),IsActive:8}]])。
        /// ⚠TotalStar 是 **u16**,不是 32 位。服务端 do_handle(23201,PS,[])(pp_constellation_equip.erl:49-59)
        /// 无失败分支,恒回本号。老端 StarEquipController.ts:147-172 on23201——**关键副作用**:落地前若
        /// model.totalStar!=scmd.total_star,额外补发 <see cref="STAREQUIP_STAR_MASTER_INFO"/>(ts:150-151);
        /// 触发链见 GAME_START(ts:470)/CHANGE_LEVEL==580(ts:461,阈值取自 ConfigFuncOpenCondition
        /// ["StarEquipView"].open_lv,与服务端门禁 560 是两个不同来源的数字,本端用 FuncOpenConfig 共享设施
        /// 做等价的"跨越开放阈值"判定,不完全照抄老端"=="精确匹配,见 StarEquipController 类注释)。</summary>
        public const int STAREQUIP_OVERVIEW = 23201;

        /// <summary>23202 穿戴(C2S "lic" GoodsAutoId:64,ConstellationId(页):32,IsReplace:8;S2C **无 Code**,
        /// 仅成功才回本号)。响应 write(23202,[GoodsId:64,GoodsTypeId:32])。失败(装备不存在/校验不过/
        /// 事务失败)一律 send_error→<see cref="STAREQUIP_ERROR"/>,不会带 Code 回本号——本号本身恒等于成功。
        /// 服务端 do_handle(23202,...)(pp_constellation_equip.erl:62-195),写在 :156。
        /// 老端请求 StarChangeEquipItem.ts:45,响应 StarEquipController.ts:174-207 on23202(额外拉
        /// config_constellation_strength/enchantment/spirit 三张 PK2 配表转交 chcModel.UpdateEquipHandle,
        /// 本轮数据层不移植该 UI 联动,留尾包/PK2 对接)。</summary>
        public const int STAREQUIP_WEAR = 23202;

        /// <summary>23203 卸下(C2S "ic" ConstellationId(页):32,Pos:8;S2C **无 Code**,仅成功回本号)。
        /// 响应 write(23203,[GoodsId:64,GoodsTypeId:32])(卸下的那件装备的 id/type_id)。失败同样一律
        /// send_error→23200。服务端 do_handle(23203,...)(pp_constellation_equip.erl:198-264),写在 :248。
        /// 老端请求 StarEquipToolTip.ts:245,响应 StarEquipController.ts:209-242 on23203(与 23202 共享同一套
        /// UpdateEquipHandle 联动,同上不移植)。</summary>
        public const int STAREQUIP_UNWEAR = 23203;

        /// <summary>23205 星级大师界面(C2S 空包;S2C 无 Code)。响应 write(23205,[Level:16,MaxLevel:16,
        /// Star:16,Power:32])。服务端 do_handle(23205,PS,[])(pp_constellation_equip.erl:280-291),失败
        /// (StarStaus 记录形状不对,实践中不会发生)走 send_error→23200。老端 StarEquipController.ts:252-275
        /// on23205——星数下降到低于已点亮的大师等级时弹窗提示,并按红点/功能图标刷新(UI 派生逻辑,本轮
        /// 数据层不移植)。触发链见 <see cref="STAREQUIP_OVERVIEW"/> 注释与 GAME_START/CHANGE_LEVEL。</summary>
        public const int STAREQUIP_STAR_MASTER_INFO = 23205;

        /// <summary>23206 星级大师升级(C2S "h" StarLevel:16;S2C **自带 Code**,不走 23200——服务端两个分支
        /// 都直接写本号:失败 write(23206,[Code:32,0,0]),成功 write(23206,[?SUCCESS,StarLevel:16,
        /// NewPower:32]))。服务端 do_handle(23206,...)(pp_constellation_equip.erl:294-323),写在 :303/:316。
        /// 老端请求/响应 StarEquipController.ts:277-296 on23206。</summary>
        public const int STAREQUIP_STAR_MASTER_UP = 23206;

        /// <summary>23207 吞噬界面信息(C2S 空包;S2C 无 Code,do_handle 无失败分支恒回本号)。响应
        /// write(23207,[Level:16,Exp:32,Power:32,Color:8,Star:8])。服务端
        /// pp_constellation_equip.erl:326-333,写在 :331。老端 StarEquipController.ts:298-308 on23207。
        /// 触发链:GAME_START 恒发(ts:471);CHANGE_LEVEL==580 补发(ts:463,与 23201/23205 同批)。</summary>
        public const int STAREQUIP_DEVOUR_INFO = 23207;

        /// <summary>23208 吞噬品质/星级筛选(C2S "cc" NewColor:8,NewStar:8;S2C **Code 在末尾**——
        /// write(23208,[NewColor:8,NewStar:8,?SUCCESS:32]),失败经 send_error→23200,本号自身恒为成功)。
        /// 服务端校验 NewColor/NewStar 是否在 config_constellation_kv 的 decompose_color_status/
        /// decompose_star_status 枚举内(pp_constellation_equip.erl:336-353,写在 :348)。
        /// 老端 StarEquipController.ts:310-321 on23208。</summary>
        public const int STAREQUIP_DEVOUR_TAB = 23208;

        /// <summary>23209 吞噬执行(C2S 变长数组:WriteBegin(23209)+WriteFMT("h",count)+逐个
        /// WriteFMT("l",materialGoodsAutoId),对标 StarEquipController.ts:61-69 REQUEST_DEVOUR;
        /// 服务端 read(23209,Bin0) 用 pt:read_array 读回同一形状——u16 计数+N×u64。S2C **无 Code**,仅成功
        /// 回本号:write(23209,[NewLevel:16,NewExp:32,NewPower:32])——**无 Color/Star 字段**(吞噬执行不改
        /// 筛选态)。失败一律 send_error→23200。服务端 pp_constellation_equip.erl:356-417,写在 :407。
        /// 老端响应 StarEquipController.ts:323-332 on23209。</summary>
        public const int STAREQUIP_DEVOUR = 23209;

        /// <summary>23250 装备属性预览/tips(C2S "ll" RoleId:64,GoodsAutoId:64;S2C **无 Code**)。
        /// 响应 write(23250,[GoodsId:64,Score:32,SendDsgt[item_to_bin_7],StarAttrCfg[item_to_bin_8],
        /// StarAttr(attr_list),SuitNum:16,SuitAttr(attr_list),BaseAttr(attr_list),ExtraAttr(attr_list),
        /// StrenAttr/EvoluAttr/MasterAttr/SpiritAttr(均 attr_list,来自 PK2 锻造系统贡献值),BaseRating:32])。
        /// RoleId!=自己时经 lib_player:apply_cast 跨进程查询他人装备(get_tips_msg 导出函数,
        /// pp_constellation_equip.erl:420-427/1055-1111,写在 :1108)。⚠**门禁豁免号之一**:未达
        /// open_lv/open_day_limit 也放行(:40)。老端请求 StarEquipToolTip.ts 等多处,响应
        /// StarEquipController.ts:334-339 on23250。</summary>
        public const int STAREQUIP_TIPS_PREVIEW = 23250;

        /// <summary>23251 星数被动推送(**无对应 C2S**——pt_232.erl 无 read(23251) 子句,纯服务端主动推)。
        /// 响应形状与 23205 完全一致:write(23251,[Level:16,MaxLevel:16,Star:16,Power:32])。触发点
        /// lib_constellation_equip:notify_client_star(穿脱装备/合成/蜕变等星数变化时调用,如
        /// pp_constellation_equip.erl:166/254/743)。老端 StarEquipController.ts:341-366 on23251——星数
        /// **下降**到低于已点亮大师等级时弹窗提示(与 23205 的弹窗方向相反,23205 是首次达标提示"可点亮",
        /// 23251 是回退提示"部分属性失效",UI 派生逻辑本轮不移植)。</summary>
        public const int STAREQUIP_STAR_PUSH = 23251;

        /// <summary>23252 合成(C2S 变长:WriteBegin(23252)+"i" RuleId+3 组"h"计数+N×"l" 材料id
        /// [IrregularGlist/RegularGlist/RatioGlist],对标 StarEquipController.ts:71-90 COM_REQUEST;
        /// S2C **自带 Code,四个不同出口都写本号,不经 23200**:①check_compose 前置校验失败→
        /// write(23252,[Code,RuleId,[]])(pp_constellation_equip.erl:554-556);②材料扣除失败→
        /// write(23252,[err150_compose_fail=1500081,RuleId,[]])(:539-541);③随机判定失败(未中奖)→
        /// 同 err150_compose_fail(:528-530);④成功→write(23252,[err150_compose_success=1500080,RuleId,
        /// SendList[item_to_bin_9:{GoodsId:64,GoodsTypeId:32}]])(:472-480)。老端 On23252(ts:368-379):
        /// code∈{1,1500080}→COM_SUCCESS;code==1500081→COM_FAIL;其余→ErrorCodeShow。</summary>
        public const int STAREQUIP_COMPOSE = 23252;

        /// <summary>23253 解锁星宿页(C2S "i" ConstellationId(页):32;S2C **末尾 Code,但本号自身恒为成功**——
        /// write(23253,[ConstellationId:32,?SUCCESS:32]),已激活/条件不满足两个失败分支都 send_error→23200,
        /// 不会带非 1 的 Code 回本号)。服务端 pp_constellation_equip.erl:560-583,写在 :573。
        /// 老端响应 StarEquipController.ts:381-404 on23253(成功后重发 <see cref="STAREQUIP_OVERVIEW"/>
        /// 刷新总览,ts:398)。</summary>
        public const int STAREQUIP_UNLOCK_PAGE = 23253;

        /// <summary>23254 蜕变/属性转移预览对比(C2S "ll" GoodsAutoId:64,TargetGoodsAutoId:64;S2C **无
        /// Code**)。响应字段序与 <see cref="STAREQUIP_TIPS_PREVIEW"/> 相同,仅在最前多一个
        /// TargetGoodsAutoId:64(write(23254,[GoodsId:64,TargetGoodsId:64,Score:32,...(其余同23250)...]))。
        /// 服务端 pp_constellation_equip.erl:585-636,写在 :632。老端响应 StarEquipController.ts:406-413
        /// on23254(model.transfromCache 单槽缓存,与 23255 共用 Fire(TRANSFROM_PREVIEW,...))。</summary>
        public const int STAREQUIP_TRANSFORM_PREVIEW = 23254;

        /// <summary>23255 按 goods_type_id 维度的类型 tips(C2S "i" GoodsTypeId:32;S2C **无 Code**)。
        /// 响应比 23250/23254 精简(无 SuitAttr/锻造四段属性):write(23255,[GoodsTypeId:32,Score:32,
        /// SendDsgt[item_to_bin_12],StarAttrCfg[item_to_bin_13],StarAttr(attr_list),SuitNum:16,
        /// BaseAttr(attr_list),ExtraAttr(attr_list),BaseRating:32])。⚠**门禁豁免号之二**:未达
        /// open_lv/open_day_limit 也放行(pp_constellation_equip.erl:40)。服务端 :639-672,写在 :664。
        /// 老端响应 StarEquipController.ts:415-421 on23255(model.typePreviewCache[goods_id] 分桶缓存)。</summary>
        public const int STAREQUIP_TYPE_TIPS_PREVIEW = 23255;

        /// <summary>23256 合成次数/特殊配方倒计时信息(C2S "i" ComposeId:32;S2C 无 Code,do_handle 无失败
        /// 分支恒回本号)。响应 write(23256,[ComposeId:32,Times:16,Index:16,Num:16])。服务端
        /// pp_constellation_equip.erl:674-692,写在 :690(缺配置时防御性 Index=Num=Times=0)。
        /// 老端响应 StarEquipController.ts:423-429 on23256(model.comSpNumList[compose_id] 分桶缓存)。</summary>
        public const int STAREQUIP_COMPOSE_TIME = 23256;

        /// <summary>23257 蜕变/属性转移执行(C2S "ll" CostGoodsAutoId:64,TargetGoodsAutoId:64;S2C
        /// **单字段 Res:32,仅成功回本号**——write(23257,[?SUCCESS])(pp_constellation_equip.erl:694-758,写在
        /// :728);check_translate 校验失败/事务失败均 send_error→23200,本号自身恒为成功,不会带非 1 的
        /// Res)。服务端成功后额外重发 <see cref="STAREQUIP_STAR_PUSH"/>(若星数变化,:747 evolution_info 是
        /// PK2 锻造侧联动)。老端响应 StarEquipController.ts:431-438 on23257——**空 if 块**(`if(scmd.res==1){}`,
        /// 老端未接任何动作),本端如实落地 Res 并补发一个事件供尾包消费(比老端多做但无害,同本仓"照接
        /// 解析落地"惯例)。</summary>
        public const int STAREQUIP_TRANSFORM = 23257;

        // ===================================================================================
        // 232 星宿锻造(chc,pt_232 兜底转发段;族路由 mod_server.erl:720 "232"→pp_constellation_equip,
        // 未匹配 cmd 经 pp_constellation_equip.erl:760-762 兜底转发给 pp_constellation_forge:handle/3)。
        // 轮23 PK2。四子系统:1强化STREN/2进化EVO/3附魔MAGIC(客户端UI显示"觉醒")/4启灵SOUL,类型码不在
        // wire 上传输,由 cmd 号本身区分(23210系/23220系/23230系/23240系)。星宿装备主系统(23200-23209/
        // 23250-23257)属 StarEquipController 段(PK1),就近但分段追加于此之前。
        // 门禁:客户端总开关 chcModel.OPEN_LV=560(硬编码);服务端每子系统各自 open_lv=580
        // (config_constellation_forge_kv id1/2/3/5,yu_server constellation_forge.hrl:10-14)。
        // ===================================================================================

        /// <summary>23210 强化界面数据(S2C,请求 c2s "c" TypeId)。
        /// 响应 pt_232.erl write(23210,[Code:32,TypeId:8,Stage:32,IsMax:8,Buff:16,
        /// EquipList_Len:16+EquipList[{EquipId:64,Pos:8,Lv:32}]])。服务端 lib_constellation_forge.erl:185-197
        /// strength_info/2(写在 :196),门禁 pp_constellation_forge.erl:24-32(RoleLv&gt;=?STRENGTH_LV,不满足
        /// 静默 skip,不回包不报错)。老端 chcController.ts:196-210 on23210。</summary>
        public const int STARFORGE_STREN_INFO = 23210;

        /// <summary>23211 强化动作(C2S "ccc" TypeId,Pos,Type[恒0,强化无自动购买语义];S2C)。
        /// 响应 pt_232.erl write(23211,[Code:32,TypeId:8,Pos:8,Type:8,Buff:16,Lv:32])。服务端
        /// lib_constellation_forge.erl:200-212 strength/4→strength_done(:474-504,写在 :498);门禁/失败
        /// pp_constellation_forge.erl:35-49。老端请求 chcStrenView.ts:98,响应 chcController.ts:212-232。
        /// ⚠本号还会"被动"收到两类误发的失败回包(服务端 bug,Unity 不 workaround,原样按本号格式解析即可):
        ///   ① 23231(附魔)失败/等级不足分支——pp_constellation_forge.erl:127,131 误发 23211(裁决2,字段形状
        ///      与本号 6 字段一致,数值语义对得上,无害误投)。
        ///   ② 23221(进化)失败/等级不足分支——pp_constellation_forge.erl:99,103 同样误发 23211(裁决2/侦察
        ///      档案未点出此第二例,本轮 PK2 直接读 pp_constellation_forge.erl 独立核实发现;且此例字段形状
        ///      对不上:[ErroCode,EVOLUTION_FAIL(=0),TypeId,EquipId(64位),Pos,0] 硬套进
        ///      [Code,TypeId,Pos,Type,Buff,Lv] 6 字段模具,EquipId 被砍到 Type:8 只剩低8位,TypeId/Pos 错位
        ///      —— 语义比①更烂,但客户端侧仍旧只是"按 23211 格式老实解析",不会抛异常,只是显示的
        ///      TypeId/Pos/Type/Lv 是垃圾值,Code 字段本身还是真错误码可正常提示。)</summary>
        public const int STARFORGE_STREN_ACTION = 23211;

        /// <summary>23212 强化大师界面(S2C,请求 c2s "c" TypeId)。响应 write(23212,[Code:32,TypeId:8,
        /// MasterList_Len:16+MasterList[{MasterLv:32,Status:8}]])。服务端 lib_constellation_forge.erl:215-224
        /// strength_master_info/2(写在 :223),门禁 pp_constellation_forge.erl:52-60。
        /// 老端 chcController.ts:234-238 on23212。</summary>
        public const int STARFORGE_STREN_MASTER_INFO = 23212;

        /// <summary>23213 点亮强化大师(C2S "c" TypeId;S2C)。响应 write(23213,[Code:32,TypeId:8,MasterLv:32])。
        /// 服务端 lib_constellation_forge.erl:227-247 lighten_strength_master(写在 :244);门禁/失败
        /// pp_constellation_forge.erl:64-77。老端请求 chcMasterView.ts:68,响应 chcController.ts:240-258
        /// (成功后 Toast"点亮成功"+自行重发 23210 刷新强化界面,:254)。</summary>
        public const int STARFORGE_STREN_MASTER_LIGHT = 23213;

        /// <summary>23220 进化界面数据(S2C,请求 c2s "c" TypeId)。响应 write(23220,[Code:32,TypeId:8,
        /// EquipList_Len:16+EquipList[{EquipId:64,Pos:8,Lv:32,AttrNum:16}]])。服务端
        /// lib_constellation_forge.erl:250-255 evolution_info/2(写在 :254),门禁 pp_constellation_forge.erl:80-88。
        /// 老端 chcController.ts:260-274 on23220。</summary>
        public const int STARFORGE_EVO_INFO = 23220;

        /// <summary>23221 进化动作(C2S 专用通道,非通用 REQUEST_PROTO——因带变长数组,老端走
        /// chcModel.SEND_EVO_PROTO→WriteBegin(23221)+WriteFMT("c",TypeId)+WriteFMT("l",EquipId)+
        /// WriteFMT("c",Pos)+WriteFMT("h",count)+逐个WriteFMT("l",CequipId),chcController.ts:62-75;
        /// S2C)。响应 write(23221,[Code:32,IsSuccess:8,TypeId:8,EquipId:64,Pos:8,Lv:32,AttrId:32])。
        /// 服务端 lib_constellation_forge.erl:258-265 evolution/5→check_evolution(:528-563)→
        /// evolution_done(:618-652,成功/失败两分支写在 :641/:646)→evolution_done_core(:654-694,含蜕变
        /// 星数/传闻广播)。门禁/请求本身失败 pp_constellation_forge.erl:91-105(⚠:99,:103 把失败/等级不足
        /// 误发成 23211,见 <see cref="STARFORGE_STREN_ACTION"/> 注释②)。老端响应 chcController.ts:276-296
        /// on23221;⚠真正走到 on23221(即 Code==1)的分支必然是"请求本身有效",此时 IsSuccess 才代表随机
        /// 判定的进化成功/失败(evolution_done_help urand:rand(1,10000),lib_constellation_forge.erl:635),
        /// 而 Code!=1 的分支因上述误发 bug 在当前服务端实现下已不可达(死代码但照抄,不删)。
        /// 裁决3"借号"写法(老端已知非 bug,照实现):lib_constellation_forge.erl:631 evolution_done_help 里
        /// `#goods{id = GoodsId, goods_id = GId} = NewGoodsInfo`——局部变量名 GoodsId 实际绑定的是
        /// `.id`(装备实例自增 id,语义应叫 EquipId),GId 才是真正的静态模板 id(语义应叫 GoodsId);
        /// 对比 :665 evolution_done_core 里 `#goods{id = EquipId, goods_id = GoodsId}` 才是"正常"命名。
        /// 写出去的 wire EquipId 字段值本身是对的(装备实例 id),只是这处局部变量名被"借用"反了,
        /// 纯命名混乱不是功能 bug——Unity 侧照单收字段即可,不必纠结命名。</summary>
        public const int STARFORGE_EVO_ACTION = 23221;

        /// <summary>23230 附魔(客户端UI"觉醒")界面数据(S2C,请求 c2s "c" TypeId)。响应
        /// write(23230,[Code:32,TypeId:8,Stage:32,IsMax:8,EquipList_Len:16+EquipList[{EquipId:64,Pos:8,Lv:32}]])
        /// ⚠比 23210 少一个 Buff:16 字段,不要对齐错位。服务端 lib_constellation_forge.erl:268-275
        /// enchantment_info/2(写在 :274),门禁 pp_constellation_forge.erl:108-116。
        /// 老端 chcController.ts:298-311 on23230。</summary>
        public const int STARFORGE_MAGIC_INFO = 23230;

        /// <summary>23231 附魔(客户端UI"觉醒")动作(C2S "ccc" TypeId,Pos,Type[0/1,材料不足时是否自动
        /// 购买消耗品];S2C)。⚠响应 write(23231,[Code:32,TypeId:8,Pos:8,Type:32,Lv:32])——Type 字段响应侧
        /// 是 32 位,请求侧是 8 位,读写宽度不对称,照抄不要按请求宽度去读响应。服务端
        /// lib_constellation_forge.erl:279-291 enchantment/4→enchantment_done(:726-753,写在 :749);门禁/失败
        /// pp_constellation_forge.erl:119-133(⚠:127,:131 失败/等级不足分支误发 23211 而非 23231,裁决2,
        /// Unity 不 workaround,见 <see cref="STARFORGE_STREN_ACTION"/> 注释①)。老端请求 chcMagicView.ts:97,
        /// 响应 chcController.ts:313-330。</summary>
        public const int STARFORGE_MAGIC_ACTION = 23231;

        /// <summary>23232 附魔大师界面(S2C,请求 c2s "c" TypeId)。响应 write(23232,[Code:32,TypeId:8,
        /// MasterList_Len:16+MasterList[{MasterLv:32,Status:8}]])。服务端 lib_constellation_forge.erl:294-303
        /// enchantment_master_info(写在 :302),门禁 pp_constellation_forge.erl:136-144。
        /// 老端 chcController.ts:332-335 on23232。</summary>
        public const int STARFORGE_MAGIC_MASTER_INFO = 23232;

        /// <summary>23233 点亮附魔大师(C2S "c" TypeId;S2C)。响应 write(23233,[Code:32,TypeId:8,MasterLv:32])。
        /// 服务端 lib_constellation_forge.erl:306-326 lighten_enchantment_master(写在 :321,该函数内调用
        /// :314 的 lighten_enchantment_master_do 助手拿到结果后才发送,发送点本身在外层非 _do 函数里,
        /// 核实原文订正,勿与 :755-783 的 _do 助手混淆);服务端在成功
        /// 分支内部还会额外主动重推一次 23210(:323 `strength_info(LastPlayer,EquipType)`——附魔大师的百分比
        /// 加成会改变强化显示属性,服务端自己推送刷新,不用客户端另外请求);门禁/失败
        /// pp_constellation_forge.erl:147-160。老端请求 chcMasterView.ts:70,响应 chcController.ts:337-354
        /// (成功后 Toast"点亮成功"+自行重发 23230 刷新附魔界面,:350——与服务端的 23210 被动刷新是两件
        /// 不同的事,互不冲突)。</summary>
        public const int STARFORGE_MAGIC_MASTER_LIGHT = 23233;

        /// <summary>23240 启灵界面数据(S2C,请求 c2s "c" TypeId)。响应 write(23240,[Code:32,TypeId:8,
        /// EquipList_Len:16+EquipList[{EquipId:64,Pos:8,IsSpirit:8}]])。服务端 lib_constellation_forge.erl:329-334
        /// spirit_info/2(写在 :333),门禁 pp_constellation_forge.erl:163-170。
        /// 老端 chcController.ts:356-364 on23240。</summary>
        public const int STARFORGE_SOUL_INFO = 23240;

        /// <summary>23241 启灵动作(C2S "cc" TypeId,Pos;S2C)。响应 write(23241,[Code:32,TypeId:8,Pos:8,
        /// IsSpirit:8])。服务端 lib_constellation_forge.erl:337-379 spirit/3(写在 :358;已启灵重复请求直接
        /// {false,err232_no_cfg}不经 send,:343);门禁/失败 pp_constellation_forge.erl:173-186——本号两个失败
        /// 分支都正确发回本号自己(23241),没有 23211/23231 那种误路由 bug,自洽。老端请求 chcSoulView.ts:80,
        /// 响应 chcController.ts:366-384(成功后 Fire(OPEN_VIEW,"chcSuccessView",scmd)弹窗,Unity 侧改走
        /// EVT_STARFORGE_ACTION_RESULT 事件通知,留给尾包 UI 消费)。</summary>
        public const int STARFORGE_SOUL_ACTION = 23241;
    }
}
