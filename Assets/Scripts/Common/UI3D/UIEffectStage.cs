using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// UI 3D 特效统一入口。公共 API 保持旧调用方式，内部按 UI 层和渲染带共享 Camera/RT/RawImage。
    /// </summary>
    public static class UIEffectStage
    {
        private const float REFERENCE_STAGE_HEIGHT = 1280f;
        private const float LAYA_STAGE_TO_WORLD = 0.01f;
        private const float CAMERA_Z = -10f;
        private const float HIDDEN_Z = 2048f;
        private const float STAGE_DEPTH_SPACING = 4096f;
        private const int EFFECT_LAYER = 31;
        private static readonly int EffectLayerMask = 1 << EFFECT_LAYER;

        private static readonly List<Handle> s_live = new List<Handle>();
        private static readonly Dictionary<ChannelKey, Channel> s_channels = new Dictionary<ChannelKey, Channel>();
        private static readonly List<ChannelKey> s_removeChannelKeys = new List<ChannelKey>();
        private static readonly List<string> s_recentFailures = new List<string>();
        private static readonly List<string> s_notes = new List<string>();
        private static Camera[] s_cameraBuffer = new Camera[16];
        private static UIEffectServiceRunner s_runner;
        private static Material s_additiveImageMaterial;
        private static bool s_additiveShaderMissingLogged;
        private static bool s_runtimeInitialized;
        private static bool s_shuttingDown;

        internal readonly struct ChannelKey : IEquatable<ChannelKey>
        {
            public readonly int RootInstanceId;
            public readonly UIEffectBand Band;

            public ChannelKey(int rootInstanceId, UIEffectBand band)
            {
                RootInstanceId = rootInstanceId;
                Band = band;
            }

            public bool Equals(ChannelKey other)
            {
                return RootInstanceId == other.RootInstanceId && Band == other.Band;
            }

            public override bool Equals(object obj)
            {
                return obj is ChannelKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked { return (RootInstanceId * 397) ^ (int)Band; }
            }
        }

        internal sealed class Channel
        {
            public ChannelKey Key;
            public string Name;
            public RectTransform UIRoot;
            public UIEffectBand Band;
            public GameObject StageRoot;
            public Transform EffectRoot;
            public Camera Camera;
            public RawImage Image;
            public RenderTexture Texture;
            public readonly List<Handle> Handles = new List<Handle>();
            public Vector2 LastUISize;
            public float LastRenderScale;
            public float IdleSince = -1f;

            public bool IsAlive => UIRoot != null && StageRoot != null && Camera != null && Image != null;
        }

        public sealed class Handle
        {
            internal Channel SharedChannel;
            internal Transform Wrapper;
            internal GameObject Effect;
            internal string Label;
            internal string Key;
            internal RectTransform Parent;
            internal Vector2 Position;
            internal Vector3 Scale;
            internal float RotationY;
            internal Vector2 RenderSize;
            internal UIEffectProfile Profile;
            internal bool Visible;
            internal bool Loading = true;
            private bool _disposed;

            public bool IsDisposed => _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                ReleaseHandle(this);
            }
        }

        public static async Task<Handle> AddAsync(string effectName, RectTransform parent,
            Vector2 position = default, Vector3 scale = default, float rotationY = 0f,
            Vector2 renderSize = default)
        {
            if (string.IsNullOrEmpty(effectName) || parent == null) return null;
            return await AddByKeyInternalAsync(effectName, GameResPath.GetUIEffectPrefabPath(effectName),
                parent, position, scale, rotationY, renderSize, null);
        }

        public static async Task<Handle> AddByKeyAsync(string label, string effectKey, RectTransform parent,
            Vector2 position = default, Vector3 scale = default, float rotationY = 0f,
            Vector2 renderSize = default, string profileId = null)
        {
            return await AddByKeyInternalAsync(label, effectKey, parent, position, scale, rotationY,
                renderSize, profileId);
        }

        public static Task<Handle> AddAsync(UIEffectSlot slot, RectTransform parent)
        {
            if (slot == null) return Task.FromResult<Handle>(null);
            return AddByKeyInternalAsync(slot.EffectName, slot.AddressKey, parent,
                slot.Position, slot.Scale, slot.RotationY, default, slot.ProfileId);
        }

        private static async Task<Handle> AddByKeyInternalAsync(string label, string effectKey,
            RectTransform parent, Vector2 position, Vector3 scale, float rotationY,
            Vector2 renderSize, string profileId)
        {
            if (string.IsNullOrEmpty(effectKey) || parent == null) return null;
            if (string.IsNullOrEmpty(label)) label = effectKey;
            if (scale == default) scale = Vector3.one;

            EnsureRuntime();
            UIEffectProfileCatalog catalog = UIEffectProfileCatalog.Runtime;
            UIEffectProfile profile = catalog.Resolve(label, profileId);
            RectTransform channelRoot = ResolveChannelRoot(parent, profile.channel);
            if (channelRoot == null)
            {
                RecordFailure(label, effectKey, "no Canvas/UILayer root found for parent");
                return null;
            }

            Channel channel = GetOrCreateChannel(channelRoot, profile.band);
            if (channel == null) return null;

            var wrapperObject = new GameObject("Effect_" + SafeName(label));
            wrapperObject.layer = EFFECT_LAYER;
            Transform wrapper = wrapperObject.transform;
            wrapper.SetParent(channel.EffectRoot, false);

            var handle = new Handle
            {
                SharedChannel = channel,
                Wrapper = wrapper,
                Label = label,
                Key = effectKey,
                Parent = parent,
                Position = position,
                Scale = scale,
                RotationY = rotationY,
                RenderSize = renderSize,
                Profile = profile
            };

            channel.Handles.Add(handle);
            channel.IdleSince = -1f;
            s_live.Add(handle);
            UpdateHandleTransform(handle);

            GameObject effect = await ResManager.InstantiateAsync(effectKey, wrapper);
            if (handle.IsDisposed || effect == null || parent == null)
            {
                if (effect == null)
                {
                    GameLog.Warn("UIEffect", "load ui effect failed: label={0} key={1}", label, effectKey);
                    RecordFailure(label, effectKey, "ResManager.InstantiateAsync returned null (key not loadable)");
                }
                else if (parent == null)
                {
                    RecordFailure(label, effectKey, "parent destroyed before effect ready");
                }

                if (effect != null) ResManager.ReleaseInstance(effect);
                if (!handle.IsDisposed) handle.Dispose();
                return null;
            }

            handle.Effect = effect;
            handle.Loading = false;
            effect.name = "__ui_effect_" + SafeName(label);
            SetLayerRecursive(effect, EFFECT_LAYER);

            Vector2 finalPosition = position + profile.positionOffset;
            Vector3 finalScale = Vector3.Scale(scale, profile.SafeScaleMultiplier);
            if (profile.mirrorX) finalScale.x = -finalScale.x;
            Transform effectTransform = effect.transform;
            effectTransform.localPosition = new Vector3(-finalPosition.x, -finalPosition.y, 0f);
            effectTransform.localRotation = Quaternion.Euler(0f, rotationY + profile.rotationYOffset, 0f);
            effectTransform.localScale = finalScale;

            ApplyRenderDefaults(effect);
            Play(effect);
            effect.SetActive(true);
            UpdateHandleTransform(handle);
            return handle;
        }

        private static Channel GetOrCreateChannel(RectTransform uiRoot, UIEffectBand band)
        {
            var key = new ChannelKey(uiRoot.GetInstanceID(), band);
            if (s_channels.TryGetValue(key, out Channel existing) && existing.IsAlive)
                return existing;

            UIEffectProfileCatalog settings = UIEffectProfileCatalog.Runtime;
            int stageSlot = AcquireStageSlot();
            string channelName = SafeName(uiRoot.name) + "_" + band;

            var stageRoot = new GameObject("__UIEffectChannelStage_" + channelName);
            stageRoot.layer = EFFECT_LAYER;
            stageRoot.transform.SetParent(s_runner.transform, false);
            stageRoot.transform.position = new Vector3(6000f, -6000f,
                6000f + stageSlot * STAGE_DEPTH_SPACING);

            UIEffectStageCameraGuard guard = stageRoot.AddComponent<UIEffectStageCameraGuard>();
            guard.StageSlot = stageSlot;
            guard.ChannelKey = channelName;

            var effectRoot = new GameObject("Effects").transform;
            effectRoot.SetParent(stageRoot.transform, false);
            effectRoot.gameObject.layer = EFFECT_LAYER;

            var cameraObject = new GameObject("Camera");
            cameraObject.layer = EFFECT_LAYER;
            cameraObject.transform.SetParent(stageRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.cullingMask = EffectLayerMask;
            camera.useOcclusionCulling = false;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.enabled = false;
            guard.Owner = camera;

            var imageObject = new GameObject("__UIEffectChannelImage_" + channelName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage),
                typeof(UIEffectChannelImageMarker));
            RectTransform imageTransform = (RectTransform)imageObject.transform;
            imageTransform.SetParent(uiRoot, false);
            imageTransform.anchorMin = Vector2.zero;
            imageTransform.anchorMax = Vector2.one;
            imageTransform.offsetMin = Vector2.zero;
            imageTransform.offsetMax = Vector2.zero;
            imageTransform.localScale = Vector3.one;

            RawImage image = imageObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.uvRect = new Rect(1f, 0f, -1f, 1f);
            Material additive = GetAdditiveImageMaterial();
            if (additive != null) image.material = additive;
            imageObject.GetComponent<UIEffectChannelImageMarker>().ChannelKey = channelName;
            imageObject.SetActive(false);

            var channel = new Channel
            {
                Key = key,
                Name = channelName,
                UIRoot = uiRoot,
                Band = band,
                StageRoot = stageRoot,
                EffectRoot = effectRoot,
                Camera = camera,
                Image = image,
                LastRenderScale = settings.renderScale
            };
            s_channels[key] = channel;
            RefreshChannelRenderTarget(channel, true);
            ExcludeStageLayerFromOtherCameras(camera);
            return channel;
        }

        internal static void Tick()
        {
            if (!s_runtimeInitialized || s_shuttingDown) return;

            for (int i = s_live.Count - 1; i >= 0; i--)
            {
                Handle handle = s_live[i];
                if (handle == null || handle.IsDisposed) continue;
                if (handle.Parent == null || handle.SharedChannel == null || !handle.SharedChannel.IsAlive)
                {
                    handle.Dispose();
                    continue;
                }
                UpdateHandleTransform(handle);
            }

            UIEffectProfileCatalog settings = UIEffectProfileCatalog.Runtime;
            s_removeChannelKeys.Clear();
            foreach (KeyValuePair<ChannelKey, Channel> pair in s_channels)
            {
                Channel channel = pair.Value;
                if (channel == null || !channel.IsAlive)
                {
                    s_removeChannelKeys.Add(pair.Key);
                    continue;
                }

                RefreshChannelRenderTarget(channel, false);
                MaintainImageOrder(channel);

                bool hasVisibleEffect = false;
                for (int h = 0; h < channel.Handles.Count; h++)
                {
                    Handle handle = channel.Handles[h];
                    if (handle != null && !handle.IsDisposed && !handle.Loading && handle.Effect != null && handle.Visible)
                    {
                        hasVisibleEffect = true;
                        break;
                    }
                }

                channel.Camera.enabled = hasVisibleEffect;
                if (channel.Image.gameObject.activeSelf != hasVisibleEffect)
                    channel.Image.gameObject.SetActive(hasVisibleEffect);

                if (channel.Handles.Count == 0)
                {
                    if (channel.IdleSince < 0f) channel.IdleSince = Time.unscaledTime;
                    if (Time.unscaledTime - channel.IdleSince >= settings.idleReleaseSeconds)
                        s_removeChannelKeys.Add(pair.Key);
                }
                else
                {
                    channel.IdleSince = -1f;
                }
            }

            for (int i = 0; i < s_removeChannelKeys.Count; i++)
            {
                ChannelKey key = s_removeChannelKeys[i];
                if (!s_channels.TryGetValue(key, out Channel channel)) continue;
                DestroyChannel(channel);
                s_channels.Remove(key);
            }
            s_removeChannelKeys.Clear();
        }

        private static void UpdateHandleTransform(Handle handle)
        {
            Channel channel = handle.SharedChannel;
            RectTransform parent = handle.Parent;
            if (handle.Wrapper == null || channel == null || channel.UIRoot == null || parent == null)
                return;

            bool visible = parent.gameObject.activeInHierarchy && HasVisibleCanvasGroups(parent);
            handle.Visible = visible;

            float channelHeight = Mathf.Max(1f, channel.UIRoot.rect.height);
            float stageHeight = GetStageHeight(channel.UIRoot);
            float pixelsPerWorld = channelHeight / Mathf.Max(0.01f, stageHeight * LAYA_STAGE_TO_WORLD);

            Vector3 centerWorld = parent.TransformPoint(parent.rect.center);
            Vector3 centerInChannel = channel.UIRoot.InverseTransformPoint(centerWorld);
            Vector3 rightInChannel = channel.UIRoot.InverseTransformVector(parent.TransformVector(Vector3.right));
            Vector3 upInChannel = channel.UIRoot.InverseTransformVector(parent.TransformVector(Vector3.up));

            float sourceHeight = handle.RenderSize.y > 1f
                ? handle.RenderSize.y
                : Mathf.Max(1f, parent.rect.height);
            float instanceFactor = sourceHeight / channelHeight;
            float relativeScaleX = Mathf.Max(0.0001f, rightInChannel.magnitude);
            float relativeScaleY = Mathf.Max(0.0001f, upInChannel.magnitude);
            float relativeScaleZ = (relativeScaleX + relativeScaleY) * 0.5f;
            float angle = Mathf.Atan2(rightInChannel.y, rightInChannel.x) * Mathf.Rad2Deg;

            handle.Wrapper.localPosition = new Vector3(
                -centerInChannel.x / pixelsPerWorld,
                centerInChannel.y / pixelsPerWorld,
                visible ? 0f : HIDDEN_Z);
            handle.Wrapper.localRotation = Quaternion.Euler(0f, 0f, -angle);
            handle.Wrapper.localScale = new Vector3(
                instanceFactor * relativeScaleX,
                instanceFactor * relativeScaleY,
                instanceFactor * relativeScaleZ);
        }

        private static bool HasVisibleCanvasGroups(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.TryGetComponent(out CanvasGroup group))
                {
                    if (group.alpha <= 0.001f) return false;
                    if (group.ignoreParentGroups) break;
                }
                current = current.parent;
            }
            return true;
        }

        private static RectTransform ResolveChannelRoot(RectTransform parent, UIEffectChannelOverride channelOverride)
        {
            if (channelOverride != UIEffectChannelOverride.Auto)
            {
                Transform requested = ViewManager.GetLayer(ToUILayer(channelOverride));
                if (requested is RectTransform requestedRect) return requestedRect;
            }

            // 复杂窗口可在 Prefab 上加 UIEffectScope，让同一窗口里的特效共享通道，同时保留
            // 窗口自身在 UILayer 中的 sibling 排序。普通界面无需配置，仍自动落到所属 UILayer。
            UIEffectScope scope = parent.GetComponentInParent<UIEffectScope>();
            if (scope != null && scope.ChannelRoot != null)
                return scope.ChannelRoot;

            Array values = Enum.GetValues(typeof(UILayer));
            for (int i = values.Length - 1; i >= 0; i--)
            {
                Transform root = ViewManager.GetLayer((UILayer)values.GetValue(i));
                if (root != null && (parent == root || parent.IsChildOf(root)))
                    return root as RectTransform;
            }

            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
                return canvas.rootCanvas.transform as RectTransform;
            return canvas != null ? canvas.transform as RectTransform : null;
        }

        private static UILayer ToUILayer(UIEffectChannelOverride value)
        {
            return value switch
            {
                UIEffectChannelOverride.Scene => UILayer.Scene,
                UIEffectChannelOverride.Main => UILayer.Main,
                UIEffectChannelOverride.Window => UILayer.Window,
                UIEffectChannelOverride.Popup => UILayer.Popup,
                UIEffectChannelOverride.Tip => UILayer.Tip,
                UIEffectChannelOverride.Loading => UILayer.Loading,
                UIEffectChannelOverride.Top => UILayer.Top,
                _ => UILayer.Main
            };
        }

        private static void RefreshChannelRenderTarget(Channel channel, bool force)
        {
            if (channel == null || channel.UIRoot == null) return;
            UIEffectProfileCatalog settings = UIEffectProfileCatalog.Runtime;
            Vector2 uiSize = GetPositiveSize(channel.UIRoot.rect.size);
            if (uiSize == default)
            {
                Canvas canvas = channel.UIRoot.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.pixelRect.width > 1f && canvas.pixelRect.height > 1f)
                    uiSize = canvas.pixelRect.size;
                else
                    uiSize = new Vector2(720f, REFERENCE_STAGE_HEIGHT);
            }

            float rtScale = Mathf.Min(settings.renderScale,
                settings.maxRenderTextureSize / Mathf.Max(1f, uiSize.x),
                settings.maxRenderTextureSize / Mathf.Max(1f, uiSize.y));
            rtScale = Mathf.Max(0.01f, rtScale);
            int width = Mathf.Max(settings.minRenderTextureSize, Mathf.CeilToInt(uiSize.x * rtScale));
            int height = Mathf.Max(settings.minRenderTextureSize, Mathf.CeilToInt(uiSize.y * rtScale));

            channel.Camera.orthographicSize = GetStageHeight(channel.UIRoot) * LAYA_STAGE_TO_WORLD * 0.5f;
            if (!force && channel.Texture != null && channel.Texture.width == width &&
                channel.Texture.height == height && Mathf.Approximately(channel.LastRenderScale, settings.renderScale))
                return;

            RenderTexture oldTexture = channel.Texture;
            var texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = "__UIEffectChannelRT_" + channel.Name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            ClearRenderTexture(texture);
            channel.Texture = texture;
            channel.Camera.targetTexture = texture;
            channel.Image.texture = texture;
            channel.LastUISize = uiSize;
            channel.LastRenderScale = settings.renderScale;

            if (oldTexture != null)
            {
                oldTexture.Release();
                DestroyObject(oldTexture);
            }
        }

        private static void MaintainImageOrder(Channel channel)
        {
            if (channel?.Image == null) return;
            if (channel.Band == UIEffectBand.Underlay)
                channel.Image.rectTransform.SetAsFirstSibling();
            else
                channel.Image.rectTransform.SetAsLastSibling();
        }

        private static void ReleaseHandle(Handle handle)
        {
            s_live.Remove(handle);
            Channel channel = handle.SharedChannel;
            if (channel != null)
            {
                channel.Handles.Remove(handle);
                if (channel.Handles.Count == 0) channel.IdleSince = Time.unscaledTime;
            }

            if (handle.Effect != null) ResManager.ReleaseInstance(handle.Effect);
            if (handle.Wrapper != null) DestroyObject(handle.Wrapper.gameObject);
            handle.Effect = null;
            handle.Wrapper = null;
            handle.Parent = null;
            handle.SharedChannel = null;
        }

        private static void DestroyChannel(Channel channel)
        {
            if (channel == null) return;
            if (channel.Handles.Count > 0)
            {
                Handle[] handles = channel.Handles.ToArray();
                for (int i = 0; i < handles.Length; i++) handles[i]?.Dispose();
            }

            if (channel.Camera != null) channel.Camera.targetTexture = null;
            if (channel.Image != null) DestroyObject(channel.Image.gameObject);
            if (channel.Texture != null)
            {
                channel.Texture.Release();
                DestroyObject(channel.Texture);
            }
            if (channel.StageRoot != null) DestroyObject(channel.StageRoot);
            channel.Texture = null;
            channel.Image = null;
            channel.Camera = null;
            channel.StageRoot = null;
            channel.EffectRoot = null;
            channel.UIRoot = null;
        }

        private static void EnsureRuntime()
        {
            if (s_runner != null && s_runtimeInitialized) return;
            UIEffectServiceRunner existing = UnityEngine.Object.FindFirstObjectByType<UIEffectServiceRunner>(
                FindObjectsInactive.Include);
            if (existing == null)
            {
                var runnerObject = new GameObject("__UIEffectService");
                existing = runnerObject.AddComponent<UIEffectServiceRunner>();
                UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            }
            AttachRunner(existing);
        }

        internal static void AttachRunner(UIEffectServiceRunner runner)
        {
            if (runner == null) return;
            s_runner = runner;
            if (s_runtimeInitialized) return;
            CleanupOrphanedLegacyObjects();
            s_runtimeInitialized = true;
            s_shuttingDown = false;
        }

        internal static void DetachRunner(UIEffectServiceRunner runner)
        {
            if (runner == null || runner != s_runner || s_shuttingDown) return;
            Shutdown();
        }

        private static void Shutdown()
        {
            s_shuttingDown = true;
            Handle[] handles = s_live.ToArray();
            for (int i = 0; i < handles.Length; i++) handles[i]?.Dispose();
            foreach (Channel channel in s_channels.Values) DestroyChannel(channel);
            s_channels.Clear();
            s_live.Clear();
            s_removeChannelKeys.Clear();

            if (s_additiveImageMaterial != null) DestroyObject(s_additiveImageMaterial);
            s_additiveImageMaterial = null;
            s_runner = null;
            s_runtimeInitialized = false;
            s_shuttingDown = false;
        }

        private static void CleanupOrphanedLegacyObjects()
        {
            UIEffectStageCameraGuard[] guards = UnityEngine.Object.FindObjectsByType<UIEffectStageCameraGuard>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < guards.Length; i++)
            {
                UIEffectStageCameraGuard guard = guards[i];
                if (guard != null) DestroyObject(guard.gameObject);
            }

            RawImage[] images = UnityEngine.Object.FindObjectsByType<RawImage>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                RawImage image = images[i];
                if (image == null) continue;
                if (image.name.StartsWith("__UIEffectImage_", StringComparison.Ordinal) ||
                    image.name.StartsWith("__UIEffectChannelImage_", StringComparison.Ordinal))
                    DestroyObject(image.gameObject);
            }
        }

        private static int AcquireStageSlot()
        {
            UIEffectStageCameraGuard[] guards = UnityEngine.Object.FindObjectsByType<UIEffectStageCameraGuard>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var occupied = new HashSet<int>();
            for (int i = 0; i < guards.Length; i++)
            {
                UIEffectStageCameraGuard guard = guards[i];
                if (guard != null && guard.StageSlot > 0) occupied.Add(guard.StageSlot);
            }
            int slot = 1;
            while (occupied.Contains(slot)) slot++;
            return slot;
        }

        private static float GetStageHeight(RectTransform parent)
        {
            Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.referenceResolution.y > 1f)
                    return scaler.referenceResolution.y;
            }
            RectTransform root = canvas != null ? canvas.transform as RectTransform : null;
            if (root != null && root.rect.height > 1f) return root.rect.height;
            if (canvas != null && canvas.pixelRect.height > 1f) return canvas.pixelRect.height;
            return REFERENCE_STAGE_HEIGHT;
        }

        private static Vector2 GetPositiveSize(Vector2 size)
        {
            if (size.x <= 1f || size.y <= 1f) return default;
            return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
        }

        private static void ApplyRenderDefaults(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            Transform transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetLayerRecursive(transform.GetChild(i).gameObject, layer);
        }

        internal static void ExcludeStageLayerFromOtherCameras(Camera owner)
        {
            int count = Camera.allCamerasCount;
            if (s_cameraBuffer.Length < count) s_cameraBuffer = new Camera[count];
            int written = Camera.GetAllCameras(s_cameraBuffer);
            for (int i = 0; i < written; i++)
            {
                Camera camera = s_cameraBuffer[i];
                if (camera == null || camera == owner) continue;
                if (camera.GetComponentInParent<UIEffectStageCameraGuard>() != null) continue;
                if ((camera.cullingMask & EffectLayerMask) != 0)
                    camera.cullingMask &= ~EffectLayerMask;
            }
        }

        private static void Play(GameObject go)
        {
            ParticleSystem[] particles = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }

            Animation[] animations = go.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < animations.Length; i++)
            {
                Animation animation = animations[i];
                if (!animation.enabled) continue;
                if (animation.clip != null)
                {
                    animation.Play();
                    continue;
                }
                foreach (AnimationState state in animation)
                {
                    animation.Play(state.name);
                    break;
                }
            }
        }

        private static void ClearRenderTexture(RenderTexture texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
        }

        private static Material GetAdditiveImageMaterial()
        {
            if (s_additiveImageMaterial != null) return s_additiveImageMaterial;
            Shader shader = Shader.Find("Shenxiao/UI/UIEffectAdditive");
            if (shader == null)
            {
                if (!s_additiveShaderMissingLogged)
                {
                    s_additiveShaderMissingLogged = true;
                    GameLog.Warn("UIEffect", "additive UI shader missing: Shenxiao/UI/UIEffectAdditive");
                    Note("additive UI shader missing -> effects may be invisible");
                }
                return null;
            }
            s_additiveImageMaterial = new Material(shader) { name = "UIEffectAdditive(Runtime)" };
            return s_additiveImageMaterial;
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "effect";
            return value.Replace('\\', '_').Replace('/', '_').Replace('.', '_').Replace(' ', '_');
        }

        private static void RecordFailure(string label, string key, string reason)
        {
            s_recentFailures.Add(string.Format("{0} | key={1} | {2}", label, key, reason));
            const int keep = 32;
            if (s_recentFailures.Count > keep)
                s_recentFailures.RemoveRange(0, s_recentFailures.Count - keep);
        }

        public struct EffectDiagnostic
        {
            public string Label;
            public string Key;
            public string Channel;
            public int ChannelHandleCount;
            public bool SharedRenderResources;
            public bool EffectAlive;
            public bool EffectActiveInHierarchy;
            public Vector3 LocalScale;
            public int ParticleSystemCount;
            public int AliveParticleCount;
            public bool AnyParticlePlaying;
            public int RendererCount;
            public bool AnyRendererVisible;
            public string FirstShader;
            public Vector3 WorldBoundsSize;
            public string ParentName;
            public bool ParentActiveInHierarchy;
            public Vector2 ParentRectSize;
            public int RtWidth;
            public int RtHeight;
            public bool CameraEnabled;
            public float CameraOrthoSize;
            public Vector3 CameraWorldPos;
            public bool ImageAlive;
            public bool ImageActiveInHierarchy;
            public Vector2 ImageRectSize;
            public Color ImageColor;
            public bool ImageHasTexture;
            public RenderTexture Texture;
        }

        public struct ChannelDiagnostic
        {
            public string Name;
            public UIEffectBand Band;
            public string UIRootName;
            public int HandleCount;
            public bool CameraEnabled;
            public int RtWidth;
            public int RtHeight;
            public Camera Camera;
            public RenderTexture Texture;
            public RawImage Image;
        }

        public static int LiveCount => s_live.Count;
        public static int ChannelCount => s_channels.Count;

        public static List<string> CollectRecentFailures()
        {
            return new List<string>(s_recentFailures);
        }

        public static void Note(string message)
        {
            s_notes.Add(message);
            const int keep = 256;
            if (s_notes.Count > keep) s_notes.RemoveRange(0, s_notes.Count - keep);
        }

        public static List<string> CollectNotes()
        {
            return new List<string>(s_notes);
        }

        public static void ClearNotes()
        {
            s_notes.Clear();
        }

        public static List<EffectDiagnostic> CollectDiagnostics()
        {
            var list = new List<EffectDiagnostic>(s_live.Count);
            for (int i = 0; i < s_live.Count; i++)
            {
                Handle handle = s_live[i];
                if (handle == null) continue;
                Channel channel = handle.SharedChannel;
                var diagnostic = new EffectDiagnostic
                {
                    Label = handle.Label,
                    Key = handle.Key,
                    LocalScale = handle.Scale,
                    Channel = channel?.Name,
                    ChannelHandleCount = channel?.Handles.Count ?? 0,
                    SharedRenderResources = true
                };

                GameObject effect = handle.Effect;
                diagnostic.EffectAlive = effect != null;
                if (effect != null)
                {
                    diagnostic.EffectActiveInHierarchy = effect.activeInHierarchy;
                    diagnostic.LocalScale = effect.transform.localScale;
                    ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
                    diagnostic.ParticleSystemCount = systems.Length;
                    for (int p = 0; p < systems.Length; p++)
                    {
                        if (systems[p] == null) continue;
                        diagnostic.AliveParticleCount += systems[p].particleCount;
                        diagnostic.AnyParticlePlaying |= systems[p].isPlaying;
                    }

                    Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);
                    diagnostic.RendererCount = renderers.Length;
                    Bounds bounds = default;
                    bool hasBounds = false;
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        Renderer renderer = renderers[r];
                        if (renderer == null) continue;
                        diagnostic.AnyRendererVisible |= renderer.isVisible;
                        if (diagnostic.FirstShader == null && renderer.sharedMaterial?.shader != null)
                            diagnostic.FirstShader = renderer.sharedMaterial.shader.name;
                        if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                    diagnostic.WorldBoundsSize = hasBounds ? bounds.size : Vector3.zero;
                }

                if (handle.Parent != null)
                {
                    diagnostic.ParentName = handle.Parent.name;
                    diagnostic.ParentActiveInHierarchy = handle.Parent.gameObject.activeInHierarchy;
                    diagnostic.ParentRectSize = handle.Parent.rect.size;
                }

                if (channel?.Texture != null)
                {
                    diagnostic.RtWidth = channel.Texture.width;
                    diagnostic.RtHeight = channel.Texture.height;
                    diagnostic.Texture = channel.Texture;
                }
                if (channel?.Camera != null)
                {
                    diagnostic.CameraEnabled = channel.Camera.enabled;
                    diagnostic.CameraOrthoSize = channel.Camera.orthographicSize;
                    diagnostic.CameraWorldPos = channel.Camera.transform.position;
                }
                if (channel?.Image != null)
                {
                    diagnostic.ImageAlive = true;
                    diagnostic.ImageActiveInHierarchy = channel.Image.gameObject.activeInHierarchy;
                    diagnostic.ImageRectSize = channel.Image.rectTransform.rect.size;
                    diagnostic.ImageColor = channel.Image.color;
                    diagnostic.ImageHasTexture = channel.Image.texture != null;
                }
                list.Add(diagnostic);
            }
            return list;
        }

        public static List<ChannelDiagnostic> CollectChannelDiagnostics()
        {
            var list = new List<ChannelDiagnostic>(s_channels.Count);
            foreach (Channel channel in s_channels.Values)
            {
                if (channel == null) continue;
                list.Add(new ChannelDiagnostic
                {
                    Name = channel.Name,
                    Band = channel.Band,
                    UIRootName = channel.UIRoot != null ? channel.UIRoot.name : null,
                    HandleCount = channel.Handles.Count,
                    CameraEnabled = channel.Camera != null && channel.Camera.enabled,
                    RtWidth = channel.Texture != null ? channel.Texture.width : 0,
                    RtHeight = channel.Texture != null ? channel.Texture.height : 0,
                    Camera = channel.Camera,
                    Texture = channel.Texture,
                    Image = channel.Image
                });
            }
            return list;
        }
    }
}
