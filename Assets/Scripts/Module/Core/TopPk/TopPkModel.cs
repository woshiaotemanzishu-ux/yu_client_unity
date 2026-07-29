using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.TopPk
{
    /// <summary>巅峰对决281家族的原始读侧状态；事件包与查询快照彼此隔离。</summary>
    public sealed class TopPkModel
    {
        public sealed class DailyCountReward
        {
            public byte Count { get; }
            public byte State { get; }

            public DailyCountReward(byte count, byte state)
            {
                Count = count;
                State = state;
            }
        }

        public sealed class InfoSnapshot
        {
            public ushort SeasonNumber { get; }
            public uint SeasonEndTime { get; }
            public byte RankLevel { get; }
            public uint Point { get; }
            public uint SeasonCount { get; }
            public uint SeasonWinCount { get; }
            public uint DailyHonorValue { get; }
            public byte HonorState { get; }
            public ushort DailyCount { get; }
            public IReadOnlyList<DailyCountReward> DailyRewards { get; }
            public ushort DailyBuyCount { get; }
            public byte YesterdayRankLevel { get; }

            public InfoSnapshot(ushort seasonNumber, uint seasonEndTime, byte rankLevel, uint point,
                uint seasonCount, uint seasonWinCount, uint dailyHonorValue, byte honorState,
                ushort dailyCount, IReadOnlyList<DailyCountReward> dailyRewards,
                ushort dailyBuyCount, byte yesterdayRankLevel)
            {
                SeasonNumber = seasonNumber;
                SeasonEndTime = seasonEndTime;
                RankLevel = rankLevel;
                Point = point;
                SeasonCount = seasonCount;
                SeasonWinCount = seasonWinCount;
                DailyHonorValue = dailyHonorValue;
                HonorState = honorState;
                DailyCount = dailyCount;
                DailyRewards = Freeze(dailyRewards);
                DailyBuyCount = dailyBuyCount;
                YesterdayRankLevel = yesterdayRankLevel;
            }
        }

        public sealed class LevelReward
        {
            public byte RankLevel { get; }
            public byte State { get; }

            public LevelReward(byte rankLevel, byte state)
            {
                RankLevel = rankLevel;
                State = state;
            }
        }

        public sealed class LevelRewardsSnapshot
        {
            public IReadOnlyList<LevelReward> Rewards { get; }
            public LevelRewardsSnapshot(IReadOnlyList<LevelReward> rewards) => Rewards = Freeze(rewards);
        }

        public sealed class ActivitySnapshot
        {
            public byte State { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }

            public ActivitySnapshot(byte state, uint startTime, uint endTime)
            {
                State = state;
                StartTime = startTime;
                EndTime = endTime;
            }
        }

        public sealed class MatchSnapshot
        {
            public byte Result { get; }
            public byte MyRankLevel { get; }
            public byte EnemyRankLevel { get; }
            public ulong FakeManPower { get; }

            public MatchSnapshot(byte result, byte myRankLevel, byte enemyRankLevel, ulong fakeManPower)
            {
                Result = result;
                MyRankLevel = myRankLevel;
                EnemyRankLevel = enemyRankLevel;
                FakeManPower = fakeManPower;
            }
        }

        public sealed class StageSnapshot
        {
            public byte Stage { get; }
            public uint Time { get; }

            public StageSnapshot(byte stage, uint time)
            {
                Stage = stage;
                Time = time;
            }
        }

        public sealed class ResultSnapshot
        {
            public byte Result { get; }
            public uint Honor { get; }
            public byte PointSign { get; }
            public uint PointDelta { get; }

            public ResultSnapshot(byte result, uint honor, byte pointSign, uint pointDelta)
            {
                Result = result;
                Honor = honor;
                PointSign = pointSign;
                PointDelta = pointDelta;
            }
        }

        public sealed class RankEntry
        {
            public ulong RoleId { get; }
            public string RoleName { get; }
            public byte Career { get; }
            public ulong Power { get; }
            public string GuildName { get; }
            public string Platform { get; }
            public ushort ServerNumber { get; }
            public byte RankLevel { get; }
            public uint Point { get; }

            public RankEntry(ulong roleId, string roleName, byte career, ulong power,
                string guildName, string platform, ushort serverNumber, byte rankLevel, uint point)
            {
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                Career = career;
                Power = power;
                GuildName = guildName ?? string.Empty;
                Platform = platform ?? string.Empty;
                ServerNumber = serverNumber;
                RankLevel = rankLevel;
                Point = point;
            }
        }

        public sealed class RanksSnapshot
        {
            public IReadOnlyList<RankEntry> Ranks { get; }
            public RanksSnapshot(IReadOnlyList<RankEntry> ranks) => Ranks = Freeze(ranks);
        }

        public sealed class PromotionSnapshot
        {
            public byte OldRankLevel { get; }
            public uint OldPoint { get; }
            public byte NewRankLevel { get; }
            public uint NewPoint { get; }

            public PromotionSnapshot(byte oldRankLevel, uint oldPoint, byte newRankLevel, uint newPoint)
            {
                OldRankLevel = oldRankLevel;
                OldPoint = oldPoint;
                NewRankLevel = newRankLevel;
                NewPoint = newPoint;
            }
        }

        public static readonly TopPkModel Instance = new TopPkModel();

        private TopPkModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorArgs { get; private set; }
        public InfoSnapshot Info { get; private set; }
        public LevelRewardsSnapshot LevelRewards { get; private set; }
        public ActivitySnapshot Activity { get; private set; }
        public MatchSnapshot LastMatch { get; private set; }
        public StageSnapshot LastStage { get; private set; }
        public ResultSnapshot LastResult { get; private set; }
        public RanksSnapshot Ranks { get; private set; }
        public PromotionSnapshot LastPromotion { get; private set; }
        public bool HasInfo => Info != null;
        public bool HasLevelRewards => LevelRewards != null;
        public bool HasActivity => Activity != null;
        public bool HasMatch => LastMatch != null;
        public bool HasStage => LastStage != null;
        public bool HasResult => LastResult != null;
        public bool HasRanks => Ranks != null;
        public bool HasPromotion => LastPromotion != null;

        public void SetError(uint code, string args)
        {
            HasError = true;
            LastErrorCode = code;
            LastErrorArgs = args;
        }

        public void ReplaceInfo(InfoSnapshot snapshot) => Info = snapshot;
        public void ReplaceLevelRewards(LevelRewardsSnapshot snapshot) => LevelRewards = snapshot;
        public void ReplaceActivity(ActivitySnapshot snapshot) => Activity = snapshot;
        public void ReplaceMatch(MatchSnapshot snapshot) => LastMatch = snapshot;
        public void ReplaceStage(StageSnapshot snapshot) => LastStage = snapshot;
        public void ReplaceResult(ResultSnapshot snapshot) => LastResult = snapshot;
        public void ReplaceRanks(RanksSnapshot snapshot) => Ranks = snapshot;
        public void ReplacePromotion(PromotionSnapshot snapshot) => LastPromotion = snapshot;

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            LastErrorArgs = null;
            Info = null;
            LevelRewards = null;
            Activity = null;
            LastMatch = null;
            LastStage = null;
            LastResult = null;
            Ranks = null;
            LastPromotion = null;
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
