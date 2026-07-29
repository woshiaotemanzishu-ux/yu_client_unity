using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Game;

namespace Shenxiao.Module.Core.RuneTreasure
{
    /// <summary>寻宝416家族安全读侧；抽奖、取物、兑换、领奖和打开确认等写操作不接。</summary>
    public sealed class RuneTreasureController : BaseController
    {
        public const byte EquipType = 1;
        public const byte PeakType = 2;
        public const byte ExtremeType = 3;
        public const byte RuneType = 4;
        public const byte BabyType = 5;
        public const byte AllRecords = 1;
        public const byte PersonalRecords = 2;
        private const int CrossRecordOpenDay = 8;

        public static readonly RuneTreasureController Instance = new RuneTreasureController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private RuneTreasureController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.RUNE_TREASURE_ERROR, On41600);
            RegisterProtocal(Proto.RUNE_TREASURE_RUNE_INFO, On41601);
            RegisterProtocal(Proto.RUNE_TREASURE_RECORD_PUSH, On41603);
            RegisterProtocal(Proto.RUNE_TREASURE_PAGE, On41608);
            RegisterProtocal(Proto.RUNE_TREASURE_LUCKY, On41610);
            RegisterProtocal(Proto.RUNE_TREASURE_CROSS_RECORDS, On41612);
            RegisterProtocal(Proto.RUNE_TREASURE_OPEN_STATE, On41613);
            RegisterProtocal(Proto.RUNE_TREASURE_WEAPON_NOTICE, On41615);
            RegisterProtocal(Proto.RUNE_TREASURE_TASKS, On41620);
            RegisterProtocal(Proto.RUNE_TREASURE_TASK_UPDATE, On41621);
        }

        /// <summary>严格镜像老端GAME_START读请求顺序；跨服记录仅开服第8天起发送。</summary>
        public void RequestStartup()
        {
            RuneTreasureModel.Instance.Reset();
            RequestRuneInfo();
            RequestPage(EquipType, AllRecords);
            RequestPage(PeakType, AllRecords);
            RequestPage(ExtremeType, AllRecords);
            RequestPage(EquipType, PersonalRecords);
            RequestPage(PeakType, PersonalRecords);
            RequestPage(ExtremeType, PersonalRecords);
            RequestLucky(EquipType);
            RequestLucky(PeakType);
            RequestLucky(ExtremeType);
            if (ServerTimeModel.GetOpenServerDay() >= CrossRecordOpenDay)
            {
                RequestCrossRecords(EquipType);
                RequestCrossRecords(PeakType);
                RequestCrossRecords(ExtremeType);
            }
            RequestPage(BabyType, AllRecords);
            RequestOpenState(EquipType);
            RequestOpenState(PeakType);
            RequestOpenState(ExtremeType);
            RequestTasks(BabyType);
        }

        public void RequestRuneInfo() =>
            SendRequest(Proto.RUNE_TREASURE_RUNE_INFO, "c", RuneType);
        public void RequestPage(byte huntType, byte recordType) =>
            SendRequest(Proto.RUNE_TREASURE_PAGE, "cc", huntType, recordType);
        public void RequestLucky(byte huntType) =>
            SendRequest(Proto.RUNE_TREASURE_LUCKY, "c", huntType);
        public void RequestCrossRecords(byte huntType) =>
            SendRequest(Proto.RUNE_TREASURE_CROSS_RECORDS, "c", huntType);
        public void RequestOpenState(byte huntType) =>
            SendRequest(Proto.RUNE_TREASURE_OPEN_STATE, "c", huntType);
        public void RequestTasks(byte huntType) =>
            SendRequest(Proto.RUNE_TREASURE_TASKS, "c", huntType);

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On41600(NetReader r) =>
            RuneTreasureModel.Instance.ReplaceError(r.ReadU32());

        private void On41601(NetReader r)
        {
            uint drawTimes = r.ReadU32();
            ushort turn = r.ReadU16();
            List<RuneTreasureModel.StageReward> rewards = r.ReadArray(rr =>
                new RuneTreasureModel.StageReward(rr.ReadU16(), rr.ReadU16()));
            RuneTreasureModel.Instance.ReplaceRune(new RuneTreasureModel.RuneSnapshot(
                drawTimes, turn, rewards, unchecked((ulong)r.ReadU64()), unchecked((ulong)r.ReadU64())));
        }

        private void On41603(NetReader r)
        {
            byte recordType = r.ReadU8();
            ulong roleId = unchecked((ulong)r.ReadU64());
            List<RuneTreasureModel.Record> records = ReadRecords(r);
            RuneTreasureModel.Instance.ReplaceRecordPush(
                new RuneTreasureModel.RecordPushSnapshot(recordType, roleId, records));
            if (recordType == AllRecords && records.Count > 0)
                RequestPage(records[0].HuntType, recordType);
        }

        private void On41608(NetReader r)
        {
            uint score = r.ReadU32();
            byte huntType = r.ReadU8();
            byte drawWeapon = r.ReadU8();
            byte recordType = r.ReadU8();
            byte freeTimes = r.ReadU8();
            ulong freeTime = unchecked((ulong)r.ReadU64());
            List<RuneTreasureModel.Record> records = ReadRecords(r);
            RuneTreasureModel model = RuneTreasureModel.Instance;
            bool refreshOpen = !model.TryGetDrawWeapon(huntType, out byte previous)
                || previous != drawWeapon;
            model.ReplacePage(new RuneTreasureModel.PageSnapshot(score, huntType, drawWeapon,
                recordType, freeTimes, freeTime, records));
            if (refreshOpen) RequestOpenState(huntType);
        }

        private void On41610(NetReader r)
        {
            byte huntType = r.ReadU8();
            RuneTreasureModel.Instance.ReplaceLucky(new RuneTreasureModel.LuckySnapshot(
                huntType, r.ReadU32(), r.ReadU16()));
        }

        private void On41612(NetReader r)
        {
            byte huntType = r.ReadU8();
            List<RuneTreasureModel.CrossRecord> records = r.ReadArray(rr =>
                new RuneTreasureModel.CrossRecord(rr.ReadU32(), rr.ReadU32(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString(), rr.ReadU8(), rr.ReadU32(),
                    rr.ReadU16(), rr.ReadU32(), rr.ReadU8()));
            RuneTreasureModel.Instance.ReplaceCrossRecords(
                new RuneTreasureModel.CrossRecordSnapshot(huntType, records));
        }

        private void On41613(NetReader r)
        {
            byte huntType = r.ReadU8();
            RuneTreasureModel.Instance.ReplaceOpenState(
                new RuneTreasureModel.OpenStateSnapshot(huntType, r.ReadU8()));
        }

        private void On41615(NetReader r)
        {
            byte huntType = r.ReadU8();
            RuneTreasureModel.Instance.ReplaceWeaponNotice(huntType);
            RequestOpenState(huntType);
        }

        private void On41620(NetReader r)
        {
            uint code = r.ReadU32();
            byte huntType = r.ReadU8();
            RuneTreasureModel.Instance.ReplaceTasks(new RuneTreasureModel.TaskSnapshot(
                code, huntType, ReadTasks(r)));
        }

        private void On41621(NetReader r)
        {
            byte huntType = r.ReadU8();
            RuneTreasureModel.Instance.ApplyTaskDelta(huntType, ReadTasks(r));
        }

        private static List<RuneTreasureModel.Record> ReadRecords(NetReader r) =>
            r.ReadArray(rr => new RuneTreasureModel.Record(unchecked((ulong)rr.ReadU64()),
                rr.ReadString(), rr.ReadU8(), rr.ReadU32(), rr.ReadU32(), rr.ReadU32(), rr.ReadU8()));

        private static List<RuneTreasureModel.TaskItem> ReadTasks(NetReader r) =>
            r.ReadArray(rr => new RuneTreasureModel.TaskItem(rr.ReadU32(), rr.ReadU32(), rr.ReadU8()));

        public override void Dispose()
        {
            RuneTreasureModel.Instance.Reset();
            base.Dispose();
        }
    }
}
