using System.Collections.Generic;

namespace Shenxiao.Module.Core.DragonWhisper
{
    /// <summary>65101 服务端主面板快照；保留原始 map/monster 顺序及重复项。</summary>
    public sealed class DragonWhisperModel
    {
        public sealed class MonsterEntry
        {
            public uint MonsterId { get; }
            public uint RebornTime { get; }

            public MonsterEntry(uint monsterId, uint rebornTime)
            {
                MonsterId = monsterId;
                RebornTime = rebornTime;
            }
        }

        public sealed class MapEntry
        {
            public byte MapId { get; }
            public ushort RoleNum { get; }
            public IReadOnlyList<MonsterEntry> Monsters { get; }

            public MapEntry(byte mapId, ushort roleNum, List<MonsterEntry> monsters)
            {
                MapId = mapId;
                RoleNum = roleNum;
                Monsters = (monsters ?? new List<MonsterEntry>()).AsReadOnly();
            }
        }

        public static readonly DragonWhisperModel Instance = new DragonWhisperModel();

        private readonly List<MapEntry> _maps = new List<MapEntry>();
        private readonly IReadOnlyList<MapEntry> _readOnlyMaps;

        private DragonWhisperModel()
        {
            _readOnlyMaps = _maps.AsReadOnly();
        }

        public bool HasSnapshot { get; private set; }
        public byte LeftCount { get; private set; }
        public byte AllCount { get; private set; }
        public IReadOnlyList<MapEntry> Maps => _readOnlyMaps;

        public void Replace(byte leftCount, byte allCount, List<MapEntry> maps)
        {
            LeftCount = leftCount;
            AllCount = allCount;
            _maps.Clear();
            if (maps != null) _maps.AddRange(maps);
            HasSnapshot = true;
        }

        public void Reset()
        {
            LeftCount = 0;
            AllCount = 0;
            _maps.Clear();
            HasSnapshot = false;
        }
    }
}
