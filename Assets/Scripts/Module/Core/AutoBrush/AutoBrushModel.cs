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

        public BrushStrangeInfo BrushInfo { get; private set; }
        public bool AutoBrushState { get; private set; }
        public int Level { get; private set; }
        public int RoleRank { get; private set; }
        public int RankType { get; private set; }
        public string TopRankName { get; private set; } = "";
        public int TopRankLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public bool FailureState { get; private set; }
        public int LastFailureLevel { get; private set; }
        public bool HasNextStageReward { get; private set; }
        public uint NextStageRewardCode { get; private set; }
        public ulong NextStageRewardGate { get; private set; }
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
            MaxLevel = 0;
            FailureState = false;
            LastFailureLevel = 0;
            HasNextStageReward = false;
            NextStageRewardCode = 0;
            NextStageRewardGate = 0;
            HasTutorialNode = false;
            TutorialNode = 0;
            HasAssistInfo = false;
            AssistDailyCount = 0;
            AssistNextTime = 0;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED);
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED);
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

        public void SetRankInfo(int rankType, int roleRank, int level, string topRankName = "", int topRankLevel = 0)
        {
            RankType = rankType;
            RoleRank = roleRank;
            Level = level;
            TopRankName = topRankName ?? "";
            TopRankLevel = topRankLevel;
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
