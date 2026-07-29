using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.SeaHegemony
{
    /// <summary>
    /// 四海争霸186家族的原始读侧状态。查询快照、推送和写操作回执彼此隔离；
    /// 所有列表保留服务端wire顺序与重复项，只有18609按旧端语义增量合并怪物字典。
    /// </summary>
    public sealed class SeaHegemonyModel
    {
        public sealed class ObjectEntry
        {
            public byte Type { get; }
            public uint TypeId { get; }
            public uint Num { get; }

            public ObjectEntry(byte type, uint typeId, uint num)
            {
                Type = type;
                TypeId = typeId;
                Num = num;
            }
        }

        public sealed class InfoSnapshot
        {
            public uint Camp { get; }
            public uint ServerId { get; }
            public ushort ServerNumber { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }
            public string KingName { get; }
            public ulong Fight { get; }
            public ulong Count { get; }
            public ushort SelfLevel { get; }
            public byte RewardStatus { get; }

            public InfoSnapshot(uint camp, uint serverId, ushort serverNumber, ulong guildId,
                string guildName, string kingName, ulong fight, ulong count,
                ushort selfLevel, byte rewardStatus)
            {
                Camp = camp;
                ServerId = serverId;
                ServerNumber = serverNumber;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                KingName = kingName ?? string.Empty;
                Fight = fight;
                Count = count;
                SelfLevel = selfLevel;
                RewardStatus = rewardStatus;
            }
        }

        public sealed class GuardMember
        {
            public ushort SelfLevel { get; }
            public uint ServerId { get; }
            public ushort ServerNumber { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public ushort RoleLevel { get; }
            public string Picture { get; }
            public ushort PictureVersion { get; }
            public ulong Power { get; }

            public GuardMember(ushort selfLevel, uint serverId, ushort serverNumber, ulong roleId,
                string roleName, ushort roleLevel, string picture, ushort pictureVersion, ulong power)
            {
                SelfLevel = selfLevel;
                ServerId = serverId;
                ServerNumber = serverNumber;
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                RoleLevel = roleLevel;
                Picture = picture ?? string.Empty;
                PictureVersion = pictureVersion;
                Power = power;
            }
        }

        public sealed class GuardSnapshot
        {
            public ushort LimitNumber { get; }
            public ushort Number { get; }
            public byte HasJoin { get; }
            public IReadOnlyList<GuardMember> Members { get; }

            public GuardSnapshot(ushort limitNumber, ushort number, byte hasJoin,
                IReadOnlyList<GuardMember> members)
            {
                LimitNumber = limitNumber;
                Number = number;
                HasJoin = hasJoin;
                Members = Freeze(members);
            }
        }

        public sealed class ApplicationEntry
        {
            public string Picture { get; }
            public ushort PictureVersion { get; }
            public ushort RoleLevel { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public ulong Power { get; }

            public ApplicationEntry(string picture, ushort pictureVersion, ushort roleLevel,
                ulong roleId, string roleName, ulong power)
            {
                Picture = picture ?? string.Empty;
                PictureVersion = pictureVersion;
                RoleLevel = roleLevel;
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                Power = power;
            }
        }

        public sealed class ApplicationsSnapshot
        {
            public IReadOnlyList<ApplicationEntry> Applications { get; }
            public ApplicationsSnapshot(IReadOnlyList<ApplicationEntry> applications) =>
                Applications = Freeze(applications);
        }

        public sealed class ActivitySnapshot
        {
            public byte ActivityType { get; }
            public byte HasFight { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public byte CanEnter { get; }

            public ActivitySnapshot(byte activityType, byte hasFight, uint startTime,
                uint endTime, byte canEnter)
            {
                ActivityType = activityType;
                HasFight = hasFight;
                StartTime = startTime;
                EndTime = endTime;
                CanEnter = canEnter;
            }
        }

        public sealed class GuildEntry
        {
            public ushort Rank { get; }
            public uint ServerId { get; }
            public ushort ServerNumber { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }
            public ulong GuildPower { get; }
            public string LeaderName { get; }
            public ulong LeaderId { get; }

            public GuildEntry(ushort rank, uint serverId, ushort serverNumber, ulong guildId,
                string guildName, ulong guildPower, string leaderName, ulong leaderId)
            {
                Rank = rank;
                ServerId = serverId;
                ServerNumber = serverNumber;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                GuildPower = guildPower;
                LeaderName = leaderName ?? string.Empty;
                LeaderId = leaderId;
            }
        }

        public sealed class GuildsSnapshot
        {
            public uint Camp { get; }
            public IReadOnlyList<GuildEntry> Guilds { get; }

            public GuildsSnapshot(uint camp, IReadOnlyList<GuildEntry> guilds)
            {
                Camp = camp;
                Guilds = Freeze(guilds);
            }
        }

        public sealed class MonsterEntry
        {
            public uint MonsterId { get; }
            public ulong Hp { get; }
            public ulong HpMax { get; }
            public byte AttackLimit { get; }
            public uint NextMonster { get; }

            public MonsterEntry(uint monsterId, ulong hp, ulong hpMax, byte attackLimit, uint nextMonster)
            {
                MonsterId = monsterId;
                Hp = hp;
                HpMax = hpMax;
                AttackLimit = attackLimit;
                NextMonster = nextMonster;
            }
        }

        public sealed class MonsterPacketSnapshot
        {
            public IReadOnlyList<MonsterEntry> Entries { get; }
            public MonsterPacketSnapshot(IReadOnlyList<MonsterEntry> entries) => Entries = Freeze(entries);
        }

        public sealed class ScoreMember
        {
            public ushort Rank { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public ushort KillScore { get; }
            public ushort Score { get; }

            public ScoreMember(ushort rank, ulong roleId, string roleName, ushort killScore, ushort score)
            {
                Rank = rank;
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                KillScore = killScore;
                Score = score;
            }
        }

        public sealed class ScoreGroup
        {
            public ulong GuildId { get; }
            public string GuildName { get; }
            public byte IsAttacker { get; }
            public byte GuildRank { get; }
            public ushort GuildScore { get; }
            public IReadOnlyList<ScoreMember> Members { get; }

            public ScoreGroup(ulong guildId, string guildName, byte isAttacker, byte guildRank,
                ushort guildScore, IReadOnlyList<ScoreMember> members)
            {
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                IsAttacker = isAttacker;
                GuildRank = guildRank;
                GuildScore = guildScore;
                Members = Freeze(members);
            }
        }

        public sealed class ScoreSnapshot
        {
            public IReadOnlyList<ScoreGroup> Groups { get; }
            public ScoreSnapshot(IReadOnlyList<ScoreGroup> groups) => Groups = Freeze(groups);
        }

        public sealed class ResultSnapshot
        {
            public byte Status { get; }
            public ushort GuildRank { get; }
            public ushort SelfRank { get; }
            public IReadOnlyList<ObjectEntry> RankReward { get; }
            public IReadOnlyList<ObjectEntry> Reward { get; }

            public ResultSnapshot(byte status, ushort guildRank, ushort selfRank,
                IReadOnlyList<ObjectEntry> rankReward, IReadOnlyList<ObjectEntry> reward)
            {
                Status = status;
                GuildRank = guildRank;
                SelfRank = selfRank;
                RankReward = Freeze(rankReward);
                Reward = Freeze(reward);
            }
        }

        public sealed class KingRewardStatus
        {
            public byte Times { get; }
            public byte Status { get; }
            public KingRewardStatus(byte times, byte status) { Times = times; Status = status; }
        }

        public sealed class KingSnapshot
        {
            public uint Camp { get; }
            public uint ServerId { get; }
            public ushort ServerNumber { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }
            public ushort Times { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public IReadOnlyList<KingRewardStatus> RewardStatuses { get; }

            public KingSnapshot(uint camp, uint serverId, ushort serverNumber, ulong guildId,
                string guildName, ushort times, uint startTime, uint endTime,
                IReadOnlyList<KingRewardStatus> rewardStatuses)
            {
                Camp = camp;
                ServerId = serverId;
                ServerNumber = serverNumber;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                Times = times;
                StartTime = startTime;
                EndTime = endTime;
                RewardStatuses = Freeze(rewardStatuses);
            }
        }

        public sealed class SideEntry
        {
            public uint ServerId { get; }
            public ushort ServerNumber { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }

            public SideEntry(uint serverId, ushort serverNumber, ulong guildId, string guildName)
            {
                ServerId = serverId;
                ServerNumber = serverNumber;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
            }
        }

        public sealed class SidesSnapshot
        {
            public IReadOnlyList<SideEntry> Attackers { get; }
            public IReadOnlyList<SideEntry> Defenders { get; }

            public SidesSnapshot(IReadOnlyList<SideEntry> attackers, IReadOnlyList<SideEntry> defenders)
            {
                Attackers = Freeze(attackers);
                Defenders = Freeze(defenders);
            }
        }

        public sealed class CampEntry
        {
            public uint Camp { get; }
            public uint ServerId { get; }
            public ushort ServerNumber { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }
            public ulong Power { get; }
            public ulong LeaderId { get; }
            public string LeaderName { get; }

            public CampEntry(uint camp, uint serverId, ushort serverNumber, ulong guildId,
                string guildName, ulong power, ulong leaderId, string leaderName)
            {
                Camp = camp;
                ServerId = serverId;
                ServerNumber = serverNumber;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                Power = power;
                LeaderId = leaderId;
                LeaderName = leaderName ?? string.Empty;
            }
        }

        public sealed class CampsSnapshot
        {
            public IReadOnlyList<CampEntry> Camps { get; }
            public CampsSnapshot(IReadOnlyList<CampEntry> camps) => Camps = Freeze(camps);
        }

        public sealed class ApplyLimitSnapshot
        {
            public ushort RoleLevel { get; }
            public ulong Power { get; }
            public byte Auto { get; }
            public ApplyLimitSnapshot(ushort roleLevel, ulong power, byte auto)
            { RoleLevel = roleLevel; Power = power; Auto = auto; }
        }

        public sealed class ActivityNoticeSnapshot
        {
            public uint Code { get; }
            public ActivityNoticeSnapshot(uint code) => Code = code;
        }

        public sealed class ActivityTimeEntry
        {
            public byte ActivityType { get; }
            public uint StartTime { get; }
            public uint EndTime { get; }
            public ActivityTimeEntry(byte activityType, uint startTime, uint endTime)
            { ActivityType = activityType; StartTime = startTime; EndTime = endTime; }
        }

        public sealed class ActivityTimesSnapshot
        {
            public IReadOnlyList<ActivityTimeEntry> Times { get; }
            public ActivityTimesSnapshot(IReadOnlyList<ActivityTimeEntry> times) => Times = Freeze(times);
        }

        public sealed class JobNoticeSnapshot
        {
            public byte Code { get; }
            public JobNoticeSnapshot(byte code) => Code = code;
        }

        public sealed class PrivilegeEntry
        {
            public ushort PrivilegeId { get; }
            public ushort RemainingNumber { get; }
            public byte Status { get; }
            public ulong EndTime { get; }
            public IReadOnlyList<ushort> NeedJobs { get; }

            public PrivilegeEntry(ushort privilegeId, ushort remainingNumber, byte status,
                ulong endTime, IReadOnlyList<ushort> needJobs)
            {
                PrivilegeId = privilegeId;
                RemainingNumber = remainingNumber;
                Status = status;
                EndTime = endTime;
                NeedJobs = Freeze(needJobs);
            }
        }

        public sealed class PrivilegesSnapshot
        {
            public IReadOnlyList<PrivilegeEntry> Privileges { get; }
            public PrivilegesSnapshot(IReadOnlyList<PrivilegeEntry> privileges) =>
                Privileges = Freeze(privileges);
        }

        public sealed class MeritSnapshot
        {
            public ushort Level { get; }
            public uint Exploit { get; }
            public MeritSnapshot(ushort level, uint exploit) { Level = level; Exploit = exploit; }
        }

        public sealed class MemberEntry
        {
            public ushort Vip { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public uint Level { get; }
            public ushort JobId { get; }
            public uint Exploit { get; }
            public ulong Fight { get; }
            public uint GuildId { get; }
            public string GuildName { get; }

            public MemberEntry(ushort vip, ulong roleId, string roleName, uint level,
                ushort jobId, uint exploit, ulong fight, uint guildId, string guildName)
            {
                Vip = vip;
                RoleId = roleId;
                RoleName = roleName ?? string.Empty;
                Level = level;
                JobId = jobId;
                Exploit = exploit;
                Fight = fight;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
            }
        }

        public sealed class MemberPageSnapshot
        {
            public ushort PageTotal { get; }
            public ushort PageSize { get; }
            public ushort PageNumber { get; }
            public IReadOnlyList<MemberEntry> Members { get; }

            public MemberPageSnapshot(ushort pageTotal, ushort pageSize, ushort pageNumber,
                IReadOnlyList<MemberEntry> members)
            {
                PageTotal = pageTotal;
                PageSize = pageSize;
                PageNumber = pageNumber;
                Members = Freeze(members);
            }
        }

        public sealed class DistributionEntry
        {
            public uint ServerNumber { get; }
            public uint GuildId { get; }
            public string GuildName { get; }
            public ulong LeaderId { get; }
            public string LeaderName { get; }
            public ulong Fight { get; }
            public uint MemberNumber { get; }

            public DistributionEntry(uint serverNumber, uint guildId, string guildName,
                ulong leaderId, string leaderName, ulong fight, uint memberNumber)
            {
                ServerNumber = serverNumber;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
                LeaderId = leaderId;
                LeaderName = leaderName ?? string.Empty;
                Fight = fight;
                MemberNumber = memberNumber;
            }
        }

        public sealed class DistributionSnapshot
        {
            public IReadOnlyList<DistributionEntry> Guilds { get; }
            public DistributionSnapshot(IReadOnlyList<DistributionEntry> guilds) => Guilds = Freeze(guilds);
        }

        public sealed class DailySeaEntry
        {
            public byte SeaId { get; }
            public uint StatueTime { get; }
            public uint BossTime { get; }
            public uint BrickNumber { get; }
            public byte BrickColor { get; }

            public DailySeaEntry(byte seaId, uint statueTime, uint bossTime,
                uint brickNumber, byte brickColor)
            {
                SeaId = seaId;
                StatueTime = statueTime;
                BossTime = bossTime;
                BrickNumber = brickNumber;
                BrickColor = brickColor;
            }
        }

        public sealed class DailyOverviewSnapshot
        {
            public IReadOnlyList<DailySeaEntry> Seas { get; }
            public DailyOverviewSnapshot(IReadOnlyList<DailySeaEntry> seas) => Seas = Freeze(seas);
        }

        public sealed class DailyBossEntry
        {
            public uint Id { get; }
            public ushort Level { get; }
            public string Name { get; }
            public uint RebornTime { get; }

            public DailyBossEntry(uint id, ushort level, string name, uint rebornTime)
            {
                Id = id;
                Level = level;
                Name = name ?? string.Empty;
                RebornTime = rebornTime;
            }
        }

        public sealed class DailySceneSnapshot
        {
            public uint SeaId { get; }
            public uint BrickNumber { get; }
            public ushort CarryCount { get; }
            public ushort DefendCount { get; }
            public IReadOnlyList<DailyBossEntry> Bosses { get; }

            public DailySceneSnapshot(uint seaId, uint brickNumber, ushort carryCount,
                ushort defendCount, IReadOnlyList<DailyBossEntry> bosses)
            {
                SeaId = seaId;
                BrickNumber = brickNumber;
                CarryCount = carryCount;
                DefendCount = defendCount;
                Bosses = Freeze(bosses);
            }
        }

        public sealed class DailySeaRankEntry
        {
            public byte Position { get; }
            public uint ServerNumber { get; }
            public string RoleName { get; }
            public ulong Power { get; }
            public uint BrickNumber { get; }

            public DailySeaRankEntry(byte position, uint serverNumber, string roleName,
                ulong power, uint brickNumber)
            {
                Position = position;
                ServerNumber = serverNumber;
                RoleName = roleName ?? string.Empty;
                Power = power;
                BrickNumber = brickNumber;
            }
        }

        public sealed class DailySeaRankSnapshot
        {
            public uint SeaId { get; }
            public uint MyBrickNumber { get; }
            public uint MyRank { get; }
            public ulong MyPower { get; }
            public byte MyPosition { get; }
            public IReadOnlyList<DailySeaRankEntry> Ranks { get; }

            public DailySeaRankSnapshot(uint seaId, uint myBrickNumber, uint myRank,
                ulong myPower, byte myPosition, IReadOnlyList<DailySeaRankEntry> ranks)
            {
                SeaId = seaId;
                MyBrickNumber = myBrickNumber;
                MyRank = myRank;
                MyPower = myPower;
                MyPosition = myPosition;
                Ranks = Freeze(ranks);
            }
        }

        public sealed class DailyAllRankEntry
        {
            public byte SeaId { get; }
            public byte Position { get; }
            public uint ServerNumber { get; }
            public string RoleName { get; }
            public ulong Power { get; }
            public uint BrickNumber { get; }

            public DailyAllRankEntry(byte seaId, byte position, uint serverNumber,
                string roleName, ulong power, uint brickNumber)
            {
                SeaId = seaId;
                Position = position;
                ServerNumber = serverNumber;
                RoleName = roleName ?? string.Empty;
                Power = power;
                BrickNumber = brickNumber;
            }
        }

        public sealed class DailyAllRankSnapshot
        {
            public uint MyBrickNumber { get; }
            public byte MySea { get; }
            public uint MyRank { get; }
            public ulong MyPower { get; }
            public byte MyPosition { get; }
            public IReadOnlyList<DailyAllRankEntry> Ranks { get; }

            public DailyAllRankSnapshot(uint myBrickNumber, byte mySea, uint myRank,
                ulong myPower, byte myPosition, IReadOnlyList<DailyAllRankEntry> ranks)
            {
                MyBrickNumber = myBrickNumber;
                MySea = mySea;
                MyRank = myRank;
                MyPower = myPower;
                MyPosition = myPosition;
                Ranks = Freeze(ranks);
            }
        }

        public sealed class DailyCarryRewardSnapshot
        {
            public byte CarryCount { get; }
            public IReadOnlyList<ObjectEntry> Reward { get; }

            public DailyCarryRewardSnapshot(byte carryCount, IReadOnlyList<ObjectEntry> reward)
            {
                CarryCount = carryCount;
                Reward = Freeze(reward);
            }
        }

        public sealed class DailyTaskEntry
        {
            public byte TaskId { get; }
            public ushort Count { get; }
            public byte Status { get; }

            public DailyTaskEntry(byte taskId, ushort count, byte status)
            {
                TaskId = taskId;
                Count = count;
                Status = status;
            }
        }

        public sealed class DailyTasksSnapshot
        {
            public IReadOnlyList<DailyTaskEntry> Tasks { get; }
            public DailyTasksSnapshot(IReadOnlyList<DailyTaskEntry> tasks) => Tasks = Freeze(tasks);
        }

        public sealed class DailyKickSnapshot
        {
            public byte Code { get; }
            public DailyKickSnapshot(byte code) => Code = code;
        }

        public sealed class DailyGuildEntry
        {
            public byte SeaId { get; }
            public ulong GuildId { get; }
            public string GuildName { get; }

            public DailyGuildEntry(byte seaId, ulong guildId, string guildName)
            {
                SeaId = seaId;
                GuildId = guildId;
                GuildName = guildName ?? string.Empty;
            }
        }

        public sealed class DailyGuildsSnapshot
        {
            public IReadOnlyList<DailyGuildEntry> Seas { get; }
            public DailyGuildsSnapshot(IReadOnlyList<DailyGuildEntry> seas) => Seas = Freeze(seas);
        }

        public static readonly SeaHegemonyModel Instance = new SeaHegemonyModel();

        private readonly Dictionary<uint, GuildsSnapshot> _guildsByCamp =
            new Dictionary<uint, GuildsSnapshot>();
        private readonly Dictionary<uint, MonsterEntry> _monsters =
            new Dictionary<uint, MonsterEntry>();
        private readonly Dictionary<uint, MemberPageSnapshot> _memberPages =
            new Dictionary<uint, MemberPageSnapshot>();
        private readonly Dictionary<uint, DailySeaRankSnapshot> _dailyRanksBySea =
            new Dictionary<uint, DailySeaRankSnapshot>();

        private SeaHegemonyModel() { }

        public const string ICON_TYPE = "18601";
        public const string RED_ICON_TYPE = "1861";
        private const long SIGNUP_WINDOW_SEC = 86400;

        public InfoSnapshot Info { get; private set; }
        public GuardSnapshot Guard { get; private set; }
        public ApplicationsSnapshot Applications { get; private set; }
        public ActivitySnapshot Activity { get; private set; }
        public MonsterPacketSnapshot LastMonsterPacket { get; private set; }
        public ScoreSnapshot Score { get; private set; }
        public ResultSnapshot LastResult { get; private set; }
        public KingSnapshot King { get; private set; }
        public SidesSnapshot Sides { get; private set; }
        public CampsSnapshot Camps { get; private set; }
        public ApplyLimitSnapshot ApplyLimit { get; private set; }
        public ActivityNoticeSnapshot LastActivityNotice { get; private set; }
        public ActivityTimesSnapshot ActivityTimes { get; private set; }
        public JobNoticeSnapshot LastJobNotice { get; private set; }
        public PrivilegesSnapshot Privileges { get; private set; }
        public MeritSnapshot Merit { get; private set; }
        public DistributionSnapshot Distribution { get; private set; }
        public DailyOverviewSnapshot DailyOverview { get; private set; }
        public DailySceneSnapshot DailyScene { get; private set; }
        public DailyCarryRewardSnapshot LastDailyCarryReward { get; private set; }
        public DailyAllRankSnapshot DailyAllRank { get; private set; }
        public DailyTasksSnapshot DailyTasks { get; private set; }
        public DailyKickSnapshot LastDailyKick { get; private set; }
        public DailyGuildsSnapshot DailyGuilds { get; private set; }

        public bool HasSignupEndTime { get; private set; }
        public long SignupEndTime { get; private set; }
        public bool HasOldJob { get; private set; }
        public ushort OldJobLevel { get; private set; }
        public bool HasExitResult { get; private set; }
        public uint LastExitCode { get; private set; }
        public bool HasDivideResult { get; private set; }
        public uint LastDivideCode { get; private set; }
        public bool HasDailyError { get; private set; }
        public uint LastDailyErrorCode { get; private set; }

        public bool HasInfo => Info != null;
        public bool HasGuard => Guard != null;
        public bool HasApplications => Applications != null;
        public bool HasActivity => Activity != null;
        public bool HasMonsters => LastMonsterPacket != null;
        public bool HasScore => Score != null;
        public bool HasResult => LastResult != null;
        public bool HasKing => King != null;
        public bool HasSides => Sides != null;
        public bool HasCamps => Camps != null;
        public bool HasApplyLimit => ApplyLimit != null;
        public bool HasActivityNotice => LastActivityNotice != null;
        public bool HasActivityTimes => ActivityTimes != null;
        public bool HasJobNotice => LastJobNotice != null;
        public bool HasPrivileges => Privileges != null;
        public bool HasMerit => Merit != null;
        public bool HasDistribution => Distribution != null;
        public bool HasDailyOverview => DailyOverview != null;
        public bool HasDailyScene => DailyScene != null;
        public bool HasDailyCarryReward => LastDailyCarryReward != null;
        public bool HasDailyAllRank => DailyAllRank != null;
        public bool HasDailyTasks => DailyTasks != null;
        public bool HasDailyKick => LastDailyKick != null;
        public bool HasDailyGuilds => DailyGuilds != null;

        public uint Camp => Info != null ? Info.Camp : 0;
        public bool HasJoinSea => Camp != 0;
        public bool DailyRewardRed => Info != null && Info.RewardStatus == 0;
        public IReadOnlyDictionary<uint, MonsterEntry> Monsters =>
            new ReadOnlyDictionary<uint, MonsterEntry>(_monsters);

        public void ReplaceInfo(InfoSnapshot snapshot) => Info = snapshot;
        public void ReplaceGuard(GuardSnapshot snapshot) => Guard = snapshot;
        public void ReplaceApplications(ApplicationsSnapshot snapshot) => Applications = snapshot;
        public void ReplaceActivity(ActivitySnapshot snapshot) => Activity = snapshot;
        public void ReplaceScore(ScoreSnapshot snapshot) => Score = snapshot;
        public void ReplaceResult(ResultSnapshot snapshot) => LastResult = snapshot;
        public void ReplaceKing(KingSnapshot snapshot) => King = snapshot;
        public void ReplaceSides(SidesSnapshot snapshot) => Sides = snapshot;
        public void ReplaceCamps(CampsSnapshot snapshot) => Camps = snapshot;
        public void ReplaceApplyLimit(ApplyLimitSnapshot snapshot) => ApplyLimit = snapshot;
        public void ReplaceActivityNotice(ActivityNoticeSnapshot snapshot) => LastActivityNotice = snapshot;
        public void ReplaceActivityTimes(ActivityTimesSnapshot snapshot) => ActivityTimes = snapshot;
        public void ReplaceJobNotice(JobNoticeSnapshot snapshot) => LastJobNotice = snapshot;
        public void ReplacePrivileges(PrivilegesSnapshot snapshot) => Privileges = snapshot;
        public void ReplaceMerit(MeritSnapshot snapshot) => Merit = snapshot;
        public void ReplaceDistribution(DistributionSnapshot snapshot) => Distribution = snapshot;
        public void ReplaceDailyOverview(DailyOverviewSnapshot snapshot) => DailyOverview = snapshot;
        public void ReplaceDailyScene(DailySceneSnapshot snapshot) => DailyScene = snapshot;
        public void ReplaceDailyCarryReward(DailyCarryRewardSnapshot snapshot) =>
            LastDailyCarryReward = snapshot;
        public void ReplaceDailyAllRank(DailyAllRankSnapshot snapshot) => DailyAllRank = snapshot;
        public void ReplaceDailyTasks(DailyTasksSnapshot snapshot) => DailyTasks = snapshot;
        public void ReplaceDailyKick(DailyKickSnapshot snapshot) => LastDailyKick = snapshot;
        public void ReplaceDailyGuilds(DailyGuildsSnapshot snapshot) => DailyGuilds = snapshot;

        public void ReplaceGuilds(GuildsSnapshot snapshot) => _guildsByCamp[snapshot.Camp] = snapshot;
        public bool TryGetGuilds(uint camp, out GuildsSnapshot snapshot) =>
            _guildsByCamp.TryGetValue(camp, out snapshot);

        public void ApplyMonsterPacket(IReadOnlyList<MonsterEntry> entries)
        {
            LastMonsterPacket = new MonsterPacketSnapshot(entries);
            for (int i = 0; i < LastMonsterPacket.Entries.Count; i++)
            {
                MonsterEntry entry = LastMonsterPacket.Entries[i];
                _monsters[entry.MonsterId] = entry;
            }
        }

        public bool TryGetMonster(uint monsterId, out MonsterEntry entry) =>
            _monsters.TryGetValue(monsterId, out entry);

        public void ReplaceMemberPage(MemberPageSnapshot snapshot)
        {
            _memberPages[MemberPageKey(snapshot.PageSize, snapshot.PageNumber)] = snapshot;
        }

        public bool TryGetMemberPage(ushort pageSize, ushort pageNumber, out MemberPageSnapshot snapshot) =>
            _memberPages.TryGetValue(MemberPageKey(pageSize, pageNumber), out snapshot);

        public void ReplaceDailySeaRank(DailySeaRankSnapshot snapshot) =>
            _dailyRanksBySea[snapshot.SeaId] = snapshot;

        public bool TryGetDailySeaRank(uint seaId, out DailySeaRankSnapshot snapshot) =>
            _dailyRanksBySea.TryGetValue(seaId, out snapshot);

        public void SetSignupEndTime(uint endTime)
        {
            HasSignupEndTime = true;
            SignupEndTime = endTime;
        }

        public void SetOldJob(ushort oldJobLevel)
        {
            HasOldJob = true;
            OldJobLevel = oldJobLevel;
        }

        public void SetExitResult(uint code)
        {
            HasExitResult = true;
            LastExitCode = code;
        }

        public void SetDivideResult(uint code)
        {
            HasDivideResult = true;
            LastDivideCode = code;
        }

        public void SetDailyError(uint code)
        {
            HasDailyError = true;
            LastDailyErrorCode = code;
        }

        public bool GetEntranceOpenState()
        {
            if (!HasSignupEndTime || SignupEndTime <= 0) return false;
            long now = TimeUtil.NowSec();
            long signupStart = SignupEndTime - SIGNUP_WINDOW_SEC;
            return signupStart < now && now < SignupEndTime;
        }

        public string GetIconText() => HasJoinSea ? "已报名" : "报名中";

        public void Reset()
        {
            Info = null;
            Guard = null;
            Applications = null;
            Activity = null;
            LastMonsterPacket = null;
            Score = null;
            LastResult = null;
            King = null;
            Sides = null;
            Camps = null;
            ApplyLimit = null;
            LastActivityNotice = null;
            ActivityTimes = null;
            LastJobNotice = null;
            Privileges = null;
            Merit = null;
            Distribution = null;
            DailyOverview = null;
            DailyScene = null;
            LastDailyCarryReward = null;
            DailyAllRank = null;
            DailyTasks = null;
            LastDailyKick = null;
            DailyGuilds = null;
            HasSignupEndTime = false;
            SignupEndTime = 0;
            HasOldJob = false;
            OldJobLevel = 0;
            HasExitResult = false;
            LastExitCode = 0;
            HasDivideResult = false;
            LastDivideCode = 0;
            HasDailyError = false;
            LastDailyErrorCode = 0;
            _guildsByCamp.Clear();
            _monsters.Clear();
            _memberPages.Clear();
            _dailyRanksBySea.Clear();
        }

        private static uint MemberPageKey(ushort pageSize, ushort pageNumber) =>
            ((uint)pageSize << 16) | pageNumber;

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            int count = source != null ? source.Count : 0;
            T[] copy = new T[count];
            for (int i = 0; i < count; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }
    }
}
