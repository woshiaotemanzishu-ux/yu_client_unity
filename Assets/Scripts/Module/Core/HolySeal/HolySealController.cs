using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HolySeal
{
    public sealed class HolySealController : BaseController
    {
        public static readonly HolySealController Instance = new HolySealController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private HolySealController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HOLY_SEAL_ERROR, On65400);
            RegisterProtocal(Proto.HOLY_SEAL_EQUIPS, On65401);
            RegisterProtocal(Proto.HOLY_SEAL_STRENGTHEN, On65402);
            RegisterProtocal(Proto.HOLY_SEAL_REPLACE, On65403);
            RegisterProtocal(Proto.HOLY_SEAL_PILLS, On65405);
            RegisterProtocal(Proto.HOLY_SEAL_USE_PILL, On65406);
            RegisterProtocal(Proto.HOLY_SEAL_RATING, On65407);
            RegisterProtocal(Proto.HOLY_SEAL_SUIT_PREVIEW, On65408);
            RegisterProtocal(Proto.HOLY_SEAL_SUITS, On65409);
        }

        private void On65400(NetReader reader)
        {
            HolySealModel.Instance.ReplaceError(reader.ReadU32(), reader.ReadString());
        }

        private void On65407(NetReader reader)
        {
            HolySealModel.Instance.ReplaceRating(reader.ReadU32());
        }

        private void On65401(NetReader reader)
        {
            HolySealModel.Instance.ReplaceEquipSnapshot(reader.ReadArray(r =>
                new HolySealModel.EquipEntry(r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadU16())));
        }

        private void On65405(NetReader reader)
        {
            HolySealModel.Instance.ReplacePillSnapshot(reader.ReadArray(r =>
                new HolySealModel.PillEntry(r.ReadU32(), r.ReadU16(), r.ReadU16())));
        }

        private void On65402(NetReader reader) =>
            HolySealModel.Instance.ApplyStrength(reader.ReadU8(), reader.ReadU16());

        private void On65403(NetReader reader) => HolySealModel.Instance.ApplyReplaceAck();

        private void On65406(NetReader reader) => HolySealModel.Instance.ApplyPillUse(
            reader.ReadU32(), reader.ReadU16(), reader.ReadU32());

        private void On65408(NetReader reader)
        {
            var suits = reader.ReadArray(ReadSuitEntry);
            HolySealModel.Instance.ReplaceSuitPreview(suits, reader.ReadU32());
        }

        private void On65409(NetReader reader)
        {
            HolySealModel.Instance.ReplaceSuitSnapshot(reader.ReadArray(ReadSuitEntry));
        }

        /// <summary>对标老端 GAME_START：先清本家族缓存，再严格发送 65401 → 65405。</summary>
        public void RequestStartup()
        {
            HolySealModel.Instance.Reset();
            SendRequest(Proto.HOLY_SEAL_EQUIPS);
            SendRequest(Proto.HOLY_SEAL_PILLS);
        }

        public void RequestPills() => SendRequest(Proto.HOLY_SEAL_PILLS);

        public void RequestRating()
        {
            SendRequest(Proto.HOLY_SEAL_RATING);
        }

        public void RequestSuitPreview(uint goodsTypeId) =>
            SendRequest(Proto.HOLY_SEAL_SUIT_PREVIEW, "i", goodsTypeId);

        public void RequestSuitSnapshot() => SendRequest(Proto.HOLY_SEAL_SUITS);

        public void Strengthen(byte position, byte strengthenType) =>
            SendRequest(Proto.HOLY_SEAL_STRENGTHEN, "cc", position, strengthenType);

        public void Replace(byte position, ulong goodsId) =>
            SendRequest(Proto.HOLY_SEAL_REPLACE, "cl", position, unchecked((long)goodsId));

        public void UsePill(uint goodsTypeId, ushort num) =>
            SendRequest(Proto.HOLY_SEAL_USE_PILL, "ih", goodsTypeId, num);

        private static HolySealModel.SuitEntry ReadSuitEntry(NetReader reader) =>
            new HolySealModel.SuitEntry(reader.ReadU32(), reader.ReadU16());

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
            HolySealModel.Instance.Reset();
            base.Dispose();
        }
    }
}
