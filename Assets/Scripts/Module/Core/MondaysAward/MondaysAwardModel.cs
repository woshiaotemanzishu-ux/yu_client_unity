using System.Collections.Generic;

namespace Shenxiao.Module.Core.MondaysAward
{
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

        public static readonly MondaysAwardModel Instance = new MondaysAwardModel();

        private readonly List<TaskStateEntry> _taskStates = new List<TaskStateEntry>();
        private readonly IReadOnlyList<TaskStateEntry> _readOnlyTaskStates;
        private readonly List<RecordEntry> _records = new List<RecordEntry>();
        private readonly IReadOnlyList<RecordEntry> _readOnlyRecords;

        private MondaysAwardModel()
        {
            _readOnlyTaskStates = _taskStates.AsReadOnly();
            _readOnlyRecords = _records.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<TaskStateEntry> TaskStates => _readOnlyTaskStates;
        public bool HasRecords { get; private set; }
        public IReadOnlyList<RecordEntry> Records => _readOnlyRecords;

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

        public void Reset()
        {
            _taskStates.Clear();
            _records.Clear();
            HasData = false;
            HasRecords = false;
        }
    }
}
