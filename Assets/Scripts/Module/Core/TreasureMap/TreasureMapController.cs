using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.TreasureMap
{
    /// <summary>藏宝图仅接管 20300 原始错误与 20303 开奖记录查询快照。</summary>
    public sealed class TreasureMapController : BaseController
    {
        public static readonly TreasureMapController Instance = new TreasureMapController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private TreasureMapController() { }
        protected override void Register()
        {
            RegisterProtocal(Proto.TREASURE_MAP_ERROR, On20300);
            RegisterProtocal(Proto.TREASURE_MAP_DRAW_LOG, On20303);
        }

        public void RequestDrawLog()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.TREASURE_MAP_DRAW_LOG, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.TREASURE_MAP_DRAW_LOG);
        }

        private void On20300(NetReader reader)
        {
            TreasureMapModel.Instance.ReplaceError(reader.ReadU32());
        }

        private void On20303(NetReader reader)
        {
            TreasureMapModel.Instance.ReplaceDrawLog(reader.ReadArray(r =>
            {
                uint serverNum = r.ReadU32();
                long roleId = r.ReadU64();
                string name = r.ReadString();
                var rewards = r.ReadArray(rr => new TreasureMapModel.RewardEntry(rr.ReadU8(), rr.ReadU32(), rr.ReadU32()));
                return new TreasureMapModel.DrawLogEntry(serverNum, roleId, name, rewards);
            }));
        }

        public override void Dispose() { TreasureMapModel.Instance.Reset(); base.Dispose(); }
    }
}
