using System.Collections.Generic;

namespace Shenxiao.Module.Core.GhostWalk
{
    public sealed class GhostWalkModel
    {
        public sealed class Server
        {
            public ushort Id { get; }
            public ushort Number { get; }
            public string Name { get; }
            public ushort OpenDay { get; }
            public ushort WorldLevel { get; }

            public Server(ushort id, ushort number, string name, ushort openDay, ushort worldLevel)
            {
                Id = id;
                Number = number;
                Name = name;
                OpenDay = openDay;
                WorldLevel = worldLevel;
            }
        }

        public static readonly GhostWalkModel Instance = new GhostWalkModel();

        private readonly List<Server> _servers = new List<Server>();
        private readonly IReadOnlyList<Server> _readOnlyServers;

        private GhostWalkModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
        }

        public byte State { get; private set; }
        public uint EndTime { get; private set; }
        public byte ServerModule { get; private set; }
        public uint GroupId { get; private set; }
        public ushort AverageWorldLevel { get; private set; }
        public bool HasData { get; private set; }
        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorArgs { get; private set; }
        public IReadOnlyList<Server> Servers => _readOnlyServers;

        public void Replace(byte state, uint endTime, byte serverModule, uint groupId, List<Server> servers, ushort averageWorldLevel)
        {
            State = state;
            EndTime = endTime;
            ServerModule = serverModule;
            GroupId = groupId;
            AverageWorldLevel = averageWorldLevel;
            _servers.Clear();

            if (servers != null)
            {
                _servers.AddRange(servers);
            }

            HasData = true;
        }

        public void SetError(uint code, string args)
        {
            HasError = true;
            LastErrorCode = code;
            LastErrorArgs = args;
        }

        public void Reset()
        {
            State = 0;
            EndTime = 0;
            ServerModule = 0;
            GroupId = 0;
            AverageWorldLevel = 0;
            _servers.Clear();
            HasData = false;
            HasError = false;
            LastErrorCode = 0;
            LastErrorArgs = null;
        }
    }
}
