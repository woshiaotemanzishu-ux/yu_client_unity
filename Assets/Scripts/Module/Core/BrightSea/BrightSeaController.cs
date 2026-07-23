using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.BrightSea
{
    /// <summary>无尽之海仅接 18900 主快照；不接航运、抢夺、场景或 UI 链。</summary>
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
    }
}
