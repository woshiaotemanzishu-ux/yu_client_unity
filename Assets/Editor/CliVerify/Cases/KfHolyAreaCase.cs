using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.KfHolyArea;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>284族安全读侧：真实wire、键控全量、推送补丁/重查、空表与ambient恢复。</summary>
    public static class KfHolyAreaCase
    {
        private const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly int[] RegisteredIds =
        {
            28400, 28401, 28403, 28405, 28407, 28410, 28411, 28412,
            28413, 28414, 28415, 28416, 28417, 28421, 28422, 28423
        };

        private static readonly int[] ExcludedIds = { 28404, 28406, 28408, 28409, 28418, 28419, 28420 };

        private sealed class EntryState
        {
            public bool Exists;
            public object Value;
        }

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
                Debug.LogError("CLIVERIFY kfholyarea EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            KfHolyAreaController controller = KfHolyAreaController.Instance;
            KfHolyAreaModel model = KfHolyAreaModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            List<FieldState> oldModel = CaptureModel(model);
            FieldInfo interceptor = typeof(KfHolyAreaController).GetField("s_outboundIntercept", SF);
            FieldInfo lastLevel = typeof(KfHolyAreaController).GetField("_lastLevel", F);
            object oldInterceptor = interceptor?.GetValue(null);
            object oldLastLevel = lastLevel?.GetValue(controller);

            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, EntryState>();
            for (int id = 28400; id <= 28423; id++) oldHandlers[id] = SaveEntry(handlers, id);

            ActivityIconManager iconManager = ActivityIconManager.Instance;
            var mainIcons = typeof(ActivityIconManager).GetField("_iconInfoByType", F)?.GetValue(iconManager) as IDictionary;
            var boxIcons = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", F)?.GetValue(iconManager) as IDictionary;
            EntryState oldMainIcon = SaveEntry(mainIcons, KfHolyAreaController.ICON_TYPE);
            EntryState oldBoxIcon = SaveEntry(boxIcons, KfHolyAreaController.ICON_TYPE);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                if (handlers != null)
                    for (int id = 28400; id <= 28423; id++) handlers.Remove(id);

                controller.Init();
                model.Reset();
                var frames = new List<byte[]>();
                interceptor?.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));

                bool a = handlers != null && interceptor != null && lastLevel != null
                    && mainIcons != null && boxIcons != null && ExactHandlers(handlers)
                    && OnlySafePublicMethods();
                object firstHandler = handlers != null && handlers.Contains(28400) ? handlers[28400] : null;
                controller.Init();
                a &= firstHandler != null && ReferenceEquals(firstHandler, handlers[28400]);

                model.SetActTime(11, 22);
                frames.Clear();
                controller.RequestStartup();
                bool b = model.ActStart == 11 && model.ActEnd == 22
                    && FramesAre(frames, EmptyFrame(28410));
                frames.Clear();
                controller.RequestOverview();
                controller.RequestBuildingInfo(uint.MaxValue);
                controller.RequestBossDamage(uint.MaxValue, 4000000000U);
                controller.RequestScore();
                controller.RequestKillLog(uint.MaxValue, 4000000000U);
                controller.RequestDeathFatigue();
                controller.RequestRoleRank(ushort.MaxValue);
                b &= FramesAre(frames, EmptyFrame(28400), U32Frame(28401, uint.MaxValue),
                    U32PairFrame(28403, uint.MaxValue, 4000000000U), EmptyFrame(28405),
                    U32PairFrame(28412, uint.MaxValue, 4000000000U), EmptyFrame(28415),
                    U16Frame(28422, ushort.MaxValue));

                frames.Clear();
                bool c = Invoke(controller, 28410,
                        new CliVerify.Pkt().I(uint.MaxValue).I(4000000000L).Bytes())
                    && model.ActStart == uint.MaxValue && model.ActEnd == 4000000000L
                    && FramesAre(frames, EmptyFrame(28400), EmptyFrame(28405));
                c &= Invoke(controller, 28400, OverviewPacket(false)) && model.Overview != null
                    && model.Overview.SanctuaryType == byte.MaxValue && model.Overview.Servers.Count == 2
                    && model.Overview.Servers[0].ServerId == uint.MaxValue
                    && model.Overview.Servers[0].ServerNum == ushort.MaxValue
                    && model.Overview.Servers[0].ServerName == "服甲"
                    && model.Overview.Servers[1].ServerId == uint.MaxValue;
                c &= Invoke(controller, 28400, OverviewPacket(true)) && model.Overview != null
                    && model.Overview.SanctuaryType == 0 && model.Overview.Servers.Count == 0;

                KfHolyAreaModel.BuildingSnapshot building = null;
                bool d = Invoke(controller, 28401, BuildingPacket(70001, false))
                    && model.TryGetBuilding(70001, out building)
                    && building.ConstructionType == byte.MaxValue && building.BelongCamp == uint.MaxValue
                    && building.PreviousBelongCamp == 4000000000U
                    && building.CampScores.Count == 2 && building.CampScores[0].Score == ushort.MaxValue
                    && building.Bosses.Count == 2 && building.Bosses[0].RebornTime == uint.MaxValue
                    && building.RankEntries.Count == 2
                    && building.RankEntries[0].PlayerId == ulong.MaxValue
                    && building.RankEntries[0].KillNum == 0xFEDCBA9876543210UL
                    && building.RankEntries[1].PlayerId == ulong.MaxValue;
                d &= Invoke(controller, 28401, BuildingPacket(70002, true))
                    && model.TryGetBuilding(70002, out KfHolyAreaModel.BuildingSnapshot emptyBuilding)
                    && emptyBuilding.CampScores.Count == 0 && emptyBuilding.Bosses.Count == 0
                    && emptyBuilding.RankEntries.Count == 0 && model.Buildings.Count == 2;
                KfHolyAreaModel.CampScoreEntry oldCampScore = building.CampScores[0];
                d &= Invoke(controller, 28421, SceneRankPacket(70001, 201, false))
                    && model.LastSceneRank != null && model.LastSceneRank.SceneId == 70001
                    && model.TryGetBuilding(70001, out KfHolyAreaModel.BuildingSnapshot patched)
                    && patched.BelongCamp == 201 && patched.RankEntries.Count == 2
                    && patched.RankEntries[0].RoleName == "推榜甲"
                    && ReferenceEquals(patched.CampScores[0], oldCampScore);
                d &= Invoke(controller, 28421, SceneRankPacket(79999, 202, true))
                    && !model.TryGetBuilding(79999, out _) && model.Buildings.Count == 2
                    && model.LastSceneRank.SceneId == 79999 && model.LastSceneRank.Entries.Count == 0;

                bool e = Invoke(controller, 28403, BossDamagePacket()) && model.LastBossDamage != null
                    && model.LastBossDamage.BossId == uint.MaxValue
                    && model.LastBossDamage.Entries.Count == 2
                    && model.LastBossDamage.Entries[0].ServerName == "伤害服甲"
                    && model.LastBossDamage.Entries[0].RoleId == uint.MaxValue
                    && model.LastBossDamage.Entries[1].RoleId == uint.MaxValue;
                e &= Invoke(controller, 28405, ScorePacket(false)) && model.Score != null
                    && model.Score.Score == uint.MaxValue && model.Score.Cost == byte.MaxValue
                    && model.Score.Anger == ushort.MaxValue && model.Score.Rewards.Count == 2
                    && model.Score.Rewards[0].ScoreConfig == ushort.MaxValue;
                e &= Invoke(controller, 28405, ScorePacket(true)) && model.Score.Rewards.Count == 0;
                e &= Invoke(controller, 28412, KillLogPacket(80001, 90001, false))
                    && model.TryGetKillLog(80001, 90001, out KfHolyAreaModel.KillLogSnapshot log)
                    && log.Entries.Count == 2 && log.Entries[0].ServerNum == uint.MaxValue
                    && log.Entries[0].RoleId == uint.MaxValue && log.Entries[0].RoleName == "击杀甲"
                    && log.Entries[1].RoleId == uint.MaxValue;
                e &= Invoke(controller, 28412, KillLogPacket(80002, 90002, true))
                    && model.TryGetKillLog(80002, 90002, out KfHolyAreaModel.KillLogSnapshot emptyLog)
                    && emptyLog.Entries.Count == 0 && model.KillLogs.Count == 2;
                e &= Invoke(controller, 28422,
                        new CliVerify.Pkt().H(ushort.MaxValue).C(byte.MaxValue)
                            .H(ushort.MaxValue).H(65534).Bytes())
                    && model.TryGetRoleRank(ushort.MaxValue, out KfHolyAreaModel.RoleRankSnapshot roleRank)
                    && roleRank.Rank == byte.MaxValue && roleRank.Score == ushort.MaxValue
                    && roleRank.KillScore == 65534;

                frames.Clear();
                bool f = Invoke(controller, 28411,
                        new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue).Bytes())
                    && model.LastOccupy != null && model.LastOccupy.SceneId == uint.MaxValue
                    && model.LastOccupy.ConstructionType == byte.MaxValue
                    && FramesAre(frames, EmptyFrame(28400));
                frames.Clear();
                f &= Invoke(controller, 28413, new CliVerify.Pkt().C(byte.MaxValue).Bytes())
                    && model.LastBossRefresh != null && model.LastBossRefresh.Code == byte.MaxValue;
                f &= Invoke(controller, 28415, new CliVerify.Pkt().H(ushort.MaxValue)
                        .I(uint.MaxValue).I(4000000000L).I(3999999999L).Bytes())
                    && model.DeathFatigue != null && model.DeathFatigue.DieTimes == ushort.MaxValue
                    && model.DeathFatigue.FreeReviveTime == uint.MaxValue
                    && model.DeathFatigue.DebuffEndTime == 4000000000U
                    && model.DeathFatigue.SafeTime == 3999999999U;
                f &= Invoke(controller, 28416,
                        new CliVerify.Pkt().I(uint.MaxValue).I(4000000000L).Bytes())
                    && model.LastBossLife != null && model.LastBossLife.BossId == uint.MaxValue
                    && model.LastBossLife.RebornTime == 4000000000U;
                f &= Invoke(controller, 28417, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.LastExitCountdown != null && model.LastExitCountdown.OutTime == uint.MaxValue;
                f &= Invoke(controller, 28423, new CliVerify.Pkt().H(60000).Bytes())
                    && model.LastBelongRefresh != null && model.LastBelongRefresh.SceneId == 60000
                    && FramesAre(frames, U32Frame(28401, 60000));

                controller.Dispose();
                bool g = !controller.IsInitialized && ModelIsEmpty(model) && NoFamilyHandlers(handlers);

                pass = a && b && c && d && e && f && g;
                Debug.Log("CLIVERIFY kfholyarea A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 28400; id <= 28423; id++) RestoreEntry(handlers, id, oldHandlers[id]);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);
                if (lastLevel != null) lastLevel.SetValue(controller, oldLastLevel);
                RestoreEntry(mainIcons, KfHolyAreaController.ICON_TYPE, oldMainIcon);
                RestoreEntry(boxIcons, KfHolyAreaController.ICON_TYPE, oldBoxIcon);

                restored = controller.IsInitialized == wasInitialized && ModelMatches(model, oldModel)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor))
                    && (lastLevel == null || Equals(lastLevel.GetValue(controller), oldLastLevel))
                    && EntryMatches(mainIcons, KfHolyAreaController.ICON_TYPE, oldMainIcon)
                    && EntryMatches(boxIcons, KfHolyAreaController.ICON_TYPE, oldBoxIcon);
                for (int id = 28400; id <= 28423; id++)
                    restored &= EntryMatches(handlers, id, oldHandlers[id]);
                Debug.Log("CLIVERIFY kfholyarea restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool ExactHandlers(IDictionary handlers)
        {
            var expected = new HashSet<int>(RegisteredIds);
            for (int id = 28400; id <= 28423; id++)
                if (handlers.Contains(id) != expected.Contains(id)) return false;
            foreach (int id in ExcludedIds) if (handlers.Contains(id)) return false;
            return true;
        }

        private static bool NoFamilyHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int id = 28400; id <= 28423; id++) if (handlers.Contains(id)) return false;
            return true;
        }

        private static bool OnlySafePublicMethods()
        {
            var expected = new HashSet<string>
            {
                "RequestStartup", "RequestOverview", "RequestBuildingInfo", "RequestBossDamage",
                "RequestScore", "RequestKillLog", "RequestDeathFatigue", "RequestRoleRank", "Dispose"
            };
            foreach (MethodInfo method in typeof(KfHolyAreaController).GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                if (!expected.Remove(method.Name)) return false;
            return expected.Count == 0;
        }

        private static bool ModelIsEmpty(KfHolyAreaModel m) =>
            m.ActStart == 0 && m.ActEnd == 0 && m.Overview == null && m.Buildings.Count == 0
            && m.LastBossDamage == null && m.Score == null && m.LastOccupy == null
            && m.KillLogs.Count == 0 && m.LastBossRefresh == null && m.DeathFatigue == null
            && m.LastBossLife == null && m.LastExitCountdown == null && m.LastSceneRank == null
            && m.RoleRanks.Count == 0 && m.LastBelongRefresh == null;

        private static bool Invoke(KfHolyAreaController controller, int protocolId, byte[] bytes)
        {
            MethodInfo method = typeof(KfHolyAreaController).GetMethod("On" + protocolId,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static byte[] OverviewPacket(bool empty)
        {
            var p = new CliVerify.Pkt().C(empty ? 0 : byte.MaxValue).H(empty ? 0 : 2);
            if (empty) return p.Bytes();
            Server(p, "甲", true);
            Server(p, "乙", false);
            return p.Bytes();
        }

        private static void Server(CliVerify.Pkt p, string suffix, bool max) =>
            p.I(uint.MaxValue).H(max ? ushort.MaxValue : 2).S("服" + suffix)
                .H(max ? ushort.MaxValue : 3).C(max ? byte.MaxValue : 4);

        private static byte[] BuildingPacket(uint scene, bool empty)
        {
            var p = new CliVerify.Pkt().I(scene).C(empty ? 0 : byte.MaxValue)
                .I(empty ? 0 : uint.MaxValue).I(empty ? 0 : 4000000000L).H(empty ? 0 : 2);
            if (!empty)
            {
                p.C(byte.MaxValue).H(ushort.MaxValue).C(byte.MaxValue).H(1);
            }
            p.C(empty ? 0 : byte.MaxValue).H(empty ? 0 : ushort.MaxValue).H(empty ? 0 : 2);
            if (!empty)
            {
                Boss(p, true);
                Boss(p, false);
            }
            p.H(empty ? 0 : 2);
            if (!empty)
            {
                SceneRank(p, "甲", true);
                SceneRank(p, "乙", false);
            }
            return p.Bytes();
        }

        private static void Boss(CliVerify.Pkt p, bool max) =>
            p.I(uint.MaxValue).C(max ? byte.MaxValue : 2).H(max ? ushort.MaxValue : 3)
                .I(max ? uint.MaxValue : 4);

        private static void SceneRank(CliVerify.Pkt p, string suffix, bool max) =>
            p.L(unchecked((long)ulong.MaxValue)).S("榜" + suffix).I(uint.MaxValue)
                .H(max ? ushort.MaxValue : 2).I(max ? uint.MaxValue : 3)
                .L(unchecked((long)(max ? 0xFEDCBA9876543210UL : ulong.MaxValue)))
                .C(max ? byte.MaxValue : 4);

        private static byte[] BossDamagePacket()
        {
            var p = new CliVerify.Pkt().I(uint.MaxValue).H(2);
            Damage(p, "甲", true);
            Damage(p, "乙", false);
            return p.Bytes();
        }

        private static void Damage(CliVerify.Pkt p, string suffix, bool max) =>
            p.I(uint.MaxValue).H(max ? ushort.MaxValue : 2).S("伤害服" + suffix)
                .I(uint.MaxValue).S("伤害" + suffix).H(max ? ushort.MaxValue : 3);

        private static byte[] ScorePacket(bool empty)
        {
            var p = new CliVerify.Pkt().I(empty ? 0 : uint.MaxValue).C(empty ? 0 : byte.MaxValue)
                .H(empty ? 0 : ushort.MaxValue).H(empty ? 0 : 2);
            if (!empty) p.H(ushort.MaxValue).C(byte.MaxValue).H(ushort.MaxValue).C(1);
            return p.Bytes();
        }

        private static byte[] KillLogPacket(uint scene, uint monster, bool empty)
        {
            var p = new CliVerify.Pkt().I(scene).I(monster).H(empty ? 0 : 2);
            if (empty) return p.Bytes();
            KillLog(p, "甲", true);
            KillLog(p, "乙", false);
            return p.Bytes();
        }

        private static void KillLog(CliVerify.Pkt p, string suffix, bool max) =>
            p.I(uint.MaxValue).I(max ? uint.MaxValue : 2).I(uint.MaxValue)
                .S("击杀" + suffix).I(max ? uint.MaxValue : 3);

        private static byte[] SceneRankPacket(uint scene, byte camp, bool empty)
        {
            var p = new CliVerify.Pkt().I(scene).C(camp).H(empty ? 0 : 2);
            if (empty) return p.Bytes();
            p.L(unchecked((long)ulong.MaxValue)).S("推榜甲").I(uint.MaxValue)
                .H(ushort.MaxValue).I(uint.MaxValue).L(unchecked((long)ulong.MaxValue)).C(byte.MaxValue);
            p.L(unchecked((long)ulong.MaxValue)).S("推榜乙").I(uint.MaxValue)
                .H(2).I(3).L(unchecked((long)ulong.MaxValue)).C(4);
            return p.Bytes();
        }

        private static byte[] EmptyFrame(int id) =>
            new byte[] { 0, 6, 3, 232, (byte)(id >> 8), (byte)id };

        private static byte[] U16Frame(int id, ushort value) =>
            new byte[] { 0, 8, 3, 232, (byte)(id >> 8), (byte)id, (byte)(value >> 8), (byte)value };

        private static byte[] U32Frame(int id, uint value) =>
            new byte[]
            {
                0, 10, 3, 232, (byte)(id >> 8), (byte)id,
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
            };

        private static byte[] U32PairFrame(int id, uint first, uint second) =>
            new byte[]
            {
                0, 14, 3, 232, (byte)(id >> 8), (byte)id,
                (byte)(first >> 24), (byte)(first >> 16), (byte)(first >> 8), (byte)first,
                (byte)(second >> 24), (byte)(second >> 16), (byte)(second >> 8), (byte)second
            };

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

        private static List<FieldState> CaptureModel(KfHolyAreaModel model)
        {
            var states = new List<FieldState>();
            foreach (FieldInfo field in typeof(KfHolyAreaModel).GetFields(F))
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

        private static void RestoreModel(KfHolyAreaModel model, IEnumerable<FieldState> states)
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

        private static bool ModelMatches(KfHolyAreaModel model, IEnumerable<FieldState> states)
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
