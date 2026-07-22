using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Threading.Tasks;
using Shenxiao.Framework.Net; using Shenxiao.Module.Core.Dress; using UnityEngine;
namespace Shenxiao.EditorTools
{
    /// <summary>11200 只读装扮快照：四类启动请求与按类型全量替换。</summary>
    public static class DressCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance; private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY dress EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            DressController c = DressController.Instance; DressModel m = DressModel.Instance; bool was = c.IsInitialized; var old = new Dictionary<byte, DressModel.Snapshot>(m.Snapshots); FieldInfo fi = typeof(DressController).GetField("s_outboundIntercept", SF); object oldIntercept = fi?.GetValue(null);
            try
            {
                c.Init(); m.Reset(); MethodInfo on = typeof(DressController).GetMethod("On11200", F); var h = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary; bool pass = fi != null && on != null && h != null && h.Contains(Proto.DRESS_INFO) && !h.Contains(11201) && !h.Contains(11202) && !h.Contains(11203) && !h.Contains(11204) && !h.Contains(11205); void Check(string tag, bool ok) { Debug.Log("CLIVERIFY dress " + tag + " ok=" + ok); if (!ok) pass = false; } Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); fi.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); c.RequestStartup(); Check("startup exact frames", Frames(frames, 1, 2, 3, 5)); frames.Clear();
                var first = new CliVerify.Pkt().C(1).I(100).H(2).I(101).H(3).L(5000000000L).L(6000000000L).I(102).H(4).L(7).L(8).Bytes(); var r1 = new NetReader(first, 0, first.Length); on.Invoke(c, new object[] { r1 }); m.TryGet(1, out DressModel.Snapshot one); Check("type1 fields/order/read-to-end", r1.Remaining == 0 && one != null && one.UsedDressId == 100 && one.EnableCount == 2 && one.Entries.Count == 2 && one.Entries[0].DressId == 101 && one.Entries[0].DressLevel == 3 && one.Entries[0].CurrentPower == 5000000000UL && one.Entries[0].NextPower == 6000000000UL && one.Entries[1].DressId == 102 && one.Entries[1].DressLevel == 4 && one.Entries[1].CurrentPower == 7 && one.Entries[1].NextPower == 8 && frames.Count == 0);
                var second = new CliVerify.Pkt().C(2).I(200).H(1).I(201).H(5).L(9).L(10).Bytes(); var r2 = new NetReader(second, 0, second.Length); on.Invoke(c, new object[] { r2 }); m.TryGet(2, out DressModel.Snapshot two); Check("types coexist/no-outbound", r2.Remaining == 0 && one != null && two != null && two.UsedDressId == 200 && two.EnableCount == 1 && two.Entries.Count == 1 && two.Entries[0].DressId == 201 && frames.Count == 0);
                var empty = new CliVerify.Pkt().C(1).I(0).H(0).Bytes(); var r3 = new NetReader(empty, 0, empty.Length); on.Invoke(c, new object[] { r3 }); m.TryGet(1, out one); m.TryGet(2, out two); Check("same type empty replace", r3.Remaining == 0 && one != null && one.EnableCount == 0 && one.Entries.Count == 0 && two != null && two.Entries.Count == 1 && frames.Count == 0);
                c.Dispose(); Check("dispose reset", !c.IsInitialized && !m.HasData && !m.TryGet(1, out _) && !m.TryGet(2, out _)); Debug.Log("CLIVERIFY dress VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (c.IsInitialized) c.Dispose(); m.Reset(); foreach (DressModel.Snapshot s in old.Values) m.Replace(s.Type, s.UsedDressId, new List<DressModel.Entry>(s.Entries)); if (was) c.Init(); if (fi != null) fi.SetValue(null, oldIntercept); }
        }
        private static bool Frames(IReadOnlyList<byte[]> frames, params byte[] types)
        {
            if (frames.Count != types.Length) return false; for (int i = 0; i < types.Length; i++) { byte[] f = frames[i]; if (f == null || f.Length != 7 || f[0] != 0 || f[1] != 7 || f[2] != 3 || f[3] != 232 || f[4] != 43 || f[5] != 192 || f[6] != types[i]) return false; } return true;
        }
    }
}
