using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.RuntimeCapture
{
    /// <summary>共享通道结构验收：并发、生命周期、分辨率重建和空闲释放。</summary>
    public static class UIEffectSharedChannelTestMenu
    {
        private const string TestRootName = "__UIEffectSharedChannelStress";
        private const string TestEffect = "ui_zhanli";
        private const string TestLabel = "__shared_channel_stress_ui_zhanli";
        private const int ConcurrentCount = 20;
        private const int LeakRounds = 10;
        private const int LeakBatchSize = 10;
        private static readonly List<UIEffectStage.Handle> s_handles = new List<UIEffectStage.Handle>();

        [MenuItem("神霄/调试/UI运行态/共享通道压力测试（20实例+100次生命周期）", priority = 114)]
        public static async void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UIEffectSharedTest] 请先进入 Play Mode。");
                return;
            }

            Clear();
            int baselineLive = UIEffectStage.LiveCount;
            RectTransform root = CreateRoot();
            var hosts = new List<RectTransform>(ConcurrentCount);
            var tasks = new List<Task<UIEffectStage.Handle>>(ConcurrentCount);

            try
            {
                for (int i = 0; i < ConcurrentCount; i++)
                {
                    RectTransform host = CreateHost(root, i, ConcurrentCount);
                    hosts.Add(host);
                    tasks.Add(AddTestEffect(host));
                }

                UIEffectStage.Handle[] concurrent = await Task.WhenAll(tasks);
                AddHandles(concurrent);
                await WaitEditorFrames(2);

                ValidateOneSharedResource("20 concurrent", ConcurrentCount, s_handles.Count);
                DisposeHandles();
                await WaitEditorFrames(2);

                RenderTexture firstTexture = null;
                for (int round = 0; round < LeakRounds; round++)
                {
                    tasks.Clear();
                    for (int i = 0; i < LeakBatchSize; i++)
                        tasks.Add(AddTestEffect(hosts[i]));

                    UIEffectStage.Handle[] batch = await Task.WhenAll(tasks);
                    AddHandles(batch);
                    await WaitEditorFrames(2);

                    RenderTexture current = FindTestChannelTexture();
                    if (firstTexture == null) firstTexture = current;
                    if (current == null || current != firstTexture)
                        Debug.LogError("[UIEffectSharedTest] FAIL: channel RT was not reused in round " + round);

                    DisposeHandles();
                    await WaitEditorFrames(2);
                }

                int finalLive = UIEffectStage.LiveCount;
                if (finalLive != baselineLive)
                    Debug.LogError($"[UIEffectSharedTest] FAIL: live handle leak baseline={baselineLive} final={finalLive}");
                else
                    Debug.Log($"[UIEffectSharedTest] PASS: 100 create/dispose operations returned to baseline={baselineLive}.");

                await ValidateResolutionRebuild(root);
                await ValidateIdleRelease();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                Clear();
            }
        }

        [MenuItem("神霄/调试/UI运行态/清理共享通道压力测试", priority = 115)]
        public static void Clear()
        {
            DisposeHandles();
            GameObject root = GameObject.Find(TestRootName);
            if (root != null) UnityEngine.Object.Destroy(root);
        }

        private static async Task ValidateResolutionRebuild(RectTransform root)
        {
            if (!TryFindTestChannel(out UIEffectStage.ChannelDiagnostic before) || before.Texture == null)
            {
                Debug.LogError("[UIEffectSharedTest] FAIL: resolution test channel missing before resize.");
                return;
            }

            RenderTexture oldTexture = before.Texture;
            Camera oldCamera = before.Camera;
            UnityEngine.UI.RawImage oldImage = before.Image;
            root.sizeDelta = new Vector2(1024f, 768f);
            await WaitEditorFrames(3);

            bool pass = TryFindTestChannel(out UIEffectStage.ChannelDiagnostic after) &&
                        after.Texture != null && after.Texture != oldTexture &&
                        after.RtWidth == 1024 && after.RtHeight == 768 &&
                        after.Camera == oldCamera && after.Image == oldImage;
            if (pass)
                Debug.Log("[UIEffectSharedTest] PASS: 720x1280 -> 1024x768 rebuilt only the shared RT; Camera/Image were reused.");
            else
                Debug.LogError("[UIEffectSharedTest] FAIL: resolution rebuild did not preserve the channel resources.");
        }

        private static async Task ValidateIdleRelease()
        {
            float waitSeconds = UIEffectProfileCatalog.Runtime.idleReleaseSeconds + 0.5f;
            await WaitEditorSeconds(waitSeconds);
            if (!TryFindTestChannel(out _))
                Debug.Log("[UIEffectSharedTest] PASS: idle channel Camera/RT/Image released after " + waitSeconds.ToString("0.0") + "s.");
            else
                Debug.LogError("[UIEffectSharedTest] FAIL: idle test channel was not released.");
        }

        private static void ValidateOneSharedResource(string phase, int expected, int loaded)
        {
            var textures = new HashSet<RenderTexture>();
            var cameras = new HashSet<Camera>();
            var images = new HashSet<UnityEngine.UI.RawImage>();
            var channelNames = new HashSet<string>();
            List<UIEffectStage.EffectDiagnostic> diagnostics = UIEffectStage.CollectDiagnostics();
            int matched = 0;
            for (int i = 0; i < diagnostics.Count; i++)
            {
                UIEffectStage.EffectDiagnostic diagnostic = diagnostics[i];
                if (diagnostic.Label != TestLabel) continue;
                matched++;
                if (diagnostic.Texture != null) textures.Add(diagnostic.Texture);
                if (!string.IsNullOrEmpty(diagnostic.Channel)) channelNames.Add(diagnostic.Channel);
            }

            List<UIEffectStage.ChannelDiagnostic> channels = UIEffectStage.CollectChannelDiagnostics();
            for (int i = 0; i < channels.Count; i++)
            {
                UIEffectStage.ChannelDiagnostic channel = channels[i];
                if (!channelNames.Contains(channel.Name)) continue;
                if (channel.Camera != null) cameras.Add(channel.Camera);
                if (channel.Image != null) images.Add(channel.Image);
            }

            bool pass = loaded == expected && matched >= expected && textures.Count == 1 &&
                        cameras.Count == 1 && images.Count == 1;
            string message = $"[UIEffectSharedTest] {phase}: loaded={loaded}/{expected} matched={matched} " +
                             $"camera={cameras.Count} rt={textures.Count} image={images.Count}";
            if (pass) Debug.Log(message + " PASS");
            else Debug.LogError(message + " FAIL");
        }

        private static bool TryFindTestChannel(out UIEffectStage.ChannelDiagnostic result)
        {
            List<UIEffectStage.ChannelDiagnostic> channels = UIEffectStage.CollectChannelDiagnostics();
            for (int i = 0; i < channels.Count; i++)
            {
                if (channels[i].UIRootName != TestRootName) continue;
                result = channels[i];
                return true;
            }
            result = default;
            return false;
        }

        private static RenderTexture FindTestChannelTexture()
        {
            return TryFindTestChannel(out UIEffectStage.ChannelDiagnostic channel) ? channel.Texture : null;
        }

        private static RectTransform CreateRoot()
        {
            // 压力测试本身走 UIEffectScope，确保复杂窗口的 opt-in 路径与普通 UILayer 通道共用同一实现。
            var go = new GameObject(TestRootName, typeof(RectTransform), typeof(Canvas), typeof(UIEffectScope));
            var root = (RectTransform)go.transform;
            root.sizeDelta = new Vector2(720f, 1280f);
            root.position = new Vector3(100000f, 100000f, 0f);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = -32000;
            return root;
        }

        private static Task<UIEffectStage.Handle> AddTestEffect(RectTransform host)
        {
            return UIEffectStage.AddByKeyAsync(TestLabel,
                GameResPath.GetUIEffectPrefabPath(TestEffect), host, Vector2.zero, Vector3.one);
        }

        private static RectTransform CreateHost(RectTransform parent, int index, int total)
        {
            var go = new GameObject("Host_" + index, typeof(RectTransform));
            var host = (RectTransform)go.transform;
            host.SetParent(parent, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.pivot = new Vector2(0.5f, 0.5f);
            host.sizeDelta = new Vector2(100f, 100f);
            int columns = 5;
            int row = index / columns;
            int column = index % columns;
            int rows = Mathf.CeilToInt(total / (float)columns);
            host.anchoredPosition = new Vector2((column - 2f) * 120f, (rows * 0.5f - row) * 120f);
            return host;
        }

        private static void AddHandles(UIEffectStage.Handle[] handles)
        {
            for (int i = 0; i < handles.Length; i++)
                if (handles[i] != null) s_handles.Add(handles[i]);
        }

        private static void DisposeHandles()
        {
            for (int i = 0; i < s_handles.Count; i++) s_handles[i]?.Dispose();
            s_handles.Clear();
        }

        private static Task WaitEditorFrames(int frameCount)
        {
            var completion = new TaskCompletionSource<bool>();
            int remaining = Mathf.Max(1, frameCount);
            void Tick()
            {
                if (--remaining > 0) return;
                EditorApplication.update -= Tick;
                completion.TrySetResult(true);
            }
            EditorApplication.update += Tick;
            return completion.Task;
        }

        private static Task WaitEditorSeconds(float seconds)
        {
            var completion = new TaskCompletionSource<bool>();
            double releaseAt = EditorApplication.timeSinceStartup + Math.Max(0.01f, seconds);
            void Tick()
            {
                if (EditorApplication.timeSinceStartup < releaseAt) return;
                EditorApplication.update -= Tick;
                completion.TrySetResult(true);
            }
            EditorApplication.update += Tick;
            return completion.Task;
        }
    }
}
