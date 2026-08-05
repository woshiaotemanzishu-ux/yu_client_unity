using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Module.Core.Boss;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>节日大妖图标 51 的墙钟边界、自动跨窗与 generation 隔离专项。</summary>
    public static class BossFeastScheduleCase
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const string Windows = "[{time,[{{8,0,0},{8,0,10}},{{8,1,0},{8,1,10}}]}]";

        public static Task<int> Run() => RunCoreAsync();
        public static void RunBatch() => _ = RunBatchAsync();

        private static async Task RunBatchAsync()
        {
            int code = 3;
            try { code = await Run(); }
            catch (Exception exception) { Debug.LogError("CLIVERIFY boss-feast-schedule EXCEPTION " + exception); }
            finally { Debug.Log("CLIVERIFY boss-feast-schedule EXIT " + code); EditorApplication.Exit(code); }
        }

        private static async Task<int> RunCoreAsync()
        {
            BossController controller = BossController.Instance;
            BossModel model = BossModel.Instance;
            Type type = typeof(BossController);
            FieldInfo ctsField = type.GetField("_feastScheduleCts", PrivateInstance);
            FieldInfo taskField = type.GetField("_feastScheduleTask", PrivateInstance);
            FieldInfo generationField = type.GetField("_feastScheduleGeneration", PrivateInstance);
            FieldInfo hasField = type.GetField("_hasFeastBossActivity", PrivateInstance);
            FieldInfo conditionField = type.GetField("_feastBossCondition", PrivateInstance);
            FieldInfo delayHook = type.GetField("s_feastDelayOverride", PrivateStatic);
            FieldInfo nowHook = type.GetField("s_feastNowSecOverride", PrivateStatic);
            FieldInfo iconHook = type.GetField("s_feastIconStateOverride", PrivateStatic);
            MethodInfo disposeFeast = type.GetMethod("DisposeFeastScheduleState", PrivateInstance);
            bool seams = ctsField != null && taskField != null && generationField != null && hasField != null
                && conditionField != null && delayHook != null && nowHook != null && iconHook != null && disposeFeast != null;
            if (!seams) return 3;

            bool isolated = !controller.IsInitialized && ctsField.GetValue(controller) == null
                && taskField.GetValue(controller) == null && !(bool)hasField.GetValue(controller)
                && conditionField.GetValue(controller) == null && !model.FeastBossActive
                && model.FeastBossEndTime == 0 && string.IsNullOrEmpty(model.FeastBossForeshadow);
            if (!isolated)
            {
                Debug.LogWarning("CLIVERIFY boss-feast-schedule SKIP=2: boss runtime is active");
                return 2;
            }

            object oldDelay = delayHook.GetValue(null);
            object oldNow = nowHook.GetValue(null);
            object oldIcon = iconHook.GetValue(null);
            long oldEnd = model.FeastBossEndTime;
            bool oldActive = model.FeastBossActive;
            string oldForeshadow = model.FeastBossForeshadow;
            long now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var delays = new Queue<TaskCompletionSource<bool>>();
            var states = new List<string>();
            bool restored = false;
            int result = 3;
            try
            {
                nowHook.SetValue(null, new Func<long>(() => now));
                delayHook.SetValue(null, new Func<int, CancellationToken, Task>((_, __) =>
                {
                    var gate = new TaskCompletionSource<bool>();
                    delays.Enqueue(gate);
                    return gate.Task; // 故意忽略取消，专门验证旧 generation 完成后不能回写。
                }));
                iconHook.SetValue(null, new Action<bool, long, string>((active, end, foreshadow) =>
                    states.Add((active ? "active" : string.IsNullOrEmpty(foreshadow) ? "end" : "foreshadow") + ":" + end + ":" + foreshadow)));

                bool beforeTotal = !BossModel.ComputeFeastWindow(Windows, now + 1, now + 100, now).active;
                bool startSecond = !BossModel.ComputeFeastWindow(Windows, 0, 0, now).active;
                bool startPlusOne = BossModel.ComputeFeastWindow(Windows, 0, 0, now + 1).active;
                bool endSecond = BossModel.ComputeFeastWindow(Windows, 0, 0, now + 10).active;
                var afterEnd = BossModel.ComputeFeastWindow(Windows, 0, 0, now + 11);
                bool nextForeshadow = !afterEnd.active && afterEnd.foreshadow == "8:01开启";
                bool afterTotal = !BossModel.ComputeFeastWindow(Windows, now - 10, now, now).active;
                bool crossDay = BossModel.ComputeFeastWindow(Windows, now - 10, now + 2 * 86400, now + 86401).active;
                bool invalid = !BossModel.HasValidFeastWindow("[{time,[{{24,0,0},{25,0,0}}]}]");

                controller.EvaluateFeastBoss(true, Windows, now - 100, now + 1000);
                await WaitUntilAsync(() => delays.Count == 1, "first delay");
                Task oldSchedule = taskField.GetValue(controller) as Task;
                object oldCts = ctsField.GetValue(controller);
                int oldGeneration = (int)generationField.GetValue(controller);
                int initialStates = states.Count;
                bool initialActive = model.FeastBossActive;
                long initialEndTime = model.FeastBossEndTime;
                string initialForeshadow = model.FeastBossForeshadow;
                controller.EvaluateFeastBoss(true, Windows, now - 100, now + 1000);
                bool singleLoop = ReferenceEquals(oldCts, ctsField.GetValue(controller))
                    && ReferenceEquals(oldSchedule, taskField.GetValue(controller))
                    && oldGeneration == (int)generationField.GetValue(controller)
                    && delays.Count == 1 && states.Count == initialStates;

                delays.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => delays.Count == 1, "second delay");
                bool noChangeTick = states.Count == initialStates && model.FeastBossActive == initialActive
                    && model.FeastBossEndTime == initialEndTime && model.FeastBossForeshadow == initialForeshadow;

                now++;
                delays.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => delays.Count == 1, "third delay");
                bool autoActive = model.FeastBossActive && model.FeastBossEndTime == now + 9;
                int statesAtActive = states.Count;

                now += 10;
                delays.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => delays.Count == 1, "fourth delay");
                bool autoForeshadow = !model.FeastBossActive && model.FeastBossForeshadow == "8:01开启";

                controller.EvaluateFeastBoss(false, null, 0, 0);
                bool noActivityCancels = ctsField.GetValue(controller) == null && !model.GetEntranceOpenState();
                controller.EvaluateFeastBoss(true, Windows, now - 100, now + 1000);
                await WaitUntilAsync(() => delays.Count == 2, "new generation delay");
                object newCts = ctsField.GetValue(controller);
                Task newSchedule = taskField.GetValue(controller) as Task;
                int newGeneration = (int)generationField.GetValue(controller);
                now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds() + 1;
                int statesBeforeOldRelease = states.Count;
                bool stateActiveBeforeOldRelease = model.FeastBossActive;
                long stateEndBeforeOldRelease = model.FeastBossEndTime;
                string stateForeshadowBeforeOldRelease = model.FeastBossForeshadow;
                delays.Dequeue().TrySetResult(true); // 被取消的旧循环终于完成其忽略取消的 delay。
                bool oldFinished = await CompletesWithinAsync(oldSchedule, 1000);
                bool generationIsolated = oldFinished && newCts != null && newSchedule != null
                    && ReferenceEquals(newCts, ctsField.GetValue(controller))
                    && ReferenceEquals(newSchedule, taskField.GetValue(controller))
                    && newGeneration > oldGeneration && !ReferenceEquals(oldCts, newCts)
                    && states.Count == statesBeforeOldRelease && model.FeastBossActive == stateActiveBeforeOldRelease
                    && model.FeastBossEndTime == stateEndBeforeOldRelease
                    && model.FeastBossForeshadow == stateForeshadowBeforeOldRelease;

                int gatesBeforeInvalid = delays.Count;
                controller.EvaluateFeastBoss(true, "[]", now - 1, now + 1000);
                bool invalidCancels = invalid && ctsField.GetValue(controller) == null && !model.GetEntranceOpenState()
                    && delays.Count == gatesBeforeInvalid;
                controller.EvaluateFeastBoss(true, Windows, now - 100, now + 1000);
                await WaitUntilAsync(() => delays.Count == 2, "reenter delay");
                bool reenterCreates = ctsField.GetValue(controller) != null;
                disposeFeast.Invoke(controller, null);
                bool disposeCancels = ctsField.GetValue(controller) == null && !model.GetEntranceOpenState();

                bool pass = beforeTotal && startSecond && startPlusOne && endSecond && nextForeshadow && afterTotal
                    && crossDay && invalid && singleLoop && noChangeTick && autoActive && autoForeshadow && noActivityCancels
                    && generationIsolated && invalidCancels && reenterCreates && disposeCancels && statesAtActive > 0;
                Debug.Log("CLIVERIFY boss-feast-schedule pass=" + pass + " states=" + states.Count);
                result = pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY boss-feast-schedule exception: " + exception);
            }
            finally
            {
                while (delays.Count > 0) delays.Dequeue().TrySetResult(true);
                disposeFeast.Invoke(controller, null);
                delayHook.SetValue(null, oldDelay);
                nowHook.SetValue(null, oldNow);
                iconHook.SetValue(null, oldIcon);
                model.SetFeastBossActivity(oldActive, oldEnd, oldForeshadow);
                restored = ctsField.GetValue(controller) == null && model.FeastBossActive == oldActive
                    && model.FeastBossEndTime == oldEnd && model.FeastBossForeshadow == oldForeshadow;
                if (!restored) result = 3;
                Debug.Log("CLIVERIFY boss-feast-schedule result=" + result + " restored=" + restored);
            }
            return result;
        }

        private static async Task WaitUntilAsync(Func<bool> condition, string label)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(1000);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("CLIVERIFY boss-feast-schedule timeout: " + label);
                await Task.Delay(10);
            }
        }

        private static async Task<bool> CompletesWithinAsync(Task task, int milliseconds)
        {
            if (task == null) return false;
            return await Task.WhenAny(task, Task.Delay(milliseconds)) == task;
        }
    }
}
