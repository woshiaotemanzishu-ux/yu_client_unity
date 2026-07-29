using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HolyTerritory;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class HolyTerritoryCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        private static readonly int[] RegisteredIds =
        {
            28300, 28301, 28302, 28306, 28307, 28308, 28309, 28310,
            28311, 28312, 28313, 28314, 28316, 28317, 28318, 28319
        };

        private sealed class EntryState { public bool Exists; public object Value; }
        private sealed class FieldState
        {
            public FieldInfo Field;
            public object Value;
            public bool IsDictionary;
            public readonly Dictionary<object, object> Entries = new Dictionary<object, object>();
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY holyterritory EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            HolyTerritoryController controller = HolyTerritoryController.Instance;
            HolyTerritoryModel model = HolyTerritoryModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            List<FieldState> oldModel = CaptureModel(model);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, EntryState>();
            for (int id = 28300; id <= 28319; id++) oldHandlers[id] = SaveEntry(handlers, id);
            FieldInfo interceptor = typeof(HolyTerritoryController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            bool pass = false;
            bool restored = false;

            try
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                controller.Init();

                bool a = ExactHandlers(handlers) && OnlySafePublicMethods();
                controller.Init();
                a &= ExactHandlers(handlers);

                var frames = new List<byte[]>();
                if (interceptor != null)
                    interceptor.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestStartup();
                bool b = FramesAre(frames,
                    U8Frame(28301, 1), U8Frame(28301, 2), U8Frame(28301, 3),
                    EmptyFrame(28314), EmptyFrame(28316), EmptyFrame(28318));
                frames.Clear();
                controller.RequestTerritoryInfo(byte.MaxValue);
                controller.RequestGuildRank();
                controller.RequestDeathFatigue();
                controller.RequestGuildMemberRank();
                controller.RequestKillLog(byte.MaxValue, uint.MaxValue);
                controller.RequestSanctuaryMemberRank(byte.MaxValue);
                controller.RequestSettlement();
                controller.RequestFirstOpen();
                controller.RequestFatigue();
                b &= FramesAre(frames, U8Frame(28301, byte.MaxValue), EmptyFrame(28302),
                    EmptyFrame(28308), EmptyFrame(28310), U8U32Frame(28311, byte.MaxValue, uint.MaxValue),
                    U8Frame(28312, byte.MaxValue), EmptyFrame(28314), EmptyFrame(28316), EmptyFrame(28318));

                HolyTerritoryModel.TerritorySnapshot territory = null;
                bool c = Invoke(controller, 28301, TerritoryPacket(1, false, uint.MaxValue))
                    && model.TryGetTerritory(1, out territory)
                    && territory.Point == uint.MaxValue && territory.BelongGuildId == ulong.MaxValue
                    && territory.BelongGuildName == "归属甲" && territory.EndTime == 4000000000U
                    && territory.Bosses.Count == 2 && territory.Bosses[0].BossId == uint.MaxValue
                    && territory.Bosses[1].BossId == uint.MaxValue
                    && territory.Bosses[0].RebornTime == 10 && territory.Bosses[1].RebornTime == 20;
                c &= Invoke(controller, 28301, TerritoryPacket(2, true, 0))
                    && model.TryGetTerritory(2, out HolyTerritoryModel.TerritorySnapshot emptyTerritory)
                    && emptyTerritory.Bosses.Count == 0 && model.Territories.Count == 2;
                c &= Invoke(controller, 28302, GuildRankPacket(false))
                    && model.GuildRank != null && model.GuildRank.MyGuildRank == uint.MaxValue
                    && model.GuildRank.MyGuildTopTenPower == ulong.MaxValue
                    && model.GuildRank.Entries.Count == 2
                    && model.GuildRank.Entries[0].GuildName == "公会甲"
                    && model.GuildRank.Entries[0].AveragePower == ulong.MaxValue
                    && model.GuildRank.Entries[1].Rank == uint.MaxValue;
                c &= Invoke(controller, 28302, GuildRankPacket(true))
                    && model.GuildRank != null && model.GuildRank.Entries.Count == 0;

                bool d = Invoke(controller, 28310, GuildMemberPacket(false))
                    && model.GuildMemberRank != null && model.GuildMemberRank.MyRank == uint.MaxValue
                    && model.GuildMemberRank.MyPower == ulong.MaxValue
                    && model.GuildMemberRank.Entries.Count == 2
                    && model.GuildMemberRank.Entries[0].RoleId == ulong.MaxValue
                    && model.GuildMemberRank.Entries[0].Picture == "头像甲"
                    && model.GuildMemberRank.Entries[0].Career == byte.MaxValue
                    && model.GuildMemberRank.Entries[0].Power == 0xFEDCBA9876543210UL;
                d &= Invoke(controller, 28310, GuildMemberPacket(true))
                    && model.GuildMemberRank.Entries.Count == 0;
                d &= Invoke(controller, 28311, KillLogPacket(1, uint.MaxValue, false))
                    && model.TryGetKillLog(1, uint.MaxValue, out HolyTerritoryModel.KillLogSnapshot log)
                    && log.Entries.Count == 2 && log.Entries[0].Name == "击杀甲"
                    && log.Entries[0].IsShow == byte.MaxValue && log.Entries[0].ReducePoint == uint.MaxValue
                    && log.Entries[1].Time == uint.MaxValue;
                d &= Invoke(controller, 28311, KillLogPacket(2, uint.MaxValue, true))
                    && model.TryGetKillLog(2, uint.MaxValue, out HolyTerritoryModel.KillLogSnapshot emptyLog)
                    && emptyLog.Entries.Count == 0 && model.KillLogs.Count == 2;
                d &= Invoke(controller, 28312, SanctuaryRankPacket(1, false))
                    && model.TryGetSanctuaryRank(1, out HolyTerritoryModel.SanctuaryRankSnapshot rank)
                    && rank.Entries.Count == 2 && rank.Entries[0].Rank == uint.MaxValue
                    && rank.Entries[0].RoleName == "层榜甲" && rank.Entries[0].Power == ulong.MaxValue;
                d &= Invoke(controller, 28312, SanctuaryRankPacket(2, true))
                    && model.TryGetSanctuaryRank(2, out HolyTerritoryModel.SanctuaryRankSnapshot emptyRank)
                    && emptyRank.Entries.Count == 0 && model.SanctuaryRanks.Count == 2;

                bool e = Invoke(controller, 28306, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.HasActivityEndTime && model.ActivityEndTime == uint.MaxValue;
                e &= Invoke(controller, 28307, new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue).Bytes())
                    && model.LastRebornNotice != null && model.LastRebornNotice.SanctuaryId == byte.MaxValue
                    && model.LastRebornNotice.BossId == uint.MaxValue;
                e &= Invoke(controller, 28308, new CliVerify.Pkt().H(ushort.MaxValue)
                        .I(uint.MaxValue).I(4000000000L).I(3999999999L).Bytes())
                    && model.DeathFatigue != null && model.DeathFatigue.DieTimes == ushort.MaxValue
                    && model.DeathFatigue.Time == uint.MaxValue
                    && model.DeathFatigue.DebuffTime == 4000000000U
                    && model.DeathFatigue.SafeTime == 3999999999U;
                uint absolutePoint = territory.Point;
                e &= Invoke(controller, 28309, new CliVerify.Pkt().C(1).I(uint.MaxValue).I(77).Bytes())
                    && model.LastBossDefeated != null && model.LastBossDefeated.RebornTime == 77
                    && model.TryGetTerritory(1, out HolyTerritoryModel.TerritorySnapshot patched)
                    && patched.Point == absolutePoint && patched.Bosses[0].RebornTime == 77
                    && patched.Bosses[1].RebornTime == 20;
                int territoryCount = model.Territories.Count;
                e &= Invoke(controller, 28309, new CliVerify.Pkt().C(9).I(123).I(88).Bytes())
                    && model.LastBossDefeated.SanctuaryId == 9 && model.Territories.Count == territoryCount
                    && !model.TryGetTerritory(9, out _);
                e &= Invoke(controller, 28313, new CliVerify.Pkt().C(3).I(uint.MaxValue).Bytes())
                    && model.LastUnderAttack != null && model.LastUnderAttack.SanctuaryId == 3
                    && model.LastUnderAttack.BossId == uint.MaxValue;

                bool f = Invoke(controller, 28314, new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue)
                        .I(4000000000L).I(3999999999L).Bytes())
                    && model.Settlement != null && model.Settlement.GuildRank == uint.MaxValue
                    && model.Settlement.SanctuaryId == byte.MaxValue
                    && model.Settlement.PersonRank == 4000000000U
                    && model.Settlement.DesignationId == 3999999999U;
                f &= Invoke(controller, 28316, new CliVerify.Pkt().C(0).Bytes())
                    && model.HasFirstOpen && model.FirstOpenCode == 0;
                f &= Invoke(controller, 28317, new CliVerify.Pkt().I(1234567890).Bytes())
                    && model.HasPointGain && model.LastPointGain == 1234567890
                    && model.TryGetTerritory(1, out HolyTerritoryModel.TerritorySnapshot afterPoint)
                    && afterPoint.Point == absolutePoint;
                f &= Invoke(controller, 28318, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.HasFatigue && model.Fatigue == uint.MaxValue;
                f &= Invoke(controller, 28319, new CliVerify.Pkt().I(5).Bytes())
                    && model.HasFatigueGain && model.LastFatigueGain == 5 && model.Fatigue == uint.MaxValue;
                f &= Invoke(controller, 28300, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.HasError && model.LastErrorCode == uint.MaxValue;

                controller.Dispose();
                bool g = !controller.IsInitialized && ModelIsEmpty(model) && NoFamilyHandlers(handlers);
                pass = a && b && c && d && e && f && g;
                Debug.Log("CLIVERIFY holyterritory A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 28300; id <= 28319; id++) RestoreEntry(handlers, id, oldHandlers[id]);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized && ModelMatches(model, oldModel)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                for (int id = 28300; id <= 28319; id++)
                    restored &= EntryMatches(handlers, id, oldHandlers[id]);
                Debug.Log("CLIVERIFY holyterritory restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool ExactHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            var expected = new HashSet<int>(RegisteredIds);
            for (int id = 28300; id <= 28319; id++)
                if (handlers.Contains(id) != expected.Contains(id)) return false;
            return true;
        }

        private static bool NoFamilyHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int id = 28300; id <= 28319; id++) if (handlers.Contains(id)) return false;
            return true;
        }

        private static bool OnlySafePublicMethods()
        {
            var expected = new HashSet<string>
            {
                "RequestStartup", "RequestTerritoryInfo", "RequestGuildRank", "RequestDeathFatigue",
                "RequestGuildMemberRank", "RequestKillLog", "RequestSanctuaryMemberRank",
                "RequestSettlement", "RequestFirstOpen", "RequestFatigue", "Dispose"
            };
            foreach (MethodInfo method in typeof(HolyTerritoryController).GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!expected.Remove(method.Name)) return false;
            return expected.Count == 0;
        }

        private static bool Invoke(HolyTerritoryController controller, int id, byte[] bytes)
        {
            MethodInfo method = typeof(HolyTerritoryController).GetMethod("On" + id, F);
            if (method == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static byte[] TerritoryPacket(byte id, bool empty, uint point)
        {
            var p = new CliVerify.Pkt().C(id).I(point).L(unchecked((long)ulong.MaxValue))
                .S(empty ? string.Empty : "归属甲").I(empty ? 0 : 4000000000L).H(empty ? 0 : 2);
            if (!empty)
                p.I(uint.MaxValue).I(10).C(byte.MaxValue)
                    .I(uint.MaxValue).I(20).C(1);
            return p.Bytes();
        }

        private static byte[] GuildRankPacket(bool empty)
        {
            var p = new CliVerify.Pkt().I(empty ? 0 : uint.MaxValue)
                .L(unchecked((long)(empty ? 0UL : ulong.MaxValue))).H(empty ? 0 : 2);
            if (!empty)
            {
                GuildRank(p, "甲", true);
                GuildRank(p, "乙", false);
            }
            return p.Bytes();
        }

        private static void GuildRank(CliVerify.Pkt p, string suffix, bool max) =>
            p.S("公会" + suffix).S("会长" + suffix).I(uint.MaxValue)
                .I(max ? uint.MaxValue : 2).I(max ? uint.MaxValue : 3)
                .L(unchecked((long)(max ? ulong.MaxValue : 4UL)));

        private static byte[] GuildMemberPacket(bool empty)
        {
            var p = new CliVerify.Pkt().I(empty ? 0 : uint.MaxValue)
                .L(unchecked((long)(empty ? 0UL : ulong.MaxValue))).H(empty ? 0 : 2);
            if (!empty)
            {
                GuildMember(p, "甲", true);
                GuildMember(p, "乙", false);
            }
            return p.Bytes();
        }

        private static void GuildMember(CliVerify.Pkt p, string suffix, bool max) =>
            p.L(unchecked((long)ulong.MaxValue)).I(uint.MaxValue).S("头像" + suffix)
                .I(max ? uint.MaxValue : 2).C(max ? byte.MaxValue : 3).S("成员" + suffix)
                .L(unchecked((long)(max ? 0xFEDCBA9876543210UL : ulong.MaxValue)))
                .I(max ? uint.MaxValue : 4);

        private static byte[] KillLogPacket(byte sanctuary, uint boss, bool empty)
        {
            var p = new CliVerify.Pkt().C(sanctuary).I(boss).H(empty ? 0 : 2);
            if (!empty)
                p.I(uint.MaxValue).S("击杀甲").C(byte.MaxValue).I(uint.MaxValue)
                    .I(uint.MaxValue).S("击杀乙").C(1).I(2);
            return p.Bytes();
        }

        private static byte[] SanctuaryRankPacket(byte sanctuary, bool empty)
        {
            var p = new CliVerify.Pkt().C(sanctuary).H(empty ? 0 : 2);
            if (!empty)
                p.I(uint.MaxValue).S("层榜甲").L(unchecked((long)ulong.MaxValue)).I(uint.MaxValue)
                    .I(uint.MaxValue).S("层榜乙").L(unchecked((long)ulong.MaxValue)).I(2);
            return p.Bytes();
        }

        private static bool ModelIsEmpty(HolyTerritoryModel m) =>
            !m.HasError && m.LastErrorCode == 0 && m.Territories.Count == 0 && m.GuildRank == null
            && !m.HasActivityEndTime && m.ActivityEndTime == 0 && m.LastRebornNotice == null
            && m.DeathFatigue == null && m.LastBossDefeated == null && m.GuildMemberRank == null
            && m.KillLogs.Count == 0 && m.SanctuaryRanks.Count == 0 && m.LastUnderAttack == null
            && m.Settlement == null && !m.HasFirstOpen && m.FirstOpenCode == 0
            && !m.HasPointGain && m.LastPointGain == 0 && !m.HasFatigue && m.Fatigue == 0
            && !m.HasFatigueGain && m.LastFatigueGain == 0;

        private static byte[] EmptyFrame(int id) => Frame(id);
        private static byte[] U8Frame(int id, byte value) => Frame(id, value);
        private static byte[] U8U32Frame(int id, byte first, uint second) => Frame(id, first,
            (byte)(second >> 24), (byte)(second >> 16), (byte)(second >> 8), (byte)second);

        private static byte[] Frame(int id, params byte[] payload)
        {
            int length = 6 + payload.Length;
            var result = new byte[length];
            result[0] = (byte)(length >> 8); result[1] = (byte)length;
            result[2] = 3; result[3] = 232; result[4] = (byte)(id >> 8); result[5] = (byte)id;
            Array.Copy(payload, 0, result, 6, payload.Length);
            return result;
        }

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] == null || actual[i].Length != expected[i].Length) return false;
                for (int j = 0; j < expected[i].Length; j++)
                    if (actual[i][j] != expected[i][j]) return false;
            }
            return true;
        }

        private static List<FieldState> CaptureModel(HolyTerritoryModel model)
        {
            var states = new List<FieldState>();
            foreach (FieldInfo field in typeof(HolyTerritoryModel).GetFields(F))
            {
                if (field.IsStatic) continue;
                object value = field.GetValue(model);
                var state = new FieldState { Field = field, Value = value, IsDictionary = value is IDictionary };
                if (value is IDictionary dictionary)
                    foreach (DictionaryEntry entry in dictionary) state.Entries[entry.Key] = entry.Value;
                states.Add(state);
            }
            return states;
        }

        private static void RestoreModel(HolyTerritoryModel model, IEnumerable<FieldState> states)
        {
            foreach (FieldState state in states)
            {
                if (state.IsDictionary)
                {
                    if (!(state.Field.GetValue(model) is IDictionary dictionary)) continue;
                    dictionary.Clear();
                    foreach (KeyValuePair<object, object> entry in state.Entries)
                        dictionary[entry.Key] = entry.Value;
                }
                else if (!state.Field.IsInitOnly) state.Field.SetValue(model, state.Value);
            }
        }

        private static bool ModelMatches(HolyTerritoryModel model, IEnumerable<FieldState> states)
        {
            foreach (FieldState state in states)
            {
                object current = state.Field.GetValue(model);
                if (state.IsDictionary)
                {
                    if (!(current is IDictionary dictionary) || dictionary.Count != state.Entries.Count) return false;
                    foreach (KeyValuePair<object, object> entry in state.Entries)
                        if (!dictionary.Contains(entry.Key) || !ReferenceEquals(dictionary[entry.Key], entry.Value))
                            return false;
                }
                else if (state.Value != null && !state.Value.GetType().IsValueType)
                {
                    if (!ReferenceEquals(current, state.Value)) return false;
                }
                else if (!Equals(current, state.Value)) return false;
            }
            return true;
        }

        private static EntryState SaveEntry(IDictionary map, object key)
        {
            bool exists = map != null && map.Contains(key);
            return new EntryState { Exists = exists, Value = exists ? map[key] : null };
        }

        private static void RestoreEntry(IDictionary map, object key, EntryState state)
        {
            if (map == null || state == null) return;
            if (state.Exists) map[key] = state.Value;
            else map.Remove(key);
        }

        private static bool EntryMatches(IDictionary map, object key, EntryState state) =>
            map != null && state != null && map.Contains(key) == state.Exists
            && (!state.Exists || ReferenceEquals(map[key], state.Value));
    }
}
