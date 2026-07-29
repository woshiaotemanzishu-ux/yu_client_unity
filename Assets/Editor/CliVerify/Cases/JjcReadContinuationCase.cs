using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Jjc;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>R506：JJC 28000/28010/28013/28014 只读续接、隔离、生命周期与 ambient 深恢复。</summary>
    public static class JjcReadContinuationCase
    {
        private const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly int[] AllCommands =
            { 28000, 28001, 28002, 28003, 28004, 28005, 28006, 28007, 28008, 28009,
              28010, 28011, 28012, 28013, 28014, 28015, 28016, 28017, 28018 };
        private static readonly int[] RegisteredCommands =
            { 28000, 28001, 28002, 28003, 28004, 28009, 28010, 28013, 28014 };

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY jjc-read-continuation EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            JjcController controller = JjcController.Instance;
            JjcModel model = JjcModel.Instance;
            bool oldInitialized = controller.IsInitialized;
            Dictionary<FieldInfo, object> oldAutoFields = CaptureAutoFields(model);
            var oldBreaks = new List<int>(model.BreakIdList);
            var oldRivals = new List<JjcModel.RivalVo>(model.Rivals);
            var oldResults = new List<JjcModel.RivalVo>(model.LastChallengeRoleList);
            var oldRecords = new List<JjcModel.RecordVo>(model.ChallengeRecords);
            FieldInfo intercept = typeof(JjcController).GetField("s_outboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var oldHandlers = new Dictionary<int, object>();
            if (handlers != null)
                foreach (int command in AllCommands)
                    if (handlers.Contains(command)) oldHandlers[command] = handlers[command];

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                MethodInfo h00 = Handler("On28000");
                MethodInfo h10 = Handler("On28010");
                MethodInfo h13 = Handler("On28013");
                MethodInfo h14 = Handler("On28014");
                pass = Proto.JJC_ERROR == 28000 && Proto.JJC_HONOUR == 28010
                    && Proto.JJC_BATTLE_PARTICIPANTS == 28013 && Proto.JJC_BATTLE_STAGE == 28014
                    && h00 != null && h10 != null && h13 != null && h14 != null && intercept != null
                    && RegistrationsExact(handlers)
                    && typeof(JjcController).GetMethod("RequestBattleStage") == null;
                Check(ref pass, "constants/registration/operations-excluded", pass);

                model.Clear();
                model.Apply28001(1, 2, 3, 4, 5, 6, 7, 123, true, 9, new List<int> { 10 });
                model.Apply28004(11, 12, 13, 14);
                model.Apply28009(15, new List<JjcModel.RecordVo> { new JjcModel.RecordVo { RoleId = 16 } });
                model.ReplaceError(17);
                model.ReplaceHonourQuery(18, 19);
                model.ReplaceBattleParticipants(20, 21, 22, 23);
                model.ReplaceBattleStage(24, 25);
                JjcModel.ErrorSnapshot sentinelError = model.Error;
                JjcModel.HonourQuerySnapshot sentinelHonour = model.HonourQuery;
                JjcModel.BattleParticipantsSnapshot sentinelParticipants = model.BattleParticipants;
                JjcModel.BattleStageSnapshot sentinelStage = model.BattleStage;

                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                controller.RequestHonour();
                controller.RequestBattleParticipants();
                Check(ref pass, "strict-empty explicit requests/no-response-preserves", frames.Count == 2
                    && EmptyFrame(frames[0], 28010) && EmptyFrame(frames[1], 28013)
                    && ReferenceEquals(model.Error, sentinelError)
                    && ReferenceEquals(model.HonourQuery, sentinelHonour)
                    && ReferenceEquals(model.BattleParticipants, sentinelParticipants)
                    && ReferenceEquals(model.BattleStage, sentinelStage));

                Check(ref pass, "28000 zero/max whole replace/read-end",
                    Feed(h00, controller, new CliVerify.Pkt().I(0)) && model.Error.Code == 0
                    && Feed(h00, controller, new CliVerify.Pkt().I(uint.MaxValue))
                    && model.Error.Code == uint.MaxValue && !ReferenceEquals(model.Error, sentinelError));
                JjcModel.ErrorSnapshot errorAfter00 = model.Error;

                Check(ref pass, "28010 raw zero/max/isolation/read-end",
                    Feed(h10, controller, new CliVerify.Pkt().I(uint.MaxValue).I(uint.MaxValue))
                    && model.HonourQuery.Code == uint.MaxValue && model.HonourQuery.Honour == uint.MaxValue
                    && model.Honour == 123 && ReferenceEquals(model.Error, errorAfter00)
                    && Feed(h10, controller, new CliVerify.Pkt().I(0).I(0))
                    && model.HonourQuery.Code == 0 && model.HonourQuery.Honour == 0 && model.Honour == 123);
                JjcModel.HonourQuerySnapshot honourAfter10 = model.HonourQuery;

                Check(ref pass, "28013 four-u64 full-width/overwrite/read-end",
                    Feed(h13, controller, new CliVerify.Pkt().L(0).L(-1).L(0x0102030405060708L).L(long.MinValue))
                    && model.BattleParticipants.SelfRobotId == 0
                    && model.BattleParticipants.SelfRoleId == ulong.MaxValue
                    && model.BattleParticipants.RivalRobotId == 0x0102030405060708UL
                    && model.BattleParticipants.RivalRoleId == 0x8000000000000000UL
                    && Feed(h13, controller, new CliVerify.Pkt().L(4).L(3).L(2).L(1))
                    && model.BattleParticipants.SelfRobotId == 4 && model.BattleParticipants.SelfRoleId == 3
                    && model.BattleParticipants.RivalRobotId == 2 && model.BattleParticipants.RivalRoleId == 1
                    && ReferenceEquals(model.HonourQuery, honourAfter10));
                JjcModel.BattleParticipantsSnapshot participantsAfter13 = model.BattleParticipants;

                Check(ref pass, "28014 stage/deadline zero/max/overwrite/read-end",
                    Feed(h14, controller, new CliVerify.Pkt().C(byte.MaxValue).I(uint.MaxValue))
                    && model.BattleStage.Stage == byte.MaxValue && model.BattleStage.EndTime == uint.MaxValue
                    && Feed(h14, controller, new CliVerify.Pkt().C(0).I(0))
                    && model.BattleStage.Stage == 0 && model.BattleStage.EndTime == 0
                    && ReferenceEquals(model.BattleParticipants, participantsAfter13));

                model.ClearReadContinuationSnapshots();
                Check(ref pass, "slice clear leaves established JJC state", model.Error == null
                    && model.HonourQuery == null && model.BattleParticipants == null && model.BattleStage == null
                    && model.HasInfo && model.Rank == 1 && model.Honour == 123
                    && model.HasTimesInfo && model.LeftNum == 12 && model.TimesRefreshAt == 13
                    && model.HasChallengeRecords && model.ChallengeRecords.Count == 1
                    && model.ChallengeRecords[0].RoleId == 16);

                model.ReplaceError(1); model.ReplaceHonourQuery(2, 3);
                model.ReplaceBattleParticipants(4, 5, 6, 7); model.ReplaceBattleStage(8, 9);
                controller.Dispose();
                frames.Clear();
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
                Check(ref pass, "dispose owns slices/handlers/event", !controller.IsInitialized
                    && !model.HasInfo && !model.HasTimesInfo && !model.HasChallengeRecords
                    && model.Error == null && model.HonourQuery == null
                    && model.BattleParticipants == null && model.BattleStage == null
                    && NoRegisteredHandlers(handlers) && frames.Count == 0);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Clear();
                model.BreakIdList.AddRange(oldBreaks);
                model.Rivals.AddRange(oldRivals);
                model.LastChallengeRoleList.AddRange(oldResults);
                model.ChallengeRecords.AddRange(oldRecords);
                RestoreAutoFields(model, oldAutoFields);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (oldInitialized) controller.Init();
                RestoreHandlers(handlers, oldHandlers);
                restored = controller.IsInitialized == oldInitialized
                    && SameAutoFields(model, oldAutoFields)
                    && SameValues(model.BreakIdList, oldBreaks)
                    && SameRefs(model.Rivals, oldRivals)
                    && SameRefs(model.LastChallengeRoleList, oldResults)
                    && SameRefs(model.ChallengeRecords, oldRecords)
                    && SameHandlers(handlers, oldHandlers)
                    && (intercept == null || ReferenceEquals(intercept.GetValue(null), oldIntercept));
            }
            bool finalPass = pass && restored;
            Debug.Log("CLIVERIFY jjc-read-continuation restored=" + restored);
            Debug.Log("CLIVERIFY jjc-read-continuation VERDICT pass=" + finalPass);
            return finalPass ? 0 : 3;
        }

        private static MethodInfo Handler(string name) => typeof(JjcController).GetMethod(name, IF);

        private static bool Feed(MethodInfo method, JjcController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool EmptyFrame(byte[] frame, int command) => frame != null && frame.Length == 6
            && frame[0] == 0 && frame[1] == 6 && frame[2] == 3 && frame[3] == 232
            && frame[4] == (byte)(command >> 8) && frame[5] == (byte)command;

        private static bool IsRegistered(int command)
        {
            for (int i = 0; i < RegisteredCommands.Length; i++)
                if (RegisteredCommands[i] == command) return true;
            return false;
        }

        private static bool RegistrationsExact(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int i = 0; i < AllCommands.Length; i++)
                if (handlers.Contains(AllCommands[i]) != IsRegistered(AllCommands[i])) return false;
            return true;
        }

        private static bool NoRegisteredHandlers(IDictionary handlers)
        {
            if (handlers == null) return false;
            for (int i = 0; i < RegisteredCommands.Length; i++)
                if (handlers.Contains(RegisteredCommands[i])) return false;
            return true;
        }

        private static Dictionary<FieldInfo, object> CaptureAutoFields(JjcModel model)
        {
            var values = new Dictionary<FieldInfo, object>();
            foreach (FieldInfo field in typeof(JjcModel).GetFields(IF))
                if (field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                    values[field] = field.GetValue(model);
            return values;
        }

        private static void RestoreAutoFields(JjcModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values) pair.Key.SetValue(model, pair.Value);
        }

        private static bool SameAutoFields(JjcModel model, Dictionary<FieldInfo, object> values)
        {
            foreach (KeyValuePair<FieldInfo, object> pair in values)
                if (!Equals(pair.Key.GetValue(model), pair.Value)) return false;
            return true;
        }

        private static void RestoreHandlers(IDictionary handlers, Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return;
            for (int i = 0; i < AllCommands.Length; i++)
            {
                int command = AllCommands[i];
                if (handlers.Contains(command)) handlers.Remove(command);
                if (oldHandlers.TryGetValue(command, out object handler)) handlers[command] = handler;
            }
        }

        private static bool SameHandlers(IDictionary handlers, Dictionary<int, object> oldHandlers)
        {
            if (handlers == null) return oldHandlers.Count == 0;
            for (int i = 0; i < AllCommands.Length; i++)
            {
                int command = AllCommands[i];
                bool existed = oldHandlers.TryGetValue(command, out object oldHandler);
                if (handlers.Contains(command) != existed
                    || (existed && !ReferenceEquals(handlers[command], oldHandler))) return false;
            }
            return true;
        }

        private static bool SameValues(IReadOnlyList<int> actual, IReadOnlyList<int> expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static bool SameRefs<T>(IReadOnlyList<T> actual, IReadOnlyList<T> expected) where T : class
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (!ReferenceEquals(actual[i], expected[i])) return false;
            return true;
        }

        private static void Check(ref bool pass, string name, bool ok)
        {
            Debug.Log("CLIVERIFY jjc-read-continuation " + name + " ok=" + ok);
            pass &= ok;
        }
    }
}
