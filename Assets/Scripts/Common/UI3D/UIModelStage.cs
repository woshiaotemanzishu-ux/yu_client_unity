using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// UI 内 3D 模型展示台(Laya SetRoleModel 的 Unity 对等物,MVP 单实例):
    /// 隔离区(远离原点)摆模型 → 专用相机渲到 RenderTexture → RawImage 贴进 UI 容器。
    /// 相机按模型包围盒自动取景;背景透明,UI 底图可透出。
    /// </summary>
    public static class UIModelStage
    {
        private static readonly Vector3 STAGE_POS = new Vector3(500f, -500f, 500f);

        private static GameObject _root;
        private static Camera _cam;
        private static RenderTexture _rt;
        private static RawImage _img;
        private static GameObject _model;

        public static void Show(RectTransform container, GameObject modelPrefab)
        {
            if (container == null || modelPrefab == null) return;
            EnsureStage();

            if (_model != null) Object.Destroy(_model);
            _model = Object.Instantiate(modelPrefab, _root.transform);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;

            FrameCamera();

            if (_img == null || _img.transform.parent != container)
            {
                if (_img != null) Object.Destroy(_img.gameObject);
                var go = new GameObject("__ModelView", typeof(RectTransform), typeof(RawImage));
                var rt = (RectTransform)go.transform;
                rt.SetParent(container, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _img = go.GetComponent<RawImage>();
                _img.raycastTarget = false;
                _img.texture = _rt;
            }
            _img.gameObject.SetActive(true);
        }

        public static void Clear()
        {
            if (_model != null) { Object.Destroy(_model); _model = null; }
            if (_img != null) _img.gameObject.SetActive(false);
        }

        private static void EnsureStage()
        {
            if (_root != null) return;
            _root = new GameObject("__UIModelStage");
            Object.DontDestroyOnLoad(_root);
            _root.transform.position = STAGE_POS;

            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(_root.transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 透明底,UI 背景透出
            _cam.fieldOfView = 35f;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 50f;

            _rt = new RenderTexture(512, 1024, 16, RenderTextureFormat.ARGB32);
            _rt.name = "UIModelStageRT";
            _cam.targetTexture = _rt;
        }

        private static void FrameCamera()
        {
            var renderers = _model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float height = Mathf.Max(b.size.y, 0.5f);
            Vector3 center = b.center;
            float distance = height * 1.4f;
            _cam.transform.position = center + new Vector3(0f, 0f, -distance);
            _cam.transform.LookAt(center);
        }
    }
}
