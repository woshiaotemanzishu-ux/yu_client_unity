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

        public static readonly HolyBattleModel Instance = new HolyBattleModel();

        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly IReadOnlyList<ServerEntry> _readOnlyServers;

        private HolyBattleModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
        }

        public byte Mod { get; private set; }
        public byte Status { get; private set; }
        public uint EndTime { get; private set; }
        public bool HasData { get; private set; }
        public bool HasExperience { get; private set; }
        public ulong AllExperience { get; private set; }
        public IReadOnlyList<ServerEntry> Servers => _readOnlyServers;

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

        public void Reset()
        {
            Mod = 0;
            Status = 0;
            EndTime = 0;
            _servers.Clear();
            HasData = false;
            HasExperience = false;
            AllExperience = 0;
        }
    }
}
