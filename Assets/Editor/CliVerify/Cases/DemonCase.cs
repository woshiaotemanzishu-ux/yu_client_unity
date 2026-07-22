using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Threading.Tasks;
using Shenxiao.Framework.Net; using Shenxiao.Module.Core.Demon; using UnityEngine;
namespace Shenxiao.EditorTools
{
    public static class DemonCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance; private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY demon EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            DemonController c = DemonController.Instance; DemonModel m = DemonModel.Instance; bool was = c.IsInitialized; byte oldOpen = m.OpenState; bool oldHas = m.HasData; var old = new List<DemonModel.Entry>(m.Demons); FieldInfo fi = typeof(DemonController).GetField("s_outboundIntercept", SF); object oldIntercept = fi?.GetValue(null);
            try
            {
                c.Init(); m.Reset(); MethodInfo on = typeof(DemonController).GetMethod("On18301", F); var h = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary; bool pass = fi != null && on != null && h != null && h.Contains(Proto.DEMON_INFO); for (int id = 18302; id <= 18317; id++) pass &= !h.Contains(id); void Check(string tag, bool ok) { Debug.Log("CLIVERIFY demon " + tag + " ok=" + ok); if (!ok) pass = false; } Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); fi.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); c.RequestStartup(); Check("startup exact empty frame", Frame(frames)); frames.Clear();
                byte[] first = new CliVerify.Pkt().C(2).H(2)
                    .I(101).H(11).I(4000000000L).C(3).C(4).H(2).I(501).H(1).I(4000000000L).C(1).I(502).H(2).I(3).C(0).H(2).I(601).H(4).C(2).C(5).H(6).I(602).H(7).C(8).C(9).H(10)
                    .I(102).H(12).I(0).C(0).C(0).H(0).H(0).Bytes(); var r1 = new NetReader(first, 0, first.Length); on.Invoke(c, new object[] { r1 }); m.TryGet(101, out DemonModel.Entry one); m.TryGet(102, out DemonModel.Entry two);
                Check("two entities/all fields/order/read-to-end", r1.Remaining == 0 && m.HasData && m.OpenState == 2 && m.Demons.Count == 2 && one != null && one.Level == 11 && one.Experience == 4000000000U && one.Star == 3 && one.SlotNumber == 4 && one.Skills.Count == 2 && one.Skills[0].SkillId == 501 && one.Skills[0].SkillLevel == 1 && one.Skills[0].Process == 4000000000U && one.Skills[0].IsActive == 1 && one.Skills[1].SkillId == 502 && one.Skills[1].SkillLevel == 2 && one.Skills[1].Process == 3 && one.Skills[1].IsActive == 0 && one.SlotSkills.Count == 2 && one.SlotSkills[0].SkillId == 601 && one.SlotSkills[0].SkillLevel == 4 && one.SlotSkills[0].Slot == 2 && one.SlotSkills[0].Quality == 5 && one.SlotSkills[0].Sort == 6 && one.SlotSkills[1].SkillId == 602 && one.SlotSkills[1].SkillLevel == 7 && one.SlotSkills[1].Slot == 8 && one.SlotSkills[1].Quality == 9 && one.SlotSkills[1].Sort == 10 && two != null && two.Level == 12 && two.Skills.Count == 0 && two.SlotSkills.Count == 0 && frames.Count == 0);
                byte[] second = new CliVerify.Pkt().C(1).H(1).I(103).H(13).I(4).C(5).C(6).H(0).H(0).Bytes(); var r2 = new NetReader(second, 0, second.Length); on.Invoke(c, new object[] { r2 }); Check("whole replace removes old", r2.Remaining == 0 && m.Demons.Count == 1 && m.TryGet(103, out DemonModel.Entry three) && three.Level == 13 && three.Experience == 4 && !m.TryGet(101, out _) && !m.TryGet(102, out _) && frames.Count == 0);
                byte[] empty = new CliVerify.Pkt().C(0).H(0).Bytes(); var r3 = new NetReader(empty, 0, empty.Length); on.Invoke(c, new object[] { r3 }); Check("empty replace clears old", r3.Remaining == 0 && m.HasData && m.OpenState == 0 && m.Demons.Count == 0 && frames.Count == 0);
                c.Dispose(); Check("dispose reset", !c.IsInitialized && !m.HasData && m.OpenState == 0 && m.Demons.Count == 0); Debug.Log("CLIVERIFY demon VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (c.IsInitialized) c.Dispose(); m.Reset(); if (oldHas) m.Replace(oldOpen, old); if (was) c.Init(); if (fi != null) fi.SetValue(null, oldIntercept); }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames) { if (frames.Count != 1 || frames[0] == null) return false; byte[] f = frames[0]; return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(Proto.DEMON_INFO >> 8) && f[5] == (byte)(Proto.DEMON_INFO & 0xFF); }
    }
}
