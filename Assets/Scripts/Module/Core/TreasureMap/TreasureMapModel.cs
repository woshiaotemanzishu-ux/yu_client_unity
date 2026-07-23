using System.Collections.Generic;

namespace Shenxiao.Module.Core.TreasureMap
{
    /// <summary>20303 服务端开奖记录快照；所有列表保持收到时的原序与重复项。</summary>
    public sealed class TreasureMapModel
    {
        public sealed class RewardEntry
        {
            public byte Style { get; }
            public uint TypeId { get; }
            public uint Count { get; }
            public RewardEntry(byte style, uint typeId, uint count) { Style = style; TypeId = typeId; Count = count; }
        }

        public sealed class DrawLogEntry
        {
            public uint ServerNum { get; }
            public long RoleId { get; }
            public string Name { get; }
            public IReadOnlyList<RewardEntry> Rewards { get; }
            public DrawLogEntry(uint serverNum, long roleId, string name, List<RewardEntry> rewards)
            {
                ServerNum = serverNum;
                RoleId = roleId;
                Name = name;
                Rewards = (rewards ?? new List<RewardEntry>()).AsReadOnly();
            }
        }

        public static readonly TreasureMapModel Instance = new TreasureMapModel();
        private readonly List<DrawLogEntry> _drawLogs = new List<DrawLogEntry>();
        private readonly IReadOnlyList<DrawLogEntry> _readOnlyDrawLogs;
        private TreasureMapModel() { _readOnlyDrawLogs = _drawLogs.AsReadOnly(); }

        public bool HasDrawLog { get; private set; }
        public IReadOnlyList<DrawLogEntry> DrawLogs => _readOnlyDrawLogs;

        public void ReplaceDrawLog(List<DrawLogEntry> drawLogs)
        {
            _drawLogs.Clear();
            if (drawLogs != null) _drawLogs.AddRange(drawLogs);
            HasDrawLog = true;
        }

        public void Reset()
        {
            _drawLogs.Clear();
            HasDrawLog = false;
        }
    }
}
