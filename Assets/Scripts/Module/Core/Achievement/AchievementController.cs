using System; using System.Collections.Generic; using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.Achievement
{
    public sealed class AchievementController : BaseController
    {
        public static readonly AchievementController Instance = new AchievementController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private AchievementController() { }
        protected override void Register() { RegisterProtocal(Proto.ACHIEVEMENT_STAGE, On40901); RegisterProtocal(Proto.ACHIEVEMENT_ENTRIES, On40903); RegisterProtocal(Proto.ACHIEVEMENT_STAR, On40906); RegisterProtocal(Proto.ACHIEVEMENT_TYPES, On40908); }
        public void RequestStartup() { SendEmpty(Proto.ACHIEVEMENT_STAGE); SendEmpty(Proto.ACHIEVEMENT_ENTRIES); SendEmpty(Proto.ACHIEVEMENT_STAR); SendEmpty(Proto.ACHIEVEMENT_TYPES); }
        private void SendEmpty(int id)
        {
#if UNITY_EDITOR
            byte[] f = UserMsgAdapter.Encode(id, null, null); if (s_outboundIntercept != null && s_outboundIntercept(f)) return;
#endif
            SendFmt(id);
        }
        private void On40901(NetReader r) { byte stage = r.ReadU8(); int count = r.ReadU16(); var a = new List<AchievementModel.Reward>(count); for (int i = 0; i < count; i++) a.Add(new AchievementModel.Reward(r.ReadU32(), r.ReadU8())); AchievementModel.Instance.ReplaceStage(stage, a, r.ReadU16()); }
        private void On40903(NetReader r) { int count = r.ReadU16(); var a = new List<AchievementModel.Entry>(count); for (int i = 0; i < count; i++) a.Add(new AchievementModel.Entry(r.ReadU8(), r.ReadU32(), unchecked((ulong)r.ReadU64()), r.ReadU8())); AchievementModel.Instance.ReplaceEntries(a); }
        private void On40906(NetReader r) { AchievementModel.Instance.ReplaceStar(r.ReadU32()); }
        private void On40908(NetReader r) { int count = r.ReadU16(); var a = new List<AchievementModel.TypeStar>(count); for (int i = 0; i < count; i++) a.Add(new AchievementModel.TypeStar(r.ReadU16(), r.ReadU32(), r.ReadU32())); AchievementModel.Instance.ReplaceTypes(a); }
        public override void Dispose() { AchievementModel.Instance.Reset(); base.Dispose(); }
    }
}
