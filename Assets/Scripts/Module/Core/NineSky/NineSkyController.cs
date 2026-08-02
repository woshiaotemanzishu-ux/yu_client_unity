using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.NineSky
{
    public sealed class NineSkyController : BaseController
    {
        public static readonly NineSkyController Instance = new NineSkyController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private NineSkyController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.NINE_SKY_INFO, On13500);
            RegisterProtocal(Proto.NINE_SKY_BATTLE_INFO, On13503);
            RegisterProtocal(Proto.NINE_SKY_FLAG_INFO, On13504);
            RegisterProtocal(Proto.NINE_SKY_SETTLEMENT, On13507);
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.NINE_SKY_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.NINE_SKY_INFO);
        }

        public void RequestBattleInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.NINE_SKY_BATTLE_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.NINE_SKY_BATTLE_INFO);
        }

        private void On13500(NetReader r)
        {
            byte state = r.ReadU8();
            uint leftTime = r.ReadU32();
            uint mod = r.ReadU32();
            uint groupId = r.ReadU32();
            int count = r.ReadU16();
            var servers = new List<NineSkyModel.ServerEntry>(count);

            for (int i = 0; i < count; i++)
            {
                ulong serverId = unchecked((ulong)r.ReadU64());
                ulong serverNumber = unchecked((ulong)r.ReadU64());
                string serverName = r.ReadString();
                ulong worldLevel = unchecked((ulong)r.ReadU64());
                servers.Add(new NineSkyModel.ServerEntry(serverId, serverNumber, serverName, worldLevel));
            }

            NineSkyModel.Instance.Replace(state, leftTime, mod, groupId, servers, unchecked((ulong)r.ReadU64()));
        }

        private void On13503(NetReader r)
        {
            NineSkyModel.Instance.ReplaceBattleInfo(r.ReadU8(), r.ReadU8(), r.ReadU32(), r.ReadU16(), r.ReadU32(), r.ReadU16(), r.ReadString());
        }

        private void On13504(NetReader r)
        {
            NineSkyModel.Instance.ReplaceFlagInfo(r.ReadU8(), r.ReadU16(), unchecked((ulong)r.ReadU64()), r.ReadString(), r.ReadU32());
        }

        private void On13507(NetReader r)
        {
            byte maxFloor = r.ReadU8();
            List<NineSkyModel.ObjectRewardEntry> rewards = r.ReadArray(ReadObjectReward);
            ushort firstServerNumber = r.ReadU16();
            string firstPlayer = r.ReadString();
            List<NineSkyModel.FloorOwnerEntry> floorOwners = r.ReadArray(ReadFloorOwner);
            NineSkyModel.Instance.ReplaceSettlement(new NineSkyModel.SettlementSnapshot(
                maxFloor,
                rewards,
                firstServerNumber,
                firstPlayer,
                floorOwners));
        }

        private static NineSkyModel.ObjectRewardEntry ReadObjectReward(NetReader r) =>
            new NineSkyModel.ObjectRewardEntry(r.ReadU8(), r.ReadU32(), r.ReadU32());

        private static NineSkyModel.FloorOwnerEntry ReadFloorOwner(NetReader r) =>
            new NineSkyModel.FloorOwnerEntry(r.ReadU8(), r.ReadU16(), r.ReadString());

        public override void Dispose()
        {
            NineSkyModel.Instance.Reset();
            base.Dispose();
        }
    }
}
