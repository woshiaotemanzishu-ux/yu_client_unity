using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.TopVip
{
    /// <summary>至尊VIP 451xx 原始协议状态；列表保持服务端 wire 顺序与重复项。</summary>
    public sealed class TopVipModel
    {
        public sealed class RightEntry
        {
            public byte RightType { get; }
            public string Data { get; }
            public uint UpdateTime { get; }

            public RightEntry(byte rightType, string data, uint updateTime)
            {
                RightType = rightType;
                Data = data;
                UpdateTime = updateTime;
            }
        }

        public sealed class InfoSnapshot
        {
            public byte SupvipType { get; }
            public uint SupvipTime { get; }
            public IReadOnlyList<RightEntry> Rights { get; }
            public byte ChargeDay { get; }
            public uint TodayGold { get; }
            public byte IsFreeProtect { get; }

            public InfoSnapshot(
                byte supvipType,
                uint supvipTime,
                IReadOnlyList<RightEntry> rights,
                byte chargeDay,
                uint todayGold,
                byte isFreeProtect)
            {
                SupvipType = supvipType;
                SupvipTime = supvipTime;
                Rights = Freeze(rights);
                ChargeDay = chargeDay;
                TodayGold = todayGold;
                IsFreeProtect = isFreeProtect;
            }
        }

        public sealed class TaskEntry
        {
            public ushort TaskId { get; }
            public byte IsFinish { get; }
            public byte IsCommit { get; }
            public string Content { get; }

            public TaskEntry(ushort taskId, byte isFinish, byte isCommit, string content)
            {
                TaskId = taskId;
                IsFinish = isFinish;
                IsCommit = isCommit;
                Content = content;
            }
        }

        public sealed class SkillTaskSnapshot
        {
            public byte Stage { get; }
            public byte SubStage { get; }
            public IReadOnlyList<TaskEntry> Tasks { get; }

            public SkillTaskSnapshot(byte stage, byte subStage, IReadOnlyList<TaskEntry> tasks)
            {
                Stage = stage;
                SubStage = subStage;
                Tasks = Freeze(tasks);
            }
        }

        public sealed class TaskListSnapshot
        {
            public IReadOnlyList<TaskEntry> Tasks { get; }

            public TaskListSnapshot(IReadOnlyList<TaskEntry> tasks)
            {
                Tasks = Freeze(tasks);
            }
        }

        public static readonly TopVipModel Instance = new TopVipModel();

        private TopVipModel() { }

        public const string ICON_TYPE = "451";
        public const int RequireVipFlag = 4;
        public const int RequireLevel = 160;

        public InfoSnapshot Info { get; private set; }
        public SkillTaskSnapshot SkillTasks { get; private set; }
        public TaskListSnapshot CurrencyTasks { get; private set; }
        public TaskListSnapshot LastSkillTaskUpdate { get; private set; }
        public TaskListSnapshot LastCurrencyTaskUpdate { get; private set; }
        public bool HasFreeProtectUpdate { get; private set; }
        public byte FreeProtectUpdate { get; private set; }

        public bool HasInfo => Info != null;
        public bool HasSkillTasks => SkillTasks != null;
        public bool HasCurrencyTasks => CurrencyTasks != null;
        public bool HasSkillTaskUpdate => LastSkillTaskUpdate != null;
        public bool HasCurrencyTaskUpdate => LastCurrencyTaskUpdate != null;

        public void ReplaceInfo(
            byte supvipType,
            uint supvipTime,
            IReadOnlyList<RightEntry> rights,
            byte chargeDay,
            uint todayGold,
            byte isFreeProtect)
        {
            Info = new InfoSnapshot(supvipType, supvipTime, rights, chargeDay, todayGold, isFreeProtect);
        }

        public void ReplaceSkillTasks(byte stage, byte subStage, IReadOnlyList<TaskEntry> tasks)
        {
            SkillTasks = new SkillTaskSnapshot(stage, subStage, tasks);
        }

        public void ReplaceCurrencyTasks(IReadOnlyList<TaskEntry> tasks)
        {
            CurrencyTasks = new TaskListSnapshot(tasks);
        }

        public void ReplaceSkillTaskUpdate(IReadOnlyList<TaskEntry> tasks)
        {
            LastSkillTaskUpdate = new TaskListSnapshot(tasks);
        }

        public void ReplaceCurrencyTaskUpdate(IReadOnlyList<TaskEntry> tasks)
        {
            LastCurrencyTaskUpdate = new TaskListSnapshot(tasks);
        }

        public void ReplaceFreeProtectUpdate(byte isFree)
        {
            HasFreeProtectUpdate = true;
            FreeProtectUpdate = isFree;
        }

        public bool GetEntranceOpenState(int vipFlag, int level)
        {
            return vipFlag >= RequireVipFlag && level >= RequireLevel;
        }

        public void Reset()
        {
            Info = null;
            SkillTasks = null;
            CurrencyTasks = null;
            LastSkillTaskUpdate = null;
            LastCurrencyTaskUpdate = null;
            HasFreeProtectUpdate = false;
            FreeProtectUpdate = 0;
        }

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }
}
