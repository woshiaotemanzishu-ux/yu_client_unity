using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.GuildFight
{
    /// <summary>
    /// 领地战 506xx 安全读侧。保留老端启动与只读重拉顺序，但不恢复领奖、奖励分配、
    /// 选战区、离场、召集移动、自动战斗、弹窗、UI 或场景联动。
    /// </summary>
    public sealed class GuildFightController : BaseController
    {
        public static readonly GuildFightController Instance = new GuildFightController();

#if UNITY_EDITOR
        // 名称沿用最初 50603 专项，现统一拦截本控制器所有安全查询，便于无网络验收。
        private static Func<byte[], bool> s_enterOutboundIntercept = null;
#endif

        private GuildFightController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GUILD_FIGHT_STATE, On50600);
            RegisterProtocal(Proto.GUILD_FIGHT_OVERVIEW, On50601);
            RegisterProtocal(Proto.GUILD_FIGHT_ENTER, On50603);
            RegisterProtocal(Proto.GUILD_FIGHT_BATTLE, On50604);
            RegisterProtocal(Proto.GUILD_FIGHT_GUILD_UPDATE, On50606);
            RegisterProtocal(Proto.GUILD_FIGHT_OWN_UPDATE, On50607);
            RegisterProtocal(Proto.GUILD_FIGHT_RESULT, On50611);
            RegisterProtocal(Proto.GUILD_FIGHT_ROLE_SCORE, On50612);
            RegisterProtocal(Proto.GUILD_FIGHT_CONVENE, On50617);
            RegisterProtocal(Proto.GUILD_FIGHT_KILL_STREAK, On50619);
            RegisterProtocal(Proto.GUILD_FIGHT_ROUND, On50620);
            RegisterProtocal(Proto.GUILD_FIGHT_WARS, On50621);
            RegisterProtocal(Proto.GUILD_FIGHT_SERVERS, On50622);
            RegisterProtocal(Proto.GUILD_FIGHT_QUALIFICATION, On50624);
            RegisterProtocal(Proto.GUILD_FIGHT_QUALIFICATION_UPDATE, On50625);
            RegisterProtocal(Proto.GUILD_FIGHT_WAR_LIST_NOTICE, On50626);
            RegisterProtocal(Proto.GUILD_FIGHT_TERRITORY_NOTICE, On50627);
        }

        /// <summary>老端 GAME_START 精确子序列：50600→50601→50622→50624；不清旧快照。</summary>
        public void RequestStartup()
        {
            RequestState();
            RequestOverview();
            RequestServers();
            RequestQualification();
        }

        public void RequestState() => SendRequest(Proto.GUILD_FIGHT_STATE);
        public void RequestOverview() => SendRequest(Proto.GUILD_FIGHT_OVERVIEW);

        /// <summary>保留既有显式进入入口；仅发送老端实际使用的 type=1。</summary>
        public void RequestEnter() => SendRequest(Proto.GUILD_FIGHT_ENTER, "c", 1);

        /// <summary>战场内显式查询。服务端在非领地战场景可能不回包，未回包不得清旧快照。</summary>
        public void RequestBattle() => SendRequest(Proto.GUILD_FIGHT_BATTLE);
        public void RequestRound() => SendRequest(Proto.GUILD_FIGHT_ROUND);
        public void RequestWars() => SendRequest(Proto.GUILD_FIGHT_WARS);
        public void RequestServers() => SendRequest(Proto.GUILD_FIGHT_SERVERS);
        public void RequestQualification() => SendRequest(Proto.GUILD_FIGHT_QUALIFICATION);

        private void SendRequest(int protocolId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocolId, format, args);
            if (s_enterOutboundIntercept != null && s_enterOutboundIntercept(frame)) return;
#endif
            SendFmt(protocolId, format, args);
        }

        private void On50600(NetReader r)
        {
            byte warState = r.ReadU8();
            GuildFightModel.Instance.ReplaceState(
                warState, r.ReadU32(), r.ReadU32(), r.ReadU32());

            // 老端先按状态补资格/对阵，再查轮次；服务端自行负责活动开关。
            if (warState == 1) RequestQualification();
            else RequestWars();
            RequestRound();
        }

        private void On50601(NetReader r)
        {
            GuildFightModel.Instance.ReplaceOverview(
                r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU16(),
                r.ReadU8(), r.ReadU16(), unchecked((ulong)r.ReadU64()));
        }

        private void On50603(NetReader r)
        {
            GuildFightModel.Instance.ReplaceEnterResult(r.ReadU32(), r.ReadU8());
        }

        private void On50604(NetReader r)
        {
            uint territoryId = r.ReadU32();
            uint endTime = r.ReadU32();
            uint roleScore = r.ReadU32();
            List<GuildFightModel.BattleGuildEntry> guilds = r.ReadArray(ReadBattleGuild);
            List<byte> stages = r.ReadArray(rr => rr.ReadU8());
            List<GuildFightModel.BattleOwnEntry> owns = r.ReadArray(ReadBattleOwn);
            GuildFightModel.Instance.ReplaceBattle(
                territoryId, endTime, roleScore, guilds, stages, owns);
        }

        private void On50606(NetReader r)
        {
            List<GuildFightModel.GuildUpdateEntry> entries = r.ReadArray(rr =>
                new GuildFightModel.GuildUpdateEntry(
                    unchecked((ulong)rr.ReadU64()), rr.ReadU32(), ReadU32List(rr)));
            GuildFightModel.Instance.ApplyGuildUpdate(entries);
        }

        private void On50607(NetReader r) =>
            GuildFightModel.Instance.ApplyOwnUpdate(r.ReadArray(ReadBattleOwn));

        private void On50611(NetReader r)
        {
            uint territoryId = r.ReadU32();
            byte modeNumber = r.ReadU8();
            List<GuildFightModel.ResultGuildEntry> guilds = r.ReadArray(rr =>
                new GuildFightModel.ResultGuildEntry(
                    unchecked((ulong)rr.ReadU64()), rr.ReadU8(), rr.ReadString(),
                    rr.ReadU16(), rr.ReadU16(), rr.ReadU32(), ReadU32List(rr)));
            GuildFightModel.Instance.ReplaceResult(territoryId, modeNumber, guilds);

            // 对标老端：结算后重新获取轮次，再取世界状态。
            RequestRound();
            RequestState();
        }

        private void On50612(NetReader r) =>
            GuildFightModel.Instance.ReplaceRoleScore(r.ReadU32());

        private void On50617(NetReader r) =>
            GuildFightModel.Instance.ReplaceConvene(r.ReadU32());

        private void On50619(NetReader r)
        {
            GuildFightModel.Instance.ReplaceKillStreak(new GuildFightModel.KillStreakSnapshot(
                r.ReadU16(), unchecked((ulong)r.ReadU64()), r.ReadString(), r.ReadU8(),
                r.ReadU8(), r.ReadU8(), r.ReadU8(), r.ReadU16(), r.ReadString(),
                r.ReadU32(), r.ReadU32()));
        }

        private void On50620(NetReader r)
        {
            GuildFightModel.Instance.ReplaceRound(r.ReadU8(), r.ReadU32(), r.ReadU32());
            RequestWars();
        }

        private void On50621(NetReader r)
        {
            List<GuildFightModel.WarEntry> wars = r.ReadArray(rr =>
                new GuildFightModel.WarEntry(
                    rr.ReadU32(), unchecked((ulong)rr.ReadU64()), rr.ReadString(),
                    rr.ReadU16(), rr.ReadU16(), unchecked((ulong)rr.ReadU64()),
                    rr.ReadString(), rr.ReadU16(), rr.ReadU16(),
                    unchecked((ulong)rr.ReadU64())));
            GuildFightModel.Instance.ReplaceWars(wars);
        }

        private void On50622(NetReader r)
        {
            byte modeNumber = r.ReadU8();
            ushort averageWorldLevel = r.ReadU16();
            List<GuildFightModel.ServerEntry> servers = r.ReadArray(rr =>
                new GuildFightModel.ServerEntry(
                    rr.ReadU16(), rr.ReadU16(), rr.ReadString(), rr.ReadU16()));
            GuildFightModel.Instance.ReplaceServers(modeNumber, averageWorldLevel, servers);
        }

        private void On50624(NetReader r) =>
            GuildFightModel.Instance.ReplaceQualification(r.ReadU8(), r.ReadU8());

        private void On50625(NetReader r)
        {
            byte qualification = r.ReadU8();
            RequestRound();
            RequestState();
            GuildFightModel.Instance.ApplyQualificationUpdate(qualification);
        }

        private void On50626(NetReader r) =>
            GuildFightModel.Instance.ReplaceWarListNotice(r.ReadU8());

        private void On50627(NetReader r) =>
            GuildFightModel.Instance.ReplaceTerritoryNotice(r.ReadU32());

        private static GuildFightModel.BattleGuildEntry ReadBattleGuild(NetReader r) =>
            new GuildFightModel.BattleGuildEntry(
                unchecked((ulong)r.ReadU64()), r.ReadString(), r.ReadU16(), r.ReadU16(),
                r.ReadU32(), ReadU32List(r));

        private static GuildFightModel.BattleOwnEntry ReadBattleOwn(NetReader r) =>
            new GuildFightModel.BattleOwnEntry(
                r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadString(),
                r.ReadU32(), r.ReadU32(), r.ReadU32());

        private static List<uint> ReadU32List(NetReader r) => r.ReadArray(rr => rr.ReadU32());

        public override void Dispose()
        {
            GuildFightModel.Instance.Reset();
            base.Dispose();
        }
    }
}
