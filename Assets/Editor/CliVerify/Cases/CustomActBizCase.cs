using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 商业礼包族(自动循环 轮17 P5)实证:ZERO_MALL=36(33136/37/38)/FTVINVEST=62(33212,含 On33211 升级
    /// 落地校验)/VIPGIFT=71(33215)/DAILYSUPPLY=61(33209)/NAMEVERIFY=69(33169)/批量兑换(33179)/
    /// QUESTIONNAIRE=90(33236)/MANY_RECHARGE=107(33247)/冲级(33248)/ADVERTISEMENT=111(33250/33251)/
    /// RED_ENVELOPE_REBATE=117(33256,含 On33255 升级落地校验)/CARNIVAL=118(33258)/
    /// TIRED_CHARGE_POLITE=121(33259)/OVER_VIEW=126(33264+RequireOverViewRew遍历补拉)/
    /// RARE_SURFACE=128(33265/33257)/33197获奖记录/33140嗨点(防御)/33115完美情缘/33216封测返还/
    /// 充值统计15955-15960。合成包驱动 CustomActivityController 反射喂包,断言 CustomActivityModel.Biz.cs
    /// 落地字段/事件(模板 CustomActCoreCase/MarriageCase,纯逻辑段)。
    ///
    /// 重点覆盖:①33179 字段序 ErrorCode,Num,BaseType,SubType,Grade(非套模板顺序);②33256/33265/33179
    /// Errcode 三种位置(第3字段/末尾/开头);③33259 两层嵌套(List[]→RewardList[])+33197 三层嵌套
    /// (LogList/SelfList[]→RewardList ObjectList)用 2 元素数组断言游标读完(计数+末条探针);④33251/33257/
    /// 33140 recv-only/防御:断言无对应公开 Request 方法且 feed 不抛;⑤On33211/On33255 升级校验:Investments/
    /// 12 字段确实落 Model(而非此前的读丢),且原有图标逻辑(ActivityIconManager 调用路径)未被破坏——用
    /// no-throw + 既有字段断言间接验证未删既有行为;⑥OVER_VIEW 遍历补拉:合成含 OVER_VIEW 条目
    /// (condition="[{36,1}]")+ 一条 base_type=36,show_id=1 的活动的 33101 包,断言追发日志命中。
    ///
    /// 不接入 CliVerify.cs(收尾阶段单点挂钩,spec §8-4)。
    /// </summary>
    public static class CustomActBizCase
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
                model.ClearBiz();

                object ctrl = Shenxiao.Module.Core.CustomActivity.CustomActivityController.Instance;
                System.Type t = ctrl.GetType();
                bool anyThrew = false;
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY customact_biz handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY customact_biz " + method + " threw: " + e);
                    }
                }

                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();

                // ---- 0. 注册线核实(NetManager._handlers,同 CustomActCoreCase 15b血训) ----
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ZEROMALL_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ZEROMALL_BUY,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ZEROMALL_REBATE, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FTVINVEST_BUY,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_VIPGIFT_SET_GRADE, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_DAILYSUPPLY_LIVENESS,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_NAMEVERIFY_CONFIRM, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BATCH_EXCHANGE,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_QUESTIONNAIRE_SUBMIT, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MANYRECHARGE_PANEL,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LEVEL_RUSH_GIFT, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_AD_CD_LIST,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_RUSH_RANK_TOP_PLAYER_PUSH, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REDENVELOPE_WITHDRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_CARNIVAL_TASK, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_TIRED_CHARGE_POLITE,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_OVERVIEW_REWARD, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_RARESURFACE_CLAIM,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REWARD_LIST_PUSH, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_WIN_LOG,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_HI_POINT_INFO, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_MARRIAGE_ACT_INFO,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BETA_RECHARGE_RETURN, Shenxiao.Framework.Net.Proto.RECHARGE_STAT_DAILY_ACCUM_INFO,
                    Shenxiao.Framework.Net.Proto.RECHARGE_STAT_DAILY_ACCUM_REWARD, Shenxiao.Framework.Net.Proto.RECHARGE_STAT_ACT_RECHARGE,
                    Shenxiao.Framework.Net.Proto.RECHARGE_STAT_POLITE_RECHARGE, Shenxiao.Framework.Net.Proto.RECHARGE_STAT_TODAY,
                    Shenxiao.Framework.Net.Proto.RECHARGE_STAT_HISTORY,
                };
                bool bRegistered = handlers != null;
                var missingReg = new List<int>();
                if (handlers != null)
                {
                    foreach (int id in mustBeRegistered) if (!handlers.Contains(id)) { bRegistered = false; missingReg.Add(id); }
                }
                Debug.Log("CLIVERIFY customact_biz 注册线核实 missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                int detailN = 0, resultN = 0; int lastResultCode = 0, lastResultBase = -1, lastResultSub = -1;
                System.Action<int, int> onDetail = (b, s) => detailN++;
                System.Action<int, int, int> onResult = (b, s, code) => { resultN++; lastResultCode = code; lastResultBase = b; lastResultSub = s; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);

                // ============================================================================================
                // ZERO_MALL=36:33136/33137/33138
                // ============================================================================================
                byte[] p33136ok = new CliVerify.Pkt().I(1).H(5).H(1)
                    .H(1).C(0).C(1).I(2).S("零元档").S("desc").S("cond").S("reward")
                    .Bytes();
                Feed("On33136", p33136ok);
                var zeroMallPanel = model.GetZeroMallPanel(5);
                bool b33136 = zeroMallPanel != null && zeroMallPanel.RewardList.Count == 1
                    && zeroMallPanel.RewardList[0].Grade == 1 && zeroMallPanel.RewardList[0].Status == 1
                    && zeroMallPanel.RewardList[0].ReceiveTime == 2 && zeroMallPanel.RewardList[0].Reward == "reward";
                Feed("On33136", new CliVerify.Pkt().I(1720101).H(9).H(0).Bytes()); // 失败:不落地9号
                bool b33136fail = model.GetZeroMallPanel(9) == null;
                // 静默阈值镜像(ts:1218-1222,自动循环 轮17三镜头验收补):code==1012 不弹错,仅 return,断言无异常。
                Feed("On33136", new CliVerify.Pkt().I(1012).H(9).H(0).Bytes());
                bool b33136silent1012 = !anyThrew && model.GetZeroMallPanel(9) == null;
                b33136fail = b33136fail && b33136silent1012;
                Debug.Log("CLIVERIFY customact_biz 33136 0元豪礼界面 ok=" + b33136 + " failNotStored=" + b33136fail + " silent1012=" + b33136silent1012);

                // ---- 33136 空 reward_list 删条目镜像(ts:1240-1242,自动循环 轮17三镜头验收补):先放一条
                // (ZERO_MALL,77) 活动条目,喂 code=1+空 reward_list,断言该条目被从 ActList 删除且 Emit
                // 与 On33103 同款 LIST_REMOVE 事件(不落 ZeroMallPanel/不发 DETAIL_UPDATE)。 ----
                model.AddActEntries(new List<Shenxiao.Module.Core.CustomActivity.CustomActivityModel.ActEntry>
                {
                    new Shenxiao.Module.Core.CustomActivity.CustomActivityModel.ActEntry { BaseType = 36, SubType = 77, Name = "待删测试" },
                });
                int listRemoveCount = 0;
                System.Action onListRemove = () => listRemoveCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE, onListRemove);
                Feed("On33136", new CliVerify.Pkt().I(1).H(77).H(0).Bytes()); // code=1,subType=77,reward_list 空
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE, onListRemove);
                bool b33136EmptyDelete = listRemoveCount == 1 && model.GetActEntry(36, 77) == null && model.GetZeroMallPanel(77) == null;
                Debug.Log("CLIVERIFY customact_biz 33136 空reward_list删条目(镜像ts:1240-1242) listRemove=" + listRemoveCount + " ok=" + b33136EmptyDelete);

                resultN = 0;
                Feed("On33137", new CliVerify.Pkt().I(1).H(2).H(5).Bytes());
                bool b33137ok = resultN == 1 && lastResultCode == 1 && lastResultBase == 36 && lastResultSub == 5;
                Feed("On33137", new CliVerify.Pkt().I(1720102).H(2).H(5).Bytes());
                bool b33137 = b33137ok && resultN == 2 && lastResultCode == 1720102;
                Debug.Log("CLIVERIFY customact_biz 33137 0元豪礼购买 ok=" + b33137);

                resultN = 0;
                Feed("On33138", new CliVerify.Pkt().I(1).H(3).H(5).Bytes());
                bool b33138 = resultN == 1 && lastResultCode == 1;
                Debug.Log("CLIVERIFY customact_biz 33138 0元豪礼返利 ok=" + b33138);

                // ============================================================================================
                // FTVINVEST=62:33211 升级(Investments 落地)+33212
                // ============================================================================================
                byte[] p33211 = new CliVerify.Pkt().H(62).H(3).H(2).C(1).C(2).I(1700005000).Bytes();
                Feed("On33211", p33211);
                var ftvInvestInfo = model.GetFtvInvestInfo(62, 3);
                bool b33211upgrade = ftvInvestInfo != null && ftvInvestInfo.Investments.Count == 2
                    && ftvInvestInfo.Investments[0] == 1 && ftvInvestInfo.Investments[1] == 2 && ftvInvestInfo.BuyTime == 1700005000;
                Debug.Log("CLIVERIFY customact_biz 33211升级 Investments落地 investN=" + (ftvInvestInfo?.Investments.Count ?? -1) + " ok=" + b33211upgrade);

                resultN = 0;
                byte[] p33212ok = new CliVerify.Pkt().I(1).H(62).H(3).C(2).H(7).H(1).C(1).I(9001).I(5).Bytes();
                Feed("On33212", p33212ok);
                var ftvBuy = model.GetFtvInvestBuyResult(62, 3);
                bool b33212 = resultN == 1 && lastResultCode == 1 && ftvBuy != null && ftvBuy.Lv == 2 && ftvBuy.LoginDays == 7
                    && ftvBuy.RewardList.Count == 1 && ftvBuy.RewardList[0].GoodsId == 9001 && ftvBuy.RewardList[0].Num == 5;
                Feed("On33212", new CliVerify.Pkt().I(1720103).H(62).H(3).C(2).H(7).H(0).Bytes());
                bool b33212fail = resultN == 2 && lastResultCode == 1720103;
                Debug.Log("CLIVERIFY customact_biz 33212 节日投资购买 ok=" + (b33212 && b33212fail));

                // ============================================================================================
                // VIPGIFT=71:33215
                // ============================================================================================
                resultN = 0;
                byte[] p33215 = new CliVerify.Pkt().I(1).H(71).H(1).H(4).H(1).C(1).I(1001).I(10).Bytes();
                Feed("On33215", p33215);
                var vipGift = model.GetVipGiftInfo(71, 1);
                bool b33215 = resultN == 1 && vipGift != null && vipGift.Grade == 4 && vipGift.NowCost.Count == 1 && vipGift.NowCost[0].GoodsId == 1001;
                Debug.Log("CLIVERIFY customact_biz 33215 vip礼包折扣 ok=" + b33215);

                // ============================================================================================
                // DAILYSUPPLY=61:33209(双向均无 BaseType/SubType)
                // ============================================================================================
                Feed("On33209", new CliVerify.Pkt().H(88).Bytes());
                bool b33209 = model.DailySupplyLiveness == 88;
                Debug.Log("CLIVERIFY customact_biz 33209 每日补给活跃度 ok=" + b33209);

                // ============================================================================================
                // NAMEVERIFY=69:33169(读写均空包)
                // ============================================================================================
                Feed("On33169", new CliVerify.Pkt().Bytes());
                bool b33169 = model.NameVerifyConfirmedAt > 0;
                Debug.Log("CLIVERIFY customact_biz 33169 实名认证(空包) ok=" + b33169);

                // ============================================================================================
                // 批量兑换:33179。**字段序 ErrorCode,Num,BaseType,SubType,Grade**
                // ============================================================================================
                Feed("On33179", new CliVerify.Pkt().I(1).H(3).H(59).H(2).H(4).Bytes());
                var batchEx = model.GetLastBatchExchange();
                bool b33179 = batchEx != null && batchEx.Num == 3 && batchEx.BaseType == 59 && batchEx.SubType == 2 && batchEx.Grade == 4;
                Debug.Log("CLIVERIFY customact_biz 33179 批量兑换(字段序ErrorCode,Num,Base,Sub,Grade) ok=" + b33179);

                // ============================================================================================
                // QUESTIONNAIRE=90:33236
                // ============================================================================================
                Feed("On33236", new CliVerify.Pkt().I(1).C(8).Bytes());
                var quest = model.GetLastQuestionnaire();
                bool b33236 = quest != null && quest.QuestionType == 8;
                // 三镜头订正:老端 On33236(ts:2356-2361)从不 ShowError,且判断是 `if(error_code)` 真值(非0即真,
                // 不是 ==1)。喂一个非1的真值错误码(9,truthy):不落 Model(仍是 code==1 那次的值),但应无异常
                // (证明去掉 ShowError 分支没崩、且追发 33104 分支被安全触发,不 mock 网络)。
                Feed("On33236", new CliVerify.Pkt().I(9).C(3).Bytes());
                var questAfterTruthyFail = model.GetLastQuestionnaire();
                bool b33236truthy = !anyThrew && questAfterTruthyFail != null && questAfterTruthyFail.QuestionType == 8; // 未被覆盖
                b33236 = b33236 && b33236truthy;
                Debug.Log("CLIVERIFY customact_biz 33236 问卷调查(老端从不ShowError+errcode truthy追发33104) truthyNoThrow=" + b33236truthy + " ok=" + b33236);

                // ============================================================================================
                // MANY_RECHARGE=107:33247
                // ============================================================================================
                Feed("On33247", new CliVerify.Pkt().H(107).H(1).C(3).Bytes());
                var manyRecharge = model.GetManyRechargeInfo(107, 1);
                bool b33247 = manyRecharge != null && manyRecharge.Times == 3;
                Debug.Log("CLIVERIFY customact_biz 33247 多倍充值界面 ok=" + b33247);

                // ============================================================================================
                // 冲级礼包:33248(全局单值,无 Base/Sub)
                // ============================================================================================
                Feed("On33248", new CliVerify.Pkt().I(1700000000).I(1700086400).Bytes());
                var rushGift = model.GetLevelRushGift();
                bool b33248 = rushGift != null && rushGift.MinTime == 1700000000 && rushGift.MaxTime == 1700086400;
                Debug.Log("CLIVERIFY customact_biz 33248 冲级礼包 ok=" + b33248);

                // ============================================================================================
                // ADVERTISEMENT=111:33250(CdLists)+33251(recv-only)
                // ============================================================================================
                byte[] p33250 = new CliVerify.Pkt().H(111).H(1).H(2)
                    .I(1).I(600)
                    .I(2).I(3600)
                    .Bytes();
                Feed("On33250", p33250);
                var adCd = model.GetAdCdList(111, 1);
                bool b33250 = adCd != null && adCd.CdLists.Count == 2 && adCd.CdLists[1].GradeId == 2 && adCd.CdLists[1].CdTime == 3600;
                Debug.Log("CLIVERIFY customact_biz 33250 广告cd列表(计数+末条探针) ok=" + b33250);

                bool noSend33251 = t.GetMethod("RequestRushRankTopPlayer", PF) == null;
                Feed("On33251", new CliVerify.Pkt().I(500).I(1).I(3).I(9000).I(0).Bytes());
                var rushTop = model.GetLastRushRankTopPlayer();
                bool b33251 = noSend33251 && rushTop != null && rushTop.RushRankId == 500 && rushTop.Rank == 3;
                Debug.Log("CLIVERIFY customact_biz 33251 头号玩家提示(recv-only) noSendMethod=" + noSend33251 + " ok=" + b33251);

                // ============================================================================================
                // RED_ENVELOPE_REBATE=117:33255 升级(12字段落地)+33256(Errcode第3字段)
                // ============================================================================================
                byte[] p33255 = new CliVerify.Pkt().H(117).H(1).C(1).I(1700000000).I(1700099999)
                    .H(10).H(20).C(1).C(0).C(1).C(0).I(3).I(5).Bytes();
                Feed("On33255", p33255);
                var rebateInfo = model.GetRedEnvelopeRebateInfo(117, 1);
                bool b33255upgrade = rebateInfo != null && rebateInfo.IsQuality == 1 && rebateInfo.LoginMoney == 10
                    && rebateInfo.RechargeMoney == 20 && rebateInfo.LoginWithdrawal == 1 && rebateInfo.RechargeWithdrawal == 0
                    && rebateInfo.LoginGlobalTimes == 3 && rebateInfo.RechargeGlobalTimes == 5;
                Debug.Log("CLIVERIFY customact_biz 33255升级 12字段落地 ok=" + b33255upgrade);

                resultN = 0;
                Feed("On33256", new CliVerify.Pkt().H(117).H(1).I(1).H(10).H(20).C(1).C(1).Bytes());
                var withdraw = model.GetRedEnvelopeWithdrawResult(117, 1);
                bool b33256 = resultN == 1 && lastResultCode == 1 && withdraw != null && withdraw.LoginMoney == 10;
                Feed("On33256", new CliVerify.Pkt().H(117).H(1).I(1720104).H(10).H(20).C(1).C(1).Bytes());
                bool b33256fail = resultN == 2 && lastResultCode == 1720104;
                Debug.Log("CLIVERIFY customact_biz 33256 红包返利提现(Errcode第3字段) ok=" + (b33256 && b33256fail));

                // ============================================================================================
                // CARNIVAL=118:33258
                // ============================================================================================
                byte[] p33258 = new CliVerify.Pkt().H(118).H(1).H(2).H(1).I(10).H(2).I(20).Bytes();
                Feed("On33258", p33258);
                var carnival = model.GetCarnivalTaskInfo(118, 1);
                bool b33258 = carnival != null && carnival.TaskList.Count == 2 && carnival.TaskList[1].Grade == 2 && carnival.TaskList[1].Process == 20;
                Debug.Log("CLIVERIFY customact_biz 33258 全民狂欢任务进度(计数+末条探针) ok=" + b33258);

                // ============================================================================================
                // TIRED_CHARGE_POLITE=121:33259(两层嵌套,2元素数组断言游标读完)
                // ============================================================================================
                byte[] p33259 = new CliVerify.Pkt().H(121).H(7).H(500).H(1).H(2)
                    .H(1).S("cond1").S("name1").S("desc1").H(1).C(0).C(1).S("r1")
                    .H(2).S("cond2").S("name2").S("desc2").H(2).C(0).C(1).S("r2a").C(1).C(0).S("r2b")
                    .Bytes();
                Feed("On33259", p33259);
                var tired = model.GetTiredChargePoliteInfo(121, 7);
                bool b33259 = tired != null && tired.RechargeNum == 500 && tired.IsRecharge == 1 && tired.List.Count == 2
                    && tired.List[1].Grade == 2 && tired.List[1].Name == "name2" && tired.List[1].RewardList.Count == 2
                    && tired.List[1].RewardList[1].Reward == "r2b" && tired.List[1].RewardList[1].FormType == 1;
                Debug.Log("CLIVERIFY customact_biz 33259 累充有礼(两层嵌套,游标读完) ok=" + b33259);

                // ============================================================================================
                // OVER_VIEW=126:33264(独立 feed,字段序订正 Grade/FormType/Reward:str)
                // ============================================================================================
                byte[] p33264 = new CliVerify.Pkt().H(36).H(5).H(2)
                    .H(1).C(0).S("reward_A")
                    .H(2).C(1).S("reward_B")
                    .Bytes();
                Feed("On33264", p33264);
                var overViewDirect = model.GetOverViewRewardInfo(36, 5);
                bool b33264 = overViewDirect != null && overViewDirect.RewardList.Count == 2
                    && overViewDirect.RewardList[1].Grade == 2 && overViewDirect.RewardList[1].FormType == 1 && overViewDirect.RewardList[1].Reward == "reward_B";
                Debug.Log("CLIVERIFY customact_biz 33264 活动奖励配置(字段序订正Grade/FormType/Reward:str) ok=" + b33264);

                // ---- OVER_VIEW 遍历补拉(RequireOverViewRew 镜像):喂 On33101 含 OVER_VIEW 条目
                // (condition="[{36,1}]")+ 一条 base_type=36,show_id=1 的活动,断言追发日志命中 ----
                model.Clear();
                logs.Clear();
                byte[] p33101ForOverView = new CliVerify.Pkt().H(2)
                    .H(36).H(5).C(1).H(1).H(0).S("零元豪礼").S("d").S("").I(1700000000).I(1700900000)
                    .H(126).H(1).C(1).H(99).H(0).S("总览").S("d").S("[{36,1}]").I(1700000000).I(1700900000)
                    .Bytes();
                Feed("On33101", p33101ForOverView);
                bool overViewLogHit = logs.Exists(s => s.Contains("OVER_VIEW 遍历补拉") && s.Contains("requested33264=1"));
                Debug.Log("CLIVERIFY customact_biz OVER_VIEW遍历补拉(镜像RequireOverViewRew) logHit=" + overViewLogHit + " ok=" + overViewLogHit);

                // ============================================================================================
                // RARE_SURFACE=128:33265(Errcode末尾)+33257(recv-only,被wxOneMoney复用)
                // ============================================================================================
                resultN = 0;
                Feed("On33265", new CliVerify.Pkt().H(128).H(1).H(2).I(1).Bytes());
                var rareSurface = model.GetRareSurfaceClaimResult(128, 1);
                bool b33265 = resultN == 1 && lastResultCode == 1 && rareSurface != null && rareSurface.Grade == 2;
                Feed("On33265", new CliVerify.Pkt().H(128).H(1).H(2).I(1720105).Bytes());
                bool b33265fail = resultN == 2 && lastResultCode == 1720105;
                Debug.Log("CLIVERIFY customact_biz 33265 绝版外显领取(Errcode末尾) ok=" + (b33265 && b33265fail));

                bool noSend33257 = t.GetMethod("RequestRewardListPush", PF) == null;
                byte[] p33257 = new CliVerify.Pkt().H(61).H(1).H(1).H(2).I(9001).I(5).Bytes();
                Feed("On33257", p33257);
                var rewardPush = model.GetRewardListPush(61, 1);
                bool b33257 = noSend33257 && rewardPush != null && rewardPush.RewardList.Count == 1 && rewardPush.RewardList[0].Style == 2;
                Debug.Log("CLIVERIFY customact_biz 33257 通用奖励列表推送(recv-only) noSendMethod=" + noSend33257 + " ok=" + b33257);

                // ============================================================================================
                // 活动通用获奖记录:33197(三层嵌套,LogList/SelfList 各2元素+RewardList ObjectList,游标探针)
                // ============================================================================================
                byte[] p33197 = new CliVerify.Pkt().H(36).H(1)
                    .H(2)
                        .L(9001).S("张三").H(1).C(1).I(1001).I(5)
                        .L(9002).S("李四").H(2).C(1).I(1002).I(6).C(0).I(1003).I(7)
                    .H(1)
                        .L(9003).S("自己").H(0)
                    .Bytes();
                Feed("On33197", p33197);
                var winLog = model.GetWinLog(36, 1);
                bool b33197 = winLog != null && winLog.LogList.Count == 2 && winLog.SelfList.Count == 1
                    && winLog.LogList[1].RoleId == 9002 && winLog.LogList[1].RewardList.Count == 2
                    && winLog.LogList[1].RewardList[1].GoodsId == 1003 && winLog.LogList[1].RewardList[1].Num == 7
                    && winLog.SelfList[0].RoleId == 9003 && winLog.SelfList[0].RewardList.Count == 0;
                Debug.Log("CLIVERIFY customact_biz 33197 获奖记录(三层嵌套,游标读完) ok=" + b33197);

                // ============================================================================================
                // 嗨点 HOTPOINT:33140(防御recv,死路径,只验安全解析不崩+无发送方法)
                // ============================================================================================
                bool noSend33140 = t.GetMethod("RequestHiPointInfo", PF) == null;
                byte[] p33140 = new CliVerify.Pkt().I(500).H(1)
                    .I(1).I(2).S("cond").S("name").H(1).H(2).I(100).S("icon").L(999).H(1).I(50).I(20).S("dec").H(1)
                    .Bytes();
                Feed("On33140", p33140);
                bool b33140 = noSend33140 && !anyThrew;
                Debug.Log("CLIVERIFY customact_biz 33140 嗨点(防御recv,死路径安全解析) noSendMethod=" + noSend33140 + " noThrow=" + !anyThrew + " ok=" + b33140);

                // ============================================================================================
                // 完美情缘 actMarriage=25:33115(Code开头+WeddingTypeList)
                // ============================================================================================
                resultN = 0;
                byte[] p33115 = new CliVerify.Pkt().I(1).H(1).C(1).C(0).H(1).C(2).H(3).Bytes();
                Feed("On33115", p33115);
                var custMarriage = model.GetCustomActMarriageInfo();
                bool b33115 = resultN == 1 && lastResultCode == 1 && custMarriage != null && custMarriage.Opr == 1
                    && custMarriage.WeddingTypeList.Count == 1 && custMarriage.WeddingTypeList[0].WeddingTypeId == 2 && custMarriage.WeddingTypeList[0].WeddingTimes == 3;
                Debug.Log("CLIVERIFY customact_biz 33115 完美情缘(Code开头) ok=" + b33115);

                // ============================================================================================
                // 封测充值返还 BETA_ACT=77:33216
                // ============================================================================================
                Feed("On33216", new CliVerify.Pkt().I(1000).I(500).I(3).Bytes());
                var beta = model.GetBetaRechargeReturn();
                bool b33216 = beta != null && beta.Gold == 1000 && beta.ReturnGold == 500 && beta.LoginDays == 3;
                Debug.Log("CLIVERIFY customact_biz 33216 封测充值返还 ok=" + b33216);

                // ============================================================================================
                // 充值统计 15955-15960
                // ============================================================================================
                byte[] p15955 = new CliVerify.Pkt().H(1).I(3)
                    .H(1).H(10).C(1).I(100).I(500).H(1).C(1).I(2001).I(1).S("cond1").S("desc1")
                    .Bytes();
                Feed("On15955", p15955);
                var accumInfo = model.GetDailyAccumInfo(1);
                bool b15955 = accumInfo != null && accumInfo.Num == 3 && accumInfo.RewardInfos.Count == 1
                    && accumInfo.RewardInfos[0].Id == 10 && accumInfo.RewardInfos[0].RewardList.Count == 1
                    && accumInfo.RewardInfos[0].RewardList[0].GoodsId == 2001 && accumInfo.RewardInfos[0].Desc == "desc1";
                Debug.Log("CLIVERIFY customact_biz 15955 每日累充信息 ok=" + b15955);

                byte[] p15956 = new CliVerify.Pkt().H(1)
                    .H(1).H(11).C(0).I(50).I(200).L(999).H(0).S("cond2").S("desc2")
                    .Bytes();
                Feed("On15956", p15956);
                var accumReward = model.GetDailyAccumReward(1);
                bool b15956 = accumReward != null && accumReward.RewardList.Count == 1 && accumReward.RewardList[0].GoldNum == 999 && accumReward.RewardList[0].RewardList.Count == 0;
                Debug.Log("CLIVERIFY customact_biz 15956 每日累充奖励列表(含GoldNum:64) ok=" + b15956);

                Feed("On15957", new CliVerify.Pkt().H(6).H(2).I(3000).Bytes());
                var actRecharge = model.GetActRecharge(6, 2);
                bool b15957 = actRecharge != null && actRecharge.TotalGold == 3000;
                Debug.Log("CLIVERIFY customact_biz 15957 某活动类型充值总额 ok=" + b15957);

                Feed("On15958", new CliVerify.Pkt().H(107).H(1).I(1500).Bytes());
                var politeRecharge = model.GetPoliteRecharge(107, 1);
                bool b15958 = politeRecharge != null && politeRecharge.TotalGold == 1500;
                Debug.Log("CLIVERIFY customact_biz 15958 充值有礼充值金额 ok=" + b15958);

                // B3(三镜头验收订正):On15959 收到后应追发 RequestActDetail(109,1)(镜像 ts:1319 RequireActInfo
                // (CON_RECHARGE,1)→分发表末尾兜底 Fire(33104,109,1))。RequestActDetail 自带"活动列表无此
                // 条目则不发送"guard,先放一条 (109,1) 活动条目让追发真正命中,断言 feed 后无异常。
                model.AddActEntries(new List<Shenxiao.Module.Core.CustomActivity.CustomActivityModel.ActEntry>
                {
                    new Shenxiao.Module.Core.CustomActivity.CustomActivityModel.ActEntry { BaseType = 109, SubType = 1, Name = "CON_RECHARGE测试" },
                });
                Feed("On15959", new CliVerify.Pkt().I(999).Bytes());
                bool b15959 = model.TodayRechargeGold == 999 && !anyThrew;
                Debug.Log("CLIVERIFY customact_biz 15959 当天充值金额(无Type/SubType,B3追发RequestActDetail(109,1)镜像ts:1319) noThrow=" + !anyThrew + " ok=" + b15959);

                byte[] p15960 = new CliVerify.Pkt().H(2).I(1700000000).I(200).I(1700086400).I(300).Bytes();
                Feed("On15960", p15960);
                IReadOnlyList<Shenxiao.Module.Core.CustomActivity.CustomActivityModel.RechargeHistoryItem> history = model.GetRechargeHistory();
                bool b15960 = history.Count == 2 && history[1].Time == 1700086400 && history[1].TotalGold == 300;
                Debug.Log("CLIVERIFY customact_biz 15960 几天前充值金额列表(计数+末条探针) ok=" + b15960);

                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);

                // ---- 公开 Request 方法反射存在性 + no-throw 抽样调用(非recv-only号) ----
                bool sendMethodsExist =
                    t.GetMethod("RequestZeroMallBuy", PF) != null && t.GetMethod("RequestZeroMallRebate", PF) != null
                    && t.GetMethod("RequestFtvInvestBuy", PF) != null && t.GetMethod("RequestVipGiftSetGrade", PF) != null
                    && t.GetMethod("RequestDailySupplyLiveness", PF) != null && t.GetMethod("RequestNameVerifyConfirm", PF) != null
                    && t.GetMethod("RequestBatchExchange", PF) != null && t.GetMethod("RequestQuestionnaireSubmit", PF) != null
                    && t.GetMethod("RequestManyRechargePanel", PF) != null && t.GetMethod("RequestLevelRushGift", PF) != null
                    && t.GetMethod("RequestRedEnvelopeWithdraw", PF) != null && t.GetMethod("RequestCarnivalTask", PF) != null
                    && t.GetMethod("RequestTiredChargePolite", PF) != null && t.GetMethod("RequestOverviewReward", PF) != null
                    && t.GetMethod("RequestRareSurfaceClaim", PF) != null && t.GetMethod("RequestWinLog", PF) != null
                    && t.GetMethod("RequestMarriageActInfo", PF) != null && t.GetMethod("RequestPoliteRecharge", PF) != null
                    && t.GetMethod("RequestTodayRecharge", PF) != null && t.GetMethod("RequestRechargeHistory", PF) != null;
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.CustomActivity.CustomActivityController)ctrl;
                    c.RequestZeroMallBuy(5, 1); c.RequestZeroMallRebate(5, 1); c.RequestFtvInvestBuy(62, 3, 2);
                    c.RequestVipGiftSetGrade(71, 1, 4); c.RequestDailySupplyLiveness(); c.RequestNameVerifyConfirm();
                    c.RequestBatchExchange(59, 2, 4, 3); c.RequestQuestionnaireSubmit(8); c.RequestManyRechargePanel(107, 1);
                    c.RequestLevelRushGift(); c.RequestRedEnvelopeWithdraw(117, 1, 1, "pkg", "tok");
                    c.RequestCarnivalTask(118, 1); c.RequestTiredChargePolite(121, 7); c.RequestOverviewReward(36, 5);
                    c.RequestRareSurfaceClaim(128, 1, 2); c.RequestWinLog(36, 1); c.RequestMarriageActInfo(1, 2);
                    c.RequestPoliteRecharge(107, 1); c.RequestTodayRecharge(); c.RequestRechargeHistory(3);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY customact_biz send methods threw: " + e); }
                bool bSend = sendMethodsExist && sendNoThrow;
                Debug.Log("CLIVERIFY customact_biz 公开Request方法 存在且noThrow methodsExist=" + sendMethodsExist + " noThrow=" + sendNoThrow + " ok=" + bSend);

                bool pass = !anyThrew && bRegistered
                    && b33136 && b33136fail && b33136EmptyDelete && b33137 && b33138
                    && b33211upgrade && b33212 && b33212fail
                    && b33215 && b33209 && b33169 && b33179 && b33236 && b33247 && b33248
                    && b33250 && b33251
                    && b33255upgrade && (b33256 && b33256fail) && b33258 && b33259
                    && b33264 && overViewLogHit
                    && (b33265 && b33265fail) && b33257
                    && b33197 && b33140 && b33115 && b33216
                    && b15955 && b15956 && b15957 && b15958 && b15959 && b15960
                    && bSend;

                Debug.Log("CLIVERIFY customact_biz VERDICT registered=" + bRegistered
                    + " zeroMall=" + (b33136 && b33137 && b33138) + " zeroMallEmptyDelete=" + b33136EmptyDelete
                    + " ftvInvest=" + (b33211upgrade && b33212)
                    + " vipGift=" + b33215 + " dailySupply=" + b33209 + " nameVerify=" + b33169 + " batchExchange=" + b33179
                    + " questionnaire=" + b33236 + " manyRecharge=" + b33247 + " levelRush=" + b33248
                    + " advertisement=" + (b33250 && b33251) + " redEnvelope=" + (b33255upgrade && b33256)
                    + " carnival=" + b33258 + " tiredCharge=" + b33259 + " overView=" + (b33264 && overViewLogHit)
                    + " rareSurface=" + (b33265 && b33257) + " winLog=" + b33197 + " hiPoint=" + b33140
                    + " marriage=" + b33115 + " betaAct=" + b33216 + " rechargeStat=" + (b15955 && b15956 && b15957 && b15958 && b15959 && b15960)
                    + " sendApi=" + bSend + " anyThrew=" + anyThrew + " pass=" + pass);

                model.Clear();
                model.ClearBiz();
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
