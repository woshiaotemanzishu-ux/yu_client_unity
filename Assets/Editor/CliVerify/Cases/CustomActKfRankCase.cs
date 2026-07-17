using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 定制活动跨服+榜(自动循环 轮17 P6)实证:KFGROUPBUY(88,33227/33228/33229/33230/33267)+ 消费/鲜花榜
    /// (224xx,22400/22403/22405,改 CustomActivityController.Kf.cs/CustomActivityModel.Kf.cs)+ TopPlayer 补全
    /// (22500/22503/22504/22505,改 TopPlayerController.cs)。合成包驱动两个控制器反射喂包,断言落地字段/事件/
    /// 死号纪律(模板 CustomActCoreCase/MarriageCase)。不接入 CliVerify.cs(收尾阶段单点挂钩)。
    ///
    /// 覆盖:①33227 信息落地(GpGoods 2 档);②33228 记录三层嵌套(FirstBuy 2 条+TailBuy 1 条,游标读完探针);
    /// ③33229 购买回执成败两包(成功落 Model+RewardList object_list,失败仅 ShowError 不落地);④33230 recv-only
    /// 原地更新已存在档位 BuyNum,且无公开发送方法;⑤33267 喊话成败两包,均更新 LastShoutTime(老端该处无
    /// else 拦截,失败也写);⑥22400 错误码阈值!=1(区别于 33100/22500 的!=1012)+无公开发送方法;⑦22403
    /// 跨服鲜花榜(RankList+FigureList 双数组,FigureList 内嵌 46 字段 FigureProto);⑧22405 消费榜;⑨TopPlayer
    /// 22500 错误码阈值!=1012;⑩22503 仅成功才重拉22502(失败静默,不对称行为);⑪22504 无论成败都落
    /// ClaimResult+重拉22502;⑫22505 GetWay 落本控制器私有缓存。
    /// </summary>
    public static class CustomActKfRankCase
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
                model.ClearKf();

                object ctrl = Shenxiao.Module.Core.CustomActivity.CustomActivityController.Instance;
                System.Type t = ctrl.GetType();
                object topCtrl = Shenxiao.Module.Core.CustomActivity.TopPlayerController.Instance;
                System.Type tt = topCtrl.GetType();
                bool anyThrew = false;

                void Feed(object c, System.Type ty, string method, byte[] pkt)
                {
                    MethodInfo m = ty.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY customact_kfrank handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(c, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY customact_kfrank " + method + " threw: " + e);
                    }
                }

                // ---- 0. 注册线核实(NetManager._handlers):两个控制器的 P6 新增号必须真的挂进去 ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                var baseTopCtrl = (Shenxiao.Framework.Net.BaseController)topCtrl;
                if (!baseTopCtrl.IsInitialized) baseTopCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_KFGROUPBUY_INFO, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_KFGROUPBUY_RECORD,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_KFGROUPBUY_BUY, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_KFGROUPBUY_COUNT_PUSH,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_KFGROUPBUY_SHOUT,
                    Shenxiao.Framework.Net.Proto.KF_FLOWER_RANK_ERROR, Shenxiao.Framework.Net.Proto.KF_FLOWER_RANK_INFO, Shenxiao.Framework.Net.Proto.CONSUME_RANK_INFO,
                    Shenxiao.Framework.Net.Proto.TOP_PLAYER_ERROR, Shenxiao.Framework.Net.Proto.TOP_PLAYER_GOAL_CLAIM,
                    Shenxiao.Framework.Net.Proto.TOP_PLAYER_RANK_CLAIM, Shenxiao.Framework.Net.Proto.TOP_PLAYER_GET_WAY,
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
                Debug.Log("CLIVERIFY customact_kfrank 注册线核实(NetManager._handlers) missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                // ==================== KFGROUPBUY(88) ====================

                // ---- A. 33227 跨服团购信息(GpGoods 2 档) ----
                int detailUpdateCount = 0;
                System.Action<int, int> onDetail = (b, s) => detailUpdateCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                byte[] p33227 = new CliVerify.Pkt().H(88).H(1)
                    .H(2)
                        .H(10).C(3).C(2).H(5)
                        .H(20).C(1).C(1).H(9)
                    .I(1700000000)
                    .Bytes();
                Feed(ctrl, t, "OnKfGroupBuyInfo", p33227);
                var info = model.GetKfGroupBuyInfo(88, 1);
                bool b33227 = info != null && info.GpGoods.Count == 2 && info.GpGoods[0].GradeId == 10 && info.GpGoods[0].FirstBuyCount == 3
                    && info.GpGoods[0].TailBuyCount == 2 && info.GpGoods[0].BuyNum == 5 && info.GpGoods[1].GradeId == 20
                    && info.LastShoutTime == 1700000000;
                Debug.Log("CLIVERIFY customact_kfrank 33227 团购信息 goodsN=" + (info?.GpGoods.Count ?? -1) + " ok=" + b33227);

                // ---- B. 33228 团购记录(FirstBuy 2条+TailBuy 1条,三层嵌套游标探针) ----
                byte[] p33228 = new CliVerify.Pkt().H(88).H(1)
                    .H(1)
                        .L(9001).S("买家甲").H(1001).H(2).H(10)
                        .H(2).I(100).I(200)
                        .I(1700001000)
                        .H(1).I(300)
                        .I(1700002000)
                    .Bytes();
                Feed(ctrl, t, "OnKfGroupBuyRecord", p33228);
                var records = model.GetKfGroupBuyRecords(88, 1);
                bool b33228 = records != null && records.Count == 1 && records[0].RoleId == 9001 && records[0].RoleName == "买家甲"
                    && records[0].ServerId == 1001 && records[0].ServerNum == 2 && records[0].GradeId == 10
                    && records[0].FirstBuy.Count == 2 && records[0].FirstBuy[0] == 100 && records[0].FirstBuy[1] == 200
                    && records[0].FirstBuyTime == 1700001000
                    && records[0].TailBuy.Count == 1 && records[0].TailBuy[0] == 300 && records[0].TailBuyTime == 1700002000;
                Debug.Log("CLIVERIFY customact_kfrank 33228 团购记录(三层嵌套) firstBuyN=" + (records?[0].FirstBuy.Count ?? -1)
                    + " tailBuyN=" + (records?[0].TailBuy.Count ?? -1) + " ok=" + b33228);

                // ---- C. 33229 购买回执(成功落 Model+RewardList;失败仅 ShowError 不落地) ----
                int resultCount = 0; int lastResultCode = 0;
                System.Action<int, int, int> onResult = (b, s, code) => { resultCount++; lastResultCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                byte[] p33229ok = new CliVerify.Pkt().I(1).H(88).H(1).H(10).C(1).C(1).H(6)
                    .H(1).C(1).I(23020001).I(5)
                    .Bytes();
                Feed(ctrl, t, "OnKfGroupBuyResult", p33229ok);
                var buyResult = model.GetKfGroupBuyBuyResult(88, 1);
                bool b33229ok = resultCount == 1 && lastResultCode == 1 && buyResult != null && buyResult.GradeId == 10
                    && buyResult.BuyCount == 1 && buyResult.BuyNum == 6 && buyResult.RewardList.Count == 1
                    && buyResult.RewardList[0].GoodsId == 23020001 && buyResult.RewardList[0].Num == 5;
                byte[] p33229fail = new CliVerify.Pkt().I(1720001).H(88).H(1).H(99).C(2).C(0).H(0).H(0).Bytes();
                Feed(ctrl, t, "OnKfGroupBuyResult", p33229fail);
                var buyResultAfterFail = model.GetKfGroupBuyBuyResult(88, 1);
                bool b33229fail = resultCount == 2 && lastResultCode == 1720001 && buyResultAfterFail != null && buyResultAfterFail.GradeId == 10; // 失败不覆盖
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                bool b33229 = b33229ok && b33229fail;
                Debug.Log("CLIVERIFY customact_kfrank 33229 购买回执 okGrade=" + (buyResult?.GradeId ?? -1)
                    + " failNotOverwrite=" + b33229fail + " ok=" + b33229);

                // ---- D. 33230(recv-only 广播):原地更新已有档位 BuyNum;不存在活动/档位则忽略,不抛 ----
                byte[] p33230 = new CliVerify.Pkt().H(88).H(1).H(10).H(7).Bytes();
                Feed(ctrl, t, "OnKfGroupBuyCountPush", p33230);
                var infoAfterPush = model.GetKfGroupBuyInfo(88, 1);
                bool b33230hit = infoAfterPush != null && infoAfterPush.GpGoods[0].GradeId == 10 && infoAfterPush.GpGoods[0].BuyNum == 7
                    && infoAfterPush.GpGoods[1].BuyNum == 9; // 未命中档位不受影响
                byte[] p33230miss = new CliVerify.Pkt().H(999).H(9).H(1).H(1).Bytes(); // 未知活动,guard 拦下不抛
                Feed(ctrl, t, "OnKfGroupBuyCountPush", p33230miss);
                bool b33230 = b33230hit && !anyThrew;
                bool noSend33230 = t.GetMethod("RequestKfGroupBuyCountPush", PF) == null; // recv-only 严禁发送方法
                Debug.Log("CLIVERIFY customact_kfrank 33230 recv-only广播 buyNumAfter=" + (infoAfterPush?.GpGoods[0].BuyNum ?? -1)
                    + " noSendMethod=" + noSend33230 + " ok=" + (b33230 && noSend33230));

                // ---- E. 33267 喊话(成败两包,均更新 LastShoutTime,老端无 else 拦截) ----
                byte[] p33267ok = new CliVerify.Pkt().I(1).H(88).H(1).I(1700005000).Bytes();
                Feed(ctrl, t, "OnKfGroupBuyShout", p33267ok);
                bool b33267ok = model.GetKfGroupBuyInfo(88, 1)?.LastShoutTime == 1700005000;
                byte[] p33267fail = new CliVerify.Pkt().I(1720099).H(88).H(1).I(1700006000).Bytes();
                bool shout33267NoThrow = true;
                try { Feed(ctrl, t, "OnKfGroupBuyShout", p33267fail); }
                catch (System.Exception e) { shout33267NoThrow = false; Debug.LogError("CLIVERIFY customact_kfrank 33267 fail threw: " + e); }
                bool b33267fail = model.GetKfGroupBuyInfo(88, 1)?.LastShoutTime == 1700006000; // 失败也照写(老端无 else 拦截)
                bool b33267 = b33267ok && b33267fail && shout33267NoThrow;
                Debug.Log("CLIVERIFY customact_kfrank 33267 喊话(成败均写) okTime=" + b33267ok + " failAlsoWrites=" + b33267fail + " ok=" + b33267);

                // ==================== 消费/鲜花榜(224xx) ====================

                // ---- F. 22400(recv-only 错误码,阈值!=1,区别于33100/22500的!=1012)+ 无公开发送方法 ----
                bool errThrow1 = true, errThrow2 = true;
                try { Feed(ctrl, t, "OnCostRankError", new CliVerify.Pkt().I(1).Bytes()); } catch { errThrow1 = false; }
                try { Feed(ctrl, t, "OnCostRankError", new CliVerify.Pkt().I(5).Bytes()); } catch { errThrow2 = false; }
                bool noSend22400 = t.GetMethod("RequestCostRankError", PF) == null;
                bool b22400 = errThrow1 && errThrow2 && noSend22400;
                Debug.Log("CLIVERIFY customact_kfrank 22400 鲜花榜错误码(阈值!=1) noThrow=" + (errThrow1 && errThrow2) + " noSendMethod=" + noSend22400 + " ok=" + b22400);

                // ---- G. 22403 跨服鲜花榜(RankList 2条 + FigureList 1条,Figure 46字段) ----
                CliVerify.Pkt p22403 = new CliVerify.Pkt().I(371).H(1)
                    .I(5).I(999999).C(1).I(50000).H(20).I(888888888)
                    .H(2)
                        .L(9101).H(1).C(0).H(1).S("花魁甲").I(8888).I(1)
                        .L(9102).H(1).C(0).H(1).S("花魁乙").I(6666).I(2)
                    .H(1);
                p22403.L(9101);
                AppendFigure(p22403, "花魁甲", 180, 0, 0, "");
                Feed(ctrl, t, "OnFlowerRankInfo", p22403.Bytes());
                var flowerInfo = model.GetFlowerRankInfo();
                bool b22403 = flowerInfo != null && flowerInfo.Type == 371 && flowerInfo.SubType == 1 && flowerInfo.Sum == 50000
                    && flowerInfo.RankList.Count == 2 && flowerInfo.RankList[0].RoleId == 9101 && flowerInfo.RankList[0].Name == "花魁甲"
                    && flowerInfo.RankList[0].FirstValue == 8888 && flowerInfo.RankList[1].Rank == 2
                    && flowerInfo.FigureList.Count == 1 && flowerInfo.FigureList[0].RoleId == 9101
                    && flowerInfo.FigureList[0].Figure != null && flowerInfo.FigureList[0].Figure.name == "花魁甲"
                    && flowerInfo.FigureList[0].Figure.level == 180;
                Debug.Log("CLIVERIFY customact_kfrank 22403 跨服鲜花榜(双数组+Figure) rankN=" + (flowerInfo?.RankList.Count ?? -1)
                    + " figureName=" + (flowerInfo?.FigureList[0].Figure.name ?? "null") + " ok=" + b22403);

                // ---- H. 22405 消费榜 ----
                byte[] p22405 = new CliVerify.Pkt().I(1).H(2).H(1).I(500).I(3).I(88888).I(70000).H(20).I(999999)
                    .H(1)
                        .L(9201).S("消费王").I(120000).I(1)
                    .Bytes();
                Feed(ctrl, t, "OnCostRankExtra", p22405);
                var costInfo = model.GetCostRankInfo();
                bool b22405 = costInfo != null && costInfo.Code == 1 && costInfo.Type == 2 && costInfo.SubType == 1
                    && costInfo.RankType == 500 && costInfo.Sum == 70000 && costInfo.RankList.Count == 1
                    && costInfo.RankList[0].RoleId == 9201 && costInfo.RankList[0].Name == "消费王" && costInfo.RankList[0].FirstValue == 120000;
                Debug.Log("CLIVERIFY customact_kfrank 22405 消费榜 rankN=" + (costInfo?.RankList.Count ?? -1) + " ok=" + b22405);

                // ---- I. 请求方法存在且 no-throw(KFGROUPBUY买/喊话/鲜花榜/消费榜) ----
                bool sendMethodsExist = t.GetMethod("RequestKfGroupBuyInfo", PF) != null && t.GetMethod("RequestKfGroupBuyRecord", PF) != null
                    && t.GetMethod("RequestKfGroupBuyBuy", PF) != null && t.GetMethod("RequestKfGroupBuyShout", PF) != null
                    && t.GetMethod("RequestFlowerRank", PF) != null && t.GetMethod("RequestCostRank", PF) != null;
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.CustomActivity.CustomActivityController)ctrl;
                    c.RequestKfGroupBuyInfo(88, 1);
                    c.RequestKfGroupBuyRecord(88, 1);
                    c.RequestKfGroupBuyBuy(88, 1, 10, 1);
                    c.RequestKfGroupBuyShout(88, 1, 10);
                    c.RequestFlowerRank(371, 1);
                    c.RequestCostRank(2, 1);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY customact_kfrank send methods threw: " + e); }
                bool bSend = sendMethodsExist && sendNoThrow;
                Debug.Log("CLIVERIFY customact_kfrank Request* 存在且noThrow methodsExist=" + sendMethodsExist + " noThrow=" + sendNoThrow + " ok=" + bSend);

                bool bDetailEvents = detailUpdateCount == 6; // 33227+33228+33230(命中+未命中各发一次)+22403+22405
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                Debug.Log("CLIVERIFY customact_kfrank EVT_CUSTOMACT_DETAIL_UPDATE 累计触发次数=" + detailUpdateCount + " ok=" + bDetailEvents);

                // ==================== TopPlayer(225xx 补全) ====================

                // ---- J. 22500 通用错误码(阈值!=1012) ----
                bool topErrThrow1 = true, topErrThrow2 = true;
                try { Feed(topCtrl, tt, "On22500", new CliVerify.Pkt().I(1012).Bytes()); } catch { topErrThrow1 = false; }
                try { Feed(topCtrl, tt, "On22500", new CliVerify.Pkt().I(5).Bytes()); } catch { topErrThrow2 = false; }
                bool b22500 = topErrThrow1 && topErrThrow2;
                Debug.Log("CLIVERIFY customact_kfrank 22500 头号玩家错误码(阈值!=1012) noThrow=" + b22500 + " ok=" + b22500);

                // ---- K. 22503 领取目标奖励(仅成功刷新,失败静默无副作用;no-throw 双证) ----
                int topResultCount = 0; int topLastCode = 0;
                System.Action<int, int, int> onTopResult = (b, s, code) => { topResultCount++; topLastCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onTopResult);
                bool b22503NoThrow = true;
                try
                {
                    Feed(topCtrl, tt, "On22503", new CliVerify.Pkt().I(1).I(371).C(2).H(1).Bytes());
                    Feed(topCtrl, tt, "On22503", new CliVerify.Pkt().I(1720001).I(371).C(3).H(1).Bytes());
                }
                catch (System.Exception e) { b22503NoThrow = false; Debug.LogError("CLIVERIFY customact_kfrank 22503 threw: " + e); }
                bool b22503 = b22503NoThrow && topResultCount == 2 && topLastCode == 1720001;
                Debug.Log("CLIVERIFY customact_kfrank 22503 领目标奖(成功刷新/失败静默) events=" + topResultCount + " noThrow=" + b22503NoThrow + " ok=" + b22503);

                // ---- L. 22504 领取排名奖励(无论成败都落 ClaimResult,复用 CustomActivityModel API) ----
                Feed(topCtrl, tt, "On22504", new CliVerify.Pkt().I(1).C(5).H(1).I(371).Bytes());
                var claimOk = model.GetClaimResult(Shenxiao.Module.Core.CustomActivity.TopPlayerModel.ACT_BASE_TYPE, 1);
                bool b22504ok = claimOk != null && claimOk.Grade == 5 && claimOk.Code == 1;
                Feed(topCtrl, tt, "On22504", new CliVerify.Pkt().I(1720002).C(6).H(1).I(371).Bytes());
                var claimFail = model.GetClaimResult(Shenxiao.Module.Core.CustomActivity.TopPlayerModel.ACT_BASE_TYPE, 1);
                bool b22504fail = claimFail != null && claimFail.Grade == 6 && claimFail.Code == 1720002; // 无条件覆盖(对标老端无条件 SetActResult)
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onTopResult);
                bool b22504 = b22504ok && b22504fail && topResultCount == 4; // 22504 也发 EVT_CUSTOMACT_RESULT,累加到 4
                Debug.Log("CLIVERIFY customact_kfrank 22504 领排名奖(无条件落地) okGrade=" + (claimOk?.Grade ?? -1)
                    + " failGrade=" + (claimFail?.Grade ?? -1) + " ok=" + b22504);

                // ---- M. 22505 获取途径(落本控制器私有缓存) ----
                byte[] p22505 = new CliVerify.Pkt().I(500).H(1).I(10086).I(2).L(1700099999).Bytes();
                Feed(topCtrl, tt, "On22505", p22505);
                var topPlayerCtrl = (Shenxiao.Module.Core.CustomActivity.TopPlayerController)topCtrl;
                var getWay = topPlayerCtrl.GetGetWay(500);
                bool b22505 = getWay != null && getWay.Count == 1 && getWay[0].JumpId == 10086 && getWay[0].Label == 2 && getWay[0].EndTime == 1700099999;
                Debug.Log("CLIVERIFY customact_kfrank 22505 获取途径 resN=" + (getWay?.Count ?? -1) + " ok=" + b22505);

                // ---- N. TopPlayer 公开发送方法存在且 no-throw ----
                bool topSendExist = tt.GetMethod("RequestGoalClaim", PF) != null && tt.GetMethod("RequestRankClaim", PF) != null
                    && tt.GetMethod("RequestGetWay", PF) != null;
                bool topSendNoThrow = true;
                try
                {
                    topPlayerCtrl.RequestGoalClaim(371, 2);
                    topPlayerCtrl.RequestRankClaim(371, 5);
                    topPlayerCtrl.RequestGetWay(500);
                }
                catch (System.Exception e) { topSendNoThrow = false; Debug.LogError("CLIVERIFY customact_kfrank TopPlayer send threw: " + e); }
                bool bTopSend = topSendExist && topSendNoThrow;
                Debug.Log("CLIVERIFY customact_kfrank TopPlayer Request* 存在且noThrow methodsExist=" + topSendExist + " noThrow=" + topSendNoThrow + " ok=" + bTopSend);

                bool pass = !anyThrew && bRegistered
                    && b33227 && b33228 && b33229 && b33230 && b33267
                    && b22400 && b22403 && b22405 && bSend && bDetailEvents
                    && b22500 && b22503 && b22504 && b22505 && bTopSend;

                Debug.Log("CLIVERIFY customact_kfrank VERDICT registered=" + bRegistered
                    + " kf227=" + b33227 + " kf228=" + b33228 + " kf229=" + b33229 + " kf230=" + b33230 + " kf267=" + b33267
                    + " c22400=" + b22400 + " c22403=" + b22403 + " c22405=" + b22405 + " kfSend=" + bSend
                    + " tp22500=" + b22500 + " tp22503=" + b22503 + " tp22504=" + b22504 + " tp22505=" + b22505 + " tpSend=" + bTopSend
                    + " anyThrew=" + anyThrew + " pass=" + pass);

                model.Clear();
                model.ClearKf();
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }

        /// <summary>按 FigureProto.SCHEMA 逐字段序写入(46 字段,5 个嵌套数组固定写 0 条;name/level 等可覆盖
        /// 供断言探针)。与 MarriageCase.AppendFigure 同款,复制自那里(两个 Case 各自独立、不跨文件共享私有方法)。</summary>
        private static CliVerify.Pkt AppendFigure(CliVerify.Pkt p, string name, int level, int isMarriage, long marriageId, string marriageName) => p
            .S(name)          // name
            .C(0)             // sex
            .C(0)             // realm
            .C(0)             // career
            .H(level)         // level
            .C(0)             // GM
            .C(0)             // vip_flag
            .C(0)             // is_hide_vip
            .C(0)             // touxian
            .H(0)             // level_model_list count
            .H(0)             // fashion_model_list count
            .S("")            // picture
            .I(0)             // prcture_ver
            .L(0)             // guild_id
            .S("")            // guild_name
            .C(0)             // position
            .S("")            // position_name
            .I(0)             // dsgt_id
            .I(0)             // liveness_id
            .C(0)             // turn
            .C(0)             // turn_stage
            .C(0)             // grade_id
            .C(isMarriage)    // is_marriage
            .L(marriageId)    // marriage_id
            .S(marriageName)  // marriage_name
            .I(0)             // escort_state
            .I(0)             // block_id
            .I(0)             // house_id
            .H(0)             // house_lv
            .H(0)             // figure_list count
            .H(0)             // figure_ride_list count
            .H(0)             // achv_lv
            .H(0)             // medal_id
            .I(0)             // fazhen_id
            .H(0)             // dress_list count
            .I(0)             // god_id
            .I(0)             // revelation_suit
            .I(0)             // demon_id
            .C(0)             // supreme_vip
            .I(0)             // title_id
            .C(0)             // mask_id
            .C(0)             // seaCamp
            .C(0)             // brick_id
            .C(0)             // dummy_type
            .C(0)             // suit_fashion_id
            .C(0);            // collect_state
    }
}
