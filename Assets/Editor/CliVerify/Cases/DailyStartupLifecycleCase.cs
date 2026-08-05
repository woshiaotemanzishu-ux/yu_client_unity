using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.ActivityForeshow;
using Shenxiao.Module.Core.Daily;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>GAME_START 配置重试、代次取消、Dispose/ReInit 和启动帧唯一性的隔离验证。</summary>
    public static class DailyStartupLifecycleCase
    {
        private const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags I = BindingFlags.NonPublic | BindingFlags.Instance;
        private const int TimeoutMs = 5000;

        private static readonly int[] RelatedProtocols =
        {
            15700, 15701, 15703, 15705, 15706, 15709, 15710, 15711, 15712, 15714,
            15715, 15716, 15717, 15718, 15719, 15720, 15721, 41900, 41903, 41904, 61801,
            65208,
        };

        private static readonly string[] RelatedEvents =
        {
            GlobalEvent.EVT_GAME_START,
            GlobalEvent.EVT_ROLE_INFO_UPDATE,
            GlobalEvent.EVT_SERVER_DAY_CHANGE,
            GlobalEvent.EVT_SERVER_TIME_REFRESH,
        };

        public static Task<int> Run() => RunSafelyAsync();

        public static void RunBatch() => _ = RunBatchSafelyAsync();

        private static async Task<int> RunSafelyAsync()
        {
            try { return await RunCoreAsync(); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY daily startup TOP-LEVEL EXCEPTION " + exception);
                return 3;
            }
        }

        private static async Task RunBatchSafelyAsync()
        {
            int code = 3;
            try { code = await RunSafelyAsync(); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY daily startup BATCH EXCEPTION " + exception);
                code = 3;
            }
            finally
            {
                Debug.Log("CLIVERIFY daily startup EXIT " + code);
                EditorApplication.Exit(code);
            }
        }

        private static async Task<int> RunCoreAsync()
        {
            if (!DailyCaseIsolation.CanTouch(out string isolationReason, out bool infrastructureOk))
            {
                if (infrastructureOk)
                {
                    Debug.LogWarning("CLIVERIFY daily startup SKIP=2 ambient " + isolationReason);
                    return 2;
                }
                Debug.LogError("CLIVERIFY daily startup FAIL isolation " + isolationReason);
                return 3;
            }

            DailyController controller = DailyController.Instance;
            RoleModel role = RoleModel.Instance;
            DailyModel model = DailyModel.Instance;
            ActivityForeshowController foreshow = ActivityForeshowController.Instance;

            FieldInfo loadHook = Field(typeof(DailyConfigs), "s_loadAssetOverride", true);
            FieldInfo releaseHook = Field(typeof(DailyConfigs), "s_releaseAssetOverride", true);
            FieldInfo loading = Field(typeof(DailyConfigs), "_loading", true);
            FieldInfo outbound = Field(typeof(DailyController), "s_outboundIntercept", true);
            FieldInfo delay = Field(typeof(DailyController), "s_retryDelayOverride", true);
            FieldInfo startupObserver = Field(typeof(DailyController), "s_startupTaskObserver", true);
            FieldInfo startupCts = Field(typeof(DailyController), "_startupCts", false);
            FieldInfo startupReady = Field(typeof(DailyController), "_startupReady", false);
            FieldInfo[] tables = DailyCaseIsolation.ConfigFields();
            IDictionary netHandlers = Field(typeof(NetManager), "_handlers", true)?.GetValue(null) as IDictionary;
            IDictionary eventHandlers = Field(typeof(EventDispatcher), "_handlers", true)?.GetValue(null) as IDictionary;
            IDictionary redDots = Field(typeof(ActivityIconManager), "_redDotByType", false)?
                .GetValue(ActivityIconManager.Instance) as IDictionary;
            if (loadHook == null || releaseHook == null || loading == null || outbound == null || delay == null
                || startupObserver == null
                || startupCts == null || startupReady == null || Array.Exists(tables, field => field == null)
                || netHandlers == null || eventHandlers == null || redDots == null)
                throw new InvalidOperationException("Daily startup reflection seams changed");

            var tableValues = new object[tables.Length];
            for (int i = 0; i < tables.Length; i++) tableValues[i] = tables[i].GetValue(null);
            object oldLoading = loading.GetValue(null);
            object oldLoadHook = loadHook.GetValue(null);
            object oldReleaseHook = releaseHook.GetValue(null);
            object oldOutbound = outbound.GetValue(null);
            object oldDelay = delay.GetValue(null);
            object oldStartupObserver = startupObserver.GetValue(null);
            var controllerState = new ObjectState(controller);
            var roleState = new ObjectState(role);
            var modelState = new ObjectState(model);
            var foreshowState = new ObjectState(foreshow);
            var handlerState = new Dictionary<int, MapSlot>();
            foreach (int protocol in RelatedProtocols) handlerState[protocol] = new MapSlot(netHandlers, protocol);
            var eventState = new Dictionary<string, EventSlot>();
            foreach (string evt in RelatedEvents) eventState[evt] = new EventSlot(eventHandlers, evt);
            var redDotState = new MapSlot(redDots, "157");

            var assets = new List<TextAsset>();
            var gates = new List<TaskCompletionSource<bool>>();
            var startupTasks = new List<Task>();
            var frames = new List<byte[]>();
            var messages = new List<string>();
            int gameStartSignals = 0;
            Application.LogCallback logCallback = (condition, stackTrace, type) => messages.Add(condition ?? string.Empty);
            bool logAttached = false;
            bool pass = true;
            bool restored = false;

            void Check(string tag, bool ok)
            {
                Debug.Log("CLIVERIFY daily startup " + tag + " ok=" + ok);
                pass &= ok;
            }

            TextAsset Asset(string name, string text)
            {
                var asset = new TextAsset(text) { name = name };
                assets.Add(asset);
                return asset;
            }

            Func<string, Task<TextAsset>> SuccessfulLoader() =>
                cfg => Task.FromResult(Asset(cfg, Json(cfg)));

            void EmitGameStart()
            {
                gameStartSignals++;
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_START);
            }

            void PrepareScenario(int level)
            {
                if (startupCts.GetValue(controller) != null)
                    throw new InvalidOperationException("previous Daily startup still active");
                ClearTables(tables);
                loading.SetValue(null, null);
                frames.Clear();
                role.Level = level;
                role.MarkBaseInfoReady();
            }

            try
            {
                Application.logMessageReceived += logCallback;
                logAttached = true;
                releaseHook.SetValue(null, new Action<TextAsset>(asset => UnityEngine.Object.DestroyImmediate(asset)));
                outbound.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    frames.Add((byte[])frame.Clone());
                    return true;
                }));
                startupObserver.SetValue(null, new Action<Task>(task =>
                {
                    if (task == null) throw new InvalidOperationException("Daily startup observer received null Task");
                    startupTasks.Add(task);
                }));
                controller.Init();

                // 1. 首轮第三表 fault；一次 1000ms retry 后低等级完整七帧且首轮零帧。
                PrepareScenario(DailyModel.LIVENESS_FIND_OPEN_LEVEL - 1);
                int loads = 0;
                int retryDelays = 0;
                loadHook.SetValue(null, new Func<string, Task<TextAsset>>(cfg =>
                {
                    loads++;
                    if (loads == 3) throw new InvalidOperationException("expected third-table fault");
                    return Task.FromResult(Asset(cfg, Json(cfg)));
                }));
                delay.SetValue(null, new Func<int, CancellationToken, Task>((milliseconds, token) =>
                {
                    Check("retry delay is 1000ms", milliseconds == 1000);
                    retryDelays++;
                    Check("fault attempt emits zero frame", frames.Count == 0);
                    return Task.CompletedTask;
                }));
                EmitGameStart();
                await Until(() => startupCts.GetValue(controller) == null, "fault then retry success");
                Check("third-table fault then one retry", loads == 8 && retryDelays == 1);
                Check("low level exact seven-frame batch", StartupFrames(frames, false));

                // 2. ready 前等级事件零帧；ready 后同级零帧，变级精确一个 15721。
                PrepareScenario(10);
                var roleGate = Gate(gates);
                var roleLoadStarted = Gate(gates);
                loadHook.SetValue(null, new Func<string, Task<TextAsset>>(async cfg =>
                {
                    roleLoadStarted.TrySetResult(true);
                    await roleGate.Task;
                    return Asset(cfg, Json(cfg));
                }));
                delay.SetValue(null, new Func<int, CancellationToken, Task>((milliseconds, token) => Task.CompletedTask));
                EmitGameStart();
                await Bounded(roleLoadStarted.Task, "role gate load start");
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                Check("not-ready role update emits zero 15721", frames.Count == 0 && !(bool)startupReady.GetValue(controller));
                roleGate.TrySetResult(true);
                await Until(() => startupCts.GetValue(controller) == null, "role gate startup ready");
                Check("role gate startup batch", StartupFrames(frames, false) && (bool)startupReady.GetValue(controller));
                int readyFrameCount = frames.Count;
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                Check("ready same level emits zero", frames.Count == readyFrameCount);
                role.Level++;
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                Check("ready changed level emits exactly one 15721",
                    frames.Count == readyFrameCount + 1 && EmptyFrame(frames[readyFrameCount], 15721));

                // 3. 高等级批次八帧，15715 固定在第七帧(index=6)。
                PrepareScenario(DailyModel.LIVENESS_FIND_OPEN_LEVEL);
                loadHook.SetValue(null, SuccessfulLoader());
                EmitGameStart();
                await Until(() => startupCts.GetValue(controller) == null, "high-level startup");
                Check("high level exact eight frames with 15715 at index 6", StartupFrames(frames, true));

                // 4. 同一在途配置上的两个 GAME_START 只允许最新 epoch 发一批。
                PrepareScenario(10);
                var sharedGate = Gate(gates);
                var sharedStarted = Gate(gates);
                loadHook.SetValue(null, new Func<string, Task<TextAsset>>(async cfg =>
                {
                    sharedStarted.TrySetResult(true);
                    await sharedGate.Task;
                    return Asset(cfg, Json(cfg));
                }));
                EmitGameStart();
                await Bounded(sharedStarted.Task, "shared load start");
                EmitGameStart();
                sharedGate.TrySetResult(true);
                await Until(() => startupCts.GetValue(controller) == null, "shared load newest epoch");
                Check("two GAME_START shared load only newest emits", StartupFrames(frames, false));

                // 5. Dispose→ReInit→新 GAME_START，共享旧 gate 释放后仍仅新会话发一批。
                PrepareScenario(10);
                var disposeGate = Gate(gates);
                var disposeStarted = Gate(gates);
                loadHook.SetValue(null, new Func<string, Task<TextAsset>>(async cfg =>
                {
                    disposeStarted.TrySetResult(true);
                    await disposeGate.Task;
                    return Asset(cfg, Json(cfg));
                }));
                EmitGameStart();
                await Bounded(disposeStarted.Task, "dispose old load start");
                controller.Dispose();
                Check("dispose clears startup ownership", startupCts.GetValue(controller) == null
                    && !(bool)startupReady.GetValue(controller));
                controller.Init();
                EmitGameStart();
                disposeGate.TrySetResult(true);
                await Until(() => startupCts.GetValue(controller) == null, "dispose reinit newest epoch");
                Check("Dispose/ReInit shared old gate only new session emits", StartupFrames(frames, false));

                // 6. 三轮 fault：仅两次 1000ms delay、一次终态日志、零帧、无残留 CTS/ready。
                PrepareScenario(10);
                messages.Clear();
                int faults = 0;
                int faultDelays = 0;
                loadHook.SetValue(null, new Func<string, Task<TextAsset>>(cfg =>
                {
                    faults++;
                    throw new InvalidOperationException("expected terminal fault");
                }));
                delay.SetValue(null, new Func<int, CancellationToken, Task>((milliseconds, token) =>
                {
                    if (milliseconds != 1000) throw new InvalidOperationException("retry delay must be 1000ms");
                    faultDelays++;
                    return Task.CompletedTask;
                }));
                EmitGameStart();
                await Until(() => startupCts.GetValue(controller) == null, "three terminal faults");
                int terminalLogs = messages.FindAll(message =>
                    message.Contains("DailyConfigs") && message.Contains("GAME_START")).Count;
                Check("three faults two delays one terminal log zero frames",
                    faults == 3 && faultDelays == 2 && terminalLogs == 1 && frames.Count == 0);
                Check("terminal failure leaves no CTS or ready",
                    startupCts.GetValue(controller) == null && !(bool)startupReady.GetValue(controller));
            }
            finally
            {
                try
                {
                    foreach (TaskCompletionSource<bool> gate in gates) gate.TrySetResult(true);
                    if (controller.IsInitialized) controller.Dispose();
                    if (startupTasks.Count != gameStartSignals)
                        throw new InvalidOperationException("Daily startup observer missed a fire-and-forget Task; state restore is unsafe");
                    Task allStartups = Task.WhenAll(startupTasks.ToArray());
                    try { await Bounded(allStartups, "all observed startup tasks"); }
                    catch (Exception startupException)
                    {
                        pass = false;
                        Debug.LogError("CLIVERIFY daily startup OBSERVED TASK EXCEPTION " + startupException);
                    }
                    if (!allStartups.IsCompleted)
                        throw new TimeoutException("Daily startup tasks are still visible; state restore is unsafe");
                    Task currentLoad = loading.GetValue(null) as Task;
                    if (currentLoad != null)
                    {
                        try { await Bounded(currentLoad, "final load observation"); }
                        catch (Exception loadException)
                        {
                            Debug.Log("CLIVERIFY daily startup observed final load fault "
                                + loadException.GetType().Name);
                        }
                        if (!currentLoad.IsCompleted)
                            throw new TimeoutException("Daily config load is still visible; state restore is unsafe");
                    }
                    await Task.Yield();

                    for (int i = 0; i < tables.Length; i++) tables[i].SetValue(null, tableValues[i]);
                    loading.SetValue(null, oldLoading);
                    loadHook.SetValue(null, oldLoadHook);
                    releaseHook.SetValue(null, oldReleaseHook);
                    outbound.SetValue(null, oldOutbound);
                    delay.SetValue(null, oldDelay);
                    startupObserver.SetValue(null, oldStartupObserver);
                    foreach (KeyValuePair<int, MapSlot> pair in handlerState) pair.Value.Restore(netHandlers, pair.Key);
                    foreach (KeyValuePair<string, EventSlot> pair in eventState) pair.Value.Restore(eventHandlers, pair.Key);
                    redDotState.Restore(redDots, "157");
                    controllerState.Restore();
                    roleState.Restore();
                    modelState.Restore();
                    foreshowState.Restore();
                    for (int i = 0; i < assets.Count; i++)
                        if (assets[i] != null) UnityEngine.Object.DestroyImmediate(assets[i]);

                    restored = controllerState.Matches() && roleState.Matches() && modelState.Matches()
                        && foreshowState.Matches() && SameTables(tables, tableValues)
                        && ReferenceEquals(loading.GetValue(null), oldLoading)
                        && ReferenceEquals(loadHook.GetValue(null), oldLoadHook)
                        && ReferenceEquals(releaseHook.GetValue(null), oldReleaseHook)
                        && ReferenceEquals(outbound.GetValue(null), oldOutbound)
                        && ReferenceEquals(delay.GetValue(null), oldDelay)
                        && ReferenceEquals(startupObserver.GetValue(null), oldStartupObserver)
                        && redDotState.Matches(redDots, "157");
                    foreach (KeyValuePair<int, MapSlot> pair in handlerState)
                        restored &= pair.Value.Matches(netHandlers, pair.Key);
                    foreach (KeyValuePair<string, EventSlot> pair in eventState)
                        restored &= pair.Value.Matches(eventHandlers, pair.Key);
                }
                catch (Exception restoreException)
                {
                    pass = false;
                    Debug.LogError("CLIVERIFY daily startup RESTORE EXCEPTION " + restoreException);
                }
                finally
                {
                    if (logAttached) Application.logMessageReceived -= logCallback;
                }
            }

            Debug.Log("CLIVERIFY daily startup VERDICT pass=" + pass + " restored=" + restored);
            return pass && restored ? 0 : 3;
        }

        private static TaskCompletionSource<bool> Gate(List<TaskCompletionSource<bool>> gates)
        {
            var gate = new TaskCompletionSource<bool>();
            gates.Add(gate);
            return gate;
        }

        private static async Task Bounded(Task task, string tag)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            Task timeout = Task.Delay(TimeoutMs);
            if (await Task.WhenAny(task, timeout) != task) throw new TimeoutException("daily startup " + tag);
            await task;
        }

        private static async Task Until(Func<bool> condition, string tag)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(TimeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("daily startup " + tag);
                await Task.Delay(1);
            }
        }

        private static void ClearTables(FieldInfo[] fields)
        {
            for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, null);
        }

        private static string Json(string cfg)
        {
            switch (cfg)
            {
                case "config_ac": return "{}";
                case "config_activity_liveness": return "{}";
                case "config_to_be_strong": return "{}";
                case "config_activity_reward": return "{}";
                case "config_liveness_active": return "{}";
                default: throw new ArgumentOutOfRangeException(nameof(cfg), cfg, null);
            }
        }

        private static bool StartupFrames(IReadOnlyList<byte[]> frames, bool highLevel)
        {
            int[] expected = highLevel
                ? new[] { 15701, 15701, 15703, 41900, 15709, 15721, 15715, 15718 }
                : new[] { 15701, 15701, 15703, 41900, 15709, 15721, 15718 };
            if (frames.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
            {
                byte[] frame = frames[i];
                if (!ValidHeader(frame) || FrameId(frame) != expected[i]) return false;
                if (i < 2)
                {
                    if (frame.Length != 7 || frame[6] != (i == 0 ? DailyModel.ACT_UNLIMIT : DailyModel.ACT_LIMIT))
                        return false;
                }
                else if (frame.Length != 6) return false;
            }
            return !highLevel || FrameId(frames[6]) == 15715;
        }

        private static bool EmptyFrame(byte[] frame, int protocol) =>
            ValidHeader(frame) && frame.Length == 6 && FrameId(frame) == protocol;

        private static bool ValidHeader(byte[] frame) => frame != null && frame.Length >= 6
            && frame[0] == (byte)(frame.Length >> 8) && frame[1] == (byte)frame.Length
            && frame[2] == 3 && frame[3] == 232;

        private static int FrameId(byte[] frame) => frame != null && frame.Length >= 6
            ? (frame[4] << 8) | frame[5]
            : -1;

        private static FieldInfo Field(Type type, string name, bool isStatic) => type.GetField(name,
            (isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic);

        private static bool SameTables(FieldInfo[] fields, object[] values)
        {
            if (fields.Length != values.Length) return false;
            for (int i = 0; i < fields.Length; i++)
                if (!ReferenceEquals(fields[i].GetValue(null), values[i])) return false;
            return true;
        }

        private sealed class MapSlot
        {
            private readonly bool _exists;
            private readonly object _value;

            public MapSlot(IDictionary map, object key)
            {
                _exists = map != null && map.Contains(key);
                _value = _exists ? map[key] : null;
            }

            public void Restore(IDictionary map, object key)
            {
                if (_exists) map[key] = _value;
                else map.Remove(key);
            }

            public bool Matches(IDictionary map, object key)
            {
                if (map == null || map.Contains(key) != _exists) return false;
                if (!_exists) return true;
                object current = map[key];
                return _value != null && _value.GetType().IsValueType
                    ? Equals(current, _value)
                    : ReferenceEquals(current, _value);
            }
        }

        private sealed class EventSlot
        {
            private readonly bool _exists;
            private readonly object _list;
            private readonly object[] _delegates;

            public EventSlot(IDictionary map, string key)
            {
                _exists = map != null && map.Contains(key);
                _list = _exists ? map[key] : null;
                if (_list is IList values)
                {
                    _delegates = new object[values.Count];
                    values.CopyTo(_delegates, 0);
                }
                else _delegates = Array.Empty<object>();
            }

            public void Restore(IDictionary map, string key)
            {
                if (!_exists) { map.Remove(key); return; }
                if (!(_list is IList values)) { map[key] = _list; return; }
                values.Clear();
                for (int i = 0; i < _delegates.Length; i++) values.Add(_delegates[i]);
                map[key] = _list;
            }

            public bool Matches(IDictionary map, string key)
            {
                if (map == null || map.Contains(key) != _exists) return false;
                if (!_exists) return true;
                if (!ReferenceEquals(map[key], _list) || !(_list is IList values) || values.Count != _delegates.Length)
                    return false;
                for (int i = 0; i < _delegates.Length; i++)
                    if (!ReferenceEquals(values[i], _delegates[i])) return false;
                return true;
            }
        }

        private sealed class ObjectState
        {
            private readonly object _target;
            private readonly FieldInfo[] _fields;
            private readonly object[] _values;

            public ObjectState(object target)
            {
                _target = target ?? throw new ArgumentNullException(nameof(target));
                var fields = new List<FieldInfo>();
                for (Type type = target.GetType(); type != null; type = type.BaseType)
                    fields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        | BindingFlags.DeclaredOnly));
                _fields = fields.ToArray();
                _values = new object[_fields.Length];
                for (int i = 0; i < _fields.Length; i++) _values[i] = _fields[i].GetValue(_target);
            }

            public void Restore()
            {
                for (int i = 0; i < _fields.Length; i++)
                {
                    if (_fields[i].IsInitOnly) continue;
                    _fields[i].SetValue(_target, _values[i]);
                }
            }

            public bool Matches()
            {
                for (int i = 0; i < _fields.Length; i++)
                {
                    object current = _fields[i].GetValue(_target);
                    if (_fields[i].FieldType.IsValueType)
                    {
                        if (!Equals(current, _values[i])) return false;
                    }
                    else if (!ReferenceEquals(current, _values[i])) return false;
                }
                return true;
            }
        }
    }

    /// <summary>Daily 两个 CLI 专项共用的只读 pre-touch 门；不满足时必须在任何写入前 SKIP。</summary>
    internal static class DailyCaseIsolation
    {
        private const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags I = BindingFlags.NonPublic | BindingFlags.Instance;

        internal static FieldInfo[] ConfigFields() => new[]
        {
            typeof(DailyConfigs).GetField("_ac", S),
            typeof(DailyConfigs).GetField("_activityLiveness", S),
            typeof(DailyConfigs).GetField("_toBeStrong", S),
            typeof(DailyConfigs).GetField("_activityReward", S),
            typeof(DailyConfigs).GetField("_livenessActive", S),
        };

        internal static bool CanTouch(out string reason, out bool infrastructureOk)
        {
            infrastructureOk = false;
            FieldInfo[] configs = ConfigFields();
            FieldInfo loading = typeof(DailyConfigs).GetField("_loading", S);
            FieldInfo loadHook = typeof(DailyConfigs).GetField("s_loadAssetOverride", S);
            FieldInfo releaseHook = typeof(DailyConfigs).GetField("s_releaseAssetOverride", S);
            FieldInfo outbound = typeof(DailyController).GetField("s_outboundIntercept", S);
            FieldInfo delay = typeof(DailyController).GetField("s_retryDelayOverride", S);
            FieldInfo startupObserver = typeof(DailyController).GetField("s_startupTaskObserver", S);
            FieldInfo netField = typeof(NetManager).GetField("_handlers", S);
            FieldInfo eventField = typeof(EventDispatcher).GetField("_handlers", S);
            FieldInfo iconField = typeof(ActivityIconManager).GetField("_iconInfoByType", I);
            FieldInfo boxIconField = typeof(ActivityIconManager).GetField("_iconBoxInfoByType", I);
            if (Array.Exists(configs, field => field == null) || loading == null || loadHook == null
                || releaseHook == null || outbound == null || delay == null || startupObserver == null
                || netField == null || eventField == null
                || iconField == null || boxIconField == null)
            {
                reason = "missing reflection seam";
                return false;
            }
            infrastructureOk = true;

            DailyController daily = DailyController.Instance;
            ActivityForeshowController foreshow = ActivityForeshowController.Instance;
            if (daily.IsInitialized || FieldValue(daily, "_startupCts") != null
                || BoolField(daily, "_startupReady") || IntField(daily, "_startupEpoch") != 0
                || IntField(daily, "_lastLevel") != -1)
            {
                reason = "Daily controller lifecycle active";
                return false;
            }
            if (foreshow.IsInitialized || FieldValue(foreshow, "_scheduleCts") != null
                || FieldValue(foreshow, "_scheduleTask") != null || FieldValue(foreshow, "_refreshTask") != null
                || BoolField(foreshow, "_refreshPending") || CountOf(FieldValue(foreshow, "_scheduledIconTypes")) != 0
                || ActivityForeshowModel.Instance.HasSnatchInfo || ActivityForeshowModel.Instance.SnatchDunId != 0
                || ActivityForeshowModel.Instance.SnatchEndTime != 0
                || typeof(ActivityForeshowController).GetField("s_refreshOverride", S)?.GetValue(null) != null
                || typeof(ActivityForeshowController).GetField("s_delayOverride", S)?.GetValue(null) != null)
            {
                reason = "ActivityForeshow lifecycle active";
                return false;
            }
            for (int i = 0; i < configs.Length; i++)
                if (configs[i].GetValue(null) != null) { reason = "DailyConfigs snapshot already present"; return false; }
            if (loading.GetValue(null) != null || loadHook.GetValue(null) != null || releaseHook.GetValue(null) != null
                || outbound.GetValue(null) != null || delay.GetValue(null) != null
                || startupObserver.GetValue(null) != null)
            {
                reason = "Daily test/config task already present";
                return false;
            }

            if (HasObjectState(RoleModel.Instance, null)) { reason = "Role state is non-empty"; return false; }
            if (HasObjectState(DailyModel.Instance, "<IsRemind>k__BackingField"))
            { reason = "DailyModel state is non-empty"; return false; }

            if (!(netField.GetValue(null) is IDictionary netHandlers))
            { infrastructureOk = false; reason = "NetManager handler map missing"; return false; }
            int[] protocols =
            {
                15700, 15701, 15703, 15705, 15706, 15709, 15710, 15711, 15712, 15714,
                15715, 15716, 15717, 15718, 15719, 15720, 15721, 41900, 41903, 41904, 61801, 65208,
            };
            foreach (int protocol in protocols)
                if (netHandlers.Contains(protocol)) { reason = "related Net handler " + protocol; return false; }

            if (!(eventField.GetValue(null) is IDictionary eventHandlers))
            { infrastructureOk = false; reason = "EventDispatcher map missing"; return false; }
            string[] events =
            {
                GlobalEvent.EVT_GAME_START, GlobalEvent.EVT_ROLE_INFO_UPDATE,
                GlobalEvent.EVT_SERVER_DAY_CHANGE, GlobalEvent.EVT_SERVER_TIME_REFRESH,
            };
            foreach (string evt in events)
                if (eventHandlers.Contains(evt) && CountOf(eventHandlers[evt]) > 0)
                { reason = "related event handler " + evt; return false; }

            IDictionary icons = iconField.GetValue(ActivityIconManager.Instance) as IDictionary;
            IDictionary boxIcons = boxIconField.GetValue(ActivityIconManager.Instance) as IDictionary;
            if ((icons != null && icons.Contains("157")) || (boxIcons != null && boxIcons.Contains("157")))
            { reason = "Daily activity icon is live"; return false; }

            reason = "clean";
            return true;
        }

        private static object FieldValue(object target, string name) => target.GetType().GetField(name, I)?.GetValue(target);
        private static int IntField(object target, string name) => FieldValue(target, name) is int value ? value : int.MinValue;
        private static bool BoolField(object target, string name) => FieldValue(target, name) is bool value && value;

        private static int CountOf(object value)
        {
            if (value == null) return 0;
            if (value is ICollection collection) return collection.Count;
            PropertyInfo count = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
            return count != null && count.PropertyType == typeof(int) ? (int)count.GetValue(value) : -1;
        }

        private static bool HasObjectState(object target, string allowedTrueField)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    object value = field.GetValue(target);
                    if (field.Name == allowedTrueField && value is bool allowed && allowed) continue;
                    if (value == null) continue;
                    if (value is string text) { if (text.Length != 0) return true; continue; }
                    int count = CountOf(value);
                    if (count >= 0) { if (count != 0) return true; continue; }
                    if (field.FieldType.IsValueType)
                    {
                        object zero = Activator.CreateInstance(field.FieldType);
                        if (!Equals(value, zero)) return true;
                        continue;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
