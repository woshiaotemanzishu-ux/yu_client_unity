using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.RuneTreasure;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>寻宝416安全读侧十协议的wire、键控、增量、自动重查、启动顺序及生命周期专项。</summary>
    public static class RuneTreasureCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly int[] RegisteredIds =
            { 41600, 41601, 41603, 41608, 41610, 41612, 41613, 41615, 41620, 41621 };
        private static readonly int[] ExcludedIds =
            { 41602, 41604, 41605, 41606, 41607, 41609, 41611, 41614, 41622 };

        private sealed class HandlerState
        {
            public bool Exists;
            public object Value;
        }

        private sealed class DictionaryState
        {
            public readonly List<KeyValuePair<object, object>> Entries =
                new List<KeyValuePair<object, object>>();
        }

        private sealed class ModelState
        {
            public object LastError;
            public object Rune;
            public object LastRecordPush;
            public object LastWeaponNotice;
            public object LastTaskDelta;
            public readonly Dictionary<string, DictionaryState> Dictionaries =
                new Dictionary<string, DictionaryState>();
        }

        public static Task<int> Run()
        {
            try { return Task.FromResult(RunSync()); }
            catch (Exception e) { Debug.LogError("CLIVERIFY runetreasure EXCEPTION " + e); return Task.FromResult(3); }
        }

        private static int RunSync()
        {
            RuneTreasureController controller = RuneTreasureController.Instance;
            RuneTreasureModel model = RuneTreasureModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            ModelState oldModel = CaptureModel(model);
            long oldOpenTime = ServerTimeModel.OpenTime;
            long oldMergeTime = ServerTimeModel.MergeTime;
            long oldMergeStartTime = ServerTimeModel.MergeStartTime;
            int oldMergeCount = ServerTimeModel.MergeCount;
            FieldInfo interceptor = typeof(RuneTreasureController).GetField("s_outboundIntercept", SF);
            object oldInterceptor = interceptor?.GetValue(null);
            var handlers = typeof(NetManager).GetField("_handlers", SF)?.GetValue(null) as IDictionary;
            var savedHandlers = new Dictionary<int, HandlerState>();
            foreach (int id in RegisteredIds) SaveHandler(handlers, savedHandlers, id);
            foreach (int id in ExcludedIds) SaveHandler(handlers, savedHandlers, id);

            bool pass = false;
            bool restored = false;
            try
            {
                controller.Init();
                model.Reset();
                var methods = new Dictionary<int, MethodInfo>();
                foreach (int id in RegisteredIds)
                    methods[id] = typeof(RuneTreasureController).GetMethod("On" + id, F);

                bool a = handlers != null;
                foreach (int id in RegisteredIds)
                    a &= methods[id] != null && handlers.Contains(id);
                foreach (int id in ExcludedIds) a &= !handlers.Contains(id);

                bool b = Invoke(methods[41600], controller,
                        new CliVerify.Pkt().I(uint.MaxValue).Bytes())
                    && model.LastError.Code == uint.MaxValue;
                RuneTreasureModel.ErrorSnapshot error = model.LastError;
                b &= Invoke(methods[41601], controller, new CliVerify.Pkt()
                        .I(uint.MaxValue).H(ushort.MaxValue).H(2)
                        .H(7).H(0).H(7).H(ushort.MaxValue)
                        .L(unchecked((long)ulong.MaxValue)).L(0).Bytes())
                    && model.HasRune && model.Rune.DrawTimes == uint.MaxValue
                    && model.Rune.Turn == ushort.MaxValue
                    && model.Rune.StageRewards.Count == 2
                    && model.Rune.StageRewards[0].Stage == 7
                    && model.Rune.StageRewards[0].Status == 0
                    && model.Rune.StageRewards[1].Stage == 7
                    && model.Rune.StageRewards[1].Status == ushort.MaxValue
                    && model.Rune.StageRefreshTime == ulong.MaxValue && model.Rune.FreeTime == 0
                    && ReferenceEquals(model.LastError, error);

                var frames = new List<byte[]>();
                interceptor?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add(frame);
                    return true;
                }));

                bool c = Invoke(methods[41608], controller, new CliVerify.Pkt()
                        .I(uint.MaxValue).C(2).C(1).C(1).C(byte.MaxValue)
                        .L(unchecked((long)ulong.MaxValue)).H(2)
                        .L(9).S("甲").C(2).I(4000000000L).I(uint.MaxValue).I(10).C(0)
                        .L(unchecked((long)0xFEDCBA9876543210UL)).S("乙").C(2)
                        .I(4000000000L).I(0).I(uint.MaxValue).C(byte.MaxValue).Bytes())
                    && model.TryGetPage(2, 1, out RuneTreasureModel.PageSnapshot page)
                    && page.Score == uint.MaxValue && page.DrawWeapon == 1
                    && page.FreeTimes == byte.MaxValue && page.FreeTime == ulong.MaxValue
                    && page.Records.Count == 2 && page.Records[0].RoleId == 9
                    && page.Records[0].RoleName == "甲" && page.Records[0].GoodsNum == uint.MaxValue
                    && page.Records[1].RoleId == 0xFEDCBA9876543210UL
                    && page.Records[1].IsRare == byte.MaxValue
                    && FramesAre(frames, Frame1(41613, 2));
                frames.Clear();
                c &= Invoke(methods[41608], controller, new CliVerify.Pkt()
                        .I(8).C(2).C(1).C(2).C(0).L(0).H(0).Bytes())
                    && model.TryGetPage(2, 2, out RuneTreasureModel.PageSnapshot personal)
                    && personal.Records.Count == 0 && frames.Count == 0
                    && model.TryGetPage(2, 1, out RuneTreasureModel.PageSnapshot original)
                    && original.Records.Count == 2;
                c &= Invoke(methods[41608], controller, new CliVerify.Pkt()
                        .I(0).C(2).C(0).C(1).C(0).L(0).H(0).Bytes())
                    && model.TryGetPage(2, 1, out RuneTreasureModel.PageSnapshot cleared)
                    && cleared.Records.Count == 0 && cleared.DrawWeapon == 0
                    && FramesAre(frames, Frame1(41613, 2));

                frames.Clear();
                bool d = Invoke(methods[41603], controller, new CliVerify.Pkt()
                        .C(1).L(unchecked((long)ulong.MaxValue)).H(2)
                        .L(1).S("推送甲").C(3).I(11).I(12).I(13).C(1)
                        .L(1).S("推送乙").C(3).I(11).I(12).I(13).C(1).Bytes())
                    && model.LastRecordPush.RecordType == 1
                    && model.LastRecordPush.RoleId == ulong.MaxValue
                    && model.LastRecordPush.Records.Count == 2
                    && FramesAre(frames, Frame2(41608, 3, 1));
                frames.Clear();
                d &= Invoke(methods[41603], controller,
                        new CliVerify.Pkt().C(2).L(0).H(0).Bytes())
                    && model.HasRecordPush && model.LastRecordPush.RecordType == 2
                    && model.LastRecordPush.Records.Count == 0 && frames.Count == 0;

                bool e = Invoke(methods[41610], controller,
                        new CliVerify.Pkt().C(3).I(uint.MaxValue).H(ushort.MaxValue).Bytes())
                    && model.TryGetLucky(3, out RuneTreasureModel.LuckySnapshot lucky)
                    && lucky.Value == uint.MaxValue && lucky.Percent == ushort.MaxValue;
                e &= Invoke(methods[41612], controller, new CliVerify.Pkt().C(3).H(2)
                        .I(uint.MaxValue).I(4000000000L).L(unchecked((long)ulong.MaxValue))
                        .S("跨服甲").C(3).I(uint.MaxValue).H(ushort.MaxValue).I(uint.MaxValue).C(1)
                        .I(uint.MaxValue).I(4000000000L).L(unchecked((long)ulong.MaxValue))
                        .S("跨服乙").C(3).I(uint.MaxValue).H(ushort.MaxValue).I(uint.MaxValue).C(1).Bytes())
                    && model.TryGetCrossRecords(3, out RuneTreasureModel.CrossRecordSnapshot cross)
                    && cross.Records.Count == 2 && cross.Records[0].ServerId == uint.MaxValue
                    && cross.Records[0].ServerNum == 4000000000U
                    && cross.Records[0].GoodsNum == ushort.MaxValue
                    && cross.Records[1].RoleName == "跨服乙";
                e &= Invoke(methods[41612], controller, new CliVerify.Pkt().C(3).H(0).Bytes())
                    && model.TryGetCrossRecords(3, out cross) && cross.Records.Count == 0;
                e &= Invoke(methods[41613], controller, new CliVerify.Pkt().C(3).C(2).Bytes())
                    && model.TryGetOpenState(3, out RuneTreasureModel.OpenStateSnapshot open)
                    && open.RawOpen == 2 && !open.IsOpen;
                frames.Clear();
                e &= Invoke(methods[41615], controller, new CliVerify.Pkt().C(3).Bytes())
                    && model.LastWeaponNotice.HuntType == 3
                    && FramesAre(frames, Frame1(41613, 3));

                RuneTreasureModel.TaskSnapshot tasks = null;
                bool f = Invoke(methods[41620], controller, new CliVerify.Pkt()
                        .I(uint.MaxValue).C(5).H(3)
                        .I(7).I(1).C(0).I(7).I(2).C(1).I(8).I(3).C(2).Bytes())
                    && model.TryGetTasks(5, out tasks)
                    && tasks.Code == uint.MaxValue && tasks.Tasks.Count == 3;
                f &= Invoke(methods[41621], controller, new CliVerify.Pkt().C(5).H(3)
                        .I(7).I(4).C(1).I(99).I(5).C(1).I(7).I(9).C(2).Bytes())
                    && model.TryGetTasks(5, out tasks)
                    && tasks.Tasks.Count == 3
                    && tasks.Tasks[0].TaskId == 7 && tasks.Tasks[0].Num == 9 && tasks.Tasks[0].State == 2
                    && tasks.Tasks[1].TaskId == 7 && tasks.Tasks[1].Num == 9 && tasks.Tasks[1].State == 2
                    && tasks.Tasks[2].TaskId == 8 && tasks.Tasks[2].Num == 3 && tasks.Tasks[2].State == 2
                    && model.LastTaskDelta.Tasks.Count == 3;
                RuneTreasureModel.TaskSnapshot patched = tasks;
                f &= Invoke(methods[41621], controller, new CliVerify.Pkt().C(5).H(0).Bytes())
                    && model.HasTaskDelta && model.LastTaskDelta.Tasks.Count == 0
                    && model.TryGetTasks(5, out tasks) && tasks.Tasks.Count == 3
                    && tasks.Tasks[0].Num == 9 && !ReferenceEquals(tasks, patched);
                f &= Invoke(methods[41621], controller,
                        new CliVerify.Pkt().C(4).H(1).I(1).I(1).C(1).Bytes())
                    && !model.TryGetTasks(4, out _) && model.LastTaskDelta.HuntType == 4;

                var stageSource = new List<RuneTreasureModel.StageReward>
                    { new RuneTreasureModel.StageReward(1, 1) };
                var immutable = new RuneTreasureModel.RuneSnapshot(1, 1, stageSource, 1, 1);
                stageSource.Clear();
                bool g = immutable.StageRewards.Count == 1;

                SeedAll(model);
                frames.Clear();
                long now = TimeUtil.NowSec();
                ServerTimeModel.ApplyServerInfo(now - 10L * 86400L, 0, 0, 0);
                controller.RequestStartup();
                bool h = IsEmpty(model) && StartupFramesAre(frames, true);

                SeedAll(model);
                frames.Clear();
                ServerTimeModel.ApplyServerInfo(0, 0, 0, 0);
                controller.RequestStartup();
                bool i = IsEmpty(model) && StartupFramesAre(frames, false);

                SeedAll(model);
                RuneTreasureModel.ErrorSnapshot seededError = model.LastError;
                RuneTreasureModel.RuneSnapshot seededRune = model.Rune;
                RuneTreasureModel.RecordPushSnapshot seededPush = model.LastRecordPush;
                RuneTreasureModel.WeaponNoticeSnapshot seededNotice = model.LastWeaponNotice;
                RuneTreasureModel.TaskDeltaSnapshot seededDelta = model.LastTaskDelta;
                int pageCount = model.Pages.Count;
                int luckyCount = model.Luckies.Count;
                int crossCount = model.CrossRecords.Count;
                int openCount = model.OpenStates.Count;
                int taskCount = model.Tasks.Count;
                frames.Clear();
                controller.RequestRuneInfo();
                controller.RequestPage(5, 2);
                controller.RequestLucky(4);
                controller.RequestCrossRecords(2);
                controller.RequestOpenState(3);
                controller.RequestTasks(5);
                bool j = ExplicitFramesAre(frames)
                    && ReferenceEquals(model.LastError, seededError)
                    && ReferenceEquals(model.Rune, seededRune)
                    && ReferenceEquals(model.LastRecordPush, seededPush)
                    && ReferenceEquals(model.LastWeaponNotice, seededNotice)
                    && ReferenceEquals(model.LastTaskDelta, seededDelta)
                    && model.Pages.Count == pageCount && model.Luckies.Count == luckyCount
                    && model.CrossRecords.Count == crossCount && model.OpenStates.Count == openCount
                    && model.Tasks.Count == taskCount;

                pass = a && b && c && d && e && f && g && h && i && j;
                Debug.Log("CLIVERIFY runetreasure A=" + a + " B=" + b + " C=" + c
                    + " D=" + d + " E=" + e + " F=" + f + " G=" + g + " H=" + h
                    + " I=" + i + " J=" + j);
            }
            finally
            {
                if (controller.IsInitialized) controller.Dispose();
                RestoreModel(model, oldModel);
                ServerTimeModel.ApplyServerInfo(oldOpenTime, oldMergeTime, oldMergeStartTime, oldMergeCount);
                if (wasInitialized) controller.Init();
                foreach (int id in RegisteredIds) RestoreHandler(handlers, savedHandlers[id], id);
                foreach (int id in ExcludedIds) RestoreHandler(handlers, savedHandlers[id], id);
                if (interceptor != null) interceptor.SetValue(null, oldInterceptor);

                restored = controller.IsInitialized == wasInitialized
                    && ModelMatches(model, oldModel)
                    && ServerTimeModel.OpenTime == oldOpenTime
                    && ServerTimeModel.MergeTime == oldMergeTime
                    && ServerTimeModel.MergeStartTime == oldMergeStartTime
                    && ServerTimeModel.MergeCount == oldMergeCount
                    && (interceptor == null || ReferenceEquals(interceptor.GetValue(null), oldInterceptor));
                foreach (int id in RegisteredIds)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                foreach (int id in ExcludedIds)
                    restored &= HandlerMatches(handlers, savedHandlers[id], id);
                Debug.Log("CLIVERIFY runetreasure restored=" + restored);
            }
            return pass && restored ? 0 : 3;
        }

        private static void SeedAll(RuneTreasureModel model)
        {
            model.Reset();
            model.ReplaceError(1);
            model.ReplaceRune(new RuneTreasureModel.RuneSnapshot(1, 1,
                Array.Empty<RuneTreasureModel.StageReward>(), 1, 1));
            model.ReplaceRecordPush(new RuneTreasureModel.RecordPushSnapshot(1, 1,
                Array.Empty<RuneTreasureModel.Record>()));
            model.ReplacePage(new RuneTreasureModel.PageSnapshot(1, 1, 1, 1, 1, 1,
                Array.Empty<RuneTreasureModel.Record>()));
            model.ReplaceLucky(new RuneTreasureModel.LuckySnapshot(1, 1, 1));
            model.ReplaceCrossRecords(new RuneTreasureModel.CrossRecordSnapshot(1,
                Array.Empty<RuneTreasureModel.CrossRecord>()));
            model.ReplaceOpenState(new RuneTreasureModel.OpenStateSnapshot(1, 1));
            model.ReplaceWeaponNotice(1);
            model.ReplaceTasks(new RuneTreasureModel.TaskSnapshot(1, 5,
                new[] { new RuneTreasureModel.TaskItem(1, 1, 1) }));
            model.ApplyTaskDelta(5, Array.Empty<RuneTreasureModel.TaskItem>());
        }

        private static bool IsEmpty(RuneTreasureModel model) =>
            !model.HasError && !model.HasRune && !model.HasRecordPush && !model.HasWeaponNotice
            && !model.HasTaskDelta && model.Pages.Count == 0 && model.Luckies.Count == 0
            && model.CrossRecords.Count == 0 && model.OpenStates.Count == 0 && model.Tasks.Count == 0;

        private static bool StartupFramesAre(IReadOnlyList<byte[]> frames, bool includeCross)
        {
            var expected = new List<byte[]>
            {
                Frame1(41601, 4),
                Frame2(41608, 1, 1), Frame2(41608, 2, 1), Frame2(41608, 3, 1),
                Frame2(41608, 1, 2), Frame2(41608, 2, 2), Frame2(41608, 3, 2),
                Frame1(41610, 1), Frame1(41610, 2), Frame1(41610, 3),
            };
            if (includeCross)
            {
                expected.Add(Frame1(41612, 1));
                expected.Add(Frame1(41612, 2));
                expected.Add(Frame1(41612, 3));
            }
            expected.Add(Frame2(41608, 5, 1));
            expected.Add(Frame1(41613, 1));
            expected.Add(Frame1(41613, 2));
            expected.Add(Frame1(41613, 3));
            expected.Add(Frame1(41620, 5));
            return FramesAre(frames, expected.ToArray());
        }

        private static bool ExplicitFramesAre(IReadOnlyList<byte[]> frames) => FramesAre(frames,
            Frame1(41601, 4), Frame2(41608, 5, 2), Frame1(41610, 4),
            Frame1(41612, 2), Frame1(41613, 3), Frame1(41620, 5));

        private static byte[] Frame1(int id, byte value) =>
            new CliVerify.Pkt().H(7).H(1000).H(id).C(value).Bytes();
        private static byte[] Frame2(int id, byte first, byte second) =>
            new CliVerify.Pkt().H(8).H(1000).H(id).C(first).C(second).Bytes();

        private static bool FramesAre(IReadOnlyList<byte[]> actual, params byte[][] expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (!BytesEqual(actual[i], expected[i])) return false;
            return true;
        }

        private static bool Invoke(MethodInfo handler, RuneTreasureController controller, byte[] bytes)
        {
            var reader = new NetReader(bytes, 0, bytes.Length);
            handler.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool BytesEqual(IReadOnlyList<byte> actual, IReadOnlyList<byte> expected)
        {
            if (actual == null || expected == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        private static ModelState CaptureModel(RuneTreasureModel model)
        {
            var state = new ModelState
            {
                LastError = model.LastError,
                Rune = model.Rune,
                LastRecordPush = model.LastRecordPush,
                LastWeaponNotice = model.LastWeaponNotice,
                LastTaskDelta = model.LastTaskDelta,
            };
            foreach (string name in DictionaryFieldNames())
                state.Dictionaries[name] = CaptureDictionary(model, name);
            return state;
        }

        private static void RestoreModel(RuneTreasureModel model, ModelState state)
        {
            model.Reset();
            foreach (string name in DictionaryFieldNames())
                RestoreDictionary(model, name, state.Dictionaries[name]);
            RestoreProperty(model, "LastError", state.LastError);
            RestoreProperty(model, "Rune", state.Rune);
            RestoreProperty(model, "LastRecordPush", state.LastRecordPush);
            RestoreProperty(model, "LastWeaponNotice", state.LastWeaponNotice);
            RestoreProperty(model, "LastTaskDelta", state.LastTaskDelta);
        }

        private static bool ModelMatches(RuneTreasureModel model, ModelState state)
        {
            if (!ReferenceEquals(model.LastError, state.LastError)
                || !ReferenceEquals(model.Rune, state.Rune)
                || !ReferenceEquals(model.LastRecordPush, state.LastRecordPush)
                || !ReferenceEquals(model.LastWeaponNotice, state.LastWeaponNotice)
                || !ReferenceEquals(model.LastTaskDelta, state.LastTaskDelta)) return false;
            foreach (string name in DictionaryFieldNames())
                if (!DictionaryMatches(model, name, state.Dictionaries[name])) return false;
            return true;
        }

        private static string[] DictionaryFieldNames() => new[]
            { "_pages", "_latestDrawWeapon", "_luckies", "_crossRecords", "_openStates", "_tasks" };

        private static DictionaryState CaptureDictionary(object target, string fieldName)
        {
            var state = new DictionaryState();
            var dictionary = target.GetType().GetField(fieldName, F)?.GetValue(target) as IDictionary;
            if (dictionary == null) return state;
            foreach (DictionaryEntry entry in dictionary)
                state.Entries.Add(new KeyValuePair<object, object>(entry.Key, entry.Value));
            return state;
        }

        private static void RestoreDictionary(object target, string fieldName, DictionaryState state)
        {
            var dictionary = target.GetType().GetField(fieldName, F)?.GetValue(target) as IDictionary;
            if (dictionary == null) return;
            dictionary.Clear();
            foreach (KeyValuePair<object, object> entry in state.Entries)
                dictionary.Add(entry.Key, entry.Value);
        }

        private static bool DictionaryMatches(object target, string fieldName, DictionaryState state)
        {
            var dictionary = target.GetType().GetField(fieldName, F)?.GetValue(target) as IDictionary;
            if (dictionary == null || dictionary.Count != state.Entries.Count) return false;
            foreach (KeyValuePair<object, object> entry in state.Entries)
                if (!dictionary.Contains(entry.Key) || !ReferenceEquals(dictionary[entry.Key], entry.Value)
                    && !Equals(dictionary[entry.Key], entry.Value)) return false;
            return true;
        }

        private static void RestoreProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value);

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
