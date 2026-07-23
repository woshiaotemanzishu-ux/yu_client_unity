using System.Collections.Generic;

namespace Shenxiao.Module.Core.HolyBattle
{
    public sealed class HolyBattleModel
    {
        public sealed class ServerEntry
        {
            public uint ServerId { get; }
            public uint ServerNumber { get; }
            public string ServerName { get; }
            public uint Level { get; }

            public ServerEntry(uint serverId, uint serverNumber, string serverName, uint level)
            {
                ServerId = serverId;
                ServerNumber = serverNumber;
                ServerName = serverName;
                Level = level;
            }
        }

        public sealed class RewardEntry
        {
            public ushort Stage { get; }
            public byte Status { get; }

            public RewardEntry(ushort stage, byte status)
            {
                Stage = stage;
                Status = status;
            }
        }

        public sealed class RecordRoleEntry
        {
            public ulong RoleId { get; }
            public byte Rank { get; }
            public uint ServerId { get; }
            public uint ServerNum { get; }
            public string Name { get; }
            public uint Point { get; }
            public ushort Kill { get; }
            public ushort Assists { get; }

            public RecordRoleEntry(ulong roleId, byte rank, uint serverId, uint serverNum, string name, uint point, ushort kill, ushort assists)
            {
                RoleId = roleId; Rank = rank; ServerId = serverId; ServerNum = serverNum; Name = name; Point = point; Kill = kill; Assists = assists;
            }
        }

        public sealed class RecordGroupEntry
        {
            public byte GroupId { get; }
            public byte TowerNum { get; }
            public uint Point { get; }
            public byte Rank { get; }
            public IReadOnlyList<RecordRoleEntry> Roles { get; }

            public RecordGroupEntry(byte groupId, byte towerNum, uint point, byte rank, List<RecordRoleEntry> roles)
            {
                GroupId = groupId; TowerNum = towerNum; Point = point; Rank = rank;
                Roles = new List<RecordRoleEntry>(roles ?? new List<RecordRoleEntry>()).AsReadOnly();
            }
        }

        public static readonly HolyBattleModel Instance = new HolyBattleModel();

        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly IReadOnlyList<ServerEntry> _readOnlyServers;
        private readonly List<RewardEntry> _rewards = new List<RewardEntry>();
        private readonly IReadOnlyList<RewardEntry> _readOnlyRewards;
        private readonly List<RecordGroupEntry> _recordStats = new List<RecordGroupEntry>();
        private readonly IReadOnlyList<RecordGroupEntry> _readOnlyRecordStats;

        private HolyBattleModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
            _readOnlyRewards = _rewards.AsReadOnly();
            _readOnlyRecordStats = _recordStats.AsReadOnly();
        }

        public byte Mod { get; private set; }
        public byte Status { get; private set; }
        public uint EndTime { get; private set; }
        public bool HasData { get; private set; }
        public bool HasExperience { get; private set; }
        public ulong AllExperience { get; private set; }
        public bool HasScore { get; private set; }
        public uint Point { get; private set; }
        public IReadOnlyList<ServerEntry> Servers => _readOnlyServers;
        public IReadOnlyList<RewardEntry> Rewards => _readOnlyRewards;
        public bool HasRecordStats { get; private set; }
        public IReadOnlyList<RecordGroupEntry> RecordStats => _readOnlyRecordStats;

        public void Replace(byte mod, byte status, uint endTime, List<ServerEntry> servers)
        {
            Mod = mod;
            Status = status;
            EndTime = endTime;
            _servers.Clear();

            if (servers != null)
            {
                _servers.AddRange(servers);
            }

            HasData = true;
        }

        public void ReplaceExperience(ulong allExperience)
        {
            AllExperience = allExperience;
            HasExperience = true;
        }

        public void ReplaceScore(uint point, List<RewardEntry> rewards)
        {
            Point = point;
            _rewards.Clear();
            if (rewards != null)
            {
                _rewards.AddRange(rewards);
            }

            HasScore = true;
        }

        public void ReplaceRecordStats(List<RecordGroupEntry> groups)
        {
            _recordStats.Clear();
            if (groups != null) _recordStats.AddRange(groups);
            HasRecordStats = true;
        }

        public void Reset()
        {
            Mod = 0;
            Status = 0;
            EndTime = 0;
            _servers.Clear();
            HasData = false;
            HasExperience = false;
            AllExperience = 0;
            HasScore = false;
            Point = 0;
            _rewards.Clear();
            _recordStats.Clear();
            HasRecordStats = false;
        }
    }
}
