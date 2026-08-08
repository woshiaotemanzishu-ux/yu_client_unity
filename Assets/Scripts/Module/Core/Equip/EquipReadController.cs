using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Mail;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备神装只读链与共鸣完整事务链。共鸣写操作只从可编辑业务页经冻结确认进入；
    /// 本控制器负责单飞、统一错误、超时、主动 15222 推送和权威重拉，不在本地预扣材料。
    /// </summary>
    public sealed class EquipReadController : BaseController
    {
        public sealed class SuitAggregateEntry
        {
            public SuitAggregateEntry(byte suitLevel, byte suitSubLevel, byte count)
            {
                SuitLevel = suitLevel;
                SuitSubLevel = suitSubLevel;
                Count = count;
            }

            public byte SuitLevel { get; }
            public byte SuitSubLevel { get; }
            public byte Count { get; }
        }

        public sealed class SuitOperationResult
        {
            public int Protocol { get; internal set; }
            public bool Success { get; internal set; }
            public bool WasRequested { get; internal set; }
            public int ErrorCode { get; internal set; }
            public byte EquipType { get; internal set; }
            public byte MakeType { get; internal set; }
            public ushort Level { get; internal set; }
            public IReadOnlyList<EquipReadModel.RewardEntry> Rewards { get; internal set; }
                = Array.Empty<EquipReadModel.RewardEntry>();
            public IReadOnlyList<SuitAggregateEntry> SuitList { get; internal set; }
                = Array.Empty<SuitAggregateEntry>();
        }

        public static readonly EquipReadController Instance = new EquipReadController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private int _pendingProtocol;
        private byte _pendingMakeType;
        private byte _pendingEquipType;
        private int _pendingEpoch;

        private EquipReadController() { }

        public bool SuitOperationPending => _pendingProtocol != 0;
        public int PendingSuitProtocol => _pendingProtocol;

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_GOD_INFO, On15217);
            RegisterProtocal(Proto.EQUIP_GOD_POWER_PREVIEW, On15219);
            RegisterProtocal(Proto.EQUIP_SUIT_INFO, On15220);
            RegisterProtocal(Proto.EQUIP_SUIT_BUILD, On15221);
            RegisterProtocal(Proto.EQUIP_SUIT_RETURN, On15222);
            RegisterProtocal(Proto.EQUIP_SUIT_RETURN_PREVIEW, On15223);
            RegisterProtocal(Proto.EQUIP_SUIT_POWER, On15262);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, RequestStartup);
            EventDispatcher.On<int>(GlobalEvent.EVT_EQUIP_ERROR, OnEquipError);
        }

        /// <summary>对标老端 GAME_START 的装备读取链；写事务绝不在启动时自动触发。</summary>
        public void RequestStartup()
        {
            EquipReadModel.Instance.Reset();
            ClearPending();
            SendRequest(Proto.EQUIP_GOD_INFO);
            SendRequest(Proto.EQUIP_SUIT_INFO);
        }

        public void RequestGodInfo() => SendRequest(Proto.EQUIP_GOD_INFO);
        public void RequestGodPowerPreview(byte pos) => SendRequest(Proto.EQUIP_GOD_POWER_PREVIEW, "c", pos);
        public void RequestSuitInfo() => SendRequest(Proto.EQUIP_SUIT_INFO);
        public void RequestSuitReturnPreview(byte makeType, byte equipType)
            => SendRequest(Proto.EQUIP_SUIT_RETURN_PREVIEW, "cc", makeType, equipType);
        public void RequestSuitPower(byte pos, byte type, ushort level)
            => SendRequest(Proto.EQUIP_SUIT_POWER, "cch", pos, type, level);

        public bool TryRequestSuitBuild(byte makeType, byte equipType)
            => TryStartOperation(Proto.EQUIP_SUIT_BUILD, makeType, equipType);

        public bool TryRequestSuitReturn(byte makeType, byte equipType)
            => TryStartOperation(Proto.EQUIP_SUIT_RETURN, makeType, equipType);

        private void On15217(NetReader reader)
        {
            uint totalPower = reader.ReadU32();
            EquipReadModel.Instance.ReplaceGodInfo(totalPower, reader.ReadArray(r =>
                new EquipReadModel.GodEntry(r.ReadU8(), r.ReadU16())));
        }

        private void On15219(NetReader reader)
            => EquipReadModel.Instance.ReplaceGodPowerPreview(reader.ReadU32());

        private void On15220(NetReader reader)
        {
            EquipReadModel.Instance.ReplaceSuitInfo(reader.ReadArray(r =>
                new EquipReadModel.SuitEntry(r.ReadU8(), r.ReadU8(), r.ReadU16())));
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
        }

        private void On15221(NetReader reader)
        {
            byte equipType = reader.ReadU8();
            byte makeType = reader.ReadU8();
            ushort level = reader.ReadU16();
            List<SuitAggregateEntry> suits = reader.ReadArray(ReadSuitAggregate);
            bool requested = CompletePending(Proto.EQUIP_SUIT_BUILD, makeType, equipType);
            EquipReadModel.Instance.UpsertSuit(equipType, makeType, level);
            RefreshAfterSuitTransaction(includeMail: false);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, new SuitOperationResult
            {
                Protocol = Proto.EQUIP_SUIT_BUILD,
                Success = true,
                WasRequested = requested,
                EquipType = equipType,
                MakeType = makeType,
                Level = level,
                SuitList = suits.AsReadOnly(),
            });
        }

        private void On15222(NetReader reader)
        {
            byte equipType = reader.ReadU8();
            byte makeType = reader.ReadU8();
            ushort level = reader.ReadU16();
            List<EquipReadModel.RewardEntry> rewards = reader.ReadArray(ReadReward);
            List<SuitAggregateEntry> suits = reader.ReadArray(ReadSuitAggregate);
            bool requested = CompletePending(Proto.EQUIP_SUIT_RETURN, makeType, equipType);
            EquipReadModel.Instance.UpsertSuit(equipType, makeType, level);
            RefreshAfterSuitTransaction(includeMail: true);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, new SuitOperationResult
            {
                Protocol = Proto.EQUIP_SUIT_RETURN,
                Success = true,
                WasRequested = requested,
                EquipType = equipType,
                MakeType = makeType,
                Level = level,
                Rewards = rewards.AsReadOnly(),
                SuitList = suits.AsReadOnly(),
            });
        }

        private void On15223(NetReader reader)
        {
            byte equipType = reader.ReadU8();
            byte makeType = reader.ReadU8();
            EquipReadModel.Instance.ReplaceReturnPreview(new EquipReadModel.SuitReturnPreview(
                equipType, makeType, reader.ReadArray(ReadReward)));
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_RETURN_PREVIEW);
        }

        private void On15262(NetReader reader)
        {
            byte pos = reader.ReadU8();
            byte type = reader.ReadU8();
            ushort level = reader.ReadU16();
            EquipReadModel.Instance.ReplaceSuitPower(new EquipReadModel.SuitPowerSnapshot(
                pos, type, level, reader.ReadArray(r =>
                    new EquipReadModel.SuitPowerEntry(r.ReadU8(), unchecked((ulong)r.ReadU64())))));
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_UPDATE);
        }

        private static EquipReadModel.RewardEntry ReadReward(NetReader reader)
            => new EquipReadModel.RewardEntry(reader.ReadU8(), reader.ReadU32(), reader.ReadU16(), reader.ReadString());

        private static SuitAggregateEntry ReadSuitAggregate(NetReader reader)
            => new SuitAggregateEntry(reader.ReadU8(), reader.ReadU8(), reader.ReadU8());

        private bool TryStartOperation(int protocol, byte makeType, byte equipType)
        {
            if (_pendingProtocol != 0 || makeType == 0 || equipType == 0) return false;
            _pendingProtocol = protocol;
            _pendingMakeType = makeType;
            _pendingEquipType = equipType;
            int epoch = ++_pendingEpoch;
            SendRequest(protocol, "cc", makeType, equipType);
            _ = WatchOperationTimeout(epoch, protocol, makeType, equipType);
            return true;
        }

        private async Task WatchOperationTimeout(int epoch, int protocol, byte makeType, byte equipType)
        {
            await TimeUtil.Delay(12000);
            if (epoch != _pendingEpoch || _pendingProtocol != protocol
                || _pendingMakeType != makeType || _pendingEquipType != equipType) return;
            ClearPending();
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, new SuitOperationResult
            {
                Protocol = protocol,
                Success = false,
                WasRequested = true,
                ErrorCode = -1,
                EquipType = equipType,
                MakeType = makeType,
            });
        }

        private bool CompletePending(int protocol, byte makeType, byte equipType)
        {
            bool match = _pendingProtocol == protocol
                && _pendingMakeType == makeType && _pendingEquipType == equipType;
            if (match) ClearPending();
            return match;
        }

        private void OnEquipError(int errorCode)
        {
            if (_pendingProtocol == 0) return;
            int protocol = _pendingProtocol;
            byte makeType = _pendingMakeType;
            byte equipType = _pendingEquipType;
            ClearPending();
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, new SuitOperationResult
            {
                Protocol = protocol,
                Success = false,
                WasRequested = true,
                ErrorCode = errorCode,
                EquipType = equipType,
                MakeType = makeType,
            });
        }

        private void ClearPending()
        {
            _pendingProtocol = 0;
            _pendingMakeType = 0;
            _pendingEquipType = 0;
            _pendingEpoch++;
        }

        private void RefreshAfterSuitTransaction(bool includeMail)
        {
            RequestSuitInfo();
            BagController.Instance.RequestContainer(BagModel.POS_BAG);
            BagController.Instance.RequestContainer(BagModel.POS_EQUIP);
            if (includeMail) MailController.Instance.RequestMailList();
        }

        private void SendRequest(int protocol, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocol, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            if (string.IsNullOrEmpty(format)) SendFmt(protocol);
            else SendFmt(protocol, format, args);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, RequestStartup);
            EventDispatcher.Off<int>(GlobalEvent.EVT_EQUIP_ERROR, OnEquipError);
            ClearPending();
            EquipReadModel.Instance.Reset();
            base.Dispose();
        }
    }
}
