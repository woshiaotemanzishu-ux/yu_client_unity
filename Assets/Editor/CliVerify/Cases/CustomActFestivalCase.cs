using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// P4 节日族(自动循环 轮17,spec §5)实证:摇钱树 MONEYTREE(50)/MOUNT_TURNTABLE(54)/MONEYTREE_SHOP(89)
    /// (33190/91/92/33168/33231)/FTVACTIVENESS(56)(33193/94/95/96)/SAIBOTREASURE(58,赛博夺宝)
    /// (33165/66/67)/绑钻转盘 TURNTABLE(28)(33130/31/32)/RED_PACKET_RAIN(82,红包雨)(33155/57/58)/
    /// HOLYCALL(67,神圣召唤)(33221/22)共 20 号,合成包驱动 CustomActivityController 反射喂包,断言
    /// CustomActivityModel.Festival 落地字段/事件(模板 CustomActCoreCase/MarriageCase,纯逻辑段)。
    ///
    /// 重点覆盖(本包嵌套最深,33190/33221/33165 是重点):①注册线核实(NetManager._handlers)——15b/16 血训;
    /// ②33190 三嵌套(ShowList/CumulateReward/Shop)+ ErrorCode 第3字段;③33191 ErrorCode 领先(订正侦察表
    /// "首字段=N"误记)+ Times **8位**;④33165 三层嵌套 Pool(**Reward 在最前**)+ StageS→GradeState(两个
    /// 奖励子数组),末条探针到第三层;⑤33221 四嵌套(ShowList/CumulateReward/RarePool)+ 尾字段 RareDrawTimes
    /// (不在任何数组内);⑥33130 NTimesList+RewardList 双平级数组(非嵌套,订正侦察表箭头写法误读);⑦33155/33157
    /// 无 BaseType 只有 SubType;⑧33158/33196 recv-only,断言无公开 Request 方法且已挂防御 recv;⑨33158 断言
    /// Emit 的是 EVT_CUSTOMACT_REDPACKET_WAVE(不是通用 DETAIL_UPDATE);⑩全部 11 个带 ErrorCode 的号成功/失败
    /// 两包,失败分支断言 Model 不被覆盖(仍是成功那次的值)+ ShowError 路径不抛异常。
    ///
    /// 事件粒度收敛断言:全局挂 3 个计数器验证 P1 通用事件按"有 ErrorCode→RESULT,无 ErrorCode→DETAIL_UPDATE,
    /// 33158 特例→REDPACKET_WAVE"三分路精确触发(不多不少),而不是逐号重复断言同一件事。
    /// </summary>
    public static class CustomActFestivalCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PF = BindingFlags.Public | BindingFlags.Instance;

        public static Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.Module.Core.CustomActivity.CustomActivityModel model = Shenxiao.Module.Core.CustomActivity.CustomActivityModel.Instance;
                model.Clear();
                model.ClearFestival();

                object ctrl = Shenxiao.Module.Core.CustomActivity.CustomActivityController.Instance;
                System.Type t = ctrl.GetType();
                bool anyThrew = false;
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY customact_festival handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY customact_festival " + method + " threw: " + e);
                    }
                }

                // ---- 0. 注册线核实(NetManager._handlers)——15b/16 血训 ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MONEYTREE_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MONEYTREE_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MONEYTREE_CUMULATE, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MONEYTREE_SHOP,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MONEYTREE_CURRENCY,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FTVACTIVE_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FTVACTIVE_SUBMIT,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FTVACTIVE_SERVER_CLAIM, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FTVACTIVE_TRIGGER_PUSH,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_SAIBO_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_SAIBO_STAGE,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_SAIBO_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BINDDIAMOND_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BINDDIAMOND_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BINDDIAMOND_RECORD,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REDRAIN_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REDRAIN_GRAB,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REDRAIN_WAVE_PUSH,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_HOLYCALL_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_HOLYCALL_RARE_DRAW,
                };
                bool bRegistered = handlers != null;
                var missingReg = new List<int>();
                if (handlers != null)
                {
                    foreach (int id in mustBeRegistered)
                    {
                        if (!handlers.Contains(id)) { bRegistered = false; missingReg.Add(id); }
                    }
                }
                Debug.Log("CLIVERIFY customact_festival 注册线核实(NetManager._handlers) missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                // ---- 事件粒度收敛:全局计数器(ErrorCode→RESULT / 无ErrorCode→DETAIL_UPDATE / 33158→REDPACKET_WAVE) ----
                int resultCount = 0, detailCount = 0, waveCount = 0;
                System.Action<int, int, int> onResult = (b, s, c) => resultCount++;
                System.Action<int, int> onDetail = (b, s) => detailCount++;
                System.Action<int, int, int> onWave = (sub, wave, st) => waveCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_REDPACKET_WAVE, onWave);

                // ==================== §1 摇钱树 MONEYTREE(50)/MOUNT_TURNTABLE(54)/MONEYTREE_SHOP(89) ====================
                const int MT_BASE = 250, MT_SUB = 1;

                // ---- A. 33190 界面(三嵌套 ShowList/CumulateReward/Shop;ErrorCode 第3字段) ----
                byte[] p33190ok = new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).I(1).H(10).H(3)
                    .H(1).H(1).C(0).H(1).C(1).I(9001).I(2)
                    .H(1).H(2).H(5).H(1).C(1).I(9002).I(1).C(1)
                    .I(888)
                    .H(1).H(3).H(1).C(1).I(9003).I(1).I(500).H(2).H(9).C(1)
                    .Bytes();
                Feed("On33190", p33190ok);
                var mtPanel = model.GetMoneyTreePanel(MT_BASE, MT_SUB);
                bool b33190ok = mtPanel != null && mtPanel.AllTimes == 10 && mtPanel.FreeTimes == 3
                    && mtPanel.ShowList.Count == 1 && mtPanel.ShowList[0].GradeId == 1 && mtPanel.ShowList[0].IsRare == 0
                    && mtPanel.ShowList[0].Reward.Count == 1 && mtPanel.ShowList[0].Reward[0].GoodsId == 9001 && mtPanel.ShowList[0].Reward[0].Num == 2
                    && mtPanel.CumulateReward.Count == 1 && mtPanel.CumulateReward[0].GradeId == 2 && mtPanel.CumulateReward[0].Times == 5
                    && mtPanel.CumulateReward[0].Status == 1 && mtPanel.CumulateReward[0].Reward[0].GoodsId == 9002
                    && mtPanel.Score == 888
                    && mtPanel.Shop.Count == 1 && mtPanel.Shop[0].GradeId == 3 && mtPanel.Shop[0].NeedScore == 500
                    && mtPanel.Shop[0].Num == 2 && mtPanel.Shop[0].MaxNum == 9 && mtPanel.Shop[0].ClearType == 1
                    && mtPanel.Shop[0].Reward[0].GoodsId == 9003;
                byte[] p33190fail = new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).I(1720101).H(0).H(0).H(0).H(0).I(0).H(0).Bytes();
                Feed("On33190", p33190fail);
                var mtPanelAfterFail = model.GetMoneyTreePanel(MT_BASE, MT_SUB);
                bool b33190fail = mtPanelAfterFail != null && mtPanelAfterFail.AllTimes == 10; // 未被失败包覆盖
                bool b33190 = b33190ok && b33190fail;
                Debug.Log("CLIVERIFY customact_festival 33190 摇钱树界面(三嵌套) showN=" + (mtPanel?.ShowList.Count ?? -1)
                    + " cumulateN=" + (mtPanel?.CumulateReward.Count ?? -1) + " shopN=" + (mtPanel?.Shop.Count ?? -1) + " ok=" + b33190);

                // ---- B. 33191 抽奖(ErrorCode领先,订正侦察表"首字段=N"误记;Times C2S 8位) ----
                byte[] p33191ok = new CliVerify.Pkt().I(1).H(MT_BASE).H(MT_SUB).H(11).H(2)
                    .H(1).H(4).C(1).H(1).C(1).I(9010).I(5)
                    .I(900)
                    .Bytes();
                Feed("On33191", p33191ok);
                var mtDraw = model.GetMoneyTreeDrawResult(MT_BASE, MT_SUB);
                bool b33191ok = mtDraw != null && mtDraw.AllTimes == 11 && mtDraw.FreeTimes == 2 && mtDraw.Score == 900
                    && mtDraw.RewardList.Count == 1 && mtDraw.RewardList[0].GradeId == 4 && mtDraw.RewardList[0].IsRare == 1
                    && mtDraw.RewardList[0].Reward[0].GoodsId == 9010 && mtDraw.RewardList[0].Reward[0].Num == 5;
                byte[] p33191fail = new CliVerify.Pkt().I(1720102).H(MT_BASE).H(MT_SUB).H(0).H(0).H(0).I(0).Bytes();
                Feed("On33191", p33191fail);
                var mtDrawAfterFail = model.GetMoneyTreeDrawResult(MT_BASE, MT_SUB);
                bool b33191fail = mtDrawAfterFail != null && mtDrawAfterFail.AllTimes == 11; // 未被失败包覆盖
                bool b33191 = b33191ok && b33191fail;
                Debug.Log("CLIVERIFY customact_festival 33191 摇钱树抽奖(ErrorCode领先) score=" + (mtDraw?.Score ?? -1) + " ok=" + b33191);

                // ---- C. 33192 累计奖励领取(2条,末条探针Reward非空验证游标未错位) ----
                byte[] p33192ok = new CliVerify.Pkt().I(1).H(MT_BASE).H(MT_SUB)
                    .H(2).H(1).H(1).H(0).C(1)
                    .H(2).H(2).H(1).C(1).I(9020).I(1).C(0)
                    .Bytes();
                Feed("On33192", p33192ok);
                var mtCumulate = model.GetMoneyTreeCumulateResult(MT_BASE, MT_SUB);
                bool b33192ok = mtCumulate != null && mtCumulate.CumulateReward.Count == 2
                    && mtCumulate.CumulateReward[0].GradeId == 1 && mtCumulate.CumulateReward[0].Status == 1 && mtCumulate.CumulateReward[0].Reward.Count == 0
                    && mtCumulate.CumulateReward[1].GradeId == 2 && mtCumulate.CumulateReward[1].Reward.Count == 1
                    && mtCumulate.CumulateReward[1].Reward[0].GoodsId == 9020; // 末条探针
                byte[] p33192fail = new CliVerify.Pkt().I(1720103).H(MT_BASE).H(MT_SUB).H(0).Bytes();
                Feed("On33192", p33192fail);
                var mtCumulateAfterFail = model.GetMoneyTreeCumulateResult(MT_BASE, MT_SUB);
                bool b33192fail = mtCumulateAfterFail != null && mtCumulateAfterFail.CumulateReward.Count == 2;
                bool b33192 = b33192ok && b33192fail;
                Debug.Log("CLIVERIFY customact_festival 33192 摇钱树累计领取 cumulateN=" + (mtCumulate?.CumulateReward.Count ?? -1) + " ok=" + b33192);

                // ---- D. 33168 树商店兑换(ErrorCode第4字段) ----
                byte[] p33168ok = new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).H(3).I(1)
                    .H(1).C(1).I(9030).I(2)
                    .H(5).I(400)
                    .Bytes();
                Feed("On33168", p33168ok);
                var mtShop = model.GetMoneyTreeShopResult(MT_BASE, MT_SUB);
                bool b33168ok = mtShop != null && mtShop.GradeId == 3 && mtShop.Num == 5 && mtShop.Score == 400
                    && mtShop.Reward.Count == 1 && mtShop.Reward[0].GoodsId == 9030 && mtShop.Reward[0].Num == 2;
                byte[] p33168fail = new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).H(3).I(1720104).H(0).H(0).I(0).Bytes();
                Feed("On33168", p33168fail);
                var mtShopAfterFail = model.GetMoneyTreeShopResult(MT_BASE, MT_SUB);
                bool b33168fail = mtShopAfterFail != null && mtShopAfterFail.Num == 5;
                bool b33168 = b33168ok && b33168fail;
                Debug.Log("CLIVERIFY customact_festival 33168 树商店兑换(第4字段ErrorCode) num=" + (mtShop?.Num ?? -1) + " ok=" + b33168);

                // ---- E. 33231 货币展示(纯推送无ErrorCode) ----
                byte[] p33231 = new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).I(12345).Bytes();
                Feed("On33231", p33231);
                var mtCurrency = model.GetMoneyTreeCurrency(MT_BASE, MT_SUB);
                bool b33231 = mtCurrency != null && mtCurrency.Currency == 12345;
                Debug.Log("CLIVERIFY customact_festival 33231 摇钱树货币(纯推送) currency=" + (mtCurrency?.Currency ?? -1) + " ok=" + b33231);

                // ==================== §2 FTVACTIVENESS(56,节日活跃) ====================
                const int FA_BASE = 56, FA_SUB = 1;

                // ---- F. 33193 界面(无ErrorCode;SerRewardList含 Param 字符串嵌套) ----
                byte[] p33193 = new CliVerify.Pkt().H(FA_BASE).H(FA_SUB).H(7).H(20)
                    .H(1).H(1).C(2).S("参数A").H(3).H(1).C(1).I(9040).I(1).C(1)
                    .Bytes();
                Feed("On33193", p33193);
                var faPanel = model.GetFtvActivePanel(FA_BASE, FA_SUB);
                bool b33193 = faPanel != null && faPanel.PersonTimes == 7 && faPanel.ServerTimes == 20
                    && faPanel.SerRewardList.Count == 1 && faPanel.SerRewardList[0].GradeId == 1 && faPanel.SerRewardList[0].TriggerType == 2
                    && faPanel.SerRewardList[0].Param == "参数A" && faPanel.SerRewardList[0].Times == 3
                    && faPanel.SerRewardList[0].Reward.Count == 1 && faPanel.SerRewardList[0].Reward[0].GoodsId == 9040 && faPanel.SerRewardList[0].Status == 1;
                Debug.Log("CLIVERIFY customact_festival 33193 节日活跃界面(Param字符串嵌套) param=" + faPanel?.SerRewardList[0].Param + " ok=" + b33193);

                // ---- G. 33194 提交(ErrorCode领先) ----
                byte[] p33194ok = new CliVerify.Pkt().I(1).H(FA_BASE).H(FA_SUB).C(2)
                    .H(1).H(5).C(0).H(1).C(1).I(9050).I(1)
                    .H(8)
                    .Bytes();
                Feed("On33194", p33194ok);
                var faSubmit = model.GetFtvActiveSubmitResult(FA_BASE, FA_SUB);
                bool b33194ok = faSubmit != null && faSubmit.CostType == 2 && faSubmit.PersonTimes == 8
                    && faSubmit.RewardList.Count == 1 && faSubmit.RewardList[0].GradeId == 5 && faSubmit.RewardList[0].Reward[0].GoodsId == 9050;
                byte[] p33194fail = new CliVerify.Pkt().I(1720105).H(FA_BASE).H(FA_SUB).C(2).H(0).H(0).Bytes();
                Feed("On33194", p33194fail);
                var faSubmitAfterFail = model.GetFtvActiveSubmitResult(FA_BASE, FA_SUB);
                bool b33194fail = faSubmitAfterFail != null && faSubmitAfterFail.PersonTimes == 8;
                bool b33194 = b33194ok && b33194fail;
                Debug.Log("CLIVERIFY customact_festival 33194 节日活跃提交 personTimes=" + (faSubmit?.PersonTimes ?? -1) + " ok=" + b33194);

                // ---- H. 33195 领取全服奖励(ErrorCode领先) ----
                byte[] p33195ok = new CliVerify.Pkt().I(1).H(FA_BASE).H(FA_SUB).H(9).H(1).C(1).I(9060).I(3).Bytes();
                Feed("On33195", p33195ok);
                var faClaim = model.GetFtvActiveServerClaimResult(FA_BASE, FA_SUB);
                bool b33195ok = faClaim != null && faClaim.GradeId == 9 && faClaim.Reward.Count == 1
                    && faClaim.Reward[0].GoodsId == 9060 && faClaim.Reward[0].Num == 3;
                byte[] p33195fail = new CliVerify.Pkt().I(1720106).H(FA_BASE).H(FA_SUB).H(9).H(0).Bytes();
                Feed("On33195", p33195fail);
                var faClaimAfterFail = model.GetFtvActiveServerClaimResult(FA_BASE, FA_SUB);
                bool b33195fail = faClaimAfterFail != null && faClaimAfterFail.Reward.Count == 1;
                bool b33195 = b33195ok && b33195fail;
                Debug.Log("CLIVERIFY customact_festival 33195 节日活跃全服奖励 grade=" + (faClaim?.GradeId ?? -1) + " ok=" + b33195);

                // ---- I. 33196 recv-only 触发广播(无ErrorCode) ----
                byte[] p33196 = new CliVerify.Pkt().H(FA_BASE).H(FA_SUB).H(15).C(1).H(2).C(3).C(4).Bytes();
                Feed("On33196", p33196);
                var faPush = model.GetFtvActiveTriggerPush(FA_BASE, FA_SUB);
                bool b33196 = faPush != null && faPush.ServerTimes == 15 && faPush.IsAsk == 1
                    && faPush.TriggerTypeList.Count == 2 && faPush.TriggerTypeList[0] == 3 && faPush.TriggerTypeList[1] == 4; // 末条探针
                Debug.Log("CLIVERIFY customact_festival 33196 节日活跃触发广播(recv-only) triggerN=" + (faPush?.TriggerTypeList.Count ?? -1) + " ok=" + b33196);

                // ==================== §3 SAIBOTREASURE(58,赛博夺宝)—— 本包嵌套最深 ====================
                const int SB_BASE = 58, SB_SUB = 1;

                // ---- J. 33165 界面(Pool 元素 Reward 在最前;StageS→GradeState 三层嵌套两个奖励子数组) ----
                byte[] p33165 = new CliVerify.Pkt().H(SB_BASE).H(SB_SUB).C(3).H(50).H(7)
                    .H(1) // Pool count
                        .H(1).C(1).I(9070).I(1) // Reward(最前)
                        .H(11).C(1).C(2).C(0)   // GradeId,IsRare,Sort,State
                    .H(1) // StageS count
                        .C(1) // Stage
                        .H(2) // GradeState count
                            .C(1).H(1).C(1).I(9080).I(1).H(0).C(0).C(5)
                            .C(2).H(0).H(1).C(1).I(9081).I(2).C(1).C(10)
                    .Bytes();
                Feed("On33165", p33165);
                var sbPanel = model.GetSaiboPanel(SB_BASE, SB_SUB);
                bool b33165 = sbPanel != null && sbPanel.Wave == 3 && sbPanel.AllTimes == 50 && sbPanel.TodayDrawtimes == 7
                    && sbPanel.Pool.Count == 1 && sbPanel.Pool[0].GradeId == 11 && sbPanel.Pool[0].IsRare == 1
                    && sbPanel.Pool[0].Sort == 2 && sbPanel.Pool[0].State == 0
                    && sbPanel.Pool[0].Reward.Count == 1 && sbPanel.Pool[0].Reward[0].GoodsId == 9070 // Reward在最前的探针
                    && sbPanel.StageS.Count == 1 && sbPanel.StageS[0].Stage == 1 && sbPanel.StageS[0].GradeState.Count == 2
                    && sbPanel.StageS[0].GradeState[0].GradeStage == 1 && sbPanel.StageS[0].GradeState[0].GradeReward.Count == 1
                    && sbPanel.StageS[0].GradeState[0].GradeReward[0].GoodsId == 9080 && sbPanel.StageS[0].GradeState[0].BuyReward.Count == 0
                    && sbPanel.StageS[0].GradeState[0].StateStage == 0 && sbPanel.StageS[0].GradeState[0].DiscountState == 5
                    && sbPanel.StageS[0].GradeState[1].GradeStage == 2 && sbPanel.StageS[0].GradeState[1].GradeReward.Count == 0
                    && sbPanel.StageS[0].GradeState[1].BuyReward.Count == 1 && sbPanel.StageS[0].GradeState[1].BuyReward[0].GoodsId == 9081 // 三层末条探针
                    && sbPanel.StageS[0].GradeState[1].StateStage == 1 && sbPanel.StageS[0].GradeState[1].DiscountState == 10;
                Debug.Log("CLIVERIFY customact_festival 33165 赛博夺宝界面(Reward最前+三层嵌套) poolN=" + (sbPanel?.Pool.Count ?? -1)
                    + " stageN=" + (sbPanel?.StageS.Count ?? -1) + " ok=" + b33165);

                // ---- K. 33166 阶段奖励(ErrorCode开头,Buy尾字段) ----
                byte[] p33166ok = new CliVerify.Pkt().I(1).H(SB_BASE).H(SB_SUB).C(1).C(2).H(1).C(1).I(9090).I(1).C(1).Bytes();
                Feed("On33166", p33166ok);
                var sbStage = model.GetSaiboStageResult(SB_BASE, SB_SUB);
                bool b33166ok = sbStage != null && sbStage.Stage == 1 && sbStage.GradeStage == 2 && sbStage.Buy == 1
                    && sbStage.Reward.Count == 1 && sbStage.Reward[0].GoodsId == 9090;
                byte[] p33166fail = new CliVerify.Pkt().I(1720107).H(SB_BASE).H(SB_SUB).C(1).C(2).H(0).C(0).Bytes();
                Feed("On33166", p33166fail);
                var sbStageAfterFail = model.GetSaiboStageResult(SB_BASE, SB_SUB);
                bool b33166fail = sbStageAfterFail != null && sbStageAfterFail.Buy == 1;
                bool b33166 = b33166ok && b33166fail;
                Debug.Log("CLIVERIFY customact_festival 33166 赛博夺宝阶段奖励(Buy尾字段) buy=" + (sbStage?.Buy ?? -1) + " ok=" + b33166);

                // ---- L. 33167 抽奖(ErrorCode开头,RewardList元素比侦察表多Sort尾字段) ----
                byte[] p33167ok = new CliVerify.Pkt().I(1).H(SB_BASE).H(SB_SUB).H(51).H(8)
                    .H(1).H(6).C(1).H(1).C(1).I(9100).I(1).C(9)
                    .Bytes();
                Feed("On33167", p33167ok);
                var sbDraw = model.GetSaiboDrawResult(SB_BASE, SB_SUB);
                bool b33167ok = sbDraw != null && sbDraw.AllTimes == 51 && sbDraw.TodayDrawtimes == 8
                    && sbDraw.RewardList.Count == 1 && sbDraw.RewardList[0].GradeId == 6 && sbDraw.RewardList[0].IsRare == 1
                    && sbDraw.RewardList[0].Sort == 9 // 侦察表未记的尾字段探针
                    && sbDraw.RewardList[0].Reward[0].GoodsId == 9100;
                byte[] p33167fail = new CliVerify.Pkt().I(1720108).H(SB_BASE).H(SB_SUB).H(0).H(0).H(0).Bytes();
                Feed("On33167", p33167fail);
                var sbDrawAfterFail = model.GetSaiboDrawResult(SB_BASE, SB_SUB);
                bool b33167fail = sbDrawAfterFail != null && sbDrawAfterFail.AllTimes == 51;
                bool b33167 = b33167ok && b33167fail;
                Debug.Log("CLIVERIFY customact_festival 33167 赛博夺宝抽奖(Sort尾字段) sort=" + (sbDraw?.RewardList[0].Sort ?? -1) + " ok=" + b33167);

                // ==================== §4 绑钻转盘 TURNTABLE(28)—— 三号均无ErrorCode ====================
                const int BD_BASE = 28, BD_SUB = 1;

                // ---- M. 33130 界面(NTimesList+RewardList 双平级数组,非嵌套) ----
                byte[] p33130 = new CliVerify.Pkt().H(BD_BASE).H(BD_SUB).I(100).I(500).I(400).I(9999).H(50)
                    .H(2).C(1).C(10)
                    .H(2).I(9110).I(1).I(9111).I(2)
                    .Bytes();
                Feed("On33130", p33130);
                var bdPanel = model.GetBindDiamondPanel(BD_BASE, BD_SUB);
                bool b33130 = bdPanel != null && bdPanel.TicketNum == 100 && bdPanel.TotalTickets == 500 && bdPanel.TotalLeftTickets == 400
                    && bdPanel.ChargeGold == 9999 && bdPanel.NeedGold == 50
                    && bdPanel.NTimesList.Count == 2 && bdPanel.NTimesList[0] == 1 && bdPanel.NTimesList[1] == 10
                    && bdPanel.RewardList.Count == 2 && bdPanel.RewardList[1].GoodsId == 9111 && bdPanel.RewardList[1].GoodsNum == 2; // 末条探针
                Debug.Log("CLIVERIFY customact_festival 33130 绑钻转盘界面(双平级数组) nTimesN=" + (bdPanel?.NTimesList.Count ?? -1)
                    + " rewardN=" + (bdPanel?.RewardList.Count ?? -1) + " ok=" + b33130);

                // ---- N. 33131 抽奖结果(无ErrorCode,C2S仅BaseType,SubType两字段) ----
                byte[] p33131 = new CliVerify.Pkt().H(BD_BASE).H(BD_SUB).I(9120).I(3).C(5).I(99).I(395).Bytes();
                Feed("On33131", p33131);
                var bdDraw = model.GetBindDiamondDrawResult(BD_BASE, BD_SUB);
                bool b33131 = bdDraw != null && bdDraw.GoodsId == 9120 && bdDraw.GoodsNum == 3 && bdDraw.NTimes == 5
                    && bdDraw.TicketNum == 99 && bdDraw.TotalLeftTickets == 395;
                Debug.Log("CLIVERIFY customact_festival 33131 绑钻转盘抽奖 goods=" + (bdDraw?.GoodsId ?? -1) + " ok=" + b33131);

                // ---- O. 33132 记录(List元素RoleId是32位) ----
                byte[] p33132 = new CliVerify.Pkt().H(BD_BASE).H(BD_SUB)
                    .H(2)
                        .I(8001).S("甲").I(9130).I(1).C(2)
                        .I(8002).S("乙").I(9131).I(2).C(3)
                    .Bytes();
                Feed("On33132", p33132);
                var bdRecord = model.GetBindDiamondRecord(BD_BASE, BD_SUB);
                bool b33132 = bdRecord != null && bdRecord.List.Count == 2
                    && bdRecord.List[0].RoleId == 8001 && bdRecord.List[0].RoleName == "甲"
                    && bdRecord.List[1].RoleId == 8002 && bdRecord.List[1].GoodsId == 9131 && bdRecord.List[1].NTimes == 3; // 末条探针
                Debug.Log("CLIVERIFY customact_festival 33132 绑钻转盘记录(RoleId 32位) listN=" + (bdRecord?.List.Count ?? -1) + " ok=" + b33132);

                // ==================== §5 RED_PACKET_RAIN(82,红包雨)—— 无BaseType只有SubType ====================
                const int RR_SUB = 14; // 老端图标key "331@14" 硬编码值

                // ---- P. 33155 界面(WaveReceive嵌套,无BaseType) ----
                byte[] p33155 = new CliVerify.Pkt().H(RR_SUB).I(500).C(2).I(1700100000).C(1)
                    .H(2)
                        .C(1).C(1).H(1).C(1).I(9140).I(1)
                        .C(2).C(0).H(0)
                    .Bytes();
                Feed("On33155", p33155);
                var rrPanel = model.GetRedRainPanel(RR_SUB);
                bool b33155 = rrPanel != null && rrPanel.ActValue == 500 && rrPanel.Wave == 2 && rrPanel.StartTime == 1700100000 && rrPanel.ClearType == 1
                    && rrPanel.WaveReceive.Count == 2 && rrPanel.WaveReceive[0].Wave2 == 1 && rrPanel.WaveReceive[0].IsReceive == 1
                    && rrPanel.WaveReceive[0].Rewards.Count == 1 && rrPanel.WaveReceive[0].Rewards[0].GoodsId == 9140
                    && rrPanel.WaveReceive[1].Wave2 == 2 && rrPanel.WaveReceive[1].IsReceive == 0 && rrPanel.WaveReceive[1].Rewards.Count == 0; // 末条探针
                Debug.Log("CLIVERIFY customact_festival 33155 红包雨界面(无BaseType) waveReceiveN=" + (rrPanel?.WaveReceive.Count ?? -1) + " ok=" + b33155);

                // ---- Q. 33157 抢红包(Errcode领先,无BaseType) ----
                byte[] p33157ok = new CliVerify.Pkt().I(1).H(RR_SUB).C(1).H(1).C(1).I(9150).I(1).Bytes();
                Feed("On33157", p33157ok);
                var rrGrab = model.GetRedRainGrabResult(RR_SUB);
                bool b33157ok = rrGrab != null && rrGrab.Wave == 1 && rrGrab.Rewards.Count == 1 && rrGrab.Rewards[0].GoodsId == 9150;
                byte[] p33157fail = new CliVerify.Pkt().I(1720109).H(RR_SUB).C(1).H(0).Bytes();
                Feed("On33157", p33157fail);
                var rrGrabAfterFail = model.GetRedRainGrabResult(RR_SUB);
                bool b33157fail = rrGrabAfterFail != null && rrGrabAfterFail.Rewards.Count == 1;
                bool b33157 = b33157ok && b33157fail;
                Debug.Log("CLIVERIFY customact_festival 33157 抢红包(无BaseType) rewardGoods=" + (rrGrab?.Rewards[0].GoodsId ?? -1) + " ok=" + b33157);

                // ---- R. 33158 recv-only 新波次推送(断言 Emit 的是 EVT_CUSTOMACT_REDPACKET_WAVE 而非通用 DETAIL_UPDATE) ----
                (int sub, int wave, int st) lastWave = (0, 0, 0);
                System.Action<int, int, int> onWaveCapture = (sub, wave, startTime) => lastWave = (sub, wave, startTime);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_REDPACKET_WAVE, onWaveCapture);
                byte[] p33158 = new CliVerify.Pkt().H(RR_SUB).C(3).I(1700200000).Bytes();
                Feed("On33158", p33158);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_REDPACKET_WAVE, onWaveCapture);
                var rrWavePush = model.GetRedRainWavePush(RR_SUB);
                bool b33158 = rrWavePush != null && rrWavePush.Wave == 3 && rrWavePush.StartTime == 1700200000
                    && lastWave.sub == RR_SUB && lastWave.wave == 3 && lastWave.st == 1700200000;
                Debug.Log("CLIVERIFY customact_festival 33158 红包雨新波次(recv-only,Emit REDPACKET_WAVE) wave=" + (rrWavePush?.Wave ?? -1) + " ok=" + b33158);

                // 追发镜像(ts:2246-2248,自动循环 轮17三镜头验收补):wave==1 时应追发 33155 重拉界面。
                // 该分支只发协议不改 Model,用"无异常"作为可观测信号,不 mock 网络。
                Feed("On33158", new CliVerify.Pkt().H(RR_SUB).C(1).I(1700200100).Bytes());
                bool b33158wave1 = !anyThrew && model.GetRedRainWavePush(RR_SUB)?.Wave == 1;
                b33158 = b33158 && b33158wave1;
                Debug.Log("CLIVERIFY customact_festival 33158 wave==1追发33155(镜像ts:2247) noThrow=" + b33158wave1 + " ok=" + b33158);

                // ==================== §6 HOLYCALL(67,神圣召唤) ====================
                const int HC_BASE = 67, HC_SUB = 1;

                // ---- S. 33221 信息(四嵌套 ShowList/CumulateReward/RarePool + 尾字段RareDrawTimes,ErrorCode第3字段) ----
                byte[] p33221ok = new CliVerify.Pkt().H(HC_BASE).H(HC_SUB).I(1).H(20).H(5)
                    .H(1).H(1).C(0).H(1).C(1).I(9160).I(1)
                    .H(1).H(2).H(3).H(1).C(1).I(9161).I(1).C(1)
                    .H(1).H(3).C(1).H(1).C(1).I(9162).I(1)
                    .H(7)
                    .Bytes();
                Feed("On33221", p33221ok);
                var hcPanel = model.GetHolyCallPanel(HC_BASE, HC_SUB);
                bool b33221ok = hcPanel != null && hcPanel.AllTimes == 20 && hcPanel.FreeTimes == 5
                    && hcPanel.ShowList.Count == 1 && hcPanel.ShowList[0].GradeId == 1 && hcPanel.ShowList[0].Reward[0].GoodsId == 9160
                    && hcPanel.CumulateReward.Count == 1 && hcPanel.CumulateReward[0].GradeId == 2 && hcPanel.CumulateReward[0].Times == 3
                    && hcPanel.CumulateReward[0].Status == 1 && hcPanel.CumulateReward[0].Reward[0].GoodsId == 9161
                    && hcPanel.RarePool.Count == 1 && hcPanel.RarePool[0].GradeId == 3 && hcPanel.RarePool[0].IsRare == 1
                    && hcPanel.RarePool[0].Reward[0].GoodsId == 9162
                    && hcPanel.RareDrawTimes == 7; // 尾字段(不在任何数组内)探针
                byte[] p33221fail = new CliVerify.Pkt().H(HC_BASE).H(HC_SUB).I(1720110).H(0).H(0).H(0).H(0).H(0).H(0).Bytes();
                Feed("On33221", p33221fail);
                var hcPanelAfterFail = model.GetHolyCallPanel(HC_BASE, HC_SUB);
                bool b33221fail = hcPanelAfterFail != null && hcPanelAfterFail.RareDrawTimes == 7;
                bool b33221 = b33221ok && b33221fail;
                Debug.Log("CLIVERIFY customact_festival 33221 神圣召唤信息(四嵌套+尾字段) rareDrawTimes=" + (hcPanel?.RareDrawTimes ?? -1) + " ok=" + b33221);

                // ---- T. 33222 稀有抽(ErrorCode领先) ----
                byte[] p33222ok = new CliVerify.Pkt().I(1).H(HC_BASE).H(HC_SUB).H(8).H(1).H(4).C(1).H(1).C(1).I(9170).I(1).Bytes();
                Feed("On33222", p33222ok);
                var hcRareDraw = model.GetHolyCallRareDrawResult(HC_BASE, HC_SUB);
                bool b33222ok = hcRareDraw != null && hcRareDraw.RareDrawTimes == 8
                    && hcRareDraw.RewardList.Count == 1 && hcRareDraw.RewardList[0].GradeId == 4 && hcRareDraw.RewardList[0].Reward[0].GoodsId == 9170;
                byte[] p33222fail = new CliVerify.Pkt().I(1720111).H(HC_BASE).H(HC_SUB).H(0).H(0).Bytes();
                Feed("On33222", p33222fail);
                var hcRareDrawAfterFail = model.GetHolyCallRareDrawResult(HC_BASE, HC_SUB);
                bool b33222fail = hcRareDrawAfterFail != null && hcRareDrawAfterFail.RareDrawTimes == 8;
                bool b33222 = b33222ok && b33222fail;
                Debug.Log("CLIVERIFY customact_festival 33222 神圣召唤稀有抽 rareDrawTimes=" + (hcRareDraw?.RareDrawTimes ?? -1) + " ok=" + b33222);

                // ---- U. 事件粒度收敛:总计数核对(11个ErrorCode号×2包=22 RESULT;8个纯推送号×1包=8 DETAIL_UPDATE;
                // 33158 本段喂了 2 次[wave=3 主断言 + wave=1 追发镜像补测]=2 WAVE) ----
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_REDPACKET_WAVE, onWave);
                bool bEventCounts = resultCount == 22 && detailCount == 8 && waveCount == 2;
                Debug.Log("CLIVERIFY customact_festival 事件粒度收敛 result=" + resultCount + "(期望22) detail=" + detailCount
                    + "(期望8) wave=" + waveCount + "(期望2) ok=" + bEventCounts);

                // ---- V. 死号防御:33196/33158 recv-only,断言无公开 Request 方法但已挂防御 recv ----
                bool dead33196 = t.GetMethod("RequestFtvActiveTriggerPush", PF) == null && t.GetMethod("On33196", F) != null;
                bool dead33158 = t.GetMethod("RequestRedRainWavePush", PF) == null && t.GetMethod("On33158", F) != null;
                Debug.Log("CLIVERIFY customact_festival recv-only防御 33196noSend=" + dead33196 + " 33158noSend=" + dead33158);

                // ---- V2. 33196 追发镜像(ts:1657-1676,自动循环 轮17三镜头验收补):is_ask!=0 且 GetDetail(base,sub)
                // 命中时才追发 33193。先喂一个 33104(Core.cs 通用详情,反射跨文件仍在同一 partial class 内可查到)
                // 让 GetDetail(FA_BASE,FA_SUB) 命中,再喂 is_ask!=0 的 33196,断言无异常(不 mock 网络)。放在
                // U 段事件计数之后,避免这里额外触发的 DETAIL_UPDATE 打乱上面 detailCount==8 的精确计数。 ----
                Feed("On33104", new CliVerify.Pkt().H(FA_BASE).H(FA_SUB).H(0).Bytes()); // 空 RewardList,只为让 GetDetail 命中
                bool b33196FollowUp = model.GetDetail(FA_BASE, FA_SUB) != null;
                byte[] p33196AskNonZero = new CliVerify.Pkt().H(FA_BASE).H(FA_SUB).H(16).C(1).H(0).Bytes(); // is_ask=1(truthy)
                Feed("On33196", p33196AskNonZero);
                b33196FollowUp = b33196FollowUp && !anyThrew;
                Debug.Log("CLIVERIFY customact_festival 33196 is_ask+GetDetail命中追发33193(镜像ts:1671-1674) noThrow=" + !anyThrew + " ok=" + b33196FollowUp);

                // ---- V3. 静默阈值镜像(自动循环 轮17三镜头验收补):33190/33221 失败 code==1012 或 3310043 不弹错,
                // 33191/33192/33168/33222 失败 code==1012 不弹错。放在 U 段事件计数之后,避免影响 resultCount==22
                // 的精确断言;只需证明这些码值走进"静默不弹"分支时不抛异常且不覆盖 Model(仿上方各号已有的
                // failNotOverwrite 断言写法)。 ----
                bool bSilentThresholds = true;
                Feed("On33190", new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).I(1012).H(0).H(0).H(0).H(0).I(0).H(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetMoneyTreePanel(MT_BASE, MT_SUB)?.AllTimes == 10; // 未被覆盖
                Feed("On33190", new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).I(3310043).H(0).H(0).H(0).H(0).I(0).H(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetMoneyTreePanel(MT_BASE, MT_SUB)?.AllTimes == 10;
                Feed("On33191", new CliVerify.Pkt().I(1012).H(MT_BASE).H(MT_SUB).H(0).H(0).H(0).I(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetMoneyTreeDrawResult(MT_BASE, MT_SUB)?.AllTimes == 11;
                Feed("On33192", new CliVerify.Pkt().I(1012).H(MT_BASE).H(MT_SUB).H(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetMoneyTreeCumulateResult(MT_BASE, MT_SUB)?.CumulateReward.Count == 2;
                Feed("On33168", new CliVerify.Pkt().H(MT_BASE).H(MT_SUB).H(3).I(1012).H(0).H(0).I(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetMoneyTreeShopResult(MT_BASE, MT_SUB)?.Num == 5;
                Feed("On33221", new CliVerify.Pkt().H(HC_BASE).H(HC_SUB).I(1012).H(0).H(0).H(0).H(0).H(0).H(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetHolyCallPanel(HC_BASE, HC_SUB)?.RareDrawTimes == 7;
                Feed("On33221", new CliVerify.Pkt().H(HC_BASE).H(HC_SUB).I(3310043).H(0).H(0).H(0).H(0).H(0).H(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetHolyCallPanel(HC_BASE, HC_SUB)?.RareDrawTimes == 7;
                Feed("On33222", new CliVerify.Pkt().I(1012).H(HC_BASE).H(HC_SUB).H(0).H(0).Bytes());
                bSilentThresholds &= !anyThrew && model.GetHolyCallRareDrawResult(HC_BASE, HC_SUB)?.RareDrawTimes == 8;
                Debug.Log("CLIVERIFY customact_festival 静默阈值(33190/33191/33192/33168/33221/33222,1012[+33190/33221额外3310043]不弹) noThrow+notOverwrite ok=" + bSilentThresholds);

                // ---- W. 公开 Request 方法存在且 no-throw(18 个非 recv-only 号,反射校验 + 直接调用) ----
                bool sendMethodsExist = t.GetMethod("RequestMoneyTreePanel", PF) != null && t.GetMethod("RequestMoneyTreeDraw", PF) != null
                    && t.GetMethod("RequestMoneyTreeCumulateClaim", PF) != null && t.GetMethod("RequestMoneyTreeShopExchange", PF) != null
                    && t.GetMethod("RequestMoneyTreeCurrency", PF) != null
                    && t.GetMethod("RequestFtvActivePanel", PF) != null && t.GetMethod("RequestFtvActiveSubmit", PF) != null
                    && t.GetMethod("RequestFtvActiveServerClaim", PF) != null
                    && t.GetMethod("RequestSaiboPanel", PF) != null && t.GetMethod("RequestSaiboStage", PF) != null && t.GetMethod("RequestSaiboDraw", PF) != null
                    && t.GetMethod("RequestBindDiamondPanel", PF) != null && t.GetMethod("RequestBindDiamondDraw", PF) != null && t.GetMethod("RequestBindDiamondRecord", PF) != null
                    && t.GetMethod("RequestRedRainPanel", PF) != null && t.GetMethod("RequestRedRainGrab", PF) != null
                    && t.GetMethod("RequestHolyCallPanel", PF) != null && t.GetMethod("RequestHolyCallRareDraw", PF) != null;
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.CustomActivity.CustomActivityController)ctrl;
                    c.RequestMoneyTreePanel(MT_BASE, MT_SUB);
                    c.RequestMoneyTreeDraw(MT_BASE, MT_SUB, 1, 0);
                    c.RequestMoneyTreeCumulateClaim(MT_BASE, MT_SUB, 1);
                    c.RequestMoneyTreeShopExchange(MT_BASE, MT_SUB, 3);
                    c.RequestMoneyTreeCurrency(MT_BASE, MT_SUB);
                    c.RequestFtvActivePanel(FA_BASE, FA_SUB);
                    c.RequestFtvActiveSubmit(FA_BASE, FA_SUB, 2);
                    c.RequestFtvActiveServerClaim(FA_BASE, FA_SUB, 9);
                    c.RequestSaiboPanel(SB_BASE, SB_SUB);
                    c.RequestSaiboStage(SB_BASE, SB_SUB, 1, 2, 1);
                    c.RequestSaiboDraw(SB_BASE, SB_SUB, 1, 0);
                    c.RequestBindDiamondPanel(BD_BASE, BD_SUB);
                    c.RequestBindDiamondDraw(BD_BASE, BD_SUB);
                    c.RequestBindDiamondRecord(BD_BASE, BD_SUB);
                    c.RequestRedRainPanel(RR_SUB);
                    c.RequestRedRainGrab(RR_SUB);
                    c.RequestHolyCallPanel(HC_BASE, HC_SUB);
                    c.RequestHolyCallRareDraw(HC_BASE, HC_SUB);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY customact_festival send methods threw: " + e); }
                bool bSend = sendMethodsExist && sendNoThrow;
                Debug.Log("CLIVERIFY customact_festival 公开发送方法存在且noThrow methodsExist=" + sendMethodsExist + " noThrow=" + sendNoThrow + " ok=" + bSend);

                bool pass = !anyThrew && bRegistered
                    && b33190 && b33191 && b33192 && b33168 && b33231
                    && b33193 && b33194 && b33195 && b33196
                    && b33165 && b33166 && b33167
                    && b33130 && b33131 && b33132
                    && b33155 && b33157 && b33158
                    && b33221 && b33222
                    && bEventCounts && dead33196 && dead33158 && b33196FollowUp && bSilentThresholds && bSend;

                Debug.Log("CLIVERIFY customact_festival VERDICT registered=" + bRegistered
                    + " moneytree=" + (b33190 && b33191 && b33192 && b33168 && b33231)
                    + " ftvactive=" + (b33193 && b33194 && b33195 && b33196)
                    + " saibo=" + (b33165 && b33166 && b33167)
                    + " binddiamond=" + (b33130 && b33131 && b33132)
                    + " redrain=" + (b33155 && b33157 && b33158)
                    + " holycall=" + (b33221 && b33222)
                    + " eventCounts=" + bEventCounts + " deadRecv=" + (dead33196 && dead33158) + " sendApi=" + bSend
                    + " anyThrew=" + anyThrew + " pass=" + pass);

                model.Clear();
                model.ClearFestival();
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
