using System.Collections.Generic;
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
        // 诊断用:所有存活的离屏特效句柄,供 RuntimeUiCaptureTool 在 Play 态导出每个 RT 内容 + 运行态指标。
        private static readonly List<Handle> s_live = new List<Handle>();
        // 最近的加载失败(key 不在 Addressables / parent 已销毁等),让「为何没出特效」在 dump 里一目了然。
        private static readonly List<string> s_recentFailures = new List<string>();
        // RT 贴回屏幕用的加色材质(惰性创建,全特效共用)。
        private static Material s_additiveImageMaterial;
        private static bool s_additiveShaderMissingLogged;

        public sealed class Handle
        {
            internal GameObject StageRoot;
            internal Transform EffectRoot;
            internal Camera Camera;
            internal RenderTexture Texture;
            internal RawImage Image;
            internal GameObject Effect;
            internal string Label;
            internal string Key;
            internal RectTransform Parent;
            internal Vector3 Scale;
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                s_live.Remove(this);

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
            handle.Label = label;
            handle.Key = effectKey;
            handle.Parent = parent;
            handle.Scale = scale;

            GameObject effect = await ResManager.InstantiateAsync(effectKey, handle.EffectRoot);
            if (effect == null || parent == null)
            {
                if (effect == null)
                {
                    GameLog.Warn("UIEffect", "load ui effect failed: label={0} key={1}", label, effectKey);
                    RecordFailure(label, effectKey, "ResManager.InstantiateAsync returned null (key not loadable)");
                }
                else
                {
                    RecordFailure(label, effectKey, "parent destroyed before effect ready");
                }
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
            s_live.Add(handle);
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
            // 关键:加色材质把 RT 的亮 RGB 直接叠到屏幕。否则加色粒子的 RT alpha≈0,标准 alpha 混合下屏幕全黑
            // (但 RT dump 强制不透明就能看到)——这正是「dump 正常、屏幕没特效」的根因。
            Material additive = GetAdditiveImageMaterial();
            if (additive != null) image.material = additive;

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
                if (!anim.enabled) continue; // 被 EffectRotationRepair 停用的退化旋转动画:别播,交给 UIEffectSpin 自转
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

        private static void RecordFailure(string label, string key, string reason)
        {
            s_recentFailures.Add(string.Format("{0} | key={1} | {2}", label, key, reason));
            const int keep = 32;
            if (s_recentFailures.Count > keep) s_recentFailures.RemoveRange(0, s_recentFailures.Count - keep);
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
                    GameLog.Warn("UIEffect", "additive UI shader missing (Shenxiao/UI/UIEffectAdditive); RT alpha-blends and likely stays invisible");
                    Note("additive UI shader missing -> standard alpha blend (effects may be invisible)");
                }
                return null;
            }
            s_additiveImageMaterial = new Material(shader) { name = "UIEffectAdditive(Runtime)" };
            return s_additiveImageMaterial;
        }

        // ===================== 运行态诊断(编辑器抓证据用)=====================
        // 静态分析已排除「盒子0尺寸/层31不渲染/缩放语义/Addressables加载」四类结构性原因,
        // 余下只能是运行态:粒子有没有渲进 RT、RawImage 有没有贴到屏幕。下面把这些一次性吐出来。

        public struct EffectDiagnostic
        {
            public string Label;
            public string Key;
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
            // 编辑器侧据此把 RT 内容 blit 成 PNG(空 RT = 相机没渲染到东西 → 渲染端问题)。
            public RenderTexture Texture;
        }

        public static int LiveCount => s_live.Count;

        public static List<string> CollectRecentFailures()
        {
            return new List<string>(s_recentFailures);
        }

        // 调用方追踪用:让「为什么这个特效没挂上」的判定一并落进抓图证据(如 ActivityIcon 每图标的 cfg/effectName/box 状态)。
        private static readonly List<string> s_notes = new List<string>();

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
                Handle h = s_live[i];
                if (h == null) continue;

                var d = new EffectDiagnostic { Label = h.Label, Key = h.Key };
                d.LocalScale = h.Scale; // 兜底:未加载到 effect 时至少报传入 scale

                GameObject eff = h.Effect;
                d.EffectAlive = eff != null;
                if (eff != null)
                {
                    d.EffectActiveInHierarchy = eff.activeInHierarchy;
                    d.LocalScale = eff.transform.localScale;

                    ParticleSystem[] systems = eff.GetComponentsInChildren<ParticleSystem>(true);
                    d.ParticleSystemCount = systems.Length;
                    int alive = 0;
                    bool playing = false;
                    for (int p = 0; p < systems.Length; p++)
                    {
                        if (systems[p] == null) continue;
                        alive += systems[p].particleCount;
                        if (systems[p].isPlaying) playing = true;
                    }
                    d.AliveParticleCount = alive;
                    d.AnyParticlePlaying = playing;

                    Renderer[] renderers = eff.GetComponentsInChildren<Renderer>(true);
                    d.RendererCount = renderers.Length;
                    bool visible = false;
                    string shader = null;
                    Bounds bounds = default;
                    bool hasBounds = false;
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        Renderer rend = renderers[r];
                        if (rend == null) continue;
                        if (rend.isVisible) visible = true;
                        if (shader == null && rend.sharedMaterial != null && rend.sharedMaterial.shader != null)
                            shader = rend.sharedMaterial.shader.name;
                        if (!hasBounds) { bounds = rend.bounds; hasBounds = true; }
                        else bounds.Encapsulate(rend.bounds);
                    }
                    d.AnyRendererVisible = visible;
                    d.FirstShader = shader;
                    d.WorldBoundsSize = hasBounds ? bounds.size : Vector3.zero;
                }

                if (h.Parent != null)
                {
                    d.ParentName = h.Parent.name;
                    d.ParentActiveInHierarchy = h.Parent.gameObject.activeInHierarchy;
                    d.ParentRectSize = h.Parent.rect.size;
                }

                if (h.Texture != null)
                {
                    d.RtWidth = h.Texture.width;
                    d.RtHeight = h.Texture.height;
                    d.Texture = h.Texture;
                }

                if (h.Camera != null)
                {
                    d.CameraEnabled = h.Camera.enabled;
                    d.CameraOrthoSize = h.Camera.orthographicSize;
                    d.CameraWorldPos = h.Camera.transform.position;
                }

                if (h.Image != null)
                {
                    d.ImageAlive = true;
                    d.ImageActiveInHierarchy = h.Image.gameObject.activeInHierarchy;
                    d.ImageRectSize = h.Image.rectTransform.rect.size;
                    d.ImageColor = h.Image.color;
                    d.ImageHasTexture = h.Image.texture != null;
                }

                list.Add(d);
            }
            return list;
        }
    }
}
