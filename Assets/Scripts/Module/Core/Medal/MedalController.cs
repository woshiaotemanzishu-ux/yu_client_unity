using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>勋章 13401 基础快照控制器；服务端主动推送仅更新模型，不形成协议回环。</summary>
    public sealed class MedalController : BaseController
    {
        public static readonly MedalController Instance = new MedalController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private MedalController() { }
        protected override void Register() => RegisterProtocal(Proto.MEDAL_INFO, On13401);
        public void RequestStartup() => SendEmpty();
        private void SendEmpty()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MEDAL_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MEDAL_INFO);
        }
        private void On13401(NetReader r)
        {
            MedalModel.Instance.ReplaceData(r.ReadU32(), r.ReadU32(), r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU32(), r.ReadU32());
        }
        public override void Dispose()
        {
            MedalModel.Instance.Reset();
            base.Dispose();
        }
    }
}
