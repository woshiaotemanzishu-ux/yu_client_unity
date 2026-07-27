using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.SnatchTreasure
{
    /// <summary>领地夺宝 65201 入口信息与 65206 退出结果；65208 仍归 ActivityForeshow。</summary>
    public sealed class SnatchTreasureController : BaseController
    {
        public static readonly SnatchTreasureController Instance = new SnatchTreasureController();
        private SnatchTreasureController() { }

        // CliVerify 临时截获真实编码帧；Player 不包含该缝。
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept = null;
#endif

        protected override void Register()
        {
            RegisterProtocal(Proto.SNATCH_TREASURE_ENTRY_INFO, On65201);
            RegisterProtocal(Proto.SNATCH_TREASURE_EXIT, On65206);
        }

        public override void Dispose()
        {
            SnatchTreasureModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>65201 入口读取，严格空包，不自动绑定 GAME_START。</summary>
        public void RequestEntryInfo() => SendEmpty(Proto.SNATCH_TREASURE_ENTRY_INFO);

        /// <summary>65206 退出领地夺宝，严格空包；切场由服务端权威执行。</summary>
        public void RequestExit() => SendEmpty(Proto.SNATCH_TREASURE_EXIT);

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(protoId);
        }

        // 65201: belong_list:u16×{dunid:u32,score:u16,guild_id:u64,guild_name:string}, territory_score:u16, have_territory:u8.
        private void On65201(NetReader r)
        {
            ushort count = r.ReadU16();
            var list = new List<SnatchTreasureModel.BelongEntry>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new SnatchTreasureModel.BelongEntry
                {
                    DunId = r.ReadU32(),
                    Score = r.ReadU16(),
                    GuildId = unchecked((ulong)r.ReadU64()),
                    GuildName = r.ReadString()
                });
            }
            ushort territoryScore = r.ReadU16();
            byte haveTerritory = r.ReadU8();
            SnatchTreasureModel.Instance.ReplaceEntryInfo(list, territoryScore, haveTerritory);
        }

        private void On65206(NetReader r)
        {
            SnatchTreasureModel.Instance.ReplaceExitResult(r.ReadU32());
        }
    }
}
