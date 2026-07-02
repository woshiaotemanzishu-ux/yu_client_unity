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
        public const int CHAT_MESSAGE = 11001;
        public const int CHAT_CACHE = 11010;

        // ----- 聊天/公告(110xx,yu_server pt_110.erl) -----
        /// <summary>传闻广播(全服)。回包 "hhs"(moduleId, id, content)。</summary>
        public const int SC_CHUANWEN = 11015;
        /// <summary>传闻广播(带发送者形象)。回包 serId:h, playerId:l, figure 块, moduleId:h, id:h, content:s。</summary>
        public const int SC_CHUANWEN_FIGURE = 11018;
        /// <summary>系统公告/提示(跑马灯)。回包 "s"(content)。</summary>
        public const int SC_SYS_NOTICE = 11020;

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
        /// <summary>进/出战斗态(C2S)。对标 FightController.ts:889 SendFmtToGame(20024,"c",1/2):1=进战斗态 2=出战斗态。
        /// 老端由 CHANGE_FIGHTING_STATE 驱动(受 ConfigClientScene.fighting_state_invalidate 限制)。</summary>
        public const int CS_FIGHTING_STATE = 20024;
        /// <summary>采集请求/回包(同号 C2S+S2C)。对标 FightController.ts:867 SendFmtToGame(20008,"iic",ins_id,type_id,flag)
        /// + handler20008(ts:583)。发 "iic"(采集物实例 id, 采集物 type_id, flag:1=请求开始/2=请求完成/3=取消);
        /// 回包 "c"(flag:1=开始成功 START→播蹲下采集动作+进度条、2=完成成功 COMPLETE→收尾删采集物、≥3=各类失败/取消)。</summary>
        public const int CS_COLLECT = 20008;

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
        public const int ROLE_LIFELONG_COUNT = 13088;

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
    }
}
