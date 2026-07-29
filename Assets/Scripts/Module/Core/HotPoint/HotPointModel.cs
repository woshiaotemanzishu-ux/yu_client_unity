using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.HotPoint
{
    /// <summary>嗨点 33300/33302/33303/33305 的互相隔离快照，以及 33306 原始错误码。</summary>
    public sealed class HotPointModel
    {
        private static readonly IReadOnlyList<ActivityInfo> EmptyActivities = Array.AsReadOnly(new ActivityInfo[0]);
        public static readonly HotPointModel Instance = new HotPointModel();
        private readonly Dictionary<uint, DetailSnapshot> _details = new Dictionary<uint, DetailSnapshot>();
        private readonly Dictionary<uint, RewardSnapshot> _rewards = new Dictionary<uint, RewardSnapshot>();
        private readonly Dictionary<uint, ProgressSnapshot> _progress = new Dictionary<uint, ProgressSnapshot>();
        private IReadOnlyList<ActivityInfo> _activities = EmptyActivities;

        private HotPointModel() { }

        public sealed class ActivityInfo
        {
            public readonly ushort BaseType;
            public readonly ushort SubType;
            public readonly string Name;
            public readonly uint StartTime;
            public readonly uint EndTime;
            public readonly uint ShowId;

            public ActivityInfo(ushort baseType, ushort subType, string name, uint startTime, uint endTime, uint showId)
            {
                BaseType = baseType;
                SubType = subType;
                Name = name ?? string.Empty;
                StartTime = startTime;
                EndTime = endTime;
                ShowId = showId;
            }
        }

        public sealed class DetailItem
        {
            public readonly uint ModuleId;
            public readonly uint SubId;
            public readonly string ConditionType;
            public readonly string Name;
            public readonly ushort OrderId;
            public readonly ushort JumpId;
            public readonly uint SecondaryValue;
            public readonly string IconType;
            public readonly ulong ProgressValue;
            public readonly ushort IsProgress;
            public readonly uint ConditionValue;
            public readonly uint RewardPoint;
            public readonly string Description;
            public readonly ushort IsComplete;

            public DetailItem(uint moduleId, uint subId, string conditionType, string name, ushort orderId,
                ushort jumpId, uint secondaryValue, string iconType, ulong progressValue, ushort isProgress,
                uint conditionValue, uint rewardPoint, string description, ushort isComplete)
            {
                ModuleId = moduleId;
                SubId = subId;
                ConditionType = conditionType ?? string.Empty;
                Name = name ?? string.Empty;
                OrderId = orderId;
                JumpId = jumpId;
                SecondaryValue = secondaryValue;
                IconType = iconType ?? string.Empty;
                ProgressValue = progressValue;
                IsProgress = isProgress;
                ConditionValue = conditionValue;
                RewardPoint = rewardPoint;
                Description = description ?? string.Empty;
                IsComplete = isComplete;
            }

            internal DetailItem WithProgress(ulong progressValue, ushort isComplete)
            {
                return new DetailItem(ModuleId, SubId, ConditionType, Name, OrderId, JumpId, SecondaryValue,
                    IconType, progressValue, IsProgress, ConditionValue, RewardPoint, Description, isComplete);
            }
        }

        public sealed class DetailSnapshot
        {
            public readonly ushort BaseType;
            public readonly ushort SubType;
            public readonly uint SumPoints;
            public readonly IReadOnlyList<DetailItem> Modules;

            public DetailSnapshot(ushort baseType, ushort subType, uint sumPoints, IList<DetailItem> modules)
            {
                BaseType = baseType;
                SubType = subType;
                SumPoints = sumPoints;
                Modules = Freeze(modules);
            }
        }

        public sealed class RewardItem
        {
            public readonly ushort Grade;
            public readonly byte FormType;
            public readonly byte Status;
            public readonly ushort ReceiveTimes;
            public readonly string Name;
            public readonly string Description;
            public readonly string Condition;
            public readonly string Reward;

            public RewardItem(ushort grade, byte formType, byte status, ushort receiveTimes, string name,
                string description, string condition, string reward)
            {
                Grade = grade;
                FormType = formType;
                Status = status;
                ReceiveTimes = receiveTimes;
                Name = name ?? string.Empty;
                Description = description ?? string.Empty;
                Condition = condition ?? string.Empty;
                Reward = reward ?? string.Empty;
            }
        }

        public sealed class RewardSnapshot
        {
            public readonly ushort BaseType;
            public readonly ushort SubType;
            public readonly IReadOnlyList<RewardItem> Rewards;

            public RewardSnapshot(ushort baseType, ushort subType, IList<RewardItem> rewards)
            {
                BaseType = baseType;
                SubType = subType;
                Rewards = Freeze(rewards);
            }
        }

        /// <summary>33305 的 Name 实际是条件描述，需与 33302 DetailItem.Description 对齐。</summary>
        public sealed class ProgressItem
        {
            public readonly uint ModuleId;
            public readonly uint SubId;
            public readonly string ConditionType;
            public readonly string Name;
            public readonly ulong ProgressValue;
            public readonly ushort IsComplete;

            public ProgressItem(uint moduleId, uint subId, string conditionType, string name,
                ulong progressValue, ushort isComplete)
            {
                ModuleId = moduleId;
                SubId = subId;
                ConditionType = conditionType ?? string.Empty;
                Name = name ?? string.Empty;
                ProgressValue = progressValue;
                IsComplete = isComplete;
            }
        }

        public sealed class ProgressSnapshot
        {
            public readonly ushort BaseType;
            public readonly ushort SubType;
            public readonly uint SumPoints;
            public readonly IReadOnlyList<ProgressItem> Modules;

            public ProgressSnapshot(ushort baseType, ushort subType, uint sumPoints, IList<ProgressItem> modules)
            {
                BaseType = baseType;
                SubType = subType;
                SumPoints = sumPoints;
                Modules = Freeze(modules);
            }
        }

        public bool HasActivities { get; private set; }
        public IReadOnlyList<ActivityInfo> Activities => _activities;
        public int DetailCount => _details.Count;
        public int RewardCount => _rewards.Count;
        public int ProgressCount => _progress.Count;
        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }

        public bool TryGetDetail(ushort baseType, ushort subType, out DetailSnapshot snapshot)
        {
            return _details.TryGetValue(Key(baseType, subType), out snapshot);
        }

        public bool TryGetReward(ushort baseType, ushort subType, out RewardSnapshot snapshot)
        {
            return _rewards.TryGetValue(Key(baseType, subType), out snapshot);
        }

        public bool TryGetProgress(ushort baseType, ushort subType, out ProgressSnapshot snapshot)
        {
            return _progress.TryGetValue(Key(baseType, subType), out snapshot);
        }

        public void ReplaceActivities(IList<ActivityInfo> activities)
        {
            _activities = Freeze(activities);
            HasActivities = true;
        }

        public void ReplaceDetail(ushort baseType, ushort subType, uint sumPoints, IList<DetailItem> modules)
        {
            _details[Key(baseType, subType)] = new DetailSnapshot(baseType, subType, sumPoints, modules);
        }

        public void ReplaceReward(ushort baseType, ushort subType, IList<RewardItem> rewards)
        {
            _rewards[Key(baseType, subType)] = new RewardSnapshot(baseType, subType, rewards);
        }

        /// <summary>
        /// 保存 33305 原始键控快照；若同键 33302 已加载，则按老端四字段键合并进度。
        /// 重复增量按 wire 顺序覆盖，最后一项生效；未加载明细时不凭空构造。
        /// </summary>
        public void ApplyProgress(ushort baseType, ushort subType, uint sumPoints, IList<ProgressItem> modules)
        {
            var progressSnapshot = new ProgressSnapshot(baseType, subType, sumPoints, modules);
            uint key = Key(baseType, subType);
            _progress[key] = progressSnapshot;
            if (!_details.TryGetValue(key, out DetailSnapshot detail)) return;

            var merged = new DetailItem[detail.Modules.Count];
            for (int i = 0; i < detail.Modules.Count; i++)
            {
                DetailItem current = detail.Modules[i];
                for (int j = 0; j < progressSnapshot.Modules.Count; j++)
                {
                    ProgressItem delta = progressSnapshot.Modules[j];
                    if (current.ModuleId == delta.ModuleId && current.SubId == delta.SubId
                        && string.Equals(current.ConditionType, delta.ConditionType, StringComparison.Ordinal)
                        && string.Equals(current.Description, delta.Name, StringComparison.Ordinal))
                    {
                        current = current.WithProgress(delta.ProgressValue, delta.IsComplete);
                    }
                }
                merged[i] = current;
            }
            _details[key] = new DetailSnapshot(baseType, subType, sumPoints, merged);
        }

        public void ReplaceError(uint errorCode)
        {
            HasError = true;
            LastErrorCode = errorCode;
        }

        public void Reset()
        {
            HasActivities = false;
            _activities = EmptyActivities;
            _details.Clear();
            _rewards.Clear();
            _progress.Clear();
            HasError = false;
            LastErrorCode = 0;
        }

        private static uint Key(ushort baseType, ushort subType)
        {
            return ((uint)baseType << 16) | subType;
        }

        private static IReadOnlyList<T> Freeze<T>(IList<T> source)
        {
            if (source == null || source.Count == 0) return Array.AsReadOnly(new T[0]);
            var copy = new T[source.Count];
            source.CopyTo(copy, 0);
            return Array.AsReadOnly(copy);
        }
    }
}
