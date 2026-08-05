using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Module.Core.ActivityForeshow;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>ActivityForeshow 调度循环的顺序、合并、取消和 generation 隔离专项。</summary>
    public static class ActivityForeshowScheduleCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const int TimeoutMs = 5000;

        public static Task<int> Run() => RunCoreAsync();
        public static void RunBatch() => _ = RunBatchAsync();

        private static async Task RunBatchAsync()
        {
            int code;
            try { code = await RunCoreAsync(); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY activity foreshow schedule EXCEPTION " + exception);
                code = 1;
            }
            Debug.Log("CLIVERIFY activity foreshow schedule EXIT " + code);
            EditorApplication.Exit(code);
        }

        private static async Task<int> RunCoreAsync()
        {
            ActivityForeshowController controller = ActivityForeshowController.Instance;
            ActivityForeshowModel model = ActivityForeshowModel.Instance;
            FieldInfo ctsField = Field("_scheduleCts", false);
            FieldInfo loopField = Field("_scheduleTask", false);
            FieldInfo refreshField = Field("_refreshTask", false);
            FieldInfo pendingField = Field("_refreshPending", false);
            FieldInfo generationField = Field("_scheduleGeneration", false);
            FieldInfo scheduledField = Field("_scheduledIconTypes", false);
            FieldInfo lastLevelField = Field("_lastLevel", false);
            FieldInfo refreshHook = Field("s_refreshOverride", true);
            FieldInfo delayHook = Field("s_delayOverride", true);
            MethodInfo ensureMethod = Method("EnsureScheduleLoop");
            MethodInfo dayChangeMethod = Method("OnServerDayChange");
            MethodInfo requestMethod = Method("RequestScheduledRefreshAsync");
            bool seams = ctsField != null && loopField != null && refreshField != null && pendingField != null
                && generationField != null && scheduledField != null && lastLevelField != null && refreshHook != null && delayHook != null
                && ensureMethod != null && dayChangeMethod != null && requestMethod != null;
            bool pass = seams;
            bool wasInitialized = controller.IsInitialized;
            bool hadLoop = ctsField?.GetValue(controller) is CancellationTokenSource oldCts && !oldCts.IsCancellationRequested;
            int oldLastLevel = lastLevelField != null ? (int)lastLevelField.GetValue(controller) : -1;
            object oldRefresh = refreshHook?.GetValue(null);
            object oldDelay = delayHook?.GetValue(null);
            bool oldHasInfo = model.HasSnatchInfo;
            int oldDunId = model.SnatchDunId;
            long oldEndTime = model.SnatchEndTime;
            var oldScheduledTypes = new List<string>();
            if (scheduledField?.GetValue(controller) is IEnumerable oldScheduled)
                foreach (object item in oldScheduled) if (item is string iconType) oldScheduledTypes.Add(iconType);
            IDictionary iconDict = typeof(ActivityIconManager).GetField("_iconInfoByType", InstancePrivate)
                ?.GetValue(ActivityIconManager.Instance) as IDictionary;
            IDictionary boxIconDict = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", InstancePrivate)
                ?.GetValue(ActivityIconManager.Instance) as IDictionary;
            bool isolated = !wasInitialized && !oldHasInfo && oldScheduledTypes.Count == 0
                && ctsField?.GetValue(controller) == null && loopField?.GetValue(controller) == null && refreshField?.GetValue(controller) == null
                && (iconDict == null || iconDict.Count == 0) && (boxIconDict == null || boxIconDict.Count == 0);
            var messages = new List<string>();
            Application.LogCallback logCallback = (condition, stackTrace, type) => messages.Add(condition ?? string.Empty);
            bool touched = false;

            void Check(string tag, bool value)
            {
                Debug.Log("CLIVERIFY activity foreshow schedule " + tag + " ok=" + value);
                pass &= value;
            }

            try
            {
                Check("reflection seams", seams);
                if (!seams) return 3;
                Check("isolated runtime", isolated);
                // Dispose/DeleteIcon 会同步变更活动栏和收纳盒并派发事件；有现场时绝不借专项清空后伪恢复。
                if (!isolated)
                {
                    Debug.LogWarning("CLIVERIFY activity foreshow schedule SKIP: controller/model/icon runtime is active");
                    return 2;
                }
                touched = true;
                Application.logMessageReceived += logCallback;
                if (controller.IsInitialized) controller.Dispose();
                controller.Init();

                // 刷新未结束不得开始 15 秒等待；首轮普通异常后仍进入下一轮。
                int refreshes = 0, active = 0, maxActive = 0, delayCalls = 0;
                var firstStarted = new TaskCompletionSource<bool>();
                var releaseFirst = new TaskCompletionSource<bool>();
                var secondStarted = new TaskCompletionSource<bool>();
                var releaseDelay = new TaskCompletionSource<bool>();
                var stopDelay = new TaskCompletionSource<bool>();
                refreshHook.SetValue(null, new Func<CancellationToken, Task>(async token =>
                {
                    active++; maxActive = Math.Max(maxActive, active);
                    try
                    {
                        refreshes++;
                        if (refreshes == 1)
                        {
                            firstStarted.TrySetResult(true);
                            await WaitBounded(releaseFirst.Task, "release first refresh");
                            throw new InvalidOperationException("expected scheduled refresh fault");
                        }
                        secondStarted.TrySetResult(true);
                    }
                    finally { active--; }
                }));
                delayHook.SetValue(null, new Func<int, CancellationToken, Task>((milliseconds, token) =>
                {
                    delayCalls++;
                    return WaitWithCancel(delayCalls == 1 ? releaseDelay.Task : stopDelay.Task, token);
                }));
                ensureMethod.Invoke(controller, null);
                await WaitBounded(firstStarted.Task, "first refresh start");
                Check("refresh before delay", delayCalls == 0);
                releaseFirst.TrySetResult(true);
                await WaitUntil(() => delayCalls == 1, "delay after refresh");
                releaseDelay.TrySetResult(true);
                await WaitBounded(secondStarted.Task, "second refresh after ordinary fault");
                Check("fault then next round", refreshes == 2 && maxActive == 1);

                // 首轮同步完成和同步重入：owner 必须先可见，结束后字段必须回到 null。
                controller.Dispose(); controller.Init();
                int syncRefreshes = 0;
                int syncGeneration = (int)generationField.GetValue(controller);
                refreshHook.SetValue(null, new Func<CancellationToken, Task>(token =>
                {
                    syncRefreshes++;
                    if (syncRefreshes == 1)
                        requestMethod.Invoke(controller, new object[] { syncGeneration, CancellationToken.None });
                    return Task.CompletedTask;
                }));
                Task synchronous = (Task)requestMethod.Invoke(controller, new object[] { syncGeneration, CancellationToken.None });
                await WaitBounded(synchronous, "synchronous refresh completion");
                Check("sync owner clears and reentry singleflight", syncRefreshes == 2 && refreshField.GetValue(controller) == null
                    && !(bool)pendingField.GetValue(controller));

                // 无既有循环时的一次跨天只使用新循环首轮，不应追加第二轮。
                controller.Dispose(); controller.Init();
                refreshes = 0; active = 0; maxActive = 0;
                var singleStarted = new TaskCompletionSource<bool>();
                var singleGate = new TaskCompletionSource<bool>();
                refreshHook.SetValue(null, new Func<CancellationToken, Task>(async token =>
                {
                    active++; maxActive = Math.Max(maxActive, active);
                    try { refreshes++; singleStarted.TrySetResult(true); await WaitWithCancel(singleGate.Task, token); }
                    finally { active--; }
                }));
                dayChangeMethod.Invoke(controller, null);
                await WaitBounded(singleStarted.Task, "single day-change refresh");
                await Task.Yield();
                Check("new loop day-change once", refreshes == 1 && maxActive == 1);
                singleGate.TrySetResult(true);

                // 已在刷新的 DAY_CHANGE/TIME_REFRESH 合并为当前轮后至多一次补跑。
                controller.Dispose(); controller.Init();
                refreshes = 0; active = 0; maxActive = 0;
                var currentStarted = new TaskCompletionSource<bool>();
                var currentGate = new TaskCompletionSource<bool>();
                var supplementStarted = new TaskCompletionSource<bool>();
                refreshHook.SetValue(null, new Func<CancellationToken, Task>(async token =>
                {
                    active++; maxActive = Math.Max(maxActive, active);
                    try
                    {
                        refreshes++;
                        if (refreshes == 1) { currentStarted.TrySetResult(true); await WaitWithCancel(currentGate.Task, token); }
                        else supplementStarted.TrySetResult(true);
                    }
                    finally { active--; }
                }));
                ensureMethod.Invoke(controller, null);
                await WaitBounded(currentStarted.Task, "current refresh start");
                dayChangeMethod.Invoke(controller, null); // DAY_CHANGE
                dayChangeMethod.Invoke(controller, null); // TIME_REFRESH 共用同一处理器
                currentGate.TrySetResult(true);
                await WaitBounded(supplementStarted.Task, "one coalesced supplement");
                await Task.Yield();
                Check("time events one supplement", refreshes == 2 && maxActive == 1
                    && !(bool)pendingField.GetValue(controller));

                // 取消不应作为普通错误记录；旧 generation 完成也不得清新循环字段。
                controller.Dispose(); controller.Init();
                refreshes = 0; messages.Clear();
                var oldStarted = new TaskCompletionSource<bool>();
                var oldGate = new TaskCompletionSource<bool>();
                var newStarted = new TaskCompletionSource<bool>();
                var newGate = new TaskCompletionSource<bool>();
                refreshHook.SetValue(null, new Func<CancellationToken, Task>(async token =>
                {
                    refreshes++;
                    if (refreshes == 1) { oldStarted.TrySetResult(true); await oldGate.Task; }
                    else { newStarted.TrySetResult(true); await WaitWithCancel(newGate.Task, token); }
                }));
                ensureMethod.Invoke(controller, null);
                await WaitBounded(oldStarted.Task, "old generation start");
                controller.Dispose(); controller.Init();
                ensureMethod.Invoke(controller, null);
                await WaitBounded(newStarted.Task, "new generation start");
                oldGate.TrySetResult(true);
                await Task.Yield();
                Check("old generation isolated", ctsField.GetValue(controller) != null && loopField.GetValue(controller) != null
                    && refreshField.GetValue(controller) != null && !(bool)pendingField.GetValue(controller));
                Check("cancel no ordinary error", !ContainsOrdinaryScheduleError(messages));
                newGate.TrySetResult(true);
            }
            finally
            {
                try
                {
                    if (touched)
                    {
                        Application.logMessageReceived -= logCallback;
                        if (controller.IsInitialized) controller.Dispose();
                        refreshHook?.SetValue(null, oldRefresh);
                        delayHook?.SetValue(null, oldDelay);
                        if (wasInitialized) controller.Init();
                        if (oldHasInfo) model.SetSnatchTimeMsg(oldDunId, oldEndTime); else model.Reset();
                        if (scheduledField?.GetValue(controller) is HashSet<string> scheduled)
                        {
                            scheduled.Clear();
                            foreach (string iconType in oldScheduledTypes) scheduled.Add(iconType);
                        }
                        lastLevelField?.SetValue(controller, oldLastLevel);
                        // 不复用已 Dispose 的 CTS/Task，也不回退 generation；仅按旧钩子重建等价循环。
                        if (wasInitialized && hadLoop) ensureMethod?.Invoke(controller, null);
                    }
                }
                catch (Exception restoreException)
                {
                    pass = false;
                    Debug.LogError("CLIVERIFY activity foreshow schedule RESTORE EXCEPTION " + restoreException);
                }
            }
            Debug.Log("CLIVERIFY activity foreshow schedule VERDICT pass=" + pass);
            return pass ? 0 : 3;
        }

        private static FieldInfo Field(string name, bool isStatic) => typeof(ActivityForeshowController).GetField(name,
            isStatic ? StaticPrivate : InstancePrivate);
        private static MethodInfo Method(string name) => typeof(ActivityForeshowController).GetMethod(name, InstancePrivate);

        private static bool ContainsOrdinaryScheduleError(List<string> messages)
        {
            foreach (string message in messages)
                if (message.Contains("调度刷新失败") || message.Contains("调度循环异常退出")) return true;
            return false;
        }

        private static async Task WaitBounded(Task task, string name)
        {
            if (await Task.WhenAny(task, Task.Delay(TimeoutMs)) != task) throw new TimeoutException(name);
            await task;
        }

        private static async Task WaitUntil(Func<bool> condition, string name)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(TimeoutMs);
            while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(10);
            if (!condition()) throw new TimeoutException(name);
        }

        private static async Task WaitWithCancel(Task wait, CancellationToken token)
        {
            Task completed = await Task.WhenAny(wait, Task.Delay(Timeout.Infinite, token));
            await completed;
        }
    }
}
