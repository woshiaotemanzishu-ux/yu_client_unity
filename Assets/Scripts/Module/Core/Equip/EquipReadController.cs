using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>装备神装/共鸣套装只读控制器；写操作15218/15221/15222保持隔离。</summary>
    public sealed class EquipReadController : BaseController
    {
        public static readonly EquipReadController Instance = new EquipReadController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private EquipReadController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_GOD_INFO, On15217);
            RegisterProtocal(Proto.EQUIP_GOD_POWER_PREVIEW, On15219);
            RegisterProtocal(Proto.EQUIP_SUIT_INFO, On15220);
            RegisterProtocal(Proto.EQUIP_SUIT_RETURN_PREVIEW, On15223);
            RegisterProtocal(Proto.EQUIP_SUIT_POWER, On15262);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, RequestStartup);
        }

        /// <summary>对标老端GAME_START中的装备读链：本控制器拥有15217→15220，15214/15210/15261由既有控制器拥有。</summary>
        public void RequestStartup()
        {
            EquipReadModel.Instance.Reset();
            SendRequest(Proto.EQUIP_GOD_INFO);
            SendRequest(Proto.EQUIP_SUIT_INFO);
        }

        public void RequestGodInfo() => SendRequest(Proto.EQUIP_GOD_INFO);
        public void RequestGodPowerPreview(byte pos) => SendRequest(Proto.EQUIP_GOD_POWER_PREVIEW, "c", pos);
        public void RequestSuitInfo() => SendRequest(Proto.EQUIP_SUIT_INFO);
        public void RequestSuitReturnPreview(byte makeType, byte equipType) =>
            SendRequest(Proto.EQUIP_SUIT_RETURN_PREVIEW, "cc", makeType, equipType);
        public void RequestSuitPower(byte pos, byte type, ushort level) =>
            SendRequest(Proto.EQUIP_SUIT_POWER, "cch", pos, type, level);

        private void On15217(NetReader reader)
        {
            uint totalPower = reader.ReadU32();
            EquipReadModel.Instance.ReplaceGodInfo(totalPower, reader.ReadArray(r =>
                new EquipReadModel.GodEntry(r.ReadU8(), r.ReadU16())));
        }

        private void On15219(NetReader reader) => EquipReadModel.Instance.ReplaceGodPowerPreview(reader.ReadU32());

        private void On15220(NetReader reader) => EquipReadModel.Instance.ReplaceSuitInfo(reader.ReadArray(r =>
            new EquipReadModel.SuitEntry(r.ReadU8(), r.ReadU8(), r.ReadU16())));

        private void On15223(NetReader reader)
        {
            byte equipType = reader.ReadU8();
            byte makeType = reader.ReadU8();
            EquipReadModel.Instance.ReplaceReturnPreview(new EquipReadModel.SuitReturnPreview(
                equipType, makeType, reader.ReadArray(ReadReward)));
        }

        private void On15262(NetReader reader)
        {
            byte pos = reader.ReadU8();
            byte type = reader.ReadU8();
            ushort level = reader.ReadU16();
            EquipReadModel.Instance.ReplaceSuitPower(new EquipReadModel.SuitPowerSnapshot(
                pos, type, level, reader.ReadArray(r => new EquipReadModel.SuitPowerEntry(r.ReadU8(), unchecked((ulong)r.ReadU64())))));
        }

        private static EquipReadModel.RewardEntry ReadReward(NetReader reader) =>
            new EquipReadModel.RewardEntry(reader.ReadU8(), reader.ReadU32(), reader.ReadU16(), reader.ReadString());

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
            EquipReadModel.Instance.Reset();
            base.Dispose();
        }
    }
}
