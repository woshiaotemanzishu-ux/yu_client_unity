using System.Collections.Generic;
using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Jjc
{
    /// <summary>
    /// 排位赛(竞技场 JJC,yu_server pt_280;老端 ArenaController.ts/ArenaModel.ts)数据层。
    /// ⚠服务端计数断链(mod_jjc_cast.erl:87 唯一 increment 被注释)——挑战流程(28003)可正常发起并拿到结果,
    /// 但 mod_daily ?JJC_USE_NUM 永不增长,主线 101465(ctype35「挑战对手」)判定无法自然完成,待服务端修复
    /// (与大妖 63 guard 并列服务端工单)。客户端侧本类/控制器/壳先备好,服务端修复后即可用。
    /// </summary>
    public sealed class JjcModel
    {
        public static readonly JjcModel Instance = new JjcModel();
        private JjcModel() { }

        /// <summary>28002/28003 role_list 单项(字段名对照 ClientProtocol.json)。</summary>
        public sealed class RivalVo
        {
            public int Rank;           // 28002 rank:i / 28003 before_rank:h+rank:h(取 rank)
            public long RoleId;        // role_id:l
            public long Combat;        // combat:l
            public int Hp;             // hp:i(28003 无此字段,默认0)
            public int PetId;          // pet_id:i(28003 无此字段,默认0)
            public FigureProto Figure; // figure:RecFigure
        }

        public sealed class ErrorSnapshot
        {
            public uint Code { get; }
            public ErrorSnapshot(uint code) => Code = code;
        }

        public sealed class HonourQuerySnapshot
        {
            public uint Code { get; }
            public uint Honour { get; }
            public HonourQuerySnapshot(uint code, uint honour) { Code = code; Honour = honour; }
        }

        public sealed class BattleParticipantsSnapshot
        {
            public ulong SelfRobotId { get; }
            public ulong SelfRoleId { get; }
            public ulong RivalRobotId { get; }
            public ulong RivalRoleId { get; }

            public BattleParticipantsSnapshot(ulong selfRobotId, ulong selfRoleId, ulong rivalRobotId, ulong rivalRoleId)
            {
                SelfRobotId = selfRobotId;
                SelfRoleId = selfRoleId;
                RivalRobotId = rivalRobotId;
                RivalRoleId = rivalRoleId;
            }
        }

        public sealed class BattleStageSnapshot
        {
            public byte Stage { get; }
            public uint EndTime { get; }
            public BattleStageSnapshot(byte stage, uint endTime) { Stage = stage; EndTime = endTime; }
        }

        // ---- 28001 页面信息 ----
        public int Rank { get; private set; }
        public int HistoryRank { get; private set; }
        public int RewardRank { get; private set; }
        public long Combat { get; private set; }
        public int Hp { get; private set; }
        public int Num { get; private set; }           // 剩余挑战次数
        public int NumRefresh { get; private set; }
        public int Honour { get; private set; }
        public bool IsReward { get; private set; }
        public int PetId { get; private set; }
        public readonly List<int> BreakIdList = new List<int>();
        public bool HasInfo { get; private set; }

        // ---- 28002 随机对手 ----
        public readonly List<RivalVo> Rivals = new List<RivalVo>();
        public bool HasRivals { get; private set; }

        // ---- 28003 最近一次挑战结果 ----
        public bool LastChallengeWin { get; private set; }
        public readonly List<RivalVo> LastChallengeRoleList = new List<RivalVo>();
        public bool HasChallengeResult { get; private set; }

        // ---- 28004 挑战次数完整快照(与 28001 页面信息独立) ----
        public bool HasTimesInfo { get; private set; }
        public int TimesErrCode { get; private set; }
        public ushort LeftNum { get; private set; }
        public uint TimesRefreshAt { get; private set; }
        public ushort CanBuyNum { get; private set; }

        public sealed class RecordVo
        {
            public long RoleId;
            public string Picture;
            public uint PictureVer;
            public string Name;
            public byte Career, Sex, Turn, VipLv, Result, State;
            public ushort Lv;
            public long CombatPower;
            public uint RankRange, Time;
        }
        public int RecordsErrCode { get; private set; }
        public bool HasChallengeRecords { get; private set; }
        public readonly List<RecordVo> ChallengeRecords = new List<RecordVo>();

        /// <summary>28000/10/13/14 彼此隔离的最后原始切片；null 表示尚未收到。</summary>
        public ErrorSnapshot Error { get; private set; }
        public HonourQuerySnapshot HonourQuery { get; private set; }
        public BattleParticipantsSnapshot BattleParticipants { get; private set; }
        public BattleStageSnapshot BattleStage { get; private set; }

        /// <summary>28001 全量套值(对标老端 On28001 → arena_model.SetPageInfo)。</summary>
        public void Apply28001(int rank, int historyRank, int rewardRank, long combat, int hp, int num,
            int numRefresh, int honour, bool isReward, int petId, List<int> breakIdList)
        {
            Rank = rank;
            HistoryRank = historyRank;
            RewardRank = rewardRank;
            Combat = combat;
            Hp = hp;
            Num = num;
            NumRefresh = numRefresh;
            Honour = honour;
            IsReward = isReward;
            PetId = petId;
            BreakIdList.Clear();
            if (breakIdList != null) BreakIdList.AddRange(breakIdList);
            HasInfo = true;
        }

        /// <summary>28002 随机对手套值(对标老端 On28002 → arena_model.ChangeVar("challenge_player_list", ...))。</summary>
        public void Apply28002(List<RivalVo> list)
        {
            Rivals.Clear();
            if (list != null) Rivals.AddRange(list);
            HasRivals = true;
        }

        /// <summary>28003 挑战结果套值(对标老端 On28003 → arena_model.result_info)。result==1 视为胜利
        /// (ClientProtocol result:c 未标注枚举语义,仅按老端 ArenaResultView 常见 1=胜 的用法降级标注,如实注明不臆造具体败因)。</summary>
        public void Apply28003(int result, List<RivalVo> roleList)
        {
            LastChallengeWin = result == 1;
            LastChallengeRoleList.Clear();
            if (roleList != null) LastChallengeRoleList.AddRange(roleList);
            HasChallengeResult = true;
        }

        /// <summary>28004 完整快照绝对覆盖；不得回写 28001 的 Num/NumRefresh。</summary>
        public void Apply28004(int errCode, ushort leftNum, uint timesRefreshAt, ushort canBuyNum)
        {
            TimesErrCode = errCode;
            LeftNum = leftNum;
            TimesRefreshAt = timesRefreshAt;
            CanBuyNum = canBuyNum;
            HasTimesInfo = true;
        }

        /// <summary>28009 无条件整体替换；空列表也标记 loaded，不在数据层排序或截断。</summary>
        public void Apply28009(int errCode, List<RecordVo> records)
        {
            RecordsErrCode = errCode;
            ChallengeRecords.Clear();
            if (records != null) ChallengeRecords.AddRange(records);
            HasChallengeRecords = true;
        }

        public void ReplaceError(uint code) => Error = new ErrorSnapshot(code);
        public void ReplaceHonourQuery(uint code, uint honour) => HonourQuery = new HonourQuerySnapshot(code, honour);
        public void ReplaceBattleParticipants(ulong selfRobotId, ulong selfRoleId, ulong rivalRobotId, ulong rivalRoleId) =>
            BattleParticipants = new BattleParticipantsSnapshot(selfRobotId, selfRoleId, rivalRobotId, rivalRoleId);
        public void ReplaceBattleStage(byte stage, uint endTime) => BattleStage = new BattleStageSnapshot(stage, endTime);

        public void ClearReadContinuationSnapshots()
        {
            Error = null;
            HonourQuery = null;
            BattleParticipants = null;
            BattleStage = null;
        }

        public void Clear()
        {
            Rank = HistoryRank = RewardRank = Hp = Num = NumRefresh = Honour = PetId = 0;
            Combat = 0;
            IsReward = false;
            BreakIdList.Clear();
            HasInfo = false;
            Rivals.Clear();
            HasRivals = false;
            LastChallengeWin = false;
            LastChallengeRoleList.Clear();
            HasChallengeResult = false;
            HasTimesInfo = false;
            TimesErrCode = 0;
            LeftNum = 0;
            TimesRefreshAt = 0;
            CanBuyNum = 0;
            RecordsErrCode = 0;
            ChallengeRecords.Clear();
            HasChallengeRecords = false;
            ClearReadContinuationSnapshots();
        }
    }
}
