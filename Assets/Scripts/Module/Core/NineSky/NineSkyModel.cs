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

        public void Reset()
        {
            State = 0;
            LeftTime = 0;
            Mod = 0;
            GroupId = 0;
            AverageLevel = 0;
            _servers.Clear();
            HasData = false;
        }
    }
}
