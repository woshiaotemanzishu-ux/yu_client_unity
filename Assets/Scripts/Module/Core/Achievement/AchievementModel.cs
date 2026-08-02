using System.Collections.Generic;

namespace Shenxiao.Module.Core.Achievement
{
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

        public static readonly AchievementModel Instance = new AchievementModel();

        private readonly List<Reward> _rewards = new List<Reward>();
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<TypeStar> _types = new List<TypeStar>();
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

        public void ReplaceStage(byte stage, List<Reward> rewards, ushort next)
        {
            CurrentStage = stage;
            NewCurrentStage = next;
            _rewards.Clear();
            if (rewards != null) _rewards.AddRange(rewards);
            HasStageData = true;
        }

        public void ReplaceEntries(List<Entry> entries)
        {
            _entries.Clear();
            if (entries != null) _entries.AddRange(entries);
            HasEntriesData = true;
        }

        public void ApplyEntryUpdates(List<EntryUpdate> updates)
        {
            _entryUpdates = new List<EntryUpdate>(updates ?? new List<EntryUpdate>()).AsReadOnly();
            HasEntryUpdateData = true;
            if (!HasEntriesData || updates == null) return;

            foreach (EntryUpdate update in updates)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    Entry entry = _entries[i];
                    if (entry.Id != update.Id) continue;
                    _entries[i] = new Entry(entry.Category, entry.Id, update.Progress, update.Status);
                    break;
                }
            }
        }

        public void ReplaceStar(uint star)
        {
            Star = star;
            HasStarData = true;
        }

        public void ApplyStageRewardUpdate(List<Reward> updates, byte stage, ushort next)
        {
            LastStageRewardUpdate = new StageRewardUpdateSnapshot(updates, stage, next);
            HasStageRewardUpdateData = true;
            if (!HasStageData) return;

            CurrentStage = stage;
            NewCurrentStage = next;
            if (updates == null) return;
            foreach (Reward update in updates)
            {
                int index = -1;
                for (int i = 0; i < _rewards.Count; i++)
                {
                    if (_rewards[i].NeedStar == update.NeedStar)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0) _rewards[index] = update;
                else _rewards.Add(update);
            }
        }

        public void ReplaceTypes(List<TypeStar> types)
        {
            _types.Clear();
            if (types != null) _types.AddRange(types);
            HasTypesData = true;
        }

        public void Reset()
        {
            CurrentStage = 0;
            NewCurrentStage = 0;
            Star = 0;
            _rewards.Clear();
            _entries.Clear();
            _entryUpdates = new List<EntryUpdate>().AsReadOnly();
            _types.Clear();
            LastStageRewardUpdate = null;
            HasStageData = false;
            HasEntriesData = false;
            HasEntryUpdateData = false;
            HasStarData = false;
            HasStageRewardUpdateData = false;
            HasTypesData = false;
        }
    }
}
