using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.Team
{
    /// <summary>
    /// 组队数据层(对标老客户端 commonModel/TeamModel.ts,自动循环 轮8)。
    /// 队伍信息(24010 全量快照)/大厅(24012)/申请(24047/24003 推送 + 本地10分钟屏蔽表)/被邀请(24007)/
    /// 投票(24020/21/35/36/37/40)/招募(24060/24061)/附近玩家(24053)/助战(24033/24049 自身 + 24034 队友广播)/
    /// 自动匹配(24048)/世界喊话(24055)/主线门槛(IsOpenTeam)。
    /// 事件走 <see cref="GlobalEvent"/>(EVT_TEAM_*),仅存数据,不发协议(协议侧见 TeamController)。
    /// </summary>
    public sealed class TeamModel
    {
        public static readonly TeamModel Instance = new TeamModel();
        private TeamModel() { }

        /// <summary>队伍最大人数(对标老端 TeamModel.TEAMER_MAX;HUD 队伍区据此决定是否补一个邀请占位格)。</summary>
        public const int TEAMER_MAX = 3;
        /// <summary>世界喊话本地冷却秒数(对标老端 WORLD_SHOUT_COOL_TIME)。</summary>
        public const int WORLD_SHOUT_COOL_TIME = 5;
        /// <summary>队长位(对标老端 team_position==1)。</summary>
        public const int TEAM_LEADER = 1;
        /// <summary>主线门槛任务号(101260,达到/超过才解锁组队)。</summary>
        public const int OPEN_TEAM_TASK_ID = 101260;

        private const long ShieldWindowSec = 600; // 本地屏蔽 10 分钟(对标老端 shield_list_ 600秒窗口)

        // ===================================================================================
        // 队伍成员(24010 item_to_bin_0 / 24012 item_to_bin_2 字段并集;两处字段序不同,读取由
        // TeamController 分别按各自序解析后落到同一个类)
        // ===================================================================================

        public sealed class MemberVo
        {
            public long Id;
            /// <summary>1队长 2队员 3假人。</summary>
            public int TeamPosition;
            public FigureProto Figure;
            public int HelpType;
            public int SceneId;
            /// <summary>仅 24010 携带;24012(大厅列表内成员)恒为 0。</summary>
            public int JoinTime;
            public long Power;
            public int Online;
            public int ServerId;
            public int ServerNum;
            public int JoinValue;
        }

        // ===================================================================================
        // 队伍信息(24010 全量快照;24005/14 清空;24014/15/34/51/52 局部改)
        // ===================================================================================

        public sealed class TeamInfoVo
        {
            public long TeamId;
            public int ActivityId;
            public int Subtype;
            public int SceneId;
            public int PreNumFull;
            public int AutoMatching;
            public long MatchSt;
            public int MinLv;
            public int MaxLv;
            public int JoinConValue;
            public int AutoStart;
            public int JoinType;
            public List<MemberVo> Members = new List<MemberVo>();
        }

        public TeamInfoVo Info { get; private set; }

        /// <summary>对标老端 MainUITaskTeamView.UpdateTeamData 的 have_team = team_info && team_info.team_id != null。</summary>
        public bool HasTeam => Info != null && Info.TeamId != 0;

        // 未建队时的目标预选(对标老端 TeamModel 顶层 activity_id/activity_sub_id/min_level/max_level/join_con_value;
        // 建队后随 24010/24017 同步)。
        public int ActivityId { get; private set; } = 1;
        public int ActivitySubId { get; private set; }
        public int ActivitySceneId { get; private set; }
        public int MinLevel { get; private set; } = 1;
        public int MaxLevel { get; private set; } = 999;
        public int JoinConValue { get; private set; }

        /// <summary>24010 落地(对标老端 UpdateTeamInfo):按 team_position 升序排序,顶层目标字段同步。</summary>
        public void UpdateTeamInfo(TeamInfoVo info)
        {
            Info = info;
            if (info?.Members != null)
            {
                info.Members.Sort((a, b) => a.TeamPosition.CompareTo(b.TeamPosition));
                ActivityId = info.ActivityId;
                ActivitySubId = info.Subtype;
                MinLevel = info.MinLv;
                MaxLevel = info.MaxLv;
                JoinConValue = info.JoinConValue;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        /// <summary>对标老端 ClearTeamInfo。</summary>
        public void ClearTeamInfo()
        {
            Info = null;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        public MemberVo FindMember(long id)
        {
            if (Info?.Members == null) return null;
            foreach (MemberVo m in Info.Members)
                if (m.Id == id) return m;
            return null;
        }

        /// <summary>24014(非自己分支):对标老端 DeleteOneTeammate。</summary>
        public void DeleteOneTeammate(long id)
        {
            if (Info?.Members == null) return;
            for (int i = Info.Members.Count - 1; i >= 0; i--)
            {
                if (Info.Members[i].Id == id) { Info.Members.RemoveAt(i); break; }
            }
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        /// <summary>24015 落地:对标老端 UpdateTeamLeader——新队长置1,**其余全部置0**(老端原样如此,
        /// 会抹掉"假人3"与"队员2"的区分,这是老端自身的一处粗糙逻辑,不纠正,原样保留)。</summary>
        public void UpdateTeamLeader(long newLeaderId)
        {
            if (Info?.Members == null) return;
            foreach (MemberVo m in Info.Members) m.TeamPosition = m.Id == newLeaderId ? TEAM_LEADER : 0;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        /// <summary>24051 落地:对标老端 MyTeammateSceneChange。</summary>
        public void UpdateMemberScene(long id, int sceneId)
        {
            MemberVo m = FindMember(id);
            if (m == null) return;
            m.SceneId = sceneId;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        /// <summary>24052 落地:对标老端 MyTeammateOnlineChange。</summary>
        public void UpdateMemberOnline(long id, int online)
        {
            MemberVo m = FindMember(id);
            if (m == null) return;
            m.Online = online;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        /// <summary>24034 落地:对标老端 UpdateHelpState(队友助战广播,区别于自身 help_data,见下方
        /// <see cref="SetMyHelpState"/>)。无队伍时静默忽略(对标老端 if(team_info)门槛)。</summary>
        public void ApplyMemberHelpBroadcast(List<(long roleId, int helpType)> list)
        {
            if (Info?.Members == null || list == null) return;
            foreach ((long roleId, int helpType) entry in list)
            {
                MemberVo m = FindMember(entry.roleId);
                if (m != null) m.HelpType = entry.helpType;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }

        /// <summary>对标老端 IsLeaderInTeam:roleId 是否为当前队伍队长。</summary>
        public bool IsLeaderInTeam(long roleId)
        {
            if (Info?.Members == null) return false;
            foreach (MemberVo m in Info.Members)
                if (m.Id == roleId && m.TeamPosition == TEAM_LEADER) return true;
            return false;
        }

        // ===================================================================================
        // 组队大厅(24012)
        // ===================================================================================

        public sealed class HallEntryVo
        {
            public long TeamId;
            public int Num;
            public int JoinConValue;
            public List<MemberVo> Members = new List<MemberVo>();
        }

        public int HallActivityId { get; private set; }
        public int HallSubtype { get; private set; }
        private readonly List<HallEntryVo> _hall = new List<HallEntryVo>();
        public IReadOnlyList<HallEntryVo> Hall => _hall;

        /// <summary>对标老端 UpdateTeamHall:按人数降序排序。</summary>
        public void UpdateTeamHall(int activityId, int subtype, List<HallEntryVo> list)
        {
            HallActivityId = activityId;
            HallSubtype = subtype;
            _hall.Clear();
            if (list != null) _hall.AddRange(list);
            _hall.Sort((a, b) => b.Num.CompareTo(a.Num));
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_HALL_UPDATE, activityId, subtype);
        }

        // ===================================================================================
        // 申请列表(24047 全量/24003 推送触发红点/24004·24063 审批;本地10分钟屏蔽表)
        // ===================================================================================

        public sealed class ApplyVo
        {
            public int ServerId;
            public long PlayerId;
            public FigureProto Figure;
            public long CombatPower;
            public int ServerNum;
        }

        private readonly List<ApplyVo> _applyList = new List<ApplyVo>();
        public IReadOnlyList<ApplyVo> ApplyList => _applyList;
        public bool HaveNewApply { get; private set; }

        private readonly Dictionary<long, long> _shieldList = new Dictionary<long, long>();

        /// <summary>对标老端 IsInShiledState:10分钟内屏蔽过的 player_id 视为屏蔽中。</summary>
        public bool IsInShieldState(long playerId) =>
            _shieldList.TryGetValue(playerId, out long t) && TimeUtil.NowSec() - t < ShieldWindowSec;

        /// <summary>对标老端 UpdateShieldList(TeamApplyRoleItem"屏蔽"按钮用,大厅 UI 后补,先备好数据层)。</summary>
        public void ShieldApplicant(long playerId) => _shieldList[playerId] = TimeUtil.NowSec();

        /// <summary>24047 全量落地(对标老端 SetApplyList):过滤屏蔽中的申请人。</summary>
        public void SetApplyList(List<ApplyVo> list)
        {
            _applyList.Clear();
            if (list != null)
            {
                foreach (ApplyVo vo in list)
                    if (!IsInShieldState(vo.PlayerId)) _applyList.Add(vo);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_APPLY_LIST_UPDATE);
        }

        /// <summary>对标老端 SetTeamApplyRedDot(RedDotManager.TEAM_APPLY)。</summary>
        public void SetApplyRedDot(bool value)
        {
            if (HaveNewApply == value) return;
            HaveNewApply = value;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_APPLY_REDDOT_UPDATE);
        }

        // ===================================================================================
        // 被邀请列表(24007 推送去重覆盖/24008 拒绝时移除)
        // ===================================================================================

        public sealed class BeInvitedVo
        {
            public long TeamId;
            public int Num;
            public int ActivityId;
            public int Subtype;
            public int SceneId;
            public long InviterId;
            public FigureProto Figure;
            public int InviteSceneId;
            public int InviteType;
        }

        private readonly List<BeInvitedVo> _beInvited = new List<BeInvitedVo>();
        public IReadOnlyList<BeInvitedVo> BeInvitedList => _beInvited;

        /// <summary>对标老端 UpdateBeInvitedList:同一 team_id 或 inviter_id 覆盖去重后插入。</summary>
        public void UpdateBeInvitedList(BeInvitedVo vo)
        {
            if (vo == null) return;
            for (int i = _beInvited.Count - 1; i >= 0; i--)
            {
                if (_beInvited[i].TeamId == vo.TeamId || _beInvited[i].InviterId == vo.InviterId)
                    _beInvited.RemoveAt(i);
            }
            _beInvited.Add(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_BE_INVITED_UPDATE);
        }

        /// <summary>对标老端 DeleteBeInvited(拒绝邀请后本地移除,按 team_id 匹配首条)。</summary>
        public void DeleteBeInvited(long teamId)
        {
            for (int i = _beInvited.Count - 1; i >= 0; i--)
            {
                if (_beInvited[i].TeamId == teamId) { _beInvited.RemoveAt(i); break; }
            }
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_BE_INVITED_UPDATE);
        }

        // ===================================================================================
        // 更改组队目标(24017)
        // ===================================================================================

        public sealed class ChangeTargetVo
        {
            public int ActivityId;
            public int Subtype;
            public int SceneId;
            public int MinLv;
            public int MaxLv;
            public int JoinConValue;
        }

        /// <summary>对标老端 ChangeTargetSuccess(数据部分;提示与自动重拉 24012 由 TeamController 负责)。</summary>
        public void ChangeTargetSuccess(ChangeTargetVo vo)
        {
            if (vo == null) return;
            ActivityId = vo.ActivityId;
            ActivitySubId = vo.Subtype;
            ActivitySceneId = vo.SceneId;
            MinLevel = vo.MinLv;
            MaxLevel = vo.MaxLv;
            JoinConValue = vo.JoinConValue;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_CHANGE_TARGET_SUCCESS);
        }

        /// <summary>24018 落地:仅在已有队伍时更新(对标老端 if(team_info)门槛)。</summary>
        public void UpdateJoinType(int joinType)
        {
            if (Info != null) Info.JoinType = joinType;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_JOIN_TYPE_UPDATE);
        }

        // ===================================================================================
        // 投票/仲裁(24035 开始/24036 队员投票/24037 结果/24040 取消)
        // ===================================================================================

        public sealed class VoteOpenVo
        {
            public int ActivityId;
            public int Subtype;
            public int SceneId;
            public int ArbitrateId;
            public long EndTime;
        }

        public VoteOpenVo CurrentVote { get; private set; }
        private readonly Dictionary<long, int> _voteData = new Dictionary<long, int>();
        public IReadOnlyDictionary<long, int> VoteData => _voteData;

        public void SetVoteOpen(VoteOpenVo vo)
        {
            CurrentVote = vo;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_VOTE_UPDATE);
        }

        public void SetVoteData(long roleId, int res)
        {
            _voteData[roleId] = res;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_VOTE_UPDATE);
        }

        public void ClearVoteData()
        {
            _voteData.Clear();
            CurrentVote = null;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_VOTE_UPDATE);
        }

        // ===================================================================================
        // 招募(24060 副本专用带次数 / 24061 通用邀请面板,老端共用同一份 zhao_mu_list[type],本端因
        // 两者字段形状不同拆成两个独立字典存,行为等价,消费方待未来邀请/招募 UI 接线)
        // ===================================================================================

        public sealed class RecruitVo
        {
            public long RoleId;
            public FigureProto Figure;
            public int Count;
            public int MaxCount;
            public long CombatPower;
        }

        public sealed class RecruitMemberVo
        {
            public long RoleId;
            public FigureProto Figure;
            public long CombatPower;
        }

        private static readonly List<RecruitVo> EmptyRecruit = new List<RecruitVo>();
        private static readonly List<RecruitMemberVo> EmptyRecruitMember = new List<RecruitMemberVo>();
        private readonly Dictionary<int, List<RecruitVo>> _recruitList = new Dictionary<int, List<RecruitVo>>();
        private readonly Dictionary<int, List<RecruitMemberVo>> _recruitMemberList = new Dictionary<int, List<RecruitMemberVo>>();

        public IReadOnlyList<RecruitVo> GetRecruitList(int type) =>
            _recruitList.TryGetValue(type, out List<RecruitVo> l) ? l : EmptyRecruit;

        public IReadOnlyList<RecruitMemberVo> GetRecruitMemberList(int type) =>
            _recruitMemberList.TryGetValue(type, out List<RecruitMemberVo> l) ? l : EmptyRecruitMember;

        public void SetRecruitList(int type, List<RecruitVo> list)
        {
            _recruitList[type] = list ?? new List<RecruitVo>();
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_ZHAO_MU_UPDATE, type);
        }

        public void SetRecruitMemberList(int type, List<RecruitMemberVo> list)
        {
            _recruitMemberList[type] = list ?? new List<RecruitMemberVo>();
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_ZHAO_MU_UPDATE, type);
        }

        // ===================================================================================
        // 附近玩家(24053)
        // ===================================================================================

        public sealed class NearbyVo
        {
            public long RoleId;
            public string Platform = "";
            public int ServNum;
            public int ServId;
            public FigureProto Figure;
        }

        private readonly List<NearbyVo> _nearby = new List<NearbyVo>();
        public IReadOnlyList<NearbyVo> NearbyPlayers => _nearby;

        /// <summary>对标老端 SetNearByPlayer:整体替换,非增量。</summary>
        public void SetNearbyPlayers(List<NearbyVo> list)
        {
            _nearby.Clear();
            if (list != null) _nearby.AddRange(list);
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_NEARBY_PLAYER_UPDATE);
        }

        // ===================================================================================
        // 助战(自身状态:24033/24049 → SetMyHelpState;队友广播见上方 ApplyMemberHelpBroadcast)
        // ===================================================================================

        private readonly Dictionary<int, int> _myHelpState = new Dictionary<int, int>();
        public int GetMyHelpState(int dunId) => _myHelpState.TryGetValue(dunId, out int v) ? v : 0;

        public void SetMyHelpState(int dunId, int state)
        {
            _myHelpState[dunId] = state;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_HELP_STATE_UPDATE, dunId);
        }

        // ===================================================================================
        // 自动匹配(24048,匹配中体验的唯一状态源)
        // ===================================================================================

        public bool AutoMatch { get; private set; }
        /// <summary>无队伍时的个人匹配时间戳基准(对标老端顶层 match_st;有队伍时用 Info.MatchSt)。</summary>
        public long MatchSt { get; private set; }
        /// <summary>取消匹配兜底文案(对标老端 cancel_match_text,由退队等主动取消场景预置)。</summary>
        public string CancelMatchText { get; set; }

        public void SetMatchTimestamp(long matchSt)
        {
            if (Info != null) Info.MatchSt = matchSt;
            else MatchSt = matchSt;
        }

        /// <summary>对标老端 SetAutoMatch(仅状态存储;倒计时/TeamMatchView 未移植,TODO)。</summary>
        public void SetAutoMatch(bool value)
        {
            if (AutoMatch == value) return;
            AutoMatch = value;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_MATCH_STATE_UPDATE, value);
        }

        // ===================================================================================
        // 世界喊话(24055)
        // ===================================================================================

        public long WorldShoutTime { get; set; }

        // ===================================================================================
        // 主线门槛(对标老端 IsOpenTeam:主线 101260 前禁组队)
        // ===================================================================================

        /// <summary>是否已解锁组队功能。task==null(主线已全部完成/尚未就绪)按老端语义视为已解锁放行;
        /// 否则按当前未完成主线任务 id 与 101260 比较——**此判定不依赖配置表加载**,恒可精确判断
        /// (仅 blockReason 的任务名在配置未就绪时退化为通用"主线")。</summary>
        public bool IsOpenTeam(out string blockReason)
        {
            TaskVo task = TaskModel.Instance.MainLineTaskVo;
            if (task == null)
            {
                blockReason = null;
                return true;
            }
            if (task.TaskId <= OPEN_TEAM_TASK_ID)
            {
                TaskConfigs.TaskCfg cfg = TaskConfigs.Get(OPEN_TEAM_TASK_ID);
                blockReason = "完成" + (cfg?.Name ?? "主线") + "任务开启";
                return false;
            }
            blockReason = null;
            return true;
        }

        /// <summary>断线/登出清空(对标老端各字段复位)。</summary>
        public void Reset()
        {
            Info = null;
            ActivityId = 1;
            ActivitySubId = 0;
            ActivitySceneId = 0;
            MinLevel = 1;
            MaxLevel = 999;
            JoinConValue = 0;
            _hall.Clear();
            HallActivityId = 0;
            HallSubtype = 0;
            _applyList.Clear();
            HaveNewApply = false;
            _shieldList.Clear();
            _beInvited.Clear();
            CurrentVote = null;
            _voteData.Clear();
            _recruitList.Clear();
            _recruitMemberList.Clear();
            _nearby.Clear();
            _myHelpState.Clear();
            AutoMatch = false;
            MatchSt = 0;
            CancelMatchText = null;
            WorldShoutTime = 0;
            EventDispatcher.Emit(GlobalEvent.EVT_TEAM_INFO_UPDATE);
        }
    }
}
