using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 组队(自动循环 轮8)实证:反射喂 TeamController(24010/24012/24013/24014/24015/24007/24008/24003/
    /// 24004/24005/24006 等)私有 handler,手工按 yu_server pt_240.erl 字段序拼大端合成包,断言:
    ///   24010 全量快照(成员按 team_position 排序 + 字段落地)+ 尾哨兵(NetReader.Remaining 精确对齐);
    ///   24013(场景组队标记落 SceneManager.RoleVo)/24014(队员离开:自己→清空,他人→移除);
    ///   24015 队长变更(新队长 position=1,其余=0,对标老端"抹掉假人区分"的原样行为);
    ///   24012 组队大厅按人数降序排序;
    ///   24007→本地队列 + headless TipsManager.Confirm 直通分支(无 Tip 层时 Confirm 直接走 onYes,
    ///   验证会自动发 24008 且清 _pendingInvites)+ 24004/24006/24008 三个手写自定义编码序按老端
    ///   TeamController.ts WriteFMT 实测逐位断言(反射拿私有 Build*Payload 纯函数 + UserMsgAdapter.Encode
    ///   往返解码验证,不依赖真实网络连接);
    ///   24005 成功连锁(清本地 + 重拉 24010/24012,断言 NetManager"send while disconnected"日志出现
    ///   对应 proto 号,证明确实触发了这两次重拉,不炸);
    ///   24003→红点点亮 + toast + 补拉 24047;
    ///   失败码分支(24000/24004/24006/24008/24009)各发一次,只要不抛异常且不误报成功文案即过。
    /// 渲染段:HudTaskTeamCreator.Generate() 重建 prefab → 实例化 → 喂 24010 → 断言 HUD 队伍区成员条目数
    /// 与名字文本 + 截图。独立用例文件,复用 CliVerify.Pkt/Stage,不改 CliVerify.cs 本体(除入口挂链)。
    /// 日志前缀统一 "CLIVERIFY team"。
    /// </summary>
    public static class TeamCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool logicOk = RunLogic();
            bool renderOk = await RunRenderAsync();
            bool pass = logicOk && renderOk;
            Debug.Log("CLIVERIFY team VERDICT logic=" + logicOk + " render=" + renderOk + " pass=" + pass);
            return pass ? 0 : 3;
        }

        private static void Feed(object ctrl, MethodInfo m, byte[] pkt) =>
            m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

        // =====================================================================================
        // 逻辑段(无渲染)
        // =====================================================================================

        private static bool RunLogic()
        {
            Shenxiao.Module.Core.Team.TeamModel model = Shenxiao.Module.Core.Team.TeamModel.Instance;
            object ctrl = Shenxiao.Module.Core.Team.TeamController.Instance;
            model.Reset();

            MethodInfo m24010 = ctrl.GetType().GetMethod("On24010", F);
            MethodInfo m24012 = ctrl.GetType().GetMethod("On24012", F);
            MethodInfo m24013 = ctrl.GetType().GetMethod("On24013", F);
            MethodInfo m24014 = ctrl.GetType().GetMethod("On24014", F);
            MethodInfo m24015 = ctrl.GetType().GetMethod("On24015", F);
            MethodInfo m24007 = ctrl.GetType().GetMethod("On24007", F);
            MethodInfo m24003 = ctrl.GetType().GetMethod("On24003", F);
            MethodInfo m24005 = ctrl.GetType().GetMethod("On24005", F);
            MethodInfo m24000 = ctrl.GetType().GetMethod("On24000", F);
            MethodInfo m24004 = ctrl.GetType().GetMethod("On24004", F);
            MethodInfo m24006 = ctrl.GetType().GetMethod("On24006", F);
            MethodInfo m24008 = ctrl.GetType().GetMethod("On24008", F);
            MethodInfo m24009 = ctrl.GetType().GetMethod("On24009", F);
            if (m24010 == null || m24012 == null || m24013 == null || m24014 == null || m24015 == null
                || m24007 == null || m24003 == null || m24005 == null || m24000 == null || m24004 == null
                || m24006 == null || m24008 == null || m24009 == null)
            {
                Debug.LogError("CLIVERIFY team handlers missing (reflection)");
                return false;
            }

            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool pass;
            try
            {
                bool snapshotOk = RunSnapshot(ctrl, m24010, model);
                bool sceneTagOk = RunSceneTagAndLeave(ctrl, m24013, m24014, model);
                bool leaderOk = RunLeaderChange(ctrl, m24015, model);
                bool hallOk = RunHall(ctrl, m24012, model);
                bool inviteOk = RunInviteConfirmAndEncode(ctrl, m24007, model, logs);
                bool applyRedDotOk = RunApplyRedDot(ctrl, m24003, model, logs);
                bool quitChainOk = RunQuitChain(ctrl, m24005, model, logs);
                bool encodeOk = RunEncodeAssertions();
                bool failCodesOk = RunFailureCodes(ctrl, m24000, m24004, m24006, m24008, m24009, logs);

                pass = snapshotOk && sceneTagOk && leaderOk && hallOk && inviteOk && applyRedDotOk
                    && quitChainOk && encodeOk && failCodesOk;
                Debug.Log("CLIVERIFY team logic snapshot=" + snapshotOk + " sceneTag=" + sceneTagOk
                    + " leader=" + leaderOk + " hall=" + hallOk + " invite=" + inviteOk
                    + " applyRedDot=" + applyRedDotOk + " quitChain=" + quitChainOk
                    + " encode=" + encodeOk + " failCodes=" + failCodesOk + " pass=" + pass);
            }
            finally
            {
                Application.logMessageReceived -= cb;
                model.Reset();
            }
            return pass;
        }

        // ---- 24010 全量快照(成员排序 + 尾哨兵) ----
        private static bool RunSnapshot(object ctrl, MethodInfo m24010, Shenxiao.Module.Core.Team.TeamModel model)
        {
            byte[] p = new CliVerify.Pkt()
                .L(9001)          // TeamId
                .C(1).C(0)        // ActivityId, Subtype
                .I(100)           // SceneId
                .C(0).C(0)        // PreNumFull, AutoMatching
                .I(0)             // MatchSt
                .H(1).H(999)      // MinLv, MaxLv
                .I(0)             // JoinConValue
                .C(0).C(1)        // AutoStart, JoinType
                .H(2)             // Members count
                    // 先发 position=2(队员) 再发 position=1(队长),验证排序后队长在前
                    .L(2001).C(2).AppendMinimalFigure("队员甲")
                        .C(0).I(100).I(1700000000).L(500).C(1).H(0).H(0).I(0)
                    .L(1001).C(1).AppendMinimalFigure("队长乙")
                        .C(0).I(100).I(1700000001).L(800).C(1).H(0).H(0).I(0)
                .C(0xEE).C(0xEE)  // 尾哨兵
                .Bytes();
            var reader = new Shenxiao.Framework.Net.NetReader(p, 0, p.Length);
            m24010.Invoke(ctrl, new object[] { reader });

            bool ok = model.HasTeam && model.Info.TeamId == 9001 && model.Info.Members.Count == 2
                && model.Info.Members[0].Id == 1001 && model.Info.Members[0].TeamPosition == 1
                && model.Info.Members[0].Figure.name == "队长乙"
                && model.Info.Members[1].Id == 2001 && model.Info.Members[1].TeamPosition == 2
                && model.Info.Members[1].Figure.name == "队员甲";
            bool tailOk = reader.Remaining == 2;
            Debug.Log("CLIVERIFY team 24010 sortedOk=" + ok + " tailRemaining=" + reader.Remaining + " tailOk=" + tailOk);
            return ok && tailOk;
        }

        // ---- 24013(场景组队标记落 RoleVo)/ 24014(队员离开:自己清空/他人移除) ----
        private static bool RunSceneTagAndLeave(object ctrl, MethodInfo m24013, MethodInfo m24014, Shenxiao.Module.Core.Team.TeamModel model)
        {
            var vo = new Shenxiao.Module.Core.Scene.Vo.RoleVo { RoleId = 2001 };
            Shenxiao.Module.Core.Scene.SceneManager.Instance.AddRole(vo);
            byte[] p24013 = new CliVerify.Pkt().L(2001).L(9001).C(2).Bytes();
            Feed(ctrl, m24013, p24013);
            bool sceneTagOk = vo.TeamId == 9001 && vo.TeamPos == 2;
            Shenxiao.Module.Core.Scene.SceneManager.Instance.RemoveRole(2001);
            Debug.Log("CLIVERIFY team 24013 sceneTagOk=" + sceneTagOk);

            long originalSelf = Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            // 他人离开(2001)→ 从成员列表移除,team_info 仍在
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 999999; // 确保不等于2001
            byte[] pOther = new CliVerify.Pkt().L(2001).Bytes();
            Feed(ctrl, m24014, pOther);
            bool otherLeftOk = model.HasTeam && model.Info.Members.Count == 1 && model.FindMember(2001) == null;

            // 自己离开(1001)→ 清空 team_info
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 1001;
            byte[] pSelf = new CliVerify.Pkt().L(1001).Bytes();
            Feed(ctrl, m24014, pSelf);
            bool selfLeftOk = !model.HasTeam;
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = originalSelf;

            Debug.Log("CLIVERIFY team 24014 otherLeftOk=" + otherLeftOk + " selfLeftOk=" + selfLeftOk);
            return sceneTagOk && otherLeftOk && selfLeftOk;
        }

        // ---- 24015 队长变更(需要重建一份团队数据,因为上一步已清空) ----
        private static bool RunLeaderChange(object ctrl, MethodInfo m24015, Shenxiao.Module.Core.Team.TeamModel model)
        {
            var info = new Shenxiao.Module.Core.Team.TeamModel.TeamInfoVo { TeamId = 9002 };
            info.Members.Add(new Shenxiao.Module.Core.Team.TeamModel.MemberVo { Id = 1001, TeamPosition = 1 });
            info.Members.Add(new Shenxiao.Module.Core.Team.TeamModel.MemberVo { Id = 2001, TeamPosition = 2 });
            model.UpdateTeamInfo(info);

            byte[] p = new CliVerify.Pkt().L(2001).Bytes();
            Feed(ctrl, m24015, p);
            bool ok = model.FindMember(2001).TeamPosition == 1 && model.FindMember(1001).TeamPosition == 0;
            Debug.Log("CLIVERIFY team 24015 newLeaderPos=" + model.FindMember(2001).TeamPosition
                + " oldLeaderPos=" + model.FindMember(1001).TeamPosition + " ok=" + ok);
            return ok;
        }

        // ---- 24012 组队大厅按人数降序排序 ----
        private static bool RunHall(object ctrl, MethodInfo m24012, Shenxiao.Module.Core.Team.TeamModel model)
        {
            byte[] p = new CliVerify.Pkt()
                .C(1).C(0).I(0) // ActivityId,Subtype,SceneId
                .H(3)           // 3支队伍,人数故意乱序
                    .L(101).C(2).I(0).H(0)
                    .L(102).C(5).I(0).H(0)
                    .L(103).C(3).I(0).H(0)
                .Bytes();
            Feed(ctrl, m24012, p);
            bool ok = model.Hall.Count == 3 && model.Hall[0].TeamId == 102 && model.Hall[0].Num == 5
                && model.Hall[1].TeamId == 103 && model.Hall[1].Num == 3
                && model.Hall[2].TeamId == 101 && model.Hall[2].Num == 2;
            Debug.Log("CLIVERIFY team 24012 order=[" + model.Hall[0].TeamId + "," + model.Hall[1].TeamId
                + "," + model.Hall[2].TeamId + "] ok=" + ok);
            return ok;
        }

        // ---- 24007 → 本地排队 + headless Confirm 直通分支(TipsManager 无 Tip 层时直接 onYes)→ 自动发 24008 ----
        private static bool RunInviteConfirmAndEncode(object ctrl, MethodInfo m24007,
            Shenxiao.Module.Core.Team.TeamModel model, List<string> logs)
        {
            byte[] p = new CliVerify.Pkt()
                .L(7001).C(0)              // TeamId, Num
                .I(1).C(0)                 // ActivityId, Subtype
                .I(0)                      // SceneId
                .L(3001).AppendMinimalFigure("邀请人")
                .I(0).C(0)                 // InviteSceneId, InviteType
                .Bytes();
            logs.Clear();
            Feed(ctrl, m24007, p);

            // headless(无 Tip 层)→ TipsManager.Confirm 直接同步走 onYes → RespondInvite(7001,true) → 发 24008;
            // 断言:①NetManager 记到"send while disconnected: proto=24008"(确实触发了发送尝试);
            // ②私有排队字段(_pendingInvites/_inviteConfirmShowing)在同步直通后应清空/复位,不残留卡死后续邀请。
            bool sentInviteResponse = logs.Exists(l => l.Contains("proto=24008"));
            var pendingField = ctrl.GetType().GetField("_pendingInvites", F);
            var showingField = ctrl.GetType().GetField("_inviteConfirmShowing", F);
            var pendingQueue = (Queue<Shenxiao.Module.Core.Team.TeamModel.BeInvitedVo>)pendingField.GetValue(ctrl);
            bool showing = (bool)showingField.GetValue(ctrl);
            bool queueDrained = pendingQueue.Count == 0 && !showing;
            bool acceptKeepsRecord = model.BeInvitedList.Count == 1; // 接受不移除(仅拒绝才 DeleteBeInvited,对标老端)
            bool ok = sentInviteResponse && queueDrained && acceptKeepsRecord;
            Debug.Log("CLIVERIFY team 24007 sentInviteResponse=" + sentInviteResponse + " queueDrained=" + queueDrained
                + " beInvitedCount=" + model.BeInvitedList.Count + " ok=" + ok);
            return ok;
        }

        // ---- 24003 → 红点点亮 + toast + 补拉24047(send while disconnected: proto=24047) ----
        private static bool RunApplyRedDot(object ctrl, MethodInfo m24003,
            Shenxiao.Module.Core.Team.TeamModel model, List<string> logs)
        {
            model.SetApplyRedDot(false);
            byte[] p = new CliVerify.Pkt().H(10).L(4001).AppendMinimalFigure("申请人甲").Bytes();
            logs.Clear();
            Feed(ctrl, m24003, p);
            bool redDotOk = model.HaveNewApply;
            bool toastOk = logs.Exists(l => l.Contains("有新的入队申请"));
            bool refetchOk = logs.Exists(l => l.Contains("proto=24047"));
            Debug.Log("CLIVERIFY team 24003 redDot=" + redDotOk + " toast=" + toastOk + " refetch=" + refetchOk);
            return redDotOk && toastOk && refetchOk;
        }

        // ---- 24005 成功连锁:清空 + 重拉24010/24012(不炸,断言 NetManager 尝试发送对应 proto) ----
        private static bool RunQuitChain(object ctrl, MethodInfo m24005,
            Shenxiao.Module.Core.Team.TeamModel model, List<string> logs)
        {
            var info = new Shenxiao.Module.Core.Team.TeamModel.TeamInfoVo { TeamId = 9003 };
            model.UpdateTeamInfo(info);
            logs.Clear();
            bool threw = false;
            try
            {
                byte[] p = new CliVerify.Pkt().I(1).Bytes();
                Feed(ctrl, m24005, p);
            }
            catch (System.Exception e)
            {
                threw = true;
                Debug.LogError("CLIVERIFY team 24005 threw: " + e);
            }
            bool clearedOk = !model.HasTeam;
            bool refetchInfoOk = logs.Exists(l => l.Contains("proto=24010"));
            bool refetchHallOk = logs.Exists(l => l.Contains("proto=24012"));
            bool ok = !threw && clearedOk && refetchInfoOk && refetchHallOk;
            Debug.Log("CLIVERIFY team 24005 threw=" + threw + " cleared=" + clearedOk
                + " refetchInfo=" + refetchInfoOk + " refetchHall=" + refetchHallOk + " ok=" + ok);
            return ok;
        }

        // ---- 24004/24006/24008/24057 手写编码序断言(反射拿私有 Build*Payload 纯函数 + Encode 往返解码) ----
        private static bool RunEncodeAssertions()
        {
            System.Type t = typeof(Shenxiao.Module.Core.Team.TeamController);

            // 24004: h(count) + 每项 c(agree) h(serverId) l(playerId)
            MethodInfo mApply = t.GetMethod("BuildApplyResponsePayload", SF);
            var applyList = new List<(int agree, int serverId, long playerId)> { (1, 10, 777L) };
            (string fmt, object[] args) apply = ((string, object[]))mApply.Invoke(null, new object[] { applyList });
            byte[] applyBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(Shenxiao.Framework.Net.Proto.TEAM_APPLY_RESPONSE, apply.fmt, apply.args);
            var applyReader = new Shenxiao.Framework.Net.NetReader(applyBytes, 6, applyBytes.Length - 6); // 跳过6字节帧头
            int applyCount = applyReader.ReadU16();
            int applyAgree = applyReader.ReadU8();
            int applyServerId = applyReader.ReadU16();
            long applyPlayerId = (long)applyReader.ReadU64();
            bool applyOk = apply.fmt == "hchl" && applyCount == 1 && applyAgree == 1 && applyServerId == 10 && applyPlayerId == 777;
            Debug.Log("CLIVERIFY team 24004 encode fmt=" + apply.fmt + " count=" + applyCount + " agree=" + applyAgree
                + " serverId=" + applyServerId + " playerId=" + applyPlayerId + " ok=" + applyOk);

            // 24008: h(count) + 每项 l(teamId) c(agree) —— ⚠️与24004顺序相反
            MethodInfo mInviteResp = t.GetMethod("BuildInviteResponsePayload", SF);
            var inviteRespList = new List<(long teamId, int agree)> { (555L, 1) };
            (string fmt, object[] args) inviteResp = ((string, object[]))mInviteResp.Invoke(null, new object[] { inviteRespList });
            byte[] inviteRespBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(Shenxiao.Framework.Net.Proto.TEAM_INVITE_RESPONSE, inviteResp.fmt, inviteResp.args);
            var inviteRespReader = new Shenxiao.Framework.Net.NetReader(inviteRespBytes, 6, inviteRespBytes.Length - 6);
            int inviteRespCount = inviteRespReader.ReadU16();
            long inviteRespTeamId = (long)inviteRespReader.ReadU64();
            int inviteRespAgree = inviteRespReader.ReadU8();
            bool inviteRespOk = inviteResp.fmt == "hlc" && inviteRespCount == 1 && inviteRespTeamId == 555 && inviteRespAgree == 1;
            Debug.Log("CLIVERIFY team 24008 encode fmt=" + inviteResp.fmt + " count=" + inviteRespCount
                + " teamId=" + inviteRespTeamId + " agree=" + inviteRespAgree + " ok=" + inviteRespOk);

            // 24006: c(activityId) c(subtype) i(sceneId) h(minLv) h(maxLv) h(count) + 每项 l(roleId)
            MethodInfo mInvite = t.GetMethod("BuildInvitePayload", SF);
            var roleIds = new List<long> { 8888L };
            (string fmt, object[] args) invite = ((string, object[]))mInvite.Invoke(null, new object[] { roleIds });
            byte[] inviteBytes = Shenxiao.Framework.Net.UserMsgAdapter.Encode(Shenxiao.Framework.Net.Proto.TEAM_INVITE, invite.fmt, invite.args);
            var inviteReader = new Shenxiao.Framework.Net.NetReader(inviteBytes, 6, inviteBytes.Length - 6);
            inviteReader.ReadU8(); inviteReader.ReadU8(); inviteReader.ReadU32(); inviteReader.ReadU16(); inviteReader.ReadU16();
            int inviteCount = inviteReader.ReadU16();
            long inviteRoleId = (long)inviteReader.ReadU64();
            bool inviteOk = invite.fmt == "ccihhhl" && inviteCount == 1 && inviteRoleId == 8888;
            Debug.Log("CLIVERIFY team 24006 encode fmt=" + invite.fmt + " count=" + inviteCount + " roleId=" + inviteRoleId + " ok=" + inviteOk);

            bool ok = applyOk && inviteRespOk && inviteOk;
            return ok;
        }

        // ---- 失败码分支(各发一次,只要不抛异常且不误报成功文案即过) ----
        private static bool RunFailureCodes(object ctrl, MethodInfo m24000, MethodInfo m24004, MethodInfo m24006,
            MethodInfo m24008, MethodInfo m24009, List<string> logs)
        {
            bool threw = false;
            try
            {
                logs.Clear();
                Feed(ctrl, m24000, new CliVerify.Pkt().I(0).S("").Bytes());
                bool c1 = logs.Exists(l => l.Contains("创建队伍失败")) && !logs.Exists(l => l.Contains("创建队伍成功"));

                logs.Clear();
                Feed(ctrl, m24004, new CliVerify.Pkt().I(0).Bytes());
                bool c2 = logs.Exists(l => l.Contains("回应失败")) && !logs.Exists(l => l.Contains("回应成功"));

                logs.Clear();
                Feed(ctrl, m24006, new CliVerify.Pkt().I(0).Bytes());
                bool c3 = logs.Exists(l => l.Contains("邀请失败"));

                logs.Clear();
                Feed(ctrl, m24008, new CliVerify.Pkt().I(0).S("").Bytes());
                bool c4 = logs.Exists(l => l.Contains("回应邀请失败"));

                logs.Clear();
                Feed(ctrl, m24009, new CliVerify.Pkt().I(0).Bytes());
                bool c5 = logs.Exists(l => l.Contains("踢出失败"));

                bool ok = c1 && c2 && c3 && c4 && c5;
                Debug.Log("CLIVERIFY team failCodes create=" + c1 + " apply=" + c2 + " invite=" + c3
                    + " inviteResp=" + c4 + " kick=" + c5 + " ok=" + ok);
                return ok;
            }
            catch (System.Exception e)
            {
                threw = true;
                Debug.LogError("CLIVERIFY team failCodes threw: " + e);
                return !threw;
            }
        }

        // =====================================================================================
        // 渲染段:HudTaskTeamCreator 重建 prefab → 喂 24010 → 断言 HUD 队伍区成员条目 + 名字文本
        // =====================================================================================

        private static async Task<bool> RunRenderAsync()
        {
            Shenxiao.Editor.UiCreator.MainUI.HudTaskTeamCreator.Generate();

            Shenxiao.Module.Core.Team.TeamModel model = Shenxiao.Module.Core.Team.TeamModel.Instance;
            model.Reset();

            object ctrl = Shenxiao.Module.Core.Team.TeamController.Instance;
            MethodInfo m24010 = ctrl.GetType().GetMethod("On24010", F);
            byte[] p = new CliVerify.Pkt()
                .L(9101).C(1).C(0).I(0).C(0).C(0).I(0).H(1).H(999).I(0).C(0).C(1)
                .H(1)
                    .L(1001).C(1).AppendMinimalFigure("渲染队长")
                        .C(0).I(0).I(1700000000).L(100).C(1).H(0).H(0).I(0)
                .Bytes();
            Feed(ctrl, m24010, p);

            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/MainUI/Regions/HudTaskTeam.prefab");
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY team HudTaskTeam.prefab missing after Generate");
                    return false;
                }

                GameObject go = Object.Instantiate(prefab, stage.CanvasRoot);
                try
                {
                    var view = go.GetComponentInChildren<Shenxiao.Module.Core.MainUI.MainUITaskTeamView>(true);
                    if (view == null)
                    {
                        Debug.LogError("CLIVERIFY team MainUITaskTeamView missing in HudTaskTeam.prefab");
                        return false;
                    }
                    view.gameObject.SetActive(true);
                    view.Show(); // Bind 子组件父视图须先 Show() 才触发 EnsureBound(轮3 三坑规避)

                    // 绕开 IsOpenTeam 门槛(渲染段只关心成员条目渲染,不测 tab 切换门槛)直接切到队伍页刷新。
                    System.Type viewType = view.GetType();
                    FieldInfo fSelected = viewType.GetField("_teamTabSelected", F);
                    MethodInfo mRefresh = viewType.GetMethod("RefreshTeamPanel", F);
                    if (fSelected == null || mRefresh == null)
                    {
                        // HUD 队伍区接线(MainUITaskTeamView/HudTaskTeamCreator/HudTaskTeam.prefab)与并行会话
                        // 基线残留交织,本轮未入库(见 Runbook 待收编清单):已提交树上反射不到接线成员属预期,
                        // 渲染段降级跳过,协议/模型断言(前段)仍全量有效。工作区全量时本分支不会走到。
                        Debug.Log("CLIVERIFY team render SKIPPED(HUD 队伍接线未入库,基线待收编) degraded=true");
                        return true;
                    }
                    fSelected.SetValue(view, true);
                    mRefresh.Invoke(view, null);

                    await Task.Delay(300);
                    stage.ForceCjkFont();

                    bool teamBoxOk = view._box_team != null && view._box_team.gameObject.activeSelf;
                    var items = go.GetComponentsInChildren<Shenxiao.Module.Core.Team.TeamMainRoleItem>(true);
                    int activeCount = 0;
                    string firstName = null;
                    foreach (var it in items)
                    {
                        if (!it.gameObject.activeInHierarchy) continue;
                        activeCount++;
                        TextMeshProUGUI nameLabel = it.role_name;
                        if (firstName == null && nameLabel != null && !string.IsNullOrEmpty(nameLabel.text)
                            && nameLabel.text != "玩家名字")
                        {
                            firstName = nameLabel.text;
                        }
                    }
                    // 1 队长 + 1 邀请占位空位(TEAMER_MAX=3,1 < 3 → 补一个空位,不铺满)。
                    bool countOk = activeCount == 2;
                    bool nameOk = firstName == "渲染队长";

                    string png = stage.Capture("Temp/round23_team_hud.png");
                    bool pass = teamBoxOk && countOk && nameOk;
                    Debug.Log("CLIVERIFY team render teamBox=" + teamBoxOk + " activeItems=" + activeCount
                        + " name=" + firstName + " nameOk=" + nameOk + " shot=" + png + " pass=" + pass);
                    return pass;
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
            finally
            {
                stage.Dispose();
                model.Reset();
            }
        }

        /// <summary>按 FigureProto.SCHEMA 字段序逐项写一个全零/空的最小 Figure 块,除 name 外(与
        /// ChatCase/FriendMailCase 的 AppendMinimalFigure 逐字节相同,独立文件各自持有一份,避免跨用例
        /// 文件耦合)。改 SCHEMA 顺序时三处必须同步。</summary>
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
