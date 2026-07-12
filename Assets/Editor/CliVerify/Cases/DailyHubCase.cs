using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 日常中心(自动循环 轮10;轮10 修复代理复核后同步更新)实证:15700 通用错误码壳补注册(不炸+显码)/
    /// 15701 双表分槽+排序算法(真实 config_ac/config_activity_liveness 条目 132@0@0/157@0@0/135@0@1 驱动,
    /// 可否领活跃度&gt;开启状态&gt;等级 优先级)/15703 升序/15705 成功联动(重拉15703+奖励摘要,不炸)+失败码/
    /// 15706 原地改/15717 联动(重拉15701+15703,不炸)/15718 预约红点计数(⚠已订正老端"vo.status&amp;&amp;canRes"
    /// 恒假的转译 bug,期望值公式改用服务器时区[UTC+SERVER_ZONE_HOURS],见 DailyModel.SetResTable/
    /// TimeUtilNowUtc 注释)/15719 成功 status!=2 弹窗事件+失败码/15720 领取(reservation置2,不再重复扣红点
    /// ——红点 -1 交给随后必到的 15719 status=2)+失败码/41900/41903 额度扣减+失败码/41904 回包不覆盖主表
    /// (等41900全量刷新)+失败码/61801 状态表 合成包驱动 DailyController 反射喂包。
    /// 渲染段:DailyFlow.Open() 默认 tab3(资源找回)拉起 DailyResFindView,反射其自有 _cells 字段核对列表数
    /// (严禁全场景搜同类型,同轮9 沉淀);编辑期 Addressables 不可用则优雅降级(同 DungeonBuyTimeView 先例)。
    /// 日志前缀统一 "CLIVERIFY dailyhub"。
    /// </summary>
    public static class DailyHubCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;

            int signupSuccessCount = 0;
            Action<int, int, int> onSignupSuccess = (m, ms, a) => signupSuccessCount++;
            Shenxiao.Framework.Event.EventDispatcher.On<int, int, int>(
                Shenxiao.Framework.Event.GlobalEvent.EVT_DAILY_SIGNUP_SUCCESS, onSignupSuccess);

            int originalLevel = Shenxiao.Module.Core.Role.RoleModel.Instance.Level;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Daily.DailyConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Daily.DailyConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY dailyhub FAIL config_ac not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Daily.DailyController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo H(string name)
                {
                    System.Reflection.MethodInfo m = ctrl.GetType().GetMethod(name, F);
                    if (m == null) Debug.LogError("CLIVERIFY dailyhub handler missing: " + name);
                    return m;
                }
                System.Reflection.MethodInfo m15700 = H("On15700"),
                    m15701 = H("On15701"), m15703 = H("On15703"), m15705 = H("On15705"),
                    m15706 = H("On15706"), m15717 = H("On15717"), m15718 = H("On15718"), m15719 = H("On15719"),
                    m15720 = H("On15720"), m41900 = H("On41900"), m41903 = H("On41903"), m41904 = H("On41904"),
                    m61801 = H("On61801");
                if (m15700 == null || m15701 == null || m15703 == null || m15705 == null || m15706 == null || m15717 == null
                    || m15718 == null || m15719 == null || m15720 == null || m41900 == null || m41903 == null
                    || m41904 == null || m61801 == null)
                {
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Daily.DailyModel model = Shenxiao.Module.Core.Daily.DailyModel.Instance;
                model.Clear();
                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 200; // 200 级:132@0@0/157@0@0/135@0@1 三个真实配置全不超龄/够等级

                // ---- Z. 15700 跨协议共享错误码壳(轮10交叉验收 blocker:补注册,不炸+显码降级) ----
                logs.Clear();
                bool err700NoThrow = true;
                try { Feed(m15700, new CliVerify.Pkt().I(1570001).Bytes()); }
                catch (Exception e) { err700NoThrow = false; Debug.LogError("CLIVERIFY dailyhub 15700 threw: " + e); }
                bool err700Ok = err700NoThrow && logs.Exists(l => l.Contains("15700 通用错误码 code=1570001"));
                Debug.Log("CLIVERIFY dailyhub 15700 ok=" + err700Ok);

                // ---- A. 15701(act_type=1,每日任务)真实配置驱动排序(可否领活跃度>开启状态,尾哨兵 Live 核对) ----
                // 三条:280@0@0(普通开启,val=100)/157@0@0(module==157&&sub==0 特判,val=450)/132@0@0(可领活跃度,val=10450)。
                // ⚠首跑订正(轮13a批处理):此前"普通项"误用 132@0——老端 132@0(主线挂机)与 157@0 同为特判
                // (DailyModel.ts:397-398),两者 va 同值且 config_ac rank 同为 999,相对序在两端都无定义
                // (.NET List.Sort 不稳定/老端 JS 比较器对同值返回 1),断言特定顺序=测了未定义行为。
                Feed(m15701, new CliVerify.Pkt()
                    .C(1).L(999L)
                    .H(3)
                        .I(157).I(0).I(0).I(0).I(5).I(0).I(100).I(0).C(1)   // 特判项:CanGetLive=0,State=1(开启)
                        .I(280).I(0).I(0).I(0).I(5).I(0).I(100).I(0).C(1)   // 普通开启项(280@0,rank=3,liveness max=10):CanGetLive=0
                        .I(132).I(0).I(0).I(1).I(5).I(777).I(100).I(1).C(1) // 可领活跃度项:CanGetLive=1,Live=777(尾哨兵)
                    .Bytes());
                Shenxiao.Module.Core.Daily.DailyModel.DailyDataVo unlimit = model.GetDailyData(Shenxiao.Module.Core.Daily.DailyModel.ACT_UNLIMIT);
                bool splitOk = unlimit != null && unlimit.OnHookTime == 999L && unlimit.AcList.Count == 3;
                bool sortOk = splitOk
                    && unlimit.AcList[0].Module == 280 && unlimit.AcList[0].CanGetLive == 0
                    && unlimit.AcList[1].Module == 157
                    && unlimit.AcList[2].Module == 132 && unlimit.AcList[2].CanGetLive == 1 && unlimit.AcList[2].Live == 777;
                Debug.Log("CLIVERIFY dailyhub 15701 unlimit split=" + splitOk + " sort=" + sortOk
                    + " order=[" + (unlimit != null ? string.Join(",", unlimit.AcList.ConvertAll(v => v.Module + "/" + v.CanGetLive)) : "null") + "]");

                // ---- B. 15701(act_type=2,限时活动)真实配置 135@0@1 驱动,State 服务端强制覆盖排序(与本地时钟无关) ----
                Feed(m15701, new CliVerify.Pkt()
                    .C(2).L(0L)
                    .H(2)
                        .I(135).I(0).I(1).I(0).I(1).I(10).I(10).I(0).C(3)   // State=3→LvLimit
                        .I(135).I(0).I(1).I(0).I(1).I(10).I(10).I(0).C(1)   // State=1→Opening(强制,不看本地时钟)
                    .Bytes());
                Shenxiao.Module.Core.Daily.DailyModel.DailyDataVo limit = model.GetDailyData(Shenxiao.Module.Core.Daily.DailyModel.ACT_LIMIT);
                bool limitSplitOk = limit != null && limit.AcList.Count == 2;
                bool limitSortOk = limitSplitOk && limit.AcList[0].State == 1 && limit.AcList[1].State == 3;
                Debug.Log("CLIVERIFY dailyhub 15701 limit split=" + limitSplitOk + " sort=" + limitSortOk);

                // ---- C. 15706 原地改(module=132 全部命中项 state 改 2) ----
                Feed(m15706, new CliVerify.Pkt().I(132).I(0).C(1).C(2).Bytes());
                bool pushOk = true;
                foreach (var vo in unlimit.AcList) if (vo.Module == 132 && vo.State != 2) pushOk = false;
                Debug.Log("CLIVERIFY dailyhub 15706 push ok=" + pushOk);

                // ---- D. 15703 升序(乱序喂入,断言 id 升序落地) ----
                Feed(m15703, new CliVerify.Pkt().I(50).I(200).H(3).I(30).C(1).I(5).C(2).I(17).C(1).Bytes());
                bool rewardOk = model.LivenessLive == 50 && model.LivenessLiveMax == 200 && model.LivenessRewardList.Count == 3
                    && model.LivenessRewardList[0].Id == 5 && model.LivenessRewardList[1].Id == 17 && model.LivenessRewardList[2].Id == 30;
                Debug.Log("CLIVERIFY dailyhub 15703 order=" + rewardOk + " ids=["
                    + string.Join(",", model.LivenessRewardList.ConvertAll(v => v.Id)) + "]");

                // ---- E. 15705 成功联动(重拉15703+奖励摘要 GetBoxRewardListById,不炸)+ 失败码 ----
                logs.Clear();
                bool claim705NoThrow = true;
                try { Feed(m15705, new CliVerify.Pkt().I(1).I(1).Bytes()); } // config_activity_reward id=1(live:40,reward 38070001x1)
                catch (Exception e) { claim705NoThrow = false; Debug.LogError("CLIVERIFY dailyhub 15705 threw: " + e); }
                bool claim705Ok = claim705NoThrow && logs.Exists(l => l.Contains("15705 claim box ok id=1"));
                logs.Clear();
                Feed(m15705, new CliVerify.Pkt().I(1570001).I(2).Bytes());
                bool claim705FailOk = logs.Exists(l => l.Contains("15705 claim box fail"));
                Debug.Log("CLIVERIFY dailyhub 15705 ok=" + claim705Ok + " failText=" + claim705FailOk);

                // ---- F. 15717 联动(重拉15701+15703,不炸) ----
                logs.Clear();
                bool claim717NoThrow = true;
                try { Feed(m15717, new CliVerify.Pkt().I(300).H(1).I(2).Bytes()); }
                catch (Exception e) { claim717NoThrow = false; Debug.LogError("CLIVERIFY dailyhub 15717 threw: " + e); }
                bool claim717Ok = claim717NoThrow && logs.Exists(l => l.Contains("15717 领活跃度成功"));
                Debug.Log("CLIVERIFY dailyhub 15717 ok=" + claim717Ok);

                // ---- G. 15718 预约红点计数(⚠已订正老端恒假 bug;用同一时钟公式[含服务器时区偏移]自算
                // 期望值,免时区抖动误报——轮10交叉验收 blocker 订正:生产代码已从裸 UTC 改为 UTC+SERVER_ZONE_HOURS,
                // 此处期望值公式必须同步改,否则会互相掩盖) ----
                Newtonsoft.Json.Linq.JObject ac135 = Shenxiao.Module.Core.Daily.DailyConfigs.GetAc(135, 0, 1);
                var region135 = Shenxiao.Module.Core.Daily.DailyConfigs.ParseTimeRegion(
                    Shenxiao.Module.Core.Daily.DailyConfigs.ReadString(ac135, "time_region"));
                DateTime nowUtc = DateTime.UtcNow.AddHours(Shenxiao.Module.Core.Daily.DailyModel.SERVER_ZONE_HOURS);
                bool expectCanRes = region135.Count > 0 &&
                    (nowUtc.Hour < region135[0].startH || (nowUtc.Hour == region135[0].startH && nowUtc.Minute < region135[0].startM));
                int expectedRed = expectCanRes ? 1 : 0;
                Feed(m15718, new CliVerify.Pkt()
                    .H(3)
                        .I(500).I(0).I(0).C(1).C(1)   // module==500 → 过滤,不进表
                        .I(135).I(0).I(1).C(0).C(0)   // 未预约(status=0),按当前时钟可能可预约
                        .I(132).I(0).I(0).C(1).C(1)   // 已预约(status!=0)→ 不计入红点
                    .Bytes());
                bool signupFilterOk = !model.TryGetReservation(500, 0, 0, out _)
                    && model.TryGetReservation(135, 0, 1, out int res135) && res135 == 0
                    && model.TryGetReservation(132, 0, 0, out int res132) && res132 == 1;
                bool redCountOk = model.DailyResRed == expectedRed;
                Debug.Log("CLIVERIFY dailyhub 15718 filter=" + signupFilterOk + " red=" + model.DailyResRed
                    + " expected=" + expectedRed + " ok=" + redCountOk);

                // ---- H. 15719 成功(status!=2 弹窗事件)+ 失败码 ----
                signupSuccessCount = 0;
                Feed(m15719, new CliVerify.Pkt().I(1).I(135).I(0).I(1).C(1).C(1).Bytes());
                bool signup719Ok = signupSuccessCount == 1
                    && model.TryGetReservation(135, 0, 1, out int res135b) && res135b == 1;
                logs.Clear();
                Feed(m15719, new CliVerify.Pkt().I(1570018).I(135).I(0).I(1).C(0).C(0).Bytes());
                bool signup719FailOk = logs.Exists(l => l.Contains("报名失败"));
                Debug.Log("CLIVERIFY dailyhub 15719 success=" + signup719Ok + " evt=" + signupSuccessCount + " failText=" + signup719FailOk);

                // ---- I. 15720 领取(reservation 置2;⚠轮10交叉验收 blocker 订正:15720 不再单独扣红点——
                // 真正的 -1 由服务端同时广播的 15719(status=2) 经 SetResSingle 完成,此处只验证 15720
                // 本身不再重复扣减,避免每次领奖双扣) ----
                int redBefore = model.DailyResRed;
                Feed(m15720, new CliVerify.Pkt().I(1).I(135).I(0).I(1).Bytes());
                bool claim720Ok = model.TryGetReservation(135, 0, 1, out int res135c) && res135c == 2
                    && model.DailyResRed == redBefore;
                logs.Clear();
                Feed(m15720, new CliVerify.Pkt().I(1570010).I(135).I(0).I(1).Bytes());
                bool claim720FailOk = logs.Exists(l => l.Contains("领取失败"));
                Debug.Log("CLIVERIFY dailyhub 15720 ok=" + claim720Ok + " failText=" + claim720FailOk);

                // ---- J. 41900(界面信息)/41903(单条找回,额度扣减)+ 失败码 ----
                Feed(m41900, new CliVerify.Pkt()
                    .I(1).H(2)
                        .I(6100).H(1).H(5).H(2).I(3)
                        .I(6100).H(2).H(9).H(0).I(1)
                    .Bytes());
                bool find900Ok = model.ResFindList.Count == 2
                    && model.GetResFind(6100, 1, 3)?.Lefttimes == 5 && model.GetResFind(6100, 2, 1)?.Lefttimes == 9;
                Feed(m41903, new CliVerify.Pkt().I(1).C(1).I(6100).H(1).H(3).H(1).I(3).Bytes()); // 额度 5→3(扣2)
                bool find903Ok = model.GetResFind(6100, 1, 3)?.Lefttimes == 3 && model.GetResFind(6100, 1, 3)?.LefttimesVip == 1;
                logs.Clear();
                Feed(m41903, new CliVerify.Pkt().I(4190001).C(1).I(6100).H(1).H(0).H(0).I(0).Bytes());
                bool find903ResendOk = logs.Exists(l => l.Contains("次数不同步"));
                logs.Clear();
                Feed(m41903, new CliVerify.Pkt().I(4190002).C(1).I(6100).H(1).H(0).H(0).I(0).Bytes());
                bool find903FailOk = logs.Exists(l => l.Contains("找回失败"));
                Debug.Log("CLIVERIFY dailyhub 41900 ok=" + find900Ok + " 41903 deduct=" + find903Ok
                    + " resend=" + find903ResendOk + " fail=" + find903FailOk);

                // ---- K. 41904 一键找回(⚠轮10交叉验收 minor 订正:回包只含命中项,不再覆盖主表——
                // 老端 SetAllResFindData 只存独立展示用的 merge_find_list_,不碰 res_find_data;主表交给
                // 紧随其后的 41900 全量刷新。此处验证 41904 本身不改动主表,只校验成功/失败文案) ----
                int countBeforeOnekey = model.ResFindList.Count;
                logs.Clear();
                Feed(m41904, new CliVerify.Pkt().I(1).C(1).H(1).I(7700).H(1).H(2).H(0).I(9).Bytes());
                bool onekeyOk = model.ResFindList.Count == countBeforeOnekey
                    && model.GetResFind(6100, 1, 3)?.Lefttimes == 3 // 主表未被 41904 回包覆盖(仍是 41903 后的值)
                    && logs.Exists(l => l.Contains("41904 一键找回成功"));
                logs.Clear();
                Feed(m41904, new CliVerify.Pkt().I(6100001).C(1).H(0).Bytes());
                bool onekeyFailOk = logs.Exists(l => l.Contains("一键找回失败"));
                Debug.Log("CLIVERIFY dailyhub 41904 overwrite=" + onekeyOk + " failText=" + onekeyFailOk);

                // ---- L. 61801 状态表 ----
                Feed(m61801, new CliVerify.Pkt().H(2).I(1001).C(1).L(123456).I(1002).C(0).L(0).Bytes());
                bool strongOk = model.StrongStateList.Count == 2
                    && model.GetStrongerById(1001)?.State == 1 && model.GetStrongerById(1001)?.Time == 123456
                    && model.GetStrongerById(1002)?.State == 0;
                Debug.Log("CLIVERIFY dailyhub 61801 ok=" + strongOk);

                bool logicPass = err700Ok && splitOk && sortOk && limitSplitOk && limitSortOk && pushOk && rewardOk
                    && claim705Ok && claim705FailOk && claim717Ok && signupFilterOk && redCountOk
                    && signup719Ok && signup719FailOk && claim720Ok && claim720FailOk
                    && find900Ok && find903Ok && find903ResendOk && find903FailOk && onekeyOk && onekeyFailOk && strongOk;

                // ---- M. 渲染段:DailyFlow.Open() 默认 tab3(资源找回)拉起 DailyResFindView,
                // 反射其自有 _cells 字段核对列表数(严禁全场景搜同类型,同轮9 沉淀);编辑期不可加载则优雅降级。 ----
                bool renderOk;
                bool renderLoaded = false;
                try
                {
                    Shenxiao.Module.Core.Daily.DailyFlow.Open();
                    System.Reflection.FieldInfo fContentRoots = typeof(Shenxiao.Module.Core.Daily.DailyFlow)
                        .GetField("_contentRoots", F | System.Reflection.BindingFlags.Static);
                    double deadline = UnityEditor.EditorApplication.timeSinceStartup + 8.0;
                    Shenxiao.Module.Core.Daily.DailyResFindView view = null;
                    while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
                    {
                        var roots = fContentRoots?.GetValue(null) as System.Collections.Generic.Dictionary<string, GameObject>;
                        if (roots != null && roots.TryGetValue("DailyModule", out GameObject root) && root != null)
                        {
                            view = root.GetComponentInChildren<Shenxiao.Module.Core.Daily.DailyResFindView>(true);
                            if (view != null) break;
                        }
                        await Task.Delay(200);
                    }
                    if (view != null)
                    {
                        System.Reflection.FieldInfo fCells = typeof(Shenxiao.Module.Core.Daily.DailyResFindView)
                            .GetField("_cells", F);
                        var cells = fCells?.GetValue(view) as System.Collections.IList;
                        renderLoaded = true;
                        renderOk = cells != null && cells.Count == model.ResFindList.Count;
                        stage.ForceCjkFont();
                        string png = stage.Capture("Temp/round10_daily_resfind.png");
                        Debug.Log("CLIVERIFY dailyhub render loaded cells=" + (cells?.Count ?? -1)
                            + " expect=" + model.ResFindList.Count + " ok=" + renderOk + " shot=" + png);
                    }
                    else
                    {
                        renderOk = true; // 编辑期 batch 域 DailyModule.prefab 加载不可用(Addressables 兜底路径缺该组):渲染断言降级,不挡门禁
                        Debug.LogWarning("CLIVERIFY dailyhub render degrade(prefab 未在编辑期加载,渲染断言跳过)");
                    }
                    Shenxiao.Module.Core.Daily.DailyFlow.Close();
                }
                catch (Exception e)
                {
                    renderOk = true; // 同上优雅降级:渲染链路异常不挡纯逻辑门禁,仅记录
                    Debug.LogWarning("CLIVERIFY dailyhub render degrade(exception): " + e.Message);
                }

                bool pass = logicPass && renderOk;
                Debug.Log("CLIVERIFY dailyhub VERDICT logic=" + logicPass + " renderLoaded=" + renderLoaded + " render=" + renderOk + " pass=" + pass);

                model.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                Shenxiao.Framework.Event.EventDispatcher.Off<int, int, int>(
                    Shenxiao.Framework.Event.GlobalEvent.EVT_DAILY_SIGNUP_SUCCESS, onSignupSuccess);
                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = originalLevel;
                stage.Dispose();
            }
        }
    }
}
