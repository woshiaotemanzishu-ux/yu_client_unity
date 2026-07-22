using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.GodBeast
{
    /// <summary>17301 幻兽总览控制器；同号推送只替换快照，不回环。</summary>
    public sealed class GodBeastController : BaseController
    {
        public static readonly GodBeastController Instance = new GodBeastController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private GodBeastController() { }
        protected override void Register() => RegisterProtocal(Proto.GODBEAST_OVERVIEW, On17301);
        public void RequestStartup() => SendEmpty();
        private void SendEmpty()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.GODBEAST_OVERVIEW, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.GODBEAST_OVERVIEW);
        }
        private void On17301(NetReader r)
        {
            byte fightCount = r.ReadU8(); int beastCount = r.ReadU16(); var beasts = new List<GodBeastModel.Beast>(beastCount);
            for (int i = 0; i < beastCount; i++) { uint id = r.ReadU32(); byte state = r.ReadU8(); uint score = r.ReadU32(); int ec = r.ReadU16(); var equips = new List<GodBeastModel.Equip>(ec); for (int j = 0; j < ec; j++) equips.Add(new GodBeastModel.Equip(r.ReadU8(), unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU32())); int ac = r.ReadU16(); var attrs = new List<GodBeastModel.Attr>(ac); for (int j = 0; j < ac; j++) attrs.Add(new GodBeastModel.Attr(r.ReadU16(), r.ReadU32())); beasts.Add(new GodBeastModel.Beast(id, state, score, equips, attrs)); }
            GodBeastModel.Instance.ReplaceData(fightCount, beasts);
        }
        public override void Dispose() { GodBeastModel.Instance.Reset(); base.Dispose(); }
    }
}
