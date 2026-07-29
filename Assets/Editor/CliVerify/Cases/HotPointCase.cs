using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.HotPoint;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>R474：33300/33302/33303/33305/33306 线格式、隔离快照、增量合并与环境恢复。</summary>
    public static class HotPointCase
    {
        private const BindingFlags I = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        private sealed class Ambient
        {
            public readonly bool Initialized;
            public readonly bool HasActivities;
            public readonly List<HotPointModel.ActivityInfo> Activities;
            public readonly Dictionary<uint, object> Details;
            public readonly Dictionary<uint, object> Rewards;
            public readonly Dictionary<uint, object> Progress;
            public readonly bool HasError;
            public readonly uint ErrorCode;
            public readonly object Intercept;

            public Ambient(HotPointController controller, HotPointModel model, FieldInfo intercept)
            {
                Initialized = controller.IsInitialized;
                HasActivities = model.HasActivities;
                Activities = new List<HotPointModel.ActivityInfo>(model.Activities);
                Details = CloneMap(model, "_details");
                Rewards = CloneMap(model, "_rewards");
                Progress = CloneMap(model, "_progress");
                HasError = model.HasError;
                ErrorCode = model.LastErrorCode;
                Intercept = intercept?.GetValue(null);
            }

            public void Restore(HotPointController controller, HotPointModel model, FieldInfo intercept)
            {
                if (controller.IsInitialized) controller.Dispose();
                model.Reset();
                if (HasActivities) model.ReplaceActivities(Activities);
                RestoreMap(model, "_details", Details);
                RestoreMap(model, "_rewards", Rewards);
                RestoreMap(model, "_progress", Progress);
                SetProperty(model, "HasError", HasError);
                SetProperty(model, "LastErrorCode", ErrorCode);
                intercept?.SetValue(null, Intercept);
                if (Initialized) controller.Init();
            }

            public bool Matches(HotPointController controller, HotPointModel model, FieldInfo intercept)
            {
                return controller.IsInitialized == Initialized
                    && model.HasActivities == HasActivities
                    && SequenceReferenceEqual(model.Activities, Activities)
                    && MapReferenceEqual(model, "_details", Details)
                    && MapReferenceEqual(model, "_rewards", Rewards)
                    && MapReferenceEqual(model, "_progress", Progress)
                    && model.HasError == HasError && model.LastErrorCode == ErrorCode
                    && ReferenceEquals(intercept?.GetValue(null), Intercept);
            }
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY hotpoint EXCEPTION " + e);
                return Task.FromResult(3);
            }
        }

        private static int RunSync()
        {
            HotPointController controller = HotPointController.Instance;
            HotPointModel model = HotPointModel.Instance;
            FieldInfo intercept = typeof(HotPointController).GetField("s_outboundIntercept", S);
            var ambient = new Ambient(controller, model, intercept);
            var handlers = typeof(NetManager).GetField("_handlers", S)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            for (int id = 33300; id <= 33306; id++) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                if (controller.IsInitialized) controller.Dispose();
                for (int id = 33300; id <= 33306; id++) handlers?.Remove(id);
                model.Reset();

                var frames = new List<byte[]>();
                Func<byte[], bool> capture = frame => { frames.Add((byte[])frame.Clone()); return true; };
                intercept?.SetValue(null, capture);
                controller.Init();

                MethodInfo on33300 = typeof(HotPointController).GetMethod("On33300", I);
                MethodInfo on33302 = typeof(HotPointController).GetMethod("On33302", I);
                MethodInfo on33303 = typeof(HotPointController).GetMethod("On33303", I);
                MethodInfo on33305 = typeof(HotPointController).GetMethod("On33305", I);
                MethodInfo on33306 = typeof(HotPointController).GetMethod("On33306", I);
                pass = handlers != null && intercept != null && on33300 != null && on33302 != null
                    && on33303 != null && on33305 != null && on33306 != null
                    && handlers.Contains(33300) && !handlers.Contains(33301) && handlers.Contains(33302)
                    && handlers.Contains(33303) && !handlers.Contains(33304) && handlers.Contains(33305)
                    && handlers.Contains(33306) && ExactPublicRequests();

                object first33300 = handlers?[33300];
                controller.Init();
                pass &= first33300 != null && ReferenceEquals(first33300, handlers?[33300]);

                controller.RequestActivityList();
                controller.RequestActivityDetail(ushort.MaxValue, 0x1234);
                controller.RequestRewardStatus(0x4321, ushort.MaxValue);
                pass &= frames.Count == 3
                    && ExactFrame(frames[0], Proto.HI_POINT_ACTIVITY_LIST)
                    && ExactKeyFrame(frames[1], Proto.HI_POINT_DETAIL, ushort.MaxValue, 0x1234)
                    && ExactKeyFrame(frames[2], Proto.HI_POINT_REWARD_STATUS, 0x4321, ushort.MaxValue);

                // 33300：完整替换，保留 wire 顺序、重复键、UTF-8 与无符号极值。
                pass &= Feed(on33300, controller, new CliVerify.Pkt().H(3)
                    .H(333).H(7).S("嗨点甲").I(0).I(uint.MaxValue).I(uint.MaxValue)
                    .H(333).H(7).S("重复").I(2).I(3).I(4)
                    .H(ushort.MaxValue).H(ushort.MaxValue).S(string.Empty).I(uint.MaxValue).I(0).I(5));
                IReadOnlyList<HotPointModel.ActivityInfo> oldActivities = model.Activities;
                pass &= model.HasActivities && oldActivities.Count == 3
                    && oldActivities[0].Name == "嗨点甲" && oldActivities[0].EndTime == uint.MaxValue
                    && oldActivities[1].BaseType == 333 && oldActivities[1].SubType == 7
                    && oldActivities[2].BaseType == ushort.MaxValue && oldActivities[2].StartTime == uint.MaxValue;

                // 33302：同键完整替换、异键共存；所有字段按 wire 宽度落地。
                pass &= Feed(on33302, controller, DetailPacket(333, 7, uint.MaxValue, true));
                pass &= model.TryGetDetail(333, 7, out HotPointModel.DetailSnapshot detailBefore)
                    && DetailMatches(detailBefore, uint.MaxValue, 3);
                pass &= Feed(on33302, controller, new CliVerify.Pkt().H(333).H(8).I(8).H(0));
                pass &= model.DetailCount == 2 && model.TryGetDetail(333, 8, out HotPointModel.DetailSnapshot secondDetail)
                    && secondDetail.Modules.Count == 0;

                // 显式请求本身无回包时不得清缓存。
                int requestCount = frames.Count;
                controller.RequestActivityDetail(333, 7);
                pass &= frames.Count == requestCount + 1 && ReferenceEquals(detailBefore, GetDetail(model, 333, 7));

                // 33303：按复合键完整替换，重复 grade 与空表均为已加载快照。
                pass &= Feed(on33303, controller, RewardPacket(333, 7));
                pass &= model.TryGetReward(333, 7, out HotPointModel.RewardSnapshot rewardBefore)
                    && RewardMatches(rewardBefore);
                pass &= Feed(on33303, controller, new CliVerify.Pkt().H(333).H(8).H(0));
                pass &= model.RewardCount == 2 && model.TryGetReward(333, 8, out HotPointModel.RewardSnapshot emptyReward)
                    && emptyReward.Rewards.Count == 0;
                controller.RequestRewardStatus(333, 7);
                pass &= ReferenceEquals(rewardBefore, GetReward(model, 333, 7));

                // 33305：保存原始增量；按 module/sub/condition/Detail.Description==delta.Name 合并。
                int beforePushFrames = frames.Count;
                HotPointModel.DetailSnapshot stableDetail = GetDetail(model, 333, 7);
                pass &= Feed(on33305, controller, ProgressPacket(333, 7));
                HotPointModel.DetailSnapshot merged = GetDetail(model, 333, 7);
                pass &= frames.Count == beforePushFrames + 1
                    && ExactKeyFrame(frames[frames.Count - 1], Proto.HI_POINT_REWARD_STATUS, 333, 7)
                    && model.TryGetProgress(333, 7, out HotPointModel.ProgressSnapshot progress)
                    && ProgressMatches(progress)
                    && merged != null && merged.SumPoints == 77 && merged.Modules.Count == 3
                    && merged.Modules[0].ProgressValue == 99 && merged.Modules[0].IsComplete == 2
                    && merged.Modules[1].ProgressValue == 99 && merged.Modules[1].IsComplete == 2
                    && merged.Modules[2].ProgressValue == 9 && merged.Modules[2].IsComplete == 0
                    && stableDetail.SumPoints == uint.MaxValue && stableDetail.Modules[0].ProgressValue == ulong.MaxValue;

                // 未加载明细的键只存 raw 增量，不凭空造 33302；仍只追发同键 33303。
                int detailsBeforeUnknown = model.DetailCount;
                pass &= Feed(on33305, controller, new CliVerify.Pkt().H(333).H(99).I(0).H(0));
                pass &= model.DetailCount == detailsBeforeUnknown && !model.TryGetDetail(333, 99, out _)
                    && model.TryGetProgress(333, 99, out HotPointModel.ProgressSnapshot emptyProgress)
                    && emptyProgress.SumPoints == 0 && emptyProgress.Modules.Count == 0
                    && ExactKeyFrame(frames[frames.Count - 1], Proto.HI_POINT_REWARD_STATUS, 333, 99);

                // 33300 空表只清自己的列表；33306 含 0/最大值全量覆盖且不串改其他切片。
                pass &= Feed(on33306, controller, new CliVerify.Pkt().I(0)) && model.HasError && model.LastErrorCode == 0;
                pass &= Feed(on33306, controller, new CliVerify.Pkt().I(uint.MaxValue))
                    && model.LastErrorCode == uint.MaxValue;
                HotPointModel.DetailSnapshot detailBeforeListClear = GetDetail(model, 333, 7);
                pass &= Feed(on33300, controller, new CliVerify.Pkt().H(0))
                    && model.HasActivities && model.Activities.Count == 0
                    && oldActivities.Count == 3 && ReferenceEquals(detailBeforeListClear, GetDetail(model, 333, 7))
                    && ReferenceEquals(rewardBefore, GetReward(model, 333, 7)) && model.ProgressCount == 2
                    && model.HasError && model.LastErrorCode == uint.MaxValue;

                // 同键空明细是有效加载替换，旧不可变快照不被回写。
                pass &= Feed(on33302, controller, new CliVerify.Pkt().H(333).H(7).I(0).H(0))
                    && GetDetail(model, 333, 7).Modules.Count == 0 && detailBeforeListClear.Modules.Count == 3;

                controller.Dispose();
                pass &= !controller.IsInitialized && !model.HasActivities && model.Activities.Count == 0
                    && model.DetailCount == 0 && model.RewardCount == 0 && model.ProgressCount == 0
                    && !model.HasError && model.LastErrorCode == 0
                    && !handlers.Contains(33300) && !handlers.Contains(33302) && !handlers.Contains(33303)
                    && !handlers.Contains(33305) && !handlers.Contains(33306);

                Debug.Log("CLIVERIFY hotpoint VERDICT pass=" + pass);
            }
            finally
            {
                ambient.Restore(controller, model, intercept);
                for (int id = 33300; id <= 33306; id++) RestoreHandler(handlers, savedHandlers[id], id);
                restored = ambient.Matches(controller, model, intercept);
                for (int id = 33300; id <= 33306; id++)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY hotpoint restored=" + restored);
            }

            return pass && restored ? 0 : 3;
        }

        private static CliVerify.Pkt DetailPacket(ushort baseType, ushort subType, uint sumPoints, bool includeRows)
        {
            var p = new CliVerify.Pkt().H(baseType).H(subType).I(sumPoints).H(includeRows ? 3 : 0);
            if (!includeRows) return p;
            AddDetail(p, 10, 20, "kill", "模块甲", ushort.MaxValue, 2, uint.MaxValue, "图标",
                ulong.MaxValue, ushort.MaxValue, uint.MaxValue, 88, "条件甲", 1);
            AddDetail(p, 10, 20, "kill", "重复模块", 3, 4, 5, "", 8, 9, 10, 11, "条件甲", 0);
            AddDetail(p, 10, 20, "kill", "不匹配", 6, 7, 8, "x", 9, 10, 11, 12, "条件乙", 0);
            return p;
        }

        private static void AddDetail(CliVerify.Pkt p, uint moduleId, uint subId, string conditionType, string name,
            ushort orderId, ushort jumpId, uint secondary, string iconType, ulong progress, ushort isProgress,
            uint conditionValue, uint rewardPoint, string description, ushort isComplete)
        {
            p.I(moduleId).I(subId).S(conditionType).S(name).H(orderId).H(jumpId).I(secondary).S(iconType)
                .L(unchecked((long)progress)).H(isProgress).I(conditionValue).I(rewardPoint).S(description).H(isComplete);
        }

        private static CliVerify.Pkt RewardPacket(ushort baseType, ushort subType)
        {
            return new CliVerify.Pkt().H(baseType).H(subType).H(2)
                .H(ushort.MaxValue).C(byte.MaxValue).C(0).H(ushort.MaxValue).S("奖甲").S("说明甲").S("条件").S("奖励")
                .H(ushort.MaxValue).C(1).C(byte.MaxValue).H(2).S("重复").S(string.Empty).S("[]").S("{} ");
        }

        private static CliVerify.Pkt ProgressPacket(ushort baseType, ushort subType)
        {
            return new CliVerify.Pkt().H(baseType).H(subType).I(77).H(3)
                .I(10).I(20).S("kill").S("条件甲").L(50).H(1)
                .I(10).I(20).S("kill").S("条件甲").L(99).H(2)
                .I(uint.MaxValue).I(uint.MaxValue).S("x").S("未命中").L(unchecked((long)ulong.MaxValue)).H(ushort.MaxValue);
        }

        private static bool DetailMatches(HotPointModel.DetailSnapshot s, uint sumPoints, int count)
        {
            return s != null && s.BaseType == 333 && s.SubType == 7 && s.SumPoints == sumPoints
                && s.Modules.Count == count && s.Modules[0].ModuleId == 10 && s.Modules[0].SubId == 20
                && s.Modules[0].ConditionType == "kill" && s.Modules[0].Name == "模块甲"
                && s.Modules[0].OrderId == ushort.MaxValue && s.Modules[0].JumpId == 2
                && s.Modules[0].SecondaryValue == uint.MaxValue && s.Modules[0].IconType == "图标"
                && s.Modules[0].ProgressValue == ulong.MaxValue && s.Modules[0].IsProgress == ushort.MaxValue
                && s.Modules[0].ConditionValue == uint.MaxValue && s.Modules[0].RewardPoint == 88
                && s.Modules[0].Description == "条件甲" && s.Modules[0].IsComplete == 1;
        }

        private static bool RewardMatches(HotPointModel.RewardSnapshot s)
        {
            return s != null && s.BaseType == 333 && s.SubType == 7 && s.Rewards.Count == 2
                && s.Rewards[0].Grade == ushort.MaxValue && s.Rewards[0].FormType == byte.MaxValue
                && s.Rewards[0].Status == 0 && s.Rewards[0].ReceiveTimes == ushort.MaxValue
                && s.Rewards[0].Name == "奖甲" && s.Rewards[0].Description == "说明甲"
                && s.Rewards[1].Grade == ushort.MaxValue && s.Rewards[1].Status == byte.MaxValue;
        }

        private static bool ProgressMatches(HotPointModel.ProgressSnapshot s)
        {
            return s != null && s.BaseType == 333 && s.SubType == 7 && s.SumPoints == 77 && s.Modules.Count == 3
                && s.Modules[0].Name == "条件甲" && s.Modules[0].ProgressValue == 50 && s.Modules[0].IsComplete == 1
                && s.Modules[1].ProgressValue == 99 && s.Modules[1].IsComplete == 2
                && s.Modules[2].ModuleId == uint.MaxValue && s.Modules[2].ProgressValue == ulong.MaxValue;
        }

        private static HotPointModel.DetailSnapshot GetDetail(HotPointModel model, ushort baseType, ushort subType)
        {
            model.TryGetDetail(baseType, subType, out HotPointModel.DetailSnapshot value);
            return value;
        }

        private static HotPointModel.RewardSnapshot GetReward(HotPointModel model, ushort baseType, ushort subType)
        {
            model.TryGetReward(baseType, subType, out HotPointModel.RewardSnapshot value);
            return value;
        }

        private static bool Feed(MethodInfo method, HotPointController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool ExactFrame(byte[] frame, int protocol)
        {
            return frame != null && frame.Length == 6 && frame[0] == 0 && frame[1] == 6
                && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(protocol >> 8) && frame[5] == (byte)protocol;
        }

        private static bool ExactKeyFrame(byte[] frame, int protocol, ushort baseType, ushort subType)
        {
            return frame != null && frame.Length == 10 && frame[0] == 0 && frame[1] == 10
                && frame[2] == 3 && frame[3] == 0xE8
                && frame[4] == (byte)(protocol >> 8) && frame[5] == (byte)protocol
                && frame[6] == (byte)(baseType >> 8) && frame[7] == (byte)baseType
                && frame[8] == (byte)(subType >> 8) && frame[9] == (byte)subType;
        }

        private static bool ExactPublicRequests()
        {
            var names = new HashSet<string>();
            foreach (MethodInfo method in typeof(HotPointController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.Name.IndexOf("Request", StringComparison.OrdinalIgnoreCase) < 0
                    && method.Name.IndexOf("Send", StringComparison.OrdinalIgnoreCase) < 0) continue;
                names.Add(method.Name);
            }
            return names.SetEquals(new[] { "RequestActivityList", "RequestActivityDetail", "RequestRewardStatus" });
        }

        private static Dictionary<uint, object> CloneMap(HotPointModel model, string fieldName)
        {
            var result = new Dictionary<uint, object>();
            var map = typeof(HotPointModel).GetField(fieldName, I)?.GetValue(model) as IDictionary;
            if (map == null) return result;
            foreach (DictionaryEntry entry in map) result[(uint)entry.Key] = entry.Value;
            return result;
        }

        private static void RestoreMap(HotPointModel model, string fieldName, Dictionary<uint, object> saved)
        {
            var map = typeof(HotPointModel).GetField(fieldName, I)?.GetValue(model) as IDictionary;
            if (map == null) return;
            map.Clear();
            foreach (KeyValuePair<uint, object> pair in saved) map[pair.Key] = pair.Value;
        }

        private static bool MapReferenceEqual(HotPointModel model, string fieldName, Dictionary<uint, object> saved)
        {
            var map = typeof(HotPointModel).GetField(fieldName, I)?.GetValue(model) as IDictionary;
            if (map == null || map.Count != saved.Count) return false;
            foreach (KeyValuePair<uint, object> pair in saved)
                if (!map.Contains(pair.Key) || !ReferenceEquals(map[pair.Key], pair.Value)) return false;
            return true;
        }

        private static bool SequenceReferenceEqual(IReadOnlyList<HotPointModel.ActivityInfo> current,
            IList<HotPointModel.ActivityInfo> saved)
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
    }
}
