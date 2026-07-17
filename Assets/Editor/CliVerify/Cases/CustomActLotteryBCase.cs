using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// P3 抽奖B(自动循环 轮17,spec §4,14号)实证:GASHAPON(103) 33245/33246、LUC_TREA_TWO(102) 33243/33244、
    /// ONLINE_DRAW(81) 33217/33266、LUC_TREA(80) 33213/33214、FORTUNECAT(87) 33224/33225/33226、
    /// BIND_JAGE_WISH(127) 33260/33262/33263。合成包驱动 CustomActivityController 反射喂包,断言
    /// CustomActivityModel 落地字段/事件(模板 CustomActCoreCase/MarriageCase)。
    ///
    /// 每号覆盖:成功包(落 Model + 事件)+ 失败/边界包(ShowError 降级,Model 不被覆盖,事件仍发,no-throw)。
    /// 嵌套数组段(33214 item_to_bin_6 嵌套三元组、33224 双数组、33226 双记录列表)额外断言游标读完
    /// (count + 末条探针),证明 NetReader 没有因为字段顺序错读而错位。
    /// </summary>
    public static class CustomActLotteryBCase
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
                model.ClearLotteryB();

                object ctrl = Shenxiao.Module.Core.CustomActivity.CustomActivityController.Instance;
                System.Type t = ctrl.GetType();
                bool anyThrew = false;
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY customact_lotteryB handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY customact_lotteryB " + method + " threw: " + e);
                    }
                }

                // ---- 0. 注册线核实(NetManager._handlers 全 14 号都真挂上,不仅仅是反射能调到方法体) ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_GASHAPON_INFO, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_GASHAPON_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LUCTREA2_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LUCTREA2_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ONLINEDRAW_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ONLINEDRAW_GOODS_POWER,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LUCTREA_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LUCTREA_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FORTUNECAT_INFO, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FORTUNECAT_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_FORTUNECAT_RECORD, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BINDJAGE_INFO,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BINDJAGE_DRAW, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_BINDJAGE_FREEGIFT,
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
                Debug.Log("CLIVERIFY customact_lotteryB 注册线核实 missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                int detailN = 0, resultN = 0; int lastResultCode = 0;
                System.Action<int, int> onDetail = (b, s) => detailN++;
                System.Action<int, int, int> onResult = (b, s, code) => { resultN++; lastResultCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);

                // ---- A. GASHAPON(103):33245 信息(无 ErrorCode)/ 33246 开抽(成功+失败) ----
                byte[] p33245 = new CliVerify.Pkt().H(103).H(1).I(1000).I(500).H(10).I(999).S("购买消耗A").S("十连消耗B")
                    .H(2).H(1).C(1).C(0).S("奖励1").H(2).C(0).C(1).S("奖励2")
                    .H(1).H(5).C(1).H(20).S("大奖")
                    .H(1).H(9).H(100).S("兑换奖")
                    .Bytes();
                Feed("On33245", p33245);
                var gashaponInfo = model.GetGashaponInfo(103, 1);
                bool b33245 = gashaponInfo != null && gashaponInfo.MaxLuck == 1000 && gashaponInfo.DrawList.Count == 2
                    && gashaponInfo.DrawList[1].Reward == "奖励2" && gashaponInfo.GrandList[0].NeedNum == 20
                    && gashaponInfo.ExchangeList[0].NeedPoint == 100;
                Debug.Log("CLIVERIFY customact_lotteryB 33245 GASHAPON信息 drawN=" + (gashaponInfo?.DrawList.Count ?? -1) + " ok=" + b33245);

                byte[] p33246ok = new CliVerify.Pkt().I(1).H(103).H(1).C(1).C(0).I(600).I(5).H(1).H(2).S("中奖啦").C(1).Bytes();
                Feed("On33246", p33246ok);
                var draw246ok = model.GetGashaponDrawResult(103, 1);
                bool b33246ok = draw246ok != null && draw246ok.CurrentTimes == 5 && draw246ok.RewardList.Count == 1
                    && draw246ok.RewardList[0].Reward == "中奖啦";
                byte[] p33246fail = new CliVerify.Pkt().I(1720001).H(103).H(1).C(0).C(1).I(0).I(0).H(0).Bytes();
                Feed("On33246", p33246fail);
                var draw246fail = model.GetGashaponDrawResult(103, 1);
                bool b33246fail = draw246fail != null && draw246fail.CurrentTimes == 5; // 失败不覆盖
                bool b33246 = b33246ok && b33246fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33246 GASHAPON开抽 ok=" + b33246ok + " failNotOverwrite=" + b33246fail + " pass=" + b33246);

                // ---- B. LUC_TREA_TWO(102):33243 界面(无 ErrorCode)/ 33244 抽奖(ErrorCode 第5顶层字段) ----
                byte[] p33243 = new CliVerify.Pkt().H(102).H(2).H(333).H(7)
                    .H(2).H(1).H(10).H(2).H(5)
                    .H(1).H(1).C(0).S("鉴宝1").S("desc1").S("cond1").S("rew1")
                    .Bytes();
                Feed("On33243", p33243);
                var luctrea2Info = model.GetLuctrea2Info(102, 2);
                bool b33243 = luctrea2Info != null && luctrea2Info.DrawTime == 333 && luctrea2Info.Turn == 7
                    && luctrea2Info.GradeInfo.Count == 2 && luctrea2Info.GradeInfo[1].Count == 5
                    && luctrea2Info.RewardList[0].Name == "鉴宝1";
                Debug.Log("CLIVERIFY customact_lotteryB 33243 幸运鉴宝2界面 gradeInfoN=" + (luctrea2Info?.GradeInfo.Count ?? -1) + " ok=" + b33243);

                byte[] p33244ok = new CliVerify.Pkt().H(102).H(2).H(1).C(0).I(1)
                    .H(2).H(3).H(4)          // GradeList(单字段)
                    .H(1).C(1).I(5001).I(2)  // Reward(标准三元组)
                    .H(222).H(8)             // DrawTime,Turn(16位)
                    .H(1).H(9).H(3)          // GradeInfo
                    .Bytes();
                Feed("On33244", p33244ok);
                var draw244ok = model.GetLuctrea2DrawResult(102, 2);
                bool b33244ok = draw244ok != null && draw244ok.GradeList.Count == 2 && draw244ok.GradeList[1] == 4
                    && draw244ok.Reward.Count == 1 && draw244ok.Reward[0].GoodsId == 5001 && draw244ok.Turn == 8
                    && draw244ok.GradeInfo.Count == 1 && draw244ok.GradeInfo[0].Count == 3;
                byte[] p33244fail = new CliVerify.Pkt().H(102).H(2).H(1).C(0).I(1720002).H(0).H(0).H(0).H(0).H(0).Bytes();
                Feed("On33244", p33244fail);
                var draw244fail = model.GetLuctrea2DrawResult(102, 2);
                bool b33244fail = draw244fail != null && draw244fail.Turn == 8; // 失败不覆盖
                bool b33244 = b33244ok && b33244fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33244 幸运鉴宝2抽奖 ok=" + b33244ok + " failNotOverwrite=" + b33244fail + " pass=" + b33244);

                // ---- C. ONLINE_DRAW(81):33217 界面信息(ErrorCode 开头,DrawTime:32,WinnerList 含 write_figure)/
                // 33266 物品期望战力(ErrorCode 末尾) ----
                byte[] p33217ok = new CliVerify.Pkt().I(1).H(81).H(3).I(1700000000).C(1)
                    .H(1).L(123456789012L).AppendMinimalFigure("赢家甲")
                    .Bytes();
                Feed("On33217", p33217ok);
                var draw217ok = model.GetOnlineDrawInfo(81, 3);
                bool b33217ok = draw217ok != null && draw217ok.DrawTime == 1700000000 && draw217ok.WinnerList.Count == 1
                    && draw217ok.WinnerList[0].RoleId == 123456789012L && draw217ok.WinnerList[0].Figure != null
                    && draw217ok.WinnerList[0].Figure.name == "赢家甲";
                byte[] p33217fail = new CliVerify.Pkt().I(1720003).H(81).H(3).I(0).C(0).H(0).Bytes();
                Feed("On33217", p33217fail);
                var draw217fail = model.GetOnlineDrawInfo(81, 3);
                bool b33217fail = draw217fail != null && draw217fail.DrawTime == 1700000000; // 失败不覆盖
                bool b33217 = b33217ok && b33217fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33217 等级活跃抽奖界面(含write_figure) ok=" + b33217ok + " failNotOverwrite=" + b33217fail + " pass=" + b33217);

                byte[] p33266ok = new CliVerify.Pkt().H(81).H(3).L(999999L).I(1).Bytes();
                Feed("On33266", p33266ok);
                var power266ok = model.GetGoodsPowerResult(81, 3);
                bool b33266ok = power266ok != null && power266ok.Power == 999999L;
                byte[] p33266fail = new CliVerify.Pkt().H(81).H(3).L(0L).I(1720004).Bytes();
                Feed("On33266", p33266fail);
                var power266fail = model.GetGoodsPowerResult(81, 3);
                bool b33266fail = power266fail != null && power266fail.Power == 999999L; // 失败不覆盖
                bool b33266 = b33266ok && b33266fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33266 物品期望战力 ok=" + b33266ok + " failNotOverwrite=" + b33266fail + " pass=" + b33266);

                // ---- D. LUC_TREA(80):33213 界面(ErrorCode 末尾,Pool 标准三元组)/ 33214 抽奖(ErrorCode 第3顶层
                // 字段,Reward 嵌套 item_to_bin_6:Grade+嵌套三元组数组+Rare) ----
                byte[] p33213ok = new CliVerify.Pkt().H(80).H(4)
                    .H(2).C(1).I(6001).I(3).C(2).I(6002).I(1)
                    .I(1)
                    .Bytes();
                Feed("On33213", p33213ok);
                var pool213ok = model.GetLuctreaPool(80, 4);
                bool b33213ok = pool213ok != null && pool213ok.Pool.Count == 2 && pool213ok.Pool[1].GoodsId == 6002;
                byte[] p33213fail = new CliVerify.Pkt().H(80).H(4).H(0).I(1720005).Bytes();
                Feed("On33213", p33213fail);
                var pool213fail = model.GetLuctreaPool(80, 4);
                bool b33213fail = pool213fail != null && pool213fail.Pool.Count == 2; // 失败不覆盖
                bool b33213 = b33213ok && b33213fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33213 幸运抽奖界面(ErrorCode末尾) ok=" + b33213ok + " failNotOverwrite=" + b33213fail + " pass=" + b33213);

                byte[] p33214ok = new CliVerify.Pkt().H(80).H(4).I(1)
                    .H(2)
                    .H(1).H(1).C(1).I(7001).I(5).C(1)   // group1: Grade=1,RewardList=1条,Rare=1
                    .H(2).H(0).C(0)                      // group2: Grade=2,RewardList=0条,Rare=0
                    .Bytes();
                Feed("On33214", p33214ok);
                var draw214ok = model.GetLuctreaDrawResult(80, 4);
                bool b33214ok = draw214ok != null && draw214ok.Reward.Count == 2
                    && draw214ok.Reward[0].RewardList.Count == 1 && draw214ok.Reward[0].RewardList[0].GoodsId == 7001
                    && draw214ok.Reward[0].Rare == 1 && draw214ok.Reward[1].RewardList.Count == 0;
                byte[] p33214fail = new CliVerify.Pkt().H(80).H(4).I(1720006).H(0).Bytes();
                Feed("On33214", p33214fail);
                var draw214fail = model.GetLuctreaDrawResult(80, 4);
                bool b33214fail = draw214fail != null && draw214fail.Reward.Count == 2; // 失败不覆盖
                bool b33214 = b33214ok && b33214fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33214 幸运抽奖抽奖(嵌套item_to_bin_6) ok=" + b33214ok + " failNotOverwrite=" + b33214fail + " pass=" + b33214);

                // ---- E. FORTUNECAT(87):33224 信息(无 ErrorCode,RewardId:64,双数组)/ 33225 转盘(ErrorCode 开头,
                // 纯标量)/ 33226 转盘记录(无 ErrorCode,双记录列表) ----
                byte[] p33224 = new CliVerify.Pkt().H(87).H(5).I(50).I(8001).I(2)
                    .H(1).I(10).I(100).I(1).L(999999999999L)
                    .H(2).H(1).I(8100).I(1).C(1).H(2).I(8200).I(2).C(0)
                    .Bytes();
                Feed("On33224", p33224);
                var fcInfo = model.GetFortunecatInfo(87, 5);
                bool b33224 = fcInfo != null && fcInfo.Turns == 50 && fcInfo.RoundsList.Count == 1
                    && fcInfo.RoundsList[0].RewardId == 999999999999L && fcInfo.RewardList.Count == 2
                    && fcInfo.RewardList[1].IsHead == 0 && fcInfo.RewardList[0].IsHead == 1;
                Debug.Log("CLIVERIFY customact_lotteryB 33224 招财猫信息(RewardId:64+双数组) roundsN=" + (fcInfo?.RoundsList.Count ?? -1)
                    + " rewardN=" + (fcInfo?.RewardList.Count ?? -1) + " ok=" + b33224);

                byte[] p33225ok = new CliVerify.Pkt().I(1).H(87).H(5).H(3).I(8300).I(1).Bytes();
                Feed("On33225", p33225ok);
                var draw225ok = model.GetFortunecatDrawResult(87, 5);
                bool b33225ok = draw225ok != null && draw225ok.GoodsId == 8300;
                byte[] p33225fail = new CliVerify.Pkt().I(1720007).H(87).H(5).H(0).I(0).I(0).Bytes();
                Feed("On33225", p33225fail);
                var draw225fail = model.GetFortunecatDrawResult(87, 5);
                bool b33225fail = draw225fail != null && draw225fail.GoodsId == 8300; // 失败不覆盖
                bool b33225 = b33225ok && b33225fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33225 招财猫转盘 ok=" + b33225ok + " failNotOverwrite=" + b33225fail + " pass=" + b33225);

                byte[] p33226 = new CliVerify.Pkt().H(87).H(5)
                    .H(1).L(111).S("自己").I(8400).I(1)
                    .H(2).L(222).S("全服甲").I(8500).I(2).L(333).S("全服乙").I(8600).I(3)
                    .Bytes();
                Feed("On33226", p33226);
                var fcRecord = model.GetFortunecatRecord(87, 5);
                bool b33226 = fcRecord != null && fcRecord.SelfList.Count == 1 && fcRecord.GolbList.Count == 2
                    && fcRecord.GolbList[1].RoleName == "全服乙" && fcRecord.SelfList[0].RoleName == "自己";
                Debug.Log("CLIVERIFY customact_lotteryB 33226 招财猫转盘记录(双列表) selfN=" + (fcRecord?.SelfList.Count ?? -1)
                    + " golbN=" + (fcRecord?.GolbList.Count ?? -1) + " ok=" + b33226);

                // 33225/33226 死活口径订正(自动循环 轮17三镜头验收):老端 On33225/On33226 函数体整段注释+
                // 全仓零发送调用点=客户端侧死号(服务端 handle 仍活,pp_custom_act_list.erl:252/261)。反射断言
                // 两号均无公开 Request 方法(仿 CustomActBizCase noSend33251/noSend33257 先例),喂包防御断言
                // (b33225/b33226,已在上方)保留不动。
                bool noSend33225 = t.GetMethod("RequestFortunecatDraw", PF) == null;
                bool noSend33226 = t.GetMethod("RequestFortunecatRecord", PF) == null;
                bool bDead33225_33226 = noSend33225 && noSend33226;
                Debug.Log("CLIVERIFY customact_lotteryB 33225/33226 死号防御 noSend33225=" + noSend33225 + " noSend33226=" + noSend33226 + " ok=" + bDead33225_33226);

                // ---- F. BIND_JAGE_WISH(127):33260 信息(无 ErrorCode)/ 33262 开抽(ErrorCode 末尾)/
                // 33263 免费礼(ErrorCode 末尾) ----
                byte[] p33260 = new CliVerify.Pkt().H(127).H(6).C(2).C(1).C(3).H(10).C(0).Bytes();
                Feed("On33260", p33260);
                var jageInfo = model.GetBindJageInfo(127, 6);
                bool b33260 = jageInfo != null && jageInfo.Times == 10 && jageInfo.Turn == 3 && jageInfo.FreeTimes == 2;
                Debug.Log("CLIVERIFY customact_lotteryB 33260 心愿单信息 times=" + (jageInfo?.Times ?? -1) + " ok=" + b33260);

                byte[] p33262ok = new CliVerify.Pkt().H(127).H(6).H(2).C(4).H(11).I(1).Bytes();
                Feed("On33262", p33262ok);
                var draw262ok = model.GetBindJageDrawResult(127, 6);
                bool b33262ok = draw262ok != null && draw262ok.Grade == 2 && draw262ok.Times == 11;
                byte[] p33262fail = new CliVerify.Pkt().H(127).H(6).H(0).C(0).H(0).I(1720008).Bytes();
                Feed("On33262", p33262fail);
                var draw262fail = model.GetBindJageDrawResult(127, 6);
                bool b33262fail = draw262fail != null && draw262fail.Grade == 2; // 失败不覆盖(服务端失败也固定回 Grade=0,但本端不落 Model)
                bool b33262 = b33262ok && b33262fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33262 心愿单开抽(ErrorCode末尾,C2S仅2字段已订正) ok=" + b33262ok + " failNotOverwrite=" + b33262fail + " pass=" + b33262);

                byte[] p33263ok = new CliVerify.Pkt().H(127).H(6).I(1).Bytes();
                Feed("On33263", p33263ok);
                var free263ok = model.GetBindJageFreeGiftResult(127, 6);
                bool b33263ok = free263ok != null && free263ok.Code == 1;
                byte[] p33263fail = new CliVerify.Pkt().H(127).H(6).I(1720009).Bytes();
                Feed("On33263", p33263fail);
                var free263fail = model.GetBindJageFreeGiftResult(127, 6);
                bool b33263fail = free263fail != null && free263fail.Code == 1; // 失败不覆盖
                bool b33263 = b33263ok && b33263fail;
                Debug.Log("CLIVERIFY customact_lotteryB 33263 心愿单免费礼(ErrorCode末尾) ok=" + b33263ok + " failNotOverwrite=" + b33263fail + " pass=" + b33263);

                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                bool bEvents = detailN >= 7 && resultN >= 12; // 7 个纯信息号(含33217成功+失败共2次)+ 12 次成败操作回执
                Debug.Log("CLIVERIFY customact_lotteryB 事件计数 detailN=" + detailN + " resultN=" + resultN + " lastResultCode=" + lastResultCode + " ok=" + bEvents);

                // ---- G. 公开发送方法存在且 no-throw(反射校验 + 直接调用,12 组;33225/33226 已改死号见上方) ----
                string[] sendMethods =
                {
                    "RequestGashaponInfo", "RequestGashaponDraw", "RequestLuctrea2Info", "RequestLuctrea2Draw",
                    "RequestOnlineDrawInfo", "RequestOnlineDrawGoodsPower", "RequestLuctreaPool", "RequestLuctreaDraw",
                    "RequestFortunecatInfo",
                    "RequestBindJageInfo", "RequestBindJageDraw", "RequestBindJageFreeGift",
                };
                bool sendMethodsExist = true;
                var missingSend = new List<string>();
                foreach (string name in sendMethods)
                {
                    if (t.GetMethod(name, PF) == null) { sendMethodsExist = false; missingSend.Add(name); }
                }
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.CustomActivity.CustomActivityController)ctrl;
                    c.RequestGashaponInfo(103, 1);
                    c.RequestGashaponDraw(103, 1, 1, 0, 0);
                    c.RequestLuctrea2Info(102, 2);
                    c.RequestLuctrea2Draw(102, 2, 1, 0, 5);
                    c.RequestOnlineDrawInfo(81, 3);
                    c.RequestOnlineDrawGoodsPower(81, 3, 520100L);
                    c.RequestLuctreaPool(80, 4);
                    c.RequestLuctreaDraw(80, 4, 1, 0);
                    c.RequestFortunecatInfo(87, 5, 0);
                    c.RequestBindJageInfo(127, 6);
                    c.RequestBindJageDraw(127, 6);
                    c.RequestBindJageFreeGift(127, 6);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY customact_lotteryB send methods threw: " + e); }
                bool bSend = sendMethodsExist && sendNoThrow;
                Debug.Log("CLIVERIFY customact_lotteryB 12组发送方法 missing=[" + string.Join(",", missingSend) + "] noThrow=" + sendNoThrow + " ok=" + bSend);

                bool pass = !anyThrew && bRegistered && b33245 && b33246 && b33243 && b33244 && b33217 && b33266
                    && b33213 && b33214 && b33224 && b33225 && b33226 && b33260 && b33262 && b33263 && bEvents
                    && bDead33225_33226 && bSend;

                Debug.Log("CLIVERIFY customact_lotteryB VERDICT registered=" + bRegistered
                    + " 33245=" + b33245 + " 33246=" + b33246 + " 33243=" + b33243 + " 33244=" + b33244
                    + " 33217=" + b33217 + " 33266=" + b33266 + " 33213=" + b33213 + " 33214=" + b33214
                    + " 33224=" + b33224 + " 33225=" + b33225 + " 33226=" + b33226
                    + " 33260=" + b33260 + " 33262=" + b33262 + " 33263=" + b33263
                    + " events=" + bEvents + " sendApi=" + bSend + " anyThrew=" + anyThrew + " pass=" + pass);

                model.ClearLotteryB();
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块(与 ChatCase/FriendMailCase 的
        /// AppendMinimalFigure 逐字节相同,独立文件各自持有一份,避免跨用例文件耦合)。改 SCHEMA 顺序时三处必须同步。</summary>
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
