using System.Collections.Generic;

namespace Shenxiao.Module.Core.SentientAct
{
    public sealed class SentientActModel
    {
        public sealed class ServerEntry
        {
            public long ServerId { get; }
            public long ServerNum { get; }
            public string Name { get; }
            public long WorldLevel { get; }

            public ServerEntry(long id, long num, string name, long world)
            {
                ServerId = id;
                ServerNum = num;
                Name = name;
                WorldLevel = world;
            }
        }

        public sealed class PortalEntry
        {
            public long PortalId { get; }
            public uint X { get; }
            public uint Y { get; }

            public PortalEntry(long id, uint x, uint y)
            {
                PortalId = id;
                X = x;
                Y = y;
            }
        }

        public sealed class MonsterProgressSnapshot
        {
            public uint WaveNum { get; }
            public uint DeadMonNum { get; }
            public uint MonNum { get; }

            public MonsterProgressSnapshot(uint waveNum, uint deadMonNum, uint monNum)
            {
                WaveNum = waveNum;
                DeadMonNum = deadMonNum;
                MonNum = monNum;
            }
        }

        public sealed class PortalRemovedSnapshot
        {
            public long PortalId { get; }

            public PortalRemovedSnapshot(long portalId)
            {
                PortalId = portalId;
            }
        }

        public static readonly SentientActModel Instance = new SentientActModel();

        public bool HasInfo { get; private set; }
        public byte State { get; private set; }
        public uint EndTime { get; private set; }
        public uint Mod { get; private set; }
        public uint GroupId { get; private set; }
        public uint NextStartTime { get; private set; }
        public long AvgLevel { get; private set; }
        public IReadOnlyList<ServerEntry> Servers { get; private set; } = new List<ServerEntry>().AsReadOnly();
        public bool HasPortals { get; private set; }
        public IReadOnlyList<PortalEntry> Portals { get; private set; } = new List<PortalEntry>().AsReadOnly();
        public bool HasPortalRemoved { get; private set; }
        public PortalRemovedSnapshot LastPortalRemoved { get; private set; }
        public bool HasCounts { get; private set; }
        public uint AssistNum { get; private set; }
        public uint EnterNum { get; private set; }
        public bool HasMonsterProgress { get; private set; }
        public MonsterProgressSnapshot LastMonsterProgress { get; private set; }

        public void ReplaceInfo(byte s, uint e, uint mod, uint g, uint n, List<ServerEntry> servers, long avg)
        {
            State = s;
            EndTime = e;
            Mod = mod;
            GroupId = g;
            NextStartTime = n;
            Servers = (servers ?? new List<ServerEntry>()).AsReadOnly();
            AvgLevel = avg;
            HasInfo = true;
        }

        public void ReplacePortals(List<PortalEntry> portals)
        {
            Portals = (portals ?? new List<PortalEntry>()).AsReadOnly();
            HasPortals = true;
        }

        public void ReplacePortalRemoved(long portalId)
        {
            LastPortalRemoved = new PortalRemovedSnapshot(portalId);
            HasPortalRemoved = true;
        }

        public void ReplaceCounts(uint assist, uint enter)
        {
            AssistNum = assist;
            EnterNum = enter;
            HasCounts = true;
        }

        public void ReplaceMonsterProgress(uint waveNum, uint deadMonNum, uint monNum)
        {
            LastMonsterProgress = new MonsterProgressSnapshot(waveNum, deadMonNum, monNum);
            HasMonsterProgress = true;
        }

        public void Reset()
        {
            HasInfo = HasPortals = HasPortalRemoved = HasCounts = false;
            HasMonsterProgress = false;
            State = 0;
            EndTime = Mod = GroupId = NextStartTime = AssistNum = EnterNum = 0;
            AvgLevel = 0;
            Servers = new List<ServerEntry>().AsReadOnly();
            Portals = new List<PortalEntry>().AsReadOnly();
            LastPortalRemoved = null;
            LastMonsterProgress = null;
        }
    }
}
