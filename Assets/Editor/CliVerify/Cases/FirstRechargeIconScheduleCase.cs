using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.Role;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>首充横幅配置续体、计时器 owner 与 Dispose generation 隔离专项。</summary>
    public static class FirstRechargeIconScheduleCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        public static Task<int> Run() => RunCoreAsync();
        public static void RunBatch() => _ = RunBatchAsync();

        private static async Task RunBatchAsync()
        {
            int code = 3;
            try { code = await Run(); }
            catch (Exception exception) { Debug.LogError("CLIVERIFY first-recharge-schedule EXCEPTION " + exception); }
            finally { Debug.Log("CLIVERIFY first-recharge-schedule EXIT " + code); EditorApplication.Exit(code); }
        }

        private static async Task<int> RunCoreAsync()
        {
            FirstRechargeController controller = FirstRechargeController.Instance;
            FirstRechargeModel model = FirstRechargeModel.Instance;
            RoleModel role = RoleModel.Instance;
            Type type = typeof(FirstRechargeController);
            FieldInfo cts = type.GetField("_bannerTimerCts", InstancePrivate);
            FieldInfo timer = type.GetField("_bannerTimerTask", InstancePrivate);
            FieldInfo refreshTask = type.GetField("_refreshTask", InstancePrivate);
            FieldInfo generation = type.GetField("_refreshGeneration", InstancePrivate);
            FieldInfo notifyGeneration = type.GetField("_notifySentGeneration", InstancePrivate);
            FieldInfo applied = type.GetField("_hasAppliedIconState", InstancePrivate);
            FieldInfo configHook = type.GetField("s_configLoadOverride", StaticPrivate);
            FieldInfo delayHook = type.GetField("s_delayOverride", StaticPrivate);
            FieldInfo nowHook = type.GetField("s_nowSecOverride", StaticPrivate);
            FieldInfo iconHook = type.GetField("s_refreshIconOverride", StaticPrivate);
            FieldInfo dotHook = type.GetField("s_redDotOverride", StaticPrivate);
            FieldInfo sendHook = type.GetField("s_notifyOutboundOverride", StaticPrivate);
            FieldInfo eventHook = type.GetField("s_updateEventOverride", StaticPrivate);
            MethodInfo refresh = type.GetMethod("RefreshMainUIIcons", InstancePrivate);
            bool seams = cts != null && timer != null && refreshTask != null && generation != null && notifyGeneration != null && applied != null && configHook != null && delayHook != null
                && nowHook != null && iconHook != null && dotHook != null && sendHook != null && eventHook != null && refresh != null;
            bool isolated = seams && !controller.IsInitialized && cts.GetValue(controller) == null && timer.GetValue(controller) == null && refreshTask.GetValue(controller) == null
                && model.Slots.Count == 0 && model.ProductId == 0 && !model.IsNotify && !model.IsBuy
                && !role.HasBaseInfo && role.RegisterTime == 0 && (int)generation.GetValue(controller) == 0
                && (int)notifyGeneration.GetValue(controller) == -1 && !(bool)applied.GetValue(controller)
                && configHook.GetValue(null) == null && delayHook.GetValue(null) == null && nowHook.GetValue(null) == null
                && iconHook.GetValue(null) == null && dotHook.GetValue(null) == null && sendHook.GetValue(null) == null && eventHook.GetValue(null) == null;
            if (!isolated)
            {
                Debug.LogWarning("CLIVERIFY first-recharge-schedule SKIP=2: first-recharge or role runtime is active");
                return 2;
            }

            object oldConfig = configHook.GetValue(null), oldDelay = delayHook.GetValue(null), oldNow = nowHook.GetValue(null);
            object oldIcon = iconHook.GetValue(null), oldDot = dotHook.GetValue(null), oldSend = sendHook.GetValue(null), oldEvent = eventHook.GetValue(null);
            var configGates = new Queue<TaskCompletionSource<bool>>();
            var delayGates = new Queue<TaskCompletionSource<bool>>();
            var startedTasks = new List<Task>();
            var iconStates = new List<string>();
            int redDots = 0, sends = 0, events = 0;
            long now = 1000;
            int result = 3;
            try
            {
                model.SetInfo(new List<FirstRechargeModel.Slot> { new FirstRechargeModel.Slot(0, 1) }, 77, false);
                role.RegisterTime = 100;
                role.MarkBaseInfoReady();
                configHook.SetValue(null, new Func<Task>(() =>
                {
                    var gate = new TaskCompletionSource<bool>(); configGates.Enqueue(gate); return gate.Task;
                }));
                delayHook.SetValue(null, new Func<int, CancellationToken, Task>((_, __) =>
                {
                    var gate = new TaskCompletionSource<bool>(); delayGates.Enqueue(gate); return gate.Task; // 故意忽略取消
                }));
                nowHook.SetValue(null, new Func<long>(() => now));
                iconHook.SetValue(null, new Func<bool, bool, long, string, Task>((show, banner, end, text) =>
                {
                    iconStates.Add(show + ":" + banner + ":" + end + ":" + text); return Task.CompletedTask;
                }));
                dotHook.SetValue(null, new Action<bool>(_ => redDots++));
                sendHook.SetValue(null, new Action<int>(cmd => { if (cmd == 15907) sends++; }));
                eventHook.SetValue(null, new Action(() => events++));

                // 两次配置等待逆序释放：过期续体不得提交或创建 timer。
                refresh.Invoke(controller, null);
                refresh.Invoke(controller, null);
                await WaitUntilAsync(() => configGates.Count == 2, "two config gates");
                TaskCompletionSource<bool> firstConfig = configGates.Dequeue();
                TaskCompletionSource<bool> latestConfig = configGates.Dequeue();
                latestConfig.TrySetResult(true);
                await WaitUntilAsync(() => iconStates.Count == 1 && delayGates.Count == 1, "latest config commit");
                object currentCts = cts.GetValue(controller);
                Task currentTimer = timer.GetValue(controller) as Task;
                if (currentTimer != null) startedTasks.Add(currentTimer);
                int currentGeneration = (int)generation.GetValue(controller);
                firstConfig.TrySetResult(true);
                await Task.Delay(20);
                bool inverseConfig = iconStates.Count == 1 && delayGates.Count == 1
                    && ReferenceEquals(currentCts, cts.GetValue(controller)) && ReferenceEquals(currentTimer, timer.GetValue(controller));

                // 首轮相同状态的并发 refresh 已只提交一次；状态变化后才替换 timer owner。
                role.RegisterTime = 110;
                refresh.Invoke(controller, null);
                await WaitUntilAsync(() => configGates.Count == 1, "changed config gate");
                configGates.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => iconStates.Count == 2 && delayGates.Count == 2, "changed latest timer");
                object newerCts = cts.GetValue(controller);
                Task newerTimer = timer.GetValue(controller) as Task;
                if (newerTimer != null) startedTasks.Add(newerTimer);
                bool replacedSingleOwner = newerCts != null && newerTimer != null && !ReferenceEquals(currentCts, newerCts)
                    && (int)generation.GetValue(controller) > currentGeneration;

                // 已被替换的 delay 忽略 cancellation 后才完成，也不得写 model/icon/event/15907。
                int iconsBeforeOld = iconStates.Count, dotsBeforeOld = redDots, sendsBeforeOld = sends, eventsBeforeOld = events;
                bool notifyBeforeOld = model.IsNotify;
                delayGates.Dequeue().TrySetResult(true);
                bool oldCompletes = await CompletesWithinAsync(currentTimer, 1000);
                bool oldDelayIsolated = oldCompletes && ReferenceEquals(newerCts, cts.GetValue(controller))
                    && ReferenceEquals(newerTimer, timer.GetValue(controller)) && iconStates.Count == iconsBeforeOld
                    && redDots == dotsBeforeOld && sends == sendsBeforeOld && events == eventsBeforeOld && model.IsNotify == notifyBeforeOld;

                // Dispose 使未完成 timer 失效；随后释放不允许任何写入。
                int iconsBeforeDispose = iconStates.Count, dotsBeforeDispose = redDots, sendsBeforeDispose = sends, eventsBeforeDispose = events;
                controller.Dispose();
                delayGates.Dequeue().TrySetResult(true);
                bool disposedTimerCompletes = await CompletesWithinAsync(newerTimer, 1000);
                bool disposeIsolated = disposedTimerCompletes && cts.GetValue(controller) == null && timer.GetValue(controller) == null
                    && iconStates.Count == iconsBeforeDispose && redDots == dotsBeforeDispose + 1 && sends == sendsBeforeDispose
                    && events == eventsBeforeDispose && model.Slots.Count == 0 && model.ProductId == 0 && !model.IsNotify && !model.IsBuy;

                // Dispose 时仍卡在配置加载的续体，在释放后也不得提交。
                model.SetInfo(new List<FirstRechargeModel.Slot> { new FirstRechargeModel.Slot(0, 2) }, 88, false);
                role.RegisterTime = 200;
                role.MarkBaseInfoReady();
                refresh.Invoke(controller, null);
                await WaitUntilAsync(() => configGates.Count == 1, "dispose config gate");
                Task disposedConfigRefresh = refreshTask.GetValue(controller) as Task;
                if (disposedConfigRefresh != null) startedTasks.Add(disposedConfigRefresh);
                int iconsBeforeConfigDispose = iconStates.Count;
                controller.Dispose();
                int dotsAfterConfigDispose = redDots;
                configGates.Dequeue().TrySetResult(true);
                await Task.Delay(20);
                bool disposeConfigIsolated = cts.GetValue(controller) == null && timer.GetValue(controller) == null
                    && iconStates.Count == iconsBeforeConfigDispose && redDots == dotsAfterConfigDispose;

                // Dispose 后重新进入的一代仍可独立工作。
                model.SetInfo(new List<FirstRechargeModel.Slot> { new FirstRechargeModel.Slot(0, 3) }, 99, false);
                role.RegisterTime = 300;
                role.MarkBaseInfoReady();
                refresh.Invoke(controller, null);
                await WaitUntilAsync(() => configGates.Count == 1, "reenter config gate");
                configGates.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => delayGates.Count == 1, "reenter timer");
                Task effectiveTimer = timer.GetValue(controller) as Task;
                if (effectiveTimer != null) startedTasks.Add(effectiveTimer);
                bool reenterWorks = cts.GetValue(controller) != null && effectiveTimer != null
                    && iconStates.Count == iconsBeforeConfigDispose + 1;

                // 当前有效 timer 到期：15907/本地事件各一次，随后只做一次横幅→普通图标重评并收口 timer。
                int iconsBeforeExpiry = iconStates.Count;
                delayGates.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => sends == 1 && events == 1 && configGates.Count == 1, "effective expiry refresh");
                configGates.Dequeue().TrySetResult(true);
                bool effectiveCompletes = await CompletesWithinAsync(effectiveTimer, 1000);
                await WaitUntilAsync(() => iconStates.Count == iconsBeforeExpiry + 1, "effective expiry icon");
                bool effectiveExpiry = effectiveCompletes && model.IsNotify && sends == 1 && events == 1
                    && cts.GetValue(controller) == null && timer.GetValue(controller) == null;

                // 展示仍是 Standard、但权威 IsNotify 回退时，待执行15907不能被展示签名去重吞掉。
                model.IsNotify = false;
                now = role.RegisterTime + 1800;
                int iconsBeforePendingNotify = iconStates.Count;
                refresh.Invoke(controller, null);
                await WaitUntilAsync(() => configGates.Count == 1, "pending notify config");
                configGates.Dequeue().TrySetResult(true);
                await WaitUntilAsync(() => sends == 2 && events == 2 && iconStates.Count == iconsBeforePendingNotify + 1, "pending notify commit");
                bool pendingNotifyBypassesDedup = model.IsNotify && sends == 2 && events == 2
                    && cts.GetValue(controller) == null && timer.GetValue(controller) == null;

                // 已通知的完全相同展示签名才真正单飞：不再等待配置、不重绘、不重建 timer/15907/事件。
                object timerBeforeRepeat = timer.GetValue(controller);
                int iconsBeforeRepeat = iconStates.Count, configsBeforeRepeat = configGates.Count;
                int sendsBeforeRepeat = sends, eventsBeforeRepeat = events;
                refresh.Invoke(controller, null);
                await Task.Delay(20);
                bool sameStateDedup = configGates.Count == configsBeforeRepeat && iconStates.Count == iconsBeforeRepeat
                    && ReferenceEquals(timerBeforeRepeat, timer.GetValue(controller)) && sends == sendsBeforeRepeat && events == eventsBeforeRepeat;

                bool pass = inverseConfig && replacedSingleOwner && oldDelayIsolated && disposeIsolated && disposeConfigIsolated
                    && reenterWorks && effectiveExpiry && pendingNotifyBypassesDedup && sameStateDedup;
                Debug.Log("CLIVERIFY first-recharge-schedule pass=" + pass + " icons=" + iconStates.Count + " sends=" + sends);
                result = pass ? 0 : 3;
            }
            catch (Exception exception) { Debug.LogError("CLIVERIFY first-recharge-schedule exception: " + exception); }
            finally
            {
                while (configGates.Count > 0) configGates.Dequeue().TrySetResult(true);
                while (delayGates.Count > 0) delayGates.Dequeue().TrySetResult(true);
                foreach (Task started in startedTasks) await CompletesWithinAsync(started, 1000);
                Task finalRefresh = refreshTask.GetValue(controller) as Task;
                if (finalRefresh != null) startedTasks.Add(finalRefresh);
                controller.Dispose();
                while (configGates.Count > 0) configGates.Dequeue().TrySetResult(true);
                while (delayGates.Count > 0) delayGates.Dequeue().TrySetResult(true);
                if (finalRefresh != null) await CompletesWithinAsync(finalRefresh, 1000);
                configHook.SetValue(null, oldConfig); delayHook.SetValue(null, oldDelay); nowHook.SetValue(null, oldNow);
                iconHook.SetValue(null, oldIcon); dotHook.SetValue(null, oldDot); sendHook.SetValue(null, oldSend); eventHook.SetValue(null, oldEvent);
                model.Clear(); role.Reset();
                bool restored = cts.GetValue(controller) == null && timer.GetValue(controller) == null && refreshTask.GetValue(controller) == null
                    && model.Slots.Count == 0 && model.ProductId == 0 && !model.IsNotify && !model.IsBuy && !role.HasBaseInfo
                    && role.RegisterTime == 0 && (int)notifyGeneration.GetValue(controller) == -1 && !(bool)applied.GetValue(controller);
                if (!restored) result = 3;
                Debug.Log("CLIVERIFY first-recharge-schedule result=" + result + " restored=" + restored);
            }
            return result;
        }

        private static async Task WaitUntilAsync(Func<bool> condition, string label)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(1000);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("CLIVERIFY first-recharge-schedule timeout: " + label);
                await Task.Delay(10);
            }
        }

        private static async Task<bool> CompletesWithinAsync(Task task, int milliseconds) =>
            task != null && await Task.WhenAny(task, Task.Delay(milliseconds)) == task;
    }
}
