using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.AutoBrush;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    public static class AutoBrushReadCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

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
                Debug.LogError("CLIVERIFY autobrushread EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            AutoBrushController controller = AutoBrushController.Instance;
            AutoBrushModel model = AutoBrushModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var old = CaptureModel(model);
            FieldInfo retryField = typeof(AutoBrushController).GetField("_exitRetryCount", F);
            int oldRetry = retryField == null ? 0 : (int)retryField.GetValue(controller);
            FieldInfo intercept = typeof(AutoBrushController).GetField("s_startupOutboundIntercept", SF);
            object oldIntercept = intercept?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 13300; id <= 13324; id++) SaveHandler(handlers, savedHandlers, id);
            SaveHandler(handlers, savedHandlers, 61002);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                if (handlers != null)
                {
                    for (int id = 13300; id <= 13324; id++) handlers.Remove(id);
                    handlers.Remove(61002);
                }

                controller.Init();
                MethodInfo on09 = Handler("On13309");
                MethodInfo on23 = Handler("On13323");
                MethodInfo on24 = Handler("On13324");
                MethodInfo onStart = Handler("OnGameStart");
                int[] expected = { 13300, 13301, 13305, 13306, 13307, 13309, 13323, 13324 };
                pass = handlers != null && intercept != null && retryField != null
                    && on09 != null && on23 != null && on24 != null && onStart != null
                    && ExactRegistrations(handlers, expected) && handlers.Contains(61002);

                var firstHandlers = new Dictionary<int, object>();
                foreach (int id in expected) firstHandlers[id] = handlers[id];
                firstHandlers[61002] = handlers[61002];
                controller.Init();
                foreach (KeyValuePair<int, object> pair in firstHandlers)
                    pass &= ReferenceEquals(handlers[pair.Key], pair.Value);

                SeedModel(model);
                var frames = new List<byte[]>();
                intercept.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                onStart.Invoke(controller, null);
                pass &= FramesEqual(frames, 13300, 13301, 13309, 13323, 13324) && IsReset(model);

                var brush = new AutoBrushModel.BrushStrangeInfo
                {
                    Code = 7, CurrentTimes = 8, NeedTimes = 9, AssistId = 10, AssisterId = 11,
                };
                model.SetBrushStrangeInfo(brush);
                model.SetRankInfo(1, 2, 3, "rank", 4);

                pass &= Feed(on09, controller, new CliVerify.Pkt().I(uint.MaxValue).L(-1))
                    && model.HasNextStageReward && model.NextStageRewardCode == uint.MaxValue
                    && model.NextStageRewardGate == ulong.MaxValue
                    && ReferenceEquals(model.BrushInfo, brush) && model.TopRankName == "rank";
                pass &= Feed(on23, controller, new CliVerify.Pkt().C(byte.MaxValue))
                    && model.HasTutorialNode && model.TutorialNode == byte.MaxValue
                    && model.NextStageRewardGate == ulong.MaxValue;
                pass &= Feed(on24, controller, new CliVerify.Pkt().H(ushort.MaxValue).I(uint.MaxValue))
                    && model.HasAssistInfo && model.AssistDailyCount == ushort.MaxValue
                    && model.AssistNextTime == uint.MaxValue && model.TutorialNode == byte.MaxValue;

                pass &= Feed(on09, controller, new CliVerify.Pkt().I(0).L(0))
                    && model.HasNextStageReward && model.NextStageRewardCode == 0 && model.NextStageRewardGate == 0
                    && model.HasTutorialNode && model.HasAssistInfo;
                pass &= Feed(on23, controller, new CliVerify.Pkt().C(0))
                    && model.HasTutorialNode && model.TutorialNode == 0 && model.HasAssistInfo;
                pass &= Feed(on24, controller, new CliVerify.Pkt().H(0).I(0))
                    && model.HasAssistInfo && model.AssistDailyCount == 0 && model.AssistNextTime == 0
                    && ReferenceEquals(model.BrushInfo, brush) && model.Level == 3;

                controller.Dispose();
                pass &= !controller.IsInitialized && IsReset(model);
                for (int id = 13300; id <= 13324; id++) pass &= !handlers.Contains(id);
                pass &= !handlers.Contains(61002);
                Debug.Log("CLIVERIFY autobrushread VERDICT pass=" + pass);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, old);
                if (retryField != null) retryField.SetValue(controller, oldRetry);
                if (intercept != null) intercept.SetValue(null, oldIntercept);
                if (wasInitialized) controller.Init();
                for (int id = 13300; id <= 13324; id++) RestoreHandler(handlers, savedHandlers[id], id);
                RestoreHandler(handlers, savedHandlers[61002], 61002);

                restored = ReferenceEquals(AutoBrushController.Instance, controller)
                    && ReferenceEquals(AutoBrushModel.Instance, model)
                    && controller.IsInitialized == wasInitialized
                    && (retryField == null || (int)retryField.GetValue(controller) == oldRetry)
                    && ModelMatches(model, old)
                    && HandlerMatches(handlers, savedHandlers[61002], 61002);
                for (int id = 13300; id <= 13324; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY autobrushread restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static Dictionary<string, object> CaptureModel(AutoBrushModel model) => new Dictionary<string, object>
        {
            ["BrushInfo"] = model.BrushInfo,
            ["AutoBrushState"] = model.AutoBrushState,
            ["Level"] = model.Level,
            ["RoleRank"] = model.RoleRank,
            ["RankType"] = model.RankType,
            ["TopRankName"] = model.TopRankName,
            ["TopRankLevel"] = model.TopRankLevel,
            ["MaxLevel"] = model.MaxLevel,
            ["FailureState"] = model.FailureState,
            ["LastFailureLevel"] = model.LastFailureLevel,
            ["HasNextStageReward"] = model.HasNextStageReward,
            ["NextStageRewardCode"] = model.NextStageRewardCode,
            ["NextStageRewardGate"] = model.NextStageRewardGate,
            ["HasTutorialNode"] = model.HasTutorialNode,
            ["TutorialNode"] = model.TutorialNode,
            ["HasAssistInfo"] = model.HasAssistInfo,
            ["AssistDailyCount"] = model.AssistDailyCount,
            ["AssistNextTime"] = model.AssistNextTime,
        };

        private static void SeedModel(AutoBrushModel model)
        {
            model.SetBrushStrangeInfo(new AutoBrushModel.BrushStrangeInfo
                { Code = 1, CurrentTimes = 2, NeedTimes = 3, AssistId = 4, AssisterId = 5 });
            model.SetAutoBrushStrangeState(true);
            model.SetRankInfo(6, 7, 8, "seed", 9);
            model.SetMaxLevel(10);
            model.SetFailureState(true, 11);
            model.ReplaceNextStageReward(12, 13);
            model.ReplaceTutorialNode(14);
            model.ReplaceAssistInfo(15, 16);
        }

        private static bool IsReset(AutoBrushModel model) => model.BrushInfo == null
            && !model.AutoBrushState && model.Level == 0 && model.RoleRank == 0 && model.RankType == 0
            && model.TopRankName == "" && model.TopRankLevel == 0 && model.MaxLevel == 0
            && !model.FailureState && model.LastFailureLevel == 0
            && !model.HasNextStageReward && model.NextStageRewardCode == 0 && model.NextStageRewardGate == 0
            && !model.HasTutorialNode && model.TutorialNode == 0
            && !model.HasAssistInfo && model.AssistDailyCount == 0 && model.AssistNextTime == 0;

        private static MethodInfo Handler(string name) => typeof(AutoBrushController).GetMethod(name, F);

        private static bool Feed(MethodInfo handler, AutoBrushController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExactRegistrations(IDictionary handlers, IReadOnlyList<int> expected)
        {
            var set = new HashSet<int>(expected);
            for (int id = 13300; id <= 13324; id++)
                if (handlers.Contains(id) != set.Contains(id)) return false;
            return true;
        }

        private static bool FramesEqual(IReadOnlyList<byte[]> frames, params int[] ids)
        {
            if (frames.Count != ids.Length) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                byte[] frame = frames[i];
                if (frame == null || frame.Length != 6 || frame[0] != 0 || frame[1] != 6
                    || frame[2] != 3 || frame[3] != 232 || frame[4] != (byte)(ids[i] >> 8)
                    || frame[5] != (byte)ids[i]) return false;
            }
            return true;
        }

        private static void RestoreModel(AutoBrushModel model, IDictionary<string, object> old)
        {
            foreach (KeyValuePair<string, object> pair in old)
                typeof(AutoBrushModel).GetProperty(pair.Key)?.SetValue(model, pair.Value);
        }

        private static bool ModelMatches(AutoBrushModel model, IDictionary<string, object> old)
        {
            foreach (KeyValuePair<string, object> pair in old)
                if (!Equals(typeof(AutoBrushModel).GetProperty(pair.Key)?.GetValue(model), pair.Value)) return false;
            return true;
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

        private static bool HandlerMatches(IDictionary handlers, HandlerState saved, int id) =>
            handlers != null && handlers.Contains(id) == saved.Exists
            && (!saved.Exists || ReferenceEquals(handlers[id], saved.Value));
    }
}
