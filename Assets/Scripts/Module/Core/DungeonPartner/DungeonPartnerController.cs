using System;
using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.DungeonPartner
{
    public sealed class DungeonPartnerController : BaseController
    {
        public static readonly DungeonPartnerController Instance = new DungeonPartnerController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private DungeonPartnerController() { }
        protected override void Register() { RegisterProtocal(Proto.DUNGEON_PARTNER_DUNGEONS, On61105); RegisterProtocal(Proto.DUNGEON_PARTNER_STAGE_REWARDS, On61106); }
        public void RequestDungeons(byte level) { SendLevel(Proto.DUNGEON_PARTNER_DUNGEONS, level); }
        public void RequestStageRewards(byte level) { SendLevel(Proto.DUNGEON_PARTNER_STAGE_REWARDS, level); }
        private void SendLevel(int proto, byte level)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(proto, "c", level); if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(proto, "c", level);
        }
        private void On61105(NetReader r) { byte level = r.ReadU8(); ushort sweep = r.ReadU16(); DungeonPartnerModel.Instance.ReplaceDungeons(level, sweep, r.ReadArray(x => new DungeonPartnerModel.DungeonEntry(x.ReadU32(), x.ReadU8()))); }
        private void On61106(NetReader r) { byte level = r.ReadU8(); DungeonPartnerModel.Instance.ReplaceStageRewards(level, r.ReadArray(x => new DungeonPartnerModel.StageRewardEntry(x.ReadU16(), x.ReadU8()))); }
        public override void Dispose() { DungeonPartnerModel.Instance.Reset(); base.Dispose(); }
    }
}
