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

        public static readonly DepositModel Instance = new DepositModel();
        private readonly List<ActivityEntry> _activities = new List<ActivityEntry>();
        private readonly IReadOnlyList<ActivityEntry> _readOnlyActivities;

        private DepositModel()
        {
            _readOnlyActivities = _activities.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public uint DayCoin { get; private set; }
        public uint OnhookCoin { get; private set; }
        public IReadOnlyList<ActivityEntry> Activities => _readOnlyActivities;

        public void Replace(uint dayCoin, uint onhookCoin, List<ActivityEntry> activities)
        {
            DayCoin = dayCoin;
            OnhookCoin = onhookCoin;
            _activities.Clear();
            if (activities != null) _activities.AddRange(activities);
            HasData = true;
        }

        public void Reset()
        {
            DayCoin = 0;
            OnhookCoin = 0;
            _activities.Clear();
            HasData = false;
        }
    }
}
