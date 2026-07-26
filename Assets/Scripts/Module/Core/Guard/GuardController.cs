using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Guard
{
    public sealed class GuardController : BaseController
    {
        public static readonly GuardController Instance = new GuardController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private GuardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GUARD_ERROR, On21600);
            RegisterProtocal(Proto.GUARD_INFO, On21601);
            RegisterProtocal(Proto.GUARD_LOGIN_CHECK_RESULT, On21606);
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.GUARD_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.GUARD_INFO);
        }

        private void On21601(NetReader r)
        {
            int count = r.ReadU16();
            var circles = new List<GuardModel.Circle>(count);
            for (int i = 0; i < count; i++)
            {
                circles.Add(new GuardModel.Circle(r.ReadU8(), r.ReadU8(), r.ReadU32(), r.ReadU8(), r.ReadU8()));
            }
            GuardModel.Instance.Replace(circles);
        }

        private void On21600(NetReader r)
        {
            GuardModel.Instance.SetError(r.ReadU32());
        }

        private void On21606(NetReader r)
        {
            GuardModel.Instance.SetLoginCheckResult(r.ReadU32());
        }

        public override void Dispose()
        {
            GuardModel.Instance.Reset();
            base.Dispose();
        }
    }
}
