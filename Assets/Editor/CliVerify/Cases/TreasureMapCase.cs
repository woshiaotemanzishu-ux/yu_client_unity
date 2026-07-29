using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.TreasureMap;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class TreasureMapCase
    {
        private const BindingFlags I = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY treasure-map EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            TreasureMapController controller = TreasureMapController.Instance;
            TreasureMapModel model = TreasureMapModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            bool oldHasDrawLog = model.HasDrawLog;
            var oldLogs = new List<TreasureMapModel.DrawLogEntry>(model.DrawLogs);
            bool oldHasError = model.HasError;
            uint oldErrorCode = model.LastErrorCode;
            FieldInfo intercept = typeof(TreasureMapController).GetField("s_outboundIntercept", S);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 20300; id <= 20304; id++) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                for (int id = 20300; id <= 20304; id++) handlers?.Remove(id);
                model.Reset();

                var frames = new List<byte[]>();
                intercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add((byte[])frame.Clone());
                    return true;
                }));
                controller.Init();

                MethodInfo on20300 = typeof(TreasureMapController).GetMethod("On20300", I);
                MethodInfo on20303 = typeof(TreasureMapController).GetMethod("On20303", I);
                pass = intercept != null && handlers != null && on20300 != null && on20303 != null
                    && handlers.Contains(20300) && !handlers.Contains(20301) && !handlers.Contains(20302)
                    && handlers.Contains(20303) && !handlers.Contains(20304) && OnlyDrawLogRequest();
                Check(ref pass, "seams/register-20300-20303-only", pass);

                object firstErrorHandler = handlers?[20300];
                object firstLogHandler = handlers?[20303];
                controller.Init();
                Check(ref pass, "init-idempotent", firstErrorHandler != null && firstLogHandler != null
                    && ReferenceEquals(firstErrorHandler, handlers?[20300])
                    && ReferenceEquals(firstLogHandler, handlers?[20303]));

                controller.RequestDrawLog();
                Check(ref pass, "request-exact-six-byte-empty-frame",
                    ExactFrames(frames, Proto.TREASURE_MAP_DRAW_LOG) && !model.HasDrawLog && !model.HasError);
                frames.Clear();

                Feed(on20300, controller, new CliVerify.Pkt().I(0).Bytes(), out NetReader zeroErrorReader);
                Check(ref pass, "error-zero-loaded/read-tail", zeroErrorReader.Remaining == 0
                    && model.HasError && model.LastErrorCode == 0 && !model.HasDrawLog && frames.Count == 0);
                Feed(on20300, controller, new CliVerify.Pkt().I(uint.MaxValue).Bytes(), out NetReader maxErrorReader);
                Check(ref pass, "error-max-overwrites", maxErrorReader.Remaining == 0
                    && model.HasError && model.LastErrorCode == uint.MaxValue && !model.HasDrawLog && frames.Count == 0);

                Feed(on20303, controller, Packet(new LogSpec[0]), out NetReader emptyReader);
                Check(ref pass, "empty-log-loaded/error-isolated", emptyReader.Remaining == 0
                    && model.HasDrawLog && model.DrawLogs.Count == 0
                    && model.HasError && model.LastErrorCode == uint.MaxValue && frames.Count == 0);

                var many = new[]
                {
                    new LogSpec(uint.MaxValue, -1L, "中文", new[]
                    {
                        new RewardSpec(byte.MaxValue, uint.MaxValue, uint.MaxValue),
                        new RewardSpec(0, 0, 0),
                        new RewardSpec(byte.MaxValue, uint.MaxValue, uint.MaxValue),
                    }),
                    new LogSpec(0, -1L, string.Empty, new RewardSpec[0]),
                };
                Feed(on20303, controller, Packet(many), out NetReader manyReader);
                TreasureMapModel.DrawLogEntry first = model.DrawLogs.Count > 0 ? model.DrawLogs[0] : null;
                TreasureMapModel.DrawLogEntry second = model.DrawLogs.Count > 1 ? model.DrawLogs[1] : null;
                Check(ref pass, "multiple/boundaries/duplicates/order/error-isolated", manyReader.Remaining == 0
                    && model.DrawLogs.Count == 2 && first != null && first.ServerNum == uint.MaxValue
                    && first.RoleId == -1L && first.Name == "中文" && first.Rewards.Count == 3
                    && Eq(first.Rewards[0], many[0].Rewards[0]) && Eq(first.Rewards[1], many[0].Rewards[1])
                    && Eq(first.Rewards[2], many[0].Rewards[2]) && second != null && second.ServerNum == 0
                    && second.RoleId == -1L && second.Name == string.Empty && second.Rewards.Count == 0
                    && model.HasError && model.LastErrorCode == uint.MaxValue && frames.Count == 0);

                TreasureMapModel.DrawLogEntry stableFirst = first;
                Feed(on20300, controller, new CliVerify.Pkt().I(1012).Bytes(), out NetReader nextErrorReader);
                Check(ref pass, "error-overwrite-preserves-log", nextErrorReader.Remaining == 0
                    && model.LastErrorCode == 1012 && model.DrawLogs.Count == 2
                    && ReferenceEquals(stableFirst, model.DrawLogs[0]));

                controller.RequestDrawLog();
                Check(ref pass, "no-response-preserves-both-slices", ExactFrames(frames, Proto.TREASURE_MAP_DRAW_LOG)
                    && model.DrawLogs.Count == 2 && ReferenceEquals(stableFirst, model.DrawLogs[0])
                    && model.HasError && model.LastErrorCode == 1012);
                frames.Clear();

                var one = new[] { new LogSpec(1, 2, "one", new[] { new RewardSpec(3, 4, 5) }) };
                Feed(on20303, controller, Packet(one), out NetReader oneReader);
                Check(ref pass, "multiple-to-one-whole-replace", oneReader.Remaining == 0
                    && model.DrawLogs.Count == 1 && model.DrawLogs[0].ServerNum == 1
                    && model.DrawLogs[0].RoleId == 2 && model.DrawLogs[0].Name == "one"
                    && model.DrawLogs[0].Rewards.Count == 1 && Eq(model.DrawLogs[0].Rewards[0], one[0].Rewards[0])
                    && model.LastErrorCode == 1012 && frames.Count == 0);

                Feed(on20303, controller, Packet(new LogSpec[0]), out NetReader clearReader);
                Check(ref pass, "one-to-empty-clears-only-log", clearReader.Remaining == 0
                    && model.HasDrawLog && model.DrawLogs.Count == 0 && model.HasError
                    && model.LastErrorCode == 1012 && frames.Count == 0);

                controller.Dispose();
                Check(ref pass, "dispose-unregisters-and-resets", !controller.IsInitialized
                    && !model.HasDrawLog && model.DrawLogs.Count == 0 && !model.HasError && model.LastErrorCode == 0
                    && !handlers.Contains(20300) && !handlers.Contains(20303));
                Debug.Log("CLIVERIFY treasure-map VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (oldHasDrawLog) model.ReplaceDrawLog(oldLogs);
                SetProperty(model, "HasError", oldHasError);
                SetProperty(model, "LastErrorCode", oldErrorCode);
                intercept?.SetValue(null, oldIntercept);
                if (wasInitialized) controller.Init();
                for (int id = 20300; id <= 20304; id++) RestoreHandler(handlers, savedHandlers[id], id);

                restored = controller.IsInitialized == wasInitialized && model.HasDrawLog == oldHasDrawLog
                    && SequenceReferenceEqual(model.DrawLogs, oldLogs)
                    && model.HasError == oldHasError && model.LastErrorCode == oldErrorCode
                    && ReferenceEquals(intercept?.GetValue(null), oldIntercept);
                for (int id = 20300; id <= 20304; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY treasure-map restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static bool OnlyDrawLogRequest()
        {
            int count = 0;
            foreach (MethodInfo method in typeof(TreasureMapController)
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) < 0
                    && method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) < 0) continue;
                count++;
                if (method.Name != nameof(TreasureMapController.RequestDrawLog)
                    || method.GetParameters().Length != 0) return false;
            }
            return count == 1;
        }

        private static void Feed(MethodInfo method, TreasureMapController controller, byte[] bytes, out NetReader reader)
        {
            reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
        }

        private static void Check(ref bool pass, string tag, bool ok)
        {
            Debug.Log("CLIVERIFY treasure-map " + tag + " ok=" + ok);
            if (!ok) pass = false;
        }

        private static bool ExactFrames(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] f = frames[i];
                if (f == null || f.Length != 6 || f[0] != 0 || f[1] != 6 || f[2] != 3 || f[3] != 232
                    || f[4] != (byte)(ids[i] >> 8) || f[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private static byte[] Packet(LogSpec[] logs)
        {
            var packet = new CliVerify.Pkt().H(logs.Length);
            foreach (LogSpec log in logs)
            {
                packet.I(log.ServerNum).L(log.RoleId).S(log.Name).H(log.Rewards.Length);
                foreach (RewardSpec reward in log.Rewards)
                    packet.C(reward.Style).I(reward.TypeId).I(reward.Count);
            }
            return packet.Bytes();
        }

        private static bool Eq(TreasureMapModel.RewardEntry actual, RewardSpec expected)
        {
            return actual.Style == expected.Style && actual.TypeId == expected.TypeId && actual.Count == expected.Count;
        }

        private static bool SequenceReferenceEqual(IReadOnlyList<TreasureMapModel.DrawLogEntry> current,
            IList<TreasureMapModel.DrawLogEntry> saved)
        {
            if (current.Count != saved.Count) return false;
            for (int i = 0; i < current.Count; i++)
                if (!ReferenceEquals(current[i], saved[i])) return false;
            return true;
        }

        private static void SetProperty(object target, string property, object value)
        {
            target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);
        }

        private static void SaveHandler(IDictionary handlers, IDictionary<int, HandlerState> saved, int id)
        {
            bool exists = handlers != null && handlers.Contains(id);
            saved[id] = new HandlerState { Exists = exists, Value = exists ? handlers[id] : null };
        }

        private static void RestoreHandler(IDictionary handlers, HandlerState saved, int id)
        {
            if (handlers == null) return;
            if (saved.Exists) handlers[id] = saved.Value;
            else handlers.Remove(id);
        }

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id)
        {
            return handlers != null && handlers.Contains(id) == saved.Exists
                && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
        }

        private struct RewardSpec
        {
            public readonly byte Style;
            public readonly uint TypeId;
            public readonly uint Count;
            public RewardSpec(byte style, uint typeId, uint count)
            {
                Style = style;
                TypeId = typeId;
                Count = count;
            }
        }

        private struct LogSpec
        {
            public readonly uint ServerNum;
            public readonly long RoleId;
            public readonly string Name;
            public readonly RewardSpec[] Rewards;
            public LogSpec(uint serverNum, long roleId, string name, RewardSpec[] rewards)
            {
                ServerNum = serverNum;
                RoleId = roleId;
                Name = name;
                Rewards = rewards;
            }
        }
    }
}
