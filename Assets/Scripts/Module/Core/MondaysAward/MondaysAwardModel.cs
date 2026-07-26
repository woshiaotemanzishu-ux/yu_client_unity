using System.Collections.Generic;

namespace Shenxiao.Module.Core.MondaysAward
{
    /// <summary>周一嘉礼错误、任务、记录、抽奖状态与奖池的独立原始状态；不驱动 UI 或操作成功链。</summary>
    public sealed class MondaysAwardModel
    {
        public sealed class TaskStateEntry
        {
            public ushort TaskId { get; }
            public byte State { get; }

            public TaskStateEntry(ushort taskId, byte state)
            {
                TaskId = taskId;
                State = state;
            }
        }

        public sealed class RecordEntry
        {
            public uint ServerId { get; }
            public ushort ServerNum { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public byte Type { get; }
            public ushort PoolId { get; }
            public uint Utime { get; }
            public string Picture { get; }
            public uint PictureVer { get; }
            public ushort Career { get; }
            public ushort Turn { get; }

            public RecordEntry(uint serverId, ushort serverNum, ulong roleId, string roleName, byte type, ushort poolId, uint utime, string picture, uint pictureVer, ushort career, ushort turn)
            {
                ServerId = serverId;
                ServerNum = serverNum;
                RoleId = roleId;
                RoleName = roleName;
                Type = type;
                PoolId = poolId;
                Utime = utime;
                Picture = picture;
                PictureVer = pictureVer;
                Career = career;
                Turn = turn;
            }
        }

        public sealed class PoolEntry
        {
            private readonly IReadOnlyList<ushort> _rids;

            public ushort Id { get; }
            public IReadOnlyList<ushort> Rids => _rids;

            public PoolEntry(ushort id, List<ushort> rids)
            {
                Id = id;
                _rids = new List<ushort>(rids ?? new List<ushort>()).AsReadOnly();
            }
        }

        public static readonly MondaysAwardModel Instance = new MondaysAwardModel();

        private readonly List<TaskStateEntry> _taskStates = new List<TaskStateEntry>();
        private readonly IReadOnlyList<TaskStateEntry> _readOnlyTaskStates;
        private readonly List<RecordEntry> _records = new List<RecordEntry>();
        private readonly IReadOnlyList<RecordEntry> _readOnlyRecords;
        private readonly List<PoolEntry> _pools = new List<PoolEntry>();
        private readonly IReadOnlyList<PoolEntry> _readOnlyPools;

        private MondaysAwardModel()
        {
            _readOnlyTaskStates = _taskStates.AsReadOnly();
            _readOnlyRecords = _records.AsReadOnly();
            _readOnlyPools = _pools.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<TaskStateEntry> TaskStates => _readOnlyTaskStates;
        public bool HasRecords { get; private set; }
        public IReadOnlyList<RecordEntry> Records => _readOnlyRecords;
        public bool HasPools { get; private set; }
        public IReadOnlyList<PoolEntry> Pools => _readOnlyPools;
        public bool HasDrawState { get; private set; }
        public byte DrawStateCode { get; private set; }
        public bool IsDrawOpen => DrawStateCode == 1;
        public ushort DrawTimes { get; private set; }
        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }

        public void Replace(List<TaskStateEntry> taskStates)
        {
            _taskStates.Clear();
            if (taskStates != null)
            {
                _taskStates.AddRange(taskStates);
            }

            HasData = true;
        }

        public void ReplaceRecords(List<RecordEntry> records)
        {
            _records.Clear();
            if (records != null)
            {
                _records.AddRange(records);
            }

            HasRecords = true;
        }

        public void ReplacePools(List<PoolEntry> pools)
        {
            _pools.Clear();
            if (pools != null)
            {
                _pools.AddRange(pools);
            }

            HasPools = true;
        }

        public void ReplaceDrawState(byte code, ushort drawTimes)
        {
            DrawStateCode = code;
            DrawTimes = drawTimes;
            HasDrawState = true;
        }

        public void SetError(uint code)
        {
            LastErrorCode = code;
            HasError = true;
        }

        public void Reset()
        {
            _taskStates.Clear();
            _records.Clear();
            _pools.Clear();
            HasData = false;
            HasRecords = false;
            HasPools = false;
            HasDrawState = false;
            DrawStateCode = 0;
            DrawTimes = 0;
            HasError = false;
            LastErrorCode = 0;
        }
    }
}
