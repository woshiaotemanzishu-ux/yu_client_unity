using System.Collections.Generic;

namespace Shenxiao.Module.Core.KfSingleRank
{
    public sealed class KfSingleRankModel
    {
        public sealed class LevelEntry
        {
            public byte Level { get; }
            public uint GoTime { get; }

            public LevelEntry(byte level, uint goTime)
            {
                Level = level;
                GoTime = goTime;
            }
        }

        public static readonly KfSingleRankModel Instance = new KfSingleRankModel();

        private readonly List<LevelEntry> _levels = new List<LevelEntry>();
        private readonly IReadOnlyList<LevelEntry> _readOnlyLevels;

        private KfSingleRankModel()
        {
            _readOnlyLevels = _levels.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public byte StartLevel { get; private set; }
        public byte RewardState { get; private set; }
        public IReadOnlyList<LevelEntry> Levels => _readOnlyLevels;

        public void Replace(byte startLevel, byte rewardState, List<LevelEntry> levels)
        {
            StartLevel = startLevel;
            RewardState = rewardState;
            _levels.Clear();
            if (levels != null) _levels.AddRange(levels);
            HasData = true;
        }

        public void Reset()
        {
            StartLevel = 0;
            RewardState = 0;
            _levels.Clear();
            HasData = false;
        }
    }
}
