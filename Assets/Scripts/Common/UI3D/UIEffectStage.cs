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
        private const float ORTHO_FULL_HEIGHT = 12.8f;
        private const float CAMERA_Z = -10f;
        private const int MIN_RT_SIZE = 16;
        private const int MAX_RT_SIZE = 2048;
        private static int _stageIndex;

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
            Vector2 position = default, Vector3 scale = default, float rotationY = 0f)
        {
            if (string.IsNullOrEmpty(effectName) || parent == null) return null;
            if (scale == default) scale = Vector3.one;

            Handle handle = CreateHandle(effectName, parent);
            if (handle == null) return null;

            string effectKey = GameResPath.GetUIEffectPath(effectName);
            GameObject effect = await ResManager.InstantiateAsync(effectKey, handle.EffectRoot);
            if (effect == null || parent == null)
            {
                if (effect == null) GameLog.Warn("UIEffect", "load ui effect failed: name={0} key={1}", effectName, effectKey);
                if (effect != null) ResManager.ReleaseInstance(effect);
                handle.Dispose();
                return null;
            }

            handle.Effect = effect;
            effect.name = "__ui_effect_" + effectName;
            Transform t = effect.transform;
            t.localPosition = new Vector3(-position.x, -position.y, 0f);
            t.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            t.localScale = scale;

            ApplyRenderDefaults(effect);
            Play(effect);
            effect.SetActive(true);
            return handle;
        }

        private static Handle CreateHandle(string effectName, RectTransform parent)
        {
            Rect rect = parent.rect;
            int width = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, rect.width)), MIN_RT_SIZE, MAX_RT_SIZE);
            int height = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, rect.height)), MIN_RT_SIZE, MAX_RT_SIZE);

            int index = ++_stageIndex;
            var stageRoot = new GameObject("__UIEffectStage_" + effectName);
            stageRoot.transform.position = new Vector3(6000f + index * 20f, -6000f, 6000f);

            var effectRoot = new GameObject("EffectRoot").transform;
            effectRoot.SetParent(stageRoot.transform, false);

            var cameraGo = new GameObject("EffectCamera");
            cameraGo.transform.SetParent(stageRoot.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);

            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = ORTHO_FULL_HEIGHT * 0.5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.useOcclusionCulling = false;
            cam.allowHDR = false;

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

            return new Handle
            {
                StageRoot = stageRoot,
                EffectRoot = effectRoot,
                Camera = cam,
                Texture = rt,
                Image = image
            };
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
    }
}
