using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Boss 家族一期·本服核心(自动循环 轮15a)实证:46000 段主链(列表/进入判定/刷新推送订正/关注/
    /// 击杀日志100条/掉落日志/体力/结算奖励/伤害榜自己+前3防抖/连杀通知/死亡debuff转发ReliveModel/
    /// 消耗复活/找回体力code字段订正)+ 免战(20201-205)+ 采集(20025-26)+ 死号未注册断言 + 边界失败码
    /// 各一发,合成包驱动 BossController 反射喂包(模板 RankCase/DungeonFamilyCase,纯逻辑段)。
    ///
    /// 本轮 UI 层(BossHelpPanelView/BossHelpItemView)已接真数据链(GuildModel 协助状态门控),但宿主
    /// BossFightSceneView 仍是死分支(战斗场景 HUD 超出本轮范围)、BossEnterView 五Tab壳未 convert-module
    /// (r15_unity §2 已证实缺失)——渲染段无处安放,不写(同 RankCase 先例:纯数据层轮无渲染段)。
    ///
    /// 死号裁决(逐号与 yu_server 源码直接核对调用点,订正了侦察子报告的 3 处误判——46001/46019/46024
    /// 经核实其实"活",改按 wire 权威实现;46030 核实 write 调用点确实被注释,真死):
    ///   真死不注册:46030(write 调用被注释)/46018/46020/46023/46032(C2S 均被老端弃用且非自主
    ///   推送,我方不发起请求则永不可达)/46037/46038/46039/46046(pp_boss.erl handle 无条件转发
    ///   mod_great_demon_local,跨服壳复用本号段)。
    ///
    /// **修复轮订正**:46013/46031 初审误判"非自主推送不可达"——直接核实服务端 send_to_scene/send_to_uid
    /// 均无条件真推送(详见 Proto.cs 注释),已改为防御 recv 登记,不再列入死号;46026-46029/46033(节日boss
    /// 场景组)+46040(血条百分比)同样补注册防御 recv;46016/46008 补 RebornTime 复位;46041/46042 成功分支
    /// 补 RequestBossList 重拉。
    /// </summary>
    public static class BossCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Boss.BossConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Boss.BossConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY boss FAIL BossConfigs not loaded");
                    return 3;
                }
                bool configOk = Shenxiao.Module.Core.Boss.BossConfigs.BossTypeCount == 17
                    && Shenxiao.Module.Core.Boss.BossConfigs.BossCfgCount == 206
                    && Shenxiao.Module.Core.Boss.BossConfigs.BossTypeKvCount == 92
                    && Shenxiao.Module.Core.Boss.BossConfigs.ShowHpCount == 68
                    && Shenxiao.Module.Core.Boss.BossConfigs.DomainKillRewardCount == 3
                    && Shenxiao.Module.Core.Boss.BossConfigs.DecorationBossCount == 25;
                Debug.Log("CLIVERIFY boss config type=" + Shenxiao.Module.Core.Boss.BossConfigs.BossTypeCount
                    + " cfg=" + Shenxiao.Module.Core.Boss.BossConfigs.BossCfgCount
                    + " kv=" + Shenxiao.Module.Core.Boss.BossConfigs.BossTypeKvCount
                    + " showHp=" + Shenxiao.Module.Core.Boss.BossConfigs.ShowHpCount
                    + " domainReward=" + Shenxiao.Module.Core.Boss.BossConfigs.DomainKillRewardCount
                    + " decoration=" + Shenxiao.Module.Core.Boss.BossConfigs.DecorationBossCount
                    + " ok=" + configOk);

                Shenxiao.Module.Core.Boss.BossModel model = Shenxiao.Module.Core.Boss.BossModel.Instance;
                model.Clear46000();
                Shenxiao.Module.Core.Relive.ReliveModel.Instance.Clear();

                object ctrl = Shenxiao.Module.Core.Boss.BossController.Instance;
                System.Type t = ctrl.GetType();
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY boss handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }

                // ---- A. 死号未注册断言(代表性子集,详见类注释裁决) ----
                string[] deadHandlers =
                {
                    "On46030", "On46018", "On46020", "On46023", "On46032",
                    "On46037", "On46038", "On46039", "On46046",
                };
                bool deadOk = true;
                foreach (string name in deadHandlers)
                {
                    if (t.GetMethod(name, F) != null) { deadOk = false; Debug.LogError("CLIVERIFY boss 死号仍被注册: " + name); }
                }
                Debug.Log("CLIVERIFY boss 死号未注册断言 ok=" + deadOk);

                // ---- B. 46000 boss列表(蛮荒 Suit=4,2条 boss_info) ----
                const int SUIT = Shenxiao.Module.Core.Boss.BossModel.BossType.Suit;
                byte[] p46000 = new CliVerify.Pkt()
                    .C(SUIT).C(5).C(3).H(10).H(100).H(0).I(0).C(0).C(0)
                    .H(2)
                        .I(4000001).C(1).I(0).C(1).C(0)
                        .I(4000002).C(0).I(1700000000).C(0).C(0)
                    .Bytes();
                Feed("On46000", p46000);
                var suitState = model.GetBossState(SUIT);
                bool b46000 = suitState != null && suitState.HasData && suitState.AllCount == 5 && suitState.Count == 3
                    && suitState.Tired == 10 && suitState.BossList.Count == 2
                    && suitState.GetEntry(4000001).IsAlive && !suitState.GetEntry(4000002).IsAlive
                    && suitState.GetEntry(4000002).RebornTime == 1700000000;
                Debug.Log("CLIVERIFY boss 46000 list count=" + (suitState?.BossList.Count ?? -1) + " ok=" + b46000);

                // ---- C. 46009 订正后类型门(rule10):Suit(4,在名单内)触发通知;Home(3,不在名单内)不触发,但列表仍更新 ----
                int killBossFireCount = 0;
                System.Action<int, int> onKillBoss = (bt, bi) => killBossFireCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_REBORN, onKillBoss);
                Feed("On46009", new CliVerify.Pkt().C(SUIT).I(4000001).I(1700001000).C(1).Bytes());
                bool notifyFired = killBossFireCount == 1;
                const int HOME = Shenxiao.Module.Core.Boss.BossModel.BossType.Home;
                Feed("On46009", new CliVerify.Pkt().C(HOME).I(3000001).I(1700002000).C(1).Bytes());
                bool notifyNotFiredForHome = killBossFireCount == 1; // 未再 +1
                bool homeListUpdated = model.GetBossState(HOME)?.GetEntry(3000001)?.RebornTime == 1700002000;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_REBORN, onKillBoss);
                bool b46009 = notifyFired && notifyNotFiredForHome && homeListUpdated;
                Debug.Log("CLIVERIFY boss 46009 rule10 notifyFired=" + notifyFired
                    + " homeNotSuppressed=" + notifyNotFiredForHome + " homeListUpdated=" + homeListUpdated + " ok=" + b46009);

                // ---- D. 46001 击杀日志(100条硬顶,?BOSS_LOG_LEN) ----
                var killPkt = new CliVerify.Pkt().H(100);
                for (int i = 0; i < 100; i++) killPkt = killPkt.I(1700000000 + i).L(9000 + i).S("甲" + i);
                Feed("On46001", killPkt.Bytes());
                bool b46001 = model.HasKillLog && model.KillLog.Count == 100 && model.KillLog[99].RoleId == 9099;
                Debug.Log("CLIVERIFY boss 46001 killLog count=" + model.KillLog.Count + " ok=" + b46001);

                // ---- E. 46002 掉落日志(1条,含1个装备附加属性,IsTop=1) ----
                byte[] p46002 = new CliVerify.Pkt().H(1)
                    .I(1700000000).L(9001).S("乙").C(SUIT).I(4000001).I(101000001).I(1).I(500)
                    .H(1).C(3).C(1).H(10).I(200).C(0).I(0)
                    .C(1)
                    .Bytes();
                Feed("On46002", p46002);
                bool b46002 = model.HasDropLog && model.DropLog.Count == 1
                    && model.DropLog[0].EquipExtraAttr.Count == 1 && model.DropLog[0].IsTop;
                Debug.Log("CLIVERIFY boss 46002 dropLog count=" + model.DropLog.Count + " ok=" + b46002);

                // ---- F. 46003/46004 进出场景(成功+失败各一,边界失败码) ----
                int enterResultCount = 0; bool lastEnterOk = false;
                System.Action<bool, int> onEnter = (isEnter, code) => { enterResultCount++; lastEnterOk = code == 1; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_ENTER_RESULT, onEnter);
                Feed("On46003", new CliVerify.Pkt().I(1).Bytes());
                bool b46003ok = lastEnterOk;
                Feed("On46003", new CliVerify.Pkt().I(4600011).Bytes()); // err460_not_enough_vit_to_enter,边界失败码
                bool b46003fail = !lastEnterOk;
                Feed("On46004", new CliVerify.Pkt().I(1).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_ENTER_RESULT, onEnter);
                bool b4600304 = enterResultCount == 3 && b46003ok && b46003fail;
                Debug.Log("CLIVERIFY boss 46003/46004 enter/leave events=" + enterResultCount + " ok=" + b4600304);

                // ---- G. 46007 关注(成功更新条目 + 失败边界码) ----
                Feed("On46007", new CliVerify.Pkt().I(1).C(SUIT).I(4000001).C(1).C(1).Bytes());
                bool b46007ok = model.GetBossState(SUIT).GetEntry(4000001).IsRemind;
                Feed("On46007", new CliVerify.Pkt().I(4600002).C(SUIT).I(4000001).C(0).C(0).Bytes()); // err460_no_boss_type,边界失败码
                bool b46007 = b46007ok; // 失败分支只需不抛异常(已到此行即成立)
                Debug.Log("CLIVERIFY boss 46007 remind ok=" + b46007);

                // ---- H. 46008(复用46016解析)/46016 击杀提醒(+ RebornTime 复位,修复轮订正) ----
                // 4000002 在段 B 的 46000 列表里带 RebornTime=1700000000(死亡冷却中);46008 复用 46016 解析,
                // 收到复活提醒后应把该条目 RebornTime 复位为 0(对标老端 SetBossRebornTime(bt,bi,0))。
                int killedNoticeCount = 0;
                System.Action<int, int> onKilled = (bt, bi) => killedNoticeCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_KILLED_NOTICE, onKilled);
                Feed("On46016", new CliVerify.Pkt().C(SUIT).I(4000001).Bytes());
                Feed("On46008", new CliVerify.Pkt().C(SUIT).I(4000002).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_KILLED_NOTICE, onKilled);
                bool b46016RebornReset = model.GetBossState(SUIT).GetEntry(4000002).RebornTime == 0;
                bool b46016 = killedNoticeCount == 2 && b46016RebornReset;
                Debug.Log("CLIVERIFY boss 46008/46016 killedNotice count=" + killedNoticeCount
                    + " rebornReset=" + b46016RebornReset + " ok=" + b46016);

                // ---- I. 46019 伤害榜前3(防抖广播)+ 46022 伤害榜-自己(非拉取) ----
                byte[] p46019 = new CliVerify.Pkt().H(3)
                    .S("甲").I(50000)
                    .S("乙").I(30000)
                    .S("丙").I(10000)
                    .Bytes();
                Feed("On46019", p46019);
                bool b46019 = model.DamageTop3.Count == 3 && model.DamageTop3[0].RoleName == "甲" && model.DamageTop3[0].Damage == 50000;
                Feed("On46022", new CliVerify.Pkt().C(1).I(50000).S("甲").I(200).Bytes());
                bool b46022 = model.DamageSelf.HasData && model.DamageSelf.SelfRank == 1 && model.DamageSelf.SelfDamage == 50000;
                Debug.Log("CLIVERIFY boss 46019 top3=" + model.DamageTop3.Count + " 46022 self=" + model.DamageSelf.HasData
                    + " ok=" + (b46019 && b46022));

                // ---- J. 46024 连杀通知(FigureProto 共用块,不抛异常即过——播报节流留TODO) ----
                bool dkillNoThrow = true;
                try { Feed("On46024", new CliVerify.Pkt().L(9001).AppendMinimalFigure("甲").H(3).Bytes()); }
                catch (System.Exception e) { dkillNoThrow = false; Debug.LogError("CLIVERIFY boss 46024 threw: " + e); }
                Debug.Log("CLIVERIFY boss 46024 dkill noThrow=" + dkillNoThrow);

                // ---- K. 46025 role信息壳(占位,不抛异常) ----
                bool infoNoThrow = true;
                try { Feed("On46025", new CliVerify.Pkt().H(2).C(1).I(100).C(2).I(200).Bytes()); }
                catch (System.Exception e) { infoNoThrow = false; Debug.LogError("CLIVERIFY boss 46025 threw: " + e); }

                // ---- L. 46034 死亡debuff → 转发 ReliveModel(spec 明示接线点) ----
                Feed("On46034", new CliVerify.Pkt().H(3).I(1700003000).I(1700003300).I(1700003060).Bytes());
                Shenxiao.Module.Core.Relive.ReliveModel rlModel = Shenxiao.Module.Core.Relive.ReliveModel.Instance;
                bool b46034 = rlModel.BossDieTimes == 3 && rlModel.BossNextEnterTime == 1700003000
                    && rlModel.BossDebuffEndTime == 1700003300 && rlModel.BossSafeEndTime == 1700003060;
                Debug.Log("CLIVERIFY boss 46034→ReliveModel dieTimes=" + rlModel.BossDieTimes + " ok=" + b46034);

                // ---- M. 46035/46036(辅助广播,不抛异常) ----
                bool auxNoThrow = true;
                try
                {
                    Feed("On46035", new CliVerify.Pkt().C(Shenxiao.Module.Core.Boss.BossModel.BossType.Mystery).C(2).Bytes());
                    Feed("On46036", new CliVerify.Pkt().I(4000001).H(2).H(100).H(200).H(150).H(250).Bytes());
                }
                catch (System.Exception e) { auxNoThrow = false; Debug.LogError("CLIVERIFY boss 46035/36 threw: " + e); }

                // ---- N. 46041 消耗复活(成功+失败边界码) ----
                int reviveCount = 0; bool lastReviveOk = false;
                System.Action<bool, int, int> onRevive = (ok, bt, bi) => { reviveCount++; lastReviveOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_REVIVE_RESULT, onRevive);
                Feed("On46041", new CliVerify.Pkt().I(1).C(SUIT).I(4000001).Bytes());
                bool b46041ok = lastReviveOk;
                Feed("On46041", new CliVerify.Pkt().I(4600017).C(SUIT).I(4000001).Bytes()); // err460_boss_die,边界失败码
                bool b46041fail = !lastReviveOk;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_REVIVE_RESULT, onRevive);
                bool b46041 = reviveCount == 2 && b46041ok && b46041fail;
                Debug.Log("CLIVERIFY boss 46041 revive events=" + reviveCount + " ok=" + b46041);

                // ---- O. 46042/46043(空包/广播,不抛异常) ----
                bool ackNoThrow = true;
                try
                {
                    Feed("On46042", new CliVerify.Pkt().C(SUIT).I(4000001).Bytes());
                    Feed("On46043", new CliVerify.Pkt().Bytes());
                }
                catch (System.Exception e) { ackNoThrow = false; Debug.LogError("CLIVERIFY boss 46042/43 threw: " + e); }

                // ---- P. 46044 体力详情 ----
                Feed("On46044", new CliVerify.Pkt().H(80).H(100).H(5).H(20).I(1700000000).Bytes());
                var vit = model.GetVit(Shenxiao.Module.Core.Boss.BossModel.BossType.Field);
                bool b46044 = vit != null && vit.HasData && vit.Vit == 80 && vit.MaxVit == 100;
                Debug.Log("CLIVERIFY boss 46044 vit=" + (vit?.Vit ?? -1) + "/" + (vit?.MaxVit ?? -1) + " ok=" + b46044);

                // ---- Q. 46045 找回体力(rule9 订正:S2C 只有 Code:32 一个字段,精确4字节包不抛越界异常
                //          即证明本端未照抄老端读取不存在的 errcode 字段) ----
                int vitRecoverCount = 0; bool lastVitRecoverOk = false;
                System.Action<bool> onVitRecover = ok => { vitRecoverCount++; lastVitRecoverOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_VIT_RECOVER_RESULT, onVitRecover);
                bool b46045NoThrow = true;
                try
                {
                    Feed("On46045", new CliVerify.Pkt().I(1).Bytes());       // 成功,精确4字节
                    Feed("On46045", new CliVerify.Pkt().I(4600004).Bytes()); // 失败(err460_count_max),边界失败码,同样精确4字节
                }
                catch (System.Exception e) { b46045NoThrow = false; Debug.LogError("CLIVERIFY boss 46045 threw (rule9 regression!): " + e); }
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_VIT_RECOVER_RESULT, onVitRecover);
                bool b46045 = b46045NoThrow && vitRecoverCount == 2 && !lastVitRecoverOk;
                Debug.Log("CLIVERIFY boss 46045 rule9 noThrow=" + b46045NoThrow + " events=" + vitRecoverCount + " ok=" + b46045);

                // ---- R. 采集 20025/20026 ----
                List<long> collectRoles = null;
                System.Action<List<long>> onCollect = list => collectRoles = list;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_COLLECT_UPDATE, onCollect);
                Feed("On20025", new CliVerify.Pkt().H(2).L(7001).L(7002).Bytes());
                bool b20025 = collectRoles != null && collectRoles.Count == 2 && collectRoles[0] == 7001;
                Feed("On20026", new CliVerify.Pkt().L(7003).Bytes());
                bool b20026 = collectRoles != null && collectRoles.Count == 1 && collectRoles[0] == 7003;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_COLLECT_UPDATE, onCollect);
                Debug.Log("CLIVERIFY boss 20025/20026 collect ok=" + (b20025 && b20026));

                // ---- S. 免战 20201-205 ----
                Feed("On20201", new CliVerify.Pkt().H(2).I(4).I(300).I(1).I(6).I(600).I(0).Bytes());
                bool b20201 = model.HasWarFreeList && model.WarFreeList.Count == 2;

                Feed("On20202", new CliVerify.Pkt().I(1).I(4).I(300).I(2).Bytes());
                bool b20202ok = model.WarFreeList.Find(e => e.SceneType == 4)?.UseCount == 2;
                Feed("On20202", new CliVerify.Pkt().I(4600001).I(4).I(300).I(2).Bytes()); // 边界失败码,不抛异常即过

                long nowSec = Shenxiao.Framework.Util.TimeUtil.NowSec();
                Feed("On20203", new CliVerify.Pkt().I((int)(nowSec + 100)).Bytes());
                bool b20203 = model.WarFreeEndTimeLeft > 90 && model.WarFreeEndTimeLeft <= 100;
                Feed("On20203", new CliVerify.Pkt().I(0).Bytes());
                bool b20203zero = model.WarFreeEndTimeLeft == 0;

                Feed("On20204", new CliVerify.Pkt().I(6).I(600).I(1).Bytes());
                bool b20204 = model.WarFreeList.Find(e => e.SceneType == 6)?.UseCount == 1;

                Feed("On20205", new CliVerify.Pkt().I(1).I(4).Bytes());
                bool b20205ok = model.WarFreeEndTimeLeft == 0;
                Feed("On20205", new CliVerify.Pkt().I(4600001).I(4).Bytes()); // 边界失败码,不抛异常即过

                bool bWarFree = b20201 && b20202ok && b20203 && b20203zero && b20204 && b20205ok;
                Debug.Log("CLIVERIFY boss warfree 20201=" + b20201 + " 20202=" + b20202ok + " 20203=" + b20203
                    + "/" + b20203zero + " 20204=" + b20204 + " 20205=" + b20205ok + " ok=" + bWarFree);

                // ---- T. 46013/46031(修复轮订正:初审误判"不可达",实为服务端主动推送——防御recv不抛异常,
                //         46031 含 Combat:64 位宽,精确核对不越界) ----
                bool b46013NoThrow = true, b46031NoThrow = true;
                try { Feed("On46013", new CliVerify.Pkt().C(Shenxiao.Module.Core.Boss.BossModel.BossType.Eudaemon).I(4000003).C(2).Bytes()); }
                catch (System.Exception e) { b46013NoThrow = false; Debug.LogError("CLIVERIFY boss 46013 threw: " + e); }
                try
                {
                    Feed("On46031", new CliVerify.Pkt().I(4000004).L(9002).S("丁").C(1).H(120)
                        .L(999999999999L).S("").I(0).I(1700004000).H(1).H(3).Bytes());
                }
                catch (System.Exception e) { b46031NoThrow = false; Debug.LogError("CLIVERIFY boss 46031 threw: " + e); }
                bool b4601331 = b46013NoThrow && b46031NoThrow;
                Debug.Log("CLIVERIFY boss 46013/46031 防御recv noThrow=" + b4601331);

                // ---- U. 节日boss场景组 46026-46029/46033 + 46040(修复轮补注册防御recv,不抛异常即过;
                //         46027 含28字段怪物快照,46040 含 AutoId/Hp/HpMax 三个64位) ----
                bool bFeastGroupNoThrow = true;
                try
                {
                    Feed("On46026", new CliVerify.Pkt().H(1).I(500001).Bytes());
                    byte[] p46027 = new CliVerify.Pkt().I(4000005).I(1000).I(2000).H(1)
                        .H(100).H(200).I(1).I(600001).L(1000).L(1000).H(1).S("小怪").H(0).I(0).S("")
                        .I(0).I(0).C(0).C(0).C(0).C(0).C(0).I(0).C(0).C(0).C(0).C(0).H(0).L(0).H(0).C(0).I(0)
                        .Bytes();
                    Feed("On46027", p46027);
                    Feed("On46028", new CliVerify.Pkt().C(1).H(1).C(1).I(100000).I(1).Bytes());
                    Feed("On46029", new CliVerify.Pkt().Bytes());
                    Feed("On46033", new CliVerify.Pkt().I(2).I(1700005000).Bytes());
                    Feed("On46040", new CliVerify.Pkt().H(1).I(600001).L(1).L(8000).L(10000).Bytes());
                }
                catch (System.Exception e) { bFeastGroupNoThrow = false; Debug.LogError("CLIVERIFY boss feastGroup/46040 threw: " + e); }
                Debug.Log("CLIVERIFY boss 46026-29/33/46040 防御recv noThrow=" + bFeastGroupNoThrow);

                bool pass = configOk && deadOk && b46000 && b46009 && b46001 && b46002 && b4600304 && b46007
                    && b46016 && b46019 && b46022 && dkillNoThrow && infoNoThrow && b46034 && auxNoThrow
                    && b46041 && ackNoThrow && b46044 && b46045 && b20025 && b20026 && bWarFree
                    && b4601331 && bFeastGroupNoThrow;

                Debug.Log("CLIVERIFY boss VERDICT config=" + configOk + " dead=" + deadOk + " l46000=" + b46000
                    + " l46009=" + b46009 + " l46001=" + b46001 + " l46002=" + b46002 + " l4600304=" + b4600304
                    + " l46007=" + b46007 + " l46016=" + b46016 + " l46019_22=" + (b46019 && b46022)
                    + " l46024=" + dkillNoThrow + " l46025=" + infoNoThrow + " l46034=" + b46034
                    + " aux=" + auxNoThrow + " l46041=" + b46041 + " ack=" + ackNoThrow + " l46044=" + b46044
                    + " l46045=" + b46045 + " collect=" + (b20025 && b20026) + " warfree=" + bWarFree
                    + " l4601331=" + b4601331 + " feastGroup=" + bFeastGroupNoThrow + " pass=" + pass);

                model.Clear46000();
                Shenxiao.Module.Core.Relive.ReliveModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块(与 ChatCase/FriendMailCase/
        /// RankCase 的 AppendMinimalFigure 逐字节相同,独立文件各自持有一份,避免跨用例文件耦合)。
        /// 改 SCHEMA 顺序时所有副本必须同步。</summary>
        private static CliVerify.Pkt AppendMinimalFigure(this CliVerify.Pkt p, string name)
        {
            return p
                .S(name)  // name
                .C(0)     // sex
                .C(0)     // realm
                .C(0)     // career
                .H(0)     // level
                .C(0)     // GM
                .C(0)     // vip_flag
                .C(0)     // is_hide_vip
                .C(0)     // touxian
                .H(0)     // level_model_list count
                .H(0)     // fashion_model_list count
                .S("")    // picture
                .I(0)     // prcture_ver
                .L(0)     // guild_id
                .S("")    // guild_name
                .C(0)     // position
                .S("")    // position_name
                .I(0)     // dsgt_id
                .I(0)     // liveness_id
                .C(0)     // turn
                .C(0)     // turn_stage
                .C(0)     // grade_id
                .C(0)     // is_marriage
                .L(0)     // marriage_id
                .S("")    // marriage_name
                .I(0)     // escort_state
                .I(0)     // block_id
                .I(0)     // house_id
                .H(0)     // house_lv
                .H(0)     // figure_list count
                .H(0)     // figure_ride_list count
                .H(0)     // achv_lv
                .H(0)     // medal_id
                .I(0)     // fazhen_id
                .H(0)     // dress_list count
                .I(0)     // god_id
                .I(0)     // revelation_suit
                .I(0)     // demon_id
                .C(0)     // supreme_vip
                .I(0)     // title_id
                .C(0)     // mask_id
                .C(0)     // seaCamp
                .C(0)     // brick_id
                .C(0)     // dummy_type
                .C(0)     // suit_fashion_id
                .C(0);    // collect_state
        }
    }
}
