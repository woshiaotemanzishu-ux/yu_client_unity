using System.Collections.Generic;

namespace Shenxiao.Module.Core.KfStage
{
    /// <summary>10200 跨服分组基础快照；不承载 Cookie、ViewOrder、UI 或功能消费。</summary>
    public sealed class KfStageModel
    {
        public sealed class ServerEntry
        {
            public ushort ServerId { get; }
            public ushort ServerNum { get; }
            public string ServerName { get; }
            public ushort WorldLevel { get; }
            public ServerEntry(ushort serverId, ushort serverNum, string serverName, ushort worldLevel) { ServerId = serverId; ServerNum = serverNum; ServerName = serverName; WorldLevel = worldLevel; }
        }
        public sealed class ModuleEntry
        {
            public ushort ModuleId { get; }
            public byte Mod { get; }
            public ushort AverageLevel { get; }
            public IReadOnlyList<ushort> ServerIds { get; }
            public IReadOnlyList<ushort> NextServerIds { get; }
            public ModuleEntry(ushort moduleId, byte mod, ushort averageLevel, List<ushort> serverIds, List<ushort> nextServerIds) { ModuleId = moduleId; Mod = mod; AverageLevel = averageLevel; ServerIds = serverIds ?? new List<ushort>(); NextServerIds = nextServerIds ?? new List<ushort>(); }
        }
        public static readonly KfStageModel Instance = new KfStageModel();
        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly List<ModuleEntry> _modules = new List<ModuleEntry>();
        private KfStageModel() { }
        public uint OpenDay { get; private set; }
        public IReadOnlyList<ServerEntry> Servers => _servers;
        public IReadOnlyList<ModuleEntry> Modules => _modules;
        public bool HasData { get; private set; }
        public ServerEntry FindServer(ushort serverId) { for (int i = 0; i < _servers.Count; i++) if (_servers[i].ServerId == serverId) return _servers[i]; return null; }
        public ModuleEntry FindModule(ushort moduleId) { for (int i = 0; i < _modules.Count; i++) if (_modules[i].ModuleId == moduleId) return _modules[i]; return null; }
        public void ReplaceData(uint openDay, List<ServerEntry> servers, List<ModuleEntry> modules)
        {
            OpenDay = openDay; _servers.Clear(); _modules.Clear();
            if (servers != null) _servers.AddRange(servers);
            if (modules != null) _modules.AddRange(modules);
            HasData = true;
        }
        public void Reset() { OpenDay = 0; _servers.Clear(); _modules.Clear(); HasData = false; }
    }
}
