using System; using System.Collections.Generic; using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.Revelation
{
    public sealed class RevelationController : BaseController
    {
        public static readonly RevelationController Instance = new RevelationController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private RevelationController() { }
        // 28600 与 28601-05/07 受既有负约束排除；28608 当前服务端从不写（R539 KILL）。
        protected override void Register() { RegisterProtocal(Proto.REVELATION_INFO, On28606); RegisterProtocal(Proto.REVELATION_POWER, On28609); }
        public void RequestStartup()
        {
#if UNITY_EDITOR
            byte[] f = UserMsgAdapter.Encode(Proto.REVELATION_INFO, null, null); if (s_outboundIntercept != null && s_outboundIntercept(f)) return;
#endif
            SendFmt(Proto.REVELATION_INFO);
        }
        private void On28606(NetReader r) { ushort max = r.ReadU16(); ushort current = r.ReadU16(); ulong power = unchecked((ulong)r.ReadU64()); int gc = r.ReadU16(); var g = new List<RevelationModel.Gathering>(gc); for (int i = 0; i < gc; i++) g.Add(new RevelationModel.Gathering(r.ReadU8(), r.ReadU16(), r.ReadU32(), r.ReadU8())); int sc = r.ReadU16(); var s = new List<RevelationModel.Suit>(sc); for (int i = 0; i < sc; i++) s.Add(new RevelationModel.Suit(r.ReadU32(), r.ReadU32())); int kc = r.ReadU16(); var k = new List<RevelationModel.Skill>(kc); for (int i = 0; i < kc; i++) k.Add(new RevelationModel.Skill(r.ReadU32(), r.ReadU16())); RevelationModel.Instance.Replace(max, current, power, g, s, k); }
        public void RequestPower()
        {
#if UNITY_EDITOR
            byte[] f = UserMsgAdapter.Encode(Proto.REVELATION_POWER, null, null); if (s_outboundIntercept != null && s_outboundIntercept(f)) return;
#endif
            SendFmt(Proto.REVELATION_POWER);
        }
        private void On28609(NetReader r) { RevelationModel.Instance.ReplacePowerIfLoaded(unchecked((ulong)r.ReadU64())); }
        public override void Dispose() { RevelationModel.Instance.Reset(); base.Dispose(); }
    }
}
