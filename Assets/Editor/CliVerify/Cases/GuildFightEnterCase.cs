using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.GuildFight;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>领地战 506xx 安全读侧：线序、增量、自动重拉、写边界与 ambient 恢复。</summary>
    public static class GuildFightEnterCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

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
                Debug.LogError("CLIVERIFY guildfight EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            GuildFightController controller = GuildFightController.Instance;
            GuildFightModel model = GuildFightModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            List<FieldState> oldModel = CaptureModel(model);
            FieldInfo interceptor = typeof(GuildFightController).GetField(
                "s_enterOutboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, EntryState>();
            for (int id = 50600; id <= 50627; id++) SaveEntry(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                if (handlers != null)
                    for (int id = 50600; id <= 50627; id++) handlers.Remove(id);

                controller.Init();
                model.Reset();
                var frames = new List<byte[]>();
                interceptor?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                bool a = handlers != null && interceptor != null && ExactHandlers(handlers)
                    && OnlySafePublicMethods();
                object first50600 = handlers != null && handlers.Contains(50600) ? handlers[50600] : null;
                controller.Init();
                a &= controller.IsInitialized && first50600 != null
                    && ReferenceEquals(first50600, handlers[50600]);

                controller.RequestStartup();
                bool b = FramesAre(frames, EmptyFrame(50600), EmptyFrame(50601),
                    EmptyFrame(50622), EmptyFrame(50624));
                frames.Clear();
                controller.RequestEnter();
                b &= FramesAre(frames, U8Frame(50603, 1));
                frames.Clear();
                controller.RequestBattle();
                controller.RequestRound();
                controller.RequestWars();
                b &= FramesAre(frames, EmptyFrame(50604), EmptyFrame(50620), EmptyFrame(50621));
                frames.Clear();

                bool c = Invoke(controller, 50600,
                    new CliVerify.Pkt().C(1).I(2).I(3).I(uint.MaxValue).Bytes());
                c &= model.HasState && model.WarState == 1 && model.ReadyTime == 2
                    && model.StartTime == 3 && model.EndTime == uint.MaxValue
                    && FramesAre(frames, EmptyFrame(50624), EmptyFrame(50620));
                frames.Clear();
                c &= Invoke(controller, 50600,
                    new CliVerify.Pkt().C(2).I(4).I(5).I(6).Bytes())
                    && FramesAre(frames, EmptyFrame(50621), EmptyFrame(50620));
                frames.Clear();
                c &= Invoke(controller, 50601, OverviewPacket())
                    && model.HasOverview && model.OverviewType == byte.MaxValue
                    && model.WinnerGuildId == ulong.MaxValue
                    && model.WinnerServerId == ushort.MaxValue && model.WinNumber == 40000
                    && model.RewardType == 7 && model.RewardKey == 50000
                    && model.RewardOwnerRoleId == 0xFEDCBA9876543210UL;
                c &= Invoke(controller, 50603,
                    new CliVerify.Pkt().I(uint.MaxValue).C(byte.MaxValue).Bytes())
                    && model.HasEnterResult && model.EnterResultCode == uint.MaxValue
                    && model.EnterResultType == byte.MaxValue && frames.Count == 0;

                bool d = Invoke(controller, 50604, BattlePacket())
                    && model.HasBattle && model.BattleTerritoryId == uint.MaxValue
                    && model.BattleEndTime == 4000000000U && model.BattleRoleScore == 77
                    && model.HasRoleScore && model.RoleScore == 77
                    && model.BattleGuilds.Count == 2 && model.BattleStages.Count == 2
                    && model.BattleOwns.Count == 2 && model.CurrentGuildsById.Count == 1
                    && model.CurrentGuildsById[10].GuildName == "guild-last"
                    && model.CurrentGuildsById[10].Score == 6
                    && model.CurrentOwnsByMonsterId.Count == 2;
                d &= Invoke(controller, 50606, GuildUpdatePacket())
                    && model.HasGuildUpdate && model.LastGuildUpdate.Count == 3
                    && model.CurrentGuildsById.Count == 1
                    && model.CurrentGuildsById[10].Score == 99
                    && model.CurrentGuildsById[10].OwnList.Count == 2
                    && model.CurrentGuildsById[10].OwnList[1] == 902
                    && model.BattleGuilds[1].Score == 6;
                d &= Invoke(controller, 50607, OwnUpdatePacket())
                    && model.HasOwnUpdate && model.LastOwnUpdate.Count == 3
                    && model.CurrentOwnsByMonsterId.Count == 2
                    && model.CurrentOwnsByMonsterId[1000].Hp == 88
                    && model.CurrentOwnsByMonsterId[1000].GuildName == "own-last"
                    && model.BattleOwns[0].Hp == 11 && frames.Count == 0;

                bool e = Invoke(controller, 50611, ResultPacket())
                    && model.HasResult && model.ResultTerritoryId == uint.MaxValue
                    && model.ResultModeNumber == byte.MaxValue && model.ResultGuilds.Count == 2
                    && model.ResultGuilds[0].GuildId == 9 && model.ResultGuilds[1].GuildId == 9
                    && FramesAre(frames, EmptyFrame(50620), EmptyFrame(50600));
                frames.Clear();
                e &= Invoke(controller, 50612, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.RoleScore == uint.MaxValue;
                e &= Invoke(controller, 50617, new CliVerify.Pkt().I(1234).Bytes())
                    && model.HasConvene && model.ConveneMonsterId == 1234;
                e &= Invoke(controller, 50619, KillPacket())
                    && model.HasKillStreak && model.KillStreak.AttackerServerId == ushort.MaxValue
                    && model.KillStreak.RoleId == ulong.MaxValue
                    && model.KillStreak.ConsecutiveKills == uint.MaxValue;
                e &= Invoke(controller, 50620,
                    new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue).I(4000000000L).Bytes())
                    && model.HasRound && model.Round == byte.MaxValue
                    && model.RoundStartTime == uint.MaxValue && model.RoundEndTime == 4000000000U
                    && FramesAre(frames, EmptyFrame(50621));
                frames.Clear();
                e &= Invoke(controller, 50621, WarsPacket())
                    && model.HasWars && model.Wars.Count == 2
                    && model.Wars[0].TerritoryId == 1 && model.Wars[1].TerritoryId == 1;
                e &= Invoke(controller, 50622, ServersPacket())
                    && model.HasServers && model.ModeNumber == byte.MaxValue
                    && model.AverageWorldLevel == ushort.MaxValue && model.Servers.Count == 2;
                e &= Invoke(controller, 50624, new CliVerify.Pkt().C(3).C(4).Bytes())
                    && model.HasQualification && model.Qualification == 3
                    && model.IsTerritoryChosen == 4;
                e &= Invoke(controller, 50625, new CliVerify.Pkt().C(7).Bytes())
                    && model.HasQualificationUpdate && model.LastQualificationUpdate == 7
                    && model.Qualification == 7 && model.IsTerritoryChosen == 4
                    && FramesAre(frames, EmptyFrame(50620), EmptyFrame(50600));
                frames.Clear();
                e &= Invoke(controller, 50626, new CliVerify.Pkt().C(byte.MaxValue).Bytes())
                    && model.HasWarListNotice && model.WarListNotice == byte.MaxValue
                    && frames.Count == 0;
                e &= Invoke(controller, 50627, new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.HasTerritoryNotice && model.TerritoryNoticeId == uint.MaxValue
                    && frames.Count == 0;

                bool f = Invoke(controller, 50604, EmptyBattlePacket())
                    && model.HasBattle && model.BattleGuilds.Count == 0
                    && model.BattleStages.Count == 0 && model.BattleOwns.Count == 0
                    && model.CurrentGuildsById.Count == 0 && model.CurrentOwnsByMonsterId.Count == 0
                    && model.HasResult && model.ResultGuilds.Count == 2
                    && model.HasWars && model.Wars.Count == 2;
                f &= Invoke(controller, 50611, new CliVerify.Pkt().I(0).C(0).H(0).Bytes())
                    && model.HasResult && model.ResultGuilds.Count == 0
                    && FramesAre(frames, EmptyFrame(50620), EmptyFrame(50600));
                frames.Clear();
                f &= Invoke(controller, 50621, new CliVerify.Pkt().H(0).Bytes())
                    && model.HasWars && model.Wars.Count == 0;
                f &= Invoke(controller, 50622, new CliVerify.Pkt().C(0).H(0).H(0).Bytes())
                    && model.HasServers && model.Servers.Count == 0
                    && model.ModeNumber == 0 && model.AverageWorldLevel == 0;

                controller.Dispose();
                bool g = !controller.IsInitialized && ModelIsEmpty(model) && NoFamilyHandlers(handlers)
                    && frames.Count == 0;
                pass = a && b && c && d && e && f && g;
                Debug.Log("CLIVERIFY guildfight A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g
                    + " pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                if (wasInitialized) controller.Init();
                for (int id = 50600; id <= 50627; id++)
                    RestoreEntry(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ModelMatches(model, oldModel)
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                for (int id = 50600; id <= 50627; id++)
                    restored &= EntryMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY guildfight restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static bool ExactHandlers(IDictionary handlers)
        {
            var expected = new HashSet<int>
            {
                50600, 50601, 50603, 50604, 50606, 50607, 50611, 50612, 50617,
                50619, 50620, 50621, 50622, 50624, 50625, 50626, 50627
            };
            for (int id = 50600; id <= 50627; id++)
                if (handlers.Contains(id) != expected.Contains(id)) return false;
            return true;
        }

        private static bool OnlySafePublicMethods()
        {
            var expected = new HashSet<string>
            {
                "RequestStartup", "RequestState", "RequestOverview", "RequestEnter",
                "RequestBattle", "RequestRound", "RequestWars", "RequestServers",
                "RequestQualification", "Dispose"
            };
            foreach (MethodInfo method in typeof(GuildFightController).GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!expected.Remove(method.Name)) return false;
            return expected.Count == 0;
        }

        private static bool Invoke(GuildFightController controller, int protocolId, byte[] bytes)
        {
            MethodInfo handler = typeof(GuildFightController).GetMethod("On" + protocolId, F);
            if (handler == null) return false;
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static byte[] OverviewPacket() => new CliVerify.Pkt()
            .C(byte.MaxValue).L(unchecked((long)ulong.MaxValue)).H(ushort.MaxValue).H(40000)
            .C(7).H(50000).L(unchecked((long)0xFEDCBA9876543210UL)).Bytes();

        private static byte[] BattlePacket() => new CliVerify.Pkt()
            .I(uint.MaxValue).I(4000000000L).I(77).H(2)
            .L(10).S("guild-first").H(1).H(2).I(3).H(1).I(100)
            .L(10).S("guild-last").H(4).H(5).I(6).H(1).I(101)
            .H(2).C(7).C(8).H(2)
            .C(1).L(10).S("own-a").I(1000).I(11).I(111)
            .C(2).L(10).S("own-b").I(1001).I(22).I(222).Bytes();

        private static byte[] EmptyBattlePacket() =>
            new CliVerify.Pkt().I(0).I(0).I(0).H(0).H(0).H(0).Bytes();

        private static byte[] GuildUpdatePacket() => new CliVerify.Pkt().H(3)
            .L(99).I(1).H(1).I(800)
            .L(10).I(88).H(1).I(901)
            .L(10).I(99).H(2).I(901).I(902).Bytes();

        private static byte[] OwnUpdatePacket() => new CliVerify.Pkt().H(3)
            .C(9).L(99).S("unknown").I(9999).I(1).I(2)
            .C(3).L(10).S("own-mid").I(1000).I(77).I(777)
            .C(4).L(10).S("own-last").I(1000).I(88).I(888).Bytes();

        private static byte[] ResultPacket() => new CliVerify.Pkt()
            .I(uint.MaxValue).C(byte.MaxValue).H(2)
            .L(9).C(0).S("result-a").H(1).H(2).I(3).H(1).I(4)
            .L(9).C(1).S("result-b").H(5).H(6).I(7).H(2).I(8).I(9).Bytes();

        private static byte[] KillPacket() => new CliVerify.Pkt()
            .H(ushort.MaxValue).L(unchecked((long)ulong.MaxValue)).S("killer")
            .C(byte.MaxValue).C(byte.MaxValue).C(byte.MaxValue).C(byte.MaxValue)
            .H(ushort.MaxValue).S("picture").I(uint.MaxValue).I(uint.MaxValue).Bytes();

        private static byte[] WarsPacket() => new CliVerify.Pkt().H(2)
            .I(1).L(2).S("attacker-a").H(3).H(4).L(5).S("defender-a").H(6).H(7).L(8)
            .I(1).L(9).S("attacker-b").H(10).H(11).L(12).S("defender-b").H(13).H(14).L(15)
            .Bytes();

        private static byte[] ServersPacket() => new CliVerify.Pkt()
            .C(byte.MaxValue).H(ushort.MaxValue).H(2)
            .H(1).H(2).S("server-a").H(3)
            .H(1).H(4).S("server-b").H(5).Bytes();

        private static byte[] EmptyFrame(int id) => new CliVerify.Pkt().H(6).H(1000).H(id).Bytes();
        private static byte[] U8Frame(int id, byte value) =>
            new CliVerify.Pkt().H(7).H(1000).H(id).C(value).Bytes();

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (actual[i] == null || actual[i].Length != expected[i].Length) return false;
                for (int j = 0; j < expected[i].Length; j++)
                    if (actual[i][j] != expected[i][j]) return false;
            }
            return true;
        }

        private static bool NoFamilyHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int id = 50600; id <= 50627; id++) if (handlers.Contains(id)) return false;
            return true;
        }

        private static bool ModelIsEmpty(GuildFightModel model) =>
            !model.HasState && !model.HasOverview && !model.HasEnterResult && !model.HasBattle
            && !model.HasGuildUpdate && !model.HasOwnUpdate && !model.HasResult
            && !model.HasRoleScore && !model.HasConvene && !model.HasKillStreak
            && !model.HasRound && !model.HasWars && !model.HasServers
            && !model.HasQualification && !model.HasQualificationUpdate
            && !model.HasWarListNotice && !model.HasTerritoryNotice
            && model.BattleGuilds.Count == 0 && model.CurrentGuildsById.Count == 0
            && model.CurrentOwnsByMonsterId.Count == 0;

        private static List<FieldState> CaptureModel(GuildFightModel model)
        {
            var result = new List<FieldState>();
            foreach (FieldInfo field in typeof(GuildFightModel).GetFields(F | BindingFlags.Public))
            {
                object value = field.GetValue(model);
                if (field.IsInitOnly
                    && (!(value is IDictionary initDictionary) || initDictionary.IsReadOnly))
                    continue;
                var state = new FieldState { Field = field, Value = value };
                if (value is IDictionary dictionary)
                {
                    state.IsDictionary = true;
                    foreach (DictionaryEntry pair in dictionary) state.Entries[pair.Key] = pair.Value;
                }
                result.Add(state);
            }
            return result;
        }

        private static void RestoreModel(GuildFightModel model, IReadOnlyList<FieldState> states)
        {
            foreach (FieldState state in states)
            {
                if (state.IsDictionary && state.Field.GetValue(model) is IDictionary dictionary)
                {
                    dictionary.Clear();
                    foreach (KeyValuePair<object, object> pair in state.Entries)
                        dictionary[pair.Key] = pair.Value;
                }
                else state.Field.SetValue(model, state.Value);
            }
        }

        private static bool ModelMatches(GuildFightModel model, IReadOnlyList<FieldState> states)
        {
            foreach (FieldState state in states)
            {
                object current = state.Field.GetValue(model);
                if (state.IsDictionary)
                {
                    if (!(current is IDictionary dictionary) || dictionary.Count != state.Entries.Count)
                        return false;
                    foreach (KeyValuePair<object, object> pair in state.Entries)
                        if (!dictionary.Contains(pair.Key) || !ReferenceEquals(dictionary[pair.Key], pair.Value))
                            return false;
                }
                else if (current is ValueType || current is string)
                {
                    if (!Equals(current, state.Value)) return false;
                }
                else if (!ReferenceEquals(current, state.Value)) return false;
            }
            return true;
        }

        private static void SaveEntry(IDictionary handlers, IDictionary<int, EntryState> saved, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            saved[id] = new EntryState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreEntry(IDictionary handlers, EntryState state, int id)
        {
            if (handlers == null) return;
            if (state.Exists) handlers[id] = state.Value;
            else handlers.Remove(id);
        }

        private static bool EntryMatches(IDictionary handlers, EntryState state, int id) =>
            handlers != null && handlers.Contains(id) == state.Exists
            && (!state.Exists || ReferenceEquals(handlers[id], state.Value));
    }
}
