using System.Collections.Generic;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>称号权威列表和互不派生、互不清理的原始只读协议切片。</summary>
    public sealed class DesignationModel
    {
        public sealed class Entry
        {
            public uint Id { get; }
            public byte Order { get; }
            public uint EndTime { get; }

            public Entry(uint id, byte order, uint endTime)
            {
                Id = id;
                Order = order;
                EndTime = endTime;
            }
        }

        public sealed class ActivationSnapshot
        {
            public uint Code { get; }
            public uint Id { get; }
            public uint EndTime { get; }

            public ActivationSnapshot(uint code, uint id, uint endTime)
            {
                Code = code;
                Id = id;
                EndTime = endTime;
            }
        }

        public sealed class SceneNoticeSnapshot
        {
            public ulong PlayerId { get; }
            public uint Id { get; }

            public SceneNoticeSnapshot(ulong playerId, uint id)
            {
                PlayerId = playerId;
                Id = id;
            }
        }

        public sealed class PowerQuerySnapshot
        {
            public uint Code { get; }
            public uint Power { get; }

            public PowerQuerySnapshot(uint code, uint power)
            {
                Code = code;
                Power = power;
            }
        }

        public sealed class RemovalSnapshot
        {
            public uint Id { get; }

            public RemovalSnapshot(uint id) { Id = id; }
        }

        public static readonly DesignationModel Instance = new DesignationModel();

        private readonly List<Entry> _entries = new List<Entry>();

        private DesignationModel() { }

        public uint CurrentUsedId { get; private set; }
        public IReadOnlyList<Entry> Entries => _entries;
        public bool HasData { get; private set; }
        public ActivationSnapshot Activation { get; private set; }
        public SceneNoticeSnapshot SceneNotice { get; private set; }
        public PowerQuerySnapshot PowerQuery { get; private set; }
        public RemovalSnapshot Removal { get; private set; }

        public void ReplaceData(uint currentUsedId, List<Entry> entries)
        {
            CurrentUsedId = currentUsedId;
            _entries.Clear();
            if (entries != null) _entries.AddRange(entries);
            HasData = true;
        }

        public void ReplaceActivation(uint code, uint id, uint endTime)
            => Activation = new ActivationSnapshot(code, id, endTime);

        public void ReplaceSceneNotice(ulong playerId, uint id)
            => SceneNotice = new SceneNoticeSnapshot(playerId, id);

        public void ReplacePowerQuery(uint code, uint power)
            => PowerQuery = new PowerQuerySnapshot(code, power);

        public void ReplaceRemoval(uint id) => Removal = new RemovalSnapshot(id);

        public void ClearReadContinuationSnapshots()
        {
            Activation = null;
            SceneNotice = null;
            PowerQuery = null;
            Removal = null;
        }

        public void Reset()
        {
            CurrentUsedId = 0;
            _entries.Clear();
            HasData = false;
            ClearReadContinuationSnapshots();
        }
    }
}
