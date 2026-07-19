using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.GuildActivity
{
    /// <summary>
    /// 公会晚宴(pt_402 主体,自动循环 轮22 PK1)控制器:老端 GuildActivityController.ts 全实现的活玩法,
    /// 公会日常社交主入口。本轮 26 号纯数据层接入(公会BOSS 40201/03/04/08/09 + 晚宴主流程
    /// 40211/12/14/17/20/21/22 + 篝火/答题/龙魂/菜肴 40255/56/57/58/59/60/62/64/65/66/67 + 族错误出口
    /// 40200)。UI(33 个 view,prefab 只烤了 4 个)与场景内交互本轮不接(主控裁决11),数据从
    /// GuildActivityModel 取,消费方留 port-view-bindings 尾包,同 15a/15b Boss 先例。
    ///
    /// 新建独立模块(不并入 GuildController,老端就是独立 GuildActivityController.ts,主控裁决12)。
    /// 结社守卫(40230-32)按主控裁决2 全部 killlist,不在此模块范围内。
    ///
    /// 纪律/存疑核实结论(逐条附证据行号,供 PK3 落 killlist/baseline):
    /// ①**40218(裁决3 核实)**:c2s"退出晚宴场景"请求会被真实处理(pp_guild_act.erl:242-251,内部调
    /// lib_scene:player_change_scene+mod_guild_feast_mgr:exit_scene,有场景切换副作用),但服务端**从未**
    /// 回写 40218 本身——全仓 grep "write(40218" 只命中 pt_402.erl:288-294 的函数定义,exit_scene 的真实
    /// 实现(mod_guild_feast_mgr.erl:416-433)里没有任何 pt_402:write(40218,...) 调用。故本端**只提供
    /// RequestExitScene 发送方法,不注册 On40218 接收**(注册了也永远收不到,还会污染协议覆盖率统计,
    /// 镜像老端"40054 单向生效无回执"先例,严格执行裁决3 原文判据)。
    /// ②**40257/点火苗(裁决4 核实)**:c2s"点击火苗采集"确认死——pp_guild_act.erl:487-496 整段注释,
    /// 且唯一存活链 mod_guild_feast_mgr:collect_fire(RoleId,FireId,GuildId)(:164-165)→cast→
    /// do_handle_event(:702-739)除该已注释 c2s 外全仓零调用点。**但推送侧另有独立存活触发链**:
    /// lib_mon_event.erl:110,239(场景通用怪物击杀事件)→mod_guild_feast_mgr:kill_boss→
    /// lib_guild_feast.erl:905 collect_fire(MonId,AtterId,State)→pt_402:write(40257,...)(:938)——
    /// 即"采集火苗"当前版本已改造成"在晚宴场景里击杀火苗怪"(火苗以怪物形式存在于场景,
    /// lib_guild_feast.erl:1078-1084 create_fire_by_guild 用 lib_mon:sync_create_mon 生成),不再是
    /// 点击 c2s;老端 GuildActivityController.ts 亦只注册 on40257 接收(:196-201,358)、全仓 zero 主动
    /// Fire(REQUEST_PROTO,40257)/SendFmtToGame(40257),与此吻合。kf 模块核实:mod_kf_guild_feast_topic
    /// 与"火苗/dragon"无关(全仓 grep "fire\|dragon" 零命中,该模块只管答题跨服同步),**无接管**。
    /// 结论:本端**只注册 On40257 接收(纯被动推送),不提供发送方法**。
    /// ③**40263/召唤远古巨龙(裁决4 核实)**:c2s"召唤巨龙"确认死——pp_guild_act.erl:624-643 整段注释,
    /// 老端 EveningDragonCallItem.ts:29 **仍在**发 Fire(REQUEST_PROTO,40263,"c",type)(即老端认为这个
    /// 功能还活着,实际已被服务端砍掉,点击会落 handle(_Cmd,_,_)->{error,"pp_guild_act no match"} 静默
    /// 吃掉,不会有任何回包)。**推送侧与 40257 不同,没有另开存活入口**:唯一 write(40263 调用点
    /// lib_guild_feast.erl:1103(summon_dragon/5)只被 mod_guild_feast_mgr.erl:1240-1247
    /// do_handle_event({'summon_dragon',...})调用,该 cast 唯一发起方是 mod_guild_feast_mgr.erl:240-241
    /// 的 summon_dragon/4 API,而这个 API 唯一调用方正是 pp_guild_act.erl:636 已注释的死 c2s——整条链
    /// 100% 不可达,三层彻底死透。kf 模块核实同②:mod_kf_guild_feast_topic/lib_kf_guild_feast_topic_mod
    /// 与 dragon 无关,**无接管**。结论:本端**发送/接收均不实现**(不建 Proto 常量,不注册任何 handler)
    /// ——这是对裁决4"发送侧不实现"的延伸执行(裁决3 立的"无 write 调用点就严禁注册接收"原则同样适用
    /// 于"write 调用点本身就是死代码"的情形,二者本质相同:注册了也永远不会触发),留给 PK3 补
    /// killlist(reason=dead_server_handle_commented,40218/40257 相反结论不构成 40263 的翻案理由)。
    /// ④**40266(S2 存疑,补 grep 定死活)**:确认**活**。唯一 write(40266 调用点 lib_guild_feast.erl:1437
    /// (send_topic_reward_in_ps/3),存活触发链:mod_guild_feast_mgr.erl:797-814
    /// do_handle_event(info,{'pre_enter_dragon',GuildId},...)(阶段切换到召龙前结算,非kf分支)→
    /// lib_guild_feast:quest_calc_reward(GameType,State)(:390-,非旧版已注释的:384-388)→
    /// lib_player:apply_cast(RoleId,...,send_topic_reward_in_ps,[Rank])(:398-399,401当:1071同链)→
    /// send_topic_reward_in_ps/3(:1430-1443)→pt_402:write(40266,[Rank,Reward])。全链无注释,确认存活。
    /// 无对应 c2s(pt_402.erl 无 read(40266)子句),本端只注册接收。
    ///
    /// 40255 子句遮蔽:pp_guild_act.erl 有两条 handle(40255,...)(:463 无判别 [_Type] 在前恒匹配,
    /// :522 [Type==1] 在后永不可达),r22 侦察坐实轮13a 疑点,注释存档,不影响本端接收实现——本端按
    /// "只会推 Type=1"落地,老端也从未主动发起 40255 c2s(全仓 zero Fire/SendFmtToGame),故不提供发送
    /// 方法(同 40257,纯被动收)。
    /// </summary>
    public sealed class GuildActivityController : BaseController
    {
        public static readonly GuildActivityController Instance = new GuildActivityController();
        private GuildActivityController() { }

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        protected override void Register()
        {
            RegisterProtocal(Proto.GUILDFEAST_ERROR, On40200);
            RegisterProtocal(Proto.GUILDFEAST_BOSS_INFO, On40201);
            RegisterProtocal(Proto.GUILDFEAST_BOSS_MAT_ADD, On40203);
            RegisterProtocal(Proto.GUILDFEAST_BOSS_CALL, On40204);
            RegisterProtocal(Proto.GUILDFEAST_BOSS_RESULT, On40208);
            RegisterProtocal(Proto.GUILDFEAST_BOSS_AUTO, On40209);
            RegisterProtocal(Proto.GUILDFEAST_ACT_INFO, On40211);
            RegisterProtocal(Proto.GUILDFEAST_ENTER_SCENE, On40212);
            RegisterProtocal(Proto.GUILDFEAST_RANK_INFO, On40214);
            RegisterProtocal(Proto.GUILDFEAST_QUEST_INFO, On40217);
            // 40218:仅发送不注册接收——见类注释①(无 write 调用点)。
            RegisterProtocal(Proto.GUILDFEAST_MY_RANK, On40220);
            RegisterProtocal(Proto.GUILDFEAST_MINI_GAME_STATUS, On40221);
            RegisterProtocal(Proto.GUILDFEAST_GAME_TYPE, On40222);
            RegisterProtocal(Proto.GUILDFEAST_EXP_PUSH, On40255);
            RegisterProtocal(Proto.GUILDFEAST_FIRE_INFO, On40256);
            RegisterProtocal(Proto.GUILDFEAST_FIRE_REWARD, On40257);
            RegisterProtocal(Proto.GUILDFEAST_STAGE_PUSH, On40258);
            RegisterProtocal(Proto.GUILDFEAST_ANSWER, On40259);
            RegisterProtocal(Proto.GUILDFEAST_DRAGON_INFO, On40260);
            // 40261:仅发送不注册接收——见类注释(pt_402.erl 无 write(40261 子句)。
            RegisterProtocal(Proto.GUILDFEAST_RESULT_INFO, On40262);
            RegisterProtocal(Proto.GUILDFEAST_FOOD_BUY, On40264);
            RegisterProtocal(Proto.GUILDFEAST_FOOD_STATUS, On40265);
            RegisterProtocal(Proto.GUILDFEAST_RANK_REWARD, On40266);
            RegisterProtocal(Proto.GUILDFEAST_EXP_BUFF, On40267);
            // 40263:发送/接收均不实现——见类注释③(三层死透,PK3 killlist)。

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            GuildActivityModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>对标老端 GAME_START 后恒发 40201(公会BOSS信息)+ 40211(晚宴活动信息,ts:79-88)。
        /// 老端用 setTimeout 1.5ms 微延迟,本端无需复刻该延迟,直接顺序发送。</summary>
        private async void OnGameStart()
        {
            await GuildActivityConfigs.EnsureLoaded();
            RequestBossInfo();
            RequestActInfo();
            GameLog.Info("GuildActivity", "GAME_START 公会晚宴登录链:40201+40211 已发");
        }

        // ---------------------------------------------------------------------------------------
        // §1 公会BOSS(40201-04/08/09)
        // ---------------------------------------------------------------------------------------

        public void RequestBossInfo() => SendFmt(Proto.GUILDFEAST_BOSS_INFO);
        public void RequestCallBoss() => SendFmt(Proto.GUILDFEAST_BOSS_CALL);
        public void RequestSetAutoDrum(int isAuto) => SendFmt(Proto.GUILDFEAST_BOSS_AUTO, "c", isAuto);

        private void On40201(NetReader r)
        {
            var info = new GuildActivityModel.BossInfo
            {
                Etime = r.ReadU32(), AutoDrumupTime = r.ReadU32(), DunId = r.ReadU32(), GbossMat = r.ReadU32(),
                RemainTimes = r.ReadU8(), IsAuto = r.ReadU8(), IsDrumToday = r.ReadU8(), MonState = r.ReadU8(),
            };
            GuildActivityModel.Instance.SetBoss(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_BOSS_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40201 公会BOSS信息 etime={0} gbossMat={1} isAuto={2} isDrumToday={3}",
                info.Etime, info.GbossMat, info.IsAuto, info.IsDrumToday);
        }

        /// <summary>40203 兽粮被动推送(内部触发,非 c2s 回执,见 Proto.cs 注释)。</summary>
        private void On40203(NetReader r)
        {
            long add = r.ReadU32();
            long total = r.ReadU32();
            GuildActivityModel.Instance.ApplyGbossMatAdd(add, total);
            TipsManager.Toast("获得神兽诱饵:" + add);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_BOSS_MAT_ADD, add, total);
            GameLog.Info("GuildActivity", "40203 兽粮被动推送 add={0} total={1}", add, total);
        }

        /// <summary>errcode:1=召集成功/2=今日已召集(老端仅关闭图标,UI,本端不处理)/其余=显码
        /// (对标老端 ts:276-290)。</summary>
        private void On40204(NetReader r)
        {
            int errcode = r.ReadI32();
            long roleId = r.ReadU64();
            if (errcode == 1)
            {
                GuildActivityModel.Instance.ApplyCallBossSuccess();
            }
            else if (errcode != 2)
            {
                ShowError(errcode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_CALL_BOSS_RESULT, errcode);
            GameLog.Info("GuildActivity", "40204 召集公会BOSS errcode={0} roleId={1}", errcode, roleId);
        }

        private void On40208(NetReader r)
        {
            int gbossResult = r.ReadU8();
            List<GuildActivityModel.GbossRewardEntry> fixReward = r.ReadArray(ReadGbossRewardEntry);
            List<GuildActivityModel.GbossRewardEntry> auctionReward = r.ReadArray(ReadGbossRewardEntry);
            var result = new GuildActivityModel.BossResult { GbossResult = gbossResult };
            result.FixReward.AddRange(fixReward);
            result.AuctionReward.AddRange(auctionReward);
            GuildActivityModel.Instance.SetBossResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_BOSS_RESULT);
            GameLog.Info("GuildActivity", "40208 BOSS结算推送 result={0} fixN={1} auctionN={2}",
                gbossResult, fixReward.Count, auctionReward.Count);
        }

        private static GuildActivityModel.GbossRewardEntry ReadGbossRewardEntry(NetReader r) => new GuildActivityModel.GbossRewardEntry
        {
            Type = r.ReadU8(), TypeId = r.ReadU32(), Num = r.ReadU16(),
        };

        private void On40209(NetReader r)
        {
            int errcode = r.ReadI32();
            int isAuto = r.ReadU8();
            if (errcode == 1)
            {
                GuildActivityModel.Instance.ApplyAutoDrumSet(isAuto);
                TipsManager.Toast(isAuto != 0 ? "设置自动召唤成功" : "取消自动召唤成功");
            }
            else
            {
                ShowError(errcode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_AUTO_DRUM_RESULT, errcode, isAuto);
            GameLog.Info("GuildActivity", "40209 设置自动召唤 errcode={0} isAuto={1}", errcode, isAuto);
        }

        // ---------------------------------------------------------------------------------------
        // §2 晚宴主流程(40211/12/14/17/20/21/22;40218 仅发送见类注释①)
        // ---------------------------------------------------------------------------------------

        public void RequestActInfo() => SendFmt(Proto.GUILDFEAST_ACT_INFO);
        public void RequestEnterScene() => SendFmt(Proto.GUILDFEAST_ENTER_SCENE);
        /// <summary>40218 退出晚宴场景请求——**无回执**(见类注释①/Proto.cs 注释),调用方不可等待响应,
        /// 效果(场景切换)由服务端内部直接处理。</summary>
        public void RequestExitScene() => SendFmt(Proto.GUILDFEAST_EXIT_SCENE);
        public void RequestRankInfo() => SendFmt(Proto.GUILDFEAST_RANK_INFO);
        public void RequestQuestInfo() => SendFmt(Proto.GUILDFEAST_QUEST_INFO);
        public void RequestMyRank() => SendFmt(Proto.GUILDFEAST_MY_RANK);
        public void RequestMiniGameStatus() => SendFmt(Proto.GUILDFEAST_MINI_GAME_STATUS);
        public void RequestGameType() => SendFmt(Proto.GUILDFEAST_GAME_TYPE);

        /// <summary>核心驱动号:老端据 Stage 决定弹哪个晚宴子面板(CheckOpenView),本轮 UI 不接,
        /// 数据先落地(主控裁决11)。</summary>
        private void On40211(NetReader r)
        {
            var info = new GuildActivityModel.ActInfo
            {
                Status = r.ReadU8(), ActEndTime = r.ReadU32(), Etime = r.ReadU32(), Stage = r.ReadU8(),
            };
            GuildActivityModel.Instance.SetAct(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_ACT_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40211 晚宴活动信息 status={0} stage={1} etime={2}", info.Status, info.Stage, info.Etime);
        }

        /// <summary>errcode==1 老端重发 40211 刷新(ts:160-161)。</summary>
        private void On40212(NetReader r)
        {
            int errcode = r.ReadI32();
            if (errcode == 1)
            {
                RequestActInfo();
            }
            else
            {
                ShowError(errcode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_ENTER_SCENE_RESULT, errcode);
            GameLog.Info("GuildActivity", "40212 进入晚宴场景 errcode={0}", errcode);
        }

        private void On40214(NetReader r)
        {
            int isKf = r.ReadU8();
            List<GuildActivityModel.RankGuildEntry> guildList = r.ReadArray(ReadRankGuildEntry);
            List<GuildActivityModel.RankServerEntry> rankList = r.ReadArray(ReadRankServerEntry);
            var info = new GuildActivityModel.RankInfo { IsKf = isKf };
            info.GuildList.AddRange(guildList);
            info.RankList.AddRange(rankList);
            GuildActivityModel.Instance.SetRank(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_RANK_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40214 积分排行榜 isKf={0} guildN={1} rankN={2}", isKf, guildList.Count, rankList.Count);
        }

        private static GuildActivityModel.RankGuildEntry ReadRankGuildEntry(NetReader r) => new GuildActivityModel.RankGuildEntry
        {
            GuildId = r.ReadU64(), ServerNum = r.ReadU32(), GuildName = r.ReadString(), GuildScore = r.ReadU32(), GuildRank = r.ReadU16(),
        };

        private static GuildActivityModel.RankServerEntry ReadRankServerEntry(NetReader r) => new GuildActivityModel.RankServerEntry
        {
            SerId = r.ReadU32(), SerNum = r.ReadU32(), Rank = r.ReadU16(), Name = r.ReadString(), Score = r.ReadU32(),
        };

        private void On40217(NetReader r)
        {
            var info = new GuildActivityModel.QuestInfo
            {
                Status = r.ReadU8(), Etime = r.ReadU32(), No = r.ReadU32(), Id = r.ReadU64(),
            };
            GuildActivityModel.Instance.SetQuest(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_QUEST_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40217 答题信息 status={0} no={1} id={2}", info.Status, info.No, info.Id);
        }

        private void On40220(NetReader r)
        {
            int rank = r.ReadU16();
            long point = r.ReadU64();
            GuildActivityModel.Instance.SetMyRank(rank, point);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_MY_RANK_UPDATE);
            GameLog.Info("GuildActivity", "40220 个人积分排行 rank={0} point={1}", rank, point);
        }

        private void On40221(NetReader r)
        {
            int isFinish = r.ReadU8();
            GuildActivityModel.Instance.SetMiniGameFinished(isFinish);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_MINI_GAME_STATUS, isFinish != 0);
            GameLog.Info("GuildActivity", "40221 小游戏完成状态 isFinish={0}", isFinish);
        }

        private void On40222(NetReader r)
        {
            int gameType = r.ReadU8();
            GuildActivityModel.Instance.SetGameType(gameType);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_GAME_TYPE_UPDATE, gameType);
            GameLog.Info("GuildActivity", "40222 当日小游戏类型 gameType={0}", gameType);
        }

        // ---------------------------------------------------------------------------------------
        // §3 篝火/答题/龙魂/菜肴(40255-67;40257 仅接收见类注释②,40261/40263 见类注释)
        // ---------------------------------------------------------------------------------------

        public void RequestFireInfo() => SendFmt(Proto.GUILDFEAST_FIRE_INFO);
        public void RequestAnswer(int answer) => SendFmt(Proto.GUILDFEAST_ANSWER, "c", answer);
        public void RequestDragonInfo() => SendFmt(Proto.GUILDFEAST_DRAGON_INFO);
        /// <summary>购买龙魂——**无独立回执**:失败走 40200 通用错误包,成功由服务端内部
        /// add_dragon_spirit 广播 40260 刷新全公会(见 Proto.cs 注释),调用方订阅
        /// EVT_GUILDACT_ERROR / EVT_GUILDACT_DRAGON_INFO_UPDATE 接结果,不要等待本请求的直接回包。
        /// num=购买数量(wire 字段名沿用服务端"DragonSpirit"但语义是 Num)。</summary>
        public void RequestBuyDragonSpirit(long num) => SendFmt(Proto.GUILDFEAST_BUY_DRAGON_SPIRIT, "l", num);
        public void RequestBuyFood(int type) => SendFmt(Proto.GUILDFEAST_FOOD_BUY, "c", type);
        public void RequestFoodStatus() => SendFmt(Proto.GUILDFEAST_FOOD_STATUS);
        public void RequestExpBuffRatio() => SendFmt(Proto.GUILDFEAST_EXP_BUFF);

        /// <summary>经验/贡献推送,服务端子句遮蔽后实际只会推 Type=1(见类注释),纯被动收不提供发送方法。</summary>
        private void On40255(NetReader r)
        {
            int type = r.ReadU8();
            long exp = r.ReadU64();
            GuildActivityModel.Instance.ApplyExpPush(type, exp);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_EXP_PUSH, type, exp);
            GameLog.Info("GuildActivity", "40255 经验/贡献推送 type={0} exp={1}", type, exp);
        }

        private void On40256(NetReader r)
        {
            var info = new GuildActivityModel.FireInfo { Wave = r.ReadU32(), NextTime = r.ReadU64() };
            GuildActivityModel.Instance.SetFire(info);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_FIRE_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40256 火苗信息 wave={0} nextTime={1}", info.Wave, info.NextTime);
        }

        /// <summary>采集火苗奖励——纯被动推送(c2s"点击采集"已死,推送侧走场景击杀火苗怪触发,
        /// 见类注释②),不提供发送方法。</summary>
        private void On40257(NetReader r)
        {
            List<GuildActivityModel.ObjectReward> list = r.ReadArray(ReadObjectReward);
            GuildActivityModel.Instance.SetFireReward(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_FIRE_REWARD_PUSH);
            GameLog.Info("GuildActivity", "40257 采集火苗奖励(纯被动推送) rewardN={0}", list.Count);
        }

        private static GuildActivityModel.ObjectReward ReadObjectReward(NetReader r) => new GuildActivityModel.ObjectReward
        {
            Type = r.ReadU8(), TypeId = r.ReadU32(), Num = r.ReadU32(),
        };

        /// <summary>阶段推送,无对应 c2s(纯推送)。老端 on40258 取出即弃(占位),本端如实落地
        /// 供尾包消费,比老端完整无害。</summary>
        private void On40258(NetReader r)
        {
            int stage = r.ReadU8();
            int time = r.ReadU16();
            GuildActivityModel.Instance.ApplyStagePush(stage, time);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_STAGE_PUSH, stage, time);
            GameLog.Info("GuildActivity", "40258 阶段推送 stage={0} time={1}", stage, time);
        }

        private void On40259(NetReader r)
        {
            int status = r.ReadU8();
            GuildActivityModel.Instance.SetQuestStatus(status);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_QUESTION_STATUS, status);
            GameLog.Info("GuildActivity", "40259 答题状态推送 status={0}", status);
        }

        private void On40260(NetReader r)
        {
            long dragonSpirit = r.ReadU64();
            GuildActivityModel.Instance.SetDragonSpirit(dragonSpirit);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_DRAGON_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40260 龙魂信息 dragonSpirit={0}", dragonSpirit);
        }

        /// <summary>战斗结果推送(无独立c2s,战斗结算内部推送)。老端对应弹窗分支已被注释
        /// (EveningResultView/DungeonFailureView),仅数据落地,UI 未接。</summary>
        private void On40262(NetReader r)
        {
            int status = r.ReadU8();
            List<GuildActivityModel.ObjectReward> rewardList = r.ReadArray(ReadObjectReward);
            var result = new GuildActivityModel.ResultInfo { Status = status };
            result.RewardList.AddRange(rewardList);
            GuildActivityModel.Instance.SetResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_RESULT_INFO_UPDATE);
            GameLog.Info("GuildActivity", "40262 战斗结果推送 status={0} rewardN={1}", status, rewardList.Count);
        }

        private void On40264(NetReader r)
        {
            int code = r.ReadI32();
            List<GuildActivityModel.FoodEntry> foodList = r.ReadArray(ReadFoodEntry);
            if (code == 1)
            {
                GuildActivityModel.Instance.SetFoodList(foodList);
                TipsManager.Toast("购买菜肴成功");
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_FOOD_BUY_RESULT, code == 1, code);
            GameLog.Info("GuildActivity", "40264 购买菜肴 code={0} foodN={1}", code, foodList.Count);
        }

        private void On40265(NetReader r)
        {
            List<GuildActivityModel.FoodEntry> foodList = r.ReadArray(ReadFoodEntry);
            GuildActivityModel.Instance.SetFoodList(foodList);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_FOOD_STATUS_UPDATE);
            GameLog.Info("GuildActivity", "40265 菜肴状态 foodN={0}", foodList.Count);
        }

        private static GuildActivityModel.FoodEntry ReadFoodEntry(NetReader r) => new GuildActivityModel.FoodEntry
        {
            Type = r.ReadU8(), Status = r.ReadU8(),
        };

        /// <summary>答题积分排名奖励,纯 S-only 推送(见类注释④,无对应 c2s)。</summary>
        private void On40266(NetReader r)
        {
            long rank = r.ReadU32();
            List<GuildActivityModel.ObjectReward> reward = r.ReadArray(ReadObjectReward);
            GuildActivityModel.Instance.SetRankReward((int)rank, reward);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_RANK_REWARD_PUSH);
            GameLog.Info("GuildActivity", "40266 答题积分排名奖励(纯推送) rank={0} rewardN={1}", rank, reward.Count);
        }

        private void On40267(NetReader r)
        {
            long ratio = r.ReadU32();
            GuildActivityModel.Instance.SetExpBuffRatio(ratio);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_EXP_BUFF_UPDATE, ratio);
            GameLog.Info("GuildActivity", "40267 经验加成状态 ratio={0}", ratio);
        }

        // ---------------------------------------------------------------------------------------
        // §4 族错误出口(40200)
        // ---------------------------------------------------------------------------------------

        private void On40200(NetReader r)
        {
            int errcode = r.ReadI32();
            GuildActivityModel.Instance.SetLastError(errcode);
            ShowError(errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_GUILDACT_ERROR, errcode);
            GameLog.Info("GuildActivity", "40200 族错误出口 errcode={0}", errcode);
        }
    }
}
