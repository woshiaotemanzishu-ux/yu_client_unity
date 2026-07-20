using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 婚宴数据层(自动循环 轮24 PB)实证:pt_172 172xx 扩壳(17249/17256 既有图标壳 + 新增 22 个活号
    /// 17250/51/52/53/57/58/59/60/61/62/65/66/67/70/71/72/75/76/77/78/79/98),合成包驱动
    /// BanquetController 反射喂包,断言 BanquetModel 落地字段/事件 + config 13 表计数(模板 MarriageCase,
    /// 纯逻辑段,无婚礼场景无渲染)。
    ///
    /// 重点覆盖:17250 三层嵌套(my_wedding_times/day_list/time_list/order_list)+ CanApply 判定
    /// (NowWeddingState==0 时按 use_times&lt;max_times 计算);17252 大包嵌套(guest_list/ask_invite_list,
    /// 两者形状不同——guest_list 三字段/ask_invite_list 两字段)+ **尾哨兵字节游标核对**;17260 双 type
    /// 分流(type==1→AskData 且"比上次更多才算新申请";type==2→GuestList,与 17252 共用顶层桶)+ 连续两次
    /// 调用验证 NewApply true→false 翻转;17262 FigureProto 全字段(pt:write_figure);17265/17272/17275/
    /// 17277/17278/17279 多个"部分号无 Code 前缀"独例;17267 config_wedding_fires 未载时整段跳过门禁;
    /// 17271 type==1 桌菜链式重发 vs 其余 type 不重发;复发链 17251→17249+17250、17253/17259/17261→17252、
    /// 17266/17267→17272、17276→17249+17250(无 read 纯推送触发)、17298→17252+17260(TypeList=[2])均
    /// 断言"调用不抛"(NetManager 未连接下的安全性)。
    /// 死号断言:17254/17255/17269/17273/17274/17280-17294 全部无 On&lt;n&gt; 反射方法(killlist,严禁注册);
    /// 17263/17264 归属 MarriageController,本控制器只提供发送 API、不重复注册接收处理器。Init 实测注册表
    /// 必须精确等于 17249/17256 + 本轮 22 个活号；17263/17264/17273 均不得出现。
    /// 关键 C2S 通过控制器 Build*Payload 同源纯函数 + UserMsgAdapter.Encode 锁定真实出站字节：
    /// 17263=l(role_id_m)、17264=空包、17266=c(candies_type=1/2)，并验证物品 id 会被 API 拒绝。
    /// batch 域加载前临时启用 ResManager.EditorPreferFallback，避免 Addressables async 不推进导致死锁；
    /// 但 fallback 只负责读表，另同步枚举 AddressableAssetSettings，13 个 wedding key 必须全部正式登记。
    ///
    /// UI/场景层:婚礼场景(WeddingScene)Unity 无对应地图资源,本轮数据层only不接 View/场景,
    /// 消费方留 UI/场景轮(同 15a/15b Boss 先例)。
    /// </summary>
    public static class BanquetCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PF = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Shenxiao.Module.Core.Banquet.BanquetController controller = Shenxiao.Module.Core.Banquet.BanquetController.Instance;
            bool initializedByCase = false;
            bool editorPreferFallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                // batch 域 Addressables async 不推进；先走 AssetDatabase 同步兜底，finally 恢复调用前状态。
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                bool addressablesOk = ValidateWeddingAddressables(out string missingAddressables);
                Debug.Log("CLIVERIFY banquet Addressables registered=13/13:" + addressablesOk
                    + (addressablesOk ? "" : " missing=[" + missingAddressables + "]"));
                await Shenxiao.Module.Core.Banquet.BanquetConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Banquet.BanquetConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY banquet FAIL BanquetConfigs not loaded");
                    return 3;
                }

                bool configOk = Shenxiao.Module.Core.Banquet.BanquetConfigs.InfoCount == 3
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.TimeCount == 12
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.TimeStageCount == 3
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.CandiesCount == 2
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.FiresCount == 2
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.TableCount == 3
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.AuraCount == 1
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.GuestPositionCount == 41
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.PositionCount == 696
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.SceneExpCoefCount == 27
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.CardCount == 0       // 空表(死链佐证)
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.SceneExpCount == 0   // 空表
                    && Shenxiao.Module.Core.Banquet.BanquetConfigs.TroubleMakerCount == 0; // 空表(killlist佐证)
                Shenxiao.Module.Core.Banquet.BanquetConfigs.CandyRow candy = Shenxiao.Module.Core.Banquet.BanquetConfigs.GetCandy(8002003);
                Shenxiao.Module.Core.Banquet.BanquetConfigs.FiresRow fires = Shenxiao.Module.Core.Banquet.BanquetConfigs.GetFires(1);
                bool configRowOk = candy != null && candy.LimitNum == 20 && candy.Aura == 40
                    && fires != null && fires.Charact == "uifx_giftlv8";
                Debug.Log("CLIVERIFY banquet config info=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.InfoCount
                    + " time=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.TimeCount
                    + " candies=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.CandiesCount
                    + " fires=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.FiresCount
                    + " guestPos=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.GuestPositionCount
                    + " pos=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.PositionCount
                    + " sceneExpCoef=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.SceneExpCoefCount
                    + " card(空)=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.CardCount
                    + " troubleMaker(空)=" + Shenxiao.Module.Core.Banquet.BanquetConfigs.TroubleMakerCount
                    + " ok=" + configOk + " rowOk=" + configRowOk);

                Shenxiao.Module.Core.Banquet.BanquetModel model = Shenxiao.Module.Core.Banquet.BanquetModel.Instance;
                model.Reset();

                object ctrl = controller;
                System.Type t = ctrl.GetType();

                bool initializedBeforeCase = controller.IsInitialized;
                controller.Init();
                initializedByCase = !initializedBeforeCase;
                controller.Init(); // 幂等探针：第二次 Init 不得重复登记。
                FieldInfo registeredField = typeof(Shenxiao.Framework.Net.BaseController)
                    .GetField("_registered", BindingFlags.NonPublic | BindingFlags.Instance);
                var registered = registeredField?.GetValue(controller) as List<int>;
                int[] expectedRegistered =
                {
                    17249, 17256, 17250, 17251, 17252, 17253, 17257, 17258, 17259, 17260, 17261, 17262,
                    17265, 17266, 17267, 17270, 17271, 17272, 17275, 17276, 17277, 17278, 17279, 17298,
                };
                var uniqueRegistered = new HashSet<int>();
                if (registered != null)
                {
                    foreach (int protoId in registered) uniqueRegistered.Add(protoId);
                }
                bool initRegistrationOk = registered != null
                    && registered.Count == expectedRegistered.Length
                    && uniqueRegistered.Count == expectedRegistered.Length
                    && !uniqueRegistered.Contains(17263) && !uniqueRegistered.Contains(17264)
                    && !uniqueRegistered.Contains(17273);
                foreach (int protoId in expectedRegistered)
                {
                    if (!uniqueRegistered.Contains(protoId)) initRegistrationOk = false;
                }
                Debug.Log("CLIVERIFY banquet Init注册 count=" + (registered?.Count ?? -1)
                    + " expected=" + expectedRegistered.Length + " exact/idempotent=" + initRegistrationOk);

                bool outboundEncodeOk = RunOutboundEncodingAssertions(controller, logs);
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY banquet handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }
                Shenxiao.Framework.Net.NetReader FeedReader(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    var reader = new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);
                    if (m == null) { Debug.LogError("CLIVERIFY banquet handler missing: " + method); return reader; }
                    m.Invoke(ctrl, new object[] { reader });
                    return reader;
                }
                bool NoThrow(string method, byte[] pkt)
                {
                    try { Feed(method, pkt); return true; }
                    catch (System.Exception e) { Debug.LogError("CLIVERIFY banquet " + method + " threw: " + e); return false; }
                }

                // ---- A. 死号断言(killlist:17254/17255/17269/17273/17274/17280-17294 全部无 On<n> 反射方法) ----
                var deadNums = new[] { 17254, 17255, 17269, 17273, 17274, 17280, 17281, 17282, 17283, 17284, 17285,
                    17286, 17287, 17288, 17289, 17290, 17291, 17292, 17293, 17294 };
                bool allDead = true;
                foreach (int n in deadNums)
                {
                    if (t.GetMethod("On" + n, F) != null) { allDead = false; Debug.LogError("CLIVERIFY banquet killlist违规: On" + n + " 不应存在"); }
                }
                // 17263/17264 归 MarriageController,本控制器不得重复注册(无双注册)。
                bool noDoubleReg = t.GetMethod("On17263", F) == null && t.GetMethod("On17264", F) == null;
                Debug.Log("CLIVERIFY banquet killlist allDead=" + allDead + " noDoubleReg17263/64=" + noDoubleReg);

                // ---- B. 17250 预约/报名视图数据(三层嵌套 + CanApply 判定,NowWeddingState==0) ----
                byte[] p17250 = new CliVerify.Pkt().I(1).C(0)
                    .H(1).C(1).H(2).H(5).C(0)   // my_wedding_times[1]:{type=1,use=2,max=5,orderToday=0}
                    .H(1).I(1700000000)         // day_list[1]:order_unix_date
                        .H(1).C(3)                  // time_list[1]:time_id=3
                            .H(1).L(1001).L(1002).C(1).C(1) // order_list[1]:{roleIdM,roleIdW,weddingType,ifOwn}
                    .Bytes();
                Feed("On17250", p17250);
                bool b17250 = model.ApplyView != null && model.ApplyView.NowWeddingState == 0
                    && model.ApplyView.MyWeddingTimes.Count == 1 && model.ApplyView.MyWeddingTimes[0].UseTimes == 2
                    && model.ApplyView.MyWeddingTimes[0].MaxTimes == 5
                    && model.ApplyView.DayList.Count == 1 && model.ApplyView.DayList[0].OrderUnixDate == 1700000000
                    && model.ApplyView.DayList[0].TimeList.Count == 1 && model.ApplyView.DayList[0].TimeList[0].TimeId == 3
                    && model.ApplyView.DayList[0].TimeList[0].OrderList.Count == 1
                    && model.ApplyView.DayList[0].TimeList[0].OrderList[0].RoleIdM == 1001
                    && model.CanApply; // use_times(2) < max_times(5) 且 wedding_type!=3 → true
                Debug.Log("CLIVERIFY banquet 17250 三层嵌套 canApply=" + model.CanApply + " ok=" + b17250);

                // ---- C. 17251 预约婚礼(成功码1/1720034 均触发链,man/woman_list 老端也不消费仅保游标) ----
                byte[] p17251 = new CliVerify.Pkt().I(1).I(1700000100).C(1).H(0).H(0).Bytes();
                bool b17251NoThrow = NoThrow("On17251", p17251);
                byte[] p17251b = new CliVerify.Pkt().I(1720034).I(1700000200).C(1).H(0).H(0).Bytes();
                bool b17251bNoThrow = NoThrow("On17251", p17251b);
                Debug.Log("CLIVERIFY banquet 17251 预约婚礼(code1/1720034链)noThrow=" + (b17251NoThrow && b17251bNoThrow));

                // ---- D. 17252 邀请视图数据(大包嵌套 guest_list/ask_invite_list + 尾哨兵) ----
                byte[] p17252 = new CliVerify.Pkt().I(1)
                    .L(2001).S("我").S("pic1").I(100)
                    .L(3001).S("她").S("pic2").I(200)
                    .C(1).I(1700000200).C(1).C(5).C(3)
                    .H(1).L(4001).C(1).S("宾客甲")   // guest_list[1]:{role_id,answer_type,name}
                    .H(1).L(5001).S("索要甲")         // ask_invite_list[1]:{role_id,name}(**无answer_type**)
                    .I(88888888)                       // 尾哨兵
                    .Bytes();
                var reader17252 = FeedReader("On17252", p17252);
                bool b17252Fields = model.InviteView != null && model.InviteView.MyRoleId == 2001 && model.InviteView.MyName == "我"
                    && model.InviteView.LoverRoleId == 3001 && model.InviteView.LoverName == "她"
                    && model.GuestList.Count == 1 && model.GuestList[0].RoleId == 4001 && model.GuestList[0].AnswerType == 1 && model.GuestList[0].Name == "宾客甲"
                    && model.HasAskData && model.AskData.Count == 1 && model.AskData[0].RoleId == 5001 && model.AskData[0].Name == "索要甲" && model.AskData[0].AnswerType == -1
                    && model.NewApply;
                bool b17252Sentinel = reader17252.Remaining == 4 && reader17252.ReadU32() == 88888888;
                bool b17252 = b17252Fields && b17252Sentinel;
                Debug.Log("CLIVERIFY banquet 17252 大包嵌套(guest/ask形状不同) guestName=" + (model.GuestList.Count > 0 ? model.GuestList[0].Name : "?")
                    + " askAnswerType=" + (model.AskData != null && model.AskData.Count > 0 ? model.AskData[0].AnswerType.ToString() : "?")
                    + " sentinel=" + (b17252Sentinel ? "4(exact)" : "MISMATCH") + " ok=" + b17252);

                // ---- E. 17253 邀请宾客(成功码1/1720033 均重发17252) ----
                byte[] p17253 = new CliVerify.Pkt().I(1).H(1).L(6001).Bytes();
                bool b17253NoThrow = NoThrow("On17253", p17253);
                Debug.Log("CLIVERIFY banquet 17253 邀请宾客 noThrow=" + b17253NoThrow);

                // ---- F. 17257 索要请柬 / 17258 购买请柬 ----
                bool b17257NoThrow = NoThrow("On17257", new CliVerify.Pkt().I(1).Bytes());
                bool b17258NoThrow = NoThrow("On17258", new CliVerify.Pkt().I(1).L(7001).Bytes());
                bool b17257_58NoThrow = b17257NoThrow && b17258NoThrow;
                Debug.Log("CLIVERIFY banquet 17257/17258 索要/购买请柬 noThrow=" + b17257_58NoThrow);

                // ---- G. 17259 购买邀请名额上限(成功重发17252;1720036 特例不显码) ----
                bool b17259aNoThrow = NoThrow("On17259", new CliVerify.Pkt().I(1).C(4).C(6).Bytes());
                bool b17259bNoThrow = NoThrow("On17259", new CliVerify.Pkt().I(1720036).C(0).C(0).Bytes());
                Debug.Log("CLIVERIFY banquet 17259 购买邀请名额上限 noThrow=" + (b17259aNoThrow && b17259bNoThrow));

                // ---- H. 17260 打开索要/邀请列表(双type分流 + 连续两次验证NewApply true→false翻转) ----
                model.AskData = null; // 复位:排除步骤D(17252)已写入的AskData干扰,确保本步"首次收到"语义干净
                byte[] p17260a = new CliVerify.Pkt().I(1).C(2)
                    .H(2)
                        .C(1).H(2).L(8001).C(0).S("甲").L(8002).C(0).S("乙") // type=1: 2条(首次收到,应判定为"新")
                        .C(2).H(1).L(9001).C(1).S("丙")                        // type=2: 1条
                    .Bytes();
                Feed("On17260", p17260a);
                bool b17260a = model.LessInviteNum == 2 && model.AskData.Count == 2 && model.NewApply
                    && model.GuestList.Count == 1 && model.GuestList[0].RoleId == 9001 && model.GuestList[0].AnswerType == 1 && model.GuestList[0].Name == "丙";
                byte[] p17260b = new CliVerify.Pkt().I(1).C(2)
                    .H(1)
                        .C(1).H(1).L(8001).C(0).S("甲") // type=1: 1条(比上次2条少,不应算"新")
                    .Bytes();
                Feed("On17260", p17260b);
                bool b17260b = model.AskData.Count == 1 && !model.NewApply;
                bool b17260 = b17260a && b17260b;
                Debug.Log("CLIVERIFY banquet 17260 双type分流 firstNewApply=" + b17260a + " secondNewApplyFlip=" + b17260b + " ok=" + b17260);

                // ---- I. 17261 回应索要请柬(无论成败均无条件重发17252) ----
                bool b17261aNoThrow = NoThrow("On17261", new CliVerify.Pkt().I(1).Bytes());
                bool b17261bNoThrow = NoThrow("On17261", new CliVerify.Pkt().I(4020000).Bytes());
                Debug.Log("CLIVERIFY banquet 17261 回应索要请柬(成败均重发) noThrow=" + (b17261aNoThrow && b17261bNoThrow));

                // ---- J. 17262 婚礼动画场景信息(FigureProto 全字段,pt:write_figure) ----
                CliVerify.Pkt p17262 = new CliVerify.Pkt().I(1)
                    .H(1).L(1001); // man_list[1]:role_id_m,接着 AppendFigure 补 figure_m
                AppendFigure(p17262, "新郎", 130, 1, 3001, "新娘");
                p17262.H(1).L(2001); // woman_list[1]:role_id_w,接着 AppendFigure 补 figure_w
                AppendFigure(p17262, "新娘", 128, 1, 1001, "新郎");
                p17262.H(1).C(1).L(5001).C(1); // guest_position_list[1]:{pos_id,guest_role_id,if_enter}
                Feed("On17262", p17262.Bytes());
                bool b17262 = model.WeddingRoleList != null && model.WeddingRoleList.ManList.Count == 1 && model.WeddingRoleList.ManList[0].RoleId == 1001
                    && model.WeddingRoleList.ManList[0].Figure.name == "新郎"
                    && model.WeddingRoleList.WomanList.Count == 1 && model.WeddingRoleList.WomanList[0].RoleId == 2001
                    && model.WeddingRoleList.GuestPositionList.Count == 1 && model.WeddingRoleList.GuestPositionList[0].GuestRoleId == 5001;
                Debug.Log("CLIVERIFY banquet 17262 场景信息(FigureProto) groomName=" + (model.WeddingRoleList?.ManList[0].Figure.name ?? "?") + " ok=" + b17262);

                // ---- K. 17265 婚礼信息(需在婚礼场景,S2C 平铺字段) ----
                byte[] p17265 = new CliVerify.Pkt().I(1).C(3).I(1700000300).I(1500).I(5).I(2).C(4).Bytes();
                Feed("On17265", p17265);
                bool b17265 = model.BanquetData != null && model.BanquetData.StageId == 3 && model.BanquetData.Aura == 1500
                    && model.BanquetData.LessNormalCandies == 5 && model.BanquetData.LessSpecialCandies == 2 && model.BanquetData.GuestsNum == 4;
                Debug.Log("CLIVERIFY banquet 17265 婚礼信息 stageId=" + (model.BanquetData?.StageId ?? -1) + " aura=" + (model.BanquetData?.Aura ?? -1) + " ok=" + b17265);

                // ---- L. 17266 撒喜糖 / 17267 放烟花(均场景内号,成功重发17272;17267 接受 1/1720071) ----
                bool b17266NoThrow = NoThrow("On17266", new CliVerify.Pkt().I(1).Bytes());
                var firesResults = new List<bool>();
                System.Action<bool> onFiresResult = success => firesResults.Add(success);
                Shenxiao.Framework.Event.EventDispatcher.On<bool>(
                    Shenxiao.Framework.Event.GlobalEvent.EVT_BANQUET_FIRES_RESULT, onFiresResult);
                bool b17267NoThrow;
                try
                {
                    bool normalNoThrow = NoThrow("On17267", new CliVerify.Pkt().I(1).S("").C(1).L(999999999).Bytes());
                    bool broadcastNoThrow = NoThrow("On17267", new CliVerify.Pkt().I(1720071).S("").C(2).L(999999999).Bytes());
                    b17267NoThrow = normalNoThrow && broadcastNoThrow;
                }
                finally
                {
                    Shenxiao.Framework.Event.EventDispatcher.Off<bool>(
                        Shenxiao.Framework.Event.GlobalEvent.EVT_BANQUET_FIRES_RESULT, onFiresResult);
                }
                bool b17267SuccessCodes = firesResults.Count == 2 && firesResults[0] && firesResults[1];
                Debug.Log("CLIVERIFY banquet 17266/17267 撒糖/烟花 noThrow=" + (b17266NoThrow && b17267NoThrow)
                    + " successCodes(1/1720071)=" + b17267SuccessCodes);

                // ---- M. 17270 发弹幕(仅code==1时发事件,失败无ShowError不炸) ----
                bool b17270aNoThrow = NoThrow("On17270", new CliVerify.Pkt().I(1).Bytes());
                bool b17270bNoThrow = NoThrow("On17270", new CliVerify.Pkt().I(0).Bytes());
                Debug.Log("CLIVERIFY banquet 17270 发弹幕 noThrow=" + (b17270aNoThrow && b17270bNoThrow));

                // ---- N. 17271 吃桌菜/采集喜糖结果推送(type==1链式重发17272;其余type不重发) ----
                bool b17271aNoThrow = NoThrow("On17271", new CliVerify.Pkt().I(1).S("桌菜").C(1).Bytes());
                bool b17271bNoThrow = NoThrow("On17271", new CliVerify.Pkt().I(1).S("糖").C(2).Bytes());
                Debug.Log("CLIVERIFY banquet 17271 吃桌菜/采集喜糖 noThrow=" + (b17271aNoThrow && b17271bNoThrow));

                // ---- O. 17272 婚礼道具使用信息(需在婚礼场景;stage_id==3 时才标记已采集餐桌) ----
                byte[] p17272 = new CliVerify.Pkt().I(1).C(1).C(2).C(3).H(1).I(555001).Bytes();
                Feed("On17272", p17272);
                bool b17272 = model.GoodsInfo != null && model.GoodsInfo.IfMaster && model.GoodsInfo.FreeCandies == 2 && model.GoodsInfo.FreeFires == 3
                    && model.GoodsInfo.CollectTableList.Count == 1 && model.GoodsInfo.CollectTableList[0] == 555001
                    && model.ListTableNum.ContainsKey(555001); // BanquetData.StageId==3(上面K步已设置),应标记采集
                Debug.Log("CLIVERIFY banquet 17272 婚礼道具信息 tableMarked=" + model.ListTableNum.ContainsKey(555001) + " ok=" + b17272);

                // ---- Q. 17275 婚礼获得总经验(无Code前缀,唯一字段) ----
                Feed("On17275", new CliVerify.Pkt().L(88888).Bytes());
                bool b17275 = model.AllExp == 88888;
                Debug.Log("CLIVERIFY banquet 17275 婚礼总经验(无Code) allExp=" + model.AllExp + " ok=" + b17275);

                // ---- R. 17276 婚礼开始推送(无read,无Code,无条件重发17249+17250) ----
                bool b17276NoThrow = NoThrow("On17276", new CliVerify.Pkt().L(1001).L(2001).Bytes());
                Debug.Log("CLIVERIFY banquet 17276 婚礼开始推送(无Code链式重发) noThrow=" + b17276NoThrow);

                // ---- S. 17277 气氛值变化推送(无Code,仅type==1落地) ----
                Feed("On17277", new CliVerify.Pkt().H(2).C(1).I(3000).C(2).I(999).Bytes()); // type1落地,type2忽略但仍需读完保游标
                bool b17277 = model.AuraValue == 3000;
                Debug.Log("CLIVERIFY banquet 17277 气氛值变化(无Code,type过滤) auraValue=" + model.AuraValue + " ok=" + b17277);

                // ---- T. 17278 气氛值奖励推送 / 17279 吃桌菜奖励推送(均无Code,标准ObjectList) ----
                Feed("On17278", new CliVerify.Pkt().I(1500).H(1).C(0).I(23020001).I(5).Bytes());
                bool b17278 = model.LastAuraNum == 1500 && model.LastAuraReward.Count == 1 && model.LastAuraReward[0].TypeId == 23020001 && model.LastAuraReward[0].Num == 5;
                Feed("On17279", new CliVerify.Pkt().C(1).H(1).C(0).I(32010166).I(1).Bytes());
                bool b17279 = model.LastTableRewardType == 1 && model.LastTableReward.Count == 1 && model.LastTableReward[0].TypeId == 32010166;
                Debug.Log("CLIVERIFY banquet 17278/17279 奖励推送(无Code) auraReward=" + b17278 + " tableReward=" + b17279 + " ok=" + (b17278 && b17279));

                // ---- U. 17298 一键邀请剩余宾客(成功重发17252+17260[TypeList=2]) ----
                bool b17298aNoThrow = NoThrow("On17298", new CliVerify.Pkt().I(1).Bytes());
                bool b17298bNoThrow = NoThrow("On17298", new CliVerify.Pkt().I(4020000).Bytes());
                Debug.Log("CLIVERIFY banquet 17298 一键邀请 noThrow=" + (b17298aNoThrow && b17298bNoThrow));

                bool pass = addressablesOk && configOk && configRowOk && initRegistrationOk && outboundEncodeOk && allDead && noDoubleReg
                    && b17250 && b17251NoThrow && b17251bNoThrow
                    && b17252 && b17253NoThrow && b17257_58NoThrow
                    && b17259aNoThrow && b17259bNoThrow && b17260
                    && b17261aNoThrow && b17261bNoThrow && b17262 && b17265
                    && b17266NoThrow && b17267NoThrow && b17267SuccessCodes && b17270aNoThrow && b17270bNoThrow
                    && b17271aNoThrow && b17271bNoThrow && b17272
                    && b17275 && b17276NoThrow && b17277 && b17278 && b17279
                    && b17298aNoThrow && b17298bNoThrow;

                Debug.Log("CLIVERIFY banquet VERDICT addressables=" + addressablesOk + " config=" + (configOk && configRowOk)
                    + " init=" + initRegistrationOk + " outbound=" + outboundEncodeOk + " killlist=" + (allDead && noDoubleReg)
                    + " apply(50/51)=" + (b17250 && b17251NoThrow) + " invite(52/53)=" + (b17252 && b17253NoThrow)
                    + " ask(57/58/59/60/61)=" + (b17257_58NoThrow && b17259aNoThrow && b17260 && b17261aNoThrow)
                    + " scene(62/65)=" + (b17262 && b17265) + " goods(66/67/70/71/72)="
                    + (b17266NoThrow && b17267NoThrow && b17267SuccessCodes && b17270aNoThrow && b17271aNoThrow && b17272)
                    + " push(75/76/77/78/79)=" + (b17275 && b17276NoThrow && b17277 && b17278 && b17279)
                    + " oneInvite98=" + b17298aNoThrow + " pass=" + pass);

                model.Reset();
                return pass ? 0 : 3;
            }
            finally
            {
                if (initializedByCase) controller.Dispose();
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = editorPreferFallbackBefore;
                Application.logMessageReceived -= cb;
            }
        }

        private static bool ValidateWeddingAddressables(out string missingCsv)
        {
            string[] expected =
            {
                "resource/config/server/config_wedding_info",
                "resource/config/server/config_wedding_time",
                "resource/config/server/config_wedding_time_stage",
                "resource/config/server/config_wedding_candies",
                "resource/config/server/config_wedding_fires",
                "resource/config/server/config_wedding_table",
                "resource/config/server/config_wedding_aura",
                "resource/config/server/config_wedding_guest_position",
                "resource/config/server/config_wedding_position",
                "resource/config/server/config_wedding_scene_exp_coef",
                "resource/config/server/config_wedding_card",
                "resource/config/server/config_wedding_scene_exp",
                "resource/config/server/config_wedding_trouble_maker",
            };
            var registered = new HashSet<string>(System.StringComparer.Ordinal);
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings =
                UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                foreach (UnityEditor.AddressableAssets.Settings.AddressableAssetGroup group in settings.groups)
                {
                    if (group == null) continue;
                    foreach (UnityEditor.AddressableAssets.Settings.AddressableAssetEntry entry in group.entries)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.address)) registered.Add(entry.address);
                    }
                }
            }

            var missing = new List<string>();
            foreach (string key in expected)
            {
                if (!registered.Contains(key)) missing.Add(key);
            }
            missingCsv = string.Join(",", missing);
            return settings != null && missing.Count == 0;
        }

        /// <summary>控制器请求方法与这些 Build*Payload 共用同一份 fmt/args；这里把它们喂给正式编码器，
        /// 逐字节验证帧头、协议号和 payload，并用离线发送日志确认公开 API 确实走到对应协议。</summary>
        private static bool RunOutboundEncodingAssertions(
            Shenxiao.Module.Core.Banquet.BanquetController controller, List<string> logs)
        {
            System.Type t = controller.GetType();
            MethodInfo enterBuilder = t.GetMethod("BuildEnterWeddingScenePayload", SF);
            MethodInfo leaveBuilder = t.GetMethod("BuildLeaveWeddingScenePayload", SF);
            MethodInfo candiesBuilder = t.GetMethod("BuildSprinkleCandiesPayload", SF);
            if (enterBuilder == null || leaveBuilder == null || candiesBuilder == null)
            {
                Debug.LogError("CLIVERIFY banquet outbound builder missing");
                return false;
            }

            const long roleIdM = 0x0102030405060708L;
            (string fmt, object[] args) enter = ((string, object[]))enterBuilder.Invoke(null, new object[] { roleIdM });
            byte[] enterBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(
                Shenxiao.Framework.Net.Proto.MARRIAGE_BANQUET_ENTER_SCENE, enter.fmt, enter.args);
            var enterReader = new Shenxiao.Framework.Net.NetReader(enterBytes, 6, enterBytes.Length - 6);
            bool enterWireOk = HasFrameHeader(enterBytes, 17263) && enter.fmt == "l"
                && enterReader.ReadU64() == roleIdM && enterReader.Remaining == 0;

            (string fmt, object[] args) leave = ((string, object[]))leaveBuilder.Invoke(null, null);
            byte[] leaveBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(
                Shenxiao.Framework.Net.Proto.MARRIAGE_BANQUET_LEAVE_SCENE, leave.fmt, leave.args);
            bool leaveWireOk = HasFrameHeader(leaveBytes, 17264) && string.IsNullOrEmpty(leave.fmt)
                && leaveBytes.Length == 6;

            (string fmt, object[] args) normal = ((string, object[]))candiesBuilder.Invoke(
                null, new object[] { Shenxiao.Module.Core.Banquet.BanquetController.CANDIES_TYPE_NORMAL });
            (string fmt, object[] args) special = ((string, object[]))candiesBuilder.Invoke(
                null, new object[] { Shenxiao.Module.Core.Banquet.BanquetController.CANDIES_TYPE_SPECIAL });
            byte[] normalBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(
                Shenxiao.Framework.Net.Proto.BANQUET_SPRINKLE_CANDIES, normal.fmt, normal.args);
            byte[] specialBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(
                Shenxiao.Framework.Net.Proto.BANQUET_SPRINKLE_CANDIES, special.fmt, special.args);
            var normalReader = new Shenxiao.Framework.Net.NetReader(normalBytes, 6, normalBytes.Length - 6);
            var specialReader = new Shenxiao.Framework.Net.NetReader(specialBytes, 6, specialBytes.Length - 6);
            bool candiesWireOk = HasFrameHeader(normalBytes, 17266) && HasFrameHeader(specialBytes, 17266)
                && normal.fmt == "c" && special.fmt == "c"
                && normalReader.ReadU8() == 1 && normalReader.Remaining == 0
                && specialReader.ReadU8() == 2 && specialReader.Remaining == 0;

            logs.Clear();
            controller.RequestEnterWeddingScene(roleIdM);
            controller.RequestLeaveWeddingScene();
            controller.RequestSprinkleCandies(Shenxiao.Module.Core.Banquet.BanquetController.CANDIES_TYPE_NORMAL);
            controller.RequestSprinkleCandies(Shenxiao.Module.Core.Banquet.BanquetController.CANDIES_TYPE_SPECIAL);
            bool publicSendOk = logs.Exists(l => l.Contains("proto=17263"))
                && logs.Exists(l => l.Contains("proto=17264"))
                && logs.FindAll(l => l.Contains("proto=17266")).Count == 2;

            logs.Clear();
            controller.RequestSprinkleCandies(8002003);
            bool goodsIdRejected = logs.Exists(l => l.Contains("非法 candies_type=8002003"))
                && !logs.Exists(l => l.Contains("proto=17266"));

            bool ok = enterWireOk && leaveWireOk && candiesWireOk && publicSendOk && goodsIdRejected;
            Debug.Log("CLIVERIFY banquet outbound 17263(l)=" + enterWireOk + " 17264(empty)=" + leaveWireOk
                + " 17266(type1/2)=" + candiesWireOk + " publicSend=" + publicSendOk
                + " goodsIdRejected=" + goodsIdRejected + " ok=" + ok);
            return ok;
        }

        private static bool HasFrameHeader(byte[] frame, int protoId)
        {
            return frame != null && frame.Length >= 6
                && ((frame[0] << 8) | frame[1]) == frame.Length
                && ((frame[2] << 8) | frame[3]) == 1000
                && ((frame[4] << 8) | frame[5]) == protoId;
        }

        /// <summary>按 FigureProto.SCHEMA 逐字段序写入(42 字段,5 个嵌套数组固定写 0 条;name/level/
        /// is_marriage/marriage_id/marriage_name 可覆盖供断言探针,同 MarriageCase.AppendFigure 套路,
        /// 本类自成一份不跨包依赖)。</summary>
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
