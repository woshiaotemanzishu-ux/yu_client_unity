using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 定制活动框架核心(自动循环 轮17 P1)实证:pt_331 33100-33108(33101 在既有 CustomActivityController.cs
    /// 主文件升级,本轮追加落 Model+Emit LIST_UPDATE+对 TIRED_CHARGE_POLITE(121)追发 33259)。合成包驱动
    /// CustomActivityController 反射喂包,断言 CustomActivityModel 落地字段/事件(模板 MarriageCase,纯逻辑段)。
    ///
    /// 覆盖:①33101 列表落地(含一条 TIRED_CHARGE_POLITE=121 条目,证明追发分支不炸游标);②33102 增量新开
    /// 合并进 Model;③33103 增量关闭从 Model 移除;④33104 单活动详情(8字段 item_to_bin_3 结构,非早期侦察表
    /// 误记的2字段);⑤33105 成败两包(成功落 Model+发事件,失败仅发事件不落 Model,不抛异常);⑥33106 全服计数
    /// 四元键落地;⑦33108 触发转发→RequestActDetail 对已知活动会级联发送(no-throw + 日志计数),对未知活动
    /// (guard `act_info` 为空)不发送任何协议、同样 no-throw。
    ///
    /// P2-P6 空壳(.LotteryA/.LotteryB/.Festival/.Biz/.Kf.cs 与对应 Model/Case)不在本 Case 覆盖范围——各自
    /// 包代理落地后已补齐断言。本文件与其余 5 个 CustomAct*Case 均已由主控收口挂钩进 CliVerify.cs。
    /// </summary>
    public static class CustomActCoreCase
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

                object ctrl = Shenxiao.Module.Core.CustomActivity.CustomActivityController.Instance;
                System.Type t = ctrl.GetType();
                bool anyThrew = false;
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY customact_core handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY customact_core " + method + " threw: " + e);
                    }
                }

                // ---- 0. 注册线核实:Init() 后 33100/02/03/04/05/06/08 必须真的挂进 NetManager(不仅仅是
                // 反射能调到方法体)——15b/16 血训"接手半成品必补验收"追加的一类回归:曾漏过给新增 handler
                // 补 RegisterProtocal,反射喂包能通过但真实网络包永远派发不到。直接反射 NetManager 私有
                // _handlers 字典核对。 ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACTIVITY_LIST, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ERROR,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ADD, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REMOVE,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_DETAIL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_CLAIM,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_ALLCOUNT, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_REFRESH,
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
                Debug.Log("CLIVERIFY customact_core 注册线核实(NetManager._handlers) missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                // ---- A0. 33100 通用错误码(对标老端 On33100:926-931,仅 code!=1012 显错):code=1012 静默一支,
                // 其它任意错误码一支,均断言 EVT_CUSTOMACT_ERROR 带对应 code 触发且不抛异常。 ----
                int errorEventCount = 0; int lastErrorCode = 0;
                System.Action<int> onError = (code) => { errorEventCount++; lastErrorCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_ERROR, onError);
                Feed("On33100", new CliVerify.Pkt().I(1012).Bytes());
                bool b33100silent = errorEventCount == 1 && lastErrorCode == 1012 && !anyThrew;
                Feed("On33100", new CliVerify.Pkt().I(1720000).Bytes());
                bool b33100show = errorEventCount == 2 && lastErrorCode == 1720000 && !anyThrew;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_ERROR, onError);
                bool b33100 = b33100silent && b33100show;
                Debug.Log("CLIVERIFY customact_core 33100 通用错误码(code=1012静默/其它弹码,均发EVT_CUSTOMACT_ERROR) events=" + errorEventCount + " ok=" + b33100);

                // ---- A. 33101 列表落地(升级后:落 Model + Emit LIST_UPDATE),含 TIRED_CHARGE_POLITE=121 条目 ----
                int listUpdateCount = 0;
                System.Action onListUpdate = () => listUpdateCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_UPDATE, onListUpdate);
                byte[] p33101 = new CliVerify.Pkt().H(2)
                    .H(200).H(1).C(1).H(5).H(10).S("测试活动A").S("descA").S("condA").I(1700000000).I(1700003600)
                    .H(121).H(7).C(2).H(8).H(20).S("累充有礼").S("descB").S("condB").I(1700000100).I(1700099999)
                    .Bytes();
                Feed("On33101", p33101);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_UPDATE, onListUpdate);
                var e200 = model.GetActEntry(200, 1);
                var e121 = model.GetActEntry(121, 7);
                bool b33101 = listUpdateCount == 1 && e200 != null && e200.Name == "测试活动A" && e200.Stime == 1700000000
                    && e121 != null && e121.Name == "累充有礼"; // 121 条目落地即证明追发 33259 分支未打断解析循环
                Debug.Log("CLIVERIFY customact_core 33101 列表落地 listUpdate=" + listUpdateCount + " e200=" + (e200 != null)
                    + " e121(TIRED_CHARGE_POLITE)=" + (e121 != null) + " ok=" + b33101);

                // ---- B. 33102 增量新开合并进 Model ----
                int listAddCount = 0;
                System.Action onListAdd = () => listAddCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_ADD, onListAdd);
                byte[] p33102 = new CliVerify.Pkt().H(1)
                    .H(201).H(2).C(1).H(6).H(11).S("测试活动B").S("descC").S("condC").I(1700000200).I(1700004600)
                    .Bytes();
                Feed("On33102", p33102);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_ADD, onListAdd);
                var e201 = model.GetActEntry(201, 2);
                bool b33102 = listAddCount == 1 && e201 != null && e201.Name == "测试活动B" && e201.Etime == 1700004600;
                Debug.Log("CLIVERIFY customact_core 33102 增量新开 listAdd=" + listAddCount + " e201=" + (e201 != null) + " ok=" + b33102);

                // ---- C. 33103 增量关闭从 Model 移除(删掉刚加的 201/2) ----
                int listRemoveCount = 0;
                System.Action onListRemove = () => listRemoveCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE, onListRemove);
                byte[] p33103 = new CliVerify.Pkt().H(1).H(201).H(2).Bytes();
                Feed("On33103", p33103);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE, onListRemove);
                bool b33103 = listRemoveCount == 1 && model.GetActEntry(201, 2) == null && model.GetActEntry(200, 1) != null; // 200/1 不受影响
                Debug.Log("CLIVERIFY customact_core 33103 增量关闭 listRemove=" + listRemoveCount + " removed=" + (model.GetActEntry(201, 2) == null) + " ok=" + b33103);

                // ---- D. 33104 单活动详情(8字段 item_to_bin_3,订正后的正确结构) ----
                int detailUpdateBase = -1, detailUpdateSub = -1, detailUpdateCount = 0;
                System.Action<int, int> onDetail = (b, s) => { detailUpdateCount++; detailUpdateBase = b; detailUpdateSub = s; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                byte[] p33104 = new CliVerify.Pkt().H(200).H(1).H(2)
                    .H(1).C(0).C(1).H(3).S("档位1名").S("档位1描述").S("档位1条件").S("档位1奖励")
                    .H(2).C(1).C(0).H(0).S("档位2名").S("档位2描述").S("档位2条件").S("档位2奖励")
                    .Bytes();
                Feed("On33104", p33104);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                var detail = model.GetDetail(200, 1);
                bool b33104 = detailUpdateCount == 1 && detailUpdateBase == 200 && detailUpdateSub == 1
                    && detail != null && detail.RewardList.Count == 2
                    && detail.RewardList[0].Grade == 1 && detail.RewardList[0].Status == 1 && detail.RewardList[0].ReceiveTimes == 3
                    && detail.RewardList[0].Name == "档位1名" && detail.RewardList[0].Reward == "档位1奖励"
                    && detail.RewardList[1].Grade == 2 && detail.RewardList[1].FormType == 1;
                Debug.Log("CLIVERIFY customact_core 33104 单活动详情(8字段订正) rewardN=" + (detail?.RewardList.Count ?? -1) + " ok=" + b33104);

                // ---- D2. 33104 base_type==120(超值礼包)数据变异镜像(自动循环 轮17三镜头验收补,ts:1010-1029):
                // 非 {is_free,1} 档 Status 强改为 2,is_free 档保留原 Status。两档初始 Status 都喂 1,
                // 断言变异后 is_free 档仍是 1、非 is_free 档被改成 2。 ----
                byte[] p33104_120 = new CliVerify.Pkt().H(120).H(1).H(2)
                    .H(1).C(0).C(1).H(0).S("免费档").S("d").S("[{is_free,1}]").S("r")
                    .H(2).C(0).C(1).H(0).S("付费档").S("d").S("[{other,2}]").S("r")
                    .Bytes();
                Feed("On33104", p33104_120);
                var detail120 = model.GetDetail(120, 1);
                bool b33104_120 = detail120 != null && detail120.RewardList.Count == 2
                    && detail120.RewardList[0].Status == 1  // is_free 档保留原 Status
                    && detail120.RewardList[1].Status == 2; // 非 is_free 档被强改为 2
                Debug.Log("CLIVERIFY customact_core 33104 base_type==120数据变异(非is_free档Status改2) freeStatus="
                    + (detail120?.RewardList[0].Status ?? -1) + " nonFreeStatus=" + (detail120?.RewardList[1].Status ?? -1) + " ok=" + b33104_120);

                // ---- E. 33105 成败两包(成功落 Model+发事件;失败仅发事件不落 Model,不抛) ----
                int resultCount = 0; int lastResultCode = 0;
                System.Action<int, int, int> onResult = (b, s, code) => { resultCount++; lastResultCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                byte[] p33105ok = new CliVerify.Pkt().I(1).H(200).H(1).H(3).Bytes();
                Feed("On33105", p33105ok);
                var claimAfterOk = model.GetClaimResult(200, 1);
                bool b33105ok = resultCount == 1 && lastResultCode == 1 && claimAfterOk != null && claimAfterOk.Grade == 3;
                byte[] p33105fail = new CliVerify.Pkt().I(1720001).H(200).H(1).H(9).Bytes();
                Feed("On33105", p33105fail);
                var claimAfterFail = model.GetClaimResult(200, 1);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_RESULT, onResult);
                // 失败分支不应覆盖 Model(仍是成功那次的 Grade=3),但仍应发事件(code=失败码)。
                bool b33105fail = resultCount == 2 && lastResultCode == 1720001 && claimAfterFail != null && claimAfterFail.Grade == 3;
                bool b33105 = b33105ok && b33105fail;
                Debug.Log("CLIVERIFY customact_core 33105 成败两包 okGrade=" + (claimAfterOk?.Grade ?? -1)
                    + " failNotOverwrite=" + (claimAfterFail?.Grade == 3) + " events=" + resultCount + " ok=" + b33105);

                // ---- F. 33106 全服计数(四元键) ----
                int allCountUpdateN = 0;
                System.Action<int, int> onAllCount = (b, s) => allCountUpdateN++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_ALLCOUNT_UPDATE, onAllCount);
                byte[] p33106 = new CliVerify.Pkt().H(200).H(1).H(5).H(9).H(42).H(2).Bytes();
                Feed("On33106", p33106);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_CUSTOMACT_ALLCOUNT_UPDATE, onAllCount);
                var allCount = model.GetAllCount(200, 1, 5, 9);
                bool b33106 = allCountUpdateN == 1 && allCount != null && allCount.Count == 42 && allCount.Grade == 2;
                Debug.Log("CLIVERIFY customact_core 33106 全服计数 count=" + (allCount?.Count ?? -1) + " grade=" + (allCount?.Grade ?? -1) + " ok=" + b33106);

                // ---- G. 33108 触发转发→RequestActDetail(200/1 已在 Model 中→级联发送;999/9 未知活动→guard 拦下不发,均 no-throw) ----
                logs.Clear();
                byte[] p33108 = new CliVerify.Pkt().H(2).H(200).H(1).H(999).H(9).Bytes();
                Feed("On33108", p33108);
                bool b33108log = logs.Exists(s => s.Contains("33108 活动刷新指令 count=2"));
                bool b33108 = !anyThrew && b33108log;
                Debug.Log("CLIVERIFY customact_core 33108 触发转发(已知+未知活动混合) noThrow=" + (!anyThrew) + " logHit=" + b33108log + " ok=" + b33108);

                // ---- H. RequestClaim/RequestAllCount 公开发送方法存在且不抛(反射校验 + 直接调用) ----
                bool sendMethodsExist = t.GetMethod("RequestClaim", PF) != null && t.GetMethod("RequestAllCount", PF) != null
                    && t.GetMethod("RequestActDetail", PF) != null;
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.CustomActivity.CustomActivityController)ctrl;
                    c.RequestClaim(200, 1, 3);
                    c.RequestAllCount(200, 1, 5, 9, 2);
                    c.RequestActDetail(200, 1);
                    c.RequestActDetail(999, 9); // 未知活动,guard 拦下
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY customact_core send methods threw: " + e); }
                bool bSend = sendMethodsExist && sendNoThrow;
                Debug.Log("CLIVERIFY customact_core RequestClaim/AllCount/ActDetail 存在且noThrow methodsExist=" + sendMethodsExist + " noThrow=" + sendNoThrow + " ok=" + bSend);

                // ---- I. "见到即拉"特判(自动循环 轮17三镜头验收补,镜像 SaveActInfo/AddActInfo 的
                // switch-case,Model.ts:378-385/450-453):On33101 全量清单 89/108/113/114/70 命中即调
                // RequestActDetail;On33102 增量清单只有 89/113/70 三个(与全量版不对称,见 Core.cs
                // RequestSeeOnArrivalDetailsIncremental 注释)。该分支只发协议不改 Model,用"无异常+条目已
                // 正常入库"作为可观测信号,不 mock 网络。放在本 Case 末尾:On33101 是全量列表替换
                // (SaveActList 先 Clear 再重建),放前面会冲掉 A/C/G 几段依赖的 200/1 等既有条目。 ----
                byte[] p33101SeeOnArrival = new CliVerify.Pkt().H(2)
                    .H(89).H(1).C(1).H(1).H(0).S("摇钱树商店").S("d").S("c").I(1700000000).I(1700900000)
                    .H(108).H(1).C(1).H(1).H(0).S("等级弹窗奖励").S("d").S("c").I(1700000000).I(1700900000)
                    .Bytes();
                Feed("On33101", p33101SeeOnArrival);
                bool bSeeArrivalFull = !anyThrew && model.GetActEntry(89, 1) != null && model.GetActEntry(108, 1) != null;
                Debug.Log("CLIVERIFY customact_core 见到即拉-全量版(On33101,89/108命中) noThrow=" + !anyThrew + " ok=" + bSeeArrivalFull);

                byte[] p33102SeeOnArrival = new CliVerify.Pkt().H(1)
                    .H(70).H(1).C(1).H(1).H(0).S("关注").S("d").S("c").I(1700000000).I(1700900000)
                    .Bytes();
                Feed("On33102", p33102SeeOnArrival);
                bool bSeeArrivalIncremental = !anyThrew && model.GetActEntry(70, 1) != null;
                Debug.Log("CLIVERIFY customact_core 见到即拉-增量版(On33102,70命中) noThrow=" + !anyThrew + " ok=" + bSeeArrivalIncremental);

                bool pass = !anyThrew && bRegistered && b33100 && b33101 && b33102 && bSeeArrivalFull && bSeeArrivalIncremental
                    && b33103 && b33104 && b33104_120 && b33105 && b33106 && b33108 && bSend;

                Debug.Log("CLIVERIFY customact_core VERDICT registered=" + bRegistered + " l33100=" + b33100 + " l33101=" + b33101 + " l33102=" + b33102
                    + " seeArrivalFull=" + bSeeArrivalFull + " seeArrivalIncremental=" + bSeeArrivalIncremental
                    + " l33103=" + b33103 + " l33104=" + b33104 + " l33104_120=" + b33104_120 + " l33105=" + b33105 + " l33106=" + b33106 + " l33108=" + b33108
                    + " sendApi=" + bSend + " anyThrew=" + anyThrew + " pass=" + pass);

                model.Clear();
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
