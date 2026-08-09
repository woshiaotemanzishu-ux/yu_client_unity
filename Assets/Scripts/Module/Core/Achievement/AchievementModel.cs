using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Achievement
{
    /// <summary>
    /// 成就权威运行态。40903 保存每个 category 当前应展示的条目；40909 保存指定 category 的完整链，
    /// 两者不能互相冒充。所有写事务只在服务端回包/推送后落状态，View 不做乐观修改。
    /// </summary>
    public sealed class AchievementModel
    {
        public sealed class Reward
        {
            public uint NeedStar { get; }
            public byte Status { get; }

            public Reward(uint star, byte status)
            {
                NeedStar = star;
                Status = status;
            }
        }

        public sealed class Entry
        {
            public byte Category { get; }
            public uint Id { get; }
            public ulong Progress { get; }
            public byte Status { get; }

            public Entry(byte category, uint id, ulong progress, byte status)
            {
                Category = category;
                Id = id;
                Progress = progress;
                Status = status;
            }
        }

        public sealed class EntryUpdate
        {
            public uint Id { get; }
            public byte Status { get; }
            public ulong Progress { get; }

            public EntryUpdate(uint id, byte status, ulong progress)
            {
                Id = id;
                Status = status;
                Progress = progress;
            }
        }

        public sealed class StageRewardUpdateSnapshot
        {
            public IReadOnlyList<Reward> Rewards { get; }
            public byte CurrentStage { get; }
            public ushort NewCurrentStage { get; }

            public StageRewardUpdateSnapshot(List<Reward> rewards, byte currentStage, ushort newCurrentStage)
            {
                Rewards = new List<Reward>(rewards ?? new List<Reward>()).AsReadOnly();
                CurrentStage = currentStage;
                NewCurrentStage = newCurrentStage;
            }
        }

        public sealed class TypeStar
        {
            public ushort Type { get; }
            public uint TotalStar { get; }
            public uint NowStar { get; }

            public TypeStar(ushort type, uint total, uint now)
            {
                Type = type;
                TotalStar = total;
                NowStar = now;
            }
        }

        public enum OperationKind
        {
            StageClaim,
            EntryClaim,
        }

        public sealed class OperationResult
        {
            public OperationKind Kind { get; }
            public uint TargetId { get; }
            public bool Success { get; }
            public uint ErrorCode { get; }

            public OperationResult(OperationKind kind, uint targetId, bool success, uint errorCode)
            {
                Kind = kind;
                TargetId = targetId;
                Success = success;
                ErrorCode = errorCode;
            }
        }

        public static readonly AchievementModel Instance = new AchievementModel();

        private readonly List<Reward> _rewards = new List<Reward>();
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<TypeStar> _types = new List<TypeStar>();
        private readonly Dictionary<byte, IReadOnlyList<Entry>> _categoryEntries =
            new Dictionary<byte, IReadOnlyList<Entry>>();
        private readonly IReadOnlyList<Reward> _roRewards;
        private readonly IReadOnlyList<Entry> _roEntries;
        private readonly IReadOnlyList<TypeStar> _roTypes;
        private IReadOnlyList<EntryUpdate> _entryUpdates = new List<EntryUpdate>().AsReadOnly();

        private AchievementModel()
        {
            _roRewards = _rewards.AsReadOnly();
            _roEntries = _entries.AsReadOnly();
            _roTypes = _types.AsReadOnly();
        }

        public event Action Changed;
        public event Action<OperationResult> OperationCompleted;

        public bool HasStageData { get; private set; }
        public bool HasEntriesData { get; private set; }
        public bool HasEntryUpdateData { get; private set; }
        public bool HasStarData { get; private set; }
        public bool HasStageRewardUpdateData { get; private set; }
        public bool HasTypesData { get; private set; }
        public bool HasAllStartupData => HasStageData && HasEntriesData && HasStarData && HasTypesData;
        public byte CurrentStage { get; private set; }
        public ushort NewCurrentStage { get; private set; }
        public uint Star { get; private set; }
        public IReadOnlyList<Reward> Rewards => _roRewards;
        public IReadOnlyList<Entry> Entries => _roEntries;
        public IReadOnlyList<EntryUpdate> EntryUpdates => _entryUpdates;
        public StageRewardUpdateSnapshot LastStageRewardUpdate { get; private set; }
        public IReadOnlyList<TypeStar> Types => _roTypes;
        public IReadOnlyDictionary<byte, IReadOnlyList<Entry>> CategoryEntries => _categoryEntries;

        public bool TryGetCategory(byte category, out IReadOnlyList<Entry> entries)
            => _categoryEntries.TryGetValue(category, out entries);

        public void ReplaceStage(byte stage, List<Reward> rewards, ushort next)
        {
            CurrentStage = stage;
            NewCurrentStage = next;
            _rewards.Clear();
            if (rewards != null) _rewards.AddRange(rewards);
            HasStageData = true;
            RaiseChanged();
        }

        public void ReplaceEntries(List<Entry> entries)
        {
            _entries.Clear();
            if (entries != null) _entries.AddRange(entries);
            HasEntriesData = true;
            RaiseChanged();
        }

        public void ReplaceCategory(byte category, List<Entry> entries)
        {
            _categoryEntries[category] = new List<Entry>(entries ?? new List<Entry>()).AsReadOnly();
            RaiseChanged();
        }

        public void ApplyEntryUpdates(List<EntryUpdate> updates)
        {
            _entryUpdates = new List<EntryUpdate>(updates ?? new List<EntryUpdate>()).AsReadOnly();
            HasEntryUpdateData = true;
            if (updates != null)
            {
                MergeUpdates(_entries, updates);
                var keys = new List<byte>(_categoryEntries.Keys);
                foreach (byte key in keys)
                {
                    var list = new List<Entry>(_categoryEntries[key]);
                    if (MergeUpdates(list, updates)) _categoryEntries[key] = list.AsReadOnly();
                }
            }
            RaiseChanged();
        }

        public void ReplaceStar(uint star)
        {
            Star = star;
            HasStarData = true;
            RaiseChanged();
        }

        public void ApplyStageRewardUpdate(List<Reward> updates, byte stage, ushort next)
        {
            LastStageRewardUpdate = new StageRewardUpdateSnapshot(updates, stage, next);
            HasStageRewardUpdateData = true;
            if (HasStageData)
            {
                CurrentStage = stage;
                NewCurrentStage = next;
                if (updates != null)
                {
                    foreach (Reward update in updates)
                    {
                        int index = _rewards.FindIndex(v => v.NeedStar == update.NeedStar);
                        if (index >= 0) _rewards[index] = update;
                        else _rewards.Add(update);
                    }
                }
            }
            RaiseChanged();
        }

        public void ReplaceTypes(List<TypeStar> types)
        {
            _types.Clear();
            if (types != null) _types.AddRange(types);
            HasTypesData = true;
            RaiseChanged();
        }

        public void NotifyOperation(OperationKind kind, uint targetId, bool success, uint errorCode)
            => OperationCompleted?.Invoke(new OperationResult(kind, targetId, success, errorCode));

        internal void NotifyTransactionGateChanged() => RaiseChanged();

        public void Reset()
        {
            CurrentStage = 0;
            NewCurrentStage = 0;
            Star = 0;
            _rewards.Clear();
            _entries.Clear();
            _entryUpdates = new List<EntryUpdate>().AsReadOnly();
            _types.Clear();
            _categoryEntries.Clear();
            LastStageRewardUpdate = null;
            HasStageData = false;
            HasEntriesData = false;
            HasEntryUpdateData = false;
            HasStarData = false;
            HasStageRewardUpdateData = false;
            HasTypesData = false;
            RaiseChanged();
        }

        private static bool MergeUpdates(List<Entry> entries, IReadOnlyList<EntryUpdate> updates)
        {
            bool changed = false;
            for (int u = 0; u < updates.Count; u++)
            {
                EntryUpdate update = updates[u];
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry entry = entries[i];
                    if (entry.Id != update.Id) continue;
                    entries[i] = new Entry(entry.Category, entry.Id, update.Progress, update.Status);
                    changed = true;
                    break;
                }
            }
            return changed;
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
