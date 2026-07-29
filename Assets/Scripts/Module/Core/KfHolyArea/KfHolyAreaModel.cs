using System;
using System.Collections.Generic;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.KfHolyArea
{
    /// <summary>神陨禁区284族原始读侧状态。所有列表保留服务器线序与重复项。</summary>
    public sealed class KfHolyAreaModel
    {
        public sealed class ServerEntry
        {
            public uint ServerId { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public ushort OpenDay { get; internal set; }
            public byte Camp { get; internal set; }
        }

        public sealed class OverviewSnapshot
        {
            public byte SanctuaryType { get; internal set; }
            public IReadOnlyList<ServerEntry> Servers { get; internal set; }
        }

        public sealed class CampScoreEntry
        {
            public byte Camp { get; internal set; }
            public ushort Score { get; internal set; }
        }

        public sealed class BossEntry
        {
            public uint BossId { get; internal set; }
            public byte MonsterType { get; internal set; }
            public ushort BossLevel { get; internal set; }
            public uint RebornTime { get; internal set; }
        }

        public sealed class SceneRankEntry
        {
            public ulong PlayerId { get; internal set; }
            public string RoleName { get; internal set; }
            public uint ServerId { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public uint Score { get; internal set; }
            public ulong KillNum { get; internal set; }
            public byte Rank { get; internal set; }
        }

        public sealed class BuildingSnapshot
        {
            public uint SceneId { get; internal set; }
            public byte ConstructionType { get; internal set; }
            public uint BelongCamp { get; internal set; }
            public uint PreviousBelongCamp { get; internal set; }
            public IReadOnlyList<CampScoreEntry> CampScores { get; internal set; }
            public byte BelongRewardState { get; internal set; }
            public ushort PersonCount { get; internal set; }
            public IReadOnlyList<BossEntry> Bosses { get; internal set; }
            public IReadOnlyList<SceneRankEntry> RankEntries { get; internal set; }

            internal BuildingSnapshot WithRanks(byte belongCamp, IReadOnlyList<SceneRankEntry> ranks)
            {
                return new BuildingSnapshot
                {
                    SceneId = SceneId,
                    ConstructionType = ConstructionType,
                    BelongCamp = belongCamp,
                    PreviousBelongCamp = PreviousBelongCamp,
                    CampScores = CampScores,
                    BelongRewardState = BelongRewardState,
                    PersonCount = PersonCount,
                    Bosses = Bosses,
                    RankEntries = ranks
                };
            }
        }

        public sealed class BossDamageEntry
        {
            public uint ServerId { get; internal set; }
            public ushort ServerNum { get; internal set; }
            public string ServerName { get; internal set; }
            public uint RoleId { get; internal set; }
            public string Name { get; internal set; }
            public ushort Hurt { get; internal set; }
        }

        public sealed class BossDamageSnapshot
        {
            public uint BossId { get; internal set; }
            public IReadOnlyList<BossDamageEntry> Entries { get; internal set; }
        }

        public sealed class ScoreRewardEntry
        {
            public ushort ScoreConfig { get; internal set; }
            public byte State { get; internal set; }
        }

        public sealed class ScoreSnapshot
        {
            public uint Score { get; internal set; }
            public byte Cost { get; internal set; }
            public ushort Anger { get; internal set; }
            public IReadOnlyList<ScoreRewardEntry> Rewards { get; internal set; }
        }

        public sealed class OccupyEvent
        {
            public uint SceneId { get; internal set; }
            public byte ConstructionType { get; internal set; }
        }

        public sealed class KillLogEntry
        {
            public uint ServerId { get; internal set; }
            public uint ServerNum { get; internal set; }
            public uint RoleId { get; internal set; }
            public string RoleName { get; internal set; }
            public uint Time { get; internal set; }
        }

        public sealed class KillLogSnapshot
        {
            public uint SceneId { get; internal set; }
            public uint MonsterId { get; internal set; }
            public IReadOnlyList<KillLogEntry> Entries { get; internal set; }
        }

        public sealed class BossRefreshEvent
        {
            public byte Code { get; internal set; }
        }

        public sealed class DeathFatigueSnapshot
        {
            public ushort DieTimes { get; internal set; }
            public uint FreeReviveTime { get; internal set; }
            public uint DebuffEndTime { get; internal set; }
            public uint SafeTime { get; internal set; }
        }

        public sealed class BossLifeEvent
        {
            public uint BossId { get; internal set; }
            public uint RebornTime { get; internal set; }
        }

        public sealed class ExitCountdownEvent
        {
            public uint OutTime { get; internal set; }
        }

        public sealed class SceneRankEvent
        {
            public uint SceneId { get; internal set; }
            public byte Camp { get; internal set; }
            public IReadOnlyList<SceneRankEntry> Entries { get; internal set; }
        }

        public sealed class RoleRankSnapshot
        {
            public ushort SceneId { get; internal set; }
            public byte Rank { get; internal set; }
            public ushort Score { get; internal set; }
            public ushort KillScore { get; internal set; }
        }

        public sealed class BelongRefreshEvent
        {
            public ushort SceneId { get; internal set; }
        }

        public static readonly KfHolyAreaModel Instance = new KfHolyAreaModel();
        private KfHolyAreaModel() { }

        public const string ICON_TYPE = "284";

        private readonly Dictionary<uint, BuildingSnapshot> _buildings =
            new Dictionary<uint, BuildingSnapshot>();
        private readonly Dictionary<ulong, KillLogSnapshot> _killLogs =
            new Dictionary<ulong, KillLogSnapshot>();
        private readonly Dictionary<ushort, RoleRankSnapshot> _roleRanks =
            new Dictionary<ushort, RoleRankSnapshot>();

        public long ActStart { get; private set; }
        public long ActEnd { get; private set; }
        public OverviewSnapshot Overview { get; private set; }
        public IReadOnlyDictionary<uint, BuildingSnapshot> Buildings => _buildings;
        public BossDamageSnapshot LastBossDamage { get; private set; }
        public ScoreSnapshot Score { get; private set; }
        public OccupyEvent LastOccupy { get; private set; }
        public IReadOnlyDictionary<ulong, KillLogSnapshot> KillLogs => _killLogs;
        public BossRefreshEvent LastBossRefresh { get; private set; }
        public DeathFatigueSnapshot DeathFatigue { get; private set; }
        public BossLifeEvent LastBossLife { get; private set; }
        public ExitCountdownEvent LastExitCountdown { get; private set; }
        public SceneRankEvent LastSceneRank { get; private set; }
        public IReadOnlyDictionary<ushort, RoleRankSnapshot> RoleRanks => _roleRanks;
        public BelongRefreshEvent LastBelongRefresh { get; private set; }

        public void SetActTime(long actStart, long actEnd)
        {
            ActStart = actStart;
            ActEnd = actEnd;
        }

        public void ReplaceOverview(OverviewSnapshot value)
        {
            value.Servers = Freeze(value.Servers);
            Overview = value;
        }

        public void ReplaceBuilding(BuildingSnapshot value)
        {
            value.CampScores = Freeze(value.CampScores);
            value.Bosses = Freeze(value.Bosses);
            value.RankEntries = Freeze(value.RankEntries);
            _buildings[value.SceneId] = value;
        }

        public void ReplaceBossDamage(BossDamageSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            LastBossDamage = value;
        }

        public void ReplaceScore(ScoreSnapshot value)
        {
            value.Rewards = Freeze(value.Rewards);
            Score = value;
        }

        public void ReplaceOccupy(OccupyEvent value) => LastOccupy = value;

        public void ReplaceKillLog(KillLogSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            _killLogs[KillLogKey(value.SceneId, value.MonsterId)] = value;
        }

        public void ReplaceBossRefresh(BossRefreshEvent value) => LastBossRefresh = value;
        public void ReplaceDeathFatigue(DeathFatigueSnapshot value) => DeathFatigue = value;
        public void ReplaceBossLife(BossLifeEvent value) => LastBossLife = value;
        public void ReplaceExitCountdown(ExitCountdownEvent value) => LastExitCountdown = value;

        /// <summary>保存28421 raw，并只更新已加载的同场景28401；早包/未知场景不创建建筑。</summary>
        public void ApplySceneRank(SceneRankEvent value)
        {
            value.Entries = Freeze(value.Entries);
            LastSceneRank = value;
            if (_buildings.TryGetValue(value.SceneId, out BuildingSnapshot building))
                _buildings[value.SceneId] = building.WithRanks(value.Camp, value.Entries);
        }

        public void ReplaceRoleRank(RoleRankSnapshot value) => _roleRanks[value.SceneId] = value;
        public void ReplaceBelongRefresh(BelongRefreshEvent value) => LastBelongRefresh = value;

        public bool TryGetBuilding(uint sceneId, out BuildingSnapshot value) =>
            _buildings.TryGetValue(sceneId, out value);

        public bool TryGetKillLog(uint sceneId, uint monsterId, out KillLogSnapshot value) =>
            _killLogs.TryGetValue(KillLogKey(sceneId, monsterId), out value);

        public bool TryGetRoleRank(ushort sceneId, out RoleRankSnapshot value) =>
            _roleRanks.TryGetValue(sceneId, out value);

        public bool GetEntranceOpenState() => ActEnd > 0;

        public string GetIconStatusText()
        {
            long now = TimeUtil.NowSec();
            return ActStart > 0 && now >= ActStart && now < ActEnd ? "进行中" : "";
        }

        public void Reset()
        {
            ActStart = 0;
            ActEnd = 0;
            Overview = null;
            _buildings.Clear();
            LastBossDamage = null;
            Score = null;
            LastOccupy = null;
            _killLogs.Clear();
            LastBossRefresh = null;
            DeathFatigue = null;
            LastBossLife = null;
            LastExitCountdown = null;
            LastSceneRank = null;
            _roleRanks.Clear();
            LastBelongRefresh = null;
        }

        private static ulong KillLogKey(uint sceneId, uint monsterId) =>
            ((ulong)sceneId << 32) | monsterId;

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }
    }
}
