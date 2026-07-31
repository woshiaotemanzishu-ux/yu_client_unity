using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>
    /// 41101 权威列表及 41104/41105/41107/41108 独立只读切片。
    /// 服务端推送只替换各自原始快照，不接 UI、场景表现或写操作回环。
    /// </summary>
    public sealed class DesignationController : BaseController
    {
        public static readonly DesignationController Instance = new DesignationController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private DesignationController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DESIGNATION_LIST, On41101);
            RegisterProtocal(Proto.DESIGNATION_ACTIVATED, On41104);
            RegisterProtocal(Proto.DESIGNATION_SCENE_NOTICE, On41105);
            RegisterProtocal(Proto.DESIGNATION_POWER, On41107);
            RegisterProtocal(Proto.DESIGNATION_REMOVED, On41108);
        }

        public void RequestStartup() => SendEmpty(Proto.DESIGNATION_LIST);

        /// <summary>显式查询一个称号的战力；无回复时保留上一次 41107 快照。</summary>
        public void RequestPower(uint designationId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DESIGNATION_POWER, "i", new object[] { designationId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DESIGNATION_POWER, "i", designationId);
        }

        private void SendEmpty(int command)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(command, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(command);
        }

        private void On41101(NetReader reader)
        {
            uint current = reader.ReadU32();
            int count = reader.ReadU16();
            var entries = new List<DesignationModel.Entry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new DesignationModel.Entry(reader.ReadU32(), reader.ReadU8(), reader.ReadU32()));
            DesignationModel.Instance.ReplaceData(current, entries);
            EventDispatcher.Emit(GlobalEvent.EVT_DESIGNATION_LIST_UPDATE);
        }

        private void On41104(NetReader reader)
            => DesignationModel.Instance.ReplaceActivation(reader.ReadU32(), reader.ReadU32(), reader.ReadU32());

        private void On41105(NetReader reader)
            => DesignationModel.Instance.ReplaceSceneNotice(unchecked((ulong)reader.ReadU64()), reader.ReadU32());

        private void On41107(NetReader reader)
            => DesignationModel.Instance.ReplacePowerQuery(reader.ReadU32(), reader.ReadU32());

        private void On41108(NetReader reader)
            => DesignationModel.Instance.ReplaceRemoval(reader.ReadU32());

        public override void Dispose()
        {
            DesignationModel.Instance.Reset();
            base.Dispose();
        }
    }
}
