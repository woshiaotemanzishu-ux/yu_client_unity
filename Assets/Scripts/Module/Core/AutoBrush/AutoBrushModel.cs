using System.Collections.Generic;
using Shenxiao.Framework.Event;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// Minimal auto-brush data model for MainUI parity.
    /// Old client source: commonModel/AutoBrushModel.ts.
    /// </summary>
    public sealed class AutoBrushModel
    {
        public static readonly AutoBrushModel Instance = new AutoBrushModel();

        /// <summary>主线副本大妖怪的占位 type_id(对标老端 AutoBrushModel.AutoBrushMonsterId=7001);
        /// 服务端只下发该占位,真实模型/名字/缩放由客户端按层级覆盖,见 <see cref="AutoBrushConfigs.GetBrushBossModel"/>。</summary>
        public const int AutoBrushMonsterId = 7001;
        public const int IgnoreRedTaskId = 100811;

        private AutoBrushModel() { }

        public sealed class BrushStrangeInfo
        {
            public int Code;
            public int CurrentTimes;
            public int NeedTimes;
            public long AssistId;
            public long AssisterId;
        }

        public readonly struct RewardEntry
        {
            public readonly int Style;
            public readonly int RawTypeId;
            public readonly long Count;

            public RewardEntry(int style, int rawTypeId, long count)
            {
                Style = style;
                RawTypeId = rawTypeId;
                Count = count;
            }
        }

        public readonly struct RankEntry
        {
            public readonly uint ServerId;
            public readonly uint ServerNum;
            public readonly long RoleId;
            public readonly string RoleName;
            public readonly uint Rank;
            public readonly uint Level;
            public readonly long Combat;

            public RankEntry(uint serverId, uint serverNum, long roleId, string roleName,
                uint rank, uint level, long combat)
            {
                ServerId = serverId;
                ServerNum = serverNum;
                RoleId = roleId;
                RoleName = roleName ?? "";
                Rank = rank;
                Level = level;
                Combat = combat;
            }
        }

        public readonly struct StageRewardEntry
        {
            public byte Style { get; }
            public uint TypeId { get; }
            public uint Count { get; }

            public StageRewardEntry(byte style, uint typeId, uint count)
            {
                Style = style;
                TypeId = typeId;
                Count = count;
            }
        }

        public sealed class StageRewardResult
        {
            public uint Code { get; }
            public IReadOnlyList<StageRewardEntry> Rewards { get; }

            public StageRewardResult(uint code, List<StageRewardEntry> rewards)
            {
                Code = code;
                Rewards = (rewards ?? new List<StageRewardEntry>()).ToArray();
            }
        }

        public BrushStrangeInfo BrushInfo { get; private set; }
        public bool AutoBrushState { get; private set; }
        public int Level { get; private set; }
        public int RoleRank { get; private set; }
        public int RankType { get; private set; }
        public string TopRankName { get; private set; } = "";
        public int TopRankLevel { get; private set; }
        public IReadOnlyList<RankEntry> RankEntries { get; private set; } = new RankEntry[0];
        public int MaxLevel { get; private set; }
        public bool FailureState { get; private set; }
        public int LastFailureLevel { get; private set; }
        public bool HasNextStageReward { get; private set; }
        public uint NextStageRewardCode { get; private set; }
        public ulong NextStageRewardGate { get; private set; }
        public StageRewardResult LastStageRewardResult { get; private set; }
        public bool HasTutorialNode { get; private set; }
        public byte TutorialNode { get; private set; }
        public bool HasAssistInfo { get; private set; }
        public ushort AssistDailyCount { get; private set; }
        public uint AssistNextTime { get; private set; }

        public void ResetData()
        {
            BrushInfo = null;
            AutoBrushState = false;
            Level = 0;
            RoleRank = 0;
            RankType = 0;
            TopRankName = "";
            TopRankLevel = 0;
            RankEntries = new RankEntry[0];
            MaxLevel = 0;
            FailureState = false;
            LastFailureLevel = 0;
            HasNextStageReward = false;
            NextStageRewardCode = 0;
            NextStageRewardGate = 0;
            LastStageRewardResult = null;
            HasTutorialNode = false;
            TutorialNode = 0;
            HasAssistInfo = false;
            AssistDailyCount = 0;
            AssistNextTime = 0;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_STAGE_REWARD_UPDATED);
        }

        public void SetBrushStrangeInfo(BrushStrangeInfo info)
        {
            BrushInfo = info;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED);
        }

        public void SetAutoBrushStrangeState(bool state)
        {
            AutoBrushState = state;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED);
        }

        public void SetRankInfo(int rankType, int roleRank, int level, string topRankName = "", int topRankLevel = 0,
            List<RankEntry> entries = null)
        {
            RankType = rankType;
            RoleRank = roleRank;
            Level = level;
            TopRankName = topRankName ?? "";
            TopRankLevel = topRankLevel;
            RankEntries = (entries ?? new List<RankEntry>()).ToArray();
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED);
        }

        public void SetMaxLevel(int maxLevel)
        {
            MaxLevel = maxLevel;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED);
        }

        public void SetLevel(int level)
        {
            Level = level;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED);
        }

        public void SetFailureState(bool failure, int level = 0)
        {
            FailureState = failure;
            if (failure) LastFailureLevel = level;
        }

        public void ReplaceNextStageReward(uint code, ulong gate)
        {
            HasNextStageReward = true;
            NextStageRewardCode = code;
            NextStageRewardGate = gate;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_STAGE_REWARD_UPDATED);
        }

        public void ReplaceStageRewardResult(uint code, List<StageRewardEntry> rewards)
        {
            LastStageRewardResult = new StageRewardResult(code, rewards);
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_STAGE_REWARD_UPDATED);
        }

        public void ReplaceTutorialNode(byte node)
        {
            HasTutorialNode = true;
            TutorialNode = node;
        }

        public void ReplaceAssistInfo(ushort dailyCount, uint nextTime)
        {
            HasAssistInfo = true;
            AssistDailyCount = dailyCount;
            AssistNextTime = nextTime;
        }

        public bool CheckDoneState()
        {
            return MaxLevel > 0 && Level >= MaxLevel;
        }
    }
}
