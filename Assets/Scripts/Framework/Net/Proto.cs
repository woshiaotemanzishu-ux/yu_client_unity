namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// 协议号常量,与 yu_client/yu_server 一致(请求与回包同号,注册回调即收该号回包)。
    /// 出处:yu_client h5/src/login/LoginController.ts。新协议号统一加在这里,不要散落。
    /// </summary>
    public static class Proto
    {
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
        /// <summary>发言(各频道通用,含喇叭)。发 "csslssis"(channel, province, city, receive_id, msg, args, tktime=0, ticket="");
        /// 对标老端 ChatController.ts send_msg,与 pt_110.erl read(11001) 字段序逐一核对一致。receive_id 语义:
        /// 私聊(channel=6)=对方 role_id;喇叭(channel=2)=范围选择(1本服/2小跨服/3全服,TRUMPET_TYPE);其余频道传0。
        /// 回包同号 11001 用于世界/公会/队伍等公共频道广播;**私聊(channel=6)真正回包走 11002,喇叭走 11029,
        /// 不会原样回 11001**(pp_chat.erl handle(11001,...) 内按 Channel 分三路 write(11001/11002/11029,...))。</summary>
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
        /// <summary>系统公告/跑马灯(GAME_START 空参发一次;服务端后台改公告表时会主动全服重推,幂等重建)。
        /// 回包 notice_list[u16 len]{Source:s, Type:8, Color:s, Content:s, Url:s, SendCount:32, SendGap:16,
        /// StartTime:32, EndTime:32, State:8}。⚠与"喇叭"是两套系统(11050 纯只读零消耗,喇叭消耗广播在 11001/11029),
        /// 客户端收到后自跑本地每秒轮询定时器按 send_gap 循环触发展示(对标老端 StartGongGaoList,定时器实现见
        /// <see cref="Shenxiao.Module.Core.Chat.ChatModel.PumpNotice"/>,勿用 MonoBehaviour.Update 直接承载判定逻辑)。</summary>
        public const int CHAT_NOTICE = 11050;
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

        // ----- 邮件(190xx,yu_server pt_190.erl) -----
        /// <summary>请求/回:邮件列表。回包 h + {MailId:l,Type:c,State:c,Title:s,IsAttach:c,Time:i,EffectEt:i}×N。</summary>
        public const int MAIL_LIST = 19001;
        /// <summary>新邮件推送(S2C,单封,同列表项格式)。</summary>
        public const int MAIL_NEW = 19007;
        /// <summary>是否有未读邮件(S2C "c")。</summary>
        public const int MAIL_UNREAD = 19008;
        /// <summary>可发邮件剩余次数(S2C "c")。</summary>
        public const int MAIL_LEFT_NUM = 19009;

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
        public const int MARKET_ICON_INFO = 15121; // 市场跨服开放时间(图标151/151@1切换)。请求无参(read(15121,_)->{ok,
        public const int LIMITLEVELSHOP_LIST = 61200; // 限时等级抢购礼包列表(模块612,驱动图标61201)。请求无参(read(61200,_)->
        public const int ACTIVITYFORESHOW_SNATCH_TIME = 65208; // 领地夺宝时间信息(预告图标 652@31@0 用)。请求无字段(read(65208,_)->{
        public const int BANQUET_WEDDING_STATE = 17249; // 婚礼状态(→172@2 宾客管理图标)。read(17249,_)->{ok,[]} 裸请求;w
        public const int BANQUET_CALL = 17256; // 婚礼召集/婚礼列表(→172@1 婚礼图标)。read(17256,_)->{ok,[]} 裸请
        public const int KAIFU_INVEST_OPEN = 42004; // 开服投资活动开启列表(驱动 4205 巅峰投资 / 1112 超值投资图标;裸请求)
        public const int KAIFU_BOOK_INFO = 42401; // 契约之书章节信息(驱动 424 / 424@1 图标;裸请求)
        public const int DIAMONDFIGHT_INFO = 13700; // 灵玉/勾玉大战活动状态(war_state 驱动图标137);请求裸发 read(13700,_
        public const int KF1VN_STAGE_INFO = 62101; // 诸天王者(跨服1vn)活动阶段。请求无字段裸发;回包 stage:c, turn:h, edti
        public const int SEAHEGEMONY_INFO = 18600; // 四海争霸基础信息(阵营/报名态)。请求无参 read(18600,_)->{ok,[]};回包 
        public const int SEAHEGEMONY_SIGNUP = 18625; // 四海争霸报名结束时间。请求无参 read(18625,_)->{ok,[]};回包 end_ti
        public const int KFHOLYAREA_ACT_STATE = 28410; // 神陨禁区(跨服圣域)活动状态/时间窗——驱动主界面图标284。请求裸发 read(28410,_
        public const int LUNG_STOVE_INFO = 18105; // 神纹熔炉数据(stove_data);回包驱动主界面图标181显隐;请求 read(18105,
        public const int BASEDUNGEON_TOWER_INFO = 61117; // 限时爬塔状态(round/over_time/reward_mode)——驱动限时塔图标 331
        public const int GROWTHBENEFITS_INFO = 41720;      // 成长福利信息/任务态
        public const int GROWTHBENEFITS_TASK_UPDATE = 41721; // 成长福利任务进度推送
        public const int FRIENDINVITE_INFO = 34001;        // 好友邀请/分享信息
        public const int TOPVIP_INFO = 45101;              // 至尊VIP基础信息
        public const int DRAGONBALL_GIFT_INFO = 14311;     // 龙玉礼包信息(图标143)
        public const int SEVENDAY_OPEN_INFO = 17500;       // 七天登录信息
        public const int SEVENDAY_MERGE_INFO = 17502;      // 合服七天信息
        public const int PUSHGIFT_LIST = 19101;            // 礼包推送列表
        public const int PUSHGIFT_OFFLINE = 19104;         // 礼包推送-离线过期领取
        public const int ADVENTURE_INFO = 42700;           // 天天冒险活动时间窗

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

        // ----- 通用副本(pt_610,yu_server dungeon;老端 BaseDungeonController.ts。御魂本 type=12,dun_id 12001~) -----
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

        // ----- 薄增量六件套(第20轮工单;详见 Docs/工单-薄增量六件套.md) -----
        /// <summary>OutWard 通用一键升星(type_id∉{1,2}:3翼影/4圣器/5神兵;发 "c" type_id;
        /// 回包=16023 少 etime/auto_buy:errcode:i, type_id:c, stage:c, star:h, blessing:i, blessing_plus:i,
        /// ratio_list[u16×{rate:c,rate_num:h}])。解主线 100665/101045/101345(ctype24/92/41)。</summary>
        public const int OUTWARD_STAR_UP_GENERIC = 16005;
        /// <summary>宝石镶嵌(发 "ccl" equipPos,stonePos,goodsId;回包 res:i, equip_type:c, pos:c, type_id:i)。主线 101175(ctype48)。</summary>
        public const int EQUIP_STONE_SET = 15208;
        /// <summary>宝石拆除(发 "cc";回包 res:i, equip_type:c, pos:c)。</summary>
        public const int EQUIP_STONE_UNSET = 15209;
        /// <summary>领取挂机收益(C2S 无参;回包 code:i + exp_list 按 ClientProtocol "13216" 读完)。
        /// 主线 101211(ctype91,唯一事件计数型:领一次即完成)。</summary>
        public const int ONHOOK_RECEIVE = 13216;
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
    }
}
