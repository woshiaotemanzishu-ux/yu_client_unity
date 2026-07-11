using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;

namespace Shenxiao.Module.Core.Team
{
    /// <summary>
    /// 组队协议控制器(对标老客户端 commonController/TeamController.ts,自动循环 轮8)。
    /// 24000-24063 段:桶1(客户端可发起+实质处理,25个)+ 桶2(纯服务器推送,14个)。
    ///
    /// 跳过(按规格§0):24011(委任队长,UI 四层链路全断的僵尸协议,老端 ShowPlayerMenu 空函数 +
    /// PlayerMenuView.ts 文件不存在)/24042(获取活动剩余次数,老端 handler 函数体为空且从未真发)/
    /// proto240 定义但 h5 全仓库零引用的 16 个 UNUSED 号/服务端 DEAD 号/区间内未分配号。
    /// 24062(催促开启活动)老端未注册 recv handler,纯 fire-and-forget,本端同样不 RegisterProtocal。
    /// 24057(跨服邀请 ack)r8_server 实证 write 子句全仓库零调用、真实 ack 走 24006——本端仍防御性注册
    /// recv,不依赖它但也不误判为"服务端返回空包异常"。
    ///
    /// **ControllerHub 未注册**(按纪律不碰该文件):需要在 ControllerHub.ALL 里补一行
    /// <c>TeamController.Instance,</c>,否则 GAME_START 不会自动拉取队伍信息——已在本轮汇报中列出。
    /// </summary>
    public sealed class TeamController : BaseController
    {
        public static readonly TeamController Instance = new TeamController();
        private TeamController() { }

        // 被邀请弹窗排队(对标老端 TeamBeInvitedView 列表弹窗;本轮走 headless TipsManager.Confirm 逐条顺序确认,
        // 同刻多条排队逐条弹——见 On24007/TryShowNextInviteConfirm)。
        private readonly Queue<TeamModel.BeInvitedVo> _pendingInvites = new Queue<TeamModel.BeInvitedVo>();
        private bool _inviteConfirmShowing;

        protected override void Register()
        {
            RegisterProtocal(Proto.TEAM_CREATE, On24000);
            RegisterProtocal(Proto.TEAM_APPLY_JOIN, On24002);
            RegisterProtocal(Proto.TEAM_APPLY_PUSH, On24003);
            RegisterProtocal(Proto.TEAM_APPLY_RESPONSE, On24004);
            RegisterProtocal(Proto.TEAM_QUIT, On24005);
            RegisterProtocal(Proto.TEAM_INVITE, On24006);
            RegisterProtocal(Proto.TEAM_INVITE_PUSH, On24007);
            RegisterProtocal(Proto.TEAM_INVITE_RESPONSE, On24008);
            RegisterProtocal(Proto.TEAM_KICK, On24009);
            RegisterProtocal(Proto.TEAM_INFO, On24010);
            RegisterProtocal(Proto.TEAM_HALL, On24012);
            RegisterProtocal(Proto.TEAM_ROLE_SCENE_TAG_PUSH, On24013);
            RegisterProtocal(Proto.TEAM_MEMBER_LEAVE_PUSH, On24014);
            RegisterProtocal(Proto.TEAM_LEADER_CHANGE_PUSH, On24015);
            RegisterProtocal(Proto.TEAM_CHANGE_TARGET, On24017);
            RegisterProtocal(Proto.TEAM_CHANGE_JOIN_TYPE, On24018);
            RegisterProtocal(Proto.TEAM_VOTE_START, On24020);
            RegisterProtocal(Proto.TEAM_VOTE, On24021);
            RegisterProtocal(Proto.TEAM_MATCH_JOIN, On24023);
            RegisterProtocal(Proto.TEAM_SELF_TIP_PUSH, On24030);
            RegisterProtocal(Proto.TEAM_HELP_TYPE, On24033);
            RegisterProtocal(Proto.TEAM_HELP_TYPE_PUSH, On24034);
            RegisterProtocal(Proto.TEAM_VOTE_OPEN_PUSH, On24035);
            RegisterProtocal(Proto.TEAM_VOTE_MEMBER_PUSH, On24036);
            RegisterProtocal(Proto.TEAM_VOTE_RESULT_PUSH, On24037);
            RegisterProtocal(Proto.TEAM_TIP_PUSH, On24038);
            RegisterProtocal(Proto.TEAM_VOTE_CANCEL_PUSH, On24040);
            RegisterProtocal(Proto.TEAM_APPLY_LIST, On24047);
            RegisterProtocal(Proto.TEAM_HELP_STATE, On24049);
            RegisterProtocal(Proto.TEAM_AUTO_MATCH, On24048);
            RegisterProtocal(Proto.TEAM_MEMBER_SCENE_PUSH, On24051);
            RegisterProtocal(Proto.TEAM_MEMBER_ONLINE_PUSH, On24052);
            RegisterProtocal(Proto.TEAM_NEARBY_PLAYERS, On24053);
            RegisterProtocal(Proto.TEAM_WORLD_SHOUT, On24055);
            RegisterProtocal(Proto.TEAM_INVITE_CROSS, On24057);
            RegisterProtocal(Proto.TEAM_RECRUIT_LIST, On24060);
            RegisterProtocal(Proto.TEAM_RECRUIT_MEMBER_LIST, On24061);
            RegisterProtocal(Proto.TEAM_APPLY_ALL, On24063);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
            TeamModel.Instance.Reset();
            _pendingInvites.Clear();
            _inviteConfirmShowing = false;
            base.Dispose();
        }

        // 对标老端 GAME_START 无条件拉一次 24010(TeamController.ts:558-562)。
        private void OnGameStart() => RequestTeamInfo();

        // 对标规格§0"断线清空"(老端 CHANGE_ROLE/CHANGE_ACCOUNT 清空同一语义)。
        private void OnDisconnected()
        {
            TeamModel.Instance.Reset();
            _pendingInvites.Clear();
            _inviteConfirmShowing = false;
        }

        // =====================================================================================
        // 发送侧 API
        // =====================================================================================

        /// <summary>24000 创建队伍。预校验 IsOpenTeam(主线101260前禁组队)。</summary>
        public void RequestCreateTeam(int activityId, int subtype, int sceneId, int minLv, int maxLv)
        {
            if (!TeamModel.Instance.IsOpenTeam(out string reason))
            {
                TipsManager.Toast(reason);
                GameLog.Info("Team", "RequestCreateTeam 拦截:{0}", reason);
                return;
            }
            SendFmt(Proto.TEAM_CREATE, "ccihhi", activityId, subtype, sceneId, minLv, maxLv, 0);
        }

        /// <summary>24002 申请入队(已知 team_id,大厅列表场景)。</summary>
        public void RequestJoinTeam(long teamId, int activityId, int subtype) =>
            SendFmt(Proto.TEAM_APPLY_JOIN, "lcc", teamId, activityId, subtype);

        /// <summary>24004 单条审批(0拒绝/1接受)。</summary>
        public void RespondApply(int agree, int serverId, long playerId) =>
            SendApplyResponse(new List<(int agree, int serverId, long playerId)> { (agree, serverId, playerId) });

        /// <summary>24004 一键清空(对标老端 TeamApplyView 一键清空传空数组:空数组=全部拒绝清空本地列表)。</summary>
        public void RespondApplyClearAll() => SendApplyResponse(new List<(int agree, int serverId, long playerId)>());

        private void SendApplyResponse(List<(int agree, int serverId, long playerId)> list)
        {
            (string fmt, object[] args) = BuildApplyResponsePayload(list);
            SendFmt(Proto.TEAM_APPLY_RESPONSE, fmt, args);
        }

        /// <summary>24004 手写编码:h(count) + 每项 c(res) h(server_id) l(player_id)(照老端 WriteFMT 实测)。
        /// 拆成纯函数供 CliVerify 反射断言编码正确性,不依赖真实网络连接。</summary>
        private static (string fmt, object[] args) BuildApplyResponsePayload(List<(int agree, int serverId, long playerId)> list)
        {
            var fmt = new StringBuilder("h");
            var args = new List<object> { list.Count };
            foreach ((int agree, int serverId, long playerId) item in list)
            {
                fmt.Append("chl");
                args.Add(item.agree);
                args.Add(item.serverId);
                args.Add(item.playerId);
            }
            return (fmt.ToString(), args.ToArray());
        }

        /// <summary>24005 离开队伍(队长发=解散,队员发=退队,服务端按角色区分,发送侧不用区分)。</summary>
        public void QuitTeam() => SendFmt(Proto.TEAM_QUIT);

        /// <summary>24006/24057 邀请入队分流(对标老端 TeamModel.InviteJoinTeam):serverId 与自己不同服 → 跨服。</summary>
        public void InviteJoinTeam(long roleId, int serverId = 0)
        {
            int selfServerId = RoleModel.Instance.ServerId;
            if (serverId != 0 && selfServerId != 0 && serverId != selfServerId)
            {
                SendCrossInvite(new List<(int serverId, long roleId)> { (serverId, roleId) });
            }
            else
            {
                SendSameServerInvite(new List<long> { roleId });
            }
        }

        private void SendSameServerInvite(List<long> roleIds)
        {
            (string fmt, object[] args) = BuildInvitePayload(roleIds);
            SendFmt(Proto.TEAM_INVITE, fmt, args);
        }

        /// <summary>24006 手写编码:c(activity_id) c(subtype) i(scene_id) h(min_lv) h(max_lv) h(count) + 每项 l(role_id)。</summary>
        private static (string fmt, object[] args) BuildInvitePayload(List<long> roleIds)
        {
            TeamModel m = TeamModel.Instance;
            int sceneId = RoleModel.Instance.SceneId;
            var fmt = new StringBuilder("ccihhh");
            var args = new List<object> { m.ActivityId, m.ActivitySubId, sceneId, m.MinLevel, m.MaxLevel, roleIds.Count };
            foreach (long id in roleIds) { fmt.Append('l'); args.Add(id); }
            return (fmt.ToString(), args.ToArray());
        }

        private void SendCrossInvite(List<(int serverId, long roleId)> list)
        {
            (string fmt, object[] args) = BuildCrossInvitePayload(list);
            SendFmt(Proto.TEAM_INVITE_CROSS, fmt, args);
        }

        /// <summary>24057 手写编码:h(count) + 每项 h(server_id) l(role_id)。</summary>
        private static (string fmt, object[] args) BuildCrossInvitePayload(List<(int serverId, long roleId)> list)
        {
            var fmt = new StringBuilder("h");
            var args = new List<object> { list.Count };
            foreach ((int serverId, long roleId) item in list)
            {
                fmt.Append("hl");
                args.Add(item.serverId);
                args.Add(item.roleId);
            }
            return (fmt.ToString(), args.ToArray());
        }

        /// <summary>24008 回应邀请(同意/拒绝);拒绝额外本地 DeleteBeInvited(对标老端)。</summary>
        public void RespondInvite(long teamId, bool agree)
        {
            (string fmt, object[] args) = BuildInviteResponsePayload(
                new List<(long teamId, int agree)> { (teamId, agree ? 1 : 0) });
            SendFmt(Proto.TEAM_INVITE_RESPONSE, fmt, args);
            if (!agree) TeamModel.Instance.DeleteBeInvited(teamId);
        }

        /// <summary>24008 手写编码(⚠️与24004顺序相反):h(count) + 每项 l(team_id) c(agree)。</summary>
        private static (string fmt, object[] args) BuildInviteResponsePayload(List<(long teamId, int agree)> list)
        {
            var fmt = new StringBuilder("h");
            var args = new List<object> { list.Count };
            foreach ((long teamId, int agree) item in list)
            {
                fmt.Append("lc");
                args.Add(item.teamId);
                args.Add(item.agree);
            }
            return (fmt.ToString(), args.ToArray());
        }

        /// <summary>24009 踢出队伍。</summary>
        public void KickMember(long kickId) => SendFmt(Proto.TEAM_KICK, "l", kickId);

        /// <summary>24010 主动拉一次队伍信息(空包=查询当前)。</summary>
        public void RequestTeamInfo() => SendFmt(Proto.TEAM_INFO);

        /// <summary>24012 查看组队大厅列表。</summary>
        public void RequestTeamHall(int activityId, int subtype, int sceneId = 0) =>
            SendFmt(Proto.TEAM_HALL, "cci", activityId, subtype, sceneId);

        /// <summary>24047 查询申请列表(仅队长有效数据,非队长服务端静默不回)。</summary>
        public void RequestApplyList() => SendFmt(Proto.TEAM_APPLY_LIST);

        /// <summary>24017 更改组队目标。</summary>
        public void ChangeTarget(int activityId, int subtype, int sceneId, int minLv, int maxLv, int joinConValue) =>
            SendFmt(Proto.TEAM_CHANGE_TARGET, "ccihhi", activityId, subtype, sceneId, minLv, maxLv, joinConValue);

        /// <summary>24018 更改申请自动进入类型(1不自动/2自动同意)。</summary>
        public void SetJoinType(int joinType) => SendFmt(Proto.TEAM_CHANGE_JOIN_TYPE, "c", joinType);

        /// <summary>24020 发起投票(仲裁);服务端 3000ms/1次 CD。</summary>
        public void StartVote(int activityId, int subtype) => SendFmt(Proto.TEAM_VOTE_START, "ic", activityId, subtype);

        /// <summary>24021 队员投票(0反对/1赞同)。</summary>
        public void Vote(int arbitrateId, int res) => SendFmt(Proto.TEAM_VOTE, "hc", arbitrateId, res);

        /// <summary>24023 匹配队伍(塞入已有同类队伍/匹配池的信令)。</summary>
        public void MatchJoin(int activityId, int subtype) => SendFmt(Proto.TEAM_MATCH_JOIN, "cc", activityId, subtype);

        /// <summary>24033 助战开关。</summary>
        public void SetHelpType(int dunId, int helpType) => SendFmt(Proto.TEAM_HELP_TYPE, "ic", dunId, helpType);

        /// <summary>24049 获取我的助战状态。</summary>
        public void RequestHelpState(int dunId) => SendFmt(Proto.TEAM_HELP_STATE, "i", dunId);

        /// <summary>24053 获取附近的玩家(邀请面板"附近"tab;老端按当前所在 field 场景+全部 field 场景轮询,
        /// 本端只暴露单次查询 API,轮询逻辑留给未来 TeamInviteView)。</summary>
        public void RequestNearbyPlayers(int sceneId) => SendFmt(Proto.TEAM_NEARBY_PLAYERS, "i", sceneId);

        /// <summary>24048 设置自动匹配状态(0取消/1开始)。</summary>
        public void SetMatchState(int state) => SendFmt(Proto.TEAM_AUTO_MATCH, "c", state);

        private void CancelMatch() => SetMatchState(0);

        /// <summary>24055 世界喊话:队长限定 + 本地5秒冷却预检(对标老端)。</summary>
        public void WorldShout()
        {
            if (!TeamModel.Instance.IsLeaderInTeam(RoleModel.Instance.RoleId))
            {
                GameLog.Info("Team", "WorldShout 拦截:非队长");
                return;
            }
            long now = TimeUtil.NowSec();
            if (now - TeamModel.Instance.WorldShoutTime < TeamModel.WORLD_SHOUT_COOL_TIME)
            {
                GameLog.Info("Team", "WorldShout 拦截:冷却中");
                return;
            }
            SendFmt(Proto.TEAM_WORLD_SHOUT);
        }

        /// <summary>24060 招募列表(副本专用,带次数)。</summary>
        public void RequestRecruitList(int type, int dunId) => SendFmt(Proto.TEAM_RECRUIT_LIST, "ci", type, dunId);

        /// <summary>24061 队员招募列表(通用邀请面板)。</summary>
        public void RequestRecruitMemberList(int type) => SendFmt(Proto.TEAM_RECRUIT_MEMBER_LIST, "c", type);

        /// <summary>24062 催促开启活动(fire-and-forget,老端无 recv)。</summary>
        public void Urge() => SendFmt(Proto.TEAM_URGE);

        /// <summary>24063 一键同意入队。</summary>
        public void RespondApplyAll() => SendFmt(Proto.TEAM_APPLY_ALL);

        // =====================================================================================
        // 接收侧
        // =====================================================================================

        // 24000: Res:i, ErrCodeArgs:s
        private void On24000(NetReader r)
        {
            int res = (int)r.ReadU32();
            r.ReadString();
            if (res == 1)
            {
                TipsManager.Toast("创建队伍成功");
                EventDispatcher.Emit(GlobalEvent.EVT_TEAM_BUILD_SUCCESS);
            }
            else
            {
                TipsManager.Toast("创建队伍失败(" + res + ")");
            }
            GameLog.Info("Team", "24000 创建队伍 res={0}", res);
        }

        // 24002: Res:i, ErrCodeArgs:s
        private void On24002(NetReader r)
        {
            int res = (int)r.ReadU32();
            r.ReadString();
            if (res == 1) TipsManager.Toast("发送申请成功");
            else TipsManager.Toast("申请失败(" + res + ")");
            GameLog.Info("Team", "24002 申请入队 res={0}", res);
        }

        // 24003(push): ServerId:h, PlayerId:l, Figure
        private void On24003(NetReader r)
        {
            int serverId = r.ReadU16();
            long playerId = (long)r.ReadU64();
            FigureProto.Read(r);
            if (!TeamModel.Instance.IsInShieldState(playerId))
            {
                TeamModel.Instance.SetApplyRedDot(true);
                TipsManager.Toast("有新的入队申请");
            }
            RequestApplyList(); // 补拉 24047(对标老端无条件补拉,红点/toast 仅屏蔽态门控)
            GameLog.Info("Team", "24003 收到入队申请 serverId={0} playerId={1}", serverId, playerId);
        }

        // 24004: Res:i(无 error_code_args,与24008不同)
        private void On24004(NetReader r)
        {
            int res = (int)r.ReadU32();
            if (res == 1)
            {
                TipsManager.Toast("回应成功");
                RequestApplyList();
            }
            else
            {
                TipsManager.Toast("回应失败(" + res + ")");
            }
            GameLog.Info("Team", "24004 回应入队申请 res={0}", res);
        }

        // 24005: Res:i —— 成功连锁:清空+重拉24010/24012+匹配中追加取消24048
        private void On24005(NetReader r)
        {
            int res = (int)r.ReadU32();
            if (res == 1)
            {
                TeamModel model = TeamModel.Instance;
                model.ClearTeamInfo();
                RequestTeamInfo();
                RequestTeamHall(model.ActivityId, model.ActivitySubId, 0);
                if (model.AutoMatch)
                {
                    model.CancelMatchText = "匹配取消";
                    CancelMatch();
                }
            }
            else
            {
                TipsManager.Toast("退出队伍失败(" + res + ")");
            }
            GameLog.Info("Team", "24005 退出/解散队伍 res={0}", res);
        }

        // 24006: Res:i
        private void On24006(NetReader r)
        {
            int res = (int)r.ReadU32();
            if (res != 1) TipsManager.Toast("邀请失败(" + res + ")");
            GameLog.Info("Team", "24006 邀请入队 res={0}", res);
        }

        // 24007(push): TeamId:l,Num:c,ActivityId:i,Subtype:c,SceneId:i,InviterId:l,Figure,InviteSceneId:i,InviteType:c
        private void On24007(NetReader r)
        {
            var vo = new TeamModel.BeInvitedVo
            {
                TeamId = (long)r.ReadU64(),
                Num = r.ReadU8(),
                ActivityId = (int)r.ReadU32(),
                Subtype = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                InviterId = (long)r.ReadU64(),
                Figure = FigureProto.Read(r),
                InviteSceneId = (int)r.ReadU32(),
                InviteType = r.ReadU8(),
            };
            TeamModel.Instance.UpdateBeInvitedList(vo);
            _pendingInvites.Enqueue(vo);
            TryShowNextInviteConfirm();
            GameLog.Info("Team", "24007 收到组队邀请 teamId={0} inviterId={1}", vo.TeamId, vo.InviterId);
        }

        /// <summary>本轮最小闭环(规格§UI-2):headless TipsManager.Confirm 逐条顺序确认队列,对标老端
        /// TeamBeInvitedView 列表弹窗(TODO:待转换为真正的列表 UI,可同屏多条各自独立同意/拒绝)。
        /// 同刻多条推送 → 逐条排队,前一条确认/拒绝后才弹下一条(TipsManager/ConfirmDialog 是单实例,
        /// 不排队,故本控制器自行排队,避免后一条 Confirm 覆盖前一条的回调)。</summary>
        private void TryShowNextInviteConfirm()
        {
            if (_inviteConfirmShowing) return;
            if (_pendingInvites.Count == 0) return;
            TeamModel.BeInvitedVo vo = _pendingInvites.Dequeue();
            _inviteConfirmShowing = true;
            string name = vo.Figure?.name ?? "";
            int level = vo.Figure?.level ?? 0;
            string text = name + "(Lv" + level + ")邀请你加入队伍";
            TipsManager.Confirm(text,
                onYes: () => OnInviteConfirmResolved(vo.TeamId, true),
                onNo: () => OnInviteConfirmResolved(vo.TeamId, false));
        }

        private void OnInviteConfirmResolved(long teamId, bool agree)
        {
            RespondInvite(teamId, agree);
            _inviteConfirmShowing = false;
            TryShowNextInviteConfirm();
        }

        // 24008: Res:i, ErrCodeArgs:s
        private void On24008(NetReader r)
        {
            int res = (int)r.ReadU32();
            r.ReadString();
            if (res != 1) TipsManager.Toast("回应邀请失败(" + res + ")");
            GameLog.Info("Team", "24008 回应邀请 res={0}", res);
        }

        // 24009: Res:i
        private void On24009(NetReader r)
        {
            int res = (int)r.ReadU32();
            if (res != 1) TipsManager.Toast("踢出失败(" + res + ")");
            GameLog.Info("Team", "24009 踢出队伍 res={0}", res);
        }

        // 24010: TeamId:l,ActivityId:c,Subtype:c,SceneId:i,PreNumFull:c,AutoMatching:c,MatchSt:i,MinLv:h,MaxLv:h,
        //   JoinConValue:i,AutoStart:c,JoinType:c,Members[h+item_to_bin_0]
        private void On24010(NetReader r)
        {
            var info = new TeamModel.TeamInfoVo
            {
                TeamId = (long)r.ReadU64(),
                ActivityId = r.ReadU8(),
                Subtype = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                PreNumFull = r.ReadU8(),
                AutoMatching = r.ReadU8(),
                MatchSt = r.ReadU32(),
                MinLv = r.ReadU16(),
                MaxLv = r.ReadU16(),
                JoinConValue = (int)r.ReadU32(),
                AutoStart = r.ReadU8(),
                JoinType = r.ReadU8(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++) info.Members.Add(ReadMemberVoWithJoinTime(r));
            TeamModel.Instance.UpdateTeamInfo(info);
            GameLog.Info("Team", "24010 队伍信息 teamId={0} members={1}", info.TeamId, count);
        }

        // item_to_bin_0(24010): Id:l,TeamPosition:c,Figure,HelpType:c,SceneId:i,JoinTime:i,Power:l,Online:c,
        //   ServerId:h,ServerNum:h,JoinValue:i
        private static TeamModel.MemberVo ReadMemberVoWithJoinTime(NetReader r)
        {
            return new TeamModel.MemberVo
            {
                Id = (long)r.ReadU64(),
                TeamPosition = r.ReadU8(),
                Figure = FigureProto.Read(r),
                HelpType = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                JoinTime = (int)r.ReadU32(),
                Power = (long)r.ReadU64(),
                Online = r.ReadU8(),
                ServerId = r.ReadU16(),
                ServerNum = r.ReadU16(),
                JoinValue = (int)r.ReadU32(),
            };
        }

        // 24012: ActivityId:c,Subtype:c,SceneId:i,Teams[h+item_to_bin_1]
        private void On24012(NetReader r)
        {
            int activityId = r.ReadU8();
            int subtype = r.ReadU8();
            r.ReadU32(); // SceneId:未落地(老端 UpdateTeamHall 只用 activity_id/subtype 触发事件),读掉保持游标对齐
            int count = r.ReadU16();
            var list = new List<TeamModel.HallEntryVo>(count);
            for (int i = 0; i < count; i++) list.Add(ReadHallEntry(r));
            TeamModel.Instance.UpdateTeamHall(activityId, subtype, list);
            GameLog.Info("Team", "24012 组队大厅 activityId={0} subtype={1} count={2}", activityId, subtype, count);
        }

        // item_to_bin_1(24012): TeamId:l,Num:c,JoinConValue:i,Members[h+item_to_bin_2]
        private static TeamModel.HallEntryVo ReadHallEntry(NetReader r)
        {
            var e = new TeamModel.HallEntryVo
            {
                TeamId = (long)r.ReadU64(),
                Num = r.ReadU8(),
                JoinConValue = (int)r.ReadU32(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++) e.Members.Add(ReadMemberVoHall(r));
            return e;
        }

        // item_to_bin_2(24012 内成员,字段序与 item_to_bin_0 不同): Id:l,TeamPosition:c,Figure,HelpType:c,
        //   SceneId:i,Online:c,ServerId:h,ServerNum:h,JoinValue:i,Power:l(**无 JoinTime,Power 挪到末尾**)
        private static TeamModel.MemberVo ReadMemberVoHall(NetReader r)
        {
            return new TeamModel.MemberVo
            {
                Id = (long)r.ReadU64(),
                TeamPosition = r.ReadU8(),
                Figure = FigureProto.Read(r),
                HelpType = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                Online = r.ReadU8(),
                ServerId = r.ReadU16(),
                ServerNum = r.ReadU16(),
                JoinValue = (int)r.ReadU32(),
                Power = (long)r.ReadU64(),
            };
        }

        // 24013(push): Id:l,TeamId:l,Position:c —— 落地到场景 RoleVo(渲染层未接,数据先备好,见 r8_unity §4)
        private void On24013(NetReader r)
        {
            long id = (long)r.ReadU64();
            long teamId = (long)r.ReadU64();
            int position = r.ReadU8();
            RoleVo vo = SceneManager.Instance.GetRole(id);
            if (vo != null)
            {
                vo.TeamId = teamId;
                vo.TeamPos = position;
            }
            GameLog.Info("Team", "24013 场景组队标记 id={0} teamId={1} pos={2}", id, teamId, position);
        }

        // 24014(push): Id:l
        private void On24014(NetReader r)
        {
            long id = (long)r.ReadU64();
            if (id == RoleModel.Instance.RoleId)
            {
                // TODO: 老端此时额外 Fire(TEAM_CLOSE_ALL_VIEW) 关闭所有队伍相关弹窗;本轮相关 UI 未移植,跳过。
                TeamModel.Instance.ClearTeamInfo();
            }
            else
            {
                TeamModel.Instance.DeleteOneTeammate(id);
            }
            GameLog.Info("Team", "24014 队员离开 id={0}", id);
        }

        // 24015(push): Id:l
        private void On24015(NetReader r)
        {
            long id = (long)r.ReadU64();
            if (id != 0) TeamModel.Instance.UpdateTeamLeader(id);
            GameLog.Info("Team", "24015 队长变更 id={0}", id);
        }

        // 24017: Res:i,ActivityId:c,Subtype:c,SceneId:i,MinLv:h,MaxLv:h,JoinConValue:i —— 成功后自动重拉24012
        private void On24017(NetReader r)
        {
            int res = (int)r.ReadU32();
            var vo = new TeamModel.ChangeTargetVo
            {
                ActivityId = r.ReadU8(),
                Subtype = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                MinLv = r.ReadU16(),
                MaxLv = r.ReadU16(),
                JoinConValue = (int)r.ReadU32(),
            };
            if (res == 1)
            {
                TeamModel.Instance.ChangeTargetSuccess(vo);
                TipsManager.Toast("更改目标成功");
                RequestTeamHall(vo.ActivityId, vo.Subtype, vo.SceneId);
            }
            else
            {
                TipsManager.Toast("更改目标失败(" + res + ")");
            }
            GameLog.Info("Team", "24017 更改组队目标 res={0}", res);
        }

        // 24018: Res:i,JoinType:c
        private void On24018(NetReader r)
        {
            int res = (int)r.ReadU32();
            int joinType = r.ReadU8();
            if (res == 1) TeamModel.Instance.UpdateJoinType(joinType);
            else TipsManager.Toast("设置失败(" + res + ")");
            GameLog.Info("Team", "24018 更改申请自动进入类型 res={0} joinType={1}", res, joinType);
        }

        // 24020: ErrorCode:i,ErrCodeArgs:s,ActivityId:i(32位),Subtype:c,SceneId:i,ArbitrateId:h
        private void On24020(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            r.ReadString();
            r.ReadU32(); // ActivityId
            r.ReadU8();  // Subtype
            r.ReadU32(); // SceneId
            int arbitrateId = r.ReadU16();
            if (errorCode != 1) TipsManager.Toast("发起投票失败(" + errorCode + ")");
            GameLog.Info("Team", "24020 发起投票 errorCode={0} arbitrateId={1}", errorCode, arbitrateId);
        }

        // 24021: ErrorCode:i,ErrCodeArgs:s,Res:c
        private void On24021(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            r.ReadString();
            int res = r.ReadU8();
            if (errorCode != 1) TipsManager.Toast("投票失败(" + errorCode + ")");
            GameLog.Info("Team", "24021 队员投票 errorCode={0} res={1}", errorCode, res);
        }

        // 24023: Res:i,ActivityId:c,Subtype:c —— 成功无本地状态变更(对标老端注释掉的分支)
        private void On24023(NetReader r)
        {
            int res = (int)r.ReadU32();
            r.ReadU8();
            r.ReadU8();
            if (res != 1) TipsManager.Toast("匹配加入失败(" + res + ")");
            GameLog.Info("Team", "24023 匹配队伍 res={0}", res);
        }

        // 24030(push): Res:i —— 自身提示;res==2400022 老端会弹 TeamView(本轮未移植,跳过);
        // 降级:不逐码接错误码表,仅失败态(!=1)才 toast(对标项目通用"错误码表未移植"降级口径,
        // 不逐字节复刻老端"任意 code 都无条件 ErrorCodeShow"的行为——见汇报偏差项)。
        private void On24030(NetReader r)
        {
            int res = (int)r.ReadU32();
            if (res != 1) TipsManager.Toast("组队提示(" + res + ")");
            GameLog.Info("Team", "24030 自身提示 res={0}", res);
        }

        // 24033: ErrorCode:i,DunId:i,HelpType:c
        private void On24033(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int dunId = (int)r.ReadU32();
            int helpType = r.ReadU8();
            if (errorCode == 1) TeamModel.Instance.SetMyHelpState(dunId, helpType);
            else TipsManager.Toast("助战设置失败(" + errorCode + ")");
            GameLog.Info("Team", "24033 助战开关 errorCode={0} dunId={1} helpType={2}", errorCode, dunId, helpType);
        }

        // 24034(push): Members[h+{RoleId:l,HelpType:c}]
        private void On24034(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<(long roleId, int helpType)>(count);
            for (int i = 0; i < count; i++) list.Add(((long)r.ReadU64(), r.ReadU8()));
            TeamModel.Instance.ApplyMemberHelpBroadcast(list);
            GameLog.Info("Team", "24034 助战状态广播 count={0}", count);
        }

        // 24035(push): ActivityId:i,Subtype:c,SceneId:i,ArbitrateId:h,EndTime:i
        private void On24035(NetReader r)
        {
            var vo = new TeamModel.VoteOpenVo
            {
                ActivityId = (int)r.ReadU32(),
                Subtype = r.ReadU8(),
                SceneId = (int)r.ReadU32(),
                ArbitrateId = r.ReadU16(),
                EndTime = r.ReadU32(),
            };
            TeamModel.Instance.SetVoteOpen(vo);
            // TODO: 老端此时关闭 TeamMatchView/TeamView 并打开 TeamVoteView,两窗口本轮均未移植,仅存数据+发事件。
            GameLog.Info("Team", "24035 发起投票广播 arbitrateId={0}", vo.ArbitrateId);
        }

        // 24036(push): RoleId:l,ArbitrateId:h,Res:c
        private void On24036(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            int arbitrateId = r.ReadU16();
            int res = r.ReadU8();
            TeamModel.Instance.SetVoteData(roleId, res);
            GameLog.Info("Team", "24036 队员投票广播 roleId={0} arbitrateId={1} res={2}", roleId, arbitrateId, res);
        }

        // 24037(push): ErrorCode:i,ErrCodeArgs:s
        private void On24037(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            r.ReadString();
            TeamModel.Instance.ClearVoteData();
            if (errorCode != 1) TipsManager.Toast("投票结果(" + errorCode + ")");
            GameLog.Info("Team", "24037 投票结果 errorCode={0}", errorCode);
        }

        // 24038(push): ErrorCode:i,ErrCodeArgs:s
        private void On24038(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            r.ReadString();
            if (errorCode != 1) TipsManager.Toast("组队提示(" + errorCode + ")");
            GameLog.Info("Team", "24038 通用提示 errorCode={0}", errorCode);
        }

        // 24040(push): 无字段
        private void On24040(NetReader r)
        {
            TeamModel.Instance.ClearVoteData();
            GameLog.Info("Team", "24040 取消仲裁");
        }

        // 24047: Applicants[h+item_to_bin_6]
        private void On24047(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<TeamModel.ApplyVo>(count);
            for (int i = 0; i < count; i++) list.Add(ReadApplyVo(r));
            TeamModel.Instance.SetApplyList(list);
            GameLog.Info("Team", "24047 申请列表 count={0}", count);
        }

        // item_to_bin_6(24047): ServerId:h,PlayerId:l,Figure,CombatPower:l,ServerNum:h
        private static TeamModel.ApplyVo ReadApplyVo(NetReader r)
        {
            return new TeamModel.ApplyVo
            {
                ServerId = r.ReadU16(),
                PlayerId = (long)r.ReadU64(),
                Figure = FigureProto.Read(r),
                CombatPower = (long)r.ReadU64(),
                ServerNum = r.ReadU16(),
            };
        }

        // 24048: Res:i,ErrCodeArgs:s,State:c,MatchSt:i,ActivityId:c,Subtype:c,RoleId:l —— 状态机最复杂的一号
        private void On24048(NetReader r)
        {
            int res = (int)r.ReadU32();
            r.ReadString();
            int state = r.ReadU8();
            long matchSt = r.ReadU32();
            r.ReadU8(); // ActivityId
            r.ReadU8(); // Subtype
            long roleId = (long)r.ReadU64();

            if (res != 1)
            {
                TipsManager.Toast("匹配设置失败(" + res + ")");
                GameLog.Info("Team", "24048 自动匹配 res={0}", res);
                return;
            }

            TeamModel model = TeamModel.Instance;
            if (state == 0)
            {
                TeamModel.MemberVo member = (roleId > 0 && roleId != RoleModel.Instance.RoleId) ? model.FindMember(roleId) : null;
                string text;
                if (member != null)
                {
                    string posStr = member.TeamPosition == TeamModel.TEAM_LEADER ? "队长" : "队员";
                    text = posStr + (member.Figure?.name ?? "") + "取消了匹配";
                }
                else
                {
                    text = model.CancelMatchText ?? "取消匹配";
                }
                TipsManager.Toast(text);
                model.CancelMatchText = null;
                model.SetMatchTimestamp(0);
            }
            else if (state == 1)
            {
                model.SetMatchTimestamp(matchSt);
                TipsManager.Toast(model.HasTeam ? "开始队伍匹配" : "开始个人匹配");
                // TODO: TeamMatchView(匹配中倒计时浮层)未移植,仅状态更新,不弹浮层。
            }
            // state==2(匹配成功)老端无专门分支,落入下方 SetAutoMatch(state==1)==false,按非1统一处理(对标保留)。
            model.SetAutoMatch(state == 1);
            GameLog.Info("Team", "24048 自动匹配 res={0} state={1} matchSt={2}", res, state, matchSt);
        }

        // 24049: DunId:i,State:c
        private void On24049(NetReader r)
        {
            int dunId = (int)r.ReadU32();
            int state = r.ReadU8();
            TeamModel.Instance.SetMyHelpState(dunId, state);
            GameLog.Info("Team", "24049 我的助战状态 dunId={0} state={1}", dunId, state);
        }

        // 24051(push): RoleId:l,SceneId:i
        private void On24051(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            int sceneId = (int)r.ReadU32();
            TeamModel.Instance.UpdateMemberScene(roleId, sceneId);
            GameLog.Info("Team", "24051 队员场景变化 roleId={0} sceneId={1}", roleId, sceneId);
        }

        // 24052(push): RoleId:l,Online:c
        private void On24052(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            int online = r.ReadU8();
            TeamModel.Instance.UpdateMemberOnline(roleId, online);
            GameLog.Info("Team", "24052 队员在线状态 roleId={0} online={1}", roleId, online);
        }

        // 24053: SceneId:i,Users[h+item_to_bin_7]
        private void On24053(NetReader r)
        {
            int sceneId = (int)r.ReadU32();
            int count = r.ReadU16();
            var list = new List<TeamModel.NearbyVo>(count);
            for (int i = 0; i < count; i++) list.Add(ReadNearbyVo(r));
            TeamModel.Instance.SetNearbyPlayers(list);
            GameLog.Info("Team", "24053 附近玩家 sceneId={0} count={1}", sceneId, count);
        }

        // item_to_bin_7(24053): RoleId:l,Platform:s,ServNum:h,ServId:h,Figure
        private static TeamModel.NearbyVo ReadNearbyVo(NetReader r)
        {
            return new TeamModel.NearbyVo
            {
                RoleId = (long)r.ReadU64(),
                Platform = r.ReadString(),
                ServNum = r.ReadU16(),
                ServId = r.ReadU16(),
                Figure = FigureProto.Read(r),
            };
        }

        // 24055: 无字段(空包=成功信号)
        private void On24055(NetReader r)
        {
            TipsManager.Toast("招募发送成功");
            TeamModel.Instance.WorldShoutTime = TimeUtil.NowSec();
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_WORLD_SHOUT_SUCCESS);
            GameLog.Info("Team", "24055 世界喊话成功");
        }

        // 24057: Res:i —— 孤儿编码,防御性注册(r8_server 实证服务端从未真实发送,真实 ack 走24006)
        private void On24057(NetReader r)
        {
            int res = (int)r.ReadU32();
            if (res != 1) TipsManager.Toast("邀请失败(" + res + ")");
            GameLog.Info("Team", "24057 跨服邀请回执(孤儿编码) res={0}", res);
        }

        // 24060: Type:c,DunId:i,List[h+item_to_bin_9]
        private void On24060(NetReader r)
        {
            int type = r.ReadU8();
            int dunId = (int)r.ReadU32();
            int count = r.ReadU16();
            var list = new List<TeamModel.RecruitVo>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new TeamModel.RecruitVo
                {
                    RoleId = (long)r.ReadU64(),
                    Figure = FigureProto.Read(r),
                    Count = r.ReadU8(),
                    MaxCount = r.ReadU8(),
                    CombatPower = (long)r.ReadU64(),
                });
            }
            TeamModel.Instance.SetRecruitList(type, list);
            GameLog.Info("Team", "24060 招募列表 type={0} dunId={1} count={2}", type, dunId, count);
        }

        // 24061: Type:c,List[h+item_to_bin_10]
        private void On24061(NetReader r)
        {
            int type = r.ReadU8();
            int count = r.ReadU16();
            var list = new List<TeamModel.RecruitMemberVo>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new TeamModel.RecruitMemberVo
                {
                    RoleId = (long)r.ReadU64(),
                    Figure = FigureProto.Read(r),
                    CombatPower = (long)r.ReadU64(),
                });
            }
            TeamModel.Instance.SetRecruitMemberList(type, list);
            GameLog.Info("Team", "24061 队员招募列表 type={0} count={1}", type, count);
        }

        // 24063: ErrorCode:i —— 无论成败都补拉24047
        private void On24063(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            RequestApplyList();
            if (errorCode != 1) TipsManager.Toast("一键同意失败(" + errorCode + ")");
            GameLog.Info("Team", "24063 一键同意入队 errorCode={0}", errorCode);
        }
    }
}
