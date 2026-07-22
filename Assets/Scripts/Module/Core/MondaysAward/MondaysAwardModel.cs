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

        public static readonly MondaysAwardModel Instance = new MondaysAwardModel();

        private readonly List<TaskStateEntry> _taskStates = new List<TaskStateEntry>();
        private readonly IReadOnlyList<TaskStateEntry> _readOnlyTaskStates;

        private MondaysAwardModel()
        {
            _readOnlyTaskStates = _taskStates.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public IReadOnlyList<TaskStateEntry> TaskStates => _readOnlyTaskStates;

        public void Replace(List<TaskStateEntry> taskStates)
        {
            _taskStates.Clear();
            if (taskStates != null)
            {
                _taskStates.AddRange(taskStates);
            }

            HasData = true;
        }

        public void Reset()
        {
            _taskStates.Clear();
            HasData = false;
        }
    }
}
