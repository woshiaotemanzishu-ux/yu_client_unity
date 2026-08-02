using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.KfSingleRank
{
    public sealed class KfSingleRankModel
    {
        public sealed class LevelEntry
        {
            public byte Level { get; }
            public uint GoTime { get; }

            public LevelEntry(byte level, uint goTime)
            {
                Level = level;
                GoTime = goTime;
            }
        }

        public sealed class AreaRankEntry
        {
            public byte Level { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public ushort ServerNum { get; }
            public uint GoTime { get; }

            public AreaRankEntry(byte level, ulong roleId, string roleName, ushort serverNum, uint goTime)
            {
                Level = level;
                RoleId = roleId;
                RoleName = roleName;
                ServerNum = serverNum;
                GoTime = goTime;
            }
        }

        public sealed class AreaSnapshot
        {
            public byte AreaId { get; }
            public IReadOnlyList<AreaRankEntry> Entries { get; }

            public AreaSnapshot(byte areaId, List<AreaRankEntry> entries)
            {
                AreaId = areaId;
                Entries = new List<AreaRankEntry>(entries ?? new List<AreaRankEntry>()).AsReadOnly();
            }
        }
        public sealed class AreaTowerEntry
        {
            public byte Level { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public ushort ServerId { get; }
            public ushort ServerNum { get; }
            public ushort LevelValue { get; }
            public byte Career { get; }
            public byte Sex { get; }
            public byte Turn { get; }
            public string Picture { get; }
            public byte PictureVer { get; }
            public uint GoTime { get; }

            public AreaTowerEntry(
                byte level,
                ulong roleId,
                string roleName,
                ushort serverId,
                ushort serverNum,
                ushort levelValue,
                byte career,
                byte sex,
                byte turn,
                string picture,
                byte pictureVer,
                uint goTime)
            {
                Level = level;
                RoleId = roleId;
                RoleName = roleName;
                ServerId = serverId;
                ServerNum = serverNum;
                LevelValue = levelValue;
                Career = career;
                Sex = sex;
                Turn = turn;
                Picture = picture;
                PictureVer = pictureVer;
                GoTime = goTime;
            }
        }

        public sealed class AreaTowerSnapshot
        {
            public byte AreaId { get; }
            public IReadOnlyList<AreaTowerEntry> Entries { get; }

            public AreaTowerSnapshot(byte areaId, List<AreaTowerEntry> entries)
            {
                AreaId = areaId;
                Entries = new List<AreaTowerEntry>(entries ?? new List<AreaTowerEntry>()).AsReadOnly();
            }
        }

        public sealed class SettlementReward
        {
            public byte Type { get; }
            public uint TypeId { get; }
            public uint Num { get; }

            public SettlementReward(byte type, uint typeId, uint num)
            {
                Type = type;
                TypeId = typeId;
                Num = num;
            }
        }

        public static readonly KfSingleRankModel Instance = new KfSingleRankModel();

        private readonly List<LevelEntry> _levels = new List<LevelEntry>();
        private readonly IReadOnlyList<LevelEntry> _readOnlyLevels;
        private readonly Dictionary<byte, AreaSnapshot> _areaTops = new Dictionary<byte, AreaSnapshot>();
        private readonly IReadOnlyDictionary<byte, AreaSnapshot> _readOnlyAreaTops;
        private readonly Dictionary<byte, AreaTowerSnapshot> _areaTowers = new Dictionary<byte, AreaTowerSnapshot>();
        private readonly IReadOnlyDictionary<byte, AreaTowerSnapshot> _readOnlyAreaTowers;
        private readonly List<SettlementReward> _settlementRewards = new List<SettlementReward>();
        private readonly IReadOnlyList<SettlementReward> _readOnlySettlementRewards;

        private KfSingleRankModel()
        {
            _readOnlyLevels = _levels.AsReadOnly();
            _readOnlyAreaTops = new ReadOnlyDictionary<byte, AreaSnapshot>(_areaTops);
            _readOnlyAreaTowers = new ReadOnlyDictionary<byte, AreaTowerSnapshot>(_areaTowers);
            _readOnlySettlementRewards = _settlementRewards.AsReadOnly();
        }

        public bool HasData { get; private set; }
        public byte StartLevel { get; private set; }
        public byte RewardState { get; private set; }
        public IReadOnlyList<LevelEntry> Levels => _readOnlyLevels;
        public IReadOnlyDictionary<byte, AreaSnapshot> AreaTops => _readOnlyAreaTops;
        public IReadOnlyDictionary<byte, AreaTowerSnapshot> AreaTowers => _readOnlyAreaTowers;
        public bool HasSettlement { get; private set; }
        public byte SettlementResultType { get; private set; }
        public byte SettlementLevel { get; private set; }
        public uint SettlementGoTime { get; private set; }
        public byte SettlementBecameChallenger { get; private set; }
        public IReadOnlyList<SettlementReward> SettlementRewards => _readOnlySettlementRewards;
        public bool TryGetAreaTowers(byte areaId, out AreaTowerSnapshot snapshot)
        {
            return _areaTowers.TryGetValue(areaId, out snapshot);
        }

        public void ReplaceAreaTowers(byte areaId, List<AreaTowerEntry> entries)
        {
            _areaTowers[areaId] = new AreaTowerSnapshot(areaId, entries);
        }

        public bool TryGetAreaTop(byte areaId, out AreaSnapshot snapshot)
        {
            return _areaTops.TryGetValue(areaId, out snapshot);
        }

        public void ReplaceAreaTop(byte areaId, List<AreaRankEntry> entries)
        {
            _areaTops[areaId] = new AreaSnapshot(areaId, entries);
        }

        public void Replace(byte startLevel, byte rewardState, List<LevelEntry> levels)
        {
            StartLevel = startLevel;
            RewardState = rewardState;
            _levels.Clear();
            if (levels != null) _levels.AddRange(levels);
            HasData = true;
        }

        public void ReplaceSettlement(byte resultType, byte level, uint goTime, byte becameChallenger, List<SettlementReward> rewards)
        {
            SettlementResultType = resultType;
            SettlementLevel = level;
            SettlementGoTime = goTime;
            SettlementBecameChallenger = becameChallenger;
            _settlementRewards.Clear();
            if (rewards != null) _settlementRewards.AddRange(rewards);
            HasSettlement = true;
        }

        public void Reset()
        {
            StartLevel = 0;
            RewardState = 0;
            _levels.Clear();
            _areaTops.Clear();
            _areaTowers.Clear();
            _settlementRewards.Clear();
            HasData = false;
            HasSettlement = false;
            SettlementResultType = 0;
            SettlementLevel = 0;
            SettlementGoTime = 0;
            SettlementBecameChallenger = 0;
        }
    }
}
