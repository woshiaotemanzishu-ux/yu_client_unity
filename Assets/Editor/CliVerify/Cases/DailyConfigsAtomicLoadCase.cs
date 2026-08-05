using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Module.Core.Daily;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>DailyConfigs 五表原子提交、单飞、异常重试与 TextAsset release 专项。</summary>
    public static class DailyConfigsAtomicLoadCase
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const int TimeoutMs = 5000;
        private static readonly string[] Configs =
        {
            "config_ac", "config_activity_liveness", "config_to_be_strong",
            "config_activity_reward", "config_liveness_active",
        };

        public static Task<int> Run() => RunSafelyAsync();

        public static void RunBatch() => _ = RunBatchSafelyAsync();

        private static async Task<int> RunSafelyAsync()
        {
            try { return await RunCoreAsync(); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY daily configs TOP-LEVEL EXCEPTION " + exception);
                return 3;
            }
        }

        private static async Task RunBatchSafelyAsync()
        {
            int code = 3;
            try { code = await RunSafelyAsync(); }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY daily configs BATCH EXCEPTION " + exception);
                code = 3;
            }
            finally
            {
                Debug.Log("CLIVERIFY daily configs EXIT " + code);
                EditorApplication.Exit(code);
            }
        }

        private static async Task<int> RunCoreAsync()
        {
            if (!DailyCaseIsolation.CanTouch(out string isolationReason, out bool infrastructureOk))
            {
                if (infrastructureOk)
                {
                    Debug.LogWarning("CLIVERIFY daily configs SKIP=2 ambient " + isolationReason);
                    return 2;
                }
                Debug.LogError("CLIVERIFY daily configs FAIL isolation " + isolationReason);
                return 3;
            }

            FieldInfo[] configFields =
            {
                typeof(DailyConfigs).GetField("_ac", StaticPrivate),
                typeof(DailyConfigs).GetField("_activityLiveness", StaticPrivate),
                typeof(DailyConfigs).GetField("_toBeStrong", StaticPrivate),
                typeof(DailyConfigs).GetField("_activityReward", StaticPrivate),
                typeof(DailyConfigs).GetField("_livenessActive", StaticPrivate),
            };
            FieldInfo loadingField = typeof(DailyConfigs).GetField("_loading", StaticPrivate);
            FieldInfo loadOverrideField = typeof(DailyConfigs).GetField("s_loadAssetOverride", StaticPrivate);
            FieldInfo releaseOverrideField = typeof(DailyConfigs).GetField("s_releaseAssetOverride", StaticPrivate);
            MethodInfo loadAllMethod = typeof(DailyConfigs).GetMethod("LoadAllAsync", StaticPrivate);
            if (Array.Exists(configFields, field => field == null) || loadingField == null
                || loadOverrideField == null || releaseOverrideField == null || loadAllMethod == null)
                throw new InvalidOperationException("DailyConfigs test hooks changed");
            var oldConfigs = new object[configFields.Length];
            for (int i = 0; i < configFields.Length; i++) oldConfigs[i] = configFields[i]?.GetValue(null);
            object oldLoading = loadingField?.GetValue(null);
            object oldLoadOverride = loadOverrideField?.GetValue(null);
            object oldReleaseOverride = releaseOverrideField?.GetValue(null);
            var createdAssets = new List<TextAsset>();
            var gates = new List<TaskCompletionSource<bool>>();
            var loadCounts = new Dictionary<string, int>();
            var releaseCounts = new Dictionary<string, int>();
            bool pass = true;
            bool restored = false;
            int nullReleases = 0;

            void Check(string tag, bool value)
            {
                Debug.Log("CLIVERIFY daily configs " + tag + " ok=" + value);
                pass &= value;
            }

            try
            {
                releaseOverrideField?.SetValue(null, new Action<TextAsset>(asset =>
                {
                    if (asset == null) { nullReleases++; return; }
                    Count(releaseCounts, asset.name);
                    UnityEngine.Object.DestroyImmediate(asset);
                }));

                // 缺资源保持既有 fail-soft：该表发布空 JObject，其余表照常发布，且不得 release(null)。
                SetAllNull(configFields);
                loadingField?.SetValue(null, null);
                loadOverrideField?.SetValue(null, new Func<string, Task<TextAsset>>(cfg =>
                {
                    Count(loadCounts, cfg);
                    return Task.FromResult(cfg == "config_to_be_strong" ? null : Asset(cfg, Json(cfg)));
                }));
                await Bounded(DailyConfigs.EnsureLoaded(), "null asset fail-soft");
                Check("null asset publishes five non-null snapshots without release(null)",
                    DailyConfigs.IsLoaded && AllNonNull(configFields) && ExactlyOnce(loadCounts)
                    && releaseCounts.Count == 4 && nullReleases == 0);

                SetAllNull(configFields);
                loadingField?.SetValue(null, null);
                loadCounts.Clear();
                releaseCounts.Clear();
                loadOverrideField?.SetValue(null, new Func<string, Task<TextAsset>>(async cfg =>
                {
                    Count(loadCounts, cfg);
                    await Task.Yield();
                    if (cfg == "config_to_be_strong") throw new InvalidOperationException("middle fault");
                    return Asset(cfg, Json(cfg));
                }));
                bool faulted = false;
                try { await Bounded(DailyConfigs.EnsureLoaded(), "middle fault"); }
                catch (InvalidOperationException) { faulted = true; }
                Check("middle fault", faulted && !DailyConfigs.IsLoaded && AllNull(configFields));
                Check("fault releases prior assets", releaseCounts.Count == 2 && releaseCounts["config_ac"] == 1
                    && releaseCounts["config_activity_liveness"] == 1);

                loadCounts.Clear();
                releaseCounts.Clear();
                loadOverrideField.SetValue(null, new Func<string, Task<TextAsset>>(cfg =>
                {
                    Count(loadCounts, cfg);
                    return Task.FromResult(Asset(cfg, Json(cfg)));
                }));
                await Bounded(DailyConfigs.EnsureLoaded(), "retry commit");
                Check("retry commits all", DailyConfigs.IsLoaded && ExactlyOnce(loadCounts)
                    && DailyConfigs.GetAc(1, 2, 3) != null
                    && DailyConfigs.ReadInt(DailyConfigs.GetAc(1, 2, 3), "marker") == 1);
                Check("retry APIs", DailyConfigs.ReadString(DailyConfigs.GetActivityLiveness(1, 2), "marker") == "live-old"
                    && DailyConfigs.GetStrongName(7) == "strong-old"
                    && DailyConfigs.GetStrongJumpId(7) == 707
                    && DailyConfigs.GetBoxRewardListById(9).Count == 1
                    && DailyConfigs.GetBoxRewardListById(9)[0].style == 1
                    && DailyConfigs.GetBoxRewardListById(9)[0].typeId == 2
                    && DailyConfigs.GetBoxRewardListById(9)[0].count == 3
                    && DailyConfigs.FindNewFigureId(10, 0) == 101);

                SetAllNull(configFields);
                loadingField?.SetValue(null, null);
                loadCounts.Clear();
                TaskCompletionSource<bool> gate = Gate(gates);
                loadOverrideField.SetValue(null, new Func<string, Task<TextAsset>>(async cfg =>
                {
                    Count(loadCounts, cfg);
                    await gate.Task;
                    return Asset(cfg, Json(cfg));
                }));
                Task first = DailyConfigs.EnsureLoaded();
                Task second = DailyConfigs.EnsureLoaded();
                Check("concurrent task shared", ReferenceEquals(first, second));
                gate.TrySetResult(true);
                await Bounded(Task.WhenAll(first, second), "concurrent single flight");
                Check("concurrent single flight", ExactlyOnce(loadCounts));

                SetAllNull(configFields);
                loadingField?.SetValue(null, null);
                loadCounts.Clear();
                releaseCounts.Clear();
                loadOverrideField.SetValue(null, new Func<string, Task<TextAsset>>(cfg =>
                {
                    Count(loadCounts, cfg);
                    return Task.FromResult(Asset(cfg, cfg == "config_activity_reward" ? "{" : "{}"));
                }));
                bool parseFaulted = false;
                try { await Bounded(DailyConfigs.EnsureLoaded(), "parse fault"); }
                catch (Newtonsoft.Json.JsonReaderException) { parseFaulted = true; }
                Check("parse fault releases asset", parseFaulted && !DailyConfigs.IsLoaded
                    && releaseCounts.Count == 4
                    && releaseCounts["config_ac"] == 1
                    && releaseCounts["config_activity_liveness"] == 1
                    && releaseCounts["config_to_be_strong"] == 1
                    && releaseCounts["config_activity_reward"] == 1);

                loadOverrideField.SetValue(null, new Func<string, Task<TextAsset>>(cfg => Task.FromResult(Asset(cfg, Json(cfg)))));
                await Bounded(DailyConfigs.EnsureLoaded(), "successful snapshot");
                JObject oldAc = DailyConfigs.GetAc(1, 2, 3);
                JObject oldLiveness = DailyConfigs.GetActivityLiveness(1, 2);
                string oldStrongName = DailyConfigs.GetStrongName(7);
                int oldStrongJump = DailyConfigs.GetStrongJumpId(7);
                List<(int style, int typeId, long count)> oldReward = DailyConfigs.GetBoxRewardListById(9);
                int oldFigure = DailyConfigs.FindNewFigureId(10, 0);
                object[] completeSnapshot = Capture(configFields);
                Check("successful APIs", oldAc != null && DailyConfigs.ReadInt(oldAc, "marker") == 1
                    && oldLiveness != null && DailyConfigs.ReadString(oldLiveness, "marker") == "live-old"
                    && oldStrongName == "strong-old" && oldStrongJump == 707
                    && oldReward.Count == 1 && oldReward[0].style == 1 && oldReward[0].typeId == 2
                    && oldReward[0].count == 3 && oldFigure == 101);
                loadOverrideField.SetValue(null, new Func<string, Task<TextAsset>>(cfg =>
                {
                    if (cfg == "config_activity_reward") throw new InvalidOperationException("late fault");
                    return Task.FromResult(Asset(cfg, "{}"));
                }));
                bool lateFaulted = false;
                try { await Bounded((Task)loadAllMethod.Invoke(null, null), "complete snapshot late fault"); }
                catch (InvalidOperationException) { lateFaulted = true; }
                catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
                { lateFaulted = true; }
                Check("complete snapshot survives fault", lateFaulted && DailyConfigs.IsLoaded
                    && SameReferences(completeSnapshot, configFields)
                    && ReferenceEquals(oldAc, DailyConfigs.GetAc(1, 2, 3))
                    && ReferenceEquals(oldLiveness, DailyConfigs.GetActivityLiveness(1, 2))
                    && DailyConfigs.GetStrongName(7) == oldStrongName
                    && DailyConfigs.GetStrongJumpId(7) == oldStrongJump
                    && SameReward(oldReward, DailyConfigs.GetBoxRewardListById(9))
                    && DailyConfigs.FindNewFigureId(10, 0) == oldFigure);
            }
            finally
            {
                try
                {
                    foreach (TaskCompletionSource<bool> gate in gates) gate.TrySetResult(true);
                    Task currentLoad = loadingField.GetValue(null) as Task;
                    if (currentLoad != null)
                    {
                        try { await Bounded(currentLoad, "final load observation"); }
                        catch (Exception loadException)
                        {
                            Debug.Log("CLIVERIFY daily configs observed final load fault "
                                + loadException.GetType().Name);
                        }
                        if (!currentLoad.IsCompleted)
                            throw new TimeoutException("Daily config load is still visible; state restore is unsafe");
                    }
                    await Task.Yield();

                    SetAll(configFields, oldConfigs);
                    loadingField?.SetValue(null, oldLoading);
                    loadOverrideField?.SetValue(null, oldLoadOverride);
                    releaseOverrideField?.SetValue(null, oldReleaseOverride);
                    for (int i = 0; i < createdAssets.Count; i++)
                        if (createdAssets[i] != null) UnityEngine.Object.DestroyImmediate(createdAssets[i]);

                    restored = SameTables(configFields, oldConfigs)
                        && ReferenceEquals(loadingField.GetValue(null), oldLoading)
                        && ReferenceEquals(loadOverrideField.GetValue(null), oldLoadOverride)
                        && ReferenceEquals(releaseOverrideField.GetValue(null), oldReleaseOverride);
                }
                catch (Exception restoreException)
                {
                    pass = false;
                    Debug.LogError("CLIVERIFY daily configs RESTORE EXCEPTION " + restoreException);
                }
            }

            Debug.Log("CLIVERIFY daily configs VERDICT pass=" + pass + " restored=" + restored);
            return pass && restored ? 0 : 3;

            TextAsset Asset(string cfg, string json)
            {
                TextAsset asset = new TextAsset(json) { name = cfg };
                createdAssets.Add(asset);
                return asset;
            }

            string Json(string cfg)
            {
                switch (cfg)
                {
                    case "config_ac": return "{\"1@2@3\":{\"marker\":1}}";
                    case "config_activity_liveness": return "{\"1@2\":{\"marker\":\"live-old\"}}";
                    case "config_to_be_strong": return "{\"7\":{\"name\":\"strong-old\",\"jump_id\":707}}";
                    case "config_activity_reward": return "{\"9\":{\"reward\":\"[{\\\"0\\\":0,\\\"1\\\":[{\\\"0\\\":1,\\\"1\\\":2,\\\"2\\\":3}]}]\"}}";
                    case "config_liveness_active": return "{\"1\":{\"id\":101,\"lv\":10}}";
                    default: throw new ArgumentOutOfRangeException(nameof(cfg), cfg, null);
                }
            }
        }

        private static void SetAllNull(FieldInfo[] fields)
        {
            for (int i = 0; i < fields.Length; i++) fields[i]?.SetValue(null, null);
        }

        private static TaskCompletionSource<bool> Gate(List<TaskCompletionSource<bool>> gates)
        {
            var gate = new TaskCompletionSource<bool>();
            gates.Add(gate);
            return gate;
        }

        private static bool SameTables(FieldInfo[] fields, object[] values)
        {
            if (fields.Length != values.Length) return false;
            for (int i = 0; i < fields.Length; i++)
                if (!ReferenceEquals(fields[i].GetValue(null), values[i])) return false;
            return true;
        }

        private static void SetAll(FieldInfo[] fields, object[] values)
        {
            for (int i = 0; i < fields.Length; i++) fields[i]?.SetValue(null, values[i]);
        }

        private static object[] Capture(FieldInfo[] fields)
        {
            var values = new object[fields.Length];
            for (int i = 0; i < fields.Length; i++) values[i] = fields[i]?.GetValue(null);
            return values;
        }

        private static bool AllNull(FieldInfo[] fields)
        {
            for (int i = 0; i < fields.Length; i++) if (fields[i]?.GetValue(null) != null) return false;
            return true;
        }

        private static bool AllNonNull(FieldInfo[] fields)
        {
            for (int i = 0; i < fields.Length; i++) if (fields[i]?.GetValue(null) == null) return false;
            return true;
        }

        private static bool SameReferences(object[] values, FieldInfo[] fields)
        {
            if (values.Length != fields.Length) return false;
            for (int i = 0; i < fields.Length; i++)
                if (!ReferenceEquals(values[i], fields[i].GetValue(null))) return false;
            return true;
        }

        private static bool SameReward(List<(int style, int typeId, long count)> a,
            List<(int style, int typeId, long count)> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void Count(Dictionary<string, int> counts, string key)
        {
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        private static bool ExactlyOnce(Dictionary<string, int> counts)
        {
            if (counts.Count != Configs.Length) return false;
            foreach (string cfg in Configs)
                if (!counts.TryGetValue(cfg, out int count) || count != 1) return false;
            return true;
        }

        private static async Task Bounded(Task task, string tag)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            Task timeout = Task.Delay(TimeoutMs);
            if (await Task.WhenAny(task, timeout) != task) throw new TimeoutException("daily configs " + tag);
            await task;
        }
    }
}
