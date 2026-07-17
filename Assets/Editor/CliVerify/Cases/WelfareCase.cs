using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Welfare 余量(417xx补全)+GrowthBenefits补全(41722)+CombatWelfare(41723/41724)+AdReward(193xx)
    /// 合包实证(自动循环 轮18 PK4)。合成包驱动 4 个 Controller(WelfareController/CombatWelfareController/
    /// AdRewardController/GrowthBenefitsController)反射喂包,断言 WelfareModel/CombatWelfareController 内部
    /// 状态/AdRewardModel/GrowthBenefitsModel(反射私有字段) 落地字段 + EVT_WELFARE_UPDATE/EVT_WELFARE_RESULT/
    /// EVT_ADREWARD_UPDATE 事件 + 41716 二层嵌套(SendList{Rewards,OtherRewards}均为 ObjectList)游标探针 +
    /// 孤号(41702/41706/41710-41714/41717/41718)禁注册反射断言 + 注册线核实(NetManager._handlers 直查,
    /// 同 CustomActCoreCase 先例)+ config 计数(13 张表,以实际文件为准,详见类内注释)。
    /// 日志前缀 "CLIVERIFY welfare"。UI 层未移植,本轮数据层only(同 15a/15b Boss/轮16 Marriage 先例)。
    /// </summary>
    public static class WelfareCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PF = BindingFlags.Public | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);

                // ---- 0. config 计数(13 张表;spec 估算 welfare_cfg~10/combat_welfare_times=100/xinyue=5,
                //         实测分别为 9/96/4——以实际文件为准[spec §0 明文授权],偏差记汇报) ----
                await Shenxiao.Module.Core.Welfare.WelfareConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Welfare.WelfareConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY welfare FAIL WelfareConfigs not loaded");
                    return 3;
                }
                bool configOk = Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinTypeCount == 12
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinDailyRewardsCount == 30
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinTotalRewardsCount == 5
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinDailyRetroactiveCount == 31
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinKeyValueCount == 5
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.OnlineRewardCount == 24
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.WelfareCfgCount == 9
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.WelfareNightRewardCount == 1
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.GrowWelfareInfoCount == 36
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.CombatWelfareRewardCount == 96
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.CombatWelfareTimesCount == 96
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.XinyueGiftCount == 4
                    && Shenxiao.Module.Core.Welfare.WelfareConfigs.RealInfoRewardCount == 4;
                Debug.Log("CLIVERIFY welfare config checkin(type/day/total/retro/kv)="
                    + Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinTypeCount + "/" + Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinDailyRewardsCount
                    + "/" + Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinTotalRewardsCount + "/" + Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinDailyRetroactiveCount
                    + "/" + Shenxiao.Module.Core.Welfare.WelfareConfigs.CheckinKeyValueCount
                    + " online=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.OnlineRewardCount
                    + " welfareCfg=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.WelfareCfgCount + "(spec估算~10,实测9,以实际为准)"
                    + " night=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.WelfareNightRewardCount
                    + " grow=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.GrowWelfareInfoCount
                    + " combatReward=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.CombatWelfareRewardCount
                    + " combatTimes=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.CombatWelfareTimesCount + "(spec估算100,实测96,以实际为准)"
                    + " xinyue=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.XinyueGiftCount + "(spec估算5,实测4,以实际为准)"
                    + " realInfo=" + Shenxiao.Module.Core.Welfare.WelfareConfigs.RealInfoRewardCount
                    + " ok=" + configOk);

                Shenxiao.Module.Core.Welfare.WelfareModel model = Shenxiao.Module.Core.Welfare.WelfareModel.Instance;
                model.Reset();
                object welfareCtrl = Shenxiao.Module.Core.Welfare.WelfareController.Instance;
                object combatCtrl = Shenxiao.Module.Core.Welfare.CombatWelfareController.Instance;
                object adCtrl = Shenxiao.Module.Core.AdReward.AdRewardController.Instance;
                object gbCtrl = Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsController.Instance;

                void Feed(object ctrl, string method, byte[] pkt)
                {
                    MethodInfo m = ctrl.GetType().GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY welfare handler missing: " + ctrl.GetType().Name + "." + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }

                // ---- 1. 孤号(41702/41706/41710-41714/41717/41718)禁注册反射断言:WelfareController
                //         不得存在任何这些号的 On 处理方法(严禁注册,见 Proto.cs §1 死号总清单) ----
                int[] deadNums = { 41702, 41706, 41710, 41711, 41712, 41713, 41714, 41717, 41718 };
                bool deadOk = true;
                var deadHits = new List<int>();
                foreach (int num in deadNums)
                {
                    if (welfareCtrl.GetType().GetMethod("On" + num, F) != null) { deadOk = false; deadHits.Add(num); }
                }
                Debug.Log("CLIVERIFY welfare 孤号禁注册 checked=" + deadNums.Length + " hits=[" + string.Join(",", deadHits) + "] ok=" + deadOk);

                // ---- 2. 注册线核实(Init() 后必须真的挂进 NetManager._handlers,同 CustomActCoreCase 先例) ----
                var baseWelfare = (Shenxiao.Framework.Net.BaseController)welfareCtrl;
                var baseCombat = (Shenxiao.Framework.Net.BaseController)combatCtrl;
                var baseAd = (Shenxiao.Framework.Net.BaseController)adCtrl;
                var baseGb = (Shenxiao.Framework.Net.BaseController)gbCtrl;
                if (!baseWelfare.IsInitialized) baseWelfare.Init();
                if (!baseCombat.IsInitialized) baseCombat.Init();
                if (!baseAd.IsInitialized) baseAd.Init();
                if (!baseGb.IsInitialized) baseGb.Init();

                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.WELFARE_CHECKIN_INFO, Shenxiao.Framework.Net.Proto.WELFARE_CHECKIN_CLAIM,
                    Shenxiao.Framework.Net.Proto.WELFARE_CHECKIN_RETROACTIVE, Shenxiao.Framework.Net.Proto.WELFARE_DOWNLOAD_INFO,
                    Shenxiao.Framework.Net.Proto.WELFARE_DOWNLOAD_CLAIM, Shenxiao.Framework.Net.Proto.WELFARE_ONLINE_INFO,
                    Shenxiao.Framework.Net.Proto.WELFARE_ONLINE_CLAIM, Shenxiao.Framework.Net.Proto.WELFARE_XINYUE_GIFT,
                    Shenxiao.Framework.Net.Proto.COMBAT_WELFARE_INFO, Shenxiao.Framework.Net.Proto.COMBAT_WELFARE_DRAW,
                    Shenxiao.Framework.Net.Proto.ADREWARD_REWARD_PUSH, Shenxiao.Framework.Net.Proto.ADREWARD_LIST,
                    Shenxiao.Framework.Net.Proto.ADREWARD_WATCH_CLAIM, Shenxiao.Framework.Net.Proto.ADREWARD_GRADE_PUSH,
                    Shenxiao.Framework.Net.Proto.GROWTHBENEFITS_TASK_CLAIM,
                };
                bool regOk = true;
                var missingReg = new List<int>();
                if (handlers != null)
                {
                    foreach (int id in mustBeRegistered) if (!handlers.Contains(id)) { regOk = false; missingReg.Add(id); }
                    foreach (int id in deadNums) if (handlers.Contains(id)) { regOk = false; missingReg.Add(-id); } // 负数标记"不该在却在"
                }
                else regOk = false;
                Debug.Log("CLIVERIFY welfare 注册线核实(NetManager._handlers) missing/unexpected=[" + string.Join(",", missingReg) + "] ok=" + regOk);

                // ---- 3. 41703 签到基础信息(裸;9字段双平行数组) ----
                byte[] p41703 = new CliVerify.Pkt()
                    .C(30).H(1)
                    .H(2).I(100).C(1).I(200).C(0)
                    .H(3).C(1).C(2).C(2).C(0).C(3).C(0)
                    .H(5).C(2).C(1).C(3).C(3)
                    .Bytes();
                Feed(welfareCtrl, "On41703", p41703);
                bool b41703 = model.HasCheckinInfo && model.CheckinTotalDays == 30 && model.CheckinTotalType == 1
                    && model.CheckinTotalState.Count == 2 && model.CheckinTotalState[0].Sum == 100 && model.CheckinTotalState[0].Receive == 1
                    && model.CheckinAccState.Count == 3 && model.CheckinAccState[2].CheckDay == 3
                    && model.CheckinCheckType == 5 && model.CheckinRetroTimes == 2 && model.CheckinDaysFresh == 1
                    && model.CheckinRemainTimes == 3 && model.CheckinCheckDay == 3 && model.TodayHadSign;
                Debug.Log("CLIVERIFY welfare 41703 签到信息 totalDays=" + model.CheckinTotalDays + " checkDay=" + model.CheckinCheckDay
                    + " totalStateN=" + model.CheckinTotalState.Count + " accStateN=" + model.CheckinAccState.Count + " ok=" + b41703);

                // ---- 4. 41704 签到领取(自定义 ReadFmt 裸读,Rewads/ExtraRewads 三字段均32位;成功/失败边界) ----
                int checkinResultCount = 0; int lastCheckinResultCode = -1; int lastCheckinResultProto = -1;
                System.Action<int, int> onWelfareResult = (proto, code) => { checkinResultCount++; lastCheckinResultProto = proto; lastCheckinResultCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_WELFARE_RESULT, onWelfareResult);
                byte[] p41704ok = new CliVerify.Pkt()
                    .I(1)
                    .H(1).I(0).I(38010001).I(5)
                    .H(1).I(0).I(38010002).I(1)
                    .Bytes();
                bool b41704NoThrow = true;
                try { Feed(welfareCtrl, "On41704", p41704ok); }
                catch (System.Exception e) { b41704NoThrow = false; Debug.LogError("CLIVERIFY welfare 41704 threw: " + e); }
                bool b41704Ok = checkinResultCount == 1 && lastCheckinResultProto == Shenxiao.Framework.Net.Proto.WELFARE_CHECKIN_CLAIM && lastCheckinResultCode == 1;
                byte[] p41704fail = new CliVerify.Pkt().I(99).H(0).H(0).Bytes();
                Feed(welfareCtrl, "On41704", p41704fail);
                bool b41704Fail = checkinResultCount == 2 && lastCheckinResultCode == 99;
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_WELFARE_RESULT, onWelfareResult);
                Debug.Log("CLIVERIFY welfare 41704 签到领取 noThrow=" + b41704NoThrow + " successEvt=" + b41704Ok + " failEvt=" + b41704Fail);

                // ---- 5. 41705 补签(Rewads 单数组,三字段均32位;成功/失败边界) ----
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_WELFARE_RESULT, onWelfareResult);
                checkinResultCount = 0;
                byte[] p41705ok = new CliVerify.Pkt().I(1).H(1).I(0).I(38010003).I(2).Bytes();
                Feed(welfareCtrl, "On41705", p41705ok);
                byte[] p41705fail = new CliVerify.Pkt().I(2).H(0).Bytes();
                Feed(welfareCtrl, "On41705", p41705fail);
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_WELFARE_RESULT, onWelfareResult);
                bool b41705 = checkinResultCount == 2;
                Debug.Log("CLIVERIFY welfare 41705 补签 events=" + checkinResultCount + " ok=" + b41705);

                // ---- 6. 41707 静默下载信息(标准 object_list) / 41708 领取(成功置2/失败不覆盖边界) ----
                byte[] p41707 = new CliVerify.Pkt().I(1).H(2).C(0).I(38020001).I(3).C(0).I(38020002).I(1).Bytes();
                Feed(welfareCtrl, "On41707", p41707);
                bool b41707 = model.HasDownloadInfo && model.DownloadCode == 1;
                byte[] p41708fail = new CliVerify.Pkt().I(2).Bytes();
                Feed(welfareCtrl, "On41708", p41708fail);
                bool b41708failNotOverwritten = model.DownloadCode == 1; // 失败不应把 1 改成 2
                byte[] p41708ok = new CliVerify.Pkt().I(1).Bytes();
                Feed(welfareCtrl, "On41708", p41708ok);
                bool b41708 = model.DownloadCode == 2;
                Debug.Log("CLIVERIFY welfare 41707/41708 静默下载 infoCode=" + model.DownloadCode
                    + " info=" + b41707 + " failNotOverwritten=" + b41708failNotOverwritten + " claimOk=" + b41708);

                // ---- 7. 41715 在线福利信息(裸) ----
                byte[] p41715 = new CliVerify.Pkt().H(300).I(1700000000).H(2).I(1).C(1).I(2).C(0).Bytes();
                Feed(welfareCtrl, "On41715", p41715);
                bool b41715 = model.HasOnlineInfo && model.OnlineTime == 300 && model.OnlineLoginTime == 1700000000
                    && model.OnlineList.Count == 2 && model.OnlineList[0].Id == 1 && model.OnlineList[0].State == 1;
                Debug.Log("CLIVERIFY welfare 41715 在线福利信息 time=" + model.OnlineTime + " listN=" + model.OnlineList.Count + " ok=" + b41715);

                // ---- 8. 41716 领取在线福利(二层嵌套 SendList{RewardId,Rewards(ObjectList),OtherRewards(ObjectList)},
                //         嵌套游标探针:多条目/含零长子数组均需读完不炸) ----
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_WELFARE_RESULT, onWelfareResult);
                checkinResultCount = 0;
                byte[] p41716 = new CliVerify.Pkt()
                    .I(1)
                    .H(1)
                        .I(1)
                        .H(1).C(0).I(38030001).I(2)
                        .H(1).C(0).I(38030002).I(1)
                    .Bytes();
                bool b41716NoThrow = true;
                try { Feed(welfareCtrl, "On41716", p41716); }
                catch (System.Exception e) { b41716NoThrow = false; Debug.LogError("CLIVERIFY welfare 41716 threw: " + e); }
                // 游标探针:2 条目,第2条 Rewards/OtherRewards 均为空数组,读完后必须仍能正确解析(不炸/不错位)
                byte[] p41716cursor = new CliVerify.Pkt()
                    .I(1)
                    .H(2)
                        .I(1).H(1).C(0).I(38030001).I(2).H(0)
                        .I(2).H(0).H(0)
                    .Bytes();
                bool b41716CursorNoThrow = true;
                try { Feed(welfareCtrl, "On41716", p41716cursor); }
                catch (System.Exception e) { b41716CursorNoThrow = false; Debug.LogError("CLIVERIFY welfare 41716 嵌套游标 threw: " + e); }
                byte[] p41716fail = new CliVerify.Pkt().I(3).H(0).Bytes();
                Feed(welfareCtrl, "On41716", p41716fail);
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_WELFARE_RESULT, onWelfareResult);
                bool b41716 = b41716NoThrow && b41716CursorNoThrow && checkinResultCount == 3;
                Debug.Log("CLIVERIFY welfare 41716 领取在线福利(二层嵌套游标) noThrow=" + b41716NoThrow
                    + " cursorNoThrow=" + b41716CursorNoThrow + " events=" + checkinResultCount + " ok=" + b41716);

                // ---- 9. 41719 心悦礼包(标准 object_list;code=1005/1028 抑制错误提示边界) ----
                byte[] p41719ok = new CliVerify.Pkt().I(1).C(4).C(1).H(1).C(0).I(38040001).I(1).Bytes();
                Feed(welfareCtrl, "On41719", p41719ok);
                bool b41719 = model.HasXinyueInfo && model.XinyueOpr == 4 && model.XinyueGiftSt == 1;
                logs.Clear();
                byte[] p41719suppress1 = new CliVerify.Pkt().I(1005).C(0).C(0).H(0).Bytes();
                Feed(welfareCtrl, "On41719", p41719suppress1);
                byte[] p41719suppress2 = new CliVerify.Pkt().I(1028).C(0).C(0).H(0).Bytes();
                Feed(welfareCtrl, "On41719", p41719suppress2);
                bool b41719Suppressed = !logs.Exists(l => l.Contains("心悦礼包失败")) && model.XinyueOpr == 4; // 未覆盖(仍是成功那次的值)
                byte[] p41719fail = new CliVerify.Pkt().I(2).C(0).C(0).H(0).Bytes();
                Feed(welfareCtrl, "On41719", p41719fail);
                bool b41719FailToast = logs.Exists(l => l.Contains("心悦礼包失败(2)"));
                Debug.Log("CLIVERIFY welfare 41719 心悦礼包 opr=" + model.XinyueOpr + " ok=" + b41719
                    + " suppress1005_1028=" + b41719Suppressed + " regularFailToast=" + b41719FailToast);

                // ---- 10. 41722 成长福利领取(GrowthBenefitsController 追加;成功/失败边界,反射私有 _taskStatus) ----
                Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsModel gbModel = Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsModel.Instance;
                FieldInfo taskStatusField = typeof(Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsModel).GetField("_taskStatus", F);
                var taskStatus = (Dictionary<int, int>)taskStatusField.GetValue(gbModel);
                taskStatus.Clear();
                bool bClaimTaskMethodOk = gbCtrl.GetType().GetMethod("ClaimTask", PF) != null;
                byte[] p41722ok = new CliVerify.Pkt().I(1).H(5).C(2).Bytes();
                Feed(gbCtrl, "On41722", p41722ok);
                bool b41722 = taskStatus.TryGetValue(5, out int status5) && status5 == 2;
                byte[] p41722fail = new CliVerify.Pkt().I(99).H(6).C(0).Bytes();
                Feed(gbCtrl, "On41722", p41722fail);
                bool b41722FailNotWritten = !taskStatus.ContainsKey(6);
                Debug.Log("CLIVERIFY welfare 41722 成长福利领取 taskId5Status=" + status5 + " claimMethod=" + bClaimTaskMethodOk
                    + " ok=" + b41722 + " failNotWritten=" + b41722FailNotWritten);

                // ---- 11. 41723 战力福利面板(裸;List 为裸 u16 RewardId) / 41724 摇奖(同轮/换轮/失败边界) ----
                Shenxiao.Module.Core.Welfare.CombatWelfareController combat = (Shenxiao.Module.Core.Welfare.CombatWelfareController)combatCtrl;
                byte[] p41723 = new CliVerify.Pkt().C(3).C(5).L(165000).L(260000).H(2).H(10).H(20).Bytes();
                Feed(combatCtrl, "On41723", p41723);
                bool b41723 = combat.HasInfo && combat.Round == 3 && combat.Times == 5 && combat.Combat == 165000
                    && combat.NextCombat == 260000 && combat.ClaimedRewardIds.Count == 2
                    && combat.IsRewardClaimed(10) && combat.IsRewardClaimed(20) && !combat.IsRewardClaimed(99);
                Debug.Log("CLIVERIFY welfare 41723 战力福利面板 round=" + combat.Round + " claimedN=" + combat.ClaimedRewardIds.Count + " ok=" + b41723);

                byte[] p41724sameRound = new CliVerify.Pkt().I(1).C(3).C(4).H(30).L(300000).Bytes();
                Feed(combatCtrl, "On41724", p41724sameRound);
                bool b41724SameRound = combat.Round == 3 && combat.Times == 4 && combat.NextCombat == 300000
                    && combat.ClaimedRewardIds.Count == 3 && combat.IsRewardClaimed(30);
                byte[] p41724newRound = new CliVerify.Pkt().I(1).C(4).C(1).H(40).L(400000).Bytes();
                Feed(combatCtrl, "On41724", p41724newRound);
                // m4修复:老端换轮分支(is_new==true,GrowthBenefitsController.ts:208-217)只清空+切轮,
                // 不把本次抽中的 reward_id 记入新一轮已领集合(fight_welfare_list[scmd.reward_id]=1 只在
                // else 分支执行)——本端此前无条件 Add 是错误镜像,修复后换轮那一次的 40 号不应计入。
                bool b41724NewRound = combat.Round == 4 && combat.Times == 1 && combat.NextCombat == 400000
                    && combat.ClaimedRewardIds.Count == 0 && !combat.IsRewardClaimed(40) && !combat.IsRewardClaimed(30);
                byte[] p41724fail = new CliVerify.Pkt().I(5).C(9).C(9).H(999).L(999999).Bytes();
                Feed(combatCtrl, "On41724", p41724fail);
                bool b41724FailNotOverwritten = combat.Round == 4 && combat.Times == 1 && combat.ClaimedRewardIds.Count == 0;
                bool b41724 = b41724SameRound && b41724NewRound && b41724FailNotOverwritten;
                Debug.Log("CLIVERIFY welfare 41724 战力福利摇奖 sameRound=" + b41724SameRound + " newRound=" + b41724NewRound
                    + " failNotOverwritten=" + b41724FailNotOverwritten + " ok=" + b41724);

                // ---- 12. 19301 广告奖励推送 / 19302 广告列表 / 19303 观看领取回执 / 19304 档位变更(占位) ----
                Shenxiao.Module.Core.AdReward.AdRewardModel adModel = Shenxiao.Module.Core.AdReward.AdRewardModel.Instance;
                int adUpdateCount = 0; var adUpdateProtos = new List<int>();
                System.Action<int> onAdUpdate = proto => { adUpdateCount++; adUpdateProtos.Add(proto); };
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_ADREWARD_UPDATE, onAdUpdate);
                byte[] p19301 = new CliVerify.Pkt().H(1).C(0).I(38050001).I(2).Bytes();
                bool b19301NoThrow = true;
                try { Feed(adCtrl, "On19301", p19301); }
                catch (System.Exception e) { b19301NoThrow = false; Debug.LogError("CLIVERIFY welfare 19301 threw: " + e); }
                byte[] p19302 = new CliVerify.Pkt().H(2).I(427).I(1).C(3).I(610).I(5).C(0).Bytes();
                Feed(adCtrl, "On19302", p19302);
                bool b19302 = adModel.HasList && adModel.AdList.Count == 2 && adModel.GetLookState(427, 1) && !adModel.GetLookState(610, 5);
                logs.Clear();
                byte[] p19303ok = new CliVerify.Pkt().I(427).I(1).I(0).I(1).Bytes();
                Feed(adCtrl, "On19303", p19303ok);
                byte[] p19303fail = new CliVerify.Pkt().I(427).I(1).I(0).I(2).Bytes();
                Feed(adCtrl, "On19303", p19303fail);
                bool b19303 = !logs.Exists(l => l.Contains("领取失败(1)")) && logs.Exists(l => l.Contains("领取失败(2)"));
                byte[] p19304 = new CliVerify.Pkt().I(610).I(2).I(5).Bytes();
                Feed(adCtrl, "On19304", p19304);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_ADREWARD_UPDATE, onAdUpdate);
                bool b19301 = b19301NoThrow && adUpdateProtos.Contains(Shenxiao.Framework.Net.Proto.ADREWARD_REWARD_PUSH);
                bool b19304 = adUpdateProtos.Contains(Shenxiao.Framework.Net.Proto.ADREWARD_GRADE_PUSH);
                bool bAdOpenStateClosed = !adModel.GetAdOpenState(); // Unity 无 Eyou 平台信号,恒 false(见 AdRewardModel 注释)
                Debug.Log("CLIVERIFY welfare 广告19301-04 push=" + b19301 + " list=" + b19302 + " watchClaim=" + b19303
                    + " gradePush=" + b19304 + " adOpenClosed=" + bAdOpenStateClosed + " updateEvents=" + adUpdateCount);

                // ---- 13. 发送方法 no-throw(签到/在线/心悦/战力福利/广告) ----
                bool sendNoThrow = true;
                try
                {
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).RequestCheckinInfo();
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).ClaimCheckin(3, 0);
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).RetroactiveCheckin(2);
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).RequestDownloadInfo();
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).ClaimDownload();
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).RequestOnlineInfo();
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).ClaimOnline(1);
                    ((Shenxiao.Module.Core.Welfare.WelfareController)welfareCtrl).RequestXinyueGift(4);
                    combat.RequestInfo();
                    combat.Draw();
                    ((Shenxiao.Module.Core.AdReward.AdRewardController)adCtrl).RequestList();
                    ((Shenxiao.Module.Core.AdReward.AdRewardController)adCtrl).WatchClaim(427, 1, 0);
                    ((Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsController)gbCtrl).ClaimTask(5);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY welfare 发送方法 threw: " + e); }
                Debug.Log("CLIVERIFY welfare 发送方法 noThrow=" + sendNoThrow);

                // ---- 14. CHANGE_LEVEL 精确命中门槛补发 41715(对标老端 ts:467-470,config_welfare_cfg["3"]=75) ----
                logs.Clear();
                Shenxiao.Module.Core.Role.RoleModel.Instance.MarkBaseInfoReady();
                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 75;
                Shenxiao.Framework.Event.EventDispatcher.Emit(Shenxiao.Framework.Event.GlobalEvent.EVT_ROLE_INFO_UPDATE);
                bool bChangeLevelResend = logs.Exists(l => l.Contains("send while disconnected: proto=" + Shenxiao.Framework.Net.Proto.WELFARE_ONLINE_INFO));
                Debug.Log("CLIVERIFY welfare CHANGE_LEVEL 精确命中(lv=75)补发41715 resendAttempted=" + bChangeLevelResend);

                bool pass = configOk && deadOk && regOk
                    && b41703 && b41704NoThrow && b41704Ok && b41704Fail && b41705
                    && b41707 && b41708failNotOverwritten && b41708
                    && b41715 && b41716
                    && b41719 && b41719Suppressed && b41719FailToast
                    && bClaimTaskMethodOk && b41722 && b41722FailNotWritten
                    && b41723 && b41724
                    && b19301 && b19302 && b19303 && b19304 && bAdOpenStateClosed
                    && sendNoThrow && bChangeLevelResend;

                Debug.Log("CLIVERIFY welfare VERDICT config=" + configOk + " dead=" + deadOk + " reg=" + regOk
                    + " checkin(703/704/705)=" + (b41703 && b41704Ok && b41704Fail && b41705)
                    + " download(707/708)=" + (b41707 && b41708) + " online(715/716)=" + (b41715 && b41716)
                    + " xinyue719=" + b41719 + " growthTask41722=" + b41722 + " combatWelfare(723/724)=" + (b41723 && b41724)
                    + " ad(19301-04)=" + (b19301 && b19302 && b19303 && b19304) + " sendNoThrow=" + sendNoThrow
                    + " changeLevelResend=" + bChangeLevelResend + " pass=" + pass);

                model.Reset();
                baseWelfare.Dispose();
                baseCombat.Dispose();
                baseAd.Dispose();
                // GrowthBenefitsController 是既有常驻模块(ControllerHub 长期持有),不在此 Dispose,
                // 避免影响同批次跑在它后面的其它 Case(同 DailyHubCase 对既有模块的克制先例)。
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
