using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// UI 内 3D 模型展示台(Laya UIModelClass3D 的 Unity 对等物,MVP 单实例):
    /// 隔离区(远离原点)摆模型 → 专用相机渲到 RenderTexture → RawImage 贴进 UI 容器。
    /// 取景复刻老客户端 UIModelClass3D.ts 的固定参数(正交相机 + 层级缩放 + 配置位移),
    /// 不做包围盒自适应 —— 老客户端就是靠 UIModelParameter/ConfigLogin 的 scale/position 控制构图。
    /// </summary>
    public static class UIModelStage
    {
        private static readonly Vector3 STAGE_POS = new Vector3(500f, -500f, 500f);

        // —— 老客户端 UIModelClass3D.ts 固定参数(逐行对标)——
        private const float ORTHO_FULL_HEIGHT = 12.8f; // camera.orthographicVerticalSize = 12.8(全高;Unity orthographicSize 是半高)
        private const float CAMERA_Z = -20f;           // Set3DLocalPosition(camera, ..., -20)
        private const float ROOT_SCALE = 1.1f;         // default_model_scale = 1.1
        private const float BODY_SCALE_MUL = 5f;       // Set3DLocalScale(transform, 5 * data.scale, ...)
        private const float BASE_Y = -5f;              // pos_y = ... - 5(模型根在相机中心下方 5,再加 position.y 配置)
        private const float MODEL_YAW = 180f;          // 默认 rotate = (0, 180, 0),模型转身面向相机

        private static GameObject _root;
        private static Camera _cam;
        private static RenderTexture _rt;
        private static RawImage _img;
        private static Transform _modelRoot; // 对标 root_transform(缩放 1.1 + 位移)
        private static Transform _modelYaw;  // 对标 model_transform(旋转用)
        private static GameObject _model;

        /// <summary>
        /// 对标 SetRoleModel(show_model_data):scale=show_model_data.scale(登录链路 0.5),
        /// position=ConfigLogin 的 ModelPos+PosOffset(x 右正,y 上正,单位=世界单位)。
        /// </summary>
        public static void Show(RectTransform container, GameObject modelPrefab,
            float scale = 1f, Vector2 position = default)
        {
            if (container == null || modelPrefab == null) return;
            EnsureStage();
            EnsureRenderTexture(container);

            if (_model != null) Object.Destroy(_model);
            _modelRoot.localPosition = new Vector3(position.x, position.y + BASE_Y, 0f);
            _model = Object.Instantiate(modelPrefab, _modelYaw);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            _model.transform.localScale = Vector3.one * (BODY_SCALE_MUL * scale);

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
            }
            _img.texture = _rt;
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
            camGo.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 透明底,UI 背景透出
            _cam.orthographic = true;
            _cam.orthographicSize = ORTHO_FULL_HEIGHT * 0.5f;
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = 100f;

            var rootGo = new GameObject("ModelRoot");
            rootGo.transform.SetParent(_root.transform, false);
            rootGo.transform.localScale = Vector3.one * ROOT_SCALE;
            _modelRoot = rootGo.transform;

            var yawGo = new GameObject("ModelYaw");
            yawGo.transform.SetParent(_modelRoot, false);
            yawGo.transform.localRotation = Quaternion.Euler(0f, MODEL_YAW, 0f);
            _modelYaw = yawGo.transform;
        }

        /// <summary>RT 尺寸跟随容器(老客户端 createFromPool(parent.width, parent.height)),保证不拉伸变形。</summary>
        private static void EnsureRenderTexture(RectTransform container)
        {
            int w = Mathf.Clamp(Mathf.RoundToInt(container.rect.width), 64, 2048);
            int h = Mathf.Clamp(Mathf.RoundToInt(container.rect.height), 64, 2048);
            if (_rt != null && _rt.width == w && _rt.height == h) return;
            if (_rt != null)
            {
                _cam.targetTexture = null;
                _rt.Release();
                Object.Destroy(_rt);
            }
            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "UIModelStageRT" };
            _cam.targetTexture = _rt;
            if (_img != null) _img.texture = _rt;
        }
    }
}
