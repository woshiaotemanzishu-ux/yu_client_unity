using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Kf1vn
{
    /// <summary>诸天王者621族原始读侧状态。列表一律保留服务器线序与重复项。</summary>
    public sealed class Kf1vnModel
    {
        public sealed class ObjectEntry
        {
            public byte Type { get; internal set; }
            public uint TypeId { get; internal set; }
            public uint Num { get; internal set; }
        }

        public sealed class WaitInfoSnapshot
        {
            public byte LeftTimes { get; internal set; }
            public uint Score { get; internal set; }
            public uint Time { get; internal set; }
            public ushort Win { get; internal set; }
            public byte Lose { get; internal set; }
            public ulong ExpSum { get; internal set; }
            public ushort DefNum { get; internal set; }
        }

        public sealed class QualificationRole
        {
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public ulong CombatPower { get; internal set; }
            public ushort Win { get; internal set; }
            public byte Lose { get; internal set; }
            public byte Sex { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ushort Level { get; internal set; }
        }

        public sealed class QualificationBattleSnapshot
        {
            public IReadOnlyList<QualificationRole> Roles { get; internal set; }
            public uint LoadingTime { get; internal set; }
            public uint BattleTime { get; internal set; }
        }

        public sealed class ResultRole
        {
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public byte Sex { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ushort Level { get; internal set; }
            public ulong Hp { get; internal set; }
            public ulong HpLimit { get; internal set; }
        }

        public sealed class QualificationResultSnapshot
        {
            public byte Result { get; internal set; }
            public uint OldScore { get; internal set; }
            public ushort AddScore { get; internal set; }
            public byte LeftTimes { get; internal set; }
            public byte IsTimeout { get; internal set; }
            public IReadOnlyList<ResultRole> Roles { get; internal set; }
        }

        public sealed class QualificationSettlementSnapshot
        {
            public byte IsDef { get; internal set; }
            public ushort Rank { get; internal set; }
            public uint Score { get; internal set; }
            public IReadOnlyList<ObjectEntry> Award { get; internal set; }
        }

        public sealed class QualificationRankEntry
        {
            public byte Rank { get; internal set; }
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public string GuildName { get; internal set; }
            public byte Vip { get; internal set; }
            public uint Score { get; internal set; }
            public ushort Win { get; internal set; }
            public byte Lose { get; internal set; }
            public ulong CombatPower { get; internal set; }
            public byte Career { get; internal set; }
            public ushort Level { get; internal set; }
        }

        public sealed class QualificationRankSnapshot
        {
            public byte Area { get; internal set; }
            public IReadOnlyList<QualificationRankEntry> Entries { get; internal set; }
        }

        public sealed class ChallengerEntry
        {
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public byte Turn { get; internal set; }
            public byte Sex { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ushort Level { get; internal set; }
            public ulong CombatPower { get; internal set; }
        }

        public sealed class LeaderBattleSnapshot
        {
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public ulong CombatPower { get; internal set; }
            public ushort Win { get; internal set; }
            public byte Lose { get; internal set; }
            public byte Sex { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ushort Level { get; internal set; }
            public IReadOnlyList<ChallengerEntry> Challengers { get; internal set; }
            public uint LoadingTime { get; internal set; }
            public uint BattleTime { get; internal set; }
        }

        public sealed class LeaderResultSnapshot
        {
            public byte Result { get; internal set; }
            public byte ChallengerNum { get; internal set; }
            public byte TotalChallengerNum { get; internal set; }
            public ulong RoleId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public byte Sex { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ushort Level { get; internal set; }
            public ulong Hp { get; internal set; }
            public ulong HpLimit { get; internal set; }
            public IReadOnlyList<ObjectEntry> Award { get; internal set; }
        }

        public sealed class LeaderRankEntry
        {
            public byte Rank { get; internal set; }
            public ushort ServerId { get; internal set; }
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public string GuildName { get; internal set; }
            public byte Vip { get; internal set; }
            public uint Score { get; internal set; }
            public byte Turn { get; internal set; }
            public ulong CombatPower { get; internal set; }
            public byte Career { get; internal set; }
            public ushort SurvivalTime { get; internal set; }
            public byte Lose { get; internal set; }
            public ushort Level { get; internal set; }
            public ulong Hp { get; internal set; }
            public ulong HpLimit { get; internal set; }
        }

        public sealed class LeaderRankSnapshot
        {
            public byte Area { get; internal set; }
            public IReadOnlyList<LeaderRankEntry> Entries { get; internal set; }
            public IReadOnlyList<ObjectEntry> DailyAward { get; internal set; }
        }

        public sealed class QuizChallengerEntry
        {
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public byte Turn { get; internal set; }
            public byte Sex { get; internal set; }
            public ushort Level { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ulong CombatPower { get; internal set; }
        }

        public sealed class QuizBattleEntry
        {
            public ushort BattleId { get; internal set; }
            public byte Status { get; internal set; }
            public ulong PlayerId { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public string Name { get; internal set; }
            public byte Career { get; internal set; }
            public byte Turn { get; internal set; }
            public byte Sex { get; internal set; }
            public ushort Level { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVer { get; internal set; }
            public ulong CombatPower { get; internal set; }
            public IReadOnlyList<QuizChallengerEntry> Challengers { get; internal set; }
            public byte BattleResult { get; internal set; }
            public byte IsBet { get; internal set; }
            public byte BetResult { get; internal set; }

            internal QuizBattleEntry WithResult(byte status, byte battleResult, byte isBet, byte betResult)
            {
                return new QuizBattleEntry
                {
                    BattleId = BattleId, Status = status, PlayerId = PlayerId, Platform = Platform,
                    ServerNum = ServerNum, ServerName = ServerName, Name = Name, Career = Career,
                    Turn = Turn, Sex = Sex, Level = Level, Picture = Picture, PictureVer = PictureVer,
                    CombatPower = CombatPower, Challengers = Challengers, BattleResult = battleResult,
                    IsBet = isBet, BetResult = betResult
                };
            }
        }

        public sealed class QuizSnapshot
        {
            public IReadOnlyList<QuizBattleEntry> Battles { get; internal set; }
            public ushort DefNum { get; internal set; }
            public byte BetNum { get; internal set; }
        }

        public sealed class WaitingRankSnapshot
        {
            public byte Rank { get; internal set; }
            public string TopName { get; internal set; }
        }

        public sealed class LeaderSettlementSnapshot
        {
            public byte Rank { get; internal set; }
            public ushort Score { get; internal set; }
            public IReadOnlyList<ObjectEntry> Award { get; internal set; }
            public byte Turn { get; internal set; }
        }

        public sealed class QuizResultDelta
        {
            public ushort BattleId { get; internal set; }
            public byte BattleResult { get; internal set; }
            public byte BetResult { get; internal set; }
        }

        public sealed class QuizHistoryEntry
        {
            public ulong Key { get; internal set; }
            public string Platform { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string Name { get; internal set; }
            public byte Race2Turn { get; internal set; }
            public byte BetCostType { get; internal set; }
            public byte BetResult { get; internal set; }
            public byte Status { get; internal set; }
        }

        public sealed class QuizHistorySnapshot
        {
            public IReadOnlyList<QuizHistoryEntry> Entries { get; internal set; }
        }

        public sealed class BattleResultDelta
        {
            public ushort BattleId { get; internal set; }
            public byte BattleResult { get; internal set; }
        }

        public static readonly Kf1vnModel Instance = new Kf1vnModel();
        private Kf1vnModel() { }

        public const string ICON_TYPE = "621";

        // 既有图标切片，保留字段接口以兼容当前 HUD 与历史专项。
        public int Stage;
        public int Turn;
        public long Edtime;
        public int SubStage;
        public long SubEdtime;
        public bool HasStageInfo;
        public bool HasActivityInfo { get; private set; }
        public byte IsSign { get; private set; }
        public uint SignNum { get; private set; }
        public ushort DefNum { get; private set; }
        public byte Zone { get; private set; }

        private readonly Dictionary<byte, QualificationRankSnapshot> _qualificationRanks =
            new Dictionary<byte, QualificationRankSnapshot>();
        private readonly Dictionary<byte, LeaderRankSnapshot> _leaderRanks =
            new Dictionary<byte, LeaderRankSnapshot>();

        public WaitInfoSnapshot WaitInfo { get; private set; }
        public QualificationBattleSnapshot QualificationBattle { get; private set; }
        public QualificationResultSnapshot LastQualificationResult { get; private set; }
        public QualificationSettlementSnapshot LastQualificationSettlement { get; private set; }
        public IReadOnlyDictionary<byte, QualificationRankSnapshot> QualificationRanks => _qualificationRanks;
        public LeaderBattleSnapshot LeaderBattle { get; private set; }
        public LeaderResultSnapshot LastLeaderResult { get; private set; }
        public IReadOnlyDictionary<byte, LeaderRankSnapshot> LeaderRanks => _leaderRanks;
        public QuizSnapshot Quiz { get; private set; }
        public WaitingRankSnapshot WaitingRank { get; private set; }
        public LeaderSettlementSnapshot LastLeaderSettlement { get; private set; }
        public QuizResultDelta LastQuizResult { get; private set; }
        public QuizHistorySnapshot QuizHistory { get; private set; }
        public BattleResultDelta LastBattleResult { get; private set; }

        public void SetActivityInfo(byte isSign, uint signNum, ushort defNum, byte zone)
        {
            HasActivityInfo = true;
            IsSign = isSign;
            SignNum = signNum;
            DefNum = defNum;
            Zone = zone;
        }

        public void SetStageInfo(int stage, int turn, long edtime, int subStage, long subEdtime)
        {
            Stage = stage;
            Turn = turn;
            Edtime = edtime;
            SubStage = subStage;
            SubEdtime = subEdtime;
            HasStageInfo = true;
        }

        public void ReplaceWaitInfo(WaitInfoSnapshot value) => WaitInfo = value;

        public void ReplaceQualificationBattle(QualificationBattleSnapshot value)
        {
            value.Roles = Freeze(value.Roles);
            QualificationBattle = value;
        }

        public void ReplaceQualificationResult(QualificationResultSnapshot value)
        {
            value.Roles = Freeze(value.Roles);
            LastQualificationResult = value;
        }

        public void ReplaceQualificationSettlement(QualificationSettlementSnapshot value)
        {
            value.Award = Freeze(value.Award);
            LastQualificationSettlement = value;
        }

        public void ReplaceQualificationRank(QualificationRankSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            _qualificationRanks[value.Area] = value;
        }

        public void ReplaceLeaderBattle(LeaderBattleSnapshot value)
        {
            value.Challengers = Freeze(value.Challengers);
            LeaderBattle = value;
        }

        public void ReplaceLeaderResult(LeaderResultSnapshot value)
        {
            value.Award = Freeze(value.Award);
            LastLeaderResult = value;
        }

        public void ReplaceLeaderRank(LeaderRankSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            value.DailyAward = Freeze(value.DailyAward);
            _leaderRanks[value.Area] = value;
        }

        public void ReplaceQuiz(QuizSnapshot value)
        {
            if (value.Battles != null)
                for (int i = 0; i < value.Battles.Count; i++)
                    value.Battles[i].Challengers = Freeze(value.Battles[i].Challengers);
            value.Battles = Freeze(value.Battles);
            Quiz = value;
        }

        public void ReplaceWaitingRank(WaitingRankSnapshot value) => WaitingRank = value;

        public void ReplaceLeaderSettlement(LeaderSettlementSnapshot value)
        {
            value.Award = Freeze(value.Award);
            LastLeaderSettlement = value;
        }

        /// <summary>62123保存raw增量；仅在62117已加载且bet_result非0时按旧端语义补丁首个同BattleId项。</summary>
        public void ApplyQuizResult(QuizResultDelta value)
        {
            LastQuizResult = value;
            if (Quiz == null || value.BetResult == 0) return;
            PatchQuiz(value.BattleId, entry => entry.WithResult(2, value.BattleResult, 1, value.BetResult));
        }

        public void ReplaceQuizHistory(QuizHistorySnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            QuizHistory = value;
        }

        /// <summary>62135保存raw增量；只补丁已加载62117首个同BattleId项，非零结果同时置status=2。</summary>
        public void ApplyBattleResult(BattleResultDelta value)
        {
            LastBattleResult = value;
            if (Quiz == null) return;
            PatchQuiz(value.BattleId, entry => entry.WithResult(
                value.BattleResult == 0 ? entry.Status : (byte)2,
                value.BattleResult, entry.IsBet, entry.BetResult));
        }

        public bool TryGetQualificationRank(byte area, out QualificationRankSnapshot value) =>
            _qualificationRanks.TryGetValue(area, out value);

        public bool TryGetLeaderRank(byte area, out LeaderRankSnapshot value) =>
            _leaderRanks.TryGetValue(area, out value);

        public bool GetEntranceOpenState()
        {
            return HasStageInfo && Stage >= 1 && Stage != 6
                && !(Stage == 1 && HasActivityInfo && IsSign == 1);
        }

        public string GetIconText()
        {
            if (Stage == 1) return "报名中";
            if (Stage >= 2) return "进行中";
            return string.Empty;
        }

        public void Reset()
        {
            Stage = 0;
            Turn = 0;
            Edtime = 0;
            SubStage = 0;
            SubEdtime = 0;
            HasStageInfo = false;
            HasActivityInfo = false;
            IsSign = 0;
            SignNum = 0;
            DefNum = 0;
            Zone = 0;
            WaitInfo = null;
            QualificationBattle = null;
            LastQualificationResult = null;
            LastQualificationSettlement = null;
            _qualificationRanks.Clear();
            LeaderBattle = null;
            LastLeaderResult = null;
            _leaderRanks.Clear();
            Quiz = null;
            WaitingRank = null;
            LastLeaderSettlement = null;
            LastQuizResult = null;
            QuizHistory = null;
            LastBattleResult = null;
        }

        private void PatchQuiz(ushort battleId, Func<QuizBattleEntry, QuizBattleEntry> patch)
        {
            var entries = new List<QuizBattleEntry>(Quiz.Battles.Count);
            bool changed = false;
            for (int i = 0; i < Quiz.Battles.Count; i++)
            {
                QuizBattleEntry current = Quiz.Battles[i];
                if (!changed && current.BattleId == battleId)
                {
                    entries.Add(patch(current));
                    changed = true;
                }
                else entries.Add(current);
            }
            if (changed) Quiz = new QuizSnapshot
            {
                Battles = Freeze(entries), DefNum = Quiz.DefNum, BetNum = Quiz.BetNum
            };
        }

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }
}
