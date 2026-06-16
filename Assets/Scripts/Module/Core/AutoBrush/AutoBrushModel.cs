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

        private AutoBrushModel() { }

        public sealed class BrushStrangeInfo
        {
            public int Code;
            public int CurrentTimes;
            public int NeedTimes;
            public long AssistId;
            public long AssisterId;
        }

        public BrushStrangeInfo BrushInfo { get; private set; }
        public bool AutoBrushState { get; private set; }
        public int Level { get; private set; }
        public int RoleRank { get; private set; }
        public int RankType { get; private set; }
        public int MaxLevel { get; private set; }

        public void ResetData()
        {
            BrushInfo = null;
            AutoBrushState = false;
            Level = 0;
            RoleRank = 0;
            RankType = 0;
            MaxLevel = 0;
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

        public void SetRankInfo(int rankType, int roleRank, int level)
        {
            RankType = rankType;
            RoleRank = roleRank;
            Level = level;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED);
        }

        public bool CheckDoneState()
        {
            return MaxLevel > 0 && Level >= MaxLevel;
        }
    }
}
