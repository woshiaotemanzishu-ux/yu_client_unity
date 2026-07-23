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

        public sealed class EquipExtraAttr
        {
            public byte Color { get; }
            public byte TypeId { get; }
            public ushort AttrId { get; }
            public uint AttrValue { get; }
            public byte PlusInterval { get; }
            public uint PlusUnit { get; }

            public EquipExtraAttr(byte color, byte typeId, ushort attrId, uint attrValue, byte plusInterval, uint plusUnit)
            {
                Color = color;
                TypeId = typeId;
                AttrId = attrId;
                AttrValue = attrValue;
                PlusInterval = plusInterval;
                PlusUnit = plusUnit;
            }
        }

        public sealed class DropLogEntry
        {
            public uint Time { get; }
            public uint ServerId { get; }
            public uint ServerNum { get; }
            public long RoleId { get; }
            public string Name { get; }
            public uint BossId { get; }
            public uint GoodsId { get; }
            public uint Num { get; }
            public uint Rating { get; }
            public IReadOnlyList<EquipExtraAttr> EquipExtraAttrs { get; }
            public byte IsTop { get; }

            public DropLogEntry(uint time, uint serverId, uint serverNum, long roleId, string name, uint bossId, uint goodsId, uint num, uint rating, List<EquipExtraAttr> equipExtraAttrs, byte isTop)
            {
                Time = time;
                ServerId = serverId;
                ServerNum = serverNum;
                RoleId = roleId;
                Name = name;
                BossId = bossId;
                GoodsId = goodsId;
                Num = num;
                Rating = rating;
                EquipExtraAttrs = (equipExtraAttrs ?? new List<EquipExtraAttr>()).AsReadOnly();
                IsTop = isTop;
            }
        }

        public static readonly DragonWhisperModel Instance = new DragonWhisperModel();

        private readonly List<MapEntry> _maps = new List<MapEntry>();
        private readonly List<DropLogEntry> _dropLogs = new List<DropLogEntry>();
        private readonly IReadOnlyList<MapEntry> _readOnlyMaps;
        private readonly IReadOnlyList<DropLogEntry> _readOnlyDropLogs;

        private DragonWhisperModel()
        {
            _readOnlyMaps = _maps.AsReadOnly();
            _readOnlyDropLogs = _dropLogs.AsReadOnly();
        }

        public bool HasSnapshot { get; private set; }
        public byte LeftCount { get; private set; }
        public byte AllCount { get; private set; }
        public IReadOnlyList<MapEntry> Maps => _readOnlyMaps;
        public bool HasDropLog { get; private set; }
        public IReadOnlyList<DropLogEntry> DropLogs => _readOnlyDropLogs;

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
            _dropLogs.Clear();
            HasSnapshot = false;
            HasDropLog = false;
        }

        public void ReplaceDropLog(List<DropLogEntry> dropLogs)
        {
            _dropLogs.Clear();
            if (dropLogs != null) _dropLogs.AddRange(dropLogs);
            HasDropLog = true;
        }
    }
}
