using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using System.Threading.Tasks;
using Shenxiao.Framework.Net; using Shenxiao.Module.Core.GodBeast; using UnityEngine;
namespace Shenxiao.EditorTools
{
    public static class GodBeastCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance; private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        public static Task<int> Run() { try { return Task.FromResult(RunSync()); } catch (Exception e) { Debug.LogError("CLIVERIFY godbeast EXCEPTION " + e); return Task.FromResult(3); } }
        private static int RunSync()
        {
            GodBeastController c = GodBeastController.Instance; GodBeastModel m = GodBeastModel.Instance; bool was = c.IsInitialized; byte oldFight = m.FightCount; var oldBeasts = new List<GodBeastModel.Beast>(m.Beasts); bool oldHas = m.HasData; FieldInfo fi = typeof(GodBeastController).GetField("s_outboundIntercept", SF); object oldIntercept = fi?.GetValue(null);
            try
            {
                c.Init(); m.Reset(); MethodInfo on = typeof(GodBeastController).GetMethod("On17301", F); var h = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary; bool pass = fi != null && on != null && h != null && h.Contains(Proto.GODBEAST_OVERVIEW) && !h.Contains(17300); void Check(string tag, bool ok) { Debug.Log("CLIVERIFY godbeast " + tag + " ok=" + ok); if (!ok) pass = false; } Check("seams/register", pass); if (!pass) return 3;
                var frames = new List<byte[]>(); fi.SetValue(null, new Func<byte[], bool>(f => { frames.Add(f); return true; })); c.RequestStartup(); Check("startup exact frame", Frame(frames, Proto.GODBEAST_OVERVIEW)); frames.Clear();
                byte[] first = new CliVerify.Pkt().C(2).H(2).I(1).C(0).I(10).H(0).H(0).I(2).C(1).I(20).H(2).C(3).L(5000000000L).H(4).I(5).C(6).L(7).H(8).I(9).H(2).H(10).I(11).H(12).I(13).Bytes(); var r = new NetReader(first, 0, first.Length); on.Invoke(c, new object[] { r });
                Check("nested/read-to-end/order", r.Remaining == 0 && m.HasData && m.FightCount == 2 && m.Beasts.Count == 2
                    && m.Beasts[0].Id == 1 && m.Beasts[0].State == 0 && m.Beasts[0].Score == 10 && m.Beasts[0].Equips.Count == 0 && m.Beasts[0].Attrs.Count == 0
                    && m.Beasts[1].Id == 2 && m.Beasts[1].State == 1 && m.Beasts[1].Score == 20 && m.Beasts[1].Equips.Count == 2
                    && m.Beasts[1].Equips[0].Position == 3 && m.Beasts[1].Equips[0].GoodsId == 5000000000UL
                    && m.Beasts[1].Equips[0].Strengthen == 4 && m.Beasts[1].Equips[0].Exp == 5
                    && m.Beasts[1].Equips[1].Position == 6 && m.Beasts[1].Equips[1].GoodsId == 7
                    && m.Beasts[1].Equips[1].Strengthen == 8 && m.Beasts[1].Equips[1].Exp == 9
                    && m.Beasts[1].Attrs.Count == 2 && m.Beasts[1].Attrs[0].Type == 10 && m.Beasts[1].Attrs[0].Value == 11
                    && m.Beasts[1].Attrs[1].Type == 12 && m.Beasts[1].Attrs[1].Value == 13 && frames.Count == 0);
                byte[] empty = new CliVerify.Pkt().C(9).H(0).Bytes(); var e = new NetReader(empty, 0, empty.Length); on.Invoke(c, new object[] { e }); Check("full replace empty", e.Remaining == 0 && m.HasData && m.FightCount == 9 && m.Beasts.Count == 0 && frames.Count == 0);
                c.Dispose(); Check("dispose reset", !c.IsInitialized && !m.HasData && m.FightCount == 0 && m.Beasts.Count == 0); Debug.Log("CLIVERIFY godbeast VERDICT pass=" + pass); return pass ? 0 : 3;
            }
            finally { if (c.IsInitialized) c.Dispose(); m.Reset(); if (oldHas) m.ReplaceData(oldFight, oldBeasts); if (was) c.Init(); if (fi != null) fi.SetValue(null, oldIntercept); }
        }
        private static bool Frame(IReadOnlyList<byte[]> frames, int id) { if (frames.Count != 1 || frames[0] == null) return false; byte[] f = frames[0]; return f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id; }
    }
}
