using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// FirstBlood(188xx)+Festival(194xx补全)合包实证(自动循环 轮18 便宜活批 PK3)。反射喂包
    /// FirstBloodController/FestivalController 全 13 号(FirstBlood 18800-18807 全 8 号 + Festival
    /// 19400/19402-19404 补全 3 号,19401 既有逻辑一并复验,19405 send-only 无 recv 反射断言"无 On19405
    /// 方法"),断言两 Model 落地字段/嵌套数组(18801/18804 DressList 二层嵌套,18801 type 分桶隔离,
    /// 19403 TypeList/TaskList 二层嵌套)。18802 Type=96&&Subtype=2 死分支用 GameLog 捕获断言"拒绝发送
    /// 不落网络层",对照同参数下 Subtype=1 正常放行。GAME_START/CHANGE_LEVEL 老端触发链镜像(18801×2+
    /// 18805×1 / 19401→自动19403)用 "send while disconnected: proto=X" 日志计数验证发送次数(CliVerify
    /// 全程不建立真实连接,NetManager.IsConnected 恒 false,此日志是发送确已发生的可靠副作用探针)。
    /// config 计数:config_boss_first_blood_plus_boss=63,config_fiesta_act=20/act_task=58/kv=2/
    /// lv_exp=1935/task=153(均已实测核对,与 r18_server_group_b.md/r18_server_fiesta.md 侦察值一致)。
    /// UI 层:FirstBloodMainView/Festival 12 子面板 Bind 均未接数据绑定(留尾包),本轮纯数据层。
    /// </summary>
    public static class FbFestivalCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);

                // ---- config 计数(6 表,均对象键 map,Count=条目数) ----
                JObject cfgFirstBloodBoss = await LoadServerConfig("config_boss_first_blood_plus_boss");
                JObject cfgFiestaAct = await LoadServerConfig("config_fiesta_act");
                JObject cfgFiestaActTask = await LoadServerConfig("config_fiesta_act_task");
                JObject cfgFiestaKv = await LoadServerConfig("config_fiesta_kv");
                JObject cfgFiestaLvExp = await LoadServerConfig("config_fiesta_lv_exp");
                JObject cfgFiestaTask = await LoadServerConfig("config_fiesta_task");
                bool configOk = cfgFirstBloodBoss.Count == 63 && cfgFiestaAct.Count == 20 && cfgFiestaActTask.Count == 58
                    && cfgFiestaKv.Count == 2 && cfgFiestaLvExp.Count == 1935 && cfgFiestaTask.Count == 153;
                Debug.Log("CLIVERIFY fbfestival config firstBloodBoss=" + cfgFirstBloodBoss.Count
                    + " fiestaAct=" + cfgFiestaAct.Count + " actTask=" + cfgFiestaActTask.Count
                    + " kv=" + cfgFiestaKv.Count + " lvExp=" + cfgFiestaLvExp.Count + " task=" + cfgFiestaTask.Count
                    + " ok=" + configOk);

                Shenxiao.Module.Core.FirstBlood.FirstBloodModel fbModel = Shenxiao.Module.Core.FirstBlood.FirstBloodModel.Instance;
                Shenxiao.Module.Core.Festival.FestivalModel fvModel = Shenxiao.Module.Core.Festival.FestivalModel.Instance;
                fbModel.Reset();
                fvModel.Reset();

                object fbCtrl = Shenxiao.Module.Core.FirstBlood.FirstBloodController.Instance;
                object fvCtrl = Shenxiao.Module.Core.Festival.FestivalController.Instance;
                System.Type fbType = fbCtrl.GetType();
                System.Type fvType = fvCtrl.GetType();

                void FeedFb(string method, byte[] pkt)
                {
                    MethodInfo m = fbType.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY fbfestival FirstBlood handler missing: " + method); return; }
                    m.Invoke(fbCtrl, new object[] { new NetReader(pkt, 0, pkt.Length) });
                }
                void FeedFv(string method, byte[] pkt)
                {
                    MethodInfo m = fvType.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY fbfestival Festival handler missing: " + method); return; }
                    m.Invoke(fvCtrl, new object[] { new NetReader(pkt, 0, pkt.Length) });
                }
                List<string> CaptureLogs(System.Action body)
                {
                    var logs = new List<string>();
                    Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
                    Application.logMessageReceived += cb;
                    try { body(); }
                    finally { Application.logMessageReceived -= cb; }
                    return logs;
                }
                int CountProto(List<string> logs, int protoId)
                {
                    string needle = "proto=" + protoId;
                    int n = 0;
                    foreach (string l in logs) if (l.Contains(needle)) n++;
                    return n;
                }

                // ======================================================================================
                // A. FirstBlood 18801 列表(二层嵌套 DressList,type 分桶隔离)+ 镜像 18806 逐条详情查询
                // ======================================================================================
                byte[] p18801Boss = new CliVerify.Pkt().C(96).C(1).H(2)
                    .C(1).I(2220105).L(9001).S("甲").H(170).C(1).C(2).S("pic1").I(3)
                        .H(2).C(1).I(500).C(2).I(501)
                        .C(1)
                    .C(0).I(2220107).L(0).S("").H(0).C(0).C(0).S("").I(0)
                        .H(0)
                        .C(0)
                    .Bytes();
                List<string> logs18801 = CaptureLogs(() => FeedFb("On18801", p18801Boss));
                bool b18801 = fbModel.ListByType.TryGetValue(96, out var bossList) && bossList.Count == 2
                    && fbModel.ListSubtypeByType[96] == 1
                    && bossList[0].BossId == 2220105 && bossList[0].FirstBloodRoleId == 9001 && bossList[0].RoleName == "甲"
                    && bossList[0].RoleLv == 170 && bossList[0].DressList.Count == 2
                    && bossList[0].DressList[0].DressType == 1 && bossList[0].DressList[0].DressId == 500
                    && bossList[0].DressList[1].DressId == 501 && bossList[0].RewardState == 1
                    && bossList[1].BossId == 2220107 && bossList[1].DressList.Count == 0 && bossList[1].RewardState == 0;
                bool b18801Mirror = CountProto(logs18801, Proto.FIRSTBLOOD_DETAIL_QUERY) == 2; // 逐条查:2 条→2 次 18806

                byte[] p18801Dungeon = new CliVerify.Pkt().C(97).C(1).H(1)
                    .C(1).I(3300001).L(9101).S("乙").H(88).C(2).C(1).S("pic2").I(1)
                        .H(1).C(3).I(700)
                        .C(1)
                    .Bytes();
                List<string> logs18801Dungeon = CaptureLogs(() => FeedFb("On18801", p18801Dungeon));
                bool b18801Bucket = fbModel.ListByType[96].Count == 2 // 97 桶不覆盖 96 桶(type 收口分桶隔离)
                    && fbModel.ListByType.TryGetValue(97, out var dunList) && dunList.Count == 1 && dunList[0].BossId == 3300001;
                // B3修复:18806 逐条查询只在 type==96 分支触发(老端 ts:106-139 循环在 if(scmd.type==96) 块内,
                // 97 只走红点不镜像)——type=97 喂包不应触发任何 18806 发送。
                bool b18801DungeonNoMirror = CountProto(logs18801Dungeon, Proto.FIRSTBLOOD_DETAIL_QUERY) == 0;
                Debug.Log("CLIVERIFY fbfestival 18801 列表(嵌套探针) bossN=" + (bossList?.Count ?? -1)
                    + " mirror18806x=" + CountProto(logs18801, Proto.FIRSTBLOOD_DETAIL_QUERY)
                    + " ok=" + b18801 + " mirrorOk=" + b18801Mirror + " bucketOk=" + b18801Bucket
                    + " dungeonNoMirror(B3)=" + b18801DungeonNoMirror);

                // ======================================================================================
                // B. 18802 领奖(正常)+ B4三路成功镜像(type分流) + 死分支 Type=96&&Subtype=2 反射断言禁发
                // ======================================================================================
                byte[] p18802 = new CliVerify.Pkt().C(97).C(1).I(1).I(3300001).H(2)
                    .C(0).I(16010001).I(1)
                    .C(2).I(0).I(20)
                    .Bytes();
                List<string> logs18802Dungeon = CaptureLogs(() => FeedFb("On18802", p18802));
                bool b18802 = fbModel.LastClaimType == 97 && fbModel.LastClaimSubtype == 1 && fbModel.LastClaimCode == 1
                    && fbModel.LastClaimBossId == 3300001 && fbModel.LastClaimRewardList.Count == 2
                    && fbModel.LastClaimRewardList[0].GoodsId == 16010001 && fbModel.LastClaimRewardList[0].Num == 1
                    && fbModel.LastClaimRewardList[1].Type == 2 && fbModel.LastClaimRewardList[1].Num == 20;
                // B4 镜像①:type==97&&subtype==1 成功后发 18801(97,1)(ts:162)。
                bool b18802MirrorDungeon = CountProto(logs18802Dungeon, Proto.FIRSTBLOOD_LIST) == 1;

                // B4 镜像②:type==96 成功后发 18801(96,subtype)(ts:149)。
                byte[] p18802Boss = new CliVerify.Pkt().C(96).C(2).I(1).I(2220107).H(0).Bytes();
                List<string> logs18802Boss = CaptureLogs(() => FeedFb("On18802", p18802Boss));
                bool b18802MirrorBoss = CountProto(logs18802Boss, Proto.FIRSTBLOOD_LIST) == 1;

                var fbTyped = (Shenxiao.Module.Core.FirstBlood.FirstBloodController)fbCtrl;
                List<string> logsDead = CaptureLogs(() => fbTyped.ClaimReward(96, 2, 2220105)); // 死分支:必须拒绝
                bool b18802DeadRejected = CountProto(logsDead, Proto.FIRSTBLOOD_REWARD_CLAIM) == 0
                    && logsDead.Exists(l => l.Contains("拒绝发送") && l.Contains("Type=96&&Subtype=2"));
                List<string> logsValid = CaptureLogs(() => fbTyped.ClaimReward(96, 1, 2220105)); // 同 type 不同 subtype:必须放行
                bool b18802ValidSent = CountProto(logsValid, Proto.FIRSTBLOOD_REWARD_CLAIM) == 1
                    && !logsValid.Exists(l => l.Contains("拒绝发送"));
                bool b18802Dead = b18802DeadRejected && b18802ValidSent;
                Debug.Log("CLIVERIFY fbfestival 18802 领奖 rewardN=" + fbModel.LastClaimRewardList.Count
                    + " ok=" + b18802 + " mirror97(B4)=" + b18802MirrorDungeon + " mirror96(B4)=" + b18802MirrorBoss
                    + " deadBranchRejected=" + b18802DeadRejected + " validSubtypeSent=" + b18802ValidSent);

                // ======================================================================================
                // C. 18803 提醒(无Code) / 18804 神符本领奖(二层嵌套,末尾Time:64) / 18805 红点 / 18806 详情 / 18807 全服归属奖
                // ======================================================================================
                byte[] p18803 = new CliVerify.Pkt().C(96).C(1).S("甲甲").S("大妖Boss").Bytes();
                FeedFb("On18803", p18803);
                bool b18803 = fbModel.NoticeType == 96 && fbModel.NoticeSubtype == 1
                    && fbModel.NoticeRoleName == "甲甲" && fbModel.NoticeBossName == "大妖Boss";

                byte[] p18804 = new CliVerify.Pkt().C(105).C(1).I(13001).C(2).H(1)
                    .L(9201).S("丙").C(1).H(250).C(1).C(3).S("pic3").I(5)
                        .H(1).C(4).I(800)
                        .L(1700000000)
                    .Bytes();
                FeedFb("On18804", p18804);
                bool b18804 = fbModel.RuneClaimByDunId.TryGetValue(13001, out var runeState) && runeState.RewardState == 2
                    && runeState.PassRoleList.Count == 1 && runeState.PassRoleList[0].RoleId == 9201
                    && runeState.PassRoleList[0].RoleName == "丙" && runeState.PassRoleList[0].DressList.Count == 1
                    && runeState.PassRoleList[0].DressList[0].DressId == 800 && runeState.PassRoleList[0].Time == 1700000000;

                byte[] p18805 = new CliVerify.Pkt().C(105).C(1).H(2).I(13001).C(1).I(13002).C(0).Bytes();
                FeedFb("On18805", p18805);
                bool b18805 = fbModel.RedPointByType.TryGetValue(105, out var redList) && redList.Count == 2
                    && redList[0].DunId == 13001 && redList[0].ShowPoint == 1 && redList[1].DunId == 13002 && redList[1].ShowPoint == 0;

                byte[] p18806 = new CliVerify.Pkt().C(96).C(1).I(2220105).C(1).Bytes();
                FeedFb("On18806", p18806);
                bool b18806 = fbModel.SharedStatusByBossId.TryGetValue(2220105, out int sharedStatus) && sharedStatus == 1;

                byte[] p18807 = new CliVerify.Pkt().C(96).C(1).I(1).I(2220105).H(1).C(3).I(0).I(666666).Bytes();
                FeedFb("On18807", p18807);
                bool b18807 = fbModel.LastGuildClaimType == 96 && fbModel.LastGuildClaimCode == 1
                    && fbModel.LastGuildClaimBossId == 2220105 && fbModel.LastGuildClaimRewardList.Count == 1
                    && fbModel.LastGuildClaimRewardList[0].Type == 3 && fbModel.LastGuildClaimRewardList[0].Num == 666666
                    // 18807 与 18802 分桶不混:LastClaimBossId 仍是 B 段最后一次成功 18802(B4镜像②,bossId=2220107)
                    // 写入的值,未被 18807 覆盖(B4 修复新增镜像②喂包后此探针期望值同步跟随,原 3300001 已被其覆盖)
                    && fbModel.LastClaimBossId == 2220107;

                byte[] p18800 = new CliVerify.Pkt().I(1234).Bytes();
                FeedFb("On18800", p18800);
                bool b18800 = fbModel.LastErrorCode == 1234;

                // B4 镜像③:18802 type==105&&subtype==1 成功后本地置位(RedPoint 桶 ShowPoint=2 +
                // RuneClaim RewardState=1),复用本段已铺好的 105@13001 数据(上面 18804/18805 喂包)。
                byte[] p18802Rune = new CliVerify.Pkt().C(105).C(1).I(1).I(13001).H(0).Bytes();
                FeedFb("On18802", p18802Rune);
                bool b18802RuneLocal = fbModel.RedPointByType[105].Find(e => e.DunId == 13001).ShowPoint == 2
                    && fbModel.RuneClaimByDunId[13001].RewardState == 1
                    && fbModel.RedPointByType[105].Find(e => e.DunId == 13002).ShowPoint == 0; // 未命中的 dun 不受影响

                Debug.Log("CLIVERIFY fbfestival 18803/04/05/06/07/00 notice=" + b18803 + " rune=" + b18804
                    + " redpoint=" + b18805 + " detail=" + b18806 + " guildClaim=" + b18807 + " errPush=" + b18800
                    + " mirror105local(B4)=" + b18802RuneLocal);

                // ======================================================================================
                // D. GAME_START/CHANGE_LEVEL 老端触发链镜像(18801×2+18805×1 / CHANGE_LEVEL 仅18801×2)
                // ======================================================================================
                // Dispose() 复位 _lastLevel=-1(+清 Model,已在上面各段读完落地断言,清空无害):
                // Controller.Instance 是进程内单例,跨多次 CliVerify 运行会残留上次的 _lastLevel,
                // 不复位会导致 CHANGE_LEVEL 去抖断言在重复运行时假失败。
                fbTyped.Dispose();
                MethodInfo mGameStart = fbType.GetMethod("OnGameStart", F);
                List<string> logsGameStart = CaptureLogs(() => mGameStart.Invoke(fbCtrl, null));
                bool b18801GameStart = CountProto(logsGameStart, Proto.FIRSTBLOOD_LIST) == 2 && CountProto(logsGameStart, Proto.FIRSTBLOOD_REDPOINT_LIST) == 1;

                // B2修复:CHANGE_LEVEL 仅在 level 精确等于 LIMIT_LV(130,FirstBloodModel.ts:28)时补发
                // 18801(96,1) 一条(老端 ts:57-63,无 97·1)——非临界值不应发送。
                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 100; // 非临界值:不应发送
                Shenxiao.Module.Core.Role.RoleModel.Instance.MarkBaseInfoReady();
                MethodInfo mRoleUpdate = fbType.GetMethod("OnRoleInfoUpdate", F);
                List<string> logsLevelNonCritical = CaptureLogs(() => mRoleUpdate.Invoke(fbCtrl, null));
                bool b18801NonCritical = CountProto(logsLevelNonCritical, Proto.FIRSTBLOOD_LIST) == 0;

                Shenxiao.Module.Core.Role.RoleModel.Instance.Level = 130; // 临界值:应发 18801(96,1) 一条(仅96,无97)
                List<string> logsLevel130 = CaptureLogs(() => mRoleUpdate.Invoke(fbCtrl, null));
                bool b18801ChangeLevel = CountProto(logsLevel130, Proto.FIRSTBLOOD_LIST) == 1 && CountProto(logsLevel130, Proto.FIRSTBLOOD_REDPOINT_LIST) == 0;
                List<string> logsLevelSame = CaptureLogs(() => mRoleUpdate.Invoke(fbCtrl, null)); // 同级不重发(去抖)
                bool b18801Dedup = CountProto(logsLevelSame, Proto.FIRSTBLOOD_LIST) == 0;
                bool bTrigger = b18801GameStart && b18801NonCritical && b18801ChangeLevel && b18801Dedup;
                Debug.Log("CLIVERIFY fbfestival GAME_START/CHANGE_LEVEL镜像 gameStart18801x2+18805x1=" + b18801GameStart
                    + " nonCriticalNoSend(B2)=" + b18801NonCritical + " changeLevel130_18801x1(B2)=" + b18801ChangeLevel
                    + " dedupNoResend=" + b18801Dedup + " ok=" + bTrigger);

                bool fbPass = b18801 && b18801Mirror && b18801Bucket && b18801DungeonNoMirror
                    && b18802 && b18802MirrorDungeon && b18802MirrorBoss && b18802RuneLocal && b18802Dead
                    && b18803 && b18804 && b18805 && b18806 && b18807 && b18800 && bTrigger;

                fbModel.Reset();
                bool fbResetOk = fbModel.ListByType.Count == 0 && fbModel.RedPointByType.Count == 0 && fbModel.RuneClaimByDunId.Count == 0;
                Debug.Log("CLIVERIFY fbfestival FirstBlood VERDICT pass=" + fbPass + " resetOk=" + fbResetOk);

                // ======================================================================================
                // E. Festival 19401(既有逻辑复验+RewardList落地+自动19403镜像)
                // ======================================================================================
                byte[] p19401 = new CliVerify.Pkt().H(555).C(3).C(1).H(42).I(12345).I(1700000000)
                    .H(2).H(10).C(1).C(0).H(20).C(0).C(1)
                    .Bytes();
                List<string> logs19401 = CaptureLogs(() => FeedFv("On19401", p19401));
                bool b19401 = fvModel.Uid == 555 && fvModel.ActId == 3 && fvModel.Type == 1 && fvModel.Lv == 42
                    && fvModel.Exp == 12345 && fvModel.ExpiredTime == 1700000000 && fvModel.GetEntranceOpenState()
                    && fvModel.RewardList.Count == 2 && fvModel.RewardList[0].Lv == 10 && fvModel.RewardList[0].Status1 == 1
                    && fvModel.RewardList[1].Lv == 20 && fvModel.RewardList[1].Status2 == 1;
                bool b19401Mirror = CountProto(logs19401, Proto.FESTIVAL_TASK_LIST) == 1; // 老端 Controller:140 镜像:自动 type=0 求任务列表
                Debug.Log("CLIVERIFY fbfestival 19401 宝录(RewardList落地+镜像19403) uid=" + fvModel.Uid
                    + " rewardN=" + fvModel.RewardList.Count + " ok=" + b19401 + " mirror19403=" + b19401Mirror);

                byte[] p19401Closed = new CliVerify.Pkt().H(0).C(0).C(0).H(0).I(0).I(0).H(0).Bytes();
                // B6修复:19403 只在 GetEntranceOpenState()==true 分支发送(老端 ts:134-141 在 if 内);
                // closed 分支(uid=0)不应再镜像发 19403——用日志捕获法验证(参照本 Case 已有技术)。
                List<string> logs19401Closed = CaptureLogs(() => FeedFv("On19401", p19401Closed));
                bool b19401Closed = !fvModel.GetEntranceOpenState() && fvModel.RewardList.Count == 0
                    && CountProto(logs19401Closed, Proto.FESTIVAL_TASK_LIST) == 0;

                // ======================================================================================
                // F. 19400 错误推送 / 19402 领等级奖(非空即成功) / 19403 任务列表(二层嵌套) / 19404 领任务经验
                // ======================================================================================
                int fvEventCount = 0;
                var fvEventProtoIds = new List<int>();
                System.Action<int> onFvEvent = protoId => { fvEventCount++; fvEventProtoIds.Add(protoId); };
                EventDispatcher.On<int>(GlobalEvent.EVT_FESTIVAL_UPDATE, onFvEvent);

                byte[] p19400 = new CliVerify.Pkt().I(1720099).S("test_arg").Bytes();
                FeedFv("On19400", p19400);

                byte[] p19402Ok = new CliVerify.Pkt().H(2)
                    .C(0).I(16010001).I(1)
                    .C(1).I(38240101).I(5)
                    .Bytes();
                FeedFv("On19402", p19402Ok);
                bool b19402Ok = fvModel.LastLevelAwardReward.Count == 2 && fvModel.LastLevelAwardSuccess
                    && fvModel.LastLevelAwardReward[0].ObjectTypeId == 16010001 && fvModel.LastLevelAwardReward[1].Num == 5;
                byte[] p19402Empty = new CliVerify.Pkt().H(0).Bytes();
                FeedFv("On19402", p19402Empty);
                bool b19402Empty = fvModel.LastLevelAwardReward.Count == 0 && !fvModel.LastLevelAwardSuccess;
                bool b19402 = b19402Ok && b19402Empty;

                byte[] p19403 = new CliVerify.Pkt().H(2)
                    .C(1).H(2)
                        .H(1001).C(1).I(3).C(0)
                        .H(1002).C(0).I(0).C(0)
                    .I(1700100000)
                    .C(2).H(1)
                        .H(2001).C(1).I(1).C(1)
                    .I(1700200000)
                    .Bytes();
                FeedFv("On19403", p19403);
                bool b19403 = fvModel.TaskGroupsByType.TryGetValue(1, out var dayGroup) && dayGroup.TaskList.Count == 2
                    && dayGroup.TaskList[0].TaskId == 1001 && dayGroup.TaskList[0].FinishTimes == 1 && dayGroup.TaskList[0].CurNum == 3
                    && dayGroup.TaskList[1].TaskId == 1002 && dayGroup.RefreshTime == 1700100000
                    && fvModel.TaskGroupsByType.TryGetValue(2, out var weekGroup) && weekGroup.TaskList.Count == 1
                    && weekGroup.TaskList[0].TaskId == 2001 && weekGroup.TaskList[0].Status == 1 && weekGroup.RefreshTime == 1700200000;

                byte[] p19404 = new CliVerify.Pkt().I(888).Bytes();
                FeedFv("On19404", p19404);
                bool b19404 = fvModel.LastTaskExpClaimed == 888;

                EventDispatcher.Off<int>(GlobalEvent.EVT_FESTIVAL_UPDATE, onFvEvent);
                // 5 次 emit:19400×1 + 19402(Ok/Empty 各一次)×2 + 19403×1 + 19404×1
                int levelAwardEvtN = fvEventProtoIds.FindAll(p => p == Proto.FESTIVAL_LEVEL_AWARD_CLAIM).Count;
                bool bFvEvents = fvEventCount == 5 && fvEventProtoIds.Contains(Proto.FESTIVAL_ERROR) && levelAwardEvtN == 2
                    && fvEventProtoIds.Contains(Proto.FESTIVAL_TASK_LIST) && fvEventProtoIds.Contains(Proto.FESTIVAL_TASK_EXP_CLAIM);

                Debug.Log("CLIVERIFY fbfestival 19400/02/03/04 errPush=true award=" + b19402
                    + " task二层嵌套=" + b19403 + " taskExp=" + b19404 + " events=" + fvEventCount + " eventsOk=" + bFvEvents);

                // ======================================================================================
                // G. 19405 send-only 死回执反射断言(无 On19405 方法;RequestPurchase 公开且能发送)
                // ======================================================================================
                bool noOn19405 = fvType.GetMethod("On19405", F) == null;
                var fvTyped = (Shenxiao.Module.Core.Festival.FestivalController)fvCtrl;
                List<string> logs19405 = CaptureLogs(() => fvTyped.RequestPurchase(1));
                bool b19405Sent = CountProto(logs19405, Proto.FESTIVAL_PURCHASE) == 1;
                bool b19405 = noOn19405 && b19405Sent;
                Debug.Log("CLIVERIFY fbfestival 19405 无回执死号断言 noRecvHandler=" + noOn19405 + " sendOk=" + b19405Sent + " ok=" + b19405);

                bool fvPass = b19401 && b19401Mirror && b19401Closed && b19402 && b19403 && b19404 && bFvEvents && b19405;

                fvModel.Reset();
                bool fvResetOk = fvModel.Uid == 0 && fvModel.RewardList.Count == 0 && fvModel.TaskGroupsByType.Count == 0
                    && fvModel.LastLevelAwardReward.Count == 0;
                Debug.Log("CLIVERIFY fbfestival Festival VERDICT pass=" + fvPass + " resetOk=" + fvResetOk);

                bool pass = configOk && fbPass && fbResetOk && fvPass && fvResetOk;
                Debug.Log("CLIVERIFY fbfestival VERDICT config=" + configOk + " firstBlood=" + fbPass + " festival=" + fvPass + " pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (System.Exception e)
            {
                Debug.LogError("CLIVERIFY fbfestival EXCEPTION " + e);
                return 3;
            }
        }

        private static async Task<JObject> LoadServerConfig(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                Debug.LogError("CLIVERIFY fbfestival 缺配表: " + key + "(跑「神霄/配表/同步客户端配置」或手动拷贝)");
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }
    }
}
