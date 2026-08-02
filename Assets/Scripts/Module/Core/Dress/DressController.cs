using System; using System.Collections.Generic; using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.Dress
{
    public sealed class DressController : BaseController
    {
        public static readonly DressController Instance = new DressController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private DressController() { }
        protected override void Register() => RegisterProtocal(Proto.DRESS_INFO, On11200);
        public void RequestStartup() { RequestInfo(1); RequestInfo(2); RequestInfo(3); RequestInfo(5); }
        public void RequestInfo(byte type) {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DRESS_INFO, "c", type); if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DRESS_INFO, "c", type); }
        private void On11200(NetReader r) { byte type = r.ReadU8(); uint used = r.ReadU32(); int count = r.ReadU16(); var entries = new List<DressModel.Entry>(count); for (int i = 0; i < count; i++) entries.Add(new DressModel.Entry(r.ReadU32(), r.ReadU16(), unchecked((ulong)r.ReadU64()), unchecked((ulong)r.ReadU64()))); DressModel.Instance.Replace(type, used, entries); }
        public override void Dispose() { DressModel.Instance.Reset(); base.Dispose(); }
    }
}
