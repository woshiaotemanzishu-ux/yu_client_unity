using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.DungeonPartner
{
    public sealed class DungeonPartnerModel
    {
        public sealed class DungeonEntry { public uint DungeonId { get; } public byte Score { get; } public DungeonEntry(uint id, byte score) { DungeonId = id; Score = score; } }
        public sealed class StageRewardEntry { public ushort Score { get; } public byte Status { get; } public StageRewardEntry(ushort score, byte status) { Score = score; Status = status; } }
        public sealed class DungeonSnapshot { public bool Loaded { get; internal set; } public IReadOnlyList<DungeonEntry> Entries { get; internal set; } = new List<DungeonEntry>().AsReadOnly(); }
        public sealed class StageRewardSnapshot { public bool Loaded { get; internal set; } public IReadOnlyList<StageRewardEntry> Entries { get; internal set; } = new List<StageRewardEntry>().AsReadOnly(); }

        public static readonly DungeonPartnerModel Instance = new DungeonPartnerModel();
        private readonly Dictionary<byte, DungeonSnapshot> _dungeons = new Dictionary<byte, DungeonSnapshot>();
        private readonly Dictionary<byte, StageRewardSnapshot> _stageRewards = new Dictionary<byte, StageRewardSnapshot>();
        public ushort SweepCount { get; private set; }
        public byte CurrentPage { get; private set; } = 1;
        public event Action<byte> DungeonsChanged;
        public event Action<byte> StageRewardsChanged;

        public bool TryGetDungeons(byte level, out DungeonSnapshot snapshot) => _dungeons.TryGetValue(level, out snapshot);
        public bool TryGetStageRewards(byte level, out StageRewardSnapshot snapshot) => _stageRewards.TryGetValue(level, out snapshot);
        public void SetCurrentPage(byte level) { if (level > 0) CurrentPage = level; }
        public int GetTotalScore(byte level)
        {
            if (!TryGetDungeons(level, out DungeonSnapshot snapshot)) return 0;
            int total = 0;
            for (int i = 0; i < snapshot.Entries.Count; i++) total += snapshot.Entries[i].Score;
            return total;
        }
        public void ReplaceDungeons(byte level, ushort sweepCount, List<DungeonEntry> entries)
        {
            SweepCount = sweepCount;
            _dungeons[level] = new DungeonSnapshot { Loaded = true, Entries = (entries ?? new List<DungeonEntry>()).AsReadOnly() };
            DungeonsChanged?.Invoke(level);
        }
        public void ReplaceStageRewards(byte level, List<StageRewardEntry> entries)
        {
            _stageRewards[level] = new StageRewardSnapshot { Loaded = true, Entries = (entries ?? new List<StageRewardEntry>()).AsReadOnly() };
            StageRewardsChanged?.Invoke(level);
        }
        public void Reset() { SweepCount = 0; CurrentPage = 1; _dungeons.Clear(); _stageRewards.Clear(); }
    }
}
