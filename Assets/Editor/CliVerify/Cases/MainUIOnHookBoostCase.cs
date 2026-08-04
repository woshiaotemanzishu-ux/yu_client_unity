using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>主界面挂机收益条“提升”按钮的 UI_tisheng 条件、循环动画、真实 RT 出帧与释放回归。</summary>
    public static class MainUIOnHookBoostCase
    {
        private const BindingFlags InstancePrivate = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;
        private const string HudPrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudOnHook.prefab";
        private const string EffectPrefabPath = "Assets/GameRes/effect/objs/ui_effect/UI_tisheng/UI_tisheng.prefab";
        private const string EffectAddress = "effect/objs/ui_effect/ui_tisheng/ui_tisheng";
        private const string EvidenceRoot = "output/ui_route_audit/2026-08-04_mainui_onhook_boost_sweep/runtime";

        private readonly struct FrameProbe
        {
            public FrameProbe(Color32[] pixels, int alphaPixels, int litPixels, string path)
            {
                Pixels = pixels;
                AlphaPixels = alphaPixels;
                LitPixels = litPixels;
                Path = path;
            }

            public Color32[] Pixels { get; }
            public int AlphaPixels { get; }
            public int LitPixels { get; }
            public string Path { get; }
        }

        public static async Task<int> Run()
        {
            bool fallbackBefore = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            CliVerify.Stage stage = null;
            GameObject instance = null;
            UIEffectStage.Handle handle = null;
            UIEffectStage.Handle reopenedHandle = null;
            bool pass = true;

            try
            {
                stage = CliVerify.Stage.Create();
                GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
                GameObject effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EffectPrefabPath);
                instance = hudPrefab != null
                    ? PrefabUtility.InstantiatePrefab(hudPrefab, stage.CanvasRoot) as GameObject
                    : null;
                MainUIOnHookView view = instance != null
                    ? instance.GetComponentInChildren<MainUIOnHookView>(true)
                    : null;
                Check("assets-and-real-prefab", hudPrefab != null && effectPrefab != null && view != null, ref pass);
                if (!pass) return 3;

                MethodInfo resolve = typeof(MainUIOnHookView).GetMethod("ResolveEffectSlots", InstancePrivate);
                MethodInfo shouldShow = typeof(MainUIOnHookView).GetMethod("ShouldShowBoostHint", StaticPrivate);
                MethodInfo apply = typeof(MainUIOnHookView).GetMethod("ApplyBoostHintEffect", InstancePrivate);
                MethodInfo clear = typeof(MainUIOnHookView).GetMethod("ClearBoostHintEffect", InstancePrivate);
                MethodInfo onHide = typeof(MainUIOnHookView).GetMethod("OnHide", InstancePrivate);
                FieldInfo slotField = typeof(MainUIOnHookView).GetField("_boostHintEffectSlot", InstancePrivate);
                FieldInfo handleField = typeof(MainUIOnHookView).GetField("_boostHintEffect", InstancePrivate);
                FieldInfo requestedField = typeof(MainUIOnHookView).GetField("_boostHintEffectRequested", InstancePrivate);
                bool reflectionReady = resolve != null && shouldShow != null && apply != null && clear != null && onHide != null
                    && slotField != null && handleField != null && requestedField != null;
                Check("business-entry", reflectionReady, ref pass);
                if (!pass) return 3;

                resolve.Invoke(view, null);
                UIEffectSlot slot = slotField.GetValue(view) as UIEffectSlot;
                bool slotReady = slot != null
                    && slot.SlotId == MainUIOnHookView.BOOST_HINT_EFFECT_SLOT_ID
                    && ResourcePath.Normalize(slot.AddressKey) == EffectAddress
                    && slot.EffectName == "UI_tisheng"
                    && slot.transform.parent == view.add
                    && Approximately(slot.Position, Vector2.zero)
                    && Approximately(slot.Scale, Vector3.one * 35f)
                    && !slot.AutoPlay;
                Check("prefab-slot-host-key-scale", slotReady, ref pass);

                bool stateGate = InvokeGate(shouldShow, false, false)
                    && !InvokeGate(shouldShow, false, true)
                    && !InvokeGate(shouldShow, true, false)
                    && !InvokeGate(shouldShow, true, true);
                Check("state-gate-not-maxed-and-no-exp-buff", stateGate, ref pass);

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetEntry entry = settings?.FindAssetEntry(AssetDatabase.AssetPathToGUID(EffectPrefabPath));
                bool addressableReady = entry != null && ResourcePath.Normalize(entry.address) == EffectAddress;
                Check("addressable-runtime-asset", addressableReady, ref pass);

                bool animationReady = VerifyLoopingSweepAsset(effectPrefab, out string animationDiagnostic);
                Debug.Log("CLIVERIFY mainui-onhook-boost animation " + animationDiagnostic);
                Check("looping-material-sweep", animationReady, ref pass);
                if (!pass) return 3;

                // Prefab 实例化会触发 BaseView 初始化与异步 Refresh；只清 Handle 仍可能被稍后完成的
                // Refresh(false) 反向覆盖。按真实隐藏生命周期注销事件并递增刷新版本，再从确定状态验证入口。
                onHide.Invoke(view, null);
                // OnInit 默认先隐藏挂机收益条，真实运行态由 Refresh(baseVisible=true) 打开；本用例已单独
                // 验证状态真值表，这里显式进入同一可见态，避免公共特效通道按隐藏父节点送到 HIDDEN_Z。
                view._box_outline_exp.gameObject.SetActive(true);
                int liveBefore = UIEffectStage.LiveCount;
                apply.Invoke(view, new object[] { true });
                Debug.Log("CLIVERIFY mainui-onhook-boost apply-state requested=" + requestedField.GetValue(view)
                    + " immediateHandle=" + (handleField.GetValue(view) != null) + " liveBefore=" + liveBefore
                    + " liveImmediate=" + UIEffectStage.LiveCount);
                // 干净 Library 首次建立编辑器 key→Prefab 缓存会扫描完整 GameRes；
                // 资源量较大时可超过 20 秒，等待窗只覆盖首次定位，不放宽出帧/动画断言。
                handle = await WaitForHandle(handleField, view, 90d);
                bool loaded = handle != null && !handle.IsDisposed && UIEffectStage.LiveCount == liveBefore + 1;
                Debug.Log("CLIVERIFY mainui-onhook-boost load-state handle=" + (handle != null)
                    + " disposed=" + (handle != null && handle.IsDisposed) + " requested=" + requestedField.GetValue(view)
                    + " liveBefore=" + liveBefore + " liveAfter=" + UIEffectStage.LiveCount);
                Check("runtime-load", loaded, ref pass);
                if (!loaded) return 3;

                // Batchmode 编辑器不会像真实客户端一样逐帧执行 UIEffectServiceRunner.Update。
                // 强制完成 UGUI 布局后推进一次公共特效通道，确保 Wrapper 坐标、RT 与可见态均来自最终页面矩形。
                Canvas.ForceUpdateCanvases();
                TickEffectStage();

                apply.Invoke(view, new object[] { true });
                await Task.Delay(50);
                bool deduplicated = ReferenceEquals(handle, handleField.GetValue(view))
                    && UIEffectStage.LiveCount == liveBefore + 1;
                Check("same-state-no-duplicate", deduplicated, ref pass);

                UIEffectStage.EffectDiagnostic diagnostic = UIEffectStage.CollectDiagnostics()
                    .Find(item => item.Label == "UI_tisheng" && item.ParentName == "ExpBoostEffectAnchor");
                bool diagnosticReady = diagnostic.EffectAlive && diagnostic.EffectActiveInHierarchy
                    && diagnostic.RendererCount >= 2 && diagnostic.ParentRectSize.x >= 68f
                    && diagnostic.ParentRectSize.y >= 30f
                    && Approximately(diagnostic.LocalScale, Vector3.one * 35f)
                    && diagnostic.Texture != null;
                Check("runtime-diagnostic", diagnosticReady, ref pass);

                GameObject effectInstance = typeof(UIEffectStage.Handle)
                    .GetField("Effect", InstancePrivate)?.GetValue(handle) as GameObject;
                Animation sweepAnimation = FindAnimation(effectInstance, "UI_tisheng");
                UIEffectStage.ChannelDiagnostic channel = FindChannel(diagnostic.Channel);
                bool renderReady = effectInstance != null && sweepAnimation != null
                    && channel.Camera != null && channel.Texture != null && channel.Image != null;
                Check("render-channel", renderReady, ref pass);
                if (!renderReady) return 3;

                LogRenderDiagnostic(effectInstance, channel.Camera, diagnostic);

                channel.Image.gameObject.SetActive(true);
                sweepAnimation.clip.SampleAnimation(sweepAnimation.gameObject, 0f);
                FrameProbe start = CaptureEffectFrame(channel.Camera, channel.Texture, "ui_tisheng_rt_start.png");
                string hudStart = stage.Capture(EvidenceRoot + "/hud_onhook_boost_start.png");
                sweepAnimation.clip.SampleAnimation(sweepAnimation.gameObject, 0.6665f);
                FrameProbe middle = CaptureEffectFrame(channel.Camera, channel.Texture, "ui_tisheng_rt_mid.png");
                string hudMiddle = stage.Capture(EvidenceRoot + "/hud_onhook_boost_mid.png");
                int changedPixels = CountChangedPixels(start.Pixels, middle.Pixels);
                bool actualFrames = start.AlphaPixels > 100 && middle.AlphaPixels > 100
                    && start.LitPixels > 100 && middle.LitPixels > 100 && changedPixels > 100;
                Debug.Log("CLIVERIFY mainui-onhook-boost render startAlpha=" + start.AlphaPixels
                    + " midAlpha=" + middle.AlphaPixels + " startLit=" + start.LitPixels
                    + " midLit=" + middle.LitPixels + " changed=" + changedPixels
                    + " rawStart=" + start.Path + " rawMid=" + middle.Path
                    + " hudStart=" + hudStart + " hudMid=" + hudMiddle);
                Check("actual-rt-frames-and-motion", actualFrames, ref pass);

                clear.Invoke(view, null);
                bool released = handle.IsDisposed && handleField.GetValue(view) == null
                    && UIEffectStage.LiveCount == liveBefore;
                Check("hide-release", released, ref pass);
                handle = null;

                apply.Invoke(view, new object[] { true });
                reopenedHandle = await WaitForHandle(handleField, view, 90d);
                bool reopened = reopenedHandle != null && !reopenedHandle.IsDisposed
                    && UIEffectStage.LiveCount == liveBefore + 1;
                clear.Invoke(view, null);
                bool reopenedReleased = reopenedHandle != null && reopenedHandle.IsDisposed
                    && handleField.GetValue(view) == null && UIEffectStage.LiveCount == liveBefore;
                Check("close-reopen-and-release", reopened && reopenedReleased, ref pass);
                reopenedHandle = null;

                Debug.Log("CLIVERIFY mainui-onhook-boost VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY mainui-onhook-boost EXCEPTION " + exception);
                return 3;
            }
            finally
            {
                handle?.Dispose();
                reopenedHandle?.Dispose();
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                stage?.Dispose();
                ResManager.EditorPreferFallback = fallbackBefore;
            }
        }

        private static bool VerifyLoopingSweepAsset(GameObject effectPrefab, out string diagnostic)
        {
            diagnostic = "missing";
            if (effectPrefab == null) return false;
            Animation sweep = FindAnimation(effectPrefab, "UI_tisheng");
            Animation pulse = FindAnimation(effectPrefab, "UI_tisheng01");
            if (sweep?.clip == null || pulse?.clip == null) return false;

            EditorCurveBinding sweepBinding = default;
            bool foundBinding = false;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sweep.clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].propertyName != "material._BaseMap_ST.z") continue;
                sweepBinding = bindings[i];
                foundBinding = true;
                break;
            }
            AnimationCurve curve = foundBinding ? AnimationUtility.GetEditorCurve(sweep.clip, sweepBinding) : null;
            bool sweepCurve = curve != null && curve.length >= 2
                && Mathf.Abs(curve.keys[0].value - 1f) < 0.001f
                && Mathf.Abs(curve.keys[curve.length - 1].value) < 0.001f
                && Mathf.Abs(curve.keys[curve.length - 1].time - 1.333f) < 0.002f;
            bool looped = sweep.clip.legacy && pulse.clip.legacy
                && sweep.clip.wrapMode == WrapMode.Loop && pulse.clip.wrapMode == WrapMode.Loop;
            MeshRenderer sweepRenderer = sweep.GetComponent<MeshRenderer>();
            Material sweepMaterial = sweepRenderer != null ? sweepRenderer.sharedMaterial : null;
            bool baseMapUvEnabled = sweepMaterial != null && sweepMaterial.HasProperty("_UseBaseMapST")
                && sweepMaterial.GetFloat("_UseBaseMapST") > 0.5f;
            diagnostic = "sweep=" + sweep.clip.name + "/" + sweep.clip.length.ToString("F3")
                + " pulse=" + pulse.clip.name + "/" + pulse.clip.length.ToString("F3")
                + " looped=" + looped + " materialOffset=" + sweepCurve
                + " baseMapUv=" + baseMapUvEnabled;
            return looped && sweepCurve && baseMapUvEnabled;
        }

        private static Animation FindAnimation(GameObject root, string clipName)
        {
            if (root == null) return null;
            Animation[] animations = root.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < animations.Length; i++)
            {
                if (animations[i] != null && animations[i].clip != null
                    && animations[i].clip.name == clipName)
                    return animations[i];
            }
            return null;
        }

        private static UIEffectStage.ChannelDiagnostic FindChannel(string channelName)
        {
            return UIEffectStage.CollectChannelDiagnostics().Find(item => item.Name == channelName);
        }

        private static void TickEffectStage()
        {
            MethodInfo tick = typeof(UIEffectStage).GetMethod("Tick", StaticPrivate);
            tick?.Invoke(null, null);
        }

        private static void LogRenderDiagnostic(GameObject effect, Camera camera,
            UIEffectStage.EffectDiagnostic diagnostic)
        {
            Debug.Log("CLIVERIFY mainui-onhook-boost render-diagnostic channel=" + diagnostic.Channel
                + " parentRect=" + diagnostic.ParentRectSize + " effectScale=" + diagnostic.LocalScale
                + " effectBounds=" + diagnostic.WorldBoundsSize + " camera=" + diagnostic.CameraWorldPos
                + " ortho=" + diagnostic.CameraOrthoSize + " rt=" + diagnostic.RtWidth + "x" + diagnostic.RtHeight);
            if (effect == null || camera == null) return;

            Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                Material material = renderer.sharedMaterial;
                Vector3 viewport = camera.WorldToViewportPoint(renderer.bounds.center);
                Debug.Log("CLIVERIFY mainui-onhook-boost renderer[" + i + "] name=" + renderer.name
                    + " active=" + renderer.gameObject.activeInHierarchy + " enabled=" + renderer.enabled
                    + " forceOff=" + renderer.forceRenderingOff + " layer=" + renderer.gameObject.layer
                    + " bounds=" + renderer.bounds.center + "/" + renderer.bounds.size
                    + " viewport=" + viewport + " shader=" + (material?.shader != null ? material.shader.name : "<null>")
                    + " supported=" + (material?.shader != null && material.shader.isSupported)
                    + " passCount=" + (material != null ? material.passCount : 0)
                    + " texture=" + (material?.mainTexture != null ? material.mainTexture.name : "<null>"));
            }
        }

        private static async Task<UIEffectStage.Handle> WaitForHandle(FieldInfo field, object target, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                UIEffectStage.Handle handle = field.GetValue(target) as UIEffectStage.Handle;
                if (handle != null) return handle;
                await Task.Delay(50);
            }
            return null;
        }

        private static FrameProbe CaptureEffectFrame(Camera camera, RenderTexture target, string fileName)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            Color32[] pixels = texture.GetPixels32();
            int alphaPixels = 0;
            int litPixels = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 2) alphaPixels++;
                if (pixel.r > 2 || pixel.g > 2 || pixel.b > 2) litPixels++;
            }

            string path = Path.GetFullPath(EvidenceRoot + "/" + fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return new FrameProbe(pixels, alphaPixels, litPixels, path);
        }

        private static int CountChangedPixels(Color32[] left, Color32[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return 0;
            int changed = 0;
            for (int i = 0; i < left.Length; i++)
            {
                Color32 a = left[i];
                Color32 b = right[i];
                int difference = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g)
                    + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
                if (difference > 8) changed++;
            }
            return changed;
        }

        private static bool InvokeGate(MethodInfo method, bool maxed, bool hasExperienceBuff)
        {
            return method != null && (bool)method.Invoke(null, new object[] { maxed, hasExperienceBuff });
        }

        private static bool Approximately(Vector2 actual, Vector2 expected) =>
            (actual - expected).sqrMagnitude <= 0.000001f;

        private static bool Approximately(Vector3 actual, Vector3 expected) =>
            (actual - expected).sqrMagnitude <= 0.000001f;

        private static void Check(string tag, bool ok, ref bool pass)
        {
            Debug.Log("CLIVERIFY mainui-onhook-boost " + tag + " ok=" + ok);
            if (!ok) pass = false;
        }
    }
}
