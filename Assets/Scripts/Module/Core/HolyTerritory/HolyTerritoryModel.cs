using System.Collections.Generic;

namespace Shenxiao.Module.Core.HolyTerritory
{
    /// <summary>神陨禁区 283 协议族的原始只读状态；列表保留服务端 wire 顺序与重复项。</summary>
    public sealed class HolyTerritoryModel
    {
        public sealed class BossEntry
        {
            public uint BossId { get; internal set; }
            public uint RebornTime { get; internal set; }
            public byte IsRemind { get; internal set; }
        }

        public sealed class TerritorySnapshot
        {
            public byte SanctuaryId { get; internal set; }
            public uint Point { get; internal set; }
            public ulong BelongGuildId { get; internal set; }
            public string BelongGuildName { get; internal set; }
            public uint EndTime { get; internal set; }
            public IReadOnlyList<BossEntry> Bosses { get; internal set; }

            internal TerritorySnapshot WithFirstBossReborn(uint bossId, uint rebornTime)
            {
                var bosses = new List<BossEntry>(Bosses.Count);
                bool patched = false;
                for (int i = 0; i < Bosses.Count; i++)
                {
                    BossEntry source = Bosses[i];
                    if (!patched && source.BossId == bossId)
                    {
                        bosses.Add(new BossEntry
                        {
                            BossId = source.BossId, RebornTime = rebornTime, IsRemind = source.IsRemind
                        });
                        patched = true;
                    }
                    else bosses.Add(source);
                }
                if (!patched) return this;
                return new TerritorySnapshot
                {
                    SanctuaryId = SanctuaryId, Point = Point, BelongGuildId = BelongGuildId,
                    BelongGuildName = BelongGuildName, EndTime = EndTime, Bosses = Freeze(bosses)
                };
            }
        }

        public sealed class GuildRankEntry
        {
            public string GuildName { get; internal set; }
            public string ChairmanName { get; internal set; }
            public uint Rank { get; internal set; }
            public uint MemberNum { get; internal set; }
            public uint AllNum { get; internal set; }
            public ulong AveragePower { get; internal set; }
        }

        public sealed class GuildRankSnapshot
        {
            public uint MyGuildRank { get; internal set; }
            public ulong MyGuildTopTenPower { get; internal set; }
            public IReadOnlyList<GuildRankEntry> Entries { get; internal set; }
        }

        public sealed class BossNotice
        {
            public byte SanctuaryId { get; internal set; }
            public uint BossId { get; internal set; }
        }

        public sealed class DeathFatigueSnapshot
        {
            public ushort DieTimes { get; internal set; }
            public uint Time { get; internal set; }
            public uint DebuffTime { get; internal set; }
            public uint SafeTime { get; internal set; }
        }

        public sealed class BossDefeatedEvent
        {
            public byte SanctuaryId { get; internal set; }
            public uint BossId { get; internal set; }
            public uint RebornTime { get; internal set; }
        }

        public sealed class GuildMemberRankEntry
        {
            public ulong RoleId { get; internal set; }
            public uint Rank { get; internal set; }
            public string Picture { get; internal set; }
            public uint PictureVersion { get; internal set; }
            public byte Career { get; internal set; }
            public string RoleName { get; internal set; }
            public ulong Power { get; internal set; }
            public uint DesignationId { get; internal set; }
        }

        public sealed class GuildMemberRankSnapshot
        {
            public uint MyRank { get; internal set; }
            public ulong MyPower { get; internal set; }
            public IReadOnlyList<GuildMemberRankEntry> Entries { get; internal set; }
        }

        public sealed class KillLogEntry
        {
            public uint Time { get; internal set; }
            public string Name { get; internal set; }
            public byte IsShow { get; internal set; }
            public uint ReducePoint { get; internal set; }
        }

        public sealed class KillLogSnapshot
        {
            public byte SanctuaryId { get; internal set; }
            public uint BossId { get; internal set; }
            public IReadOnlyList<KillLogEntry> Entries { get; internal set; }
        }

        public sealed class SanctuaryRankEntry
        {
            public uint Rank { get; internal set; }
            public string RoleName { get; internal set; }
            public ulong Power { get; internal set; }
            public uint DesignationId { get; internal set; }
        }

        public sealed class SanctuaryRankSnapshot
        {
            public byte SanctuaryId { get; internal set; }
            public IReadOnlyList<SanctuaryRankEntry> Entries { get; internal set; }
        }

        public sealed class SettlementSnapshot
        {
            public uint GuildRank { get; internal set; }
            public byte SanctuaryId { get; internal set; }
            public uint PersonRank { get; internal set; }
            public uint DesignationId { get; internal set; }
        }

        public static readonly HolyTerritoryModel Instance = new HolyTerritoryModel();

        private readonly Dictionary<byte, TerritorySnapshot> _territories =
            new Dictionary<byte, TerritorySnapshot>();
        private readonly Dictionary<ulong, KillLogSnapshot> _killLogs =
            new Dictionary<ulong, KillLogSnapshot>();
        private readonly Dictionary<byte, SanctuaryRankSnapshot> _sanctuaryRanks =
            new Dictionary<byte, SanctuaryRankSnapshot>();
        private HolyTerritoryModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public IReadOnlyDictionary<byte, TerritorySnapshot> Territories => _territories;
        public GuildRankSnapshot GuildRank { get; private set; }
        public bool HasActivityEndTime { get; private set; }
        public uint ActivityEndTime { get; private set; }
        public BossNotice LastRebornNotice { get; private set; }
        public DeathFatigueSnapshot DeathFatigue { get; private set; }
        public BossDefeatedEvent LastBossDefeated { get; private set; }
        public GuildMemberRankSnapshot GuildMemberRank { get; private set; }
        public IReadOnlyDictionary<ulong, KillLogSnapshot> KillLogs => _killLogs;
        public IReadOnlyDictionary<byte, SanctuaryRankSnapshot> SanctuaryRanks => _sanctuaryRanks;
        public BossNotice LastUnderAttack { get; private set; }
        public SettlementSnapshot Settlement { get; private set; }
        public bool HasFirstOpen { get; private set; }
        public byte FirstOpenCode { get; private set; }
        public bool HasPointGain { get; private set; }
        public uint LastPointGain { get; private set; }
        public bool HasFatigue { get; private set; }
        public uint Fatigue { get; private set; }
        public bool HasFatigueGain { get; private set; }
        public uint LastFatigueGain { get; private set; }

        public void SetError(uint code) { HasError = true; LastErrorCode = code; }

        public void ReplaceTerritory(TerritorySnapshot value)
        {
            value.Bosses = Freeze(value.Bosses);
            _territories[value.SanctuaryId] = value;
        }

        public void ReplaceGuildRank(GuildRankSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            GuildRank = value;
        }

        public void SetActivityEndTime(uint value) { HasActivityEndTime = true; ActivityEndTime = value; }
        public void ReplaceRebornNotice(BossNotice value) => LastRebornNotice = value;
        public void ReplaceDeathFatigue(DeathFatigueSnapshot value) => DeathFatigue = value;

        public void ApplyBossDefeated(BossDefeatedEvent value)
        {
            LastBossDefeated = value;
            if (!_territories.TryGetValue(value.SanctuaryId, out TerritorySnapshot territory)) return;
            TerritorySnapshot patched = territory.WithFirstBossReborn(value.BossId, value.RebornTime);
            if (!ReferenceEquals(patched, territory)) _territories[value.SanctuaryId] = patched;
        }

        public void ReplaceGuildMemberRank(GuildMemberRankSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            GuildMemberRank = value;
        }

        public void ReplaceKillLog(KillLogSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            _killLogs[KillLogKey(value.SanctuaryId, value.BossId)] = value;
        }

        public void ReplaceSanctuaryRank(SanctuaryRankSnapshot value)
        {
            value.Entries = Freeze(value.Entries);
            _sanctuaryRanks[value.SanctuaryId] = value;
        }

        public void ReplaceUnderAttack(BossNotice value) => LastUnderAttack = value;
        public void ReplaceSettlement(SettlementSnapshot value) => Settlement = value;
        public void SetFirstOpen(byte code) { HasFirstOpen = true; FirstOpenCode = code; }
        public void SetPointGain(uint point) { HasPointGain = true; LastPointGain = point; }
        public void SetFatigue(uint fatigue) { HasFatigue = true; Fatigue = fatigue; }
        public void SetFatigueGain(uint fatigue) { HasFatigueGain = true; LastFatigueGain = fatigue; }

        public bool TryGetTerritory(byte sanctuaryId, out TerritorySnapshot value) =>
            _territories.TryGetValue(sanctuaryId, out value);
        public bool TryGetKillLog(byte sanctuaryId, uint bossId, out KillLogSnapshot value) =>
            _killLogs.TryGetValue(KillLogKey(sanctuaryId, bossId), out value);
        public bool TryGetSanctuaryRank(byte sanctuaryId, out SanctuaryRankSnapshot value) =>
            _sanctuaryRanks.TryGetValue(sanctuaryId, out value);

        public void Reset()
        {
            HasError = false; LastErrorCode = 0; _territories.Clear(); GuildRank = null;
            HasActivityEndTime = false; ActivityEndTime = 0; LastRebornNotice = null;
            DeathFatigue = null; LastBossDefeated = null; GuildMemberRank = null;
            _killLogs.Clear(); _sanctuaryRanks.Clear(); LastUnderAttack = null; Settlement = null;
            HasFirstOpen = false; FirstOpenCode = 0; HasPointGain = false; LastPointGain = 0;
            HasFatigue = false; Fatigue = 0; HasFatigueGain = false; LastFatigueGain = 0;
        }

        private static ulong KillLogKey(byte sanctuaryId, uint bossId) =>
            ((ulong)sanctuaryId << 32) | bossId;

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0) return new T[0];
            var copy = new T[values.Count];
            for (int i = 0; i < values.Count; i++) copy[i] = values[i];
            return copy;
        }
    }
}
