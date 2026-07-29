using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.GuildFight
{
    /// <summary>
    /// 领地战 506xx 原始状态模型。全量包保留线序与重复项；50606/50607 另按老端语义
    /// 只合并到 50604 已知的公会/据点字典，未知 id 不凭空创建。
    /// </summary>
    public sealed class GuildFightModel
    {
        public sealed class BattleGuildEntry
        {
            public ulong GuildId { get; }
            public string GuildName { get; }
            public ushort ServerId { get; }
            public ushort ServerNumber { get; }
            public uint Score { get; }
            public IReadOnlyList<uint> OwnList { get; }

            public BattleGuildEntry(ulong guildId, string guildName, ushort serverId,
                ushort serverNumber, uint score, IReadOnlyList<uint> ownList)
            {
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                ServerId = serverId;
                ServerNumber = serverNumber;
                Score = score;
                OwnList = Freeze(ownList);
            }

            public BattleGuildEntry WithCurrent(uint score, IReadOnlyList<uint> ownList) =>
                new BattleGuildEntry(GuildId, GuildName, ServerId, ServerNumber, score, ownList);
        }

        public sealed class BattleOwnEntry
        {
            public byte Type { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }
            public uint MonsterId { get; }
            public uint Hp { get; }
            public uint HpLimit { get; }

            public BattleOwnEntry(byte type, ulong guildId, string guildName, uint monsterId,
                uint hp, uint hpLimit)
            {
                Type = type;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                MonsterId = monsterId;
                Hp = hp;
                HpLimit = hpLimit;
            }
        }

        public sealed class GuildUpdateEntry
        {
            public ulong GuildId { get; }
            public uint Score { get; }
            public IReadOnlyList<uint> OwnList { get; }

            public GuildUpdateEntry(ulong guildId, uint score, IReadOnlyList<uint> ownList)
            {
                GuildId = guildId;
                Score = score;
                OwnList = Freeze(ownList);
            }
        }

        public sealed class ResultGuildEntry
        {
            public ulong GuildId { get; }
            public byte IsWin { get; }
            public string GuildName { get; }
            public ushort ServerId { get; }
            public ushort ServerNumber { get; }
            public uint Score { get; }
            public IReadOnlyList<uint> OwnList { get; }

            public ResultGuildEntry(ulong guildId, byte isWin, string guildName, ushort serverId,
                ushort serverNumber, uint score, IReadOnlyList<uint> ownList)
            {
                GuildId = guildId;
                IsWin = isWin;
                GuildName = guildName ?? string.Empty;
                ServerId = serverId;
                ServerNumber = serverNumber;
                Score = score;
                OwnList = Freeze(ownList);
            }
        }

        public sealed class KillStreakSnapshot
        {
            public ushort AttackerServerId { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public byte Sex { get; }
            public byte Realm { get; }
            public byte Career { get; }
            public byte Turn { get; }
            public ushort Level { get; }
            public string Picture { get; }
            public uint PictureVersion { get; }
            public uint ConsecutiveKills { get; }

            public KillStreakSnapshot(ushort attackerServerId, ulong roleId, string roleName,
                byte sex, byte realm, byte career, byte turn, ushort level, string picture,
                uint pictureVersion, uint consecutiveKills)
            {
                AttackerServerId = attackerServerId;
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                Sex = sex;
                Realm = realm;
                Career = career;
                Turn = turn;
                Level = level;
                Picture = picture ?? string.Empty;
                PictureVersion = pictureVersion;
                ConsecutiveKills = consecutiveKills;
            }
        }

        public sealed class WarEntry
        {
            public uint TerritoryId { get; }
            public ulong AttackerGuildId { get; }
            public string AttackerGuildName { get; }
            public ushort AttackerServerId { get; }
            public ushort AttackerServerNumber { get; }
            public ulong DefenderGuildId { get; }
            public string DefenderGuildName { get; }
            public ushort DefenderServerId { get; }
            public ushort DefenderServerNumber { get; }
            public ulong WinnerGuildId { get; }

            public WarEntry(uint territoryId, ulong attackerGuildId, string attackerGuildName,
                ushort attackerServerId, ushort attackerServerNumber, ulong defenderGuildId,
                string defenderGuildName, ushort defenderServerId, ushort defenderServerNumber,
                ulong winnerGuildId)
            {
                TerritoryId = territoryId;
                AttackerGuildId = attackerGuildId;
                AttackerGuildName = attackerGuildName ?? string.Empty;
                AttackerServerId = attackerServerId;
                AttackerServerNumber = attackerServerNumber;
                DefenderGuildId = defenderGuildId;
                DefenderGuildName = defenderGuildName ?? string.Empty;
                DefenderServerId = defenderServerId;
                DefenderServerNumber = defenderServerNumber;
                WinnerGuildId = winnerGuildId;
            }
        }

        public sealed class ServerEntry
        {
            public ushort ServerId { get; }
            public ushort ServerNumber { get; }
            public string ServerName { get; }
            public ushort WorldLevel { get; }

            public ServerEntry(ushort serverId, ushort serverNumber, string serverName,
                ushort worldLevel)
            {
                ServerId = serverId;
                ServerNumber = serverNumber;
                ServerName = serverName ?? string.Empty;
                WorldLevel = worldLevel;
            }
        }

        public static readonly GuildFightModel Instance = new GuildFightModel();

        private readonly Dictionary<ulong, BattleGuildEntry> _currentGuilds =
            new Dictionary<ulong, BattleGuildEntry>();
        private readonly Dictionary<uint, BattleOwnEntry> _currentOwns =
            new Dictionary<uint, BattleOwnEntry>();
        private readonly IReadOnlyDictionary<ulong, BattleGuildEntry> _readOnlyCurrentGuilds;
        private readonly IReadOnlyDictionary<uint, BattleOwnEntry> _readOnlyCurrentOwns;

        private GuildFightModel()
        {
            _readOnlyCurrentGuilds = new ReadOnlyDictionary<ulong, BattleGuildEntry>(_currentGuilds);
            _readOnlyCurrentOwns = new ReadOnlyDictionary<uint, BattleOwnEntry>(_currentOwns);
            Reset();
        }

        public bool HasState { get; private set; }
        public byte WarState { get; private set; }
        public uint ReadyTime { get; private set; }
        public uint StartTime { get; private set; }
        public uint EndTime { get; private set; }

        public bool HasOverview { get; private set; }
        public byte OverviewType { get; private set; }
        public ulong WinnerGuildId { get; private set; }
        public ushort WinnerServerId { get; private set; }
        public ushort WinNumber { get; private set; }
        public byte RewardType { get; private set; }
        public ushort RewardKey { get; private set; }
        public ulong RewardOwnerRoleId { get; private set; }

        public bool HasEnterResult { get; private set; }
        public uint EnterResultCode { get; private set; }
        public byte EnterResultType { get; private set; }

        public bool HasBattle { get; private set; }
        public uint BattleTerritoryId { get; private set; }
        public uint BattleEndTime { get; private set; }
        public uint BattleRoleScore { get; private set; }
        public IReadOnlyList<BattleGuildEntry> BattleGuilds { get; private set; }
        public IReadOnlyList<byte> BattleStages { get; private set; }
        public IReadOnlyList<BattleOwnEntry> BattleOwns { get; private set; }
        public IReadOnlyDictionary<ulong, BattleGuildEntry> CurrentGuildsById => _readOnlyCurrentGuilds;
        public IReadOnlyDictionary<uint, BattleOwnEntry> CurrentOwnsByMonsterId => _readOnlyCurrentOwns;

        public bool HasGuildUpdate { get; private set; }
        public IReadOnlyList<GuildUpdateEntry> LastGuildUpdate { get; private set; }
        public bool HasOwnUpdate { get; private set; }
        public IReadOnlyList<BattleOwnEntry> LastOwnUpdate { get; private set; }

        public bool HasResult { get; private set; }
        public uint ResultTerritoryId { get; private set; }
        public byte ResultModeNumber { get; private set; }
        public IReadOnlyList<ResultGuildEntry> ResultGuilds { get; private set; }

        public bool HasRoleScore { get; private set; }
        public uint RoleScore { get; private set; }
        public bool HasConvene { get; private set; }
        public uint ConveneMonsterId { get; private set; }
        public bool HasKillStreak { get; private set; }
        public KillStreakSnapshot KillStreak { get; private set; }

        public bool HasRound { get; private set; }
        public byte Round { get; private set; }
        public uint RoundStartTime { get; private set; }
        public uint RoundEndTime { get; private set; }
        public bool HasWars { get; private set; }
        public IReadOnlyList<WarEntry> Wars { get; private set; }
        public bool HasServers { get; private set; }
        public byte ModeNumber { get; private set; }
        public ushort AverageWorldLevel { get; private set; }
        public IReadOnlyList<ServerEntry> Servers { get; private set; }

        public bool HasQualification { get; private set; }
        public byte Qualification { get; private set; }
        public byte IsTerritoryChosen { get; private set; }
        public bool HasQualificationUpdate { get; private set; }
        public byte LastQualificationUpdate { get; private set; }
        public bool HasWarListNotice { get; private set; }
        public byte WarListNotice { get; private set; }
        public bool HasTerritoryNotice { get; private set; }
        public uint TerritoryNoticeId { get; private set; }

        public void ReplaceState(byte warState, uint readyTime, uint startTime, uint endTime)
        {
            HasState = true;
            WarState = warState;
            ReadyTime = readyTime;
            StartTime = startTime;
            EndTime = endTime;
        }

        public void ReplaceOverview(byte type, ulong winnerGuildId, ushort serverId,
            ushort winNumber, byte rewardType, ushort rewardKey, ulong rewardOwnerRoleId)
        {
            HasOverview = true;
            OverviewType = type;
            WinnerGuildId = winnerGuildId;
            WinnerServerId = serverId;
            WinNumber = winNumber;
            RewardType = rewardType;
            RewardKey = rewardKey;
            RewardOwnerRoleId = rewardOwnerRoleId;
        }

        public void ReplaceEnterResult(uint errorCode, byte type)
        {
            HasEnterResult = true;
            EnterResultCode = errorCode;
            EnterResultType = type;
        }

        public void ReplaceBattle(uint territoryId, uint endTime, uint roleScore,
            IReadOnlyList<BattleGuildEntry> guilds, IReadOnlyList<byte> stages,
            IReadOnlyList<BattleOwnEntry> owns)
        {
            BattleTerritoryId = territoryId;
            BattleEndTime = endTime;
            BattleRoleScore = roleScore;
            BattleGuilds = Freeze(guilds);
            BattleStages = Freeze(stages);
            BattleOwns = Freeze(owns);
            _currentGuilds.Clear();
            foreach (BattleGuildEntry entry in BattleGuilds) _currentGuilds[entry.GuildId] = entry;
            _currentOwns.Clear();
            foreach (BattleOwnEntry entry in BattleOwns) _currentOwns[entry.MonsterId] = entry;
            HasBattle = true;
            HasRoleScore = true;
            RoleScore = roleScore;
        }

        public void ApplyGuildUpdate(IReadOnlyList<GuildUpdateEntry> entries)
        {
            LastGuildUpdate = Freeze(entries);
            foreach (GuildUpdateEntry entry in LastGuildUpdate)
            {
                if (_currentGuilds.TryGetValue(entry.GuildId, out BattleGuildEntry oldEntry))
                    _currentGuilds[entry.GuildId] = oldEntry.WithCurrent(entry.Score, entry.OwnList);
            }
            HasGuildUpdate = true;
        }

        public void ApplyOwnUpdate(IReadOnlyList<BattleOwnEntry> entries)
        {
            LastOwnUpdate = Freeze(entries);
            foreach (BattleOwnEntry entry in LastOwnUpdate)
                if (_currentOwns.ContainsKey(entry.MonsterId)) _currentOwns[entry.MonsterId] = entry;
            HasOwnUpdate = true;
        }

        public void ReplaceResult(uint territoryId, byte modeNumber,
            IReadOnlyList<ResultGuildEntry> guilds)
        {
            HasResult = true;
            ResultTerritoryId = territoryId;
            ResultModeNumber = modeNumber;
            ResultGuilds = Freeze(guilds);
        }

        public void ReplaceRoleScore(uint roleScore)
        {
            HasRoleScore = true;
            RoleScore = roleScore;
        }

        public void ReplaceConvene(uint monsterId)
        {
            HasConvene = true;
            ConveneMonsterId = monsterId;
        }

        public void ReplaceKillStreak(KillStreakSnapshot snapshot)
        {
            HasKillStreak = true;
            KillStreak = snapshot;
        }

        public void ReplaceRound(byte round, uint startTime, uint endTime)
        {
            HasRound = true;
            Round = round;
            RoundStartTime = startTime;
            RoundEndTime = endTime;
        }

        public void ReplaceWars(IReadOnlyList<WarEntry> wars)
        {
            HasWars = true;
            Wars = Freeze(wars);
        }

        public void ReplaceServers(byte modeNumber, ushort averageWorldLevel,
            IReadOnlyList<ServerEntry> servers)
        {
            HasServers = true;
            ModeNumber = modeNumber;
            AverageWorldLevel = averageWorldLevel;
            Servers = Freeze(servers);
        }

        public void ReplaceQualification(byte qualification, byte isTerritoryChosen)
        {
            HasQualification = true;
            Qualification = qualification;
            IsTerritoryChosen = isTerritoryChosen;
        }

        public void ApplyQualificationUpdate(byte qualification)
        {
            HasQualificationUpdate = true;
            LastQualificationUpdate = qualification;
            if (HasQualification) Qualification = qualification;
        }

        public void ReplaceWarListNotice(byte value)
        {
            HasWarListNotice = true;
            WarListNotice = value;
        }

        public void ReplaceTerritoryNotice(uint territoryId)
        {
            HasTerritoryNotice = true;
            TerritoryNoticeId = territoryId;
        }

        public void Reset()
        {
            HasState = false;
            WarState = 0;
            ReadyTime = StartTime = EndTime = 0;
            HasOverview = false;
            OverviewType = 0;
            WinnerGuildId = 0;
            WinnerServerId = WinNumber = RewardKey = 0;
            RewardType = 0;
            RewardOwnerRoleId = 0;
            HasEnterResult = false;
            EnterResultCode = 0;
            EnterResultType = 0;
            HasBattle = false;
            BattleTerritoryId = BattleEndTime = BattleRoleScore = 0;
            BattleGuilds = Empty<BattleGuildEntry>();
            BattleStages = Empty<byte>();
            BattleOwns = Empty<BattleOwnEntry>();
            _currentGuilds.Clear();
            _currentOwns.Clear();
            HasGuildUpdate = false;
            LastGuildUpdate = Empty<GuildUpdateEntry>();
            HasOwnUpdate = false;
            LastOwnUpdate = Empty<BattleOwnEntry>();
            HasResult = false;
            ResultTerritoryId = 0;
            ResultModeNumber = 0;
            ResultGuilds = Empty<ResultGuildEntry>();
            HasRoleScore = false;
            RoleScore = 0;
            HasConvene = false;
            ConveneMonsterId = 0;
            HasKillStreak = false;
            KillStreak = null;
            HasRound = false;
            Round = 0;
            RoundStartTime = RoundEndTime = 0;
            HasWars = false;
            Wars = Empty<WarEntry>();
            HasServers = false;
            ModeNumber = 0;
            AverageWorldLevel = 0;
            Servers = Empty<ServerEntry>();
            HasQualification = false;
            Qualification = IsTerritoryChosen = 0;
            HasQualificationUpdate = false;
            LastQualificationUpdate = 0;
            HasWarListNotice = false;
            WarListNotice = 0;
            HasTerritoryNotice = false;
            TerritoryNoticeId = 0;
        }

        private static IReadOnlyList<T> Empty<T>() => Array.AsReadOnly(new T[0]);

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Empty<T>();
            T[] copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }
    }
}
