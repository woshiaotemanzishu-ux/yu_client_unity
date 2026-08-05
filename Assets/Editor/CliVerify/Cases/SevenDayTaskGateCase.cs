using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.SevenDay;
using Shenxiao.Module.Core.Tasks;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>SevenDay 17500 任务门槛真实事件、异步配置与出站帧专项。</summary>
    public static class SevenDayTaskGateCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        public static void RunBatch() => _ = RunBatchAsync();

        private static async Task RunBatchAsync()
        {
            int code = 3;
            bool pass = false;
            SevenDayController controller = SevenDayController.Instance;
            TaskModel model = TaskModel.Instance;
            FieldInfo interceptField = typeof(SevenDayController).GetField("s_taskGateOutboundIntercept", StaticPrivate);
            FieldInfo ensureField = typeof(SevenDayController).GetField("s_taskGateEnsureLoadedOverride", StaticPrivate);
            FieldInfo configField = typeof(MainUIConfigs).GetField("_functionIcon", StaticPrivate);
            FieldInfo configLoadingField = typeof(MainUIConfigs).GetField("_functionIconLoading", StaticPrivate);
            object oldIntercept = interceptField?.GetValue(null);
            object oldEnsure = ensureField?.GetValue(null);
            object oldConfig = configField?.GetValue(null);
            object oldConfigLoading = configLoadingField?.GetValue(null);
            int oldTask = model.NewestFinishTaskId;
            bool wasInitialized = controller.IsInitialized;
            IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
            bool had17500 = handlers != null && handlers.Contains(Proto.SEVENDAY_OPEN_INFO);
            bool had17502 = handlers != null && handlers.Contains(Proto.SEVENDAY_MERGE_INFO);
            object old17500 = had17500 ? handlers[Proto.SEVENDAY_OPEN_INFO] : null;
            object old17502 = had17502 ? handlers[Proto.SEVENDAY_MERGE_INFO] : null;
            FieldInfo currentDayField = typeof(SevenDayModel).GetField("_currentDay", StaticPrivate | BindingFlags.Instance);
            FieldInfo openDayTypeField = typeof(SevenDayModel).GetField("_openDayType", StaticPrivate | BindingFlags.Instance);
            int[] oldCurrentDays = currentDayField?.GetValue(SevenDayModel.Instance) is int[] currentDays ? (int[])currentDays.Clone() : null;
            int[] oldOpenDayTypes = openDayTypeField?.GetValue(SevenDayModel.Instance) is int[] openDayTypes ? (int[])openDayTypes.Clone() : null;
            IDictionary iconEntries = SnapshotDictionary(typeof(ActivityIconManager), "_iconInfoByType", new[] { SevenDayModel.ICON_OPEN, SevenDayModel.ICON_EIGHT, SevenDayModel.ICON_MERGE });
            IDictionary boxEntries = SnapshotDictionary(typeof(ActivityIconManager), "_iconBoxInfoByType", new[] { SevenDayModel.ICON_OPEN, SevenDayModel.ICON_EIGHT, SevenDayModel.ICON_MERGE });
            var frames = new List<byte[]>();

            try
            {
                pass = true;
                if (controller.IsInitialized) controller.Dispose();
                configField?.SetValue(null, BuildIconConfig(50));
                configLoadingField?.SetValue(null, null);
                ensureField?.SetValue(null, new Func<Task>(() => Task.CompletedTask));
                interceptField?.SetValue(null, new Func<byte[], bool>(frame => { frames.Add(frame); return true; }));
                model.SetNewestFinishTaskId(10);
                controller.Init(); // 基线必须从 10 开始，首次无关事件不应发包。

                EmitTask(10);
                Check("baseline/noise", frames.Count == 0);
                EmitTask(50);
                Check("exact hit", Count(frames, Proto.SEVENDAY_OPEN_INFO) == 1);
                EmitTask(50);
                EmitTask(80);
                Check("duplicate and forward", Count(frames, Proto.SEVENDAY_OPEN_INFO) == 1);
                Check("17502 zero", Count(frames, Proto.SEVENDAY_MERGE_INFO) == 0);

                frames.Clear();
                EmitTask(20);
                EmitTask(5);
                EmitTask(60);
                Check("rollback then cross", Count(frames, Proto.SEVENDAY_OPEN_INFO) == 1);

                frames.Clear();
                controller.Dispose();
                model.SetNewestFinishTaskId(10);
                controller.Init();
                var gate = new TaskCompletionSource<bool>();
                ensureField.SetValue(null, new Func<Task>(() => gate.Task));
                EmitTask(60);
                EmitTask(70); // 首轮已跨门槛，等待期间继续推进不能让下一轮从10重算。
                Check("loading single flight", frames.Count == 0);
                gate.SetResult(true);
                await Task.Yield();
                await Task.Yield();
                Check("loading cross", Count(frames, Proto.SEVENDAY_OPEN_INFO) == 1);
                EmitTask(70);
                Check("loading duplicate", Count(frames, Proto.SEVENDAY_OPEN_INFO) == 1);

                frames.Clear();
                controller.Dispose();
                model.SetNewestFinishTaskId(10);
                controller.Init();
                var lateGate = new TaskCompletionSource<bool>();
                ensureField.SetValue(null, new Func<Task>(() => lateGate.Task));
                EmitTask(20); // worker 启动时仍在门槛下。
                EmitTask(100); // 配置等待期间才跨过 50，不能丢失这次推进。
                Check("late crossing waits", frames.Count == 0);
                lateGate.SetResult(true);
                await Task.Yield();
                await Task.Yield();
                Check("late crossing sends once", Count(frames, Proto.SEVENDAY_OPEN_INFO) == 1);

                frames.Clear();
                gate = new TaskCompletionSource<bool>();
                ensureField.SetValue(null, new Func<Task>(() => gate.Task));
                EmitTask(110);
                controller.Dispose();
                gate.SetResult(true);
                await Task.Yield();
                await Task.Yield();
                Check("dispose cancels continuation", frames.Count == 0);

                controller.Init();
                pass &= frames.Count == 0;
                code = pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY sevenday task gate EXCEPTION " + exception);
                code = 1;
            }
            finally
            {
                int exitCode = code;
                try
                {
                    if (controller.IsInitialized) controller.Dispose();
                    model.SetNewestFinishTaskId(oldTask);
                    RestoreArray(currentDayField, SevenDayModel.Instance, oldCurrentDays);
                    RestoreArray(openDayTypeField, SevenDayModel.Instance, oldOpenDayTypes);
                    RestoreDictionary(typeof(ActivityIconManager), "_iconInfoByType", iconEntries);
                    RestoreDictionary(typeof(ActivityIconManager), "_iconBoxInfoByType", boxEntries);
                    if (wasInitialized) controller.Init();
                    RestoreHandler(handlers, Proto.SEVENDAY_OPEN_INFO, had17500, old17500);
                    RestoreHandler(handlers, Proto.SEVENDAY_MERGE_INFO, had17502, old17502);
                    interceptField?.SetValue(null, oldIntercept);
                    ensureField?.SetValue(null, oldEnsure);
                    configField?.SetValue(null, oldConfig);
                    configLoadingField?.SetValue(null, oldConfigLoading);
                }
                catch (Exception restoreException)
                {
                    pass = false;
                    exitCode = 1;
                    Debug.LogError("CLIVERIFY sevenday task gate RESTORE EXCEPTION " + restoreException);
                }
                Debug.Log("CLIVERIFY sevenday task gate VERDICT pass=" + pass);
                EditorApplication.Exit(pass && exitCode == 0 ? 0 : exitCode == 0 ? 3 : exitCode);
            }

            void Check(string tag, bool value)
            {
                Debug.Log("CLIVERIFY sevenday task gate " + tag + " ok=" + value);
                if (!value) pass = false;
            }
        }

        private static void EmitTask(int taskId)
        {
            TaskModel.Instance.SetNewestFinishTaskId(taskId);
            EventDispatcher.Emit(GlobalEvent.EVT_TASK_LIST_UPDATED);
        }

        private static JObject BuildIconConfig(int openTaskId)
        {
            return new JObject
            {
                ["1"] = new JObject
                {
                    [SevenDayModel.ICON_OPEN] = new JObject
                    {
                        ["icon_type"] = SevenDayModel.ICON_OPEN,
                        ["open_task_id"] = openTaskId,
                    }
                }
            };
        }

        private static int Count(IReadOnlyList<byte[]> frames, int proto)
        {
            int count = 0;
            for (int i = 0; i < frames.Count; i++)
                if (frames[i] != null && frames[i].Length >= 6
                    && frames[i][4] == (byte)(proto >> 8) && frames[i][5] == (byte)proto) count++;
            return count;
        }

        private static IDictionary SnapshotDictionary(Type owner, string fieldName, string[] keys)
        {
            IDictionary source = owner.GetField(fieldName, InstancePrivate)?.GetValue(ActivityIconManager.Instance) as IDictionary;
            var snapshot = new Hashtable();
            if (source == null) return snapshot;
            foreach (string key in keys)
                if (source.Contains(key)) snapshot[key] = source[key];
            return snapshot;
        }

        private static void RestoreDictionary(Type owner, string fieldName, IDictionary snapshot)
        {
            IDictionary target = owner.GetField(fieldName, InstancePrivate)?.GetValue(ActivityIconManager.Instance) as IDictionary;
            if (target == null || snapshot == null) return;
            foreach (DictionaryEntry entry in snapshot) target[entry.Key] = entry.Value;
        }

        private static void RestoreArray(FieldInfo field, object target, int[] values)
        {
            if (field == null || values == null) return;
            int[] current = field.GetValue(target) as int[];
            if (current == null || current.Length != values.Length) field.SetValue(target, (int[])values.Clone());
            else Array.Copy(values, current, values.Length);
        }

        private static void RestoreHandler(IDictionary handlers, int proto, bool had, object value)
        {
            if (handlers == null) return;
            if (had) handlers[proto] = value;
            else handlers.Remove(proto);
        }
    }
}
