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

        public sealed class BuffEntry
        {
            public ushort AttrId { get; }
            public uint Value { get; }
            public BuffEntry(ushort attrId, uint value) { AttrId = attrId; Value = value; }
        }
        public sealed class MonsterEntry
        {
            public uint MonAuto { get; }
            public uint MonCfgId { get; }
            public uint Hp { get; }
            public uint HpAll { get; }
            public byte GroupId { get; }

            public MonsterEntry(uint monAuto, uint monCfgId, uint hp, uint hpAll, byte groupId)
            {
                MonAuto = monAuto;
                MonCfgId = monCfgId;
                Hp = hp;
                HpAll = hpAll;
                GroupId = groupId;
            }
        }

        public static readonly HolyBattleModel Instance = new HolyBattleModel();

        private readonly List<ServerEntry> _servers = new List<ServerEntry>();
        private readonly IReadOnlyList<ServerEntry> _readOnlyServers;
        private readonly List<RewardEntry> _rewards = new List<RewardEntry>();
        private readonly IReadOnlyList<RewardEntry> _readOnlyRewards;
        private readonly List<RecordGroupEntry> _recordStats = new List<RecordGroupEntry>();
        private readonly IReadOnlyList<RecordGroupEntry> _readOnlyRecordStats;
        private readonly List<BuffEntry> _buffs = new List<BuffEntry>();
        private readonly IReadOnlyList<BuffEntry> _readOnlyBuffs;
        private readonly Dictionary<uint, MonsterEntry> _monsters = new Dictionary<uint, MonsterEntry>();
        private readonly IReadOnlyDictionary<uint, MonsterEntry> _readOnlyMonsters;

        private HolyBattleModel()
        {
            _readOnlyServers = _servers.AsReadOnly();
            _readOnlyRewards = _rewards.AsReadOnly();
            _readOnlyRecordStats = _recordStats.AsReadOnly();
            _readOnlyBuffs = _buffs.AsReadOnly();
            _readOnlyMonsters = new System.Collections.ObjectModel.ReadOnlyDictionary<uint, MonsterEntry>(_monsters);
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
        public bool HasPhaseTime { get; private set; }
        public byte PhaseStatus { get; private set; }
        public uint PhaseEndTime { get; private set; }
        public bool HasFightState { get; private set; }
        public ushort FightPoint { get; private set; }
        public ushort SingleRank { get; private set; }
        public byte GroupRank { get; private set; }
        public byte Anger { get; private set; }
        public uint AngerEnd { get; private set; }
        public IReadOnlyList<BuffEntry> Buffs => _readOnlyBuffs;
        public bool HasMonsterInfo { get; private set; }
        public IReadOnlyDictionary<uint, MonsterEntry> MonstersByCfgId => _readOnlyMonsters;
        public bool HasDeathInfo { get; private set; }
        public string DeathRoleName { get; private set; }
        public ulong DeathRoleId { get; private set; }
        public ushort DeathLevel { get; private set; }
        public ulong DeathPower { get; private set; }
        public uint DeathPictureVersion { get; private set; }
        public string DeathPicture { get; private set; }
        public uint DeathAnger { get; private set; }
        public uint DeathServerId { get; private set; }
        public byte DeathCareer { get; private set; }
        public byte DeathTurn { get; private set; }

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

        public void ReplacePhaseTime(byte status, uint endTime)
        {
            PhaseStatus = status;
            PhaseEndTime = endTime;
            HasPhaseTime = true;
        }

        public void ReplaceFightState(ushort point, ushort singleRank, byte groupRank, byte anger, uint angerEnd, List<BuffEntry> buffs)
        {
            FightPoint = point; SingleRank = singleRank; GroupRank = groupRank; Anger = anger; AngerEnd = angerEnd;
            _buffs.Clear(); if (buffs != null) _buffs.AddRange(buffs); HasFightState = true;
        }
        public void ApplyMonsterInfo(List<MonsterEntry> entries)
        {
            if (entries != null)
            {
                foreach (MonsterEntry entry in entries)
                {
                    if (entry.Hp == 0)
                    {
                        _monsters.Remove(entry.MonCfgId);
                    }
                    else
                    {
                        _monsters[entry.MonCfgId] = entry;
                    }
                }
            }

            HasMonsterInfo = true;
        }
        public void ReplaceDeathInfo(string roleName, ulong roleId, ushort level, ulong power, uint pictureVersion, string picture, uint anger, uint serverId, byte career, byte turn)
        {
            DeathRoleName = roleName;
            DeathRoleId = roleId;
            DeathLevel = level;
            DeathPower = power;
            DeathPictureVersion = pictureVersion;
            DeathPicture = picture;
            DeathAnger = anger;
            DeathServerId = serverId;
            DeathCareer = career;
            DeathTurn = turn;
            HasDeathInfo = true;
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
            HasPhaseTime = false;
            PhaseStatus = 0;
            PhaseEndTime = 0;
            HasFightState = false; FightPoint = 0; SingleRank = 0; GroupRank = 0; Anger = 0; AngerEnd = 0; _buffs.Clear();
            _monsters.Clear();
            HasMonsterInfo = false;
            HasDeathInfo = false;
            DeathRoleName = null;
            DeathRoleId = 0;
            DeathLevel = 0;
            DeathPower = 0;
            DeathPictureVersion = 0;
            DeathPicture = null;
            DeathAnger = 0;
            DeathServerId = 0;
            DeathCareer = 0;
            DeathTurn = 0;
        }
    }
}
