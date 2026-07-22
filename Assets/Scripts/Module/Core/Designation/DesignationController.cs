using System; using System.Collections.Generic; using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.Designation
{
    /// <summary>41101 称号列表基础快照；同号推送只替换模型，不形成回环。</summary>
    public sealed class DesignationController : BaseController
    {
        public static readonly DesignationController Instance = new DesignationController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private DesignationController() { }
        protected override void Register() => RegisterProtocal(Proto.DESIGNATION_LIST, On41101);
        public void RequestStartup() => SendEmpty();
        private void SendEmpty() {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DESIGNATION_LIST, null, null); if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DESIGNATION_LIST); }
        private void On41101(NetReader r) { uint current = r.ReadU32(); int count = r.ReadU16(); var entries = new List<DesignationModel.Entry>(count); for (int i = 0; i < count; i++) entries.Add(new DesignationModel.Entry(r.ReadU32(), r.ReadU8(), r.ReadU32())); DesignationModel.Instance.ReplaceData(current, entries); }
        public override void Dispose() { DesignationModel.Instance.Reset(); base.Dispose(); }
    }
}
