using System.Collections.Generic;

namespace Shenxiao.Module.Core.NineSky
{
    public sealed class NineSkyModel
    {
        public sealed class ServerEntry
        {
            public ulong ServerId { get; }
            public ulong ServerNumber { get; }
            public string ServerName { get; }
            public ulong WorldLevel { get; }

            public ServerEntry(ulong id, ulong number, string name, ulong worldLevel)
            {
                ServerId = id;
                ServerNumber = number;
                ServerName = name;
                WorldLevel = worldLevel;
            }
        }

        public sealed class ObjectRewardEntry
        {
            public byte Type { get; }
            public uint TypeId { get; }
            public uint Num { get; }

            public ObjectRewardEntry(byte type, uint typeId, uint num)
            {
                Type = type;
                TypeId = typeId;
                Num = num;
            }
        }

        public sealed class FloorOwnerEntry
        {
            public byte Index { get; }
            public ushort ServerNumber { get; }
            public string RoleName { get; }

            public FloorOwnerEntry(byte index, ushort serverNumber, string roleName)
            {
                Index = index;
                ServerNumber = serverNumber;
                RoleName = roleName;
            }
        }

        public sealed class SettlementSnapshot
        {
            public byte MaxFloor { get; }
            public IReadOnlyList<ObjectRewardEntry> Rewards { get; }
            public ushort FirstServerNumber { get; }
            public string FirstPlayer { get; }
            public IReadOnlyList<FloorOwnerEntry> FloorOwners { get; }

            public SettlementSnapshot(
                byte maxFloor,
                List<ObjectRewardEntry> rewards,
                ushort firstServerNumber,
                string firstPlayer,
                List<FloorOwnerEntry> floorOwners)
            {
                MaxFloor = maxFloor;
                Rewards = (rewards == null ? new List<ObjectRewardEntry>() : new List<ObjectRewardEntry>(rewards)).AsReadOnly();
                FirstServerNumber = firstServerNumber;
                FirstPlayer = firstPlayer;
                FloorOwners = (floorOwners == null ? new List<FloorOwnerEntry>() : new List<FloorOwnerEntry>(floorOwners)).AsReadOnly();
            }
        }

        public static readonly NineSkyModel Instance = new NineSkyModel();

        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly IReadOnlyList<ServerEntry> _readOnlyServers;

        private NineSkyModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
        }

        public byte State { get; private set; }
        public uint LeftTime { get; private set; }
        public uint Mod { get; private set; }
        public uint GroupId { get; private set; }
        public ulong AverageLevel { get; private set; }
        public bool HasData { get; private set; }
        public IReadOnlyList<ServerEntry> Servers => _readOnlyServers;
        public bool HasBattleInfo { get; private set; }
        public byte CurFloor { get; private set; }
        public byte MaxFloor { get; private set; }
        public uint BattleLeftTime { get; private set; }
        public ushort KillNum { get; private set; }
        public uint Score { get; private set; }
        public ushort FirstServerNum { get; private set; }
        public string FirstPlayer { get; private set; }
        public bool HasFlagInfo { get; private set; }
        public byte FlagIndex { get; private set; }
        public ushort FlagServerNum { get; private set; }
        public ulong FlagRoleId { get; private set; }
        public string FlagRoleName { get; private set; }
        public uint FlagLeftTime { get; private set; }
        public bool HasSettlement => Settlement != null;
        public SettlementSnapshot Settlement { get; private set; }

        public void Replace(byte state, uint leftTime, uint mod, uint groupId, List<ServerEntry> servers, ulong averageLevel)
        {
            State = state;
            LeftTime = leftTime;
            Mod = mod;
            GroupId = groupId;
            AverageLevel = averageLevel;
            _servers.Clear();

            if (servers != null)
            {
                _servers.AddRange(servers);
            }

            HasData = true;
        }

        public void ReplaceBattleInfo(byte curFloor, byte maxFloor, uint leftTime, ushort killNum, uint score, ushort firstServerNum, string firstPlayer)
        {
            CurFloor = curFloor; MaxFloor = maxFloor; BattleLeftTime = leftTime; KillNum = killNum; Score = score; FirstServerNum = firstServerNum; FirstPlayer = firstPlayer; HasBattleInfo = true;
        }

        public void ReplaceFlagInfo(byte index, ushort serverNum, ulong roleId, string roleName, uint leftTime)
        {
            FlagIndex = index; FlagServerNum = serverNum; FlagRoleId = roleId; FlagRoleName = roleName; FlagLeftTime = leftTime; HasFlagInfo = true;
        }

        public void ReplaceSettlement(SettlementSnapshot value)
        {
            Settlement = value;
        }

        public void Reset()
        {
            State = 0;
            LeftTime = 0;
            Mod = 0;
            GroupId = 0;
            AverageLevel = 0;
            _servers.Clear();
            HasData = false;
            CurFloor = 0; MaxFloor = 0; BattleLeftTime = 0; KillNum = 0; Score = 0; FirstServerNum = 0; FirstPlayer = null; HasBattleInfo = false;
            FlagIndex = 0; FlagServerNum = 0; FlagRoleId = 0; FlagRoleName = null; FlagLeftTime = 0; HasFlagInfo = false;
            Settlement = null;
        }
    }
}
