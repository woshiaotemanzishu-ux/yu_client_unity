using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.GhostWalk
{
    public sealed class GhostWalkController : BaseController
    {
        public static readonly GhostWalkController Instance = new GhostWalkController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private GhostWalkController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GHOST_WALK_INFO, On20601);
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.GHOST_WALK_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.GHOST_WALK_INFO);
        }

        private void On20601(NetReader r)
        {
            byte state = r.ReadU8();
            uint endTime = r.ReadU32();
            byte serverModule = r.ReadU8();
            uint groupId = r.ReadU32();
            int count = r.ReadU16();
            var servers = new List<GhostWalkModel.Server>(count);

            for (int i = 0; i < count; i++)
            {
                ushort serverId = r.ReadU16();
                ushort serverNumber = r.ReadU16();
                string serverName = r.ReadString();
                ushort openDay = r.ReadU16();
                ushort worldLevel = r.ReadU16();
                servers.Add(new GhostWalkModel.Server(serverId, serverNumber, serverName, openDay, worldLevel));
            }

            ushort averageWorldLevel = r.ReadU16();
            GhostWalkModel.Instance.Replace(state, endTime, serverModule, groupId, servers, averageWorldLevel);
        }

        public override void Dispose()
        {
            GhostWalkModel.Instance.Reset();
            base.Dispose();
        }
    }
}
