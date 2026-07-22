using System.Collections.Generic;

namespace Shenxiao.Module.Core.TSCrack
{
    public sealed class TSCrackModel
    {
        public sealed class ServerEntry
        {
            public uint ServerNumber { get; }
            public string ServerName { get; }
            public ushort Level { get; }

            public ServerEntry(uint serverNumber, string serverName, ushort level)
            {
                ServerNumber = serverNumber;
                ServerName = serverName;
                Level = level;
            }
        }

        public static readonly TSCrackModel Instance = new TSCrackModel();

        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly IReadOnlyList<ServerEntry> _readOnlyServers;

        private TSCrackModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
        }

        public byte Status { get; private set; }
        public bool HasData { get; private set; }
        public IReadOnlyList<ServerEntry> Servers => _readOnlyServers;

        public void Replace(byte status, List<ServerEntry> servers)
        {
            Status = status;
            _servers.Clear();

            if (servers != null)
            {
                _servers.AddRange(servers);
            }

            HasData = true;
        }

        public void Reset()
        {
            Status = 0;
            _servers.Clear();
            HasData = false;
        }
    }
}
