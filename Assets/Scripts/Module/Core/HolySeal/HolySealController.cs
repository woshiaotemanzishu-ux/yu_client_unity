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
            RegisterProtocal(Proto.HOLY_SEAL_RATING, On65407);
        }

        private void On65400(NetReader reader)
        {
            HolySealModel.Instance.ReplaceError(reader.ReadU32(), reader.ReadString());
        }

        private void On65407(NetReader reader)
        {
            HolySealModel.Instance.ReplaceRating(reader.ReadU32());
        }

        public void RequestRating()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_SEAL_RATING, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.HOLY_SEAL_RATING);
        }

        public override void Dispose()
        {
            HolySealModel.Instance.Reset();
            base.Dispose();
        }
    }
}
