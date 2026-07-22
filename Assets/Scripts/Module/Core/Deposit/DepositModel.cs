using System.Collections.Generic;

namespace Shenxiao.Module.Core.Deposit
{
    public sealed class DepositModel
    {
        public sealed class BehaviourEntry
        {
            public ushort BehaviourId { get; }
            public uint SelectTime { get; }
            public ushort Times { get; }

            public BehaviourEntry(ushort behaviourId, uint selectTime, ushort times)
            {
                BehaviourId = behaviourId;
                SelectTime = selectTime;
                Times = times;
            }
        }

        public sealed class ActivityEntry
        {
            public ushort ModuleId { get; }
            public ushort SubModule { get; }
            public uint SelectTime { get; }
            public IReadOnlyList<BehaviourEntry> Behaviours { get; }

            public ActivityEntry(ushort moduleId, ushort subModule, uint selectTime, List<BehaviourEntry> behaviours)
            {
                ModuleId = moduleId;
                SubModule = subModule;
                SelectTime = selectTime;
                Behaviours = (behaviours ?? new List<BehaviourEntry>()).AsReadOnly();
            }
        }
        public sealed class RecordEntry
        {
            public ushort ModuleId { get; }
            public ushort SubModule { get; }
            public uint OnhookTime { get; }
            public uint Result { get; }
            public ushort CostCoin { get; }
            public uint Time { get; }
            public RecordEntry(ushort moduleId, ushort subModule, uint onhookTime, uint result, ushort costCoin, uint time)
            {
                ModuleId = moduleId;
                SubModule = subModule;
                OnhookTime = onhookTime;
                Result = result;
                CostCoin = costCoin;
                Time = time;
            }
        }

        public static readonly DepositModel Instance = new DepositModel();
        private readonly List<ActivityEntry> _activities = new List<ActivityEntry>();
        private readonly IReadOnlyList<ActivityEntry> _readOnlyActivities;
        private readonly List<RecordEntry> _records = new List<RecordEntry>();
        private readonly IReadOnlyList<RecordEntry> _readOnlyRecords;

        private DepositModel()
        {
            _readOnlyActivities = _activities.AsReadOnly();
            _readOnlyRecords = _records.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public bool HasCoins { get; private set; }
        public bool HasRecords { get; private set; }
        public uint DayCoin { get; private set; }
        public uint OnhookCoin { get; private set; }
        public IReadOnlyList<ActivityEntry> Activities => _readOnlyActivities;
        public IReadOnlyList<RecordEntry> Records => _readOnlyRecords;

        public void Replace(uint dayCoin, uint onhookCoin, List<ActivityEntry> activities)
        {
            DayCoin = dayCoin;
            OnhookCoin = onhookCoin;
            _activities.Clear();
            if (activities != null) _activities.AddRange(activities);
            HasData = true;
            HasCoins = true;
        }

        public void ReplaceCoins(uint dayCoin, uint onhookCoin)
        {
            DayCoin = dayCoin;
            OnhookCoin = onhookCoin;
            HasCoins = true;
        }
        public void ReplaceRecords(List<RecordEntry> records)
        {
            _records.Clear();
            if (records != null) _records.AddRange(records);
            HasRecords = true;
        }

        public void Reset()
        {
            DayCoin = 0;
            OnhookCoin = 0;
            _activities.Clear();
            _records.Clear();
            HasData = false;
            HasCoins = false;
            HasRecords = false;
        }
    }
}
