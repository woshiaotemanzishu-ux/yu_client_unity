using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Demon;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>18314 天赋真实战力纯查询快照专项（不挂总路由）。</summary>
    public static class DemonTalentPowerCase
    {
        private const BindingFlags I = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic;
        public static Task<int> Run() { try { return Task.FromResult(RunCore()); } catch (Exception e) { Debug.LogError("CLIVERIFY demon-talent-power EXCEPTION " + e); return Task.FromResult(3); } }

        private static int RunCore()
        {
            DemonController c = DemonController.Instance; DemonModel m = DemonModel.Instance;
            FieldInfo intercept = typeof(DemonController).GetField("s_outboundIntercept", S); var ambient = new Ambient(c, m, intercept);
            bool pass = false, restored = false;
            try
            {
                c.Init(); m.Reset(); MethodInfo on = typeof(DemonController).GetMethod("On18314", I); MethodInfo onPower = typeof(DemonController).GetMethod("On18302", I); IDictionary h = typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary;
                pass = intercept != null && on != null && onPower != null && h != null && h.Contains(18301) && h.Contains(18302) && h.Contains(18303) && h.Contains(18307) && h.Contains(18311) && h.Contains(18314) && h.Contains(18315) && h.Contains(18317) && h.Contains(50901) && !h.Contains(18304) && !h.Contains(18305) && !h.Contains(18306) && !h.Contains(18308) && !h.Contains(18309) && !h.Contains(18310) && !h.Contains(18312) && !h.Contains(18313) && !h.Contains(18316);
                var frames = new List<byte[]>(); intercept.SetValue(null, new Func<byte[], bool>(x => { frames.Add(x); return true; }));
                c.RequestTalentPower(uint.MaxValue, byte.MaxValue, 4000000000U, ushort.MaxValue);
                pass &= PowerFrame(frames, uint.MaxValue, byte.MaxValue, 4000000000U, ushort.MaxValue);
                frames.Clear(); c.RequestStartup(); pass &= EmptyFrames(frames, 18301, 18303, 18307, 50901);

                frames.Clear(); c.RequestPower(uint.MaxValue); pass &= DemonPowerFrame(frames, uint.MaxValue); frames.Clear();
                Feed(onPower, c, DemonPowerPacket(uint.MaxValue, uint.MaxValue), out int powerRem);
                pass &= powerRem == 0 && m.TryGetDemonPower(uint.MaxValue, out uint power) && power == uint.MaxValue && m.DemonPowerCount == 1;
                Feed(onPower, c, DemonPowerPacket(uint.MaxValue, 7), out powerRem);
                pass &= powerRem == 0 && m.TryGetDemonPower(uint.MaxValue, out power) && power == 7 && m.DemonPowerCount == 1;
                Feed(onPower, c, DemonPowerPacket(9, 8), out powerRem);
                pass &= powerRem == 0 && m.TryGetDemonPower(uint.MaxValue, out power) && power == 7 && m.TryGetDemonPower(9, out uint secondPower) && secondPower == 8 && m.DemonPowerCount == 2;
                c.RequestPower(uint.MaxValue); pass &= DemonPowerFrame(frames, uint.MaxValue) && m.TryGetDemonPower(uint.MaxValue, out power) && power == 7; frames.Clear();
                Feed(onPower, c, DemonPowerPacket(0, 0), out powerRem);
                pass &= powerRem == 0 && m.TryGetDemonPower(0, out uint zeroPower) && zeroPower == 0 && m.DemonPowerCount == 3;

                m.Replace(8, new List<DemonModel.Entry>()); m.ReplaceFetters(new List<uint> { 9 }); m.ReplacePaintings(new List<byte> { 10 }); m.ReplaceBlessing(11);
                m.ReplaceTalentShop(12, 13, new List<DemonModel.ObjectEntry>(), new List<DemonModel.TalentShopEntry>());
                Feed(onPower, c, DemonPowerPacket(9, 10), out powerRem);
                pass &= powerRem == 0 && m.TryGetDemonPower(9, out secondPower) && secondPower == 10 && m.OpenState == 8 && m.HasFetter(9) && m.HasPainting(10) && m.BlessingValue == 11 && m.TalentShopRefreshTime == 12 && m.TalentShopRefreshNum == 13;

                DemonModel.TalentPower demon = null, goods = null;
                Feed(on, c, Packet(uint.MaxValue, uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, 1), out int rem);
                pass &= rem == 0 && m.TryGetTalentPower(uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, out demon) && Same(demon, uint.MaxValue, uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, 1) && m.DemonPowerCount == 3 && m.TryGetDemonPower(uint.MaxValue, out power) && power == 7 && m.TryGetDemonPower(9, out secondPower) && secondPower == 10 && m.TryGetDemonPower(0, out zeroPower) && zeroPower == 0;
                Feed(on, c, Packet(7, 9, 0, 22, 3, 1), out rem);
                pass &= rem == 0 && m.TryGetTalentPower(999, 0, 22, 3, out goods) && Same(goods, 7, 9, 0, 22, 3, 1) && m.TalentPowerCount == 2;
                Feed(on, c, Packet(8, uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, 1), out rem);
                pass &= rem == 0 && m.TryGetTalentPower(uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, out demon) && demon.Power == 8 && m.TryGetTalentPower(0, 0, 22, 3, out goods) && goods.Power == 7;
                Feed(on, c, Packet(0, uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, 999), out rem);
                pass &= rem == 0 && m.TryGetTalentPower(uint.MaxValue, 1, uint.MaxValue, ushort.MaxValue, out demon) && demon.Power == 8 && m.TalentPowerCount == 2;
                frames.Clear(); c.RequestTalentPower(9, 0, 22, 3); bool kept = m.TryGetTalentPower(9, 0, 22, 3, out DemonModel.TalentPower sameGoods) && ReferenceEquals(goods, sameGoods); pass &= PowerFrame(frames, 9, 0, 22, 3) && kept;
                c.Dispose(); pass &= !c.IsInitialized && !h.Contains(18301) && !h.Contains(18302) && !h.Contains(18303) && !h.Contains(18307) && !h.Contains(18311) && !h.Contains(18314) && !h.Contains(18315) && !h.Contains(18317) && !h.Contains(50901) && m.DemonPowerCount == 0 && m.TalentPowerCount == 0 && !m.HasTalentShopOpenState && !m.HasLifeSkillProgressData;
            }
            finally { restored = ambient.Restore(c, m, intercept); Debug.Log("CLIVERIFY demon-talent-power restored=" + restored + " pass=" + pass); }
            return pass && restored ? 0 : 3;
        }

        private static void Feed(MethodInfo method, DemonController c, byte[] bytes, out int remaining) { var r = new NetReader(bytes, 0, bytes.Length); method.Invoke(c, new object[] { r }); remaining = r.Remaining; }
        private static byte[] Packet(uint power, uint demonId, byte sign, uint skillId, ushort skillLv, uint code) => new CliVerify.Pkt().I(power).I(demonId).C(sign).I(skillId).H(skillLv).I(code).Bytes();
        private static byte[] DemonPowerPacket(uint demonId, uint power) => new CliVerify.Pkt().I(demonId).I(power).Bytes();
        private static bool Same(DemonModel.TalentPower v, uint power, uint demonId, byte sign, uint skillId, ushort lv, uint code) => v != null && v.Power == power && v.DemonsId == demonId && v.Sign == sign && v.SkillId == skillId && v.SkillLevel == lv && v.Code == code;
        private static bool EmptyFrames(List<byte[]> frames, params int[] ids) { if (frames.Count != ids.Length) return false; for (int i = 0; i < ids.Length; i++) if (!EmptyFrame(frames[i], ids[i])) return false; return true; }
        private static bool EmptyFrame(byte[] f, int id) => f != null && f.Length == 6 && f[0] == 0 && f[1] == 6 && f[2] == 3 && f[3] == 232 && f[4] == (byte)(id >> 8) && f[5] == (byte)id;
        private static bool PowerFrame(List<byte[]> f, uint demonId, byte sign, uint id, ushort lv) => f.Count == 1 && f[0].Length == 17 && f[0][0] == 0 && f[0][1] == 17 && f[0][2] == 3 && f[0][3] == 232 && f[0][4] == 71 && f[0][5] == 138 && f[0][6] == (byte)(demonId >> 24) && f[0][7] == (byte)(demonId >> 16) && f[0][8] == (byte)(demonId >> 8) && f[0][9] == (byte)demonId && f[0][10] == sign && f[0][11] == (byte)(id >> 24) && f[0][12] == (byte)(id >> 16) && f[0][13] == (byte)(id >> 8) && f[0][14] == (byte)id && f[0][15] == (byte)(lv >> 8) && f[0][16] == (byte)lv;
        private static bool DemonPowerFrame(List<byte[]> f, uint demonId) => f.Count == 1 && f[0].Length == 10 && f[0][0] == 0 && f[0][1] == 10 && f[0][2] == 3 && f[0][3] == 232 && f[0][4] == (byte)(Proto.DEMON_POWER >> 8) && f[0][5] == (byte)(Proto.DEMON_POWER & 0xFF) && f[0][6] == (byte)(demonId >> 24) && f[0][7] == (byte)(demonId >> 16) && f[0][8] == (byte)(demonId >> 8) && f[0][9] == (byte)demonId;

        private sealed class Ambient
        {
            private static readonly int[] P = { 18301, 18302, 18303, 18307, 18311, 18312, 18313, 18314, 18315, 18317, 50901 };
            private readonly bool _init, _has, _hasFetters, _hasPaintings, _hasBlessing, _hasShop, _hasShopOpen, _hasLife; private readonly byte _open, _shopOpen; private readonly uint _blessing, _refreshTime; private readonly ushort _refreshNum; private readonly object _intercept; private readonly Dictionary<int, object> _handlers = new Dictionary<int, object>(); private readonly Dictionary<uint, uint> _power; private readonly Dictionary<string, DemonModel.TalentPower> _demon, _goods; private readonly Dictionary<ulong, DemonModel.LifeSkillProgress> _life; private readonly List<DemonModel.Entry> _entries; private readonly List<uint> _fetters; private readonly List<byte> _paintings; private readonly List<DemonModel.ObjectEntry> _cost; private readonly List<DemonModel.TalentShopEntry> _shop;
            public Ambient(DemonController c, DemonModel m, FieldInfo i) { _init = c.IsInitialized; _open = m.OpenState; _shopOpen = m.TalentShopOpenState; _has = m.HasData; _hasFetters = m.HasFettersData; _hasPaintings = m.HasPaintingsData; _hasBlessing = m.HasBlessingData; _hasShop = m.HasTalentShopSnapshot; _hasShopOpen = m.HasTalentShopOpenState; _hasLife = m.HasLifeSkillProgressData; _blessing = m.BlessingValue; _refreshTime = m.TalentShopRefreshTime; _refreshNum = m.TalentShopRefreshNum; _entries = new List<DemonModel.Entry>(m.Demons); _fetters = new List<uint>(m.Fetters); _paintings = new List<byte>(m.Paintings); _cost = new List<DemonModel.ObjectEntry>(m.TalentShopCost); _shop = new List<DemonModel.TalentShopEntry>(m.TalentShop); _intercept = i == null ? null : i.GetValue(null); _power = CopyPower(m); _demon = Copy(m, "_demonTalentPower"); _goods = Copy(m, "_goodsTalentPower"); _life = CopyLife(m); IDictionary h = typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary; if (h != null) foreach (int x in P) if (h.Contains(x)) _handlers[x] = h[x]; }
            public bool Restore(DemonController c, DemonModel m, FieldInfo i) { try { if (c.IsInitialized) c.Dispose(); m.Reset(); if (_has) m.Replace(_open, _entries); if (_hasFetters) m.ReplaceFetters(_fetters); if (_hasPaintings) m.ReplacePaintings(_paintings); if (_hasBlessing) m.ReplaceBlessing(_blessing); if (_hasShop) m.ReplaceTalentShop(_refreshTime, _refreshNum, _cost, _shop); if (_hasShopOpen) m.ReplaceTalentShopOpenState(_shopOpen); if (_hasLife) foreach (var x in _life) m.ReplaceLifeSkillProgress(x.Value); PutPower(m, _power); Put(m, "_demonTalentPower", _demon); Put(m, "_goodsTalentPower", _goods); if (_init) c.Init(); IDictionary h = typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary; if (h == null) return false; foreach (int x in P) if (_handlers.TryGetValue(x, out object v)) h[x] = v; else h.Remove(x); if (i != null) i.SetValue(null, _intercept); if (m.DemonPowerCount != _power.Count) return false; foreach (var x in _power) if (!m.TryGetDemonPower(x.Key, out uint value) || value != x.Value) return false; foreach (int x in P) { bool existed = _handlers.TryGetValue(x, out object expected); if (h.Contains(x) != existed || (existed && !ReferenceEquals(h[x], expected))) return false; } return c.IsInitialized == _init && m.TalentPowerCount == _demon.Count + _goods.Count && m.HasTalentShopOpenState == _hasShopOpen && m.TalentShopOpenState == _shopOpen && m.HasLifeSkillProgressData == _hasLife && SameLife(m, _life) && (i == null || ReferenceEquals(i.GetValue(null), _intercept)); } catch (Exception e) { Debug.LogError("CLIVERIFY demon-talent-power restore " + e); return false; } }
            private static Dictionary<string, DemonModel.TalentPower> Copy(DemonModel m, string n) => new Dictionary<string, DemonModel.TalentPower>((Dictionary<string, DemonModel.TalentPower>)typeof(DemonModel).GetField(n, I).GetValue(m));
            private static void Put(DemonModel m, string n, Dictionary<string, DemonModel.TalentPower> v) { var t = (Dictionary<string, DemonModel.TalentPower>)typeof(DemonModel).GetField(n, I).GetValue(m); t.Clear(); foreach (var x in v) t[x.Key] = x.Value; }
            private static Dictionary<uint, uint> CopyPower(DemonModel m) => new Dictionary<uint, uint>((Dictionary<uint, uint>)typeof(DemonModel).GetField("_demonPower", I).GetValue(m));
            private static void PutPower(DemonModel m, Dictionary<uint, uint> value) { var target = (Dictionary<uint, uint>)typeof(DemonModel).GetField("_demonPower", I).GetValue(m); target.Clear(); foreach (var x in value) target[x.Key] = x.Value; }
            private static Dictionary<ulong, DemonModel.LifeSkillProgress> CopyLife(DemonModel m) => new Dictionary<ulong, DemonModel.LifeSkillProgress>((Dictionary<ulong, DemonModel.LifeSkillProgress>)typeof(DemonModel).GetField("_lifeSkillProgress", I).GetValue(m));
            private static bool SameLife(DemonModel m, Dictionary<ulong, DemonModel.LifeSkillProgress> expected) { var actual = (Dictionary<ulong, DemonModel.LifeSkillProgress>)typeof(DemonModel).GetField("_lifeSkillProgress", I).GetValue(m); if (actual.Count != expected.Count) return false; foreach (var x in expected) if (!actual.TryGetValue(x.Key, out DemonModel.LifeSkillProgress value) || !ReferenceEquals(value, x.Value)) return false; return true; }
        }
    }
}
