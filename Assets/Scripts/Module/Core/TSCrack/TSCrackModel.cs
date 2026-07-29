using System.Collections.Generic;

namespace Shenxiao.Module.Core.TSCrack
{
    /// <summary>时空圣痕原始协议快照；各协议切片互不派生、互不清理。</summary>
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

        public sealed class CastleServerEntry
        {
            public uint ServerNumber { get; }
            public string ServerName { get; }
            public uint Value { get; }

            public CastleServerEntry(uint serverNumber, string serverName, uint value)
            {
                ServerNumber = serverNumber;
                ServerName = serverName;
                Value = value;
            }
        }

        public sealed class CastleRoleEntry
        {
            public uint ServerNumber { get; }
            public string RoleName { get; }
            public uint Value { get; }
            public byte IsOccupying { get; }

            public CastleRoleEntry(uint serverNumber, string roleName, uint value, byte isOccupying)
            {
                ServerNumber = serverNumber;
                RoleName = roleName;
                Value = value;
                IsOccupying = isOccupying;
            }
        }

        public sealed class CastleEntry
        {
            public ushort CastleId { get; }
            public uint BaseServerNumber { get; }
            public uint NeedValue { get; }
            public uint ServerNumber { get; }
            public string ServerName { get; }
            public IReadOnlyList<CastleServerEntry> Servers { get; }
            public IReadOnlyList<CastleRoleEntry> Roles { get; }
            public ushort RoleCount { get; }
            public ushort ProviderCount { get; }

            public CastleEntry(
                ushort castleId,
                uint baseServerNumber,
                uint needValue,
                uint serverNumber,
                string serverName,
                List<CastleServerEntry> servers,
                List<CastleRoleEntry> roles,
                ushort roleCount,
                ushort providerCount)
            {
                CastleId = castleId;
                BaseServerNumber = baseServerNumber;
                NeedValue = needValue;
                ServerNumber = serverNumber;
                ServerName = serverName;
                var serverCopy = servers == null ? new List<CastleServerEntry>() : new List<CastleServerEntry>(servers);
                var roleCopy = roles == null ? new List<CastleRoleEntry>() : new List<CastleRoleEntry>(roles);
                Servers = serverCopy.AsReadOnly();
                Roles = roleCopy.AsReadOnly();
                RoleCount = roleCount;
                ProviderCount = providerCount;
            }
        }

        public sealed class DailyActivityEntry
        {
            public ushort ModuleId { get; }
            public ushort SubModuleId { get; }
            public uint Value { get; }

            public DailyActivityEntry(ushort moduleId, ushort subModuleId, uint value)
            {
                ModuleId = moduleId;
                SubModuleId = subModuleId;
                Value = value;
            }
        }

        public sealed class DailyRewardEntry
        {
            public byte Stage { get; }
            public byte Status { get; }

            public DailyRewardEntry(byte stage, byte status)
            {
                Stage = stage;
                Status = status;
            }
        }

        public sealed class SeasonGoalEntry
        {
            public ushort GoalId { get; }
            public uint Value { get; }
            public byte Status { get; }

            public SeasonGoalEntry(ushort goalId, uint value, byte status)
            {
                GoalId = goalId;
                Value = value;
                Status = status;
            }
        }

        public sealed class RankEntry
        {
            public uint ServerNumber { get; }
            /// <summary>u64 wire bit-pattern；值超过 long.MaxValue 时按 unchecked long 保存。</summary>
            public long RoleId { get; }
            public string RoleName { get; }
            public uint Value { get; }

            public RankEntry(uint serverNumber, long roleId, string roleName, uint value)
            {
                ServerNumber = serverNumber;
                RoleId = roleId;
                RoleName = roleName;
                Value = value;
            }
        }

        public static readonly TSCrackModel Instance = new TSCrackModel();

        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly List<CastleEntry> _mainCastles = new List<CastleEntry>();
        private readonly Dictionary<ushort, CastleEntry> _castleDetails = new Dictionary<ushort, CastleEntry>();
        private readonly List<DailyActivityEntry> _dailyActivities = new List<DailyActivityEntry>();
        private readonly List<DailyRewardEntry> _dailyRewards = new List<DailyRewardEntry>();
        private readonly List<SeasonGoalEntry> _seasonGoals = new List<SeasonGoalEntry>();
        private readonly List<RankEntry> _personalRanks = new List<RankEntry>();

        private readonly IReadOnlyList<ServerEntry> _readOnlyServers;
        private readonly IReadOnlyList<CastleEntry> _readOnlyMainCastles;
        private readonly IReadOnlyList<DailyActivityEntry> _readOnlyDailyActivities;
        private readonly IReadOnlyList<DailyRewardEntry> _readOnlyDailyRewards;
        private readonly IReadOnlyList<SeasonGoalEntry> _readOnlySeasonGoals;
        private readonly IReadOnlyList<RankEntry> _readOnlyPersonalRanks;

        private TSCrackModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
            _readOnlyMainCastles = _mainCastles.AsReadOnly();
            _readOnlyDailyActivities = _dailyActivities.AsReadOnly();
            _readOnlyDailyRewards = _dailyRewards.AsReadOnly();
            _readOnlySeasonGoals = _seasonGoals.AsReadOnly();
            _readOnlyPersonalRanks = _personalRanks.AsReadOnly();
        }

        // 20411
        public byte Status { get; private set; }
        public bool HasData { get; private set; }
        public IReadOnlyList<ServerEntry> Servers => _readOnlyServers;

        // 20401
        public bool HasMainData { get; private set; }
        public uint MyValue { get; private set; }
        public uint MyServerValue { get; private set; }
        public IReadOnlyList<CastleEntry> MainCastles => _readOnlyMainCastles;

        // 20402
        public bool HasAnyCastleDetail => _castleDetails.Count != 0;
        public int CastleDetailCount => _castleDetails.Count;
        public IEnumerable<CastleEntry> CastleDetails => _castleDetails.Values;

        // 20404
        public bool HasDailyActivities { get; private set; }
        public IReadOnlyList<DailyActivityEntry> DailyActivities => _readOnlyDailyActivities;

        // 20405
        public bool HasDailyRewards { get; private set; }
        public uint DailyValue { get; private set; }
        public uint TotalValue { get; private set; }
        public IReadOnlyList<DailyRewardEntry> DailyRewards => _readOnlyDailyRewards;

        // 20407
        public bool HasSeasonGoals { get; private set; }
        public IReadOnlyList<SeasonGoalEntry> SeasonGoals => _readOnlySeasonGoals;

        // 20409
        public bool HasPersonalRanks { get; private set; }
        public IReadOnlyList<RankEntry> PersonalRanks => _readOnlyPersonalRanks;

        // 20410
        public bool HasCurrentCastle { get; private set; }
        public uint CurrentCastleId { get; private set; }

        public void Replace(byte status, List<ServerEntry> servers)
        {
            Status = status;
            ReplaceList(_servers, servers);
            HasData = true;
        }

        public void ReplaceMain(uint myValue, uint myServerValue, List<CastleEntry> castles)
        {
            MyValue = myValue;
            MyServerValue = myServerValue;
            ReplaceList(_mainCastles, castles);
            HasMainData = true;
        }

        public void ReplaceCastleDetail(CastleEntry castle)
        {
            if (castle == null) return;
            _castleDetails[castle.CastleId] = castle;
        }

        public bool TryGetCastleDetail(ushort castleId, out CastleEntry castle)
        {
            return _castleDetails.TryGetValue(castleId, out castle);
        }

        public void ReplaceDailyActivities(List<DailyActivityEntry> entries)
        {
            ReplaceList(_dailyActivities, entries);
            HasDailyActivities = true;
        }

        public void ReplaceDailyRewards(uint value, uint totalValue, List<DailyRewardEntry> entries)
        {
            DailyValue = value;
            TotalValue = totalValue;
            ReplaceList(_dailyRewards, entries);
            HasDailyRewards = true;
        }

        public void ReplaceSeasonGoals(List<SeasonGoalEntry> entries)
        {
            ReplaceList(_seasonGoals, entries);
            HasSeasonGoals = true;
        }

        public void ReplacePersonalRanks(List<RankEntry> entries)
        {
            ReplaceList(_personalRanks, entries);
            HasPersonalRanks = true;
        }

        public void ReplaceCurrentCastle(uint castleId)
        {
            CurrentCastleId = castleId;
            HasCurrentCastle = true;
        }

        public void Reset()
        {
            Status = 0;
            _servers.Clear();
            HasData = false;

            MyValue = 0;
            MyServerValue = 0;
            _mainCastles.Clear();
            HasMainData = false;
            _castleDetails.Clear();

            _dailyActivities.Clear();
            HasDailyActivities = false;

            DailyValue = 0;
            TotalValue = 0;
            _dailyRewards.Clear();
            HasDailyRewards = false;

            _seasonGoals.Clear();
            HasSeasonGoals = false;
            _personalRanks.Clear();
            HasPersonalRanks = false;

            CurrentCastleId = 0;
            HasCurrentCastle = false;
        }

        private static void ReplaceList<T>(List<T> target, List<T> source)
        {
            target.Clear();
            if (source != null) target.AddRange(source);
        }
    }
}
