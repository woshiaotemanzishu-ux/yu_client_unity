using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Pray
{
    public sealed class PrayController : BaseController
    {
        public static readonly PrayController Instance = new PrayController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_prayInfoOutboundIntercept = null;
#endif

        private PrayController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.PRAY_ERROR, On41500);
            RegisterProtocal(Proto.PRAY_INFO, On41501);
        }

        /// <summary>显式查询祈愿状态；不绑定 GAME_START、日切、等级或 VIP 事件。</summary>
        public void RequestPrayInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.PRAY_INFO, null, null);
            if (s_prayInfoOutboundIntercept != null && s_prayInfoOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.PRAY_INFO);
        }

        private void On41500(NetReader reader)
        {
            PrayModel.Instance.ReplaceError(reader.ReadU32());
        }

        private void On41501(NetReader reader)
        {
            int count = reader.ReadU16();
            var entries = new List<PrayModel.PrayInfo>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new PrayModel.PrayInfo(reader.ReadU8(), reader.ReadU8(), reader.ReadU8(), reader.ReadU32()));
            }

            PrayModel.Instance.ReplacePrayInfo(entries);
        }

        public override void Dispose()
        {
            PrayModel.Instance.Reset();
            base.Dispose();
        }
    }
}
