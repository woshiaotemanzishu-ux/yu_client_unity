using System.Collections.Generic;

namespace Shenxiao.Module.Core.Armor
{
    /// <summary>14401 不朽圣骸基础快照；仅保存服务端数据，不启用 UI、红点或后续操作协议。</summary>
    public sealed class ArmorModel
    {
        public sealed class PositionEntry
        {
            public uint GTypeId { get; }
            public byte Position { get; }
            public byte Status { get; }
            public PositionEntry(uint gTypeId, byte position, byte status) { GTypeId = gTypeId; Position = position; Status = status; }
        }
        public sealed class TypeEntry
        {
            public byte Type { get; }
            public byte Status { get; }
            public IReadOnlyList<PositionEntry> Positions { get; }
            public TypeEntry(byte type, byte status, List<PositionEntry> positions)
            {
                Type = type;
                Status = status;
                positions = positions ?? new List<PositionEntry>();
                positions.Sort((a, b) => a.Position.CompareTo(b.Position));
                Positions = positions;
            }
        }
        public sealed class StageEntry
        {
            public byte Stage { get; }
            public IReadOnlyList<TypeEntry> Types { get; }
            public StageEntry(byte stage, List<TypeEntry> types)
            {
                Stage = stage;
                types = types ?? new List<TypeEntry>();
                types.Sort((a, b) => a.Type.CompareTo(b.Type));
                Types = types;
            }
        }

        public static readonly ArmorModel Instance = new ArmorModel();
        private readonly List<StageEntry> _stages = new List<StageEntry>();
        private ArmorModel() { }
        public IReadOnlyList<StageEntry> Stages => _stages;
        public bool HasData { get; private set; }

        /// <summary>14401 的每个包都是全量树，必须替换并允许空包清旧。</summary>
        public void ReplaceData(List<StageEntry> stages)
        {
            _stages.Clear();
            if (stages != null)
            {
                stages.Sort((a, b) => a.Stage.CompareTo(b.Stage));
                _stages.AddRange(stages);
            }
            HasData = true;
        }

        public void Reset()
        {
            _stages.Clear();
            HasData = false;
        }
    }
}
