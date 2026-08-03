using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>11200 装扮权威快照。仅承载读模型，不推导激活、穿戴或升级结果。</summary>
    public sealed class DressModel
    {
        public sealed class Entry
        {
            public uint DressId { get; }
            public ushort DressLevel { get; }
            public ulong CurrentPower { get; }
            public ulong NextPower { get; }

            public Entry(uint id, ushort level, ulong currentPower, ulong nextPower)
            {
                DressId = id;
                DressLevel = level;
                CurrentPower = currentPower;
                NextPower = nextPower;
            }
        }

        public sealed class Snapshot
        {
            public byte Type { get; }
            public uint UsedDressId { get; }
            public int EnableCount => Entries.Count;
            public IReadOnlyList<Entry> Entries { get; }

            public Snapshot(byte type, uint usedDressId, List<Entry> entries)
            {
                Type = type;
                UsedDressId = usedDressId;
                Entries = (entries ?? new List<Entry>()).AsReadOnly();
            }
        }

        public static readonly DressModel Instance = new DressModel();
        private readonly Dictionary<byte, Snapshot> _byType = new Dictionary<byte, Snapshot>();
        private readonly IReadOnlyDictionary<byte, Snapshot> _snapshots;

        private DressModel()
        {
            _snapshots = new ReadOnlyDictionary<byte, Snapshot>(_byType);
        }

        public event Action<byte> Changed;

        public bool HasData => _byType.Count > 0;
        public IReadOnlyDictionary<byte, Snapshot> Snapshots => _snapshots;
        public bool TryGet(byte type, out Snapshot snapshot) => _byType.TryGetValue(type, out snapshot);

        public bool TryGetEntry(byte type, uint id, out Entry entry)
        {
            entry = null;
            if (!_byType.TryGetValue(type, out Snapshot snapshot)) return false;
            for (int i = 0; i < snapshot.Entries.Count; i++)
            {
                if (snapshot.Entries[i].DressId != id) continue;
                entry = snapshot.Entries[i];
                return true;
            }
            return false;
        }

        public bool IsActive(byte type, uint id) => TryGetEntry(type, id, out _);

        public void Replace(byte type, uint usedDressId, List<Entry> entries)
        {
            _byType[type] = new Snapshot(type, usedDressId, entries);
            Changed?.Invoke(type);
        }

        public void Reset()
        {
            _byType.Clear();
        }
    }
}
