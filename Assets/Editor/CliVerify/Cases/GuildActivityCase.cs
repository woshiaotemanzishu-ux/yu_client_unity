using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 公会晚宴(pt_402 主体,自动循环 轮22 PK1)实证:公会BOSS(40201/03/04/08/09)+ 晚宴主流程
    /// (40211/12/14/17/20/21/22)+ 篝火/答题/龙魂/菜肴(40255/56/57/58/59/60/62/64/65/66/67)+ 族错误出口
    /// 40200,共 26 号合成包驱动 GuildActivityController 反射喂包,断言 GuildActivityModel 落地字段/事件 +
    /// config 六项计数(模板 MarriageCase,纯逻辑段)。
    ///
    /// 重点覆盖:40208 结算奖励 Gnum:**16**独例(勿与40257/40262/40266标准ObjectList的Num:32混淆);
    /// 40214 双嵌套数组(GuildList/RankList,含字符串字段)+ **尾哨兵字节游标核对**(数组读完后紧跟一个
    /// 32位哨兵值,断言 NetReader 剩余字节数与哨兵值精确匹配,防止字符串/嵌套数组场景下的游标错位);
    /// 40200 族错误出口/40211 核心驱动主面板/40259 答题(收发)/40255 纯被动收;40203 兽粮被动推送对
    /// Boss.GbossMat 原地刷新;40209/40204 成功码原地更新与失败码不覆盖边界。
    /// 死号断言:40218(仅发送无接收,退出场景请求无回执)/40261(仅发送无接收,购买龙魂走40200/40260联动)/
    /// 40263(发送+接收均不存在,且 Proto 类里没有对应的 40263 常量——三层彻底死透,详见
    /// GuildActivityController 类注释②③④与主控裁决3/4)。
    ///
    /// UI 层:33 个 EveningXXX 相关 View 里 prefab 只烤了 4 个,本轮数据层only不接 View(同 15a/15b Boss
    /// 先例,纯数据层轮无渲染段);结社守卫(40230-32)按裁决2 全部 killlist,不在本 Case 覆盖范围。
    /// </summary>
    public static class GuildActivityCase
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
                await Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY guildact FAIL GuildActivityConfigs not loaded");
                    return 3;
                }

                bool configOk = Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.FirePosCount == 22
                    && Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.FireCfgCount == 3
                    && Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EveningIntroCount == 3
                    && Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EveningStageCount == 6
                    && Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EveningMainCount == 3
                    && Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.GiftCount == 5;
                var firePos0 = Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.GetFirePos(0);
                var fireCfg1 = Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.GetFireCfg(1);
                var stage6 = Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.GetEveningStage(6);
                var gift1 = Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.GetGift(1);
                bool configRowOk = firePos0 != null && firePos0.X == 307 && firePos0.Y == 245
                    && fireCfg1 != null && fireCfg1.Color == "白" && fireCfg1.Effect == "ui_white_fire"
                    && stage6 != null && stage6.Name == "召唤龙魂"
                    && gift1 != null && gift1.Activity == 600 && gift1.Icon == "guild_038";
                Debug.Log("CLIVERIFY guildact config firePos=" + Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.FirePosCount
                    + " fireCfg=" + Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.FireCfgCount
                    + " intro=" + Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EveningIntroCount
                    + " stage=" + Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EveningStageCount
                    + " main=" + Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.EveningMainCount
                    + " gift=" + Shenxiao.Module.Core.GuildActivity.GuildActivityConfigs.GiftCount
                    + " ok=" + configOk + " rowOk=" + configRowOk);

                Shenxiao.Module.Core.GuildActivity.GuildActivityModel model = Shenxiao.Module.Core.GuildActivity.GuildActivityModel.Instance;
                model.Clear();

                object ctrl = Shenxiao.Module.Core.GuildActivity.GuildActivityController.Instance;
                System.Type t = ctrl.GetType();
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY guildact handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }
                Shenxiao.Framework.Net.NetReader FeedReader(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    var reader = new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);
                    if (m == null) { Debug.LogError("CLIVERIFY guildact handler missing: " + method); return reader; }
                    m.Invoke(ctrl, new object[] { reader });
                    return reader;
                }

                // ---- A. 40200 族错误出口(错误码段;真实 err402_no_in_act_scene=4020000) ----
                int errEventCount = 0; int lastErrEvent = 0;
                System.Action<int> onErr = code => { errEventCount++; lastErrEvent = code; };
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_ERROR, onErr);
                Feed("On40200", new CliVerify.Pkt().I(4020000).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_ERROR, onErr);
                bool b40200 = model.LastErrorCode == 4020000 && errEventCount == 1 && lastErrEvent == 4020000;
                Debug.Log("CLIVERIFY guildact 40200 族错误出口 code=" + model.LastErrorCode + " ok=" + b40200);

                // ---- B. 40201 公会BOSS信息(全字段) ----
                byte[] p40201 = new CliVerify.Pkt().I(1700000000).I(1700003600).I(2007001).I(500).C(3).C(1).C(0).C(2).Bytes();
                Feed("On40201", p40201);
                bool b40201 = model.HasBoss && model.Boss.Etime == 1700000000 && model.Boss.AutoDrumupTime == 1700003600
                    && model.Boss.DunId == 2007001 && model.Boss.GbossMat == 500 && model.Boss.RemainTimes == 3
                    && model.Boss.IsAuto == 1 && model.Boss.IsDrumToday == 0 && model.Boss.MonState == 2;
                Debug.Log("CLIVERIFY guildact 40201 公会BOSS信息 gbossMat=" + (model.Boss?.GbossMat ?? -1) + " ok=" + b40201);

                // ---- C. 40203 兽粮被动推送(原地刷新 Boss.GbossMat,内部触发非c2s回执) ----
                int matAddCount = 0; long lastAdd = 0, lastTotal = 0;
                System.Action<long, long> onMatAdd = (add, total) => { matAddCount++; lastAdd = add; lastTotal = total; };
                Shenxiao.Framework.Event.EventDispatcher.On<long, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_BOSS_MAT_ADD, onMatAdd);
                Feed("On40203", new CliVerify.Pkt().I(50).I(550).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<long, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_BOSS_MAT_ADD, onMatAdd);
                bool b40203 = model.LastGbossMatAdd == 50 && model.Boss.GbossMat == 550 && matAddCount == 1 && lastAdd == 50 && lastTotal == 550;
                Debug.Log("CLIVERIFY guildact 40203 兽粮被动推送 add=50 boss.GbossMat=" + model.Boss.GbossMat + " ok=" + b40203);

                // ---- D. 40204 召集公会BOSS(成功置IsDrumToday=1;errcode=2不显码但仍发事件;边界不覆盖) ----
                int callBossEvents = 0;
                System.Action<int> onCallBoss = code => callBossEvents++;
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_CALL_BOSS_RESULT, onCallBoss);
                Feed("On40204", new CliVerify.Pkt().I(1).L(9001).Bytes());
                bool b40204Success = model.Boss.IsDrumToday == 1;
                bool b40204Code2NoThrow = true;
                try { Feed("On40204", new CliVerify.Pkt().I(2).L(9002).Bytes()); }
                catch (System.Exception e) { b40204Code2NoThrow = false; Debug.LogError("CLIVERIFY guildact 40204 code2 threw: " + e); }
                Feed("On40204", new CliVerify.Pkt().I(4021046).L(9003).Bytes()); // err402_boss_not_drum_up,只显码不崩
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_CALL_BOSS_RESULT, onCallBoss);
                bool b40204 = b40204Success && b40204Code2NoThrow && callBossEvents == 3;
                Debug.Log("CLIVERIFY guildact 40204 召集公会BOSS isDrumToday=" + model.Boss.IsDrumToday + " events=" + callBossEvents + " ok=" + b40204);

                // ---- E. 40208 BOSS结算推送(Gnum:**16**独例,双数组) ----
                byte[] p40208 = new CliVerify.Pkt().C(1)
                    .H(1).C(0).I(2001).H(5)
                    .H(1).C(0).I(3001).H(10)
                    .Bytes();
                Feed("On40208", p40208);
                bool b40208 = model.LastBossResult != null && model.LastBossResult.GbossResult == 1
                    && model.LastBossResult.FixReward.Count == 1 && model.LastBossResult.FixReward[0].TypeId == 2001 && model.LastBossResult.FixReward[0].Num == 5
                    && model.LastBossResult.AuctionReward.Count == 1 && model.LastBossResult.AuctionReward[0].TypeId == 3001 && model.LastBossResult.AuctionReward[0].Num == 10;
                Debug.Log("CLIVERIFY guildact 40208 BOSS结算(Gnum16独例) fixNum=" + (model.LastBossResult?.FixReward[0].Num ?? -1) + " ok=" + b40208);

                // ---- F. 40209 自动召唤设置(成功原地更新;失败不覆盖) ----
                Feed("On40209", new CliVerify.Pkt().I(1).C(1).Bytes());
                bool b40209Success = model.Boss.IsAuto == 1;
                Feed("On40209", new CliVerify.Pkt().I(4021049).C(0).Bytes()); // err402_not_drum_time,失败不应把IsAuto改成0
                bool b40209FailNotOverwritten = model.Boss.IsAuto == 1;
                bool b40209 = b40209Success && b40209FailNotOverwritten;
                Debug.Log("CLIVERIFY guildact 40209 自动召唤 isAuto=" + model.Boss.IsAuto + " failNotOverwritten=" + b40209FailNotOverwritten + " ok=" + b40209);

                // ---- G. 40211 晚宴活动信息(核心驱动主面板) ----
                Feed("On40211", new CliVerify.Pkt().C(1).I(1700100000).I(1700000000).C(4).Bytes());
                bool b40211 = model.HasAct && model.Act.Status == 1 && model.Act.Stage == 4 && model.Act.ActEndTime == 1700100000 && model.Act.Etime == 1700000000;
                Debug.Log("CLIVERIFY guildact 40211 核心驱动主面板 stage=" + (model.Act?.Stage ?? -1) + " ok=" + b40211);

                // ---- H. 40212 进入晚宴场景(成功重发40211/失败显码,不抛) ----
                bool b40212NoThrow = true;
                try
                {
                    Feed("On40212", new CliVerify.Pkt().I(1).Bytes());
                    Feed("On40212", new CliVerify.Pkt().I(4020000).Bytes()); // err402_no_in_act_scene
                }
                catch (System.Exception e) { b40212NoThrow = false; Debug.LogError("CLIVERIFY guildact 40212 threw: " + e); }
                Debug.Log("CLIVERIFY guildact 40212 进入晚宴场景 noThrow=" + b40212NoThrow);

                // ---- I. 40214 积分排行榜(双嵌套数组含字符串 + **尾哨兵字节游标核对**) ----
                byte[] p40214 = new CliVerify.Pkt().C(0)
                    .H(1).L(500001).I(1).S("测试公会").I(100).H(1)
                    .H(1).I(1).I(1).H(1).S("服务器1").I(200)
                    .I(88888888) // 尾哨兵:紧跟在两个数组读完之后,验证游标精确落在此处
                    .Bytes();
                var reader40214 = FeedReader("On40214", p40214);
                bool b40214Fields = model.HasRank && model.Rank.IsKf == 0
                    && model.Rank.GuildList.Count == 1 && model.Rank.GuildList[0].GuildId == 500001
                    && model.Rank.GuildList[0].GuildName == "测试公会" && model.Rank.GuildList[0].GuildScore == 100 && model.Rank.GuildList[0].GuildRank == 1
                    && model.Rank.RankList.Count == 1 && model.Rank.RankList[0].SerId == 1 && model.Rank.RankList[0].Name == "服务器1" && model.Rank.RankList[0].Score == 200;
                bool b40214Sentinel = reader40214.Remaining == 4 && reader40214.ReadU32() == 88888888;
                bool b40214 = b40214Fields && b40214Sentinel;
                Debug.Log("CLIVERIFY guildact 40214 积分排行榜(双嵌套数组) guildName=" + (model.Rank?.GuildList[0].GuildName ?? "?")
                    + " sentinelRemainBefore=" + (b40214Sentinel ? "4(exact)" : "MISMATCH") + " ok=" + b40214);

                // ---- J. 40217 答题信息 ----
                Feed("On40217", new CliVerify.Pkt().C(1).I(1700000500).I(5).L(123456789).Bytes());
                bool b40217 = model.Quest != null && model.Quest.Status == 1 && model.Quest.No == 5 && model.Quest.Id == 123456789;
                Debug.Log("CLIVERIFY guildact 40217 答题信息 no=" + (model.Quest?.No ?? -1) + " ok=" + b40217);

                // ---- K. 40218 死号(仅发送,严禁接收注册——见类注释①) ----
                bool dead40218SendOk = t.GetMethod("RequestExitScene", PF) != null;
                bool dead40218NoRecv = t.GetMethod("On40218", F) == null;
                Debug.Log("CLIVERIFY guildact 40218 hasSend=" + dead40218SendOk + " noRecvRegistered=" + dead40218NoRecv);

                // ---- L. 40220 个人积分排行/40221 小游戏完成/40222 当日小游戏类型 ----
                Feed("On40220", new CliVerify.Pkt().H(3).L(99999).Bytes());
                bool b40220 = model.MyRank == 3 && model.MyPoint == 99999;
                Feed("On40221", new CliVerify.Pkt().C(1).Bytes());
                bool b40221 = model.MiniGameFinished;
                Feed("On40222", new CliVerify.Pkt().C(2).Bytes());
                bool b40222 = model.GameType == 2;
                Debug.Log("CLIVERIFY guildact 40220/21/22 myRank=" + model.MyRank + " miniFinish=" + model.MiniGameFinished
                    + " gameType=" + model.GameType + " ok=" + (b40220 && b40221 && b40222));

                // ---- M. 40255 经验/贡献推送(纯被动收) ----
                Feed("On40255", new CliVerify.Pkt().C(1).L(12345).Bytes());
                bool b40255 = model.LastExpPushType == 1 && model.LastExpPushValue == 12345;
                bool dead40255NoSend = t.GetMethod("RequestExpPush", PF) == null && t.GetMethod("RequestExp", PF) == null;
                Debug.Log("CLIVERIFY guildact 40255 纯被动收 exp=" + model.LastExpPushValue + " noSendMethod=" + dead40255NoSend + " ok=" + (b40255 && dead40255NoSend));

                // ---- N. 40256 火苗信息/40257 采集火苗奖励(纯被动收,标准ObjectList Num:32) ----
                Feed("On40256", new CliVerify.Pkt().I(3).L(1700005000).Bytes());
                bool b40256 = model.Fire != null && model.Fire.Wave == 3 && model.Fire.NextTime == 1700005000;
                byte[] p40257 = new CliVerify.Pkt().H(1).C(0).I(38040002).I(10).Bytes();
                Feed("On40257", p40257);
                bool b40257 = model.LastFireReward.Count == 1 && model.LastFireReward[0].TypeId == 38040002 && model.LastFireReward[0].Num == 10;
                bool dead40257NoSend = t.GetMethod("RequestCollectFire", PF) == null && t.GetMethod("RequestFireCollect", PF) == null;
                Debug.Log("CLIVERIFY guildact 40256/40257 fireWave=" + (model.Fire?.Wave ?? -1) + " fireRewardTypeId=" + (model.LastFireReward.Count > 0 ? model.LastFireReward[0].TypeId : -1)
                    + " noSendMethod257=" + dead40257NoSend + " ok=" + (b40256 && b40257 && dead40257NoSend));

                // ---- O. 40258 阶段推送(无c2s,纯推送) ----
                Feed("On40258", new CliVerify.Pkt().C(4).H(30).Bytes());
                bool b40258 = model.LastStage == 4 && model.LastStageTime == 30;
                Debug.Log("CLIVERIFY guildact 40258 阶段推送 stage=" + model.LastStage + " time=" + model.LastStageTime + " ok=" + b40258);

                // ---- P. 40259 答题(收发) ----
                Feed("On40259", new CliVerify.Pkt().C(1).Bytes());
                bool b40259Recv = model.QuestStatus == 1;
                bool b40259SendNoThrow = true;
                try { ((Shenxiao.Module.Core.GuildActivity.GuildActivityController)ctrl).RequestAnswer(2); }
                catch (System.Exception e) { b40259SendNoThrow = false; Debug.LogError("CLIVERIFY guildact 40259 send threw: " + e); }
                bool b40259 = b40259Recv && b40259SendNoThrow;
                Debug.Log("CLIVERIFY guildact 40259 答题(收发) status=" + model.QuestStatus + " sendNoThrow=" + b40259SendNoThrow + " ok=" + b40259);

                // ---- Q. 40260 龙魂信息/40261 死号(仅发送,购买走40200/40260联动无独立回执) ----
                Feed("On40260", new CliVerify.Pkt().L(777).Bytes());
                bool b40260 = model.HasDragonInfo && model.DragonSpirit == 777;
                bool dead40261SendOk = t.GetMethod("RequestBuyDragonSpirit", PF) != null;
                bool dead40261NoRecv = t.GetMethod("On40261", F) == null;
                bool b40261SendNoThrow = true;
                try { ((Shenxiao.Module.Core.GuildActivity.GuildActivityController)ctrl).RequestBuyDragonSpirit(5); }
                catch (System.Exception e) { b40261SendNoThrow = false; Debug.LogError("CLIVERIFY guildact 40261 send threw: " + e); }
                Debug.Log("CLIVERIFY guildact 40260/40261 dragonSpirit=" + model.DragonSpirit + " 40261hasSend=" + dead40261SendOk
                    + " 40261noRecv=" + dead40261NoRecv + " sendNoThrow=" + b40261SendNoThrow
                    + " ok=" + (b40260 && dead40261SendOk && dead40261NoRecv && b40261SendNoThrow));

                // ---- R. 40263 三层死透:发送+接收均不存在,Proto 无对应常量 ----
                bool dead40263NoRecv = t.GetMethod("On40263", F) == null;
                bool dead40263NoSend = t.GetMethod("RequestSummonDragon", PF) == null && t.GetMethod("RequestCallDragon", PF) == null && t.GetMethod("RequestDragonSummon", PF) == null;
                bool dead40263NoProtoConst = !typeof(Shenxiao.Framework.Net.Proto).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Any(fi => fi.FieldType == typeof(int) && fi.IsLiteral && (int)fi.GetRawConstantValue() == 40263);
                bool b40263Dead = dead40263NoRecv && dead40263NoSend && dead40263NoProtoConst;
                Debug.Log("CLIVERIFY guildact 40263 三层死透 noRecv=" + dead40263NoRecv + " noSend=" + dead40263NoSend
                    + " noProtoConst=" + dead40263NoProtoConst + " ok=" + b40263Dead);

                // ---- S. 40262 战斗结果推送(标准ObjectList Num:32) ----
                byte[] p40262 = new CliVerify.Pkt().C(1).H(1).C(0).I(5001).I(20).Bytes();
                Feed("On40262", p40262);
                bool b40262 = model.LastResult != null && model.LastResult.Status == 1
                    && model.LastResult.RewardList.Count == 1 && model.LastResult.RewardList[0].TypeId == 5001 && model.LastResult.RewardList[0].Num == 20;
                Debug.Log("CLIVERIFY guildact 40262 战斗结果推送 status=" + (model.LastResult?.Status ?? -1) + " ok=" + b40262);

                // ---- T. 40264 购买菜肴(成功落地/失败不覆盖)/40265 菜肴状态 ----
                int foodBuyCount = 0; bool lastFoodBuyOk = false;
                System.Action<bool, int> onFoodBuy = (ok, code) => { foodBuyCount++; lastFoodBuyOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On<bool, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_FOOD_BUY_RESULT, onFoodBuy);
                Feed("On40264", new CliVerify.Pkt().I(1).H(1).C(2).C(1).Bytes());
                bool b40264Success = model.FoodList.Count == 1 && model.FoodList[0].Type == 2 && model.FoodList[0].Status == 1;
                Feed("On40264", new CliVerify.Pkt().I(4021050).H(0).Bytes()); // err402_yet_buy_food,失败不应清空已购列表
                bool b40264FailNotOverwritten = model.FoodList.Count == 1 && model.FoodList[0].Type == 2;
                Shenxiao.Framework.Event.EventDispatcher.Off<bool, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILDACT_FOOD_BUY_RESULT, onFoodBuy);
                bool b40264 = b40264Success && b40264FailNotOverwritten && foodBuyCount == 2 && !lastFoodBuyOk;
                Feed("On40265", new CliVerify.Pkt().H(1).C(3).C(0).Bytes());
                bool b40265 = model.FoodList.Count == 1 && model.FoodList[0].Type == 3 && model.FoodList[0].Status == 0;
                Debug.Log("CLIVERIFY guildact 40264/40265 菜肴 buyOk=" + b40264Success + " failNotOverwritten=" + b40264FailNotOverwritten
                    + " statusRefresh=" + b40265 + " ok=" + (b40264 && b40265));

                // ---- U. 40266 答题积分排名奖励(纯S-only推送,标准ObjectList) ----
                Feed("On40266", new CliVerify.Pkt().I(5).H(1).C(0).I(6001).I(30).Bytes());
                bool b40266 = model.LastRankRewardRank == 5 && model.LastRankReward.Count == 1
                    && model.LastRankReward[0].TypeId == 6001 && model.LastRankReward[0].Num == 30;
                Debug.Log("CLIVERIFY guildact 40266 答题积分排名奖励 rank=" + model.LastRankRewardRank + " ok=" + b40266);

                // ---- V. 40267 经验加成状态 ----
                Feed("On40267", new CliVerify.Pkt().I(150).Bytes());
                bool b40267 = model.ExpBuffRatio == 150;
                Debug.Log("CLIVERIFY guildact 40267 经验加成状态 ratio=" + model.ExpBuffRatio + " ok=" + b40267);

                bool pass = configOk && configRowOk && b40200 && b40201 && b40203 && b40204 && b40208 && b40209
                    && b40211 && b40212NoThrow && b40214 && b40217
                    && dead40218SendOk && dead40218NoRecv
                    && b40220 && b40221 && b40222
                    && b40255 && dead40255NoSend
                    && b40256 && b40257 && dead40257NoSend
                    && b40258 && b40259
                    && b40260 && dead40261SendOk && dead40261NoRecv && b40261SendNoThrow
                    && b40263Dead
                    && b40262 && b40264 && b40265 && b40266 && b40267;

                Debug.Log("CLIVERIFY guildact VERDICT config=" + (configOk && configRowOk)
                    + " boss(201/03/04/08/09)=" + (b40201 && b40203 && b40204 && b40208 && b40209)
                    + " main(11/12/14/17)=" + (b40211 && b40212NoThrow && b40214 && b40217)
                    + " dead218=" + (dead40218SendOk && dead40218NoRecv)
                    + " rank(20/21/22)=" + (b40220 && b40221 && b40222)
                    + " exp255=" + (b40255 && dead40255NoSend)
                    + " fire(56/57)=" + (b40256 && b40257 && dead40257NoSend)
                    + " stage258=" + b40258 + " quiz259=" + b40259
                    + " dragon(260/dead261)=" + (b40260 && dead40261SendOk && dead40261NoRecv && b40261SendNoThrow)
                    + " dead263=" + b40263Dead
                    + " result262=" + b40262 + " food(64/65)=" + (b40264 && b40265)
                    + " rankReward266=" + b40266 + " expBuff267=" + b40267
                    + " pass=" + pass);

                model.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
