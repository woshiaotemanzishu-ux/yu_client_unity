using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>
    /// 资产管理的可播放预览台:PreviewRenderUtility 渲 prefab 实例,
    /// 选中动作后逐帧采样 Legacy clip(编辑器不进 Play 模式即可看动画)。
    /// 拖拽=旋转视角,滚轮=缩放。粒子特效预览待特效转换线接入后叠加。
    /// </summary>
    public sealed class AssetHubPreview : System.IDisposable
    {
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
            _instance.transform.position = Vector3.zero;

            var anim = _instance.GetComponent<Animation>();
            Clips = anim != null ? AnimationUtility.GetAnimationClips(_instance) : System.Array.Empty<AnimationClip>();
            _clip = null;
            _bounds = CalcBounds();
        }

        public void Play(AnimationClip clip)
        {
            _clip = clip;
            _time = 0f;
            _lastTick = EditorApplication.timeSinceStartup;
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

            // 动画采样(Legacy clip 编辑器采样)
            if (_clip != null)
            {
                double now = EditorApplication.timeSinceStartup;
                _time += Mathf.Clamp((float)(now - _lastTick), 0f, 0.1f);
                _lastTick = now;
                float t = _clip.length > 0.01f ? _time % _clip.length : 0f;
                _clip.SampleAnimation(_instance, t);
                _bounds = CalcBounds();
            }

            float size = Mathf.Max(_bounds.extents.magnitude, 0.2f);
            float dist = size * 2.4f / _zoom;
            Quaternion rot = Quaternion.Euler(_orbit.y, _orbit.x, 0f);
            _pru.camera.transform.position = _bounds.center + rot * (Vector3.forward * -dist);
            _pru.camera.transform.LookAt(_bounds.center);
            _pru.camera.nearClipPlane = dist * 0.01f;
            _pru.camera.farClipPlane = dist * 10f;
            if (_pru.lights.Length > 0)
            {
                _pru.lights[0].transform.rotation = _pru.camera.transform.rotation;
                _pru.lights[0].intensity = 1.2f;
            }

            _pru.BeginPreview(rect, GUIStyle.none);
            _pru.camera.Render();
            Texture tex = _pru.EndPreview();
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
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
        }

        public void Dispose()
        {
            ClearInstance();
            _pru?.Cleanup();
            _pru = null;
            _prefabPath = null;
        }
    }
}
