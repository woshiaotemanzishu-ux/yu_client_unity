using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HolyBattle
{
    /// <summary>
    /// 圣灵战场的请求回全量快照。仅接收 21801；不复刻老端同启 21805，
    /// 也不在 21801 后自动请求 21811。
    /// </summary>
    public sealed class HolyBattleController : BaseController
    {
        public static readonly HolyBattleController Instance = new HolyBattleController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private HolyBattleController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HOLY_BATTLE_INFO, On21801);
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.HOLY_BATTLE_INFO);
        }

        private void On21801(NetReader reader)
        {
            byte mod = reader.ReadU8();
            byte status = reader.ReadU8();
            uint endTime = reader.ReadU32();
            int count = reader.ReadU16();
            var servers = new List<HolyBattleModel.ServerEntry>(count);

            for (int i = 0; i < count; i++)
            {
                uint serverId = reader.ReadU32();
                uint serverNumber = reader.ReadU32();
                string serverName = reader.ReadString();
                uint level = reader.ReadU32();
                servers.Add(new HolyBattleModel.ServerEntry(serverId, serverNumber, serverName, level));
            }

            HolyBattleModel.Instance.Replace(mod, status, endTime, servers);
        }

        public override void Dispose()
        {
            HolyBattleModel.Instance.Reset();
            base.Dispose();
        }
    }
}
