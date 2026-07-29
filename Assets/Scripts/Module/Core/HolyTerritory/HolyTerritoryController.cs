using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HolyTerritory
{
    /// <summary>神陨禁区 283 协议族的安全读侧；不开放进场、关注或每日首开计数写操作。</summary>
    public sealed class HolyTerritoryController : BaseController
    {
        public static readonly HolyTerritoryController Instance = new HolyTerritoryController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept = null;
#endif
        private HolyTerritoryController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HOLY_TERRITORY_ERROR, On28300);
            RegisterProtocal(Proto.HOLY_TERRITORY_INFO, On28301);
            RegisterProtocal(Proto.HOLY_TERRITORY_GUILD_RANK, On28302);
            RegisterProtocal(Proto.HOLY_TERRITORY_ACTIVITY_END, On28306);
            RegisterProtocal(Proto.HOLY_TERRITORY_REBORN_NOTICE, On28307);
            RegisterProtocal(Proto.HOLY_TERRITORY_DEATH_FATIGUE, On28308);
            RegisterProtocal(Proto.HOLY_TERRITORY_BOSS_DEFEATED, On28309);
            RegisterProtocal(Proto.HOLY_TERRITORY_GUILD_MEMBER_RANK, On28310);
            RegisterProtocal(Proto.HOLY_TERRITORY_KILL_LOG, On28311);
            RegisterProtocal(Proto.HOLY_TERRITORY_SANCTUARY_RANK, On28312);
            RegisterProtocal(Proto.HOLY_TERRITORY_UNDER_ATTACK, On28313);
            RegisterProtocal(Proto.HOLY_TERRITORY_LAST_SETTLEMENT, On28314);
            RegisterProtocal(Proto.HOLY_TERRITORY_FIRST_OPEN, On28316);
            RegisterProtocal(Proto.HOLY_TERRITORY_POINT_GAIN, On28317);
            RegisterProtocal(Proto.HOLY_TERRITORY_FATIGUE, On28318);
            RegisterProtocal(Proto.HOLY_TERRITORY_FATIGUE_GAIN, On28319);
        }

        /// <summary>老端 GAME_START 精确顺序：三层领地、上次结算、首开状态、绝对疲劳值。</summary>
        public void RequestStartup()
        {
            RequestTerritoryInfo(1);
            RequestTerritoryInfo(2);
            RequestTerritoryInfo(3);
            RequestSettlement();
            RequestFirstOpen();
            RequestFatigue();
        }

        public void RequestTerritoryInfo(byte sanctuaryId) =>
            SendRead(Proto.HOLY_TERRITORY_INFO, "c", sanctuaryId);
        public void RequestGuildRank() => SendRead(Proto.HOLY_TERRITORY_GUILD_RANK);
        public void RequestDeathFatigue() => SendRead(Proto.HOLY_TERRITORY_DEATH_FATIGUE);
        public void RequestGuildMemberRank() => SendRead(Proto.HOLY_TERRITORY_GUILD_MEMBER_RANK);
        public void RequestKillLog(byte sanctuaryId, uint bossId) =>
            SendRead(Proto.HOLY_TERRITORY_KILL_LOG, "ci", sanctuaryId, bossId);
        public void RequestSanctuaryMemberRank(byte sanctuaryId) =>
            SendRead(Proto.HOLY_TERRITORY_SANCTUARY_RANK, "c", sanctuaryId);
        public void RequestSettlement() => SendRead(Proto.HOLY_TERRITORY_LAST_SETTLEMENT);
        public void RequestFirstOpen() => SendRead(Proto.HOLY_TERRITORY_FIRST_OPEN);
        public void RequestFatigue() => SendRead(Proto.HOLY_TERRITORY_FATIGUE);

        private void SendRead(int protocolId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocolId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protocolId, format, args);
        }

        private void On28300(NetReader r) => HolyTerritoryModel.Instance.SetError(r.ReadU32());

        private void On28301(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceTerritory(new HolyTerritoryModel.TerritorySnapshot
            {
                SanctuaryId = r.ReadU8(), Point = r.ReadU32(),
                BelongGuildId = unchecked((ulong)r.ReadU64()), BelongGuildName = r.ReadString(),
                EndTime = r.ReadU32(),
                Bosses = r.ReadArray(rr => new HolyTerritoryModel.BossEntry
                {
                    BossId = rr.ReadU32(), RebornTime = rr.ReadU32(), IsRemind = rr.ReadU8()
                })
            });
        }

        private void On28302(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceGuildRank(new HolyTerritoryModel.GuildRankSnapshot
            {
                MyGuildRank = r.ReadU32(), MyGuildTopTenPower = unchecked((ulong)r.ReadU64()),
                Entries = r.ReadArray(rr => new HolyTerritoryModel.GuildRankEntry
                {
                    GuildName = rr.ReadString(), ChairmanName = rr.ReadString(), Rank = rr.ReadU32(),
                    MemberNum = rr.ReadU32(), AllNum = rr.ReadU32(),
                    AveragePower = unchecked((ulong)rr.ReadU64())
                })
            });
        }

        private void On28306(NetReader r) => HolyTerritoryModel.Instance.SetActivityEndTime(r.ReadU32());
        private void On28307(NetReader r) => HolyTerritoryModel.Instance.ReplaceRebornNotice(ReadBossNotice(r));

        private void On28308(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceDeathFatigue(new HolyTerritoryModel.DeathFatigueSnapshot
            {
                DieTimes = r.ReadU16(), Time = r.ReadU32(), DebuffTime = r.ReadU32(), SafeTime = r.ReadU32()
            });
        }

        private void On28309(NetReader r)
        {
            HolyTerritoryModel.Instance.ApplyBossDefeated(new HolyTerritoryModel.BossDefeatedEvent
            {
                SanctuaryId = r.ReadU8(), BossId = r.ReadU32(), RebornTime = r.ReadU32()
            });
        }

        private void On28310(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceGuildMemberRank(new HolyTerritoryModel.GuildMemberRankSnapshot
            {
                MyRank = r.ReadU32(), MyPower = unchecked((ulong)r.ReadU64()),
                Entries = r.ReadArray(rr => new HolyTerritoryModel.GuildMemberRankEntry
                {
                    RoleId = unchecked((ulong)rr.ReadU64()), Rank = rr.ReadU32(), Picture = rr.ReadString(),
                    PictureVersion = rr.ReadU32(), Career = rr.ReadU8(), RoleName = rr.ReadString(),
                    Power = unchecked((ulong)rr.ReadU64()), DesignationId = rr.ReadU32()
                })
            });
        }

        private void On28311(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceKillLog(new HolyTerritoryModel.KillLogSnapshot
            {
                SanctuaryId = r.ReadU8(), BossId = r.ReadU32(),
                Entries = r.ReadArray(rr => new HolyTerritoryModel.KillLogEntry
                {
                    Time = rr.ReadU32(), Name = rr.ReadString(), IsShow = rr.ReadU8(),
                    ReducePoint = rr.ReadU32()
                })
            });
        }

        private void On28312(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceSanctuaryRank(new HolyTerritoryModel.SanctuaryRankSnapshot
            {
                SanctuaryId = r.ReadU8(),
                Entries = r.ReadArray(rr => new HolyTerritoryModel.SanctuaryRankEntry
                {
                    Rank = rr.ReadU32(), RoleName = rr.ReadString(),
                    Power = unchecked((ulong)rr.ReadU64()), DesignationId = rr.ReadU32()
                })
            });
        }

        private void On28313(NetReader r) => HolyTerritoryModel.Instance.ReplaceUnderAttack(ReadBossNotice(r));

        private void On28314(NetReader r)
        {
            HolyTerritoryModel.Instance.ReplaceSettlement(new HolyTerritoryModel.SettlementSnapshot
            {
                GuildRank = r.ReadU32(), SanctuaryId = r.ReadU8(),
                PersonRank = r.ReadU32(), DesignationId = r.ReadU32()
            });
        }

        private void On28316(NetReader r) => HolyTerritoryModel.Instance.SetFirstOpen(r.ReadU8());
        private void On28317(NetReader r) => HolyTerritoryModel.Instance.SetPointGain(r.ReadU32());
        private void On28318(NetReader r) => HolyTerritoryModel.Instance.SetFatigue(r.ReadU32());
        private void On28319(NetReader r) => HolyTerritoryModel.Instance.SetFatigueGain(r.ReadU32());

        private static HolyTerritoryModel.BossNotice ReadBossNotice(NetReader r) =>
            new HolyTerritoryModel.BossNotice { SanctuaryId = r.ReadU8(), BossId = r.ReadU32() };

        public override void Dispose()
        {
            HolyTerritoryModel.Instance.Reset();
            base.Dispose();
        }
    }
}
