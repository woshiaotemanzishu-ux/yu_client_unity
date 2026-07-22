using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Threading.Tasks;
using Shenxiao.Framework.Net; using Shenxiao.Module.Core.Mask; using UnityEngine;
namespace Shenxiao.EditorTools
{
    public static class MaskCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance; private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY mask EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            MaskController c = MaskController.Instance; MaskModel m = MaskModel.Instance; bool was = c.IsInitialized; byte oldMask = m.MaskId; uint oldEnd = m.EndTime; bool oldHas = m.HasData; FieldInfo fi = typeof(MaskController).GetField("s_outboundIntercept", SF); object oldIntercept = fi?.GetValue(null);
            try
            {
                c.Init(); m.Reset(); MethodInfo on = typeof(MaskController).GetMethod("On51101", F); var h = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary; bool pass = fi != null && on != null && h != null && h.Contains(Proto.MASK_INFO) && !h.Contains(51102); void Check(string tag, bool ok) { Debug.Log("CLIVERIFY mask " + tag + " ok=" + ok); if (!ok) pass = false; } Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); fi.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); c.RequestStartup(); Check("startup exact frame", Frame(frames, Proto.MASK_INFO)); frames.Clear();
                byte[] zero = new CliVerify.Pkt().C(0).I(0).Bytes(); var r0 = new NetReader(zero, 0, zero.Length); on.Invoke(c, new object[] { r0 }); Check("zero state/read-to-end", r0.Remaining == 0 && m.HasData && m.MaskId == 0 && m.EndTime == 0 && frames.Count == 0);
                byte[] value = new CliVerify.Pkt().C(7).I(4000000000L).Bytes(); var r1 = new NetReader(value, 0, value.Length); on.Invoke(c, new object[] { r1 }); Check("overwrite/read-to-end/no-outbound", r1.Remaining == 0 && m.MaskId == 7 && m.EndTime == 4000000000U && frames.Count == 0);
                c.Dispose(); Check("dispose reset", !c.IsInitialized && !m.HasData && m.MaskId == 0 && m.EndTime == 0); Debug.Log("CLIVERIFY mask VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (c.IsInitialized) c.Dispose(); m.Reset(); if (oldHas) m.ReplaceData(oldMask, oldEnd); if (was) c.Init(); if (fi != null) fi.SetValue(null, oldIntercept); }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id) { if (frames.Count != 1 || frames[0] == null) return false; byte[] f = frames[0]; return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id; }
    }
}
