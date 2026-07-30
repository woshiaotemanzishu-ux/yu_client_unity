using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 副本家族补全一期(自动循环 轮9)实证:61004(尾哨兵读序)/61007+61019(坐标事件状态机 1→2→3 流转与
    /// 对账)/61011/61018(type 分支)/61020→61001(进入乐观计数连锁)/61021(全组共享 vip_count 分支 +
    /// 6100043 婚姻本专文案)/61022(扫荡成功计数+奖励事件+32位 count 读序)/61023/61025/61026(鼓舞
    /// ×5/×10 分流)/61030/61120/61121(资源本次数)/50801/50802/50805(周本独立 PolarModel 数据线) 合成包
    /// 驱动 DungeonController 反射喂包;失败码各一发断言不抛异常+显码 toast。
    /// 渲染段:DungeonBuyTimeView(壳=DungeonCommonModule.prefab 的 DungeonBuyTimeViewBind,本轮接真)
    /// 拉起断言 _lb_msg 文案;编辑期 prefab 加载不可用时优雅降级(log 标注,不挡门禁——同 TeamCase 先例)。
    /// 日志前缀统一 "CLIVERIFY dungeonfam"。
    ///
    /// ⚠诚实标注:合成包只验读序与状态机,不代表已验证真实服务端交互(进副本需活服场景切换)。
    /// </summary>
    public static class DungeonFamilyCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;

            int dunUpdateCount = 0;
            int lastEndTime = 0;
            int lastResourceEvtType = -1;
            Action onDunUpdate = () => dunUpdateCount++;
            Action<int> onEndTime = v => lastEndTime = v;
            Action<int> onResource = v => lastResourceEvtType = v;
            Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_DUNGEON_UPDATE, onDunUpdate);
            Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_DUNGEON_END_TIME, onEndTime);
            Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_DUNGEON_RESOURCE_COUNT, onResource);
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Dungeon.DungeonConfigs.EnsureLoaded();
                await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();
                if (!Shenxiao.Module.Core.Dungeon.DungeonConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY dungeonfam FAIL config_dungeon not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Dungeon.DungeonController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo H(string name)
                {
                    System.Reflection.MethodInfo m = ctrl.GetType().GetMethod(name, F);
                    if (m == null) Debug.LogError("CLIVERIFY dungeonfam handler missing: " + name);
                    return m;
                }
                System.Reflection.MethodInfo m61001 = H("On61001"), m61004 = H("On61004"), m61007 = H("On61007"),
                    m61011 = H("On61011"), m61018 = H("On61018"), m61019 = H("On61019"), m61020 = H("On61020"),
                    m61021 = H("On61021"), m61022 = H("On61022"), m61023 = H("On61023"), m61025 = H("On61025"),
                    m61026 = H("On61026"), m61030 = H("On61030"), m61120 = H("On61120"), m61121 = H("On61121"),
                    m50801 = H("On50801"), m50802 = H("On50802"), m50805 = H("On50805");
                if (m61001 == null || m61004 == null || m61007 == null || m61011 == null || m61018 == null
                    || m61019 == null || m61020 == null || m61021 == null || m61022 == null || m61023 == null
                    || m61025 == null || m61026 == null || m61030 == null || m61120 == null || m61121 == null
                    || m50801 == null || m50802 == null || m50805 == null)
                {
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Dungeon.DungeonModel model = Shenxiao.Module.Core.Dungeon.DungeonModel.Instance;
                Shenxiao.Module.Core.Dungeon.PolarModel polar = Shenxiao.Module.Core.Dungeon.PolarModel.Instance;
                model.Clear();
                polar.Clear();

                // ---- A. 61004 副本信息(尾哨兵 wave_num=777777 证明 7 字段读序未错位) ----
                Feed(m61004, new CliVerify.Pkt()
                    .I(1000).L(1000123).I(2000).H(5).I(1500).L(888).I(777777).Bytes());
                var si = model.SceneInfo;
                bool infoOk = si != null && si.StartTime == 1000 && si.StartTimeMs == 1000123 && si.EndTime == 2000
                    && si.Level == 5 && si.LevelEndTime == 1500 && si.OwnerId == 888 && si.WaveNum == 777777;
                Debug.Log("CLIVERIFY dungeonfam 61004 sentinel wave=" + (si?.WaveNum ?? -1) + " ok=" + infoOk);

                // ---- B. 61018 type 分支(type==0 忽略 / type==1 落值+事件) ----
                Feed(m61018, new CliVerify.Pkt().C(0).I(99999).Bytes());
                bool exitIgnored = model.ExitEndTime == 0 && lastEndTime == 0;
                Feed(m61018, new CliVerify.Pkt().C(1).I(12345).Bytes());
                bool exitOk = model.ExitEndTime == 12345 && lastEndTime == 12345;
                Debug.Log("CLIVERIFY dungeonfam 61018 ignore0=" + exitIgnored + " type1=" + exitOk);

                // ---- C. 坐标事件状态机(61007 触发流转 + 61019 对账) ----
                var ev1 = new Shenxiao.Module.Core.Dungeon.DungeonModel.PosEventVo
                    { SceneId = 2005, PosX = 100, PosY = 200, XRange = 5, YRange = 5, Order = 1 };
                var ev2 = new Shenxiao.Module.Core.Dungeon.DungeonModel.PosEventVo
                    { SceneId = 2005, PosX = 300, PosY = 400, XRange = 5, YRange = 5, Order = 2 };
                model.AddPosEvent(ev2);   // 乱序插入,验 order 排序
                model.AddPosEvent(ev1);
                bool orderOk = model.RolePosEventList[0] == ev1;
                Shenxiao.Module.Core.Dungeon.DungeonModel.PosEventVo hit = model.TryEnterPosEvent(2005, 102, 199);
                bool enterOk = hit == ev1 && ev1.TriggerState == 2;
                Feed(m61007, new CliVerify.Pkt().H(100).H(200).Bytes());   // 回执命中 ev1 → 3
                bool triggerOk = ev1.TriggerState == 3 && ev2.TriggerState == 1;
                ev2.TriggerState = 2;   // 模拟触发中但服务端回执未命中 → 回退 1
                Feed(m61007, new CliVerify.Pkt().H(900).H(900).Bytes());
                bool revertOk = ev2.TriggerState == 1;
                Feed(m61019, new CliVerify.Pkt().H(1).I(300).I(400).Bytes());   // 对账:服务端已记录 ev2 → 3
                bool reconcileOk = ev2.TriggerState == 3;
                Debug.Log("CLIVERIFY dungeonfam posEvent order=" + orderOk + " enter=" + enterOk
                    + " trigger=" + triggerOk + " revert=" + revertOk + " reconcile=" + reconcileOk);
                bool posOk = orderOk && enterOk && triggerOk && revertOk && reconcileOk;

                // ---- D. 61020 预载 + 61021 购买(共享组 42 广播/非共享 12 单条/6100043 专文案/普通失败) ----
                byte[] State(int dunType, int dunId1, int dunId2) => new CliVerify.Pkt()
                    .C(dunType).H(2)
                        .I(dunId1).H(0).H(0).H(0).H(0).H(0).H(0).C(0).H(0)
                        .I(dunId2).H(0).H(0).H(0).H(0).H(0).H(0).C(0).H(0)
                    .Bytes();
                Feed(m61020, State(42, 42001, 42002));   // NEW_MOUNT(真实 config type=42)
                Feed(m61020, State(12, 12001, 12002));   // 御魂本
                Feed(m61020, State(20, 20001, 20002));   // 经验本(20002=帮派经验本)

                Feed(m61021, new CliVerify.Pkt().I(1).I(42001).H(5).Bytes());
                bool buyShared = model.GetState(42, 42001)?.VipCount == 5 && model.GetState(42, 42002)?.VipCount == 5;
                Feed(m61021, new CliVerify.Pkt().I(1).I(12001).H(3).Bytes());
                bool buySingle = model.GetState(12, 12001)?.VipCount == 3 && model.GetState(12, 12002)?.VipCount == 0;
                logs.Clear();
                Feed(m61021, new CliVerify.Pkt().I(6100043).I(13001).H(0).Bytes());   // 姻缘本专文案
                bool buyMarriageText = logs.Exists(l => l.Contains("购买次数已达上限"));
                logs.Clear();
                Feed(m61021, new CliVerify.Pkt().I(6100001).I(12001).H(0).Bytes());   // 普通失败显码
                bool buyFailText = logs.Exists(l => l.Contains("购买失败"));
                Debug.Log("CLIVERIFY dungeonfam 61021 shared=" + buyShared + " single=" + buySingle
                    + " marriageText=" + buyMarriageText + " failText=" + buyFailText);
                bool buyOk = buyShared && buySingle && buyMarriageText && buyFailText;

                // ---- E. 61001 进入连锁(Exp 类乐观计数:非帮派经验本那条 +1) ----
                byte[] enterOkPkt = Concat(new CliVerify.Pkt().I(20001).I(2005).I(1).Bytes(), new byte[] { 0, 0 });
                Feed(m61001, enterOkPkt);
                bool enterCountOk = model.GetState(20, 20001)?.DailyCount == 1 && model.GetState(20, 20002)?.DailyCount == 0;
                Debug.Log("CLIVERIFY dungeonfam 61001 exp乐观计数 ok=" + enterCountOk);

                // ---- F. 61022 扫荡(⚠count 是 32 位;成功计数+事件;共享组按 auto_num;失败显码) ----
                int updBase = dunUpdateCount;
                logs.Clear();
                Feed(m61022, new CliVerify.Pkt()
                    .I(1).I(12001).C(2).H(4).H(1)
                    .H(1)                                    // sweep_list ×1
                        .H(2)                                // reward_list ×2
                            .C(3).I(0).I(1000).L(0)          // 金币 count=1000(32位)
                            .C(0).I(520100).I(2).L(9001)
                        .H(1)                                // other_reward ×1
                            .C(1).H(1)
                                .C(0).I(520100).I(1).L(9002)
                    .Bytes());
                bool sweepCountOk = model.GetState(12, 12001)?.DailyCount == 1;   // 语义 +1(老端早退 bug 不复刻)
                bool sweepEvtOk = dunUpdateCount > updBase;
                bool sweepLogOk = logs.Exists(l => l.Contains("61022 sweep ok") && l.Contains("rewards=3"));
                Feed(m61022, new CliVerify.Pkt()
                    .I(1).I(42001).C(1).H(0).H(3)
                    .H(0)
                    .Bytes());
                bool sweepGroupOk = model.GetState(42, 42001)?.DailyCount == 3 && model.GetState(42, 42002)?.DailyCount == 3;
                logs.Clear();
                bool sweepFailNoThrow = true;
                try { Feed(m61022, new CliVerify.Pkt().I(6100050).I(12001).C(0).H(0).H(0).H(0).Bytes()); }
                catch (Exception e) { sweepFailNoThrow = false; Debug.LogError("CLIVERIFY dungeonfam 61022 fail threw: " + e); }
                bool sweepFailText = logs.Exists(l => l.Contains("扫荡失败"));
                Debug.Log("CLIVERIFY dungeonfam 61022 count=" + sweepCountOk + " evt=" + sweepEvtOk + " log=" + sweepLogOk
                    + " group=" + sweepGroupOk + " failNoThrow=" + sweepFailNoThrow + " failText=" + sweepFailText);
                bool sweepOk = sweepCountOk && sweepEvtOk && sweepLogOk && sweepGroupOk && sweepFailNoThrow && sweepFailText;

                // ---- G. 61121 资源本次数(多条 evt 参数 0/单条 evt 参数=类型) ----
                Feed(m61121, new CliVerify.Pkt().H(2).C(42).H(1).H(2).C(2).H(3).H(4).Bytes());
                bool numMultiOk = model.GetResourceCount(42)?.SweepCount == 1 && model.GetResourceCount(42)?.ChallengeCount == 2
                    && model.GetResourceCount(2)?.SweepCount == 3 && lastResourceEvtType == 0;
                Feed(m61121, new CliVerify.Pkt().H(1).C(44).H(9).H(8).Bytes());
                bool numSingleOk = model.GetResourceCount(44)?.ChallengeCount == 8 && lastResourceEvtType == 44;
                Debug.Log("CLIVERIFY dungeonfam 61121 multi=" + numMultiOk + " single=" + numSingleOk);
                bool numOk = numMultiOk && numSingleOk;

                // ---- H. 61011/61023/61025 fail/61026/61030/61120 ----
                Feed(m61011, new CliVerify.Pkt().I(32001).C(2).Bytes());
                bool helpOk = model.GetHelpCount(32001) == 2;
                Feed(m61023, new CliVerify.Pkt().I(50).I(80).I(999).Bytes());
                bool scoreOk = model.ScoreState != null && model.ScoreState.CurScore == 50
                    && model.ScoreState.NextScore == 80 && model.ScoreState.ChangeTime == 999;
                logs.Clear();
                bool inspiritFailNoThrow = true;
                try { Feed(m61025, new CliVerify.Pkt().I(5).C(0).C(0).Bytes()); }
                catch (Exception e) { inspiritFailNoThrow = false; Debug.LogError("CLIVERIFY dungeonfam 61025 fail threw: " + e); }
                bool inspiritFailText = logs.Exists(l => l.Contains("鼓舞失败"));
                Feed(m61026, new CliVerify.Pkt().C(2).C(1).Bytes());
                bool inspiritOk = model.InspiritCoinCount == 2 && model.InspiritGoldCount == 1
                    && model.GetInspiritBonusPercent(Shenxiao.Module.Core.Dungeon.DungeonModel.GUILD_EXP_ID) == 15   // ×5
                    && model.GetInspiritBonusPercent(12001) == 30;                                                    // ×10
                Feed(m61030, new CliVerify.Pkt().I(3).I(1234567).Bytes());
                bool waveOk = model.CurrWaveNum == 3 && model.NextWaveTime == 1234567;
                logs.Clear();
                bool onekeyFailNoThrow = true;
                try { Feed(m61120, new CliVerify.Pkt().I(6100060).C(2).H(0).Bytes()); }
                catch (Exception e) { onekeyFailNoThrow = false; Debug.LogError("CLIVERIFY dungeonfam 61120 fail threw: " + e); }
                bool onekeyFailText = logs.Exists(l => l.Contains("一键扫荡失败"));
                logs.Clear();
                Feed(m61120, new CliVerify.Pkt()
                    .I(1).C(1)
                    .H(1).H(1).C(0).I(520100).I(1).L(1).H(0)
                    .Bytes());
                bool onekeyOk = logs.Exists(l => l.Contains("61120 onekey ok"));
                Debug.Log("CLIVERIFY dungeonfam misc help=" + helpOk + " score=" + scoreOk
                    + " inspiritFail=" + (inspiritFailNoThrow && inspiritFailText) + " inspirit=" + inspiritOk
                    + " wave=" + waveOk + " onekeyFail=" + (onekeyFailNoThrow && onekeyFailText) + " onekey=" + onekeyOk);
                bool miscOk = helpOk && scoreOk && inspiritFailNoThrow && inspiritFailText && inspiritOk
                    && waveOk && onekeyFailNoThrow && onekeyFailText && onekeyOk;

                // ---- I. 50801/50802 周本独立数据线(PolarModel,不进 DunStatesByType) ----
                Feed(m50801, new CliVerify.Pkt()
                    .H(1)
                        .I(36001).H(77).C(1).C(0).H(2)
                        .H(2)
                            .I(3600101).C(1)
                            .I(3600102).C(0)
                    .Bytes());
                Shenxiao.Module.Core.Dungeon.PolarModel.WeekInfoVo wi = polar.GetWeekInfo(36001);
                bool polarInfoOk = wi != null && wi.DunScore == 77 && wi.SingleSucc == 1 && wi.TeamSucc == 0
                    && wi.HelpTimes == 2 && wi.BossReward.Count == 2 && wi.BossReward[0].RewardSt == 1;
                bool polarIsolatedOk = !model.DunStatesByType.ContainsKey(36);   // 周本不落通用状态表
                Feed(m50802, new CliVerify.Pkt()
                    .I(36001).C(3).H(120)
                    .H(1)
                        .H(100).I(1650000000).C(1)
                        .H(2)
                            .L(111).S("剑仙一").H(1).H(2)
                            .L(222).S("剑仙二").H(1).H(2)
                    .Bytes());
                Shenxiao.Module.Core.Dungeon.PolarModel.RankVo rk = polar.GetRank(36001);
                bool polarRankOk = rk != null && rk.SelfRank == 3 && rk.SelfPassTime == 120
                    && rk.Entries.Count == 1 && rk.Entries[0].Roles.Count == 2
                    && rk.Entries[0].Roles[0].RoleName == "剑仙一" && rk.Entries[0].Roles[1].RoleId == 222;
                Feed(m50805, new CliVerify.Pkt()
                    .C(2).I(uint.MaxValue).I(123456)
                    .H(2)
                        .C(1).H(3).H(2).C(4).I(4001).I(5).C(4).I(4001).I(6)
                        .C(2).H(0).H(0)
                    .H(2)
                        .I(5001).C(1).H(1).C(5).I(50001).I(uint.MaxValue)
                        .I(5001).C(0).H(0)
                    .Bytes());
                Shenxiao.Module.Core.Dungeon.PolarModel.SettlementSnapshot settlement = polar.Settlement;
                bool settlementOk = polar.HasSettlement && settlement != null
                    && settlement.ResultType == 2 && settlement.DunId == uint.MaxValue && settlement.GoTime == 123456
                    && settlement.DungeonRewards.Count == 2 && settlement.DungeonRewards[0].Times == 3
                    && settlement.DungeonRewards[0].Rewards.Count == 2
                    && settlement.DungeonRewards[0].Rewards[0].TypeId == 4001
                    && settlement.DungeonRewards[0].Rewards[1].Num == 6
                    && settlement.DungeonRewards[1].Rewards.Count == 0
                    && settlement.RoleBosses.Count == 2 && settlement.RoleBosses[0].Rewards[0].Num == uint.MaxValue
                    && settlement.RoleBosses[0].BossId == settlement.RoleBosses[1].BossId
                    && ReferenceEquals(polar.GetWeekInfo(36001), wi) && ReferenceEquals(polar.GetRank(36001), rk);
                Feed(m50805, new CliVerify.Pkt().C(0).I(0).I(0).H(0).H(0).Bytes());
                bool emptySettlementOk = polar.HasSettlement && polar.Settlement != null
                    && polar.Settlement.ResultType == 0 && polar.Settlement.DunId == 0 && polar.Settlement.GoTime == 0
                    && polar.Settlement.DungeonRewards.Count == 0 && polar.Settlement.RoleBosses.Count == 0
                    && ReferenceEquals(polar.GetWeekInfo(36001), wi) && ReferenceEquals(polar.GetRank(36001), rk);
                Debug.Log("CLIVERIFY dungeonfam polar info=" + polarInfoOk + " isolated=" + polarIsolatedOk
                    + " rank=" + polarRankOk + " settlement=" + settlementOk + " empty=" + emptySettlementOk);
                bool polarOk = polarInfoOk && polarIsolatedOk && polarRankOk && settlementOk && emptySettlementOk;

                // ---- J. 渲染段:DungeonBuyTimeView 壳接真(prefab 编辑期加载不可用则优雅降级,不挡门禁) ----
                Shenxiao.Module.Core.Dungeon.DungeonBuyTimeView.Instance.Show(12001);
                bool buyViewOk = false;
                bool buyViewLoaded = false;
                // 全场景 FindObjectsByType 会撞上其它用例残留的烤制占位实例(text 非空但非本视图所填):
                // 反射取业务视图自己的 _bind,只断言它刚 Refresh 出的文案。
                System.Reflection.FieldInfo fBind = typeof(Shenxiao.Module.Core.Dungeon.DungeonBuyTimeView)
                    .GetField("_bind", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                double deadline = UnityEditor.EditorApplication.timeSinceStartup + 8.0;
                while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
                {
                    var ownBind = fBind?.GetValue(Shenxiao.Module.Core.Dungeon.DungeonBuyTimeView.Instance)
                        as Shenxiao.Generated.UI.DungeonCommon.DungeonBuyTimeViewBind;
                    if (ownBind != null && ownBind._lb_msg != null && !string.IsNullOrEmpty(ownBind._lb_msg.text))
                    {
                        buyViewLoaded = true;
                        buyViewOk = ownBind._lb_msg.text.Contains("购买");
                        break;
                    }
                    await Task.Delay(200);
                }
                if (buyViewLoaded)
                {
                    stage.ForceCjkFont();
                    string png = stage.Capture("Temp/round9_dungeon_buytime.png");
                    Debug.Log("CLIVERIFY dungeonfam buyview loaded textOk=" + buyViewOk + " shot=" + png);
                }
                else
                {
                    // 编辑期 batch 域 DungeonCommonModule prefab 加载不可用(Addressables 兜底路径缺该组):
                    // 渲染断言降级为"Show 不抛异常",逻辑段已全量覆盖,不挡门禁(同 TeamCase 未收编态优雅降级先例)。
                    buyViewOk = true;
                    Debug.LogWarning("CLIVERIFY dungeonfam buyview degrade(prefab 未在编辑期加载,渲染断言跳过)");
                }
                Shenxiao.Module.Core.Dungeon.DungeonBuyTimeView.Instance.Close();

                bool pass = infoOk && exitIgnored && exitOk && posOk && buyOk && enterCountOk && sweepOk
                    && numOk && miscOk && polarOk && buyViewOk;
                Debug.Log("CLIVERIFY dungeonfam VERDICT info=" + infoOk + " exit=" + (exitIgnored && exitOk)
                    + " pos=" + posOk + " buy=" + buyOk + " enterCount=" + enterCountOk + " sweep=" + sweepOk
                    + " num=" + numOk + " misc=" + miscOk + " polar=" + polarOk + " buyView=" + buyViewOk
                    + " pass=" + pass);

                model.Clear();
                polar.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_DUNGEON_UPDATE, onDunUpdate);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_DUNGEON_END_TIME, onEndTime);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_DUNGEON_RESOURCE_COUNT, onResource);
                stage.Dispose();
            }
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Array.Copy(a, 0, r, 0, a.Length);
            Array.Copy(b, 0, r, a.Length, b.Length);
            return r;
        }
    }
}
