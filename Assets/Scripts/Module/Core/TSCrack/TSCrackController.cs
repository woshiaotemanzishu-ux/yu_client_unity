using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.TSCrack
{
    /// <summary>
    /// 时空圣痕只读协议切片。GAME_START 仍只请求 20411；其余快照只允许显式请求或接收服务端推送，
    /// 不复刻老端 20411 status==1 后的五连请求，也不接驻扎/领奖操作。
    /// </summary>
    public sealed class TSCrackController : BaseController
    {
        public static readonly TSCrackController Instance = new TSCrackController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private TSCrackController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TS_CRACK_MAIN_INFO, On20401);
            RegisterProtocal(Proto.TS_CRACK_CASTLE_INFO, On20402);
            RegisterProtocal(Proto.TS_CRACK_DAILY_ACTIVITY, On20404);
            RegisterProtocal(Proto.TS_CRACK_DAILY_REWARD, On20405);
            RegisterProtocal(Proto.TS_CRACK_SEASON_GOAL, On20407);
            RegisterProtocal(Proto.TS_CRACK_PERSON_RANK, On20409);
            RegisterProtocal(Proto.TS_CRACK_CURRENT_CASTLE, On20410);
            RegisterProtocal(Proto.TS_CRACK_WORLD_INFO, On20411);
        }

        /// <summary>GAME_START 唯一调用的启动查询。</summary>
        public void RequestInfo() => SendEmpty(Proto.TS_CRACK_WORLD_INFO);

        public void RequestMainInfo() => SendEmpty(Proto.TS_CRACK_MAIN_INFO);

        public void RequestCastleInfo(ushort castleId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.TS_CRACK_CASTLE_INFO, "h", castleId);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.TS_CRACK_CASTLE_INFO, "h", castleId);
        }

        public void RequestDailyActivities() => SendEmpty(Proto.TS_CRACK_DAILY_ACTIVITY);
        public void RequestDailyRewards() => SendEmpty(Proto.TS_CRACK_DAILY_REWARD);
        public void RequestSeasonGoals() => SendEmpty(Proto.TS_CRACK_SEASON_GOAL);
        public void RequestPersonalRanks() => SendEmpty(Proto.TS_CRACK_PERSON_RANK);
        public void RequestCurrentCastle() => SendEmpty(Proto.TS_CRACK_CURRENT_CASTLE);

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }

        private void On20401(NetReader reader)
        {
            uint myValue = reader.ReadU32();
            uint myServerValue = reader.ReadU32();
            int count = reader.ReadU16();
            var castles = new List<TSCrackModel.CastleEntry>(count);
            for (int i = 0; i < count; i++) castles.Add(ReadCastle(reader));
            TSCrackModel.Instance.ReplaceMain(myValue, myServerValue, castles);
        }

        private void On20402(NetReader reader)
        {
            TSCrackModel.Instance.ReplaceCastleDetail(ReadCastle(reader));
        }

        private void On20404(NetReader reader)
        {
            int count = reader.ReadU16();
            var entries = new List<TSCrackModel.DailyActivityEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new TSCrackModel.DailyActivityEntry(
                    reader.ReadU16(), reader.ReadU16(), reader.ReadU32()));
            }

            TSCrackModel.Instance.ReplaceDailyActivities(entries);
        }

        private void On20405(NetReader reader)
        {
            uint value = reader.ReadU32();
            uint totalValue = reader.ReadU32();
            int count = reader.ReadU16();
            var entries = new List<TSCrackModel.DailyRewardEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new TSCrackModel.DailyRewardEntry(reader.ReadU8(), reader.ReadU8()));
            }

            TSCrackModel.Instance.ReplaceDailyRewards(value, totalValue, entries);
        }

        private void On20407(NetReader reader)
        {
            int count = reader.ReadU16();
            var entries = new List<TSCrackModel.SeasonGoalEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new TSCrackModel.SeasonGoalEntry(
                    reader.ReadU16(), reader.ReadU32(), reader.ReadU8()));
            }

            TSCrackModel.Instance.ReplaceSeasonGoals(entries);
        }

        private void On20409(NetReader reader)
        {
            int count = reader.ReadU16();
            var entries = new List<TSCrackModel.RankEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new TSCrackModel.RankEntry(
                    reader.ReadU32(), reader.ReadU64(), reader.ReadString(), reader.ReadU32()));
            }

            TSCrackModel.Instance.ReplacePersonalRanks(entries);
        }

        private void On20410(NetReader reader)
        {
            TSCrackModel.Instance.ReplaceCurrentCastle(reader.ReadU32());
        }

        private void On20411(NetReader reader)
        {
            byte status = reader.ReadU8();
            int count = reader.ReadU16();
            var servers = new List<TSCrackModel.ServerEntry>(count);

            for (int i = 0; i < count; i++)
            {
                uint serverNumber = reader.ReadU32();
                string serverName = reader.ReadString();
                ushort level = reader.ReadU16();
                servers.Add(new TSCrackModel.ServerEntry(serverNumber, serverName, level));
            }

            TSCrackModel.Instance.Replace(status, servers);
        }

        private static TSCrackModel.CastleEntry ReadCastle(NetReader reader)
        {
            ushort castleId = reader.ReadU16();
            uint baseServerNumber = reader.ReadU32();
            uint needValue = reader.ReadU32();
            uint serverNumber = reader.ReadU32();
            string serverName = reader.ReadString();

            int serverCount = reader.ReadU16();
            var servers = new List<TSCrackModel.CastleServerEntry>(serverCount);
            for (int i = 0; i < serverCount; i++)
            {
                servers.Add(new TSCrackModel.CastleServerEntry(
                    reader.ReadU32(), reader.ReadString(), reader.ReadU32()));
            }

            int roleCount = reader.ReadU16();
            var roles = new List<TSCrackModel.CastleRoleEntry>(roleCount);
            for (int i = 0; i < roleCount; i++)
            {
                roles.Add(new TSCrackModel.CastleRoleEntry(
                    reader.ReadU32(), reader.ReadString(), reader.ReadU32(), reader.ReadU8()));
            }

            return new TSCrackModel.CastleEntry(
                castleId,
                baseServerNumber,
                needValue,
                serverNumber,
                serverName,
                servers,
                roles,
                reader.ReadU16(),
                reader.ReadU16());
        }

        public override void Dispose()
        {
            TSCrackModel.Instance.Reset();
            base.Dispose();
        }
    }
}
