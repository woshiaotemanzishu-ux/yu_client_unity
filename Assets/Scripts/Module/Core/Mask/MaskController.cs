using System; using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.Mask
{
    /// <summary>51101 面具状态控制器；同号推送只覆盖快照，不形成回环。</summary>
    public sealed class MaskController : BaseController
    {
        public static readonly MaskController Instance = new MaskController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private MaskController() { }
        protected override void Register() => RegisterProtocal(Proto.MASK_INFO, On51101);
        public void RequestStartup() => SendEmpty();
        private void SendEmpty() {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MASK_INFO, null, null); if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MASK_INFO); }
        private void On51101(NetReader r) { MaskModel.Instance.ReplaceData(r.ReadU8(), r.ReadU32()); }
        public override void Dispose() { MaskModel.Instance.Reset(); base.Dispose(); }
    }
}
