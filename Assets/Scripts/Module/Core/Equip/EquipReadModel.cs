using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>装备遗留只读切片：神装15217/15219与共鸣套装15220/15223/15262。</summary>
    public sealed class EquipReadModel
    {
        public sealed class GodEntry
        {
            public GodEntry(byte pos, ushort level) { Pos = pos; Level = level; }
            public byte Pos { get; }
            public ushort Level { get; }
        }

        public sealed class SuitEntry
        {
            public SuitEntry(byte equipType, byte type, ushort level) { EquipType = equipType; Type = type; Level = level; }
            public byte EquipType { get; }
            public byte Type { get; }
            public ushort Level { get; }
        }

        public sealed class RewardEntry
        {
            public RewardEntry(byte type, uint id, ushort num, string attrList)
            {
                Type = type; Id = id; Num = num; AttrList = attrList;
            }
            public byte Type { get; }
            public uint Id { get; }
            public ushort Num { get; }
            public string AttrList { get; }
        }

        public sealed class SuitReturnPreview
        {
            public SuitReturnPreview(byte equipType, byte makeType, List<RewardEntry> rewards)
            {
                EquipType = equipType;
                MakeType = makeType;
                Rewards = (rewards ?? new List<RewardEntry>()).AsReadOnly();
            }
            public byte EquipType { get; }
            public byte MakeType { get; }
            public IReadOnlyList<RewardEntry> Rewards { get; }
        }

        public sealed class SuitPowerEntry
        {
            public SuitPowerEntry(byte num, ulong combat) { Num = num; Combat = combat; }
            public byte Num { get; }
            public ulong Combat { get; }
        }

        public sealed class SuitPowerSnapshot
        {
            public SuitPowerSnapshot(byte pos, byte type, ushort level, List<SuitPowerEntry> entries)
            {
                Pos = pos; Type = type; Level = level;
                Entries = (entries ?? new List<SuitPowerEntry>()).AsReadOnly();
            }
            public byte Pos { get; }
            public byte Type { get; }
            public ushort Level { get; }
            public IReadOnlyList<SuitPowerEntry> Entries { get; }
        }

        public static readonly EquipReadModel Instance = new EquipReadModel();

        private readonly Dictionary<ushort, SuitReturnPreview> _returnPreviews = new Dictionary<ushort, SuitReturnPreview>();
        private readonly Dictionary<uint, SuitPowerSnapshot> _suitPowers = new Dictionary<uint, SuitPowerSnapshot>();

        private EquipReadModel() { }

        public bool HasGodInfo { get; private set; }
        public uint GodTotalPower { get; private set; }
        public IReadOnlyList<GodEntry> GodEntries { get; private set; } = Array.Empty<GodEntry>();
        public bool HasGodPowerPreview { get; private set; }
        public uint GodPowerPreview { get; private set; }
        public bool HasSuitInfo { get; private set; }
        public IReadOnlyList<SuitEntry> SuitEntries { get; private set; } = Array.Empty<SuitEntry>();
        public int ReturnPreviewCount => _returnPreviews.Count;
        public int SuitPowerCount => _suitPowers.Count;

        public void ReplaceGodInfo(uint totalPower, List<GodEntry> entries)
        {
            HasGodInfo = true;
            GodTotalPower = totalPower;
            GodEntries = (entries ?? new List<GodEntry>()).AsReadOnly();
        }

        public void ReplaceGodPowerPreview(uint power)
        {
            HasGodPowerPreview = true;
            GodPowerPreview = power;
        }

        public void ReplaceSuitInfo(List<SuitEntry> entries)
        {
            HasSuitInfo = true;
            SuitEntries = (entries ?? new List<SuitEntry>()).AsReadOnly();
        }

        public void ReplaceReturnPreview(SuitReturnPreview snapshot) =>
            _returnPreviews[ReturnKey(snapshot.EquipType, snapshot.MakeType)] = snapshot;

        public bool TryGetReturnPreview(byte equipType, byte makeType, out SuitReturnPreview snapshot) =>
            _returnPreviews.TryGetValue(ReturnKey(equipType, makeType), out snapshot);

        public void ReplaceSuitPower(SuitPowerSnapshot snapshot) =>
            _suitPowers[PowerKey(snapshot.Pos, snapshot.Type, snapshot.Level)] = snapshot;

        public bool TryGetSuitPower(byte pos, byte type, ushort level, out SuitPowerSnapshot snapshot) =>
            _suitPowers.TryGetValue(PowerKey(pos, type, level), out snapshot);

        public void Reset()
        {
            HasGodInfo = false;
            GodTotalPower = 0;
            GodEntries = Array.Empty<GodEntry>();
            HasGodPowerPreview = false;
            GodPowerPreview = 0;
            HasSuitInfo = false;
            SuitEntries = Array.Empty<SuitEntry>();
            _returnPreviews.Clear();
            _suitPowers.Clear();
        }

        private static ushort ReturnKey(byte equipType, byte makeType) => (ushort)((equipType << 8) | makeType);
        private static uint PowerKey(byte pos, byte type, ushort level) => ((uint)pos << 24) | ((uint)type << 16) | level;
    }
}
