using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.Eternity
{
    public sealed class EternityModel
    {
        public static readonly EternityModel Instance = new EternityModel();

        public uint OpenTime { get; private set; }
        public uint EnterTime { get; private set; }
        public uint EndTime { get; private set; }
        public bool HasData { get; private set; }
        private readonly List<JoinEntry> _joinList = new List<JoinEntry>();
        private readonly IReadOnlyList<JoinEntry> _readOnlyJoinList;
        public bool HasJoinInfo { get; private set; }
        public byte CanEnterScene { get; private set; }
        public IReadOnlyList<JoinEntry> JoinList => _readOnlyJoinList;
        public ushort DieTimes { get; private set; }
        public uint Time { get; private set; }
        public uint DieTime { get; private set; }
        public uint SafeTime { get; private set; }
        public bool HasReliveInfo { get; private set; }
        private readonly List<MonsterEntry> _monsterInfo = new List<MonsterEntry>();
        private readonly IReadOnlyList<MonsterEntry> _readOnlyMonsterInfo;
        public bool HasMonsterInfo { get; private set; }
        public ushort MonsterScene { get; private set; }
        public IReadOnlyList<MonsterEntry> MonsterInfo => _readOnlyMonsterInfo;
        private readonly List<DamageEntry> _damageRank = new List<DamageEntry>();
        private readonly IReadOnlyList<DamageEntry> _readOnlyDamageRank;
        public bool HasDamageRank { get; private set; }
        public ushort DamageScene { get; private set; }
        public uint DamageMonId { get; private set; }
        public IReadOnlyList<DamageEntry> DamageRank => _readOnlyDamageRank;
        private readonly Dictionary<uint, BossStateEntry> _bossStates = new Dictionary<uint, BossStateEntry>();
        private readonly IReadOnlyDictionary<uint, BossStateEntry> _readOnlyBossStates;
        public bool HasBossStates { get; private set; }
        public IReadOnlyDictionary<uint, BossStateEntry> BossStates => _readOnlyBossStates;

        public sealed class JoinEntry
        {
            public uint Scene { get; }
            public ushort SelfServerNum { get; }
            public ushort SceneNum { get; }
            public JoinEntry(uint scene, ushort selfServerNum, ushort sceneNum) { Scene = scene; SelfServerNum = selfServerNum; SceneNum = sceneNum; }
        }

        public sealed class BossStateEntry
        {
            public uint MonId { get; }
            public uint RebornTime { get; }
            public uint BlServer { get; }
            public uint BlServerNum { get; }
            public string BlServerName { get; }

            public BossStateEntry(uint monId, uint rebornTime, uint blServer, uint blServerNum, string blServerName)
            {
                MonId = monId;
                RebornTime = rebornTime;
                BlServer = blServer;
                BlServerNum = blServerNum;
                BlServerName = blServerName;
            }
        }

        public sealed class DamageEntry
        {
            public uint ServerId { get; }
            public ushort ServerNum { get; }
            public string ServerName { get; }
            public uint PlayerId { get; }
            public string PlayerName { get; }
            public ushort Damage { get; }

            public DamageEntry(uint serverId, ushort serverNum, string serverName, uint playerId, string playerName, ushort damage)
            {
                ServerId = serverId;
                ServerNum = serverNum;
                ServerName = serverName;
                PlayerId = playerId;
                PlayerName = playerName;
                Damage = damage;
            }
        }

        public sealed class MonsterEntry
        {
            public uint MonId { get; }
            public ushort MonLv { get; }
            public byte MonType { get; }
            public uint BlServer { get; }
            public string BlServerName { get; }
            public uint BlServerNum { get; }
            public uint RebornTime { get; }

            public MonsterEntry(uint monId, ushort monLv, byte monType, uint blServer, string blServerName, uint blServerNum, uint rebornTime)
            {
                MonId = monId;
                MonLv = monLv;
                MonType = monType;
                BlServer = blServer;
                BlServerName = blServerName;
                BlServerNum = blServerNum;
                RebornTime = rebornTime;
            }
        }

        private EternityModel()
        {
            _readOnlyJoinList = _joinList.AsReadOnly();
            _readOnlyMonsterInfo = _monsterInfo.AsReadOnly();
            _readOnlyDamageRank = _damageRank.AsReadOnly();
            _readOnlyBossStates = new ReadOnlyDictionary<uint, BossStateEntry>(_bossStates);
        }

        public void Replace(uint openTime, uint enterTime, uint endTime)
        {
            OpenTime = openTime;
            EnterTime = enterTime;
            EndTime = endTime;
            HasData = true;
        }

        public void ReplaceJoinInfo(byte canEnterScene, List<JoinEntry> joinList)
        {
            CanEnterScene = canEnterScene;
            _joinList.Clear();
            if (joinList != null) _joinList.AddRange(joinList);
            HasJoinInfo = true;
        }

        public void ReplaceReliveInfo(ushort dieTimes, uint time, uint dieTime, uint safeTime)
        {
            DieTimes = dieTimes;
            Time = time;
            DieTime = dieTime;
            SafeTime = safeTime;
            HasReliveInfo = true;
        }

        public void ReplaceDamageRank(ushort scene, uint monId, List<DamageEntry> entries)
        {
            DamageScene = scene;
            DamageMonId = monId;
            _damageRank.Clear();
            if (entries != null) _damageRank.AddRange(entries);
            HasDamageRank = true;
        }

        public void ReplaceMonsterInfo(ushort scene, List<MonsterEntry> entries)
        {
            MonsterScene = scene;
            _monsterInfo.Clear();
            if (entries != null) _monsterInfo.AddRange(entries);
            HasMonsterInfo = true;
        }

        public void ApplyMonsterReborn(uint monId)
        {
            if (!HasMonsterInfo) return;
            for (int i = 0; i < _monsterInfo.Count; i++)
            {
                MonsterEntry entry = _monsterInfo[i];
                if (entry.MonId != monId) continue;
                _monsterInfo[i] = new MonsterEntry(entry.MonId, entry.MonLv, entry.MonType, entry.BlServer, entry.BlServerName, entry.BlServerNum, 0);
                return;
            }
        }

        public void ReplaceBossState(BossStateEntry bossState)
        {
            _bossStates[bossState.MonId] = bossState;
            HasBossStates = true;
        }

        public void Reset()
        {
            OpenTime = 0;
            EnterTime = 0;
            EndTime = 0;
            HasData = false;
            CanEnterScene = 0;
            _joinList.Clear();
            HasJoinInfo = false;
            DieTimes = 0;
            Time = 0;
            DieTime = 0;
            SafeTime = 0;
            HasReliveInfo = false;
            MonsterScene = 0;
            _monsterInfo.Clear();
            HasMonsterInfo = false;
            DamageScene = 0;
            DamageMonId = 0;
            _damageRank.Clear();
            HasDamageRank = false;
            _bossStates.Clear();
            HasBossStates = false;
        }
    }
}
