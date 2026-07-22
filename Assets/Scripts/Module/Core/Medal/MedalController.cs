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
        protected override void Register()
        {
            RegisterProtocal(Proto.MEDAL_INFO, On13401);
            RegisterProtocal(Proto.MEDAL_TITLE_SNAPSHOT, On13405);
        }
        public void RequestStartup()
        {
            SendEmpty(Proto.MEDAL_INFO);
            SendEmpty(Proto.MEDAL_TITLE_SNAPSHOT);
        }
        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }
        private void On13401(NetReader r)
        {
            MedalModel.Instance.ReplaceData(r.ReadU32(), r.ReadU32(), r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU32(), r.ReadU32());
        }
        private void On13405(NetReader r)
        {
            int count = r.ReadU16(); var titles = new System.Collections.Generic.List<MedalModel.TitleEntry>(count);
            for (int i = 0; i < count; i++) titles.Add(new MedalModel.TitleEntry(r.ReadU32(), r.ReadU16(), r.ReadU32(), r.ReadU8()));
            MedalModel.Instance.ReplaceTitles(titles);
        }
        public override void Dispose()
        {
            MedalModel.Instance.Reset();
            base.Dispose();
        }
    }
}
