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
            RegisterProtocal(Proto.GHOST_WALK_ERROR, On20600);
            RegisterProtocal(Proto.GHOST_WALK_INFO, On20601);
            RegisterProtocal(Proto.GHOST_WALK_BOSS_INFO, On20602);
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

        /// <summary>显式查询指定场景（0 为服务端全量）；不加入启动或 20601 链。</summary>
        public void RequestBossInfo(uint sceneId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.GHOST_WALK_BOSS_INFO, "i", sceneId);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.GHOST_WALK_BOSS_INFO, "i", sceneId);
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

        private void On20600(NetReader r)
        {
            GhostWalkModel.Instance.SetError(r.ReadU32(), r.ReadString());
        }

        private void On20602(NetReader r)
        {
            ushort count = r.ReadU16();
            var scenes = new List<GhostWalkModel.SceneBossInfo>(count);
            for (int i = 0; i < count; i++)
            {
                uint sceneId = r.ReadU32();
                byte num = r.ReadU8();
                ushort bossCount = r.ReadU16();
                var bossIds = new List<uint>(bossCount);
                for (int j = 0; j < bossCount; j++) bossIds.Add(r.ReadU32());
                scenes.Add(new GhostWalkModel.SceneBossInfo(sceneId, num, bossIds));
            }
            GhostWalkModel.Instance.ReplaceBossInfo(scenes);
        }

        public override void Dispose()
        {
            GhostWalkModel.Instance.Reset();
            base.Dispose();
        }
    }
}
