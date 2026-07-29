using System.Collections.Generic;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    public sealed class AssetAssemblyPreview : System.IDisposable
    {
        private PreviewRenderUtility _pru;
        private GameObject _instance;
        private GameObject _weaponInstance;
        private string _modelKey;
        private string _weaponKey;
        private Bounds _bounds;

        private Animation _animation;
        private AnimationClip _clip;
        private float _time;
        private double _lastTick;

        private readonly List<GameObject> _alwaysEffects = new List<GameObject>();
        private readonly List<GameObject> _actionEffects = new List<GameObject>();
        private ParticleSystem[] _particles = System.Array.Empty<ParticleSystem>();

        private Vector2 _orbit = new Vector2(180f, 10f);
        private float _zoom = 1f;

        public void SetModel(string modelKey)
        {
            if (_modelKey == modelKey && _instance != null) return;
            ClearInstance();
            _modelKey = modelKey;
            if (string.IsNullOrEmpty(modelKey)) return;

            string path = AssetAssemblyProfileStore.AssetPathOfKey(modelKey, ".prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            EnsurePru();
            _instance = Object.Instantiate(prefab);
            _pru.AddSingleGO(_instance);
            _instance.transform.position = Vector3.zero;
            AssetHubArtPreview.Apply(_pru, _instance);
            _animation = _instance.GetComponent<Animation>();
            if (_animation == null) _animation = _instance.AddComponent<Animation>();
            _lastTick = EditorApplication.timeSinceStartup;
            RefreshParticles();
            _bounds = CalcBounds();
        }

        public void SetWeapon(string weaponKey)
        {
            if (_weaponKey == weaponKey && _weaponInstance != null) return;
            if (_weaponInstance != null) Object.DestroyImmediate(_weaponInstance);
            _weaponInstance = null;
            _weaponKey = weaponKey;
            if (_instance == null) return;
            if (string.IsNullOrEmpty(weaponKey))
            {
                AssetHubArtPreview.Apply(_pru, _instance);
                return;
            }

            string path = AssetAssemblyProfileStore.AssetPathOfKey(weaponKey, ".prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            Transform hand = RoleModelAssembler.FindBone(_instance.transform, "rhand") ?? _instance.transform;
            _weaponInstance = Object.Instantiate(prefab, hand);
            _weaponInstance.transform.localPosition = Vector3.zero;
            _weaponInstance.transform.localRotation = Quaternion.identity;
            _weaponInstance.transform.localScale = Vector3.one;
            AssetHubArtPreview.Apply(_pru, _instance);
            RefreshParticles();
            _bounds = CalcBounds();
        }

        public void ApplyAlwaysEffects(List<AssetEffectBinding> bindings)
        {
            ClearEffects(_alwaysEffects);
            AttachEffects(bindings, _alwaysEffects);
            RefreshParticles();
            _bounds = CalcBounds();
        }

        public void PlayAction(string actionName, string actionKey, List<AssetEffectBinding> actionEffects)
        {
            if (_instance == null || string.IsNullOrEmpty(actionName)) return;
            _clip = null;
            if (!string.IsNullOrEmpty(actionKey))
            {
                string path = AssetAssemblyProfileStore.AssetPathOfKey(actionKey, ".anim");
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    if (_animation.GetClip(actionName) == null) _animation.AddClip(clip, actionName);
                    _animation.Stop();
                    _animation.Play(actionName);
                    _clip = clip;
                    _time = 0f;
                    _lastTick = EditorApplication.timeSinceStartup;
                }
            }

            ClearEffects(_actionEffects);
            AttachEffects(actionEffects, _actionEffects);
            RefreshParticles();
            _bounds = CalcBounds();
        }

        public void OnGUI(Rect rect)
        {
            if (_instance == null)
            {
                EditorGUI.HelpBox(rect, "无模型可预览", MessageType.None);
                return;
            }
            HandleInput(rect);
            if (Event.current.type != EventType.Repaint) return;

            Tick();
            float size = Mathf.Max(_bounds.extents.magnitude, 0.2f);
            float dist = size * 2.4f / _zoom;
            Quaternion rot = Quaternion.Euler(_orbit.y, _orbit.x, 0f);
            _pru.camera.transform.position = _bounds.center + rot * (Vector3.forward * -dist);
            _pru.camera.transform.LookAt(_bounds.center);
            _pru.camera.nearClipPlane = dist * 0.01f;
            _pru.camera.farClipPlane = dist * 10f;
            _pru.BeginPreview(rect, GUIStyle.none);
            _pru.camera.Render();
            Texture tex = _pru.EndPreview();
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Clamp((float)(now - _lastTick), 0f, 0.1f);
            _lastTick = now;
            if (_clip != null)
            {
                _time += dt;
                float t = _clip.length > 0.01f ? _time % _clip.length : 0f;
                _clip.SampleAnimation(_instance, t);
            }
            foreach (ParticleSystem ps in _particles)
            {
                if (ps != null) ps.Simulate(dt, true, false);
            }
        }

        private void AttachEffects(List<AssetEffectBinding> bindings, List<GameObject> bucket)
        {
            if (_instance == null || bindings == null) return;
            foreach (AssetEffectBinding binding in bindings)
            {
                if (binding == null) continue;
                string effectKey = binding.ResolveEffectKey();
                if (string.IsNullOrEmpty(effectKey)) continue;
                string path = AssetAssemblyProfileStore.AssetPathOfKey(effectKey, ".prefab");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                Transform host = BindingHost(binding);
                Transform bone = RoleModelAssembler.FindBone(host, binding.Bone) ?? host;
                GameObject go = Object.Instantiate(prefab, bone);
                go.transform.localPosition = binding.ResolvePosition();
                go.transform.localRotation = binding.ResolveRotation();
                go.transform.localScale = binding.ResolveScale();
                bucket.Add(go);
            }
        }

        private Transform BindingHost(AssetEffectBinding binding)
        {
            if (binding != null && binding.Target == "weapon" && _weaponInstance != null)
                return _weaponInstance.transform;
            return _instance.transform;
        }

        private static void ClearEffects(List<GameObject> bucket)
        {
            for (int i = 0; i < bucket.Count; i++)
            {
                if (bucket[i] != null) Object.DestroyImmediate(bucket[i]);
            }
            bucket.Clear();
        }

        private void RefreshParticles()
        {
            _particles = _instance != null
                ? _instance.GetComponentsInChildren<ParticleSystem>(true)
                : System.Array.Empty<ParticleSystem>();
            foreach (ParticleSystem ps in _particles)
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private Bounds CalcBounds()
        {
            if (_instance == null) return new Bounds(Vector3.zero, Vector3.one);
            Renderer[] renderers = _instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private void HandleInput(Rect rect)
        {
            Event ev = Event.current;
            if (!rect.Contains(ev.mousePosition)) return;
            if (ev.type == EventType.MouseDrag && ev.button == 0)
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
            ClearEffects(_alwaysEffects);
            ClearEffects(_actionEffects);
            if (_weaponInstance != null) Object.DestroyImmediate(_weaponInstance);
            _weaponInstance = null;
            if (_instance != null) Object.DestroyImmediate(_instance);
            _instance = null;
            _weaponKey = null;
            _animation = null;
            _clip = null;
            _particles = System.Array.Empty<ParticleSystem>();
        }

        public void Dispose()
        {
            ClearInstance();
            _pru?.Cleanup();
            _pru = null;
        }
    }
}
