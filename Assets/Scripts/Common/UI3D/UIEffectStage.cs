using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// Runtime UI effect bridge mirroring the old client UIEffect.AddUIEffect path:
    /// spawn a 3D UI effect on an isolated stage, render it with an orthographic camera
    /// to a RenderTexture, then place that texture back into the target UI container.
    /// </summary>
    public static class UIEffectStage
    {
        private const float REFERENCE_STAGE_HEIGHT = 1280f;
        private const float LAYA_STAGE_TO_WORLD = 0.01f;
        private const float CAMERA_Z = -10f;
        private const int MIN_RT_SIZE = 16;
        private const int MAX_RT_SIZE = 2048;
        // Reserved unnamed layer for offscreen UI effects; non-owner cameras are forced to exclude it.
        private const int EFFECT_LAYER = 31;
        private static readonly int EffectLayerMask = 1 << EFFECT_LAYER;
        private static int _stageIndex;
        private static Camera[] _cameraBuffer = new Camera[16];

        public sealed class Handle
        {
            internal GameObject StageRoot;
            internal Transform EffectRoot;
            internal Camera Camera;
            internal RenderTexture Texture;
            internal RawImage Image;
            internal GameObject Effect;
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (Image != null) DestroyObject(Image.gameObject);
                if (Effect != null) ResManager.ReleaseInstance(Effect);
                if (Camera != null) Camera.targetTexture = null;
                if (Texture != null)
                {
                    Texture.Release();
                    DestroyObject(Texture);
                }
                if (StageRoot != null) DestroyObject(StageRoot);

                Image = null;
                Effect = null;
                Camera = null;
                Texture = null;
                StageRoot = null;
                EffectRoot = null;
            }
        }

        public static async Task<Handle> AddAsync(string effectName, RectTransform parent,
            Vector2 position = default, Vector3 scale = default, float rotationY = 0f,
            Vector2 renderSize = default)
        {
            if (string.IsNullOrEmpty(effectName) || parent == null) return null;
            return await AddByKeyAsync(effectName, GameResPath.GetUIEffectPrefabPath(effectName),
                parent, position, scale, rotationY, renderSize);
        }

        public static async Task<Handle> AddByKeyAsync(string label, string effectKey, RectTransform parent,
            Vector2 position = default, Vector3 scale = default, float rotationY = 0f,
            Vector2 renderSize = default)
        {
            if (string.IsNullOrEmpty(effectKey) || parent == null) return null;
            if (string.IsNullOrEmpty(label)) label = effectKey;
            if (scale == default) scale = Vector3.one;

            Handle handle = CreateHandle(SafeName(label), parent, renderSize);
            if (handle == null) return null;

            GameObject effect = await ResManager.InstantiateAsync(effectKey, handle.EffectRoot);
            if (effect == null || parent == null)
            {
                if (effect == null) GameLog.Warn("UIEffect", "load ui effect failed: label={0} key={1}", label, effectKey);
                if (effect != null) ResManager.ReleaseInstance(effect);
                handle.Dispose();
                return null;
            }

            handle.Effect = effect;
            effect.name = "__ui_effect_" + SafeName(label);
            SetLayerRecursive(effect, EFFECT_LAYER);
            Transform t = effect.transform;
            t.localPosition = new Vector3(-position.x, -position.y, 0f);
            t.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            t.localScale = scale;

            ApplyRenderDefaults(effect);
            Play(effect);
            effect.SetActive(true);
            return handle;
        }

        public static Task<Handle> AddAsync(UIEffectSlot slot, RectTransform parent)
        {
            if (slot == null) return Task.FromResult<Handle>(null);
            return AddByKeyAsync(slot.EffectName, slot.AddressKey, parent,
                slot.Position, slot.Scale, slot.RotationY);
        }

        private static Handle CreateHandle(string effectName, RectTransform parent, Vector2 renderSize)
        {
            Vector2 displaySize = GetPositiveSize(parent.rect.size);
            Vector2 sourceSize = GetPositiveSize(renderSize);
            if (sourceSize == default) sourceSize = displaySize;

            float stageHeight = GetStageHeight(parent);
            float renderScale = Mathf.Max(0.01f, stageHeight / REFERENCE_STAGE_HEIGHT);
            int width = Mathf.Clamp(Mathf.CeilToInt(sourceSize.x * renderScale), MIN_RT_SIZE, MAX_RT_SIZE);
            int height = Mathf.Clamp(Mathf.CeilToInt(sourceSize.y * renderScale), MIN_RT_SIZE, MAX_RT_SIZE);

            int index = ++_stageIndex;
            var stageRoot = new GameObject("__UIEffectStage_" + effectName);
            stageRoot.layer = EFFECT_LAYER;
            stageRoot.transform.position = new Vector3(6000f + index * 20f, -6000f, 6000f);
            UIEffectStageCameraGuard guard = stageRoot.AddComponent<UIEffectStageCameraGuard>();

            var effectRoot = new GameObject("EffectRoot").transform;
            effectRoot.SetParent(stageRoot.transform, false);
            effectRoot.gameObject.layer = EFFECT_LAYER;

            var cameraGo = new GameObject("EffectCamera");
            cameraGo.transform.SetParent(stageRoot.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);
            cameraGo.layer = EFFECT_LAYER;

            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = stageHeight * LAYA_STAGE_TO_WORLD * 0.5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.cullingMask = EffectLayerMask;
            cam.useOcclusionCulling = false;
            cam.allowHDR = false;
            guard.Owner = cam;
            ExcludeStageLayerFromOtherCameras(cam);

            var rt = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = "__UIEffectRT_" + effectName
            };
            ClearRenderTexture(rt);
            cam.targetTexture = rt;

            var imageGo = new GameObject("__UIEffectImage_" + effectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform imageRt = (RectTransform)imageGo.transform;
            imageRt.SetParent(parent, false);
            imageRt.anchorMin = Vector2.zero;
            imageRt.anchorMax = Vector2.one;
            imageRt.offsetMin = Vector2.zero;
            imageRt.offsetMax = Vector2.zero;
            imageRt.localScale = Vector3.one;

            RawImage image = imageGo.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = rt;
            image.uvRect = CreateCenteredUvRect(displaySize, sourceSize);

            return new Handle
            {
                StageRoot = stageRoot,
                EffectRoot = effectRoot,
                Camera = cam,
                Texture = rt,
                Image = image
            };
        }

        private static Vector2 GetPositiveSize(Vector2 size)
        {
            if (size.x <= 1f || size.y <= 1f) return default;
            return new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
        }

        private static Rect CreateCenteredUvRect(Vector2 displaySize, Vector2 sourceSize)
        {
            if (sourceSize.x <= displaySize.x + 0.01f && sourceSize.y <= displaySize.y + 0.01f)
                return new Rect(0f, 0f, 1f, 1f);

            float width = Mathf.Clamp01(displaySize.x / Mathf.Max(1f, sourceSize.x));
            float height = Mathf.Clamp01(displaySize.y / Mathf.Max(1f, sourceSize.y));
            return new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);
        }

        private static float GetStageHeight(RectTransform parent)
        {
            Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.referenceResolution.y > 1f) return scaler.referenceResolution.y;
            }
            RectTransform root = canvas != null ? canvas.transform as RectTransform : null;
            if (root != null && root.rect.height > 1f) return root.rect.height;
            if (canvas != null && canvas.pixelRect.height > 1f) return canvas.pixelRect.height;
            return REFERENCE_STAGE_HEIGHT;
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
            Transform tr = go.transform;
            for (int i = 0; i < tr.childCount; i++)
            {
                SetLayerRecursive(tr.GetChild(i).gameObject, layer);
            }
        }

        internal static void ExcludeStageLayerFromOtherCameras(Camera owner)
        {
            int count = Camera.allCamerasCount;
            if (_cameraBuffer.Length < count) _cameraBuffer = new Camera[count];
            int written = Camera.GetAllCameras(_cameraBuffer);
            for (int i = 0; i < written; i++)
            {
                Camera cam = _cameraBuffer[i];
                if (cam == null || cam == owner) continue;
                // UIEffectStage cameras share the reserved layer; only strip real scene/UI cameras.
                if (cam.GetComponentInParent<UIEffectStageCameraGuard>() != null) continue;
                if ((cam.cullingMask & EffectLayerMask) != 0)
                    cam.cullingMask &= ~EffectLayerMask;
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
                Animation anim = animations[i];
                if (anim.clip != null)
                {
                    anim.Play();
                    continue;
                }

                foreach (AnimationState state in anim)
                {
                    anim.Play(state.name);
                    break;
                }
            }
        }

        private static void ClearRenderTexture(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        private static void DestroyObject(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "effect";
            return value.Replace('\\', '_').Replace('/', '_').Replace('.', '_');
        }
    }
}
