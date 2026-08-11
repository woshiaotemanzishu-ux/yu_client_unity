using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Longlang
{
    /// <summary>龙语622家族读侧控制器；强化、穿戴和脱下等资产写操作不接。</summary>
    public sealed class LonglangController : BaseController
    {
        public static readonly LonglangController Instance = new LonglangController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private LonglangController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.LONGLANG_ERROR, On62200);
            RegisterProtocal(Proto.LONGLANG_EQUIPMENT, On62201);
            RegisterProtocal(Proto.LONGLANG_STRENGTHEN, On62202);
            RegisterProtocal(Proto.LONGLANG_REPLACE, On62203);
            RegisterProtocal(Proto.LONGLANG_UNLOAD, On62204);
            RegisterProtocal(Proto.LONGLANG_RATING, On62207);
            RegisterProtocal(Proto.LONGLANG_SUIT_PREVIEW, On62208);
            RegisterProtocal(Proto.LONGLANG_SUIT_INFO, On62209);
        }

        public void RequestStartup()
        {
            LonglangModel.Instance.Reset();
            RequestEquipment();
        }

        public void RequestEquipment() => SendRequest(Proto.LONGLANG_EQUIPMENT);
        public void RequestRating() => SendRequest(Proto.LONGLANG_RATING);
        public void RequestSuitPreview(uint goodsTypeId) =>
            SendRequest(Proto.LONGLANG_SUIT_PREVIEW, "i", goodsTypeId);
        public void RequestSuitInfo() => SendRequest(Proto.LONGLANG_SUIT_INFO);
        public void Strengthen(byte position, byte strengthenType) =>
            SendRequest(Proto.LONGLANG_STRENGTHEN, "cc", position, strengthenType);
        public void Replace(byte position, ulong goodsId) =>
            SendRequest(Proto.LONGLANG_REPLACE, "cl", position, unchecked((long)goodsId));
        /// <summary>服务端 62204 C2S 严格只有 pos:u8；禁止照抄老端历史尾包。</summary>
        public void Unload(byte position) => SendRequest(Proto.LONGLANG_UNLOAD, "c", position);

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On62200(NetReader r) =>
            LonglangModel.Instance.ReplaceError(r.ReadU32(), r.ReadString());

        private void On62201(NetReader r)
        {
            List<LonglangModel.Equipment> items = r.ReadArray(rr => new LonglangModel.Equipment(
                rr.ReadU8(), unchecked((ulong)rr.ReadU64()), rr.ReadU16()));
            LonglangModel.Instance.ReplaceEquipments(items);
        }

        private void On62202(NetReader r) =>
            LonglangModel.Instance.ApplyStrength(r.ReadU8(), r.ReadU16());

        private void On62203(NetReader r) => LonglangModel.Instance.ApplyReplaceAck();
        private void On62204(NetReader r) => LonglangModel.Instance.ApplyUnloadAck();

        private void On62207(NetReader r) => LonglangModel.Instance.ReplaceRating(r.ReadU32());

        private void On62208(NetReader r)
        {
            List<LonglangModel.SuitEntry> suits = ReadSuitList(r);
            LonglangModel.Instance.ReplacePreview(suits, r.ReadU32());
        }

        private void On62209(NetReader r) => LonglangModel.Instance.ReplaceSuitInfo(ReadSuitList(r));

        private static List<LonglangModel.SuitEntry> ReadSuitList(NetReader r) =>
            r.ReadArray(rr => new LonglangModel.SuitEntry(rr.ReadU32(), rr.ReadU16()));

        public override void Dispose()
        {
            LonglangModel.Instance.Reset();
            base.Dispose();
        }
    }
}
