using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// UI 内 3D 模型展示台(Laya UIModelClass3D 的 Unity 对等物):
    /// 隔离区(远离原点)摆模型 → 专用正交相机渲到 RenderTexture → RawImage 贴进 UI 容器。
    /// 取景复刻老客户端 UIModelClass3D.ts 的固定参数(正交相机 + 层级缩放 + 配置位移)。
    ///
    /// 【多实例规则】本类是【可实例化】的:每个实例有独立的隔离区(按序号偏移 x)、相机、RT、RawImage,互不干扰。
    ///   - 互斥使用的界面(背包/角色/时装/对话/登录 等同一时刻只开一个)→ 用【静态接口】UIModelStage.Show/ShowInstance/Clear,
    ///     它们共享一个默认实例(Default),省内存且无冲突。
    ///   - 常驻 / 需要与别的模型面板【同时显示】的(如主界面循环冲榜榜单)→ 必须 new UIModelStage() 持有【独立实例】,
    ///     用 PlaceInstance/ClearStage,并在视图销毁时 Dispose()。否则会被静态接口的调用方抢占/清空。
    /// </summary>
    public sealed class UIModelStage
    {
        // —— 老客户端 UIModelClass3D.ts 固定参数(逐行对标)——
        private const float ORTHO_FULL_HEIGHT = 12.8f; // camera.orthographicVerticalSize = 12.8(全高;Unity orthographicSize 是半高)
        private const float CAMERA_Z = -20f;           // Set3DLocalPosition(camera, ..., -20)

        // 整模用透视相机(美术工程 Main.unity 同款 FOV):新美术出场动画的位移大量沿 Z(镜头深度),
        // 正交投影下深度移动不可见(=看着原地做动作,实锤);透视才能还原"从远处飞过来"。
        // 距离按"落点平面(z=0)取景高度仍=12.8"反算,页面构图与正交时代一致。
        private const float ART_FOV = 60f;
        private const float ROOT_SCALE = 1.1f;         // default_model_scale = 1.1
        private const float BODY_SCALE_MUL = 5f;       // Set3DLocalScale(transform, 5 * data.scale, ...)
        private const float BASE_Y = -5f;              // pos_y = ... - 5(模型根在相机中心下方 5,再加 position.y 配置)
        public const float MODEL_YAW = 180f;           // 默认 rotate = (0, 180, 0),模型转身面向相机

        // Laya UI 相机 rotY180 → 屏幕x = 世界-X;Unity 相机屏幕x = +X,同一几何互为镜像 → 渲染层把 RT 水平翻转补偿。
        private static readonly Rect FLIP_HORIZONTAL = new Rect(1f, 0f, -1f, 1f);

        // 每个实例一个隔离区:位置 = (500 + index*1000, -500, 500)。间距 1000 远大于相机 far clip(100),
        // 故各台相机只拍自己的模型,互不穿帮,也拍不到原点附近的主场景。
        private static readonly Vector3 STAGE_BASE = new Vector3(500f, -500f, 500f);
        private const float STAGE_SPACING = 1000f;
        private static int _nextIndex;

        private readonly Vector3 _stagePos;
        private GameObject _root;
        private Camera _cam;
        private RenderTexture _rt;
        private RawImage _img;
        private Transform _modelRoot; // 对标 root_transform(缩放 1.1 + 位移)
        private Transform _modelYaw;  // 对标 model_transform(旋转用)
        private GameObject _model;

        public UIModelStage()
        {
            _stagePos = STAGE_BASE + new Vector3(STAGE_SPACING * _nextIndex++, 0f, 0f);
        }

        // ——————————————— 静态默认实例 + 转发(兼容互斥使用的现有调用方)———————————————
        private static UIModelStage _default;
        public static UIModelStage Default => _default ?? (_default = new UIModelStage());

        public static void Show(RectTransform container, GameObject modelPrefab,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW)
            => Default.Place(container, modelPrefab, scale, position, yaw);

        public static void ShowInstance(RectTransform container, GameObject modelInstance,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW)
            => Default.PlaceInstance(container, modelInstance, scale, position, yaw);

        public static void Clear() => Default.ClearStage();

        // ——————————————— 实例 API ———————————————
        /// <summary>实例化 prefab 上台(所有权交给本台)。</summary>
        public void Place(RectTransform container, GameObject modelPrefab,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW)
        {
            if (modelPrefab == null) return;
            PlaceInstance(container, Object.Instantiate(modelPrefab), scale, position, yaw);
        }

        /// <summary>已组装好的实例上台,所有权交给本台。</summary>
        public void PlaceInstance(RectTransform container, GameObject modelInstance,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW)
        {
            if (container == null || modelInstance == null)
            {
                if (modelInstance != null) Object.Destroy(modelInstance);
                return;
            }
            EnsureStage();
            EnsureRenderTexture(container);

            if (_model != null) Object.Destroy(_model);
            _baseYaw = yaw;
            _userYaw = 0f; // 换人/换模型回正,拖拽旋转从默认朝向重新开始(对标老端)
            _modelYaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _model = modelInstance;
            _model.transform.SetParent(_modelYaw, false);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            _model.transform.localScale = Vector3.one * (BODY_SCALE_MUL * scale);
            bool isArtModel = ApplyRenderProfile(_model);
            SetArtAmbient(isArtModel);
            _displayFlipped = !isArtModel; // 老模型 FLIP 镜像展示 → 拖拽转身方向取反(见 AddUserYaw)

            // 摆位分流(选角"镜头偏右"实锤根因,2026-07-11):
            //  老模型(正交):config 偏移直接挪 3D——正交是平行投影,离轴=纯平移,行为与从前一致;
            //  新模型(透视):模型离轴=被斜视(等效视口偏转)。故模型中心必须锁在相机光轴上
            //  (落点=脚底在容器原点,抬半个归一身高即中心对轴),config 构图偏移改挪 2D 贴图(见下)。
            if (isArtModel)
            {
                float stageHeight = 2.33f * BODY_SCALE_MUL * scale * ROOT_SCALE; // 导入烤入的归一身高 × 台上缩放链
                _modelRoot.localPosition = new Vector3(0f, -stageHeight * 0.5f, 0f);
            }
            else
            {
                _modelRoot.localPosition = new Vector3(position.x, position.y + BASE_Y, 0f);
            }

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
                _img.uvRect = FLIP_HORIZONTAL;
                var drag = go.AddComponent<UIModelDragRotate>(); // 拖拽旋转(是否命中由 raycastTarget 开关)
                drag.Stage = this;
            }
            _img.raycastTarget = _dragRotate; // 开了拖拽才吃指针;按钮等 UI 在其上层,优先命中不受影响
            _img.texture = _rt;
            // 整模(带渲染档案)换预乘合成材质:加法特效渲到透明 RT 再按默认 SrcAlpha 贴 UI 会洗成白块;
            // 老模型 material=null 走 UI 默认材质,行为与从前一致
            _img.material = isArtModel ? CompositeMaterial() : null;
            // 水平翻转只给 Laya 转换的老模型(它们的几何本来就是镜像的,翻一次才正);
            // 新美术成品是原生 Unity 朝向,再翻=镜像(武器换手)——创角整模时代实锤的铁律
            _img.uvRect = isArtModel ? new Rect(0f, 0f, 1f, 1f) : FLIP_HORIZONTAL;
            // 新模型的页面构图偏移挪 2D 贴图(模型本体锁光轴防斜视);老模型偏移在 3D,贴图归零。
            // 基准换算必须复刻老构图:老路径模型可视中心 = pos + (0, BASE_Y + 半身高);新路径模型
            // 渲在画面正中,把贴图平移到同一构图点 → 页面位置与老基准一致(漏掉基准项=人飘上天,实锤)。
            Vector2 imgOffset = Vector2.zero;
            if (isArtModel)
            {
                float pxPerUnit = container.rect.height / ORTHO_FULL_HEIGHT; // 12.8 台上单位 = 容器全高
                float stageHeight = 2.33f * BODY_SCALE_MUL * scale * ROOT_SCALE;
                imgOffset = new Vector2(position.x, position.y + BASE_Y + stageHeight * 0.5f) * pxPerUnit;
            }
            _img.rectTransform.offsetMin = imgOffset;
            _img.rectTransform.offsetMax = imgOffset;
            _img.gameObject.SetActive(true);
            // 镜像/口径排查诊断:art=按激活子树认定的整模判定,flip=是否套了 Laya 镜像补偿
            Shenxiao.Framework.Util.GameLog.Info("UI3D",
                "UI台上台:{0} art={1} flip={2} 相机={3} 容器={4}x{5}",
                modelInstance.name, isArtModel, !isArtModel,
                _cam.orthographic ? "正交12.8" : "透视60",
                Mathf.RoundToInt(container.rect.width), Mathf.RoundToInt(container.rect.height));
        }

        private static Material _compositeMat;

        private static Material CompositeMaterial()
        {
            if (_compositeMat == null)
            {
                Shader shader = Shader.Find("Shenxiao/UI/StageComposite");
                if (shader == null)
                {
                    Debug.LogWarning("[UIModelStage] 找不到 Shenxiao/UI/StageComposite,整模特效在 UI 上可能发白");
                    return null;
                }
                _compositeMat = new Material(shader);
            }
            return _compositeMat;
        }

        /// <summary>
        /// 按模型自带的渲染档案配置本台相机(ArtImport 导入的成品模型:独立 renderer + 强制
        /// Depth/Opaque Texture,PandaShader 软粒子/扭曲依赖);不带档案(所有老模型)则还原
        /// 默认——相机行为与从前完全一致,老模型渲染路径零改动。返回是否带档案(决定合成材质)。
        /// </summary>
        private bool ApplyRenderProfile(GameObject model)
        {
            if (_cam == null) return false;
            UniversalAdditionalCameraData camData = _cam.GetUniversalAdditionalCameraData();
            if (camData == null) return false;

            // 只看【激活中】的子树:混合模型(ReplaceableRoleModel)容器里新老实例并存,
            // 亮着的才是正在展示的那个——按它决定相机/合成,而不是"藏着新模型就当整模"
            ArtModelRenderProfile profile =
                model != null ? model.GetComponentInChildren<ArtModelRenderProfile>(false) : null;
            if (profile != null)
            {
                camData.SetRenderer(profile.useDedicatedRenderer && profile.rendererIndex >= 0
                    ? profile.rendererIndex : -1); // -1 = RP Asset 默认 renderer
                camData.requiresDepthOption = profile.forceDepthTexture
                    ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
                camData.requiresColorOption = profile.forceOpaqueTexture
                    ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
                // 透视相机:深度方向的出场位移才看得见;距离保证 z=0(落点)平面取景高度不变
                _cam.orthographic = false;
                _cam.fieldOfView = ART_FOV;
                _cam.transform.localPosition = new Vector3(0f, 0f,
                    -(ORTHO_FULL_HEIGHT * 0.5f) / Mathf.Tan(ART_FOV * 0.5f * Mathf.Deg2Rad));
                return true;
            }

            camData.SetRenderer(-1);
            camData.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            camData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
            _cam.orthographic = true;
            _cam.orthographicSize = ORTHO_FULL_HEIGHT * 0.5f;
            _cam.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);
            return false;
        }

        /// <summary>清掉当前模型并隐藏贴图(台子/相机/RT 保留,可再 Place)。</summary>
        public void ClearStage()
        {
            if (_model != null) { Object.Destroy(_model); _model = null; }
            SetArtAmbient(false);
            if (_img != null) _img.gameObject.SetActive(false);
        }

        /// <summary>彻底销毁本台(独立实例的视图销毁时调用,释放相机/RT/RawImage)。</summary>
        public void Dispose()
        {
            ClearStage();
            SetArtAmbient(false);
            if (_img != null) { Object.Destroy(_img.gameObject); _img = null; }
            if (_rt != null)
            {
                if (_cam != null) _cam.targetTexture = null;
                _rt.Release();
                Object.Destroy(_rt);
                _rt = null;
            }
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        private void EnsureStage()
        {
            if (_root != null) return;
            _root = new GameObject("__UIModelStage");
            Object.DontDestroyOnLoad(_root);
            _root.transform.position = _stagePos;

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

        // —— 拖拽旋转(对标老客户端:模型区横向拖动=左右转身,无缩放/无平移)——
        private bool _dragRotate;
        private bool _displayFlipped; // 老模型走 FLIP 镜像展示:画面左右反了,拖拽方向要跟着反才顺手
        private float _baseYaw = MODEL_YAW;
        private float _userYaw;

        /// <summary>开关默认台的拖拽旋转(选角等页 OnShow 开、OnHide 关;不开时画面贴图不吃指针)。</summary>
        public static void SetDragRotate(bool on) => Default.EnableDragRotate(on);

        public void EnableDragRotate(bool on)
        {
            _dragRotate = on;
            if (_img != null) _img.raycastTarget = on;
        }

        /// <summary>拖拽增量转身(UIModelDragRotate 回调):只动 yaw,基准朝向来自 ShowInstance。
        /// 老模型的画面是镜像贴的(Laya 补偿),同样的物理旋转在屏幕上看是反的——增量取反对齐手感。</summary>
        public void AddUserYaw(float degrees)
        {
            _userYaw += _displayFlipped ? -degrees : degrees;
            if (_modelYaw != null)
                _modelYaw.localRotation = Quaternion.Euler(0f, _baseYaw + _userYaw, 0f);
        }

        /// <summary>拖拽结束回调:把当前朝向偏移吐到日志——拖到满意的角度后,把这个数填进
        /// configlogin 对应页的 NewModel.yaw,默认朝向就固定成这个角度。</summary>
        public void ReportUserYaw()
        {
            float normalized = Mathf.Repeat(_userYaw + 180f, 360f) - 180f; // 归一到 ±180
            Shenxiao.Framework.Util.GameLog.Info("UI3D",
                "拖拽朝向偏移 {0}°(固定它:填进 configlogin 该页 NewModel.yaw)", Mathf.Round(normalized));
        }

        // —— 整模环境光(定案:用环境光,不用平行光;实现收编进 ArtAmbient 引用计数,
        // UI 台+场景台共用,谁有新模型谁持有,全放光了才恢复)——
        private bool _ambientHeld;

        private void SetArtAmbient(bool on)
        {
            if (on == _ambientHeld) return;
            _ambientHeld = on;
            if (on) ArtAmbient.Retain();
            else ArtAmbient.Release();
        }

        /// <summary>RT 尺寸跟随容器(老客户端 createFromPool(parent.width, parent.height)),保证不拉伸变形。</summary>
        private void EnsureRenderTexture(RectTransform container)
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
            ClearRenderTexture(_rt);
            _cam.targetTexture = _rt;
            if (_img != null) _img.texture = _rt;
        }

        private static void ClearRenderTexture(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }
    }
}
