using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.RuneTreasure
{
    /// <summary>寻宝416家族的原始读侧状态；查询快照按服务端回显键隔离保存。</summary>
    public sealed class RuneTreasureModel
    {
        public readonly struct PageKey : IEquatable<PageKey>
        {
            public byte HuntType { get; }
            public byte RecordType { get; }

            public PageKey(byte huntType, byte recordType)
            {
                HuntType = huntType;
                RecordType = recordType;
            }

            public bool Equals(PageKey other) =>
                HuntType == other.HuntType && RecordType == other.RecordType;
            public override bool Equals(object obj) => obj is PageKey other && Equals(other);
            public override int GetHashCode() => (HuntType << 8) | RecordType;
        }

        public sealed class ErrorSnapshot
        {
            public uint Code { get; }
            public ErrorSnapshot(uint code) => Code = code;
        }

        public sealed class StageReward
        {
            public ushort Stage { get; }
            public ushort Status { get; }

            public StageReward(ushort stage, ushort status)
            {
                Stage = stage;
                Status = status;
            }
        }

        public sealed class RuneSnapshot
        {
            public uint DrawTimes { get; }
            public ushort Turn { get; }
            public IReadOnlyList<StageReward> StageRewards { get; }
            public ulong StageRefreshTime { get; }
            public ulong FreeTime { get; }

            public RuneSnapshot(uint drawTimes, ushort turn, IReadOnlyList<StageReward> stageRewards,
                ulong stageRefreshTime, ulong freeTime)
            {
                DrawTimes = drawTimes;
                Turn = turn;
                StageRewards = Freeze(stageRewards);
                StageRefreshTime = stageRefreshTime;
                FreeTime = freeTime;
            }
        }

        public sealed class Record
        {
            public ulong RoleId { get; }
            public string RoleName { get; }
            public byte HuntType { get; }
            public uint GoodsTypeId { get; }
            public uint GoodsNum { get; }
            public uint Time { get; }
            public byte IsRare { get; }

            public Record(ulong roleId, string roleName, byte huntType, uint goodsTypeId,
                uint goodsNum, uint time, byte isRare)
            {
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                HuntType = huntType;
                GoodsTypeId = goodsTypeId;
                GoodsNum = goodsNum;
                Time = time;
                IsRare = isRare;
            }
        }

        public sealed class RecordPushSnapshot
        {
            public byte RecordType { get; }
            public ulong RoleId { get; }
            public IReadOnlyList<Record> Records { get; }

            public RecordPushSnapshot(byte recordType, ulong roleId, IReadOnlyList<Record> records)
            {
                RecordType = recordType;
                RoleId = roleId;
                Records = Freeze(records);
            }
        }

        public sealed class PageSnapshot
        {
            public uint Score { get; }
            public byte HuntType { get; }
            public byte DrawWeapon { get; }
            public byte RecordType { get; }
            public byte FreeTimes { get; }
            public ulong FreeTime { get; }
            public IReadOnlyList<Record> Records { get; }

            public PageSnapshot(uint score, byte huntType, byte drawWeapon, byte recordType,
                byte freeTimes, ulong freeTime, IReadOnlyList<Record> records)
            {
                Score = score;
                HuntType = huntType;
                DrawWeapon = drawWeapon;
                RecordType = recordType;
                FreeTimes = freeTimes;
                FreeTime = freeTime;
                Records = Freeze(records);
            }
        }

        public sealed class LuckySnapshot
        {
            public byte HuntType { get; }
            public uint Value { get; }
            public ushort Percent { get; }

            public LuckySnapshot(byte huntType, uint value, ushort percent)
            {
                HuntType = huntType;
                Value = value;
                Percent = percent;
            }
        }

        public sealed class CrossRecord
        {
            public uint ServerId { get; }
            public uint ServerNum { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public byte HuntType { get; }
            public uint GoodsTypeId { get; }
            public ushort GoodsNum { get; }
            public uint Time { get; }
            public byte IsRare { get; }

            public CrossRecord(uint serverId, uint serverNum, ulong roleId, string roleName,
                byte huntType, uint goodsTypeId, ushort goodsNum, uint time, byte isRare)
            {
                ServerId = serverId;
                ServerNum = serverNum;
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                HuntType = huntType;
                GoodsTypeId = goodsTypeId;
                GoodsNum = goodsNum;
                Time = time;
                IsRare = isRare;
            }
        }

        public sealed class CrossRecordSnapshot
        {
            public byte HuntType { get; }
            public IReadOnlyList<CrossRecord> Records { get; }

            public CrossRecordSnapshot(byte huntType, IReadOnlyList<CrossRecord> records)
            {
                HuntType = huntType;
                Records = Freeze(records);
            }
        }

        public sealed class OpenStateSnapshot
        {
            public byte HuntType { get; }
            public byte RawOpen { get; }
            public bool IsOpen => RawOpen == 1;

            public OpenStateSnapshot(byte huntType, byte rawOpen)
            {
                HuntType = huntType;
                RawOpen = rawOpen;
            }
        }

        public sealed class WeaponNoticeSnapshot
        {
            public byte HuntType { get; }
            public WeaponNoticeSnapshot(byte huntType) => HuntType = huntType;
        }

        public sealed class TaskItem
        {
            public uint TaskId { get; }
            public uint Num { get; }
            public byte State { get; }

            public TaskItem(uint taskId, uint num, byte state)
            {
                TaskId = taskId;
                Num = num;
                State = state;
            }
        }

        public sealed class TaskSnapshot
        {
            public uint Code { get; }
            public byte HuntType { get; }
            public IReadOnlyList<TaskItem> Tasks { get; }

            public TaskSnapshot(uint code, byte huntType, IReadOnlyList<TaskItem> tasks)
            {
                Code = code;
                HuntType = huntType;
                Tasks = Freeze(tasks);
            }
        }

        public sealed class TaskDeltaSnapshot
        {
            public byte HuntType { get; }
            public IReadOnlyList<TaskItem> Tasks { get; }

            public TaskDeltaSnapshot(byte huntType, IReadOnlyList<TaskItem> tasks)
            {
                HuntType = huntType;
                Tasks = Freeze(tasks);
            }
        }

        public static readonly RuneTreasureModel Instance = new RuneTreasureModel();

        private readonly Dictionary<PageKey, PageSnapshot> _pages =
            new Dictionary<PageKey, PageSnapshot>();
        private readonly Dictionary<byte, byte> _latestDrawWeapon = new Dictionary<byte, byte>();
        private readonly Dictionary<byte, LuckySnapshot> _luckies =
            new Dictionary<byte, LuckySnapshot>();
        private readonly Dictionary<byte, CrossRecordSnapshot> _crossRecords =
            new Dictionary<byte, CrossRecordSnapshot>();
        private readonly Dictionary<byte, OpenStateSnapshot> _openStates =
            new Dictionary<byte, OpenStateSnapshot>();
        private readonly Dictionary<byte, TaskSnapshot> _tasks =
            new Dictionary<byte, TaskSnapshot>();
        private readonly IReadOnlyDictionary<PageKey, PageSnapshot> _pagesView;
        private readonly IReadOnlyDictionary<byte, LuckySnapshot> _luckiesView;
        private readonly IReadOnlyDictionary<byte, CrossRecordSnapshot> _crossRecordsView;
        private readonly IReadOnlyDictionary<byte, OpenStateSnapshot> _openStatesView;
        private readonly IReadOnlyDictionary<byte, TaskSnapshot> _tasksView;

        private RuneTreasureModel()
        {
            _pagesView = new ReadOnlyDictionary<PageKey, PageSnapshot>(_pages);
            _luckiesView = new ReadOnlyDictionary<byte, LuckySnapshot>(_luckies);
            _crossRecordsView = new ReadOnlyDictionary<byte, CrossRecordSnapshot>(_crossRecords);
            _openStatesView = new ReadOnlyDictionary<byte, OpenStateSnapshot>(_openStates);
            _tasksView = new ReadOnlyDictionary<byte, TaskSnapshot>(_tasks);
        }

        public ErrorSnapshot LastError { get; private set; }
        public RuneSnapshot Rune { get; private set; }
        public RecordPushSnapshot LastRecordPush { get; private set; }
        public WeaponNoticeSnapshot LastWeaponNotice { get; private set; }
        public TaskDeltaSnapshot LastTaskDelta { get; private set; }
        public IReadOnlyDictionary<PageKey, PageSnapshot> Pages => _pagesView;
        public IReadOnlyDictionary<byte, LuckySnapshot> Luckies => _luckiesView;
        public IReadOnlyDictionary<byte, CrossRecordSnapshot> CrossRecords => _crossRecordsView;
        public IReadOnlyDictionary<byte, OpenStateSnapshot> OpenStates => _openStatesView;
        public IReadOnlyDictionary<byte, TaskSnapshot> Tasks => _tasksView;
        public bool HasError => LastError != null;
        public bool HasRune => Rune != null;
        public bool HasRecordPush => LastRecordPush != null;
        public bool HasWeaponNotice => LastWeaponNotice != null;
        public bool HasTaskDelta => LastTaskDelta != null;

        public void ReplaceError(uint code) => LastError = new ErrorSnapshot(code);
        public void ReplaceRune(RuneSnapshot snapshot) => Rune = snapshot;
        public void ReplaceRecordPush(RecordPushSnapshot snapshot) => LastRecordPush = snapshot;

        public void ReplacePage(PageSnapshot snapshot)
        {
            _pages[new PageKey(snapshot.HuntType, snapshot.RecordType)] = snapshot;
            _latestDrawWeapon[snapshot.HuntType] = snapshot.DrawWeapon;
        }

        public void ReplaceLucky(LuckySnapshot snapshot) => _luckies[snapshot.HuntType] = snapshot;
        public void ReplaceCrossRecords(CrossRecordSnapshot snapshot) =>
            _crossRecords[snapshot.HuntType] = snapshot;
        public void ReplaceOpenState(OpenStateSnapshot snapshot) =>
            _openStates[snapshot.HuntType] = snapshot;
        public void ReplaceWeaponNotice(byte huntType) =>
            LastWeaponNotice = new WeaponNoticeSnapshot(huntType);
        public void ReplaceTasks(TaskSnapshot snapshot) => _tasks[snapshot.HuntType] = snapshot;

        /// <summary>
        /// 对标老端41621：只修改已有41620条目；重复task_id按delta中最后一项生效，
        /// 不新增未知任务、不重排或去重已有wire列表。空delta只标记已收到而不清全量。
        /// </summary>
        public void ApplyTaskDelta(byte huntType, IReadOnlyList<TaskItem> delta)
        {
            LastTaskDelta = new TaskDeltaSnapshot(huntType, delta);
            if (!_tasks.TryGetValue(huntType, out TaskSnapshot current)) return;

            var updated = new List<TaskItem>(current.Tasks.Count);
            for (int i = 0; i < current.Tasks.Count; i++)
            {
                TaskItem old = current.Tasks[i];
                uint num = old.Num;
                byte state = old.State;
                if (delta != null)
                {
                    for (int j = 0; j < delta.Count; j++)
                    {
                        TaskItem change = delta[j];
                        if (change.TaskId != old.TaskId) continue;
                        num = change.Num;
                        state = change.State;
                    }
                }
                updated.Add(new TaskItem(old.TaskId, num, state));
            }
            _tasks[huntType] = new TaskSnapshot(current.Code, huntType, updated);
        }

        public bool TryGetPage(byte huntType, byte recordType, out PageSnapshot snapshot) =>
            _pages.TryGetValue(new PageKey(huntType, recordType), out snapshot);
        public bool TryGetDrawWeapon(byte huntType, out byte drawWeapon) =>
            _latestDrawWeapon.TryGetValue(huntType, out drawWeapon);
        public bool TryGetLucky(byte huntType, out LuckySnapshot snapshot) =>
            _luckies.TryGetValue(huntType, out snapshot);
        public bool TryGetCrossRecords(byte huntType, out CrossRecordSnapshot snapshot) =>
            _crossRecords.TryGetValue(huntType, out snapshot);
        public bool TryGetOpenState(byte huntType, out OpenStateSnapshot snapshot) =>
            _openStates.TryGetValue(huntType, out snapshot);
        public bool TryGetTasks(byte huntType, out TaskSnapshot snapshot) =>
            _tasks.TryGetValue(huntType, out snapshot);

        public void Reset()
        {
            LastError = null;
            Rune = null;
            LastRecordPush = null;
            LastWeaponNotice = null;
            LastTaskDelta = null;
            _pages.Clear();
            _latestDrawWeapon.Clear();
            _luckies.Clear();
            _crossRecords.Clear();
            _openStates.Clear();
            _tasks.Clear();
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
