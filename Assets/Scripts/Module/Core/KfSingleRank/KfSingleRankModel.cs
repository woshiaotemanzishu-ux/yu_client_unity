using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        public sealed class AreaRankEntry
        {
            public byte Level { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public ushort ServerNum { get; }
            public uint GoTime { get; }

            public AreaRankEntry(byte level, ulong roleId, string roleName, ushort serverNum, uint goTime)
            {
                Level = level;
                RoleId = roleId;
                RoleName = roleName;
                ServerNum = serverNum;
                GoTime = goTime;
            }
        }

        public sealed class AreaSnapshot
        {
            public byte AreaId { get; }
            public IReadOnlyList<AreaRankEntry> Entries { get; }

            public AreaSnapshot(byte areaId, List<AreaRankEntry> entries)
            {
                AreaId = areaId;
                Entries = new List<AreaRankEntry>(entries ?? new List<AreaRankEntry>()).AsReadOnly();
            }
        }

        public static readonly KfSingleRankModel Instance = new KfSingleRankModel();

        private readonly List<LevelEntry> _levels = new List<LevelEntry>();
        private readonly IReadOnlyList<LevelEntry> _readOnlyLevels;
        private readonly Dictionary<byte, AreaSnapshot> _areaTops = new Dictionary<byte, AreaSnapshot>();
        private readonly IReadOnlyDictionary<byte, AreaSnapshot> _readOnlyAreaTops;

        private KfSingleRankModel()
        {
            _readOnlyLevels = _levels.AsReadOnly();
            _readOnlyAreaTops = new ReadOnlyDictionary<byte, AreaSnapshot>(_areaTops);
        }

        public bool HasData { get; private set; }
        public byte StartLevel { get; private set; }
        public byte RewardState { get; private set; }
        public IReadOnlyList<LevelEntry> Levels => _readOnlyLevels;
        public IReadOnlyDictionary<byte, AreaSnapshot> AreaTops => _readOnlyAreaTops;

        public bool TryGetAreaTop(byte areaId, out AreaSnapshot snapshot)
        {
            return _areaTops.TryGetValue(areaId, out snapshot);
        }

        public void ReplaceAreaTop(byte areaId, List<AreaRankEntry> entries)
        {
            _areaTops[areaId] = new AreaSnapshot(areaId, entries);
        }

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
            _areaTops.Clear();
            HasData = false;
        }
    }
}
