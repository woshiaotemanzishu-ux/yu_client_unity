using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>11200 装扮权威快照，以及 11201/02/03/05 的权威增量结果。</summary>
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

        public sealed class InactivePowerSnapshot
        {
            public byte Type { get; }
            public uint DressId { get; }
            public ulong ActivePower { get; }

            public InactivePowerSnapshot(byte type, uint dressId, ulong activePower)
            {
                Type = type;
                DressId = dressId;
                ActivePower = activePower;
            }
        }

        public static readonly DressModel Instance = new DressModel();
        private readonly Dictionary<byte, Snapshot> _byType = new Dictionary<byte, Snapshot>();
        private readonly IReadOnlyDictionary<byte, Snapshot> _snapshots;
        private readonly Dictionary<ulong, InactivePowerSnapshot> _inactivePowerByKey = new Dictionary<ulong, InactivePowerSnapshot>();
        private readonly IReadOnlyDictionary<ulong, InactivePowerSnapshot> _inactivePowerSnapshots;

        private DressModel()
        {
            _snapshots = new ReadOnlyDictionary<byte, Snapshot>(_byType);
            _inactivePowerSnapshots = new ReadOnlyDictionary<ulong, InactivePowerSnapshot>(_inactivePowerByKey);
        }

        public event Action<byte> Changed;

        public bool HasData => _byType.Count > 0;
        public IReadOnlyDictionary<byte, Snapshot> Snapshots => _snapshots;
        public bool HasInactivePowerData => _inactivePowerByKey.Count > 0;
        public IReadOnlyDictionary<ulong, InactivePowerSnapshot> InactivePowerSnapshots => _inactivePowerSnapshots;
        public bool TryGet(byte type, out Snapshot snapshot) => _byType.TryGetValue(type, out snapshot);
        public bool TryGetInactivePower(byte type, uint dressId, out InactivePowerSnapshot snapshot)
            => _inactivePowerByKey.TryGetValue(ToInactivePowerKey(type, dressId), out snapshot);

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

        public void ApplyActivation(byte type, uint dressId, ushort level, ulong currentPower, ulong nextPower)
        {
            uint used = _byType.TryGetValue(type, out Snapshot snapshot) ? snapshot.UsedDressId : 0;
            var entries = snapshot != null ? new List<Entry>(snapshot.Entries) : new List<Entry>();
            int index = entries.FindIndex(item => item.DressId == dressId);
            var next = new Entry(dressId, level, currentPower, nextPower);
            if (index >= 0) entries[index] = next;
            else entries.Add(next);
            _byType[type] = new Snapshot(type, used, entries);
            _inactivePowerByKey.Remove(ToInactivePowerKey(type, dressId));
            Changed?.Invoke(type);
        }

        public void ApplyUsed(byte type, uint dressId)
        {
            List<Entry> entries = _byType.TryGetValue(type, out Snapshot snapshot)
                ? new List<Entry>(snapshot.Entries)
                : new List<Entry>();
            _byType[type] = new Snapshot(type, dressId, entries);
            Changed?.Invoke(type);
        }

        public void ReplaceInactivePower(byte type, uint dressId, ulong activePower)
        {
            _inactivePowerByKey[ToInactivePowerKey(type, dressId)] = new InactivePowerSnapshot(type, dressId, activePower);
            Changed?.Invoke(type);
        }

        public void Reset()
        {
            _byType.Clear();
            _inactivePowerByKey.Clear();
        }

        private static ulong ToInactivePowerKey(byte type, uint dressId) => ((ulong)type << 32) | dressId;
    }
}
