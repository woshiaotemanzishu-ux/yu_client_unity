using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Boss 家族二期·跨服族(自动循环 轮15b)实证:pt_470 千幻蜃楼主链(列表/进出场景/关注/47006复活提醒
    /// 含服务端误发壳量/47008防御recv/47009-47023宝箱狩猎榜单/47034死亡debuff转ReliveModel.HolyBoss/47035
    /// 复活)+ pt_471 镇煞封魂全链(47101主信息/47102-47104进出购买/47105-47106关注/47107复活提醒/47108
    /// 掉落/47109·47112排名全量与patch/47113双套奖励结算/47114-47116场景信息/47117防御recv)+ pt_619 论剑
    /// 恩怨簿(61900全量+61901/61902增量,含32位ServerId独例)+ pt_460 kf_great_demon 壳(46037-39/46046)+
    /// 死号未注册断言(47001/47011)+ 边界失败码,合成包驱动 KfBossController 反射喂包(模板 BossCase,纯逻辑段)。
    ///
    /// UI 层:CrossServerEnterView(7 Tab 入口壳)与千幻蜃楼 eudaemon 视图均无 Bind 供给(convert-module 未做,
    /// Eudaemon*/BossDomain* 目录虽有 Bind 但宿主 Tab 容器缺失),本轮无处安放渲染段,不写(同 RankCase/BossCase
    /// 先例:纯数据层轮无渲染段)。
    /// </summary>
    public static class KfBossCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Boss.KfBossConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Boss.KfBossConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY kfboss FAIL KfBossConfigs not loaded");
                    return 3;
                }
                bool configOk = Shenxiao.Module.Core.Boss.KfBossConfigs.EudemonsBossCfgCount == 48
                    && Shenxiao.Module.Core.Boss.KfBossConfigs.KfGreatDemonCount == 31;
                Debug.Log("CLIVERIFY kfboss config eudemonsBossCfg=" + Shenxiao.Module.Core.Boss.KfBossConfigs.EudemonsBossCfgCount
                    + " kfGreatDemon=" + Shenxiao.Module.Core.Boss.KfBossConfigs.KfGreatDemonCount + " ok=" + configOk);

                Shenxiao.Module.Core.Boss.KfBossModel model = Shenxiao.Module.Core.Boss.KfBossModel.Instance;
                model.Clear();
                Shenxiao.Module.Core.Relive.ReliveModel.Instance.Clear();

                object ctrl = Shenxiao.Module.Core.Boss.KfBossController.Instance;
                System.Type t = ctrl.GetType();
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY kfboss handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }

                // ---- A. 死号未注册断言(47001发送侧死号/47011整链路死代码) ----
                bool deadOk = t.GetMethod("On47001", F) == null && t.GetMethod("On47011", F) == null;
                Debug.Log("CLIVERIFY kfboss 死号未注册断言 ok=" + deadOk);

                // ---- B. Mystery=20 订正后类型门回归(与 BossCase 46009 断言同轮跟随,此处复核订正落地) ----
                bool mysteryDoorOk = Shenxiao.Module.Core.Boss.BossModel.ShouldNotifyKillBoss(
                        Shenxiao.Module.Core.Boss.BossModel.BossType.Mystery)      // 16,双收
                    && Shenxiao.Module.Core.Boss.BossModel.ShouldNotifyKillBoss(
                        Shenxiao.Module.Core.Boss.BossModel.BossType.KfGreatDemon); // 20,老端真值,主收
                Debug.Log("CLIVERIFY kfboss mysteryDoor(16&&20) ok=" + mysteryDoorOk);

                // ---- C. 47000 千幻蜃楼列表(rawType=1=holy,client类型=1001;Tired/MaxTired均8位) ----
                const int HOLY = Shenxiao.Module.Core.Boss.KfBossModel.BossType.Holy; // 1001
                byte[] p47000 = new CliVerify.Pkt().C(1).C(0).I(0).C(5).C(20)
                    .H(1).C(3).C(2).C(10) // CollectList: 1条 {type=3,collect=2,total=10}
                    .H(2)
                        .I(2101001).C(1).I(0).C(1)
                        .I(2101002).C(0).I(1700010000).C(0)
                    .Bytes();
                Feed("On47000", p47000);
                var holy = model.GetEudemons(HOLY);
                bool b47000 = holy != null && holy.HasData && holy.Tired == 5 && holy.MaxTired == 20
                    && holy.BossList.Count == 2 && holy.GetEntry(2101001).IsAlive && !holy.GetEntry(2101002).IsAlive
                    && holy.CollectList.Count == 1 && holy.CollectList[0].Type == 3 && holy.CollectList[0].TotalCollectTimes == 10;
                Debug.Log("CLIVERIFY kfboss 47000 list tired=" + (holy?.Tired ?? -1) + "/" + (holy?.MaxTired ?? -1)
                    + " bossN=" + (holy?.BossList.Count ?? -1) + " ok=" + b47000);

                // ---- D. 47003/47004 进出千幻蜃楼(成功+失败边界码) ----
                int enterCount = 0; bool lastEnterOk = false;
                System.Action<bool, int> onEnter = (isEnter, code) => { enterCount++; lastEnterOk = code == 1; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_EUDEMONS_ENTER_RESULT, onEnter);
                Feed("On47003", new CliVerify.Pkt().I(1).Bytes());
                bool b47003ok = lastEnterOk;
                Feed("On47003", new CliVerify.Pkt().I(4700008).Bytes()); // err470_in_team,边界失败码
                bool b47003fail = !lastEnterOk;
                Feed("On47004", new CliVerify.Pkt().I(1).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_EUDEMONS_ENTER_RESULT, onEnter);
                bool b47003_04 = enterCount == 3 && b47003ok && b47003fail;
                Debug.Log("CLIVERIFY kfboss 47003/04 enter/leave events=" + enterCount + " ok=" + b47003_04);

                // ---- E. 47005 关注(cross变体少AutoRemind) ----
                Feed("On47005", new CliVerify.Pkt().I(1).C(1).I(2101001).C(1).Bytes());
                bool b47005 = holy.GetEntry(2101001).IsRemind;
                Feed("On47005", new CliVerify.Pkt().I(4700001).C(1).I(2101001).C(0).Bytes()); // err470_boss_not_open,边界失败码,不抛即过
                Debug.Log("CLIVERIFY kfboss 47005 remind ok=" + b47005);

                // ---- F. 47006 复活提醒(含服务端误发壳量:rawType=20→clientType=1020,验证不特殊处理仍正常到达) ----
                int rebornTipCount = 0; int lastTipType = 0, lastTipBossId = 0;
                System.Action<int, int> onTip = (bt, bi) => { rebornTipCount++; lastTipType = bt; lastTipBossId = bi; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_EUDEMONS_REBORN_TIP, onTip);
                Feed("On47006", new CliVerify.Pkt().C(1).I(2101001).Bytes());
                bool b47006Holy = rebornTipCount == 1 && lastTipType == HOLY && lastTipBossId == 2101001;
                Feed("On47006", new CliVerify.Pkt().C(20).I(430001).Bytes()); // kf_great_demon(20)误发壳量quirk
                bool b47006Quirk = rebornTipCount == 2 && lastTipType == 20 + 1000 && lastTipBossId == 430001;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_EUDEMONS_REBORN_TIP, onTip);
                bool b47006 = b47006Holy && b47006Quirk;
                Debug.Log("CLIVERIFY kfboss 47006 reborn tip holy=" + b47006Holy + " quirk(20misfire)=" + b47006Quirk + " ok=" + b47006);

                // ---- G. 47007 击杀信息(holy类型触发共享 EVT_BOSS_REBORN;RebornTime upsert)+ 47008 防御recv(不抛) ----
                int killBossCount = 0;
                System.Action<int, int> onKillBoss = (bt, bi) => killBossCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_REBORN, onKillBoss);
                Feed("On47007", new CliVerify.Pkt().C(1).I(2101002).I(1700020000).C(1).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_BOSS_REBORN, onKillBoss);
                bool b47007 = killBossCount == 1 && holy.GetEntry(2101002).RebornTime == 1700020000 && holy.GetEntry(2101002).IsAlive;
                bool b47008NoThrow = true;
                try { Feed("On47008", new CliVerify.Pkt().C(1).I(2101002).I(0).C(1).Bytes()); }
                catch (System.Exception e) { b47008NoThrow = false; Debug.LogError("CLIVERIFY kfboss 47008 threw: " + e); }
                Debug.Log("CLIVERIFY kfboss 47007 killBossFired=" + (killBossCount == 1) + " rebornUpsert=" + b47007 + " 47008noThrow=" + b47008NoThrow);

                // ---- H. 47009疲劳/47017-18宝箱坐标/47019狩猎等级/47021榜单/47023最大疲劳 ----
                Feed("On47009", new CliVerify.Pkt().C(15).Bytes());
                bool b47009 = holy.Tired == 15;

                byte[] p47017 = new CliVerify.Pkt().H(1)
                    .I(2101001).H(2).H(100).H(200).H(150).H(250)
                    .Bytes();
                Feed("On47017", p47017);
                var box1 = model.GetEudemonsBoxPos(2101001);
                bool b47017 = box1 != null && box1.Count == 2 && box1[0].X == 100 && box1[1].Y == 250;
                Feed("On47018", new CliVerify.Pkt().I(2101003).H(1).H(300).H(400).Bytes());
                var box2 = model.GetEudemonsBoxPos(2101003);
                bool b47018 = box2 != null && box2.Count == 1 && box2[0].X == 300;

                Feed("On47019", new CliVerify.Pkt().H(7).I(12000).I(500).Bytes());
                bool b47019 = model.HuntLevel == 7 && model.HuntExp == 12000 && model.HuntAddExp == 500;

                byte[] p47021 = new CliVerify.Pkt().H(2)
                    .L(9101).S("甲").H(1001).H(1).I(50000).I(1).H(10).I(2).I(80000).I(1)
                    .L(9102).S("乙").H(1001).H(1).I(30000).I(2).H(5).I(3).I(60000).I(2)
                    .Bytes();
                Feed("On47021", p47021);
                bool b47021 = model.EudemonsRank.Count == 2 && model.EudemonsRank[0].RoleName == "甲" && model.EudemonsRank[0].KillNum == 10;

                Feed("On47023", new CliVerify.Pkt().C(30).Bytes());
                bool b47023 = holy.MaxTired == 30;

                bool bEudemonsMisc = b47009 && b47017 && b47018 && b47019 && b47021 && b47023;
                Debug.Log("CLIVERIFY kfboss 47009/17/18/19/21/23 misc ok=" + bEudemonsMisc
                    + "(tired=" + b47009 + " box17=" + b47017 + " box18=" + b47018 + " hunt=" + b47019
                    + " rank=" + b47021 + " maxTired=" + b47023 + ")");

                // ---- I. 47015 结算奖励 ----
                byte[] p47015 = new CliVerify.Pkt().C(1).H(1).C(0).I(38240101).I(3).L(0).Bytes();
                Feed("On47015", p47015);
                bool b47015 = model.HasEudemonsReward && model.EudemonsRewardType == 1 && model.EudemonsRewardList.Count == 1
                    && model.EudemonsRewardList[0].GoodsTypeId == 38240101;
                Debug.Log("CLIVERIFY kfboss 47015 settle reward ok=" + b47015);

                // ---- J. 47002 掉落日志(跨服变体,含Layers字段,与46046共用CrossDropLogEntry) ----
                byte[] p47002 = new CliVerify.Pkt().H(1)
                    .L(9001).H(1001).H(1).S("丙").I(2101001).C(1).I(38240101).I(1500)
                    .H(1).C(3).C(1).H(10).I(200).C(0).I(0)
                    .I(1700030000)
                    .Bytes();
                Feed("On47002", p47002);
                bool b47002 = model.HasEudemonsDropLog && model.EudemonsDropLog.Count == 1
                    && model.EudemonsDropLog[0].Layers == 1 && model.EudemonsDropLog[0].EquipExtraAttr.Count == 1;
                Debug.Log("CLIVERIFY kfboss 47002 drop log ok=" + b47002);

                // ---- K. 47034 死亡debuff → ReliveModel.HolyBoss ----
                Feed("On47034", new CliVerify.Pkt().H(2).I(1700040000).I(1700040300).I(1700040060).Bytes());
                var rl = Shenxiao.Module.Core.Relive.ReliveModel.Instance;
                bool b47034 = rl.HolyBossDieTimes == 2 && rl.HolyBossNextEnterTime == 1700040000
                    && rl.HolyBossDebuffEndTime == 1700040300 && rl.HolyBossSafeEndTime == 1700040060;
                Debug.Log("CLIVERIFY kfboss 47034→ReliveModel.HolyBoss dieTimes=" + rl.HolyBossDieTimes + " ok=" + b47034);

                // ---- L. 47035 复活(成功+失败边界码;成功分支老端硬编码补发47000 boss_type=1,不抛即过) ----
                int reviveCount = 0; bool lastReviveOk = false;
                System.Action<bool, int, int> onRevive = (ok, bt, bi) => { reviveCount++; lastReviveOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_EUDEMONS_REVIVE_RESULT, onRevive);
                bool b47035NoThrow = true;
                try
                {
                    Feed("On47035", new CliVerify.Pkt().I(1).C(1).I(2101002).Bytes());
                    Feed("On47035", new CliVerify.Pkt().I(4600017).C(1).I(2101002).Bytes()); // err460_boss_die,边界失败码
                }
                catch (System.Exception e) { b47035NoThrow = false; Debug.LogError("CLIVERIFY kfboss 47035 threw: " + e); }
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_EUDEMONS_REVIVE_RESULT, onRevive);
                bool b47035 = b47035NoThrow && reviveCount == 2 && lastReviveOk == false;
                Debug.Log("CLIVERIFY kfboss 47035 revive events=" + reviveCount + " ok=" + b47035);

                // ---- M. 47010/47016/47022(占位/壳/纯推送,不抛异常即过) ----
                bool bPlaceholderNoThrow = true;
                try
                {
                    Feed("On47010", new CliVerify.Pkt().I(1031).Bytes());       // kf_server_allot 特殊文案
                    Feed("On47010", new CliVerify.Pkt().I(4700001).Bytes());    // 通用错误码兜底
                    Feed("On47016", new CliVerify.Pkt().H(1).C(1).I(100).Bytes());
                    Feed("On47022", new CliVerify.Pkt().C(1).H(50).Bytes());
                }
                catch (System.Exception e) { bPlaceholderNoThrow = false; Debug.LogError("CLIVERIFY kfboss 47010/16/22 threw: " + e); }
                Debug.Log("CLIVERIFY kfboss 47010/16/22 占位/壳 noThrow=" + bPlaceholderNoThrow);

                // ================= pt_471 镇煞封魂 =================

                // ---- N. 47101 主信息 ----
                byte[] p47101 = new CliVerify.Pkt().C(1).C(3).C(1).C(0).C(0).C(0).H(5).C(1).C(2)
                    .H(2)
                        .I(500001).I(0).C(3).C(1)
                        .I(500002).I(1700050000).C(0).C(0)
                    .Bytes();
                Feed("On47101", p47101);
                bool b47101 = model.HasDecorationInfo && model.DecorationActStatus == 1 && model.DecorationCount == 3
                    && model.DecorationBossList.Count == 2 && model.DecorationBossList[0].IsAlive && !model.DecorationBossList[1].IsAlive;
                Debug.Log("CLIVERIFY kfboss 47101 info actStatus=" + model.DecorationActStatus + " bossN=" + model.DecorationBossList.Count + " ok=" + b47101);

                // ---- O. 47102/47103/47104/47110(进出购买+进特殊boss,成功/失败边界码) ----
                int decEnterCount = 0; int lastDecCode = 0;
                System.Action<int> onDecEnter = c => { decEnterCount++; lastDecCode = c; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_DECORATION_ENTER_RESULT, onDecEnter);
                Feed("On47102", new CliVerify.Pkt().I(1).I(500001).C(1).Bytes());
                bool b47102ok = lastDecCode == 1;
                Feed("On47102", new CliVerify.Pkt().I(4710007).I(500001).C(1).Bytes()); // err471_no_boss_cfg,边界失败码
                bool b47102fail = lastDecCode != 1;
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_DECORATION_ENTER_RESULT, onDecEnter);
                bool b47103_110NoThrow = true;
                try
                {
                    Feed("On47103", new CliVerify.Pkt().I(1).Bytes());
                    Feed("On47110", new CliVerify.Pkt().I(4710003).Bytes()); // err471_boss_die_not_to_enter,边界失败码
                }
                catch (System.Exception e) { b47103_110NoThrow = false; Debug.LogError("CLIVERIFY kfboss 47103/110 threw: " + e); }
                Feed("On47104", new CliVerify.Pkt().I(1).Bytes());
                bool b47104 = model.DecorationBuyCount == 1; // 47101 初值0(BuyCount字段) + 47104 成功自增1
                bool b471020304 = decEnterCount == 2 && b47102ok && b47102fail && b47103_110NoThrow && b47104;
                Debug.Log("CLIVERIFY kfboss 47102/03/04/10 ok=" + b471020304 + "(enter=" + decEnterCount + " buyCount=" + model.DecorationBuyCount + ")");

                // ---- P. 47105/47106 关注(取关列表初始化+单个切换) ----
                Feed("On47105", new CliVerify.Pkt().H(2).I(500001).I(500003).Bytes());
                bool b47105 = model.IsDecorationUnfollowed(500001) && model.IsDecorationUnfollowed(500003) && !model.IsDecorationUnfollowed(500002);
                Feed("On47106", new CliVerify.Pkt().I(1).I(500001).C(1).Bytes()); // 重新关注(取消unfollow)
                bool b47106 = !model.IsDecorationUnfollowed(500001);
                Feed("On47106", new CliVerify.Pkt().I(4710001).I(500002).C(0).Bytes()); // err471_no_guild_to_ask_help,边界失败码,不抛即过
                bool b47105_06 = b47105 && b47106;
                Debug.Log("CLIVERIFY kfboss 47105/06 follow ok=" + b47105_06);

                // ---- Q. 47107复活提醒/47108掉落日志(少Layers多Num的独立形态) ----
                int decTipCount = 0; int lastDecTipBossId = 0;
                System.Action<int> onDecTip = bi => { decTipCount++; lastDecTipBossId = bi; };
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_DECORATION_REVIVE_TIP, onDecTip);
                Feed("On47107", new CliVerify.Pkt().I(500002).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_DECORATION_REVIVE_TIP, onDecTip);
                bool b47107 = decTipCount == 1 && lastDecTipBossId == 500002;

                byte[] p47108 = new CliVerify.Pkt().H(1)
                    .L(9003).H(1001).H(1).S("丁").I(500001).I(38240102).I(2).I(300)
                    .H(1).C(2).C(2).H(20).I(100).C(1).I(0)
                    .I(1700060000)
                    .Bytes();
                Feed("On47108", p47108);
                bool b47108 = model.HasDecorationDropLog && model.DecorationDropLog.Count == 1
                    && model.DecorationDropLog[0].Num == 2 && model.DecorationDropLog[0].EquipExtraAttr.Count == 1;
                Debug.Log("CLIVERIFY kfboss 47107 tip=" + b47107 + " 47108 dropLog=" + b47108);

                // ---- R. 47109全量/47112单条patch排名(按role_id命中更新否则追加) ----
                byte[] p47109 = new CliVerify.Pkt().H(1)
                    .L(9101).S("戊").H(1001).H(1).S("一区").L(80000)
                    .Bytes();
                Feed("On47109", p47109);
                Feed("On47112", new CliVerify.Pkt().L(9101).S("戊").H(1001).H(1).S("一区").L(95000).Bytes()); // 命中更新
                Feed("On47112", new CliVerify.Pkt().L(9102).S("己").H(1001).H(2).S("二区").L(40000).Bytes()); // 未命中追加
                bool b47109_112 = model.DecorationRank.Count == 2 && model.DecorationRank[0].Hurt == 95000 && model.DecorationRank[1].Hurt == 40000;
                Debug.Log("CLIVERIFY kfboss 47109/112 rank patch ok=" + b47109_112);

                // ---- S. 47113 结算(双套奖励表嵌套数组) ----
                byte[] p47113 = new CliVerify.Pkt().C(1).C(0)
                    .H(1)
                        .C(1).H(2)
                            .C(0).I(38240101).I(1).L(0)
                            .C(0).I(38240102).I(2).L(0)
                    .H(1)
                        .C(2).H(1)
                            .C(0).I(38240103).I(1).L(0)
                    .Bytes();
                Feed("On47113", p47113);
                var settle = model.LastDecorationSettle;
                bool b47113 = settle != null && settle.IsBelong && !settle.IsDouble
                    && settle.RewardTypeList.Count == 1 && settle.RewardTypeList[0].Items.Count == 2
                    && settle.RewardTypeList2.Count == 1 && settle.RewardTypeList2[0].Items.Count == 1
                    && settle.RewardTypeList2[0].Items[0].TypeId == 38240103;
                Debug.Log("CLIVERIFY kfboss 47113 settle g1=" + (settle?.RewardTypeList.Count ?? -1)
                    + " g2=" + (settle?.RewardTypeList2.Count ?? -1) + " ok=" + b47113);

                // ---- T. 47114/47115/47116 场景信息 ----
                Feed("On47114", new CliVerify.Pkt().C(1).I(1700070000).I(1700070300).Bytes());
                bool b47114 = model.DecorationEnterType == 1 && model.DecorationQuitTime == 1700070000 && model.DecorationReviveTime == 1700070300;
                Feed("On47115", new CliVerify.Pkt().I(1700070999).Bytes());
                bool b47115 = model.DecorationQuitTime == 1700070999;
                Feed("On47116", new CliVerify.Pkt().I(1700071111).Bytes());
                bool b47116 = model.DecorationReviveTime == 1700071111;
                bool b47114_16 = b47114 && b47115 && b47116;
                Debug.Log("CLIVERIFY kfboss 47114/15/16 scene info ok=" + b47114_16);

                // ---- U. 47111仙宗召援 / 47117死亡广播(防御recv) ----
                int guildHelpCount = 0;
                System.Action<int> onGuildHelp = c => guildHelpCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_DECORATION_GUILD_HELP_RESULT, onGuildHelp);
                Feed("On47111", new CliVerify.Pkt().I(1).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_DECORATION_GUILD_HELP_RESULT, onGuildHelp);
                bool b47111 = guildHelpCount == 1;
                bool b47117NoThrow = true;
                try { Feed("On47117", new CliVerify.Pkt().I(500001).I(1700080000).Bytes()); }
                catch (System.Exception e) { b47117NoThrow = false; Debug.LogError("CLIVERIFY kfboss 47117 threw: " + e); }
                Debug.Log("CLIVERIFY kfboss 47111 guildHelp=" + b47111 + " 47117 noThrow=" + b47117NoThrow);

                // ================= pt_619 论剑恩怨簿 =================

                // ---- V. 61900全量(本服+跨服两个数组)/61901本服增量/61902跨服增量(32位ServerId独例) ----
                byte[] p61900 = new CliVerify.Pkt()
                    .H(1).C(1).I(1700090000).S("蛮荒禁地").S("张三").L(9201)
                    .H(1).C(2).I(1700090100).S("千幻蜃楼").I(1001).I(1).S("李四").L(9202)
                    .Bytes();
                Feed("On61900", p61900);
                bool b61900 = model.HasKillRecord && model.KillRecordList.Count == 1 && model.KfKillRecordList.Count == 1
                    && model.KillRecordList[0].AttrName == "张三" && model.KfKillRecordList[0].ServerId == 1001;
                Feed("On61901", new CliVerify.Pkt().C(1).I(1700090200).S("深渊").S("王五").L(9203).Bytes());
                bool b61901 = model.KillRecordList.Count == 2 && model.KillRecordList[1].AttrName == "王五";
                Feed("On61902", new CliVerify.Pkt().C(2).I(1700090300).S("跨服圣域").I(1002).I(1).S("赵六").L(9204).Bytes());
                bool b61902 = model.KfKillRecordList.Count == 2 && model.KfKillRecordList[1].ServerId == 1002 && model.KfKillRecordList[1].AttrName == "赵六";
                bool b619 = b61900 && b61901 && b61902;
                Debug.Log("CLIVERIFY kfboss 619 killRecord local=" + model.KillRecordList.Count + " kf=" + model.KfKillRecordList.Count + " ok=" + b619);

                // ================= pt_460 kf_great_demon 壳(46037-39/46046) =================

                // ---- W. 46037阶段奖励状态/46038领取(成功+失败边界码)/46039宝箱信息/46046掉落日志(16位BossType) ----
                byte[] p46037 = new CliVerify.Pkt().I(58).H(2).H(1).H(2).Bytes();
                Feed("On46037", p46037);
                bool b46037 = model.HasGreatDemonReward && model.GreatDemonKillNum == 58 && model.GreatDemonHadRewardStages.Count == 2;

                int rewardTakeCount = 0;
                System.Action onGreatReward = () => rewardTakeCount++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE, onGreatReward);
                Feed("On46038", new CliVerify.Pkt().I(3).I(1).Bytes());       // 成功(领取阶段3)
                Feed("On46038", new CliVerify.Pkt().I(4).I(4600033).Bytes()); // err460_domain_had_reward,边界失败码
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE, onGreatReward);
                bool b46038 = rewardTakeCount == 2; // 成功分支额外触发 RequestGreatDemonRewardState() 重拉,不额外计入事件计数(SendFmt非事件)

                byte[] p46039 = new CliVerify.Pkt().H(1)
                    .I(430002).H(1).H(500).H(600)
                    .Bytes();
                Feed("On46039", p46039);
                bool b46039 = model.HasGreatDemonBox && model.GreatDemonBoxList.Count == 1 && model.GreatDemonBoxList[0].XyList.Count == 1
                    && model.GreatDemonBoxList[0].XyList[0].X == 500;

                byte[] p46046 = new CliVerify.Pkt().H(20).H(1)
                    .L(9301).H(1001).H(1).S("庚").I(430001).C(1).I(38240104).I(2000)
                    .H(1).C(4).C(1).H(30).I(500).C(1).I(0)
                    .I(1700100000)
                    .Bytes();
                Feed("On46046", p46046);
                bool b46046 = model.HasGreatDemonDropLog && model.GreatDemonDropLog.Count == 1
                    && model.GreatDemonDropLog[0].Layers == 1 && model.GreatDemonDropLog[0].EquipExtraAttr.Count == 1;

                bool bGreatDemon = b46037 && b46038 && b46039 && b46046;
                Debug.Log("CLIVERIFY kfboss greatDemon 46037=" + b46037 + " 46038=" + b46038 + " 46039=" + b46039 + " 46046=" + b46046 + " ok=" + bGreatDemon);

                bool pass = configOk && deadOk && mysteryDoorOk && b47000 && b47003_04 && b47006 && b47007 && b47008NoThrow
                    && bEudemonsMisc && b47015 && b47002 && b47034 && b47035 && bPlaceholderNoThrow
                    && b47101 && b471020304 && b47105_06 && b47107 && b47108 && b47109_112 && b47113 && b47114_16
                    && b47111 && b47117NoThrow && b619 && bGreatDemon;

                Debug.Log("CLIVERIFY kfboss VERDICT config=" + configOk + " dead=" + deadOk + " mysteryDoor=" + mysteryDoorOk
                    + " l47000=" + b47000 + " l4703_04=" + b47003_04 + " l47006=" + b47006 + " l47007=" + b47007
                    + " l47008=" + b47008NoThrow + " eudemonsMisc=" + bEudemonsMisc + " l47015=" + b47015 + " l47002=" + b47002
                    + " l47034=" + b47034 + " l47035=" + b47035 + " placeholder=" + bPlaceholderNoThrow
                    + " l47101=" + b47101 + " l471020304=" + b471020304 + " l4710506=" + b47105_06 + " l47107=" + b47107
                    + " l47108=" + b47108 + " l47109112=" + b47109_112 + " l47113=" + b47113 + " l471416=" + b47114_16
                    + " l47111=" + b47111 + " l47117=" + b47117NoThrow + " l619=" + b619 + " greatDemon=" + bGreatDemon
                    + " pass=" + pass);

                model.Clear();
                Shenxiao.Module.Core.Relive.ReliveModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
