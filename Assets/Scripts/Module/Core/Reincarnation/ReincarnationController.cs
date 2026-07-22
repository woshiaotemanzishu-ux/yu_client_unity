using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Reincarnation
{
    /// <summary>16400 天命觉醒基础快照控制器；同号推送只更新模型，不形成回环。</summary>
    public sealed class ReincarnationController : BaseController
    {
        public static readonly ReincarnationController Instance = new ReincarnationController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private ReincarnationController() { }
        protected override void Register() => RegisterProtocal(Proto.REINCARNATION_AWAKEN_INFO, On16400);
        public void RequestStartup() => SendEmpty();
        private void SendEmpty()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.REINCARNATION_AWAKEN_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.REINCARNATION_AWAKEN_INFO);
        }
        private void On16400(NetReader r)
        {
            int count = r.ReadU16(); var ids = new List<uint>(count);
            for (int i = 0; i < count; i++) ids.Add(r.ReadU32());
            ReincarnationModel.Instance.ReplaceData(ids);
        }
        public override void Dispose() { ReincarnationModel.Instance.Reset(); base.Dispose(); }
    }
}
