using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Relive;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// Boss 家族二期·跨服族(自动循环 轮15b)控制器:pt_470(千幻蜃楼/圣兽岭)+ pt_471(镇煞封魂/幻域Boss)+
    /// pt_619(论剑恩怨簿)+ pt_460 内 kf_great_demon 壳(46037-39/46046)。与 15a 的 <see cref="BossController"/>
    /// (本服 46000 段)并列注册,互不重叠协议号,共用 <see cref="BossModel"/> 的少量共享形态
    /// (EquipExtraAttrEntry)。数据落 <see cref="KfBossModel"/>。
    ///
    /// 纪律:①服务端行文逐行复核原文(转发层+跨服层两层),本控制器实现前直接 grep yu_server 调用点核对,
    /// 订正了 r15b 侦察子报告 2 处"服务端无发送调用点"误判(47008/47117,详见 Proto.cs 各号注释);
    /// ②进场景族分两族时序(15b三镜头验收订正):千幻蜃楼 47003/47004 走"失败显式回包/成功隐式(靠场景切换
    /// 事件)确认",严禁等成功包;但镇煞封魂 47102/47103 成败**均显式回包**(lib_decoration_boss_local.erl:269
    /// 成功也回 47102[?SUCCESS,...]),On47102 读 code 分流即可——两族时序不同,勿一概而论"成功隐式";
    /// ③47010 kf_server_allot 占位错误码不做自动重试轮询(老端本身也只是提示语);
    /// ④死号严禁发:47001(发送侧 C2S 死号,老端从未 Fire;服务端 read/response 链其实完整,仅老端不发)/47011
    /// (整链路死:pp_eudemons_land.erl 无 handle(47011)子句,write(47011) 组包虽在 zone_local.erl:154 有唯一
    /// 调用点,但其宿主触发函数 get_same_zone_servers/1 全仓库零调用者,永不执行)不注册。
    /// </summary>
    public sealed class KfBossController : BaseController
    {
        public static readonly KfBossController Instance = new KfBossController();
        private KfBossController() { }

        private const int CROSS_BASE = KfBossModel.CROSS_BOSS_BASE_INDEX;

        /// <summary>老端 On47010 特判文案(code==1031)对应的 kf_server_allot 错误码——"正在获取千幻蜃楼信息"。</summary>
        private const int KF_SERVER_ALLOT_CODE = 1031;

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        protected override void Register()
        {
            // ---- pt_470 千幻蜃楼/圣兽岭(20 活号;47001/47011 死号跳过,见类注释) ----
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_LIST, On47000);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_DROP_LOG, On47002);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_ENTER, On47003);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_LEAVE, On47004);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_REMIND, On47005);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_REBORN_TIP, On47006);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_KILLED_NOTICE, On47007);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_REBORN_REFRESH, On47008);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_TIRED, On47009);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_SYNC_CODE, On47010);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_SETTLE_REWARD, On47015);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_ROLE_INFO, On47016);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_BOX_POS, On47017);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_BOX_POS_UPDATE, On47018);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_HUNT_LEVEL, On47019);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_RANK, On47021);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_SCORE, On47022);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_MAX_TIRED, On47023);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_DEATH_DEBUFF, On47034);
            RegisterProtocal(Proto.KFBOSS_EUDEMONS_REVIVE, On47035);

            // ---- pt_471 镇煞封魂/幻域Boss(17 号全活) ----
            RegisterProtocal(Proto.KFBOSS_DECORATION_INFO, On47101);
            RegisterProtocal(Proto.KFBOSS_DECORATION_ENTER, On47102);
            RegisterProtocal(Proto.KFBOSS_DECORATION_LEAVE, On47103);
            RegisterProtocal(Proto.KFBOSS_DECORATION_BUY_COUNT, On47104);
            RegisterProtocal(Proto.KFBOSS_DECORATION_UNFOLLOW_LIST, On47105);
            RegisterProtocal(Proto.KFBOSS_DECORATION_FOLLOW, On47106);
            RegisterProtocal(Proto.KFBOSS_DECORATION_REBORN, On47107);
            RegisterProtocal(Proto.KFBOSS_DECORATION_DROP_LOG, On47108);
            RegisterProtocal(Proto.KFBOSS_DECORATION_RANK, On47109);
            RegisterProtocal(Proto.KFBOSS_DECORATION_ENTER_SPECIAL, On47110);
            RegisterProtocal(Proto.KFBOSS_DECORATION_GUILD_HELP, On47111);
            RegisterProtocal(Proto.KFBOSS_DECORATION_DAMAGE_PUSH, On47112);
            RegisterProtocal(Proto.KFBOSS_DECORATION_SETTLE, On47113);
            RegisterProtocal(Proto.KFBOSS_DECORATION_SCENE_INFO, On47114);
            RegisterProtocal(Proto.KFBOSS_DECORATION_QUIT_TIME, On47115);
            RegisterProtocal(Proto.KFBOSS_DECORATION_REVIVE_TIME, On47116);
            RegisterProtocal(Proto.KFBOSS_DECORATION_DEATH, On47117);

            // ---- pt_619 论剑恩怨簿(3 号) ----
            RegisterProtocal(Proto.KFBOSS_KILL_RECORD_LIST, On61900);
            RegisterProtocal(Proto.KFBOSS_KILL_RECORD_NEW, On61901);
            RegisterProtocal(Proto.KFBOSS_KILL_RECORD_KF_NEW, On61902);

            // ---- pt_460 内 kf_great_demon 壳(4 号,太古遗凶专属;pp_boss.erl 无条件转发) ----
            RegisterProtocal(Proto.KFBOSS_GREAT_DEMON_REWARD_STATE, On46037);
            RegisterProtocal(Proto.KFBOSS_GREAT_DEMON_REWARD_TAKE, On46038);
            RegisterProtocal(Proto.KFBOSS_GREAT_DEMON_BOX_INFO, On46039);
            RegisterProtocal(Proto.KFBOSS_GREAT_DEMON_DROP_LOG, On46046);
        }

        public override void Dispose()
        {
            KfBossModel.Instance.Clear();
            base.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // pt_470 千幻蜃楼/圣兽岭(Eudemons)
        // ---------------------------------------------------------------------------------------

        /// <summary>C2S "c" 服务端裸类型值(如 holy=1,非客户端 +1000 后的值)。</summary>
        public void RequestEudemonsBossList(int rawBossType) => SendFmt(Proto.KFBOSS_EUDEMONS_LIST, "c", rawBossType);
        public void RequestEudemonsDropLog() => SendFmt(Proto.KFBOSS_EUDEMONS_DROP_LOG);
        public void EnterEudemonsBoss(int rawBossType, int bossId) => SendFmt(Proto.KFBOSS_EUDEMONS_ENTER, "ci", rawBossType, bossId);
        public void LeaveEudemonsBoss(int rawBossType) => SendFmt(Proto.KFBOSS_EUDEMONS_LEAVE, "c", rawBossType);
        public void SetEudemonsRemindReq(int rawBossType, int bossId, bool remind) =>
            SendFmt(Proto.KFBOSS_EUDEMONS_REMIND, "cic", rawBossType, bossId, remind ? 1 : 0);
        public void RequestEudemonsSettleReward() => SendFmt(Proto.KFBOSS_EUDEMONS_SETTLE_REWARD);
        public void RequestEudemonsRoleInfo() => SendFmt(Proto.KFBOSS_EUDEMONS_ROLE_INFO);
        public void RequestEudemonsBoxPos() => SendFmt(Proto.KFBOSS_EUDEMONS_BOX_POS);
        public void RequestEudemonsHuntLevel() => SendFmt(Proto.KFBOSS_EUDEMONS_HUNT_LEVEL);
        public void RequestEudemonsRank() => SendFmt(Proto.KFBOSS_EUDEMONS_RANK);
        public void RequestEudemonsScore() => SendFmt(Proto.KFBOSS_EUDEMONS_SCORE);
        public void RequestEudemonsMaxTired() => SendFmt(Proto.KFBOSS_EUDEMONS_MAX_TIRED);
        public void RequestEudemonsDeathDebuff() => SendFmt(Proto.KFBOSS_EUDEMONS_DEATH_DEBUFF);
        public void ReviveEudemonsBoss(int rawBossType, int bossId) => SendFmt(Proto.KFBOSS_EUDEMONS_REVIVE, "ci", rawBossType, bossId);

        private void On47000(NetReader r)
        {
            int rawType = r.ReadU8();
            int clientType = rawType + CROSS_BASE;
            int actStatus = r.ReadU8();
            long resetEtime = r.ReadU32();
            int tired = r.ReadU8();
            int maxTired = r.ReadU8();
            List<KfBossModel.CollectEntry> collectList = r.ReadArray(ReadCollectEntry);
            List<KfBossModel.EudemonsBossEntry> bossList = r.ReadArray(ReadEudemonsBossEntry);
            KfBossModel.Instance.ApplyEudemonsList(clientType, actStatus, resetEtime, tired, maxTired, collectList, bossList);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, clientType);
            GameLog.Info("KfBoss", "47000 千幻蜃楼列表 rawType={0} actStatus={1} tired={2}/{3} bossN={4}",
                rawType, actStatus, tired, maxTired, bossList.Count);
        }

        private static KfBossModel.CollectEntry ReadCollectEntry(NetReader r) => new KfBossModel.CollectEntry
        {
            Type = r.ReadU8(), CollectTimes = r.ReadU8(), TotalCollectTimes = r.ReadU8(),
        };

        private static KfBossModel.EudemonsBossEntry ReadEudemonsBossEntry(NetReader r) => new KfBossModel.EudemonsBossEntry
        {
            BossId = r.ReadI32(), Num = r.ReadU8(), RebornTime = r.ReadU32(), IsRemind = r.ReadU8() != 0,
        };

        private void On47002(NetReader r)
        {
            List<KfBossModel.CrossDropLogEntry> list = r.ReadArray(ReadCrossDropLogEntry);
            KfBossModel.Instance.ApplyEudemonsDropLog(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_DROP_LOG_UPDATE);
            GameLog.Info("KfBoss", "47002 千幻蜃楼掉落日志 count={0}", list.Count);
        }

        /// <summary>47002/46046 共用形态读取(RoleId:64,ServerId:16,ServerNum:16,Name,BossId:32,Layers:8,
        /// GoodsId:32,Rating:32,EquipExtraAttr[...],Time:32)。</summary>
        private static KfBossModel.CrossDropLogEntry ReadCrossDropLogEntry(NetReader r)
        {
            var e = new KfBossModel.CrossDropLogEntry
            {
                RoleId = r.ReadU64(), ServerId = r.ReadU16(), ServerNum = r.ReadU16(), Name = r.ReadString(),
                BossId = r.ReadI32(), Layers = r.ReadU8(), GoodsId = r.ReadI32(), Rating = r.ReadU32(),
            };
            e.EquipExtraAttr = r.ReadArray(ReadEquipExtraAttr);
            e.Time = r.ReadU32();
            return e;
        }

        private static BossModel.EquipExtraAttrEntry ReadEquipExtraAttr(NetReader r) => new BossModel.EquipExtraAttrEntry
        {
            Color = r.ReadU8(), TypeId = r.ReadU8(), AttrId = r.ReadU16(), AttrVal = r.ReadU32(),
            PlusInterval = r.ReadU8(), PlusUnit = r.ReadU32(),
        };

        private void On47003(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_ENTER_RESULT, true, code);
            if (code != 1) ShowError(code);
            GameLog.Info("KfBoss", "47003 进入千幻蜃楼 code={0}", code);
        }

        private void On47004(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_ENTER_RESULT, false, code);
            if (code != 1) ShowError(code);
            GameLog.Info("KfBoss", "47004 离开千幻蜃楼 code={0}", code);
        }

        private void On47005(NetReader r)
        {
            int code = r.ReadI32();
            int rawType = r.ReadU8();
            int clientType = rawType + CROSS_BASE;
            int bossId = r.ReadI32();
            int remind = r.ReadU8();
            if (code == 1)
            {
                KfBossModel.Instance.SetEudemonsRemind(clientType, bossId, remind != 0);
                TipsManager.Toast(remind == 0 ? "成功取消关注" : "关注成功");
                EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, clientType);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("KfBoss", "47005 千幻蜃楼关注 code={0} clientType={1} bossId={2} remind={3}", code, clientType, bossId, remind);
        }

        /// <summary>47006 重生提醒——纯事件转发,不落 KfBossModel(对标老端 On47006 只 Fire 打开提示弹窗,
        /// 不做数据落地)。⚠含服务端 kf_great_demon(20) 误发壳量,见 Proto.cs 注释,本处不特殊处理。</summary>
        private void On47006(NetReader r)
        {
            int rawType = r.ReadU8();
            int clientType = rawType + CROSS_BASE;
            int bossId = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_REBORN_TIP, clientType, bossId);
            GameLog.Info("KfBoss", "47006 千幻蜃楼重生提醒 clientType={0} bossId={1}(若 rawType==20 为服务端误发壳量,详见 Proto.cs)", clientType, bossId);
        }

        private void On47007(NetReader r)
        {
            int rawType = r.ReadU8();
            int clientType = rawType + CROSS_BASE;
            int bossId = r.ReadI32();
            long rebornTime = r.ReadU32();
            int num = r.ReadU8();
            KfBossModel.Instance.ApplyEudemonsReborn(clientType, bossId, rebornTime, num);
            if (clientType == KfBossModel.BossType.Holy)
            {
                // 对标老端 boss_type==holy 才 Fire KILL_BOSS——复用与 46009 相同的全局事件总线。
                EventDispatcher.Emit(GlobalEvent.EVT_BOSS_REBORN, clientType, bossId);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, clientType);
            GameLog.Info("KfBoss", "47007 千幻蜃楼被击杀信息 clientType={0} bossId={1} rebornTime={2} num={3}", clientType, bossId, rebornTime, num);
        }

        /// <summary>47008 怪物重生刷新信息——对标老端 RegisteredHandler 函数体为空,收到即弃(不落 KfBossModel、
        /// 不发事件),但服务端确认真会发送(见 Proto.cs 注释),故仍需注册防御 recv,只解析不消费。</summary>
        private void On47008(NetReader r)
        {
            int rawType = r.ReadU8();
            int bossId = r.ReadI32();
            long rebornTime = r.ReadU32();
            int num = r.ReadU8();
            GameLog.Info("KfBoss", "47008 重生刷新(防御recv,老端空处理) rawType={0} bossId={1} rebornTime={2} num={3}", rawType, bossId, rebornTime, num);
        }

        private void On47009(NetReader r)
        {
            int tired = r.ReadU8();
            KfBossModel.EudemonsState s = KfBossModel.Instance.GetEudemons(KfBossModel.BossType.Holy);
            if (s != null) s.Tired = tired;
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, KfBossModel.BossType.Holy);
            GameLog.Info("KfBoss", "47009 千幻蜃楼疲劳广播 tired={0}", tired);
        }

        private void On47010(NetReader r)
        {
            int code = r.ReadI32();
            if (code == KF_SERVER_ALLOT_CODE) TipsManager.Toast("正在获取千幻蜃楼信息 请稍候再试");
            else ShowError(code);
            GameLog.Info("KfBoss", "47010 千幻蜃楼同步占位码 code={0}(不做自动重试,需调用方重新发起47000/47021)", code);
        }

        private void On47015(NetReader r)
        {
            int rewardType = r.ReadU8();
            List<KfBossModel.RewardEntry> list = r.ReadArray(ReadRewardEntry);
            KfBossModel.Instance.SetEudemonsReward(rewardType, list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_SETTLE_REWARD);
            GameLog.Info("KfBoss", "47015 千幻蜃楼结算奖励 rewardType={0} count={1}", rewardType, list.Count);
        }

        private static KfBossModel.RewardEntry ReadRewardEntry(NetReader r) => new KfBossModel.RewardEntry
        {
            Type = r.ReadU8(), GoodsTypeId = r.ReadI32(), Num = r.ReadU32(), Id = r.ReadU64(),
        };

        /// <summary>47016 个人信息推送壳(老端 switch(key){} 空 case,占位壳,同 46025 处理方式)。</summary>
        private void On47016(NetReader r)
        {
            int len = r.ReadU16();
            for (int i = 0; i < len; i++) { r.ReadU8(); r.ReadU32(); } // Key,Val
            GameLog.Info("KfBoss", "47016 个人信息壳(占位) count={0}", len);
        }

        private void On47017(NetReader r)
        {
            int len = r.ReadU16();
            var dict = new Dictionary<int, List<KfBossModel.XY>>(len);
            for (int i = 0; i < len; i++)
            {
                int bossId = r.ReadI32();
                dict[bossId] = r.ReadArray(ReadXY);
            }
            KfBossModel.Instance.SetEudemonsBoxPos(dict);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, KfBossModel.BossType.Holy);
            GameLog.Info("KfBoss", "47017 千幻蜃楼宝箱坐标全量 count={0}", dict.Count);
        }

        private static KfBossModel.XY ReadXY(NetReader r) => new KfBossModel.XY { X = r.ReadU16(), Y = r.ReadU16() };

        private void On47018(NetReader r)
        {
            int bossId = r.ReadI32();
            List<KfBossModel.XY> xy = r.ReadArray(ReadXY);
            KfBossModel.Instance.AddEudemonsBoxPos(bossId, xy);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, KfBossModel.BossType.Holy);
            GameLog.Info("KfBoss", "47018 千幻蜃楼宝箱坐标更新 bossId={0} count={1}", bossId, xy.Count);
        }

        private void On47019(NetReader r)
        {
            int level = r.ReadU16();
            long exp = r.ReadU32();
            long addExp = r.ReadU32();
            KfBossModel.Instance.SetHuntLevel(level, exp, addExp);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, KfBossModel.BossType.Holy);
            if (addExp > 0) TipsManager.Toast("获得狩猎经验x" + addExp);
            GameLog.Info("KfBoss", "47019 狩猎等级信息 level={0} exp={1} addExp={2}", level, exp, addExp);
        }

        private void On47021(NetReader r)
        {
            List<KfBossModel.EudemonsRankEntry> list = r.ReadArray(ReadEudemonsRankEntry);
            KfBossModel.Instance.ApplyEudemonsRank(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, KfBossModel.BossType.Holy);
            GameLog.Info("KfBoss", "47021 圣兽领榜单 count={0}", list.Count);
        }

        private static KfBossModel.EudemonsRankEntry ReadEudemonsRankEntry(NetReader r) => new KfBossModel.EudemonsRankEntry
        {
            RoleId = r.ReadU64(), RoleName = r.ReadString(), ServerId = r.ReadU16(), ServerNum = r.ReadU16(),
            Score = r.ReadU32(), SortKey1 = r.ReadI32(), KillNum = r.ReadU16(), SortKey2 = r.ReadU32(),
            TotalScore = r.ReadU32(), SortKey3 = r.ReadU32(),
        };

        /// <summary>47022 玩家获得积分推送——老端 Handler 函数体为空,纯推送未消费,本端同样只落日志。</summary>
        private void On47022(NetReader r)
        {
            int scoreType = r.ReadU8();
            int scoreAdd = r.ReadU16();
            GameLog.Info("KfBoss", "47022 积分推送(占位) scoreType={0} scoreAdd={1}", scoreType, scoreAdd);
        }

        private void On47023(NetReader r)
        {
            int maxTired = r.ReadU8();
            KfBossModel.EudemonsState s = KfBossModel.Instance.GetEudemons(KfBossModel.BossType.Holy);
            if (s != null) s.MaxTired = maxTired;
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_UPDATE, KfBossModel.BossType.Holy);
            GameLog.Info("KfBoss", "47023 最大疲劳刷新 maxTired={0}", maxTired);
        }

        /// <summary>47034 千幻蜃楼死亡debuff——转发 ReliveModel.HolyBoss 槽位(spec 明示接线点,与 46034 的
        /// WorldBoss 槽位并列)。</summary>
        private void On47034(NetReader r)
        {
            int dieTimes = r.ReadU16();
            long nextEnterTime = r.ReadU32();
            long debuffEndTime = r.ReadU32();
            long safeEndTime = r.ReadU32();
            ReliveModel.Instance.SetHolyBossDieInfo(dieTimes, nextEnterTime, debuffEndTime, safeEndTime);
            GameLog.Info("KfBoss", "47034 千幻蜃楼死亡debuff dieTimes={0} nextEnterTime={1} debuffEnd={2} safeEnd={3}",
                dieTimes, nextEnterTime, debuffEndTime, safeEndTime);
        }

        /// <summary>47035 复活千幻蜃楼boss。老端成功分支固定补发 47000 boss_type=1(硬编码,不取响应字段——
        /// 照抄老端行为镜像,不做"看起来更合理"的改写)。</summary>
        private void On47035(NetReader r)
        {
            int errcode = r.ReadI32();
            int rawType = r.ReadU8();
            int clientType = rawType + CROSS_BASE;
            int bossId = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_EUDEMONS_REVIVE_RESULT, errcode == 1, clientType, bossId);
            if (errcode == 1)
            {
                TipsManager.Toast("复活成功");
                RequestEudemonsBossList(1); // 老端硬编码 SendFmtToGame(47000,"c",1)
            }
            else
            {
                ShowError(errcode);
            }
            GameLog.Info("KfBoss", "47035 复活千幻蜃楼boss errcode={0} clientType={1} bossId={2}", errcode, clientType, bossId);
        }

        // ---------------------------------------------------------------------------------------
        // pt_471 镇煞封魂/幻域Boss(Decoration)
        // ---------------------------------------------------------------------------------------

        public void RequestDecorationInfo() => SendFmt(Proto.KFBOSS_DECORATION_INFO);

        /// <summary>C2S "ic" boss_id,type(1=普通进入/2=协助进入,对标老端 `_help && 2 || 1`)。</summary>
        public void EnterDecorationBoss(int bossId, int type) => SendFmt(Proto.KFBOSS_DECORATION_ENTER, "ic", bossId, type);
        public void LeaveDecorationBoss() => SendFmt(Proto.KFBOSS_DECORATION_LEAVE);
        public void BuyDecorationCount() => SendFmt(Proto.KFBOSS_DECORATION_BUY_COUNT);
        public void RequestDecorationUnfollowList() => SendFmt(Proto.KFBOSS_DECORATION_UNFOLLOW_LIST);
        public void SetDecorationFollowReq(int bossId, bool follow) => SendFmt(Proto.KFBOSS_DECORATION_FOLLOW, "ic", bossId, follow ? 1 : 0);
        public void RequestDecorationDropLog() => SendFmt(Proto.KFBOSS_DECORATION_DROP_LOG);
        public void RequestDecorationRank() => SendFmt(Proto.KFBOSS_DECORATION_RANK);
        public void EnterDecorationSpecialBoss() => SendFmt(Proto.KFBOSS_DECORATION_ENTER_SPECIAL);
        /// <summary>仙宗召援(镇煞封魂场景内独立入口,勿与 Guild 40060 混淆)。</summary>
        public void RequestDecorationGuildHelp() => SendFmt(Proto.KFBOSS_DECORATION_GUILD_HELP);
        public void RequestDecorationSceneInfo() => SendFmt(Proto.KFBOSS_DECORATION_SCENE_INFO);

        private void On47101(NetReader r)
        {
            int actStatus = r.ReadU8();
            int count = r.ReadU8();
            int assistCount = r.ReadU8();
            int buyCount = r.ReadU8();
            int addCount = r.ReadU8();
            int inBuff = r.ReadU8();
            int killCount = r.ReadU16();
            int isAlive = r.ReadU8();
            int sbossRoleNum = r.ReadU8();
            List<KfBossModel.DecorationBossEntry> list = r.ReadArray(ReadDecorationBossEntry);
            KfBossModel.Instance.ApplyDecorationInfo(actStatus, count, assistCount, buyCount, addCount,
                inBuff != 0, killCount, isAlive != 0, sbossRoleNum, list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47101 镇煞封魂主信息 actStatus={0} count={1} bossN={2}", actStatus, count, list.Count);
        }

        private static KfBossModel.DecorationBossEntry ReadDecorationBossEntry(NetReader r) => new KfBossModel.DecorationBossEntry
        {
            BossId = r.ReadI32(), RebornTime = r.ReadU32(), RoleNum = r.ReadU8(), IsHadAssist = r.ReadU8() != 0,
        };

        private void On47102(NetReader r)
        {
            int code = r.ReadI32();
            int bossId = r.ReadI32();
            int type = r.ReadU8();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_ENTER_RESULT, code);
            if (code != 1) ShowError(code);
            GameLog.Info("KfBoss", "47102 进入镇煞封魂boss code={0} bossId={1} type={2}", code, bossId, type);
        }

        private void On47103(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1) ShowError(code);
            GameLog.Info("KfBoss", "47103 退出镇煞封魂 code={0}", code);
        }

        private void On47104(NetReader r)
        {
            int code = r.ReadI32();
            if (code == 1)
            {
                KfBossModel.Instance.IncrementDecorationBuyCount();
                EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("KfBoss", "47104 购买进入次数 code={0}", code);
        }

        private void On47105(NetReader r)
        {
            List<int> list = r.ReadArray(rr => rr.ReadI32());
            KfBossModel.Instance.SetDecorationUnfollowList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47105 取消关注列表 count={0}", list.Count);
        }

        private void On47106(NetReader r)
        {
            int code = r.ReadI32();
            int bossId = r.ReadI32();
            int isFollow = r.ReadU8();
            if (code == 1)
            {
                KfBossModel.Instance.SetDecorationFollow(bossId, isFollow != 0);
                TipsManager.Toast(isFollow != 0 ? "关注成功" : "取消关注");
                EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("KfBoss", "47106 关注/取关 code={0} bossId={1} isFollow={2}", code, bossId, isFollow);
        }

        private void On47107(NetReader r)
        {
            int bossId = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_REVIVE_TIP, bossId);
            GameLog.Info("KfBoss", "47107 boss复活通知 bossId={0}", bossId);
        }

        private void On47108(NetReader r)
        {
            List<KfBossModel.DecorationDropLogEntry> list = r.ReadArray(ReadDecorationDropLogEntry);
            KfBossModel.Instance.ApplyDecorationDropLog(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_DROP_LOG_UPDATE);
            GameLog.Info("KfBoss", "47108 镇煞封魂掉落日志 count={0}", list.Count);
        }

        private static KfBossModel.DecorationDropLogEntry ReadDecorationDropLogEntry(NetReader r)
        {
            var e = new KfBossModel.DecorationDropLogEntry
            {
                RoleId = r.ReadU64(), ServerId = r.ReadU16(), ServerNum = r.ReadU16(), Name = r.ReadString(),
                BossId = r.ReadI32(), GoodsId = r.ReadI32(), Num = r.ReadU32(), Rating = r.ReadU32(),
            };
            e.EquipExtraAttr = r.ReadArray(ReadEquipExtraAttr);
            e.Time = r.ReadU32();
            return e;
        }

        private void On47109(NetReader r)
        {
            List<KfBossModel.DecorationRankEntry> list = r.ReadArray(ReadDecorationRankEntry);
            KfBossModel.Instance.ApplyDecorationRank(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47109 特殊boss排名 count={0}", list.Count);
        }

        private static KfBossModel.DecorationRankEntry ReadDecorationRankEntry(NetReader r) => new KfBossModel.DecorationRankEntry
        {
            RoleId = r.ReadU64(), Name = r.ReadString(), ServerId = r.ReadU16(), ServerNum = r.ReadU16(),
            ServerName = r.ReadString(), Hurt = r.ReadU64(),
        };

        private void On47110(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1) ShowError(code);
            GameLog.Info("KfBoss", "47110 进入特殊boss code={0}", code);
        }

        private void On47111(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_GUILD_HELP_RESULT, code);
            if (code != 1) ShowError(code);
            GameLog.Info("KfBoss", "47111 仙宗召援 code={0}", code);
        }

        private void On47112(NetReader r)
        {
            KfBossModel.DecorationRankEntry e = ReadDecorationRankEntry(r);
            KfBossModel.Instance.PatchDecorationRank(e);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47112 特殊boss伤害推送 roleId={0} hurt={1}", e.RoleId, e.Hurt);
        }

        private void On47113(NetReader r)
        {
            bool isBelong = r.ReadU8() != 0;
            bool isDouble = r.ReadU8() != 0;
            List<KfBossModel.DecorationRewardGroup> list1 = r.ReadArray(ReadDecorationRewardGroup);
            List<KfBossModel.DecorationRewardGroup> list2 = r.ReadArray(ReadDecorationRewardGroup);
            var result = new KfBossModel.DecorationSettleResult { IsBelong = isBelong, IsDouble = isDouble };
            result.RewardTypeList.AddRange(list1);
            result.RewardTypeList2.AddRange(list2);
            KfBossModel.Instance.SetDecorationSettle(result);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_SETTLE);
            GameLog.Info("KfBoss", "47113 镇煞封魂结算 isBelong={0} isDouble={1} g1={2} g2={3}", isBelong, isDouble, list1.Count, list2.Count);
        }

        private static KfBossModel.DecorationRewardGroup ReadDecorationRewardGroup(NetReader r)
        {
            var g = new KfBossModel.DecorationRewardGroup { RewardType = r.ReadU8() };
            g.Items.AddRange(r.ReadArray(ReadDecorationRewardItem));
            return g;
        }

        private static KfBossModel.DecorationRewardItem ReadDecorationRewardItem(NetReader r) => new KfBossModel.DecorationRewardItem
        {
            Style = r.ReadU8(), TypeId = r.ReadI32(), Count = r.ReadU32(), GoodsId = r.ReadU64(),
        };

        private void On47114(NetReader r)
        {
            int enterType = r.ReadU8();
            long quitTime = r.ReadU32();
            long reviveTime = r.ReadU32();
            KfBossModel.Instance.SetDecorationSceneInfo(enterType, quitTime, reviveTime);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47114 战斗场景信息 enterType={0} quitTime={1} reviveTime={2}", enterType, quitTime, reviveTime);
        }

        private void On47115(NetReader r)
        {
            long quitTime = r.ReadU32();
            KfBossModel.Instance.SetDecorationQuitTime(quitTime);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47115 退出时间刷新 quitTime={0}", quitTime);
        }

        private void On47116(NetReader r)
        {
            long reviveTime = r.ReadU32();
            KfBossModel.Instance.SetDecorationReviveTime(reviveTime);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE);
            GameLog.Info("KfBoss", "47116 复活时间刷新 reviveTime={0}", reviveTime);
        }

        /// <summary>47117 boss/特殊boss死亡广播——对标老端 RegisteredHandler 函数体为空,收到即弃(不落
        /// KfBossModel、不发事件),但服务端确认真会 send_to_all 全服广播(见 Proto.cs 注释),按活号防御 recv。</summary>
        private void On47117(NetReader r)
        {
            int bossId = r.ReadI32();
            long rebornTime = r.ReadU32();
            GameLog.Info("KfBoss", "47117 boss死亡广播(防御recv,老端空处理) bossId={0} rebornTime={1}", bossId, rebornTime);
        }

        // ---------------------------------------------------------------------------------------
        // pt_619 论剑恩怨簿(PkLog)
        // ---------------------------------------------------------------------------------------

        public void RequestKillRecord() => SendFmt(Proto.KFBOSS_KILL_RECORD_LIST);

        private void On61900(NetReader r)
        {
            List<KfBossModel.KillRecordEntry> local = r.ReadArray(ReadKillRecordEntry);
            List<KfBossModel.KfKillRecordEntry> kf = r.ReadArray(ReadKfKillRecordEntry);
            KfBossModel.Instance.ApplyKillRecord(local, kf);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_KILL_RECORD_UPDATE);
            GameLog.Info("KfBoss", "61900 论剑恩怨簿全量 local={0} kf={1}", local.Count, kf.Count);
        }

        private static KfBossModel.KillRecordEntry ReadKillRecordEntry(NetReader r) => new KfBossModel.KillRecordEntry
        {
            Sign = r.ReadU8(), Time = r.ReadU32(), SceneName = r.ReadString(), AttrName = r.ReadString(), AttrId = r.ReadU64(),
        };

        /// <summary>61902 KfSendList 单条形态:ServerId/ServerNum 用 32 位(pt_619 家族独例,与 47xxx/46xxx
        /// 系普遍 16 位不同,见 r15b 实证)。</summary>
        private static KfBossModel.KfKillRecordEntry ReadKfKillRecordEntry(NetReader r) => new KfBossModel.KfKillRecordEntry
        {
            Sign = r.ReadU8(), Time = r.ReadU32(), SceneName = r.ReadString(), ServerId = r.ReadI32(), ServerNum = r.ReadI32(),
            AttrName = r.ReadString(), AttrId = r.ReadU64(),
        };

        private void On61901(NetReader r)
        {
            KfBossModel.KillRecordEntry e = ReadKillRecordEntry(r);
            KfBossModel.Instance.AddKillRecord(e);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_KILL_RECORD_UPDATE);
            GameLog.Info("KfBoss", "61901 本服新击杀记录 attrName={0} attrId={1}", e.AttrName, e.AttrId);
        }

        private void On61902(NetReader r)
        {
            KfBossModel.KfKillRecordEntry e = ReadKfKillRecordEntry(r);
            KfBossModel.Instance.AddKfKillRecord(e);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_KILL_RECORD_UPDATE);
            GameLog.Info("KfBoss", "61902 跨服击杀记录 attrName={0} attrId={1}", e.AttrName, e.AttrId);
        }

        // ---------------------------------------------------------------------------------------
        // pt_460 内 kf_great_demon 壳(46037-39/46046,跨服太古遗凶专属)
        // ---------------------------------------------------------------------------------------

        public void RequestGreatDemonRewardState() => SendFmt(Proto.KFBOSS_GREAT_DEMON_REWARD_STATE);
        public void TakeGreatDemonReward(int rewardId) => SendFmt(Proto.KFBOSS_GREAT_DEMON_REWARD_TAKE, "i", rewardId);
        public void RequestGreatDemonBoxInfo() => SendFmt(Proto.KFBOSS_GREAT_DEMON_BOX_INFO);
        /// <summary>C2S "h" boss_type,老端固定传 BossType.mystery(即 BossModel.BossType.KfGreatDemon=20)。</summary>
        public void RequestGreatDemonDropLog() => SendFmt(Proto.KFBOSS_GREAT_DEMON_DROP_LOG, "h", BossModel.BossType.KfGreatDemon);

        private void On46037(NetReader r)
        {
            int killNum = r.ReadI32();
            List<int> stages = r.ReadArray(rr => (int)rr.ReadU16());
            KfBossModel.Instance.SetGreatDemonReward(killNum, stages);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE);
            GameLog.Info("KfBoss", "46037 太古遗凶阶段奖励状态 killNum={0} stages={1}", killNum, stages.Count);
        }

        private void On46038(NetReader r)
        {
            int rewardId = r.ReadI32();
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE);
            if (code == 1)
            {
                TipsManager.Toast("领取奖励成功");
                RequestGreatDemonRewardState(); // 对标老端成功后 Fire(SCMD_REQUEST,46037) 重拉
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("KfBoss", "46038 领取太古遗凶阶段奖励 rewardId={0} code={1}", rewardId, code);
        }

        private void On46039(NetReader r)
        {
            List<KfBossModel.GreatDemonBoxEntry> list = r.ReadArray(ReadGreatDemonBoxEntry);
            KfBossModel.Instance.SetGreatDemonBoxList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_BOX_UPDATE);
            GameLog.Info("KfBoss", "46039 太古遗凶进场景宝箱信息 count={0}", list.Count);
        }

        private static KfBossModel.GreatDemonBoxEntry ReadGreatDemonBoxEntry(NetReader r)
        {
            var e = new KfBossModel.GreatDemonBoxEntry { BossId = r.ReadI32() };
            e.XyList.AddRange(r.ReadArray(ReadXY));
            return e;
        }

        private void On46046(NetReader r)
        {
            int bossType = r.ReadU16(); // wire BossType:16(与 460 系普遍 8 位不同,本号独例)
            List<KfBossModel.CrossDropLogEntry> list = r.ReadArray(ReadCrossDropLogEntry);
            KfBossModel.Instance.ApplyGreatDemonDropLog(list);
            EventDispatcher.Emit(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_DROP_LOG_UPDATE);
            GameLog.Info("KfBoss", "46046 太古遗凶掉落日志 bossType={0} count={1}", bossType, list.Count);
        }
    }
}
