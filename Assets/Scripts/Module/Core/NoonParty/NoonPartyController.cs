using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.NoonParty
{
    public sealed class NoonPartyController : BaseController
    {
        public static readonly NoonPartyController Instance = new NoonPartyController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private NoonPartyController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.NOON_PARTY_ERROR, On28500);
            RegisterProtocal(Proto.NOON_PARTY_TOTAL_EXP, On28503);
            RegisterProtocal(Proto.NOON_PARTY_BOX_COUNTS, On28504);
            RegisterProtocal(Proto.NOON_PARTY_REBORN_DEADLINE, On28505);
            RegisterProtocal(Proto.NOON_PARTY_END_DEADLINE, On28506);
        }

        private void On28500(NetReader reader)
        {
            NoonPartyModel.Instance.ReplaceError(reader.ReadU32());
        }

        public void RequestExp()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.NOON_PARTY_TOTAL_EXP, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.NOON_PARTY_TOTAL_EXP);
        }

        private void On28503(NetReader reader)
        {
            NoonPartyModel.Instance.Replace(reader.ReadU32());
        }

        public void RequestBoxCounts()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.NOON_PARTY_BOX_COUNTS, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.NOON_PARTY_BOX_COUNTS);
        }

        private void On28504(NetReader reader)
        {
            NoonPartyModel.Instance.ReplaceBoxCounts(reader.ReadU32(), reader.ReadU32());
        }

        public void RequestRebornDeadline()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.NOON_PARTY_REBORN_DEADLINE, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.NOON_PARTY_REBORN_DEADLINE);
        }

        private void On28505(NetReader reader)
        {
            NoonPartyModel.Instance.ReplaceRebornDeadline(reader.ReadU32());
        }

        public void RequestEndDeadline()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.NOON_PARTY_END_DEADLINE, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.NOON_PARTY_END_DEADLINE);
        }

        private void On28506(NetReader reader)
        {
            NoonPartyModel.Instance.ReplaceEndDeadline(reader.ReadU32());
        }

        public override void Dispose()
        {
            NoonPartyModel.Instance.Reset();
            base.Dispose();
        }
    }
}
