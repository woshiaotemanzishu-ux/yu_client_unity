using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.SentientAct
{
    public sealed class SentientActController : BaseController
    {
        public static readonly SentientActController Instance = new SentientActController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private SentientActController()
        {
        }

        protected override void Register()
        {
            RegisterProtocal(Proto.SENTIENT_ACT_INFO, On24101);
            RegisterProtocal(Proto.SENTIENT_ACT_PORTALS, On24102);
            RegisterProtocal(Proto.SENTIENT_ACT_COUNTS, On24107);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            SentientActModel.Instance.Reset();
            base.Dispose();
        }

        private void OnGameStart()
        {
            SentientActModel.Instance.Reset();
            RequestInfo();
            RequestCounts();
            RequestPortals();
        }

        public void RequestInfo()
        {
            SendEmpty(Proto.SENTIENT_ACT_INFO);
        }

        public void RequestPortals()
        {
            SendEmpty(Proto.SENTIENT_ACT_PORTALS);
        }

        public void RequestCounts()
        {
            SendEmpty(Proto.SENTIENT_ACT_COUNTS);
        }

        private void SendEmpty(int proto)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(proto, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(proto);
        }

        private void On24101(NetReader r)
        {
            byte state = r.ReadU8();
            uint end = r.ReadU32();
            uint mod = r.ReadU32();
            uint group = r.ReadU32();
            uint next = r.ReadU32();
            var servers = r.ReadArray(x => new SentientActModel.ServerEntry(
                x.ReadU64(), x.ReadU64(), x.ReadString(), x.ReadU64()));
            SentientActModel.Instance.ReplaceInfo(state, end, mod, group, next, servers, r.ReadU64());
            if (state != 0) RequestPortals();
        }

        private void On24102(NetReader r)
        {
            SentientActModel.Instance.ReplacePortals(r.ReadArray(x => new SentientActModel.PortalEntry(
                x.ReadU64(), x.ReadU32(), x.ReadU32())));
        }

        private void On24107(NetReader r)
        {
            SentientActModel.Instance.ReplaceCounts(r.ReadU32(), r.ReadU32());
        }
    }
}
