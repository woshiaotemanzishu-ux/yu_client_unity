using System; using System.Collections.Generic; using Shenxiao.Framework.Net;
namespace Shenxiao.Module.Core.Demon
{
    public sealed class DemonController : BaseController
    {
        public static readonly DemonController Instance = new DemonController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private DemonController() { }
        protected override void Register() { RegisterProtocal(Proto.DEMON_INFO, On18301); RegisterProtocal(Proto.DEMON_POWER, On18302); RegisterProtocal(Proto.DEMON_FETTERS, On18303); RegisterProtocal(Proto.DEMON_PAINTINGS, On18307); RegisterProtocal(Proto.DEMON_BLESSING, On50901); RegisterProtocal(Proto.DEMON_TALENT_SHOP, On18311); RegisterProtocal(Proto.DEMON_TALENT_POWER, On18314); }
        /// <summary>受控简化：当前未移植 DemonMainView 开放门控，18301/18303/18307/50901 均为无参只读快照，故登录各拉取一次。</summary>
        public void RequestStartup() { SendEmpty(Proto.DEMON_INFO); SendEmpty(Proto.DEMON_FETTERS); SendEmpty(Proto.DEMON_PAINTINGS); SendEmpty(Proto.DEMON_BLESSING); }
        public void RequestTalentShop() => SendEmpty(Proto.DEMON_TALENT_SHOP);
        public void RequestPower(uint demonsId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DEMON_POWER, "i", new object[] { demonsId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DEMON_POWER, "i", demonsId);
        }
        /// <summary>显式查询天赋技能真实战力；不绑定 GAME_START。</summary>
        public void RequestTalentPower(uint demonsId, byte sign, uint id, ushort skillLv)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.DEMON_TALENT_POWER, "icih", new object[] { demonsId, sign, id, skillLv });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.DEMON_TALENT_POWER, "icih", demonsId, sign, id, skillLv);
        }
        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null); if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }
        private void On18301(NetReader r)
        {
            byte openState = r.ReadU8(); int count = r.ReadU16(); var demons = new List<DemonModel.Entry>(count);
            for (int i = 0; i < count; i++) demons.Add(ReadEntry(r));
            DemonModel.Instance.Replace(openState, demons);
        }
        private void On18302(NetReader r) { DemonModel.Instance.ReplaceDemonPower(r.ReadU32(), r.ReadU32()); }
        private void On18303(NetReader r) { int count = r.ReadU16(); var fetters = new List<uint>(count); for (int i = 0; i < count; i++) fetters.Add(r.ReadU32()); DemonModel.Instance.ReplaceFetters(fetters); }
        private void On18307(NetReader r) { int count = r.ReadU16(); var paintings = new List<byte>(count); for (int i = 0; i < count; i++) paintings.Add(r.ReadU8()); DemonModel.Instance.ReplacePaintings(paintings); }
        private void On50901(NetReader r) { DemonModel.Instance.ReplaceBlessing(r.ReadU32()); }
        private void On18311(NetReader r)
        {
            uint refreshTime = r.ReadU32(); ushort refreshNum = r.ReadU16();
            var cost = r.ReadArray(rr => new DemonModel.ObjectEntry(rr.ReadU8(), rr.ReadU32(), rr.ReadU32()));
            var shop = r.ReadArray(rr => new DemonModel.TalentShopEntry(rr.ReadU32(), rr.ReadU32(), rr.ReadU32(), rr.ReadU16(), rr.ReadU16(), rr.ReadU8(), rr.ReadU16(), rr.ReadU16()));
            DemonModel.Instance.ReplaceTalentShop(refreshTime, refreshNum, cost, shop);
        }
        private void On18314(NetReader r)
        {
            uint power = r.ReadU32(); uint demonsId = r.ReadU32(); byte sign = r.ReadU8(); uint skillId = r.ReadU32(); ushort skillLv = r.ReadU16(); uint code = r.ReadU32();
            if (code == 1) DemonModel.Instance.ReplaceTalentPower(new DemonModel.TalentPower(power, demonsId, sign, skillId, skillLv, code));
        }
        private static DemonModel.Entry ReadEntry(NetReader r)
        {
            uint id = r.ReadU32(); ushort level = r.ReadU16(); uint exp = r.ReadU32(); byte star = r.ReadU8(); byte slotNumber = r.ReadU8(); int skillCount = r.ReadU16(); var skills = new List<DemonModel.Skill>(skillCount);
            for (int i = 0; i < skillCount; i++) skills.Add(new DemonModel.Skill(r.ReadU32(), r.ReadU16(), r.ReadU32(), r.ReadU8()));
            int slotSkillCount = r.ReadU16(); var slotSkills = new List<DemonModel.SlotSkill>(slotSkillCount);
            for (int i = 0; i < slotSkillCount; i++) slotSkills.Add(new DemonModel.SlotSkill(r.ReadU32(), r.ReadU16(), r.ReadU8(), r.ReadU8(), r.ReadU16()));
            return new DemonModel.Entry(id, level, exp, star, slotNumber, skills, slotSkills);
        }
        public override void Dispose() { DemonModel.Instance.Reset(); base.Dispose(); }
    }
}
