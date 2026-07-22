using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.TSCrack
{
    /// <summary>
    /// 时空圣痕跨服世界快照。仅接收/请求 20411；不复刻老端 status==1 后自动请求其他 204xx，
    /// 也不在等级变化时重拉。
    /// </summary>
    public sealed class TSCrackController : BaseController
    {
        public static readonly TSCrackController Instance = new TSCrackController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private TSCrackController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TS_CRACK_WORLD_INFO, On20411);
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.TS_CRACK_WORLD_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.TS_CRACK_WORLD_INFO);
        }

        private void On20411(NetReader reader)
        {
            byte status = reader.ReadU8();
            int count = reader.ReadU16();
            var servers = new List<TSCrackModel.ServerEntry>(count);

            for (int i = 0; i < count; i++)
            {
                uint serverNumber = reader.ReadU32();
                string serverName = reader.ReadString();
                ushort level = reader.ReadU16();
                servers.Add(new TSCrackModel.ServerEntry(serverNumber, serverName, level));
            }

            TSCrackModel.Instance.Replace(status, servers);
        }

        public override void Dispose()
        {
            TSCrackModel.Instance.Reset();
            base.Dispose();
        }
    }
}
