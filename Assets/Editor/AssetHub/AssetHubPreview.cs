using System.Collections.Generic;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>
    /// 资产管理的可播放预览台:PreviewRenderUtility 渲 prefab 实例,
    /// 选中动作后逐帧采样 Legacy clip(编辑器不进 Play 模式即可看动画)。
    /// 拖拽=旋转视角,滚轮=缩放。粒子特效产物在编辑器内逐帧 Simulate 预览。
    /// </summary>
    public sealed class AssetHubPreview : System.IDisposable
    {
        private const float UiEffectReferenceHeight = 1280f;
        private const float UiEffectOrthographicSize = UiEffectReferenceHeight * 0.01f * 0.5f;
        private const float UiEffectAspect = 720f / UiEffectReferenceHeight;
        private const float ParticleFrame = 1f / 60f;

        private PreviewRenderUtility _pru;
        private GameObject _instance;
        private string _prefabPath;
        private Bounds _bounds;

        private AnimationClip _clip;
        private float _time;
        private double _lastTick;

        private Vector2 _orbit = new Vector2(180f, 10f); // 默认正面(模型多面向 +Z)
        private float _zoom = 1f;

        public AnimationClip[] Clips { get; private set; } = System.Array.Empty<AnimationClip>();
        public AnimationClip Playing => _clip;
        public bool HasModel => _instance != null;
        /// <summary>含粒子系统(特效产物):编辑器内逐帧 Simulate,无需 Play 模式。</summary>
        public bool HasParticles { get; private set; }
        private ParticleSystem[] _particles = System.Array.Empty<ParticleSystem>();
        private bool[] _particleRendererDefaults = System.Array.Empty<bool>();
        private Animation[] _embeddedAnimations = System.Array.Empty<Animation>();
        private string[] _particleOptions = { "全部节点" };
        private float _psTime;
        private float _particleDuration = 1f;
        private float _particlePlaybackSpeed = 1f;
        private bool _particlePlaying = true;
        private int _particleSoloIndex;
        private bool _isUiEffect;

        public bool ParticlePlaying => _particlePlaying;
        public float ParticleTime => _psTime;
        public float ParticleDuration => _particleDuration;
        public float ParticlePlaybackSpeed
        {
            get => _particlePlaybackSpeed;
            set => _particlePlaybackSpeed = Mathf.Clamp(value, 0.05f, 2f);
        }
        public int ParticleSoloIndex => _particleSoloIndex;
        public string[] ParticleOptions => _particleOptions;
        /// <summary>UI 特效使用与 UIEffectStage 一致的 720x1280 正交取景,避免透视/旋转预览误判方向。</summary>
        public bool IsUiEffect => _isUiEffect;

        public void SetPrefab(string prefabPath)
        {
            if (_prefabPath == prefabPath && _instance != null) return;
            ClearInstance();
            _prefabPath = prefabPath;
            if (string.IsNullOrEmpty(prefabPath)) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            EnsurePru();
            _instance = Object.Instantiate(prefab);
            _pru.AddSingleGO(_instance);
            // 对标 UIEffectStage.AddAsync:实例化后会把资源根位置覆盖为调用点 position(当前 ui_zhanli 为 0,0)。
            _instance.transform.position = Vector3.zero;
            AssetHubArtPreview.Apply(_pru, _instance);

            _embeddedAnimations = _instance.GetComponentsInChildren<Animation>(true);
            var clips = new List<AnimationClip>();
            foreach (Animation anim in _embeddedAnimations)
            {
                foreach (AnimationClip clip in AnimationUtility.GetAnimationClips(anim.gameObject))
                {
                    if (clip != null && !clips.Contains(clip)) clips.Add(clip);
                }
            }
            Clips = clips.ToArray();
            _clip = null;
            _particles = _instance.GetComponentsInChildren<ParticleSystem>(true);
            HasParticles = _particles.Length > 0;
            _particleRendererDefaults = new bool[_particles.Length];
            _particleOptions = new string[_particles.Length + 1];
            _particleOptions[0] = "全部节点";
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystemRenderer renderer = _particles[i].GetComponent<ParticleSystemRenderer>();
                _particleRendererDefaults[i] = renderer == null || renderer.enabled;
                _particleOptions[i + 1] = RelativePath(_particles[i].transform, _instance.transform);
            }
            _particleSoloIndex = 0;
            _isUiEffect = prefabPath.Replace('\\', '/').Contains("/ui_effect/");
            _particleDuration = CalculateParticleDuration();
            _particlePlaying = HasParticles;
            RestartParticles();
            _lastTick = EditorApplication.timeSinceStartup;
            _bounds = CalcBounds();
        }

        public void Play(AnimationClip clip)
        {
            _clip = clip;
            _time = 0f;
            _lastTick = EditorApplication.timeSinceStartup;
        }

        public void ToggleParticles()
        {
            _particlePlaying = !_particlePlaying;
            _lastTick = EditorApplication.timeSinceStartup;
        }

        public void RestartParticles()
        {
            _psTime = 0f;
            _particlePlaying = HasParticles;
            RebuildParticleTimeline(0f);
            _lastTick = EditorApplication.timeSinceStartup;
        }

        public void StepParticles(float seconds)
        {
            _particlePlaying = false;
            SetParticleTime(_psTime + seconds);
        }

        public void SetParticleTime(float seconds)
        {
            _psTime = Mathf.Clamp(seconds, 0f, _particleDuration);
            RebuildParticleTimeline(_psTime);
            _lastTick = EditorApplication.timeSinceStartup;
        }

        /// <summary>只改变预览副本的 Renderer,不改 prefab;用于拆看复合特效中的单个粒子节点。</summary>
        public void SetParticleSolo(int optionIndex)
        {
            _particleSoloIndex = Mathf.Clamp(optionIndex, 0, _particles.Length);
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystemRenderer renderer = _particles[i] != null
                    ? _particles[i].GetComponent<ParticleSystemRenderer>()
                    : null;
                if (renderer != null)
                    renderer.enabled = _particleRendererDefaults[i] && (_particleSoloIndex == 0 || _particleSoloIndex == i + 1);
            }
            RestartParticles();
        }

        public void OnGUI(Rect rect)
        {
            if (_instance == null)
            {
                EditorGUI.HelpBox(rect, "无产物可预览(先转换)", MessageType.None);
                return;
            }
            HandleInput(rect);
            if (Event.current.type != EventType.Repaint) return;

            // 动画采样(Legacy clip 编辑器采样)+ 粒子模拟(编辑器内不依赖 Play 模式)
            if (_clip != null || HasParticles)
            {
                double now = EditorApplication.timeSinceStartup;
                float dt = Mathf.Clamp((float)(now - _lastTick), 0f, 0.1f);
                _lastTick = now;
                if (_clip != null)
                {
                    _time += dt;
                    float t = _clip.length > 0.01f ? _time % _clip.length : 0f;
                    _clip.SampleAnimation(_instance, t);
                    _bounds = CalcBounds();
                }
                if (HasParticles)
                {
                    if (_particlePlaying)
                    {
                        float scaledDt = dt * _particlePlaybackSpeed;
                        float next = _psTime + scaledDt;
                        if (next > _particleDuration)
                        {
                            RestartParticles();
                        }
                        else
                        {
                            _psTime = next;
                            SampleEmbeddedAnimations(_psTime);
                            SimulateParticleDelta(scaledDt);
                        }
                    }
                }
            }

            Rect renderRect = _isUiEffect ? FitAspect(rect, UiEffectAspect) : rect;
            if (_isUiEffect)
            {
                // 与 UIEffectStage.CreateHandle 同一取景:相机位于 z=-10、朝 +Z、正交全高 12.8 世界单位；
                // 最终屏幕方向还会在下方 DrawTextureWithTexCoords 复刻 Laya rotationY=180 的水平翻转。
                _pru.camera.orthographic = true;
                _pru.camera.orthographicSize = UiEffectOrthographicSize / _zoom;
                _pru.camera.transform.position = new Vector3(0f, 0f, -10f);
                _pru.camera.transform.rotation = Quaternion.identity;
                _pru.camera.nearClipPlane = 0.3f;
                _pru.camera.farClipPlane = 1000f;
            }
            else
            {
                _pru.camera.orthographic = false;
                float size = Mathf.Max(_bounds.extents.magnitude, 0.2f);
                float dist = size * 2.4f / _zoom;
                Quaternion rot = Quaternion.Euler(_orbit.y, _orbit.x, 0f);
                _pru.camera.transform.position = _bounds.center + rot * (Vector3.forward * -dist);
                _pru.camera.transform.LookAt(_bounds.center);
                _pru.camera.nearClipPlane = dist * 0.01f;
                _pru.camera.farClipPlane = dist * 10f;
            }
            if (_pru.lights.Length > 0)
            {
                _pru.lights[0].transform.rotation = _pru.camera.transform.rotation;
                _pru.lights[0].intensity = 1.2f;
            }

            EditorGUI.DrawRect(rect, new Color(0.10f, 0.11f, 0.13f, 1f));
            _pru.BeginPreview(renderRect, GUIStyle.none);
            _pru.camera.Render();
            Texture tex = _pru.EndPreview();
            if (_isUiEffect)
                GUI.DrawTextureWithTexCoords(renderRect, tex, new Rect(1f, 0f, -1f, 1f), false);
            else
                GUI.DrawTexture(renderRect, tex, ScaleMode.StretchToFill, false);
            if (_isUiEffect)
                GUI.Label(new Rect(renderRect.x + 6f, renderRect.y + 4f, 190f, 18f), "UI运行取景 720×1280（已镜像补偿）", EditorStyles.miniLabel);
        }

        private void HandleInput(Rect rect)
        {
            Event ev = Event.current;
            if (!rect.Contains(ev.mousePosition)) return;
            if (!_isUiEffect && ev.type == EventType.MouseDrag && ev.button == 0)
            {
                _orbit.x += ev.delta.x * 0.6f;
                _orbit.y = Mathf.Clamp(_orbit.y + ev.delta.y * 0.4f, -80f, 80f);
                ev.Use();
            }
            else if (ev.type == EventType.ScrollWheel)
            {
                _zoom = Mathf.Clamp(_zoom * (1f - ev.delta.y * 0.04f), 0.3f, 4f);
                ev.Use();
            }
        }

        private void RebuildParticleTimeline(float targetTime)
        {
            foreach (ParticleSystem psys in RootParticles())
            {
                psys.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                psys.Clear(true);
            }
            SampleEmbeddedAnimations(0f);
            float simulated = 0f;
            while (simulated + 0.0001f < targetTime)
            {
                float dt = Mathf.Min(ParticleFrame, targetTime - simulated);
                simulated += dt;
                SampleEmbeddedAnimations(simulated);
                SimulateParticleDelta(dt);
            }
        }

        private void SimulateParticleDelta(float dt)
        {
            if (dt <= 0f) return;
            foreach (ParticleSystem psys in RootParticles())
                psys.Simulate(dt, withChildren: true, restart: false, fixedTimeStep: false);
        }

        private IEnumerable<ParticleSystem> RootParticles()
        {
            foreach (ParticleSystem psys in _particles)
            {
                if (psys == null) continue;
                Transform parent = psys.transform.parent;
                if (parent != null && parent.GetComponentInParent<ParticleSystem>() != null) continue;
                yield return psys;
            }
        }

        /// <summary>运行时 UIEffectStage.Play 会播放所有子 Animation;预览也必须逐个按自身节点采样。</summary>
        private void SampleEmbeddedAnimations(float time)
        {
            if (!_isUiEffect) return;
            foreach (Animation anim in _embeddedAnimations)
            {
                if (anim == null) continue;
                AnimationClip clip = anim.clip;
                if (clip == null)
                {
                    foreach (AnimationState state in anim) { clip = state.clip; break; }
                }
                if (clip == null) continue;
                float sample = clip.length > 0.0001f
                    ? (clip.wrapMode == WrapMode.Loop ? time % clip.length : Mathf.Min(time, clip.length))
                    : 0f;
                clip.SampleAnimation(anim.gameObject, sample);
            }
        }

        private float CalculateParticleDuration()
        {
            float end = 0.1f;
            foreach (ParticleSystem psys in _particles)
            {
                ParticleSystem.MainModule main = psys.main;
                end = Mathf.Max(end, CurveMax(main.startDelay) + main.duration + CurveMax(main.startLifetime));
            }
            foreach (Animation anim in _embeddedAnimations)
            {
                if (anim != null && anim.clip != null) end = Mathf.Max(end, anim.clip.length);
            }
            return Mathf.Clamp(end, 0.1f, 30f);
        }

        private static float CurveMax(ParticleSystem.MinMaxCurve curve)
        {
            float max = Mathf.Max(curve.constant, curve.constantMax);
            if (curve.curve != null)
            {
                foreach (Keyframe key in curve.curve.keys) max = Mathf.Max(max, key.value * curve.curveMultiplier);
            }
            if (curve.curveMax != null)
            {
                foreach (Keyframe key in curve.curveMax.keys) max = Mathf.Max(max, key.value * curve.curveMultiplier);
            }
            return Mathf.Max(0f, max);
        }

        private static string RelativePath(Transform node, Transform root)
        {
            if (node == null) return "(missing)";
            string path = node.name;
            while (node.parent != null && node.parent != root)
            {
                node = node.parent;
                path = node.name + "/" + path;
            }
            return path;
        }

        private static Rect FitAspect(Rect outer, float aspect)
        {
            float width = Mathf.Min(outer.width, outer.height * aspect);
            float height = width / aspect;
            return new Rect(outer.x + (outer.width - width) * 0.5f,
                outer.y + (outer.height - height) * 0.5f, width, height);
        }

        private Bounds CalcBounds()
        {
            var renderers = _instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void EnsurePru()
        {
            if (_pru != null) return;
            _pru = new PreviewRenderUtility();
            _pru.camera.fieldOfView = 30f;
            _pru.camera.clearFlags = CameraClearFlags.SolidColor;
            _pru.camera.backgroundColor = new Color(0.16f, 0.17f, 0.20f, 1f);
        }

        private void ClearInstance()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
            _instance = null;
            _clip = null;
            Clips = System.Array.Empty<AnimationClip>();
            _particles = System.Array.Empty<ParticleSystem>();
            _particleRendererDefaults = System.Array.Empty<bool>();
            _embeddedAnimations = System.Array.Empty<Animation>();
            _particleOptions = new[] { "全部节点" };
            _particleSoloIndex = 0;
            _isUiEffect = false;
            HasParticles = false;
        }

        public void Dispose()
        {
            ClearInstance();
            _pru?.Cleanup();
            _pru = null;
            _prefabPath = null;
        }
    }

    /// <summary>让资产管理预览相机复用游戏内 ArtModelRenderProfile，而不是按裸 Scene 相机渲染。</summary>
    internal static class AssetHubArtPreview
    {
        public static void Apply(PreviewRenderUtility preview, GameObject instance)
        {
            if (preview == null || preview.camera == null) return;
            ArtModelRenderProfile profile = instance != null
                ? instance.GetComponentInChildren<ArtModelRenderProfile>(false)
                : null;
            ArtModelRenderProfile.ApplyToCamera(preview.camera, profile);
            preview.ambientColor = profile != null ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0f);
        }
    }
}
