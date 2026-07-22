using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.MonBook
{
    public sealed class MonBookController : BaseController
    {
        public static readonly MonBookController Instance = new MonBookController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private MonBookController() { }
        protected override void Register() { RegisterProtocal(Proto.MON_BOOK_ACTIVATED_PICS, On44205); }
        public void RequestActivatedPics()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MON_BOOK_ACTIVATED_PICS, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MON_BOOK_ACTIVATED_PICS);
        }
        private void On44205(NetReader reader)
        {
            int count = reader.ReadU16();
            var pics = new List<uint>(count);
            for (int i = 0; i < count; i++) pics.Add(reader.ReadU32());
            MonBookModel.Instance.Replace(pics);
        }
        public override void Dispose() { MonBookModel.Instance.Reset(); base.Dispose(); }
    }
}
