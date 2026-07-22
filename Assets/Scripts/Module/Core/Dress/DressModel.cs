using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Shenxiao.Module.Core.Dress
{
    public sealed class DressModel
    {
        public sealed class Entry { public uint DressId { get; } public ushort DressLevel { get; } public ulong CurrentPower { get; } public ulong NextPower { get; } public Entry(uint id, ushort level, ulong currentPower, ulong nextPower) { DressId = id; DressLevel = level; CurrentPower = currentPower; NextPower = nextPower; } }
        public sealed class Snapshot { public byte Type { get; } public uint UsedDressId { get; } public int EnableCount => Entries.Count; public IReadOnlyList<Entry> Entries { get; } public Snapshot(byte type, uint usedDressId, List<Entry> entries) { Type = type; UsedDressId = usedDressId; Entries = (entries ?? new List<Entry>()).AsReadOnly(); } }
        public static readonly DressModel Instance = new DressModel(); private readonly Dictionary<byte, Snapshot> _byType = new Dictionary<byte, Snapshot>(); private readonly IReadOnlyDictionary<byte, Snapshot> _snapshots; private DressModel() { _snapshots = new ReadOnlyDictionary<byte, Snapshot>(_byType); }
        public bool HasData => _byType.Count > 0;
        public IReadOnlyDictionary<byte, Snapshot> Snapshots => _snapshots;
        public bool TryGet(byte type, out Snapshot snapshot) => _byType.TryGetValue(type, out snapshot);
        public void Replace(byte type, uint usedDressId, List<Entry> entries) { _byType[type] = new Snapshot(type, usedDressId, entries); }
        public void Reset() { _byType.Clear(); }
    }
}
