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

        protected override void Register()
        {
            RegisterProtocal(Proto.MON_BOOK_TYPE_INFO, On44201);
            RegisterProtocal(Proto.MON_BOOK_ACTIVATED_PICS, On44205);
            RegisterProtocal(Proto.MON_BOOK_PREVIEW_POWER, On44207);
        }

        public void RequestType(ushort type)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MON_BOOK_TYPE_INFO, "h", new object[] { type });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MON_BOOK_TYPE_INFO, "h", type);
        }

        private void On44201(NetReader reader)
        {
            ushort type = reader.ReadU16();
            int groupCount = reader.ReadU16();
            var groups = new List<MonBookModel.GroupEntry>(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                groups.Add(new MonBookModel.GroupEntry(reader.ReadU32(), reader.ReadU16()));
            }

            int pictureCount = reader.ReadU16();
            var pictures = new List<MonBookModel.PictureEntry>(pictureCount);
            for (int i = 0; i < pictureCount; i++)
            {
                pictures.Add(new MonBookModel.PictureEntry(
                    reader.ReadU32(), reader.ReadU16(),
                    unchecked((ulong)reader.ReadU64()), unchecked((ulong)reader.ReadU64())));
            }

            MonBookModel.Instance.ReplaceBook(type, groups, pictures, unchecked((ulong)reader.ReadU64()));
        }

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
        public void RequestPreviewPower(uint picId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.MON_BOOK_PREVIEW_POWER, "i", new object[] { picId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.MON_BOOK_PREVIEW_POWER, "i", picId);
        }
        private void On44207(NetReader reader) { MonBookModel.Instance.ReplacePreviewPower(reader.ReadU32(), unchecked((ulong)reader.ReadU64())); }
        public override void Dispose() { MonBookModel.Instance.Reset(); base.Dispose(); }
    }
}
