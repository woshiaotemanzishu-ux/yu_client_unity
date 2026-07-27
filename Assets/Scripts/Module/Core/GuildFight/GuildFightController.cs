using System;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.GuildFight
{
    public sealed class GuildFightController : BaseController
    {
        public static readonly GuildFightController Instance = new GuildFightController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_enterOutboundIntercept = null;
#endif

        private GuildFightController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GUILD_FIGHT_ENTER, On50603);
        }

        public void RequestEnter()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.GUILD_FIGHT_ENTER, "c", new object[] { 1 });
            if (s_enterOutboundIntercept != null && s_enterOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.GUILD_FIGHT_ENTER, "c", 1);
        }

        private void On50603(NetReader reader)
        {
            uint errorCode = reader.ReadU32();
            byte type = reader.ReadU8();
            GuildFightModel.Instance.ReplaceEnterResult(errorCode, type);
        }

        public override void Dispose()
        {
            GuildFightModel.Instance.Reset();
            base.Dispose();
        }
    }
}
