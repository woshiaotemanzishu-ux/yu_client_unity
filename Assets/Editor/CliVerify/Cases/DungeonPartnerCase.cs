using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.DungeonPartner;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class DungeonPartnerCase
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.NonPublic,
            S = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY dungeon-partner EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        static int RunSync()
        {
            var c = DungeonPartnerController.Instance;
            var m = DungeonPartnerModel.Instance;
            bool was = c.IsInitialized;
            ushort oldSweep = m.SweepCount;
            var od = new Dictionary<byte, DungeonPartnerModel.DungeonSnapshot>();
            var or = new Dictionary<byte, DungeonPartnerModel.StageRewardSnapshot>();
            for (int i = 0; i < 256; i++)
            {
                byte l = (byte)i;
                if (m.TryGetDungeons(l, out var d)) od[l] = d;
                if (m.TryGetStageRewards(l, out var r)) or[l] = r;
            }

            var fi = typeof(DungeonPartnerController).GetField("s_outboundIntercept", S);
            object oi = fi?.GetValue(null);
            try
            {
                if (c.IsInitialized) c.Dispose();
                m.Reset();
                c.Init();
                var a = typeof(DungeonPartnerController).GetMethod("On61105", I);
                var b = typeof(DungeonPartnerController).GetMethod("On61106", I);
                var h = typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary;
                bool p = fi != null && a != null && b != null && h != null;
                for (int x = 61100; x <= 61110; x++)
                    p &= (x == 61105 || x == 61106) ? h.Contains(x) : !h.Contains(x);
                Chk(ref p, "seams", p);

                var f = new List<byte[]>();
                fi.SetValue(null, new Func<byte[], bool>(x =>
                {
                    f.Add(x);
                    return true;
                }));
                c.RequestDungeons(0);
                c.RequestStageRewards(0);
                Chk(ref p, "exact requests", Frames(f, 61105, 0, 61106, 0));
                f.Clear();
                Feed(a, c, D(0, 0, new DS[0]), out var e1);
                Feed(b, c, R(0, new RS[0]), out var e2);
                Chk(ref p, "level0 empty", e1.Remaining == 0 && e2.Remaining == 0 && m.TryGetDungeons(0, out var d0) && m.TryGetStageRewards(0, out var r0) && d0.Loaded && r0.Loaded && d0.Entries.Count == 0 && r0.Entries.Count == 0 && f.Count == 0);

                var ds = new[]
                {
                    new DS(uint.MaxValue, 255), new DS(uint.MaxValue, 0), new DS(0, 0)
                };
                var rs = new[]
                {
                    new RS(ushort.MaxValue, 255), new RS(ushort.MaxValue, 0), new RS(0, 0)
                };
                Feed(a, c, D(255, ushort.MaxValue, ds), out var m1);
                Feed(b, c, R(255, rs), out var m2);
                m.TryGetDungeons(255, out var d255);
                m.TryGetStageRewards(255, out var r255);
                Chk(ref p, "level255 many boundaries/order", m1.Remaining == 0 && m2.Remaining == 0 && m.SweepCount == ushort.MaxValue && d255.Entries.Count == 3 && d255.Entries[0].DungeonId == uint.MaxValue && d255.Entries[0].Score == 255 && d255.Entries[1].DungeonId == uint.MaxValue && d255.Entries[1].Score == 0 && d255.Entries[2].DungeonId == 0 && d255.Entries[2].Score == 0 && r255.Entries.Count == 3 && r255.Entries[0].Score == ushort.MaxValue && r255.Entries[0].Status == 255 && r255.Entries[1].Score == ushort.MaxValue && r255.Entries[1].Status == 0 && r255.Entries[2].Score == 0 && r255.Entries[2].Status == 0 && f.Count == 0);

                Feed(a, c, D(0, 7, new[]
                {
                    new DS(1, 2)
                }), out var l0d);
                Feed(b, c, R(0, new[]
                {
                    new RS(1, 2)
                }), out var l0r);
                m.TryGetDungeons(0, out d0);
                m.TryGetStageRewards(0, out r0);
                Chk(ref p, "cross-level coexist", l0d.Remaining == 0 && l0r.Remaining == 0 && m.SweepCount == 7 && d0.Entries.Count == 1 && r0.Entries.Count == 1 && d255.Entries.Count == 3 && r255.Entries.Count == 3 && f.Count == 0);

                Feed(a, c, D(255, 8, new[]
                {
                    new DS(3, 4)
                }), out var oneD);
                m.TryGetDungeons(255, out d255);
                Chk(ref p, "05 many-to-one isolates06", oneD.Remaining == 0 && m.SweepCount == 8 && d255.Entries.Count == 1 && d255.Entries[0].DungeonId == 3 && d255.Entries[0].Score == 4 && r255.Entries.Count == 3 && f.Count == 0);
                Feed(b, c, R(255, new[]
                {
                    new RS(3, 4)
                }), out var oneR);
                m.TryGetStageRewards(255, out r255);
                Chk(ref p, "06 many-to-one isolates05", oneR.Remaining == 0 && m.SweepCount == 8 && d255.Entries.Count == 1 && r255.Entries.Count == 1 && r255.Entries[0].Score == 3 && r255.Entries[0].Status == 4 && f.Count == 0);

                c.RequestDungeons(255);
                c.RequestStageRewards(255);
                Chk(ref p, "no response keeps both", Frames(f, 61105, 255, 61106, 255) && d255.Entries.Count == 1 && r255.Entries.Count == 1 && m.SweepCount == 8);
                f.Clear();
                Feed(a, c, D(255, 9, new DS[0]), out var clearD);
                Feed(b, c, R(255, new RS[0]), out var clearR);
                m.TryGetDungeons(255, out d255);
                m.TryGetStageRewards(255, out r255);
                Chk(ref p, "255 one-to-empty keeps0", clearD.Remaining == 0 && clearR.Remaining == 0 && m.SweepCount == 9 && d255.Entries.Count == 0 && r255.Entries.Count == 0 && d0.Entries.Count == 1 && r0.Entries.Count == 1 && f.Count == 0);
                c.Dispose();
                Chk(ref p, "dispose", !c.IsInitialized && m.SweepCount == 0 && !m.TryGetDungeons(0, out _) && !m.TryGetStageRewards(0, out _) && !h.Contains(61105) && !h.Contains(61106));
                Debug.Log("CLIVERIFY dungeon-partner VERDICT pass=" + p);
                return p ? 0 : 3;
            }
            finally
            {
                if (c.IsInitialized) c.Dispose();
                m.Reset();
                foreach (var x in od) m.ReplaceDungeons(x.Key, oldSweep, new List<DungeonPartnerModel.DungeonEntry>(x.Value.Entries));
                foreach (var x in or) m.ReplaceStageRewards(x.Key, new List<DungeonPartnerModel.StageRewardEntry>(x.Value.Entries));
                if (was) c.Init();
                if (fi != null) fi.SetValue(null, oi);
            }
        }

        static void Feed(MethodInfo x, DungeonPartnerController c, byte[] b, out NetReader r)
        {
            r = new NetReader(b, 0, b.Length);
            x.Invoke(c, new object[]
            {
                r
            });
        }

        static void Chk(ref bool p, string t, bool ok)
        {
            Debug.Log("CLIVERIFY dungeon-partner " + t + " ok=" + ok);
            p &= ok;
        }

        static bool Frames(List<byte[]> f, int p, byte l)
        {
            return f.Count == 1 && F(f[0], p, l);
        }

        static bool Frames(List<byte[]> f, int p, byte l, int q, byte k)
        {
            return f.Count == 2 && F(f[0], p, l) && F(f[1], q, k);
        }

        static bool F(byte[] x, int p, byte l)
        {
            return x.Length == 7 && x[0] == 0 && x[1] == 7 && x[2] == 3 && x[3] == 232 && x[4] == (byte)(p >> 8) && x[5] == (byte)p && x[6] == l;
        }

        static byte[] D(byte l, ushort s, DS[] a)
        {
            var p = new CliVerify.Pkt().C(l).H(s).H(a.Length);
            foreach (var x in a) p.I(x.Id).C(x.Score);
            return p.Bytes();
        }

        static byte[] R(byte l, RS[] a)
        {
            var p = new CliVerify.Pkt().C(l).H(a.Length);
            foreach (var x in a) p.H(x.Score).C(x.Status);
            return p.Bytes();
        }

        struct DS
        {
            public uint Id;
            public byte Score;

            public DS(uint i, byte s)
            {
                Id = i;
                Score = s;
            }
        }

        struct RS
        {
            public ushort Score;
            public byte Status;

            public RS(ushort s, byte t)
            {
                Score = s;
                Status = t;
            }
        }
    }
}
