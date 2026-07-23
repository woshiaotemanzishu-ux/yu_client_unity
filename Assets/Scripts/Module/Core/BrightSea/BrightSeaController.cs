using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.BrightSea
{
    /// <summary>无尽之海仅接 18900 主快照、显式 18901 日志和显式 18915 跨服快照；不接航运、抢夺、场景或 UI 链。</summary>
    public sealed class BrightSeaController : BaseController
    {
        public static readonly BrightSeaController Instance = new BrightSeaController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private BrightSeaController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.BRIGHT_SEA_INFO, On18900);
            RegisterProtocal(Proto.BRIGHT_SEA_CRUISE_LOGS, On18901);
            RegisterProtocal(Proto.BRIGHT_SEA_SERVER_INFO, On18915);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            BrightSeaModel.Instance.Clear();
            base.Dispose();
        }

        private void OnGameStart()
        {
            BrightSeaModel.Instance.Clear();
            RequestInfo();
        }

        /// <summary>请求 18900 无尽之海完整主快照（严格空包）。</summary>
        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.BRIGHT_SEA_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.BRIGHT_SEA_INFO);
            GameLog.Info("BrightSea", "request 18900 bright sea info");
        }

        /// <summary>请求 18901 巡航/掠夺记录完整快照（严格空包，不绑定 GAME_START）。</summary>
        public void RequestCruiseLogs()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.BRIGHT_SEA_CRUISE_LOGS, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.BRIGHT_SEA_CRUISE_LOGS);
            GameLog.Info("BrightSea", "request 18901 bright sea cruise logs");
        }

        /// <summary>请求 18915 跨服信息完整快照（严格空包，不绑定 GAME_START）。</summary>
        public void RequestServerInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.BRIGHT_SEA_SERVER_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.BRIGHT_SEA_SERVER_INFO);
            GameLog.Info("BrightSea", "request 18915 bright sea server info");
        }

        private void On18900(NetReader r)
        {
            string picture = r.ReadString();
            uint pictureVersion = r.ReadU32();
            byte rewardTimes = r.ReadU8();
            byte totalRewardTimes = r.ReadU8();
            byte robTimes = r.ReadU8();
            byte totalRobTimes = r.ReadU8();
            ulong autoId = unchecked((ulong)r.ReadU64());
            byte status = r.ReadU8();
            List<BrightSeaModel.ShippingEntry> sendList = r.ReadArray(ReadShippingEntry);
            BrightSeaModel.Instance.Replace(picture, pictureVersion, rewardTimes, totalRewardTimes,
                robTimes, totalRobTimes, autoId, status, sendList);
            GameLog.Info("BrightSea", "18900 status={0} ships={1} remaining={2}B", status, sendList.Count, r.Remaining);
        }

        private static BrightSeaModel.ShippingEntry ReadShippingEntry(NetReader r)
        {
            return new BrightSeaModel.ShippingEntry
            {
                AutoId = unchecked((ulong)r.ReadU64()),
                ShippingId = r.ReadU8(),
                ServerId = r.ReadU32(),
                ServerNumber = r.ReadU32(),
                GuildId = unchecked((ulong)r.ReadU64()),
                GuildName = r.ReadString(),
                RoleId = unchecked((ulong)r.ReadU64()),
                RoleName = r.ReadString(),
                RoleLevel = r.ReadU16(),
                Power = unchecked((ulong)r.ReadU64()),
                Sex = r.ReadU8(),
                Career = r.ReadU16(),
                Turn = r.ReadU8(),
                Picture = r.ReadString(),
                PictureVersion = r.ReadU32(),
                EndTime = r.ReadU32(),
                RobTimes = r.ReadU8(),
            };
        }

        private void On18901(NetReader r)
        {
            List<BrightSeaModel.CruiseLogEntry> logs = r.ReadArray(ReadCruiseLogEntry);
            BrightSeaModel.Instance.ReplaceCruiseLogs(logs);
            GameLog.Info("BrightSea", "18901 logs={0} remaining={1}B", logs.Count, r.Remaining);
        }

        private static BrightSeaModel.CruiseLogEntry ReadCruiseLogEntry(NetReader r)
        {
            var entry = new BrightSeaModel.CruiseLogEntry
            {
                AutoId = unchecked((ulong)r.ReadU64()),
                Type = r.ReadU8(),
                RoberServerId = r.ReadU32(),
                RoberServerNumber = r.ReadU32(),
                RoberGuildId = unchecked((ulong)r.ReadU64()),
                RoberGuildName = r.ReadString(),
                RoberId = unchecked((ulong)r.ReadU64()),
                RoberName = r.ReadString(),
                RoberPower = unchecked((ulong)r.ReadU64()),
                ShippingId = r.ReadU8(),
            };
            entry.Reward.AddRange(r.ReadArray(ReadObjectEntry));
            entry.BackList.AddRange(r.ReadArray(ReadObjectEntry));
            entry.ReceiveList.AddRange(r.ReadArray(ReadObjectEntry));
            entry.Time = r.ReadU32();
            return entry;
        }

        private static BrightSeaModel.ObjectEntry ReadObjectEntry(NetReader r)
        {
            return new BrightSeaModel.ObjectEntry { Type = r.ReadU8(), TypeId = r.ReadU32(), Num = r.ReadU32() };
        }

        private void On18915(NetReader r)
        {
            byte treasureModule = r.ReadU8();
            ushort worldLevel = r.ReadU16();
            List<BrightSeaModel.ServerEntry> enemyServers = r.ReadArray(ReadServerEntry);
            byte unsatisfiedModule = r.ReadU8();
            ushort unsatisfiedWorldLevel = r.ReadU16();
            ushort minWorldLevel = r.ReadU16();
            List<BrightSeaModel.ServerEntry> unsatisfiedServers = r.ReadArray(ReadServerEntry);
            BrightSeaModel.Instance.ReplaceServerInfo(treasureModule, worldLevel, enemyServers,
                unsatisfiedModule, unsatisfiedWorldLevel, minWorldLevel, unsatisfiedServers);
            GameLog.Info("BrightSea", "18915 mod={0} enemy={1} unsatisfied={2} remaining={3}B",
                treasureModule, enemyServers.Count, unsatisfiedServers.Count, r.Remaining);
        }

        private static BrightSeaModel.ServerEntry ReadServerEntry(NetReader r)
        {
            return new BrightSeaModel.ServerEntry
            {
                ServerId = r.ReadU32(),
                ServerNumber = r.ReadU16(),
                ServerName = r.ReadString(),
                WorldLevel = r.ReadU16(),
            };
        }
    }
}
