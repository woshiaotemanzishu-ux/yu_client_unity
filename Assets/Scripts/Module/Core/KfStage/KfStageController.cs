using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.KfStage
{
    /// <summary>10200 跨服分组基础快照控制器；同号推送只替换模型，不形成回环。</summary>
    public sealed class KfStageController : BaseController
    {
        public static readonly KfStageController Instance = new KfStageController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private KfStageController() { }
        protected override void Register() => RegisterProtocal(Proto.KF_STAGE_INFO, On10200);
        public void RequestStartup() => SendEmpty();
        private void SendEmpty()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.KF_STAGE_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.KF_STAGE_INFO);
        }
        private void On10200(NetReader r)
        {
            uint openDay = r.ReadU32();
            int serverCount = r.ReadU16(); var servers = new List<KfStageModel.ServerEntry>(serverCount);
            for (int i = 0; i < serverCount; i++) servers.Add(new KfStageModel.ServerEntry(r.ReadU16(), r.ReadU16(), r.ReadString(), r.ReadU16()));
            int moduleCount = r.ReadU16(); var modules = new List<KfStageModel.ModuleEntry>(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                ushort moduleId = r.ReadU16(); byte mod = r.ReadU8(); ushort avg = r.ReadU16();
                int idsCount = r.ReadU16(); var ids = new List<ushort>(idsCount); for (int j = 0; j < idsCount; j++) ids.Add(r.ReadU16());
                int nextCount = r.ReadU16(); var next = new List<ushort>(nextCount); for (int j = 0; j < nextCount; j++) next.Add(r.ReadU16());
                modules.Add(new KfStageModel.ModuleEntry(moduleId, mod, avg, ids, next));
            }
            KfStageModel.Instance.ReplaceData(openDay, servers, modules);
        }
        public override void Dispose() { KfStageModel.Instance.Reset(); base.Dispose(); }
    }
}
