using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// P2 抽奖A(自动循环 轮17,spec §3)实证:OPTIONALLOTTO=76(33128/29/33/34/35/39)/WISH_POOL=79
    /// (33141/42/44)/DESTINY_TURNTABLE=99(33238/39/40)/TURNTABLE_100=100(33241/42)共 14 号,合成包驱动
    /// CustomActivityController 反射喂包,断言 CustomActivityModel.LotteryA 落地字段/事件(模板
    /// CustomActCoreCase/MarriageCase,纯逻辑段)。
    ///
    /// 重点覆盖:①注册线核实(NetManager._handlers)——15b/16 血训,反射能调到方法体不代表真的挂了协议号;
    /// ②33128 三嵌套数组(Pool 3字段/Stage 2字段/RewardList 6字段 Name→Desc→Condition→Reward 顺序探针);
    /// ③33129 变长请求(动态 fmt)+ ErrorCode 末尾 + 成功回写缓存面板 Pool、失败不覆盖;④33133 ErrorCode
    /// 第3字段 + 成功回写 DrawTimes/Reset/Pool/Stage;⑤33134 ErrorCode 第4字段 + Reward 直接展开 ObjectList
    /// 末条探针;⑥33135 ErrorCode 末尾;⑦33139 Pool **2字段**(区别于33128/33の3字段);⑧33142 ErrorCode
    /// 第4字段 + RewardList 元素 {嵌套ObjectList,IsRare} 包装(与33134不同结构)末条探针;⑨33144 字段名
    /// "Code"；⑩33238 RewardList 字段序 Name→Desc→**Reward→Condition**(与33128/33241相反)+ DoublePoint
    /// 嵌套;⑪33239 recv-only(面板缺失/存在两种边界,回写面板 Turn/Point/NeedPoint);⑫33240 ErrorCode **最前**
    /// + Reward 走 write_string(非 ObjectList)+ 成功回写面板并翻转匹配 Grade 的 Status;⑬33241 字段序
    /// Name→Desc→**Condition→Reward**(与33238相反);⑭33242 recv-only,RewardList 元素**Grade+Process**
    /// (订正 r17_server 侦察表"Status"误记,见 Model 注释),按 Grade 合并进缓存面板。
    ///
    /// 死号防御:33239/33242 断言无公开 Request 方法(仅防御 recv)。
    /// </summary>
    public static class CustomActLotteryACase
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
                model.ClearLotteryA();

                object ctrl = Shenxiao.Module.Core.CustomActivity.CustomActivityController.Instance;
                System.Type t = ctrl.GetType();
                bool anyThrew = false;
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY customact_lotteryA handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY customact_lotteryA " + method + " threw: " + e);
                    }
                }

                // ---- 0. 注册线核实(NetManager._handlers)——15b/16 血训 ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LOTTO_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LOTTO_LOCK,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LOTTO_RESET, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LOTTO_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LOTTO_STAGE, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_LOTTO_POOL,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_WISHPOOL_POOL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_WISHPOOL_CLAIM,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_WISHPOOL_RESET, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_DESTINY_PANEL,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_DESTINY_PUSH, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_DESTINY_DRAW,
                    Shenxiao.Framework.Net.Proto.CUSTOM_ACT_TURN100_PANEL, Shenxiao.Framework.Net.Proto.CUSTOM_ACT_TURN100_PUSH,
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
                Debug.Log("CLIVERIFY customact_lotteryA 注册线核实(NetManager._handlers) missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                // ==================== OPTIONALLOTTO(76) ====================
                const int LOTTO_BASE = 200, LOTTO_SUB = 1;

                // ---- A. 33128 界面(三嵌套数组:Pool 3字段/Stage 2字段/RewardList 6字段 探针字段序) ----
                byte[] p33128 = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(10).H(3)
                    .H(2)
                        .C(1).H(101).C(0)
                        .C(2).H(102).C(1)
                    .H(1)
                        .H(201).C(1)
                    .H(1)
                        .H(301).C(0).S("奖励名").S("奖励描述").S("奖励条件").S("奖励文案")
                    .Bytes();
                Feed("On33128", p33128);
                var panelLotto = model.GetLottoPanel(LOTTO_BASE, LOTTO_SUB);
                bool b33128 = panelLotto != null && panelLotto.DrawTimes == 10 && panelLotto.Reset == 3
                    && panelLotto.Pool.Count == 2 && panelLotto.Pool[1].Rare == 2 && panelLotto.Pool[1].Grade == 102 && panelLotto.Pool[1].Status == 1
                    && panelLotto.Stage.Count == 1 && panelLotto.Stage[0].Grade == 201 && panelLotto.Stage[0].Status == 1
                    && panelLotto.RewardList.Count == 1 && panelLotto.RewardList[0].Grade == 301
                    && panelLotto.RewardList[0].Name == "奖励名" && panelLotto.RewardList[0].Desc == "奖励描述"
                    && panelLotto.RewardList[0].Condition == "奖励条件" && panelLotto.RewardList[0].Reward == "奖励文案";
                Debug.Log("CLIVERIFY customact_lotteryA 33128 界面 poolN=" + (panelLotto?.Pool.Count ?? -1) + " ok=" + b33128);

                // ---- B. 33129 锁定(ErrorCode末尾;成功回写面板Pool,失败不覆盖) ----
                byte[] p33129ok = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB)
                    .H(1).C(3).H(999).C(2)
                    .I(1)
                    .Bytes();
                Feed("On33129", p33129ok);
                var lockOk = model.GetLottoLockResult(LOTTO_BASE, LOTTO_SUB);
                bool b33129ok = lockOk != null && lockOk.ErrorCode == 1 && lockOk.Pool.Count == 1
                    && lockOk.Pool[0].Rare == 3 && lockOk.Pool[0].Grade == 999 && lockOk.Pool[0].Status == 2
                    && panelLotto.Pool.Count == 1 && panelLotto.Pool[0].Grade == 999; // 面板已被回写为锁定后的新Pool
                byte[] p33129fail = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(0).I(1720001).Bytes();
                Feed("On33129", p33129fail);
                var lockAfterFail = model.GetLottoLockResult(LOTTO_BASE, LOTTO_SUB);
                bool b33129fail = lockAfterFail != null && lockAfterFail.ErrorCode == 1 && lockAfterFail.Pool.Count == 1 // 未被失败包覆盖
                    && panelLotto.Pool.Count == 1 && panelLotto.Pool[0].Grade == 999; // 面板同样未被失败包覆盖
                bool b33129 = b33129ok && b33129fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33129 锁定奖池(末尾ErrorCode) ok=" + b33129ok + " failNotOverwritten=" + b33129fail);

                // ---- C. 33133 重置(ErrorCode第3字段;成功回写DrawTimes/Reset/Pool/Stage) ----
                byte[] p33133ok = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).I(1).H(20).H(5)
                    .H(1).C(1).H(888).C(0)
                    .H(1).H(202).C(0)
                    .Bytes();
                Feed("On33133", p33133ok);
                bool b33133ok = panelLotto.DrawTimes == 20 && panelLotto.Reset == 5 && panelLotto.Pool.Count == 1
                    && panelLotto.Pool[0].Grade == 888 && panelLotto.Stage.Count == 1 && panelLotto.Stage[0].Grade == 202;
                byte[] p33133fail = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).I(1720002).H(0).H(0).H(0).H(0).Bytes();
                Feed("On33133", p33133fail);
                bool b33133fail = panelLotto.DrawTimes == 20 && panelLotto.Reset == 5; // 未被失败包覆盖
                bool b33133 = b33133ok && b33133fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33133 重置(第3字段ErrorCode) ok=" + b33133ok + " failNotOverwritten=" + b33133fail);

                // ---- D. 33134 抽奖(ErrorCode第4字段;Reward直接展开ObjectList,末条探针) ----
                byte[] p33134ok = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(4).I(1).H(7).C(1)
                    .H(2).C(0).I(50001).I(1).C(1).I(50002).I(3)
                    .Bytes();
                Feed("On33134", p33134ok);
                var drawOk = model.GetLottoDrawResult(LOTTO_BASE, LOTTO_SUB);
                bool b33134ok = drawOk != null && drawOk.DrawTimes == 4 && drawOk.Grade == 7 && drawOk.Rare == 1
                    && drawOk.Reward.Count == 2 && drawOk.Reward[1].Type == 1 && drawOk.Reward[1].GoodsId == 50002 && drawOk.Reward[1].Num == 3;
                byte[] p33134fail = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(9).I(1720003).H(9).C(9).H(0).Bytes();
                Feed("On33134", p33134fail);
                var drawAfterFail = model.GetLottoDrawResult(LOTTO_BASE, LOTTO_SUB);
                bool b33134fail = drawAfterFail != null && drawAfterFail.DrawTimes == 4; // 未被失败包覆盖
                bool b33134 = b33134ok && b33134fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33134 抽奖(第4字段ErrorCode+末条探针) ok=" + b33134ok + " failNotOverwritten=" + b33134fail);

                // ---- E. 33135 阶段奖励(ErrorCode末尾) ----
                byte[] p33135ok = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(4).I(1).Bytes();
                Feed("On33135", p33135ok);
                var stageOk = model.GetLottoStageResult(LOTTO_BASE, LOTTO_SUB);
                bool b33135ok = stageOk != null && stageOk.Grade == 4 && stageOk.ErrorCode == 1;
                byte[] p33135fail = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(9).I(1720004).Bytes();
                Feed("On33135", p33135fail);
                var stageAfterFail = model.GetLottoStageResult(LOTTO_BASE, LOTTO_SUB);
                bool b33135fail = stageAfterFail != null && stageAfterFail.Grade == 4; // 未被失败包覆盖
                bool b33135 = b33135ok && b33135fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33135 阶段奖励(末尾ErrorCode) ok=" + b33135ok + " failNotOverwritten=" + b33135fail);

                // ---- F. 33139 奖池(2字段Pool,区别于33128/33133的3字段Pool) ----
                byte[] p33139 = new CliVerify.Pkt().H(LOTTO_BASE).H(LOTTO_SUB).H(2).C(1).H(401).C(2).H(402).Bytes();
                Feed("On33139", p33139);
                var randomPool = model.GetLottoRandomPool(LOTTO_BASE, LOTTO_SUB);
                bool b33139 = randomPool != null && randomPool.Count == 2 && randomPool[0].Rare == 1 && randomPool[0].Grade == 401
                    && randomPool[1].Rare == 2 && randomPool[1].Grade == 402;
                Debug.Log("CLIVERIFY customact_lotteryA 33139 奖池(2字段) poolN=" + (randomPool?.Count ?? -1) + " ok=" + b33139);

                // ==================== WISH_POOL(79) ====================
                const int WISH_BASE = 79, WISH_SUB = 1;

                // ---- G. 33141 奖池(无ErrorCode) ----
                byte[] p33141 = new CliVerify.Pkt().H(WISH_BASE).H(WISH_SUB).H(1)
                    .H(1).H(500).H(3).C(0).H(1000)
                    .Bytes();
                Feed("On33141", p33141);
                var wishPool = model.GetWishPool(WISH_BASE, WISH_SUB);
                bool b33141 = wishPool != null && wishPool.Count == 1 && wishPool[0].Grade == 1 && wishPool[0].LuckyValue == 500
                    && wishPool[0].FreeTimes == 3 && wishPool[0].MaxLuckyValue == 1000;
                Debug.Log("CLIVERIFY customact_lotteryA 33141 许愿池奖池 ok=" + b33141);

                // ---- H. 33142 取奖池奖励(ErrorCode第4字段;RewardList元素{嵌套ObjectList,IsRare}包装,末条探针) ----
                byte[] p33142ok = new CliVerify.Pkt().H(WISH_BASE).H(WISH_SUB).H(2).I(1)
                    .H(1)
                        .H(2).C(0).I(60001).I(1).C(1).I(60002).I(5).C(1)
                    .H(80).H(4).C(1)
                    .Bytes();
                Feed("On33142", p33142ok);
                var wishClaimOk = model.GetWishClaimResult(WISH_BASE, WISH_SUB);
                bool b33142ok = wishClaimOk != null && wishClaimOk.Grade == 2 && wishClaimOk.LuckyValue == 80 && wishClaimOk.FreeTimes == 4
                    && wishClaimOk.State == 1 && wishClaimOk.RewardList.Count == 1 && wishClaimOk.RewardList[0].IsRare == 1
                    && wishClaimOk.RewardList[0].Reward.Count == 2 && wishClaimOk.RewardList[0].Reward[1].GoodsId == 60002 && wishClaimOk.RewardList[0].Reward[1].Num == 5;
                byte[] p33142fail = new CliVerify.Pkt().H(WISH_BASE).H(WISH_SUB).H(9).I(1720005).H(0).H(0).H(0).C(0).Bytes();
                Feed("On33142", p33142fail);
                var wishClaimAfterFail = model.GetWishClaimResult(WISH_BASE, WISH_SUB);
                bool b33142fail = wishClaimAfterFail != null && wishClaimAfterFail.Grade == 2; // 未被失败包覆盖
                bool b33142 = b33142ok && b33142fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33142 取奖池奖励(嵌套ObjectList+IsRare) ok=" + b33142ok + " failNotOverwritten=" + b33142fail);

                // ---- I. 33144 重置(字段名"Code") ----
                byte[] p33144ok = new CliVerify.Pkt().H(WISH_BASE).H(WISH_SUB).H(2).I(1).H(0).H(5).C(0).H(1000).Bytes();
                Feed("On33144", p33144ok);
                var wishResetOk = model.GetWishResetResult(WISH_BASE, WISH_SUB);
                bool b33144ok = wishResetOk != null && wishResetOk.Code == 1 && wishResetOk.FreeTimes == 5 && wishResetOk.MaxLuckyValue == 1000;
                byte[] p33144fail = new CliVerify.Pkt().H(WISH_BASE).H(WISH_SUB).H(9).I(1720006).H(9).H(9).C(9).H(9).Bytes();
                Feed("On33144", p33144fail);
                var wishResetAfterFail = model.GetWishResetResult(WISH_BASE, WISH_SUB);
                bool b33144fail = wishResetAfterFail != null && wishResetAfterFail.Code == 1; // 未被失败包覆盖
                bool b33144 = b33144ok && b33144fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33144 许愿池重置(字段名Code) ok=" + b33144ok + " failNotOverwritten=" + b33144fail);

                // ==================== DESTINY_TURNTABLE(99) ====================
                const int DEST_BASE = 99, DEST_SUB = 1;

                // ---- J. 33238 界面(RewardList字段序 Name→Desc→Reward→Condition,与33128/33241相反) ----
                byte[] p33238 = new CliVerify.Pkt().H(DEST_BASE).H(DEST_SUB).H(1).I(300).I(1000).H(20)
                    .H(1)
                        .H(5).C(0).C(0).S("天命名").S("天命描述").S("天命奖励文案").S("天命条件")
                    .H(1)
                        .I(70001).C(1)
                    .C(9)
                    .Bytes();
                Feed("On33238", p33238);
                var destPanel = model.GetDestinyPanel(DEST_BASE, DEST_SUB);
                bool b33238 = destPanel != null && destPanel.Turn == 1 && destPanel.Point == 300 && destPanel.NeedPoint == 1000
                    && destPanel.MaxTurn == 20 && destPanel.Label == 9
                    && destPanel.RewardList.Count == 1 && destPanel.RewardList[0].Grade == 5
                    && destPanel.RewardList[0].Name == "天命名" && destPanel.RewardList[0].Desc == "天命描述"
                    && destPanel.RewardList[0].Reward == "天命奖励文案" && destPanel.RewardList[0].Condition == "天命条件" // 顺序探针
                    && destPanel.DoublePoint.Count == 1 && destPanel.DoublePoint[0].JumpId == 70001 && destPanel.DoublePoint[0].IsBuy == 1;
                Debug.Log("CLIVERIFY customact_lotteryA 33238 天命转盘界面(字段序Reward在Condition前) ok=" + b33238);

                // ---- K. 33239 recv-only(面板存在:回写Turn/Point/NeedPoint;另测面板缺失边界不抛) ----
                byte[] p33239 = new CliVerify.Pkt().H(DEST_BASE).H(DEST_SUB).H(2).I(600).I(1000).Bytes();
                Feed("On33239", p33239);
                bool b33239withPanel = destPanel.Turn == 2 && destPanel.Point == 600 && model.GetDestinyPushInfo(DEST_BASE, DEST_SUB)?.Point == 600;
                byte[] p33239noPanel = new CliVerify.Pkt().H(DEST_BASE).H(999).H(1).I(1).I(2).Bytes(); // 999子类型未建面板
                Feed("On33239", p33239noPanel);
                bool b33239noPanelOk = !anyThrew && model.GetDestinyPushInfo(DEST_BASE, 999) != null; // 仅落推送记录,不抛
                bool b33239 = b33239withPanel && b33239noPanelOk;
                Debug.Log("CLIVERIFY customact_lotteryA 33239 recv-only(面板存在回写/面板缺失不抛) withPanel=" + b33239withPanel + " noPanelSafe=" + b33239noPanelOk);

                // ---- L. 33240 开抽(ErrorCode最前;Reward走write_string;成功回写面板+翻转匹配Grade的Status) ----
                byte[] p33240ok = new CliVerify.Pkt().I(1).H(DEST_BASE).H(DEST_SUB).H(5).S("恭喜获得天命奖励").H(3).I(900).I(1000).Bytes();
                Feed("On33240", p33240ok);
                var destDrawOk = model.GetDestinyDrawResult(DEST_BASE, DEST_SUB);
                bool b33240ok = destDrawOk != null && destDrawOk.GradeId == 5 && destDrawOk.Reward == "恭喜获得天命奖励" && destDrawOk.Turn == 3
                    && destPanel.Turn == 3 && destPanel.Point == 900 && destPanel.RewardList[0].Status == 1; // Grade==5 匹配翻转
                byte[] p33240fail = new CliVerify.Pkt().I(1720007).H(DEST_BASE).H(DEST_SUB).H(5).S("").H(9).I(9).I(9).Bytes();
                Feed("On33240", p33240fail);
                var destDrawAfterFail = model.GetDestinyDrawResult(DEST_BASE, DEST_SUB);
                bool b33240fail = destDrawAfterFail != null && destDrawAfterFail.Turn == 3 && destPanel.Turn == 3; // 未被失败包覆盖(面板与结果都不变)
                bool b33240 = b33240ok && b33240fail;
                Debug.Log("CLIVERIFY customact_lotteryA 33240 天命转盘开抽(最前ErrorCode+write_string+Status翻转) ok=" + b33240ok + " failNotOverwritten=" + b33240fail);

                // ==================== TURNTABLE_100(100) ====================
                const int T100_BASE = 100, T100_SUB = 1;

                // ---- M. 33241 界面(RewardList字段序 Name→Desc→Condition→Reward,与33238相反) ----
                byte[] p33241 = new CliVerify.Pkt().H(T100_BASE).H(T100_SUB).H(1)
                    .H(1).C(0).C(0).H(0).H(10).S("寻宝名").S("寻宝描述").S("寻宝条件").S("寻宝奖励文案")
                    .Bytes();
                Feed("On33241", p33241);
                var t100Panel = model.GetTurn100Panel(T100_BASE, T100_SUB);
                bool b33241 = t100Panel != null && t100Panel.Count == 1 && t100Panel[0].Grade == 1 && t100Panel[0].ReceiveTimes == 10
                    && t100Panel[0].Condition == "寻宝条件" && t100Panel[0].Reward == "寻宝奖励文案"; // 顺序探针(Condition在Reward之前)
                Debug.Log("CLIVERIFY customact_lotteryA 33241 幸运寻宝界面(字段序Condition在Reward前) ok=" + b33241);

                // ---- N. 33242 recv-only(RewardList元素Grade+Process,订正Status误记;按Grade合并进面板;含未匹配边界) ----
                byte[] p33242 = new CliVerify.Pkt().H(T100_BASE).H(T100_SUB).H(2)
                    .H(1).H(55)     // 匹配面板已有的 Grade=1,应合并更新 Process
                    .H(999).H(77)   // 未匹配任何面板条目,应安全忽略不抛
                    .Bytes();
                Feed("On33242", p33242);
                var t100Push = model.GetTurn100Push(T100_BASE, T100_SUB);
                bool b33242 = !anyThrew && t100Push != null && t100Push.Count == 2 && t100Push[0].Process == 55 && t100Push[1].Grade == 999
                    && t100Panel[0].Process == 55; // 面板条目已按Grade合并更新Process
                Debug.Log("CLIVERIFY customact_lotteryA 33242 recv-only(Grade+Process订正+合并) ok=" + b33242);

                // ---- O. 死号防御:33239/33242 无公开 Request 方法 ----
                bool dead33239 = t.GetMethod("RequestDestinyPush", PF) == null && t.GetMethod("On33239", F) != null;
                bool dead33242 = t.GetMethod("RequestTurn100Push", PF) == null && t.GetMethod("On33242", F) != null;
                Debug.Log("CLIVERIFY customact_lotteryA 死号防御 33239noSend=" + dead33239 + " 33242noSend=" + dead33242);

                // ---- P. 公开 Request 方法存在且 no-throw(反射校验 + 直接调用) ----
                bool sendMethodsExist = t.GetMethod("RequestLottoPanel", PF) != null && t.GetMethod("RequestLottoLock", PF) != null
                    && t.GetMethod("RequestLottoReset", PF) != null && t.GetMethod("RequestLottoDraw", PF) != null
                    && t.GetMethod("RequestLottoStage", PF) != null && t.GetMethod("RequestLottoPool", PF) != null
                    && t.GetMethod("RequestWishPoolPanel", PF) != null && t.GetMethod("RequestWishPoolClaim", PF) != null
                    && t.GetMethod("RequestWishPoolReset", PF) != null && t.GetMethod("RequestDestinyPanel", PF) != null
                    && t.GetMethod("RequestDestinyDraw", PF) != null && t.GetMethod("RequestTurn100Panel", PF) != null;
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.CustomActivity.CustomActivityController)ctrl;
                    c.RequestLottoPanel(LOTTO_BASE, LOTTO_SUB);
                    c.RequestLottoLock(LOTTO_BASE, LOTTO_SUB, new List<(int, int)> { (1, 100), (2, 200) });
                    c.RequestLottoLock(LOTTO_BASE, LOTTO_SUB, null); // 空池边界(变长发送 0 条不抛)
                    c.RequestLottoReset(LOTTO_BASE, LOTTO_SUB);
                    c.RequestLottoDraw(LOTTO_BASE, LOTTO_SUB, 1);
                    c.RequestLottoStage(LOTTO_BASE, LOTTO_SUB, 4);
                    c.RequestLottoPool(LOTTO_BASE, LOTTO_SUB);
                    c.RequestWishPoolPanel(WISH_BASE, WISH_SUB);
                    c.RequestWishPoolClaim(WISH_BASE, WISH_SUB, 2, 10, 1);
                    c.RequestWishPoolReset(WISH_BASE, WISH_SUB, 2);
                    c.RequestDestinyPanel(DEST_BASE, DEST_SUB);
                    c.RequestDestinyDraw(DEST_BASE, DEST_SUB);
                    c.RequestTurn100Panel(T100_BASE, T100_SUB);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY customact_lotteryA send methods threw: " + e); }
                bool bSend = sendMethodsExist && sendNoThrow;
                Debug.Log("CLIVERIFY customact_lotteryA 公开发送方法存在且noThrow methodsExist=" + sendMethodsExist + " noThrow=" + sendNoThrow + " ok=" + bSend);

                bool pass = !anyThrew && bRegistered
                    && b33128 && b33129 && b33133 && b33134 && b33135 && b33139
                    && b33141 && b33142 && b33144
                    && b33238 && b33239 && b33240
                    && b33241 && b33242
                    && dead33239 && dead33242 && bSend;

                Debug.Log("CLIVERIFY customact_lotteryA VERDICT registered=" + bRegistered
                    + " l33128=" + b33128 + " l33129=" + b33129 + " l33133=" + b33133 + " l33134=" + b33134 + " l33135=" + b33135 + " l33139=" + b33139
                    + " l33141=" + b33141 + " l33142=" + b33142 + " l33144=" + b33144
                    + " l33238=" + b33238 + " l33239=" + b33239 + " l33240=" + b33240
                    + " l33241=" + b33241 + " l33242=" + b33242
                    + " dead33239=" + dead33239 + " dead33242=" + dead33242 + " sendApi=" + bSend
                    + " anyThrew=" + anyThrew + " pass=" + pass);

                model.Clear();
                model.ClearLotteryA();
                return Task.FromResult(pass ? 0 : 3);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
