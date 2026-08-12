using UnityEngine;
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
        private const float ART_REFERENCE_HEIGHT = 1.7657f; // 1400 idle 紧致蒙皮高度，仅用于构图中心估算
        private const float BASE_Y = -5f;              // pos_y = ... - 5(模型根在相机中心下方 5,再加 position.y 配置)
        public const float MODEL_YAW = 180f;           // 默认 rotate = (0, 180, 0),模型转身面向相机
        private const RenderTextureFormat MODEL_RT_FORMAT = RenderTextureFormat.ARGBHalf;

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

        // —— 整模(带渲染档案)的当前排布状态:供实时调参工具重排,也是"所见即所得"输出配置的真值源 ——
        private bool _isArt;               // 当前展示的是不是整模(带 ArtModelRenderProfile)
        private RectTransform _container;  // 当前贴进的 UI 容器(重排 imgOffset 要用它的高)
        private float _artScaleParam;      // 传进来的显示 scale(= MODEL_SCALE × 页面配置 scale)
        private Vector2 _artPos;           // 构图位移(= ModelPos + 配置 x/y),叠加在 BASE_Y 之上
        private float _artYaw = MODEL_YAW; // 绝对朝向角(= 180 + 配置 yaw)
        private float _artPitch;           // 俯仰角(度,正=相机抬高往下看=俯视,治仰视)

        public UIModelStage()
        {
            _stagePos = STAGE_BASE + new Vector3(STAGE_SPACING * _nextIndex++, 0f, 0f);
        }

        // ——————————————— 静态默认实例 + 转发(兼容互斥使用的现有调用方)———————————————
        private static UIModelStage _default;
        public static UIModelStage Default => _default ?? (_default = new UIModelStage());

        public static void Show(RectTransform container, GameObject modelPrefab,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW, float pitch = 0f)
            => Default.Place(container, modelPrefab, scale, position, yaw, pitch);

        public static void ShowInstance(RectTransform container, GameObject modelInstance,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW, float pitch = 0f)
            => Default.PlaceInstance(container, modelInstance, scale, position, yaw, pitch);

        public static void Clear() => Default.ClearStage();

        /// <summary>立即把当前模型台渲染进 RT；供异步预览完成态和非 PlayMode 截图验收消除首帧空白。</summary>
        public static void RenderNow() => Default.RenderStageNow();

        // ——————————————— 实例 API ———————————————
        /// <summary>实例化 prefab 上台(所有权交给本台)。</summary>
        public void Place(RectTransform container, GameObject modelPrefab,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW, float pitch = 0f)
        {
            if (modelPrefab == null) return;
            PlaceInstance(container, Object.Instantiate(modelPrefab), scale, position, yaw, pitch);
        }

        /// <summary>已组装好的实例上台,所有权交给本台。</summary>
        public void PlaceInstance(RectTransform container, GameObject modelInstance,
            float scale = 1f, Vector2 position = default, float yaw = MODEL_YAW, float pitch = 0f)
        {
            if (container == null || modelInstance == null)
            {
                if (modelInstance != null) Object.Destroy(modelInstance);
                return;
            }
            EnsureStage();
            EnsureRenderTexture(container);

            if (_model != null)
            {
                // Destroy 在 Editor 非 PlayMode 与运行时都延迟到帧尾。套装页连续换模型时，
                // 旧翅膀/武器会被同一台相机再拍一帧，形成部件逐页累积。先失活再销毁，
                // 保证本次 RenderNow 只包含新实例。
                _model.SetActive(false);
                Object.Destroy(_model);
            }
            _baseYaw = yaw;
            _userYaw = 0f; // 换人/换模型回正,拖拽旋转从默认朝向重新开始(对标老端)
            _modelYaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _model = modelInstance;
            _model.transform.SetParent(_modelYaw, false);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            _model.transform.localScale = Vector3.one * (BODY_SCALE_MUL * scale);
            bool isArtModel = ApplyRenderProfile(_model);
            _displayFlipped = !isArtModel; // 老模型 FLIP 镜像展示 → 拖拽转身方向取反(见 AddUserYaw)
            _isArt = isArtModel;
            _container = container;

            // 摆位分流(选角"镜头偏右"实锤根因,2026-07-11):
            //  老模型(正交):config 偏移直接挪 3D——正交是平行投影,离轴=纯平移,行为与从前一致;
            //  新模型(透视):模型离轴=被斜视(等效视口偏转)。故模型中心必须锁在相机光轴上
            //  (落点=脚底在容器原点,抬半个归一身高即中心对轴),config 构图偏移改挪 2D 贴图(见下)。
            //  整模的排布(缩放/中心对轴/相机俯仰/imgOffset)统一走 RelayoutArt(_img 建好后再调)。
            if (isArtModel)
            {
                _artScaleParam = scale;
                _artPos = position;
                _artYaw = yaw;
                _artPitch = pitch;
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
            if (isArtModel)
            {
                RelayoutArt(); // 缩放/中心对轴/相机俯仰/imgOffset 一并按 _art* 状态排好(_img 已建)
            }
            else
            {
                _img.rectTransform.offsetMin = Vector2.zero;
                _img.rectTransform.offsetMax = Vector2.zero;
            }
            _img.gameObject.SetActive(true);
            // 镜像/口径排查诊断:art=按激活子树认定的整模判定,flip=是否套了 Laya 镜像补偿
            Shenxiao.Framework.Util.GameLog.Info("UI3D",
                "UI台上台:{0} art={1} flip={2} 相机={3} 容器={4}x{5}",
                modelInstance.name, isArtModel, !isArtModel,
                _cam.orthographic ? "正交12.8" : "透视60",
                Mathf.RoundToInt(container.rect.width), Mathf.RoundToInt(container.rect.height));
        }

        private static Material _compositeMat;

        /// <summary>
        /// 透明 RT 的预乘合成材质。场景角色台与 UI 展示台共用同一口径，避免默认
        /// SrcAlpha 把已经写进 RT 的加法光再次乘 alpha。
        /// </summary>
        internal static Material CompositeMaterial()
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

            // 只看【激活中】的子树:混合模型(ReplaceableRoleModel)容器里新老实例并存,
            // 亮着的才是正在展示的那个——按它决定相机/合成,而不是"藏着新模型就当整模"
            ArtModelRenderProfile profile =
                model != null ? model.GetComponentInChildren<ArtModelRenderProfile>(false) : null;
            if (profile != null)
            {
                ArtModelRenderProfile.ApplyToCamera(_cam, profile);
                // 透视相机:深度方向的出场位移才看得见;距离保证 z=0(落点)平面取景高度不变
                _cam.orthographic = false;
                _cam.fieldOfView = ART_FOV;
                _cam.transform.localPosition = new Vector3(0f, 0f,
                    -(ORTHO_FULL_HEIGHT * 0.5f) / Mathf.Tan(ART_FOV * 0.5f * Mathf.Deg2Rad));
                return true;
            }

            ArtModelRenderProfile.ApplyToCamera(_cam, null);
            _cam.orthographic = true;
            _cam.orthographicSize = ORTHO_FULL_HEIGHT * 0.5f;
            _cam.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);
            _cam.transform.localRotation = Quaternion.identity; // 还原:上一个整模可能把相机俯仰过
            return false;
        }

        // ——————————————— 整模排布 + 实时调参 API(供 ArtModelTuner 所见即所得拧)———————————————

        /// <summary>
        /// 按当前 _art* 状态把整模排好:朝向 yaw、体量缩放、中心锁相机光轴、相机俯仰、构图 imgOffset。
        /// 缩放与 stageHeight 同步用 _artScaleParam,故缩放后脚底仍归一,不飘;俯仰绕构图中心转相机,模型不出框。
        /// </summary>
        private void RelayoutArt()
        {
            if (!_isArt || _model == null || _modelRoot == null || _modelYaw == null) return;
            _modelYaw.localRotation = Quaternion.Euler(0f, _artYaw, 0f);
            _model.transform.localScale = Vector3.one * (BODY_SCALE_MUL * _artScaleParam);
            float stageHeight = ART_REFERENCE_HEIGHT * BODY_SCALE_MUL * _artScaleParam * ROOT_SCALE;
            _modelRoot.localPosition = new Vector3(0f, -stageHeight * 0.5f, 0f);
            ApplyArtCameraPitch(_artPitch);
            if (_img != null && _container != null)
            {
                float pxPerUnit = _container.rect.height / ORTHO_FULL_HEIGHT; // 12.8 台上单位 = 容器全高
                Vector2 imgOffset =
                    new Vector2(_artPos.x, _artPos.y + BASE_Y + stageHeight * 0.5f) * pxPerUnit;
                _img.rectTransform.offsetMin = imgOffset;
                _img.rectTransform.offsetMax = imgOffset;
            }
        }

        /// <summary>相机绕构图中心(_root 空间 y=0)做俯仰:pitch>0 抬高相机往下看=俯视(治仰视),模型始终在框内。</summary>
        private void ApplyArtCameraPitch(float pitch)
        {
            if (_cam == null) return;
            float dist = (ORTHO_FULL_HEIGHT * 0.5f) / Mathf.Tan(ART_FOV * 0.5f * Mathf.Deg2Rad);
            float th = pitch * Mathf.Deg2Rad;
            _cam.transform.localPosition = new Vector3(0f, dist * Mathf.Sin(th), -dist * Mathf.Cos(th));
            _cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // 当前整模排布真值(实时调参工具读它显示、松手/输出时换算回配置)。
        public bool IsArt => _isArt && _model != null;
        public float ArtYaw => _artYaw;               // 绝对朝向(配置 yaw = ArtYaw - MODEL_YAW)
        public float ArtPitch => _artPitch;
        public float ArtScaleParam => _artScaleParam; // 显示 scale(配置 scale = ArtScaleParam / 页面基准 scale)
        public Vector2 ArtPosition => _artPos;        // 构图位移(配置 x/y = ArtPosition - 页面 ModelPos)
        public AnimatedAttachmentPositionFollower ActiveAttachmentFollower =>
            _model != null
                ? _model.GetComponentInChildren<AnimatedAttachmentPositionFollower>(false)
                : null;

        /// <summary>当前激活实例上的武器对齐器(调参浮层用;武器挂 rhand,对齐 weapon_attach)。</summary>
        public AttachmentSocketAligner ActiveWeaponAligner =>
            _model != null
                ? _model.GetComponentInChildren<AttachmentSocketAligner>(false)
                : null;

        public void SetArtYaw(float absoluteYaw) { _artYaw = absoluteYaw; RelayoutArt(); }
        public void SetArtPitch(float pitch) { _artPitch = pitch; RelayoutArt(); }
        public void SetArtScaleParam(float scaleParam) { _artScaleParam = Mathf.Max(0.01f, scaleParam); RelayoutArt(); }
        public void SetArtPosition(Vector2 pos) { _artPos = pos; RelayoutArt(); }

        /// <summary>清掉当前模型并隐藏贴图(台子/相机/RT 保留,可再 Place)。</summary>
        public void ClearStage()
        {
            if (_model != null)
            {
                _model.SetActive(false);
                Object.Destroy(_model);
                _model = null;
            }
            if (_img != null) _img.gameObject.SetActive(false);
        }

        public void RenderStageNow()
        {
            if (_cam != null && _model != null && _rt != null) _cam.Render();
        }

        /// <summary>模型台实际渲染诊断；给截图验收区分“贴图已绑定”与“相机里真有可渲染物”。</summary>
        public string GetRenderDiagnostics()
        {
            if (_cam == null || _model == null)
                return "camera=" + (_cam != null) + ",model=" + (_model != null);
            Renderer[] renderers = _model.GetComponentsInChildren<Renderer>(true);
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_cam);
            int active = 0;
            int inFrustum = 0;
            Bounds combined = default;
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.forceRenderingOff
                    || !renderer.gameObject.activeInHierarchy) continue;
                active++;
                Bounds bounds = renderer.bounds;
                if (!hasBounds) { combined = bounds; hasBounds = true; }
                else combined.Encapsulate(bounds);
                if (GeometryUtility.TestPlanesAABB(planes, bounds)) inFrustum++;
            }
            Vector3 viewport = hasBounds ? _cam.WorldToViewportPoint(combined.center) : Vector3.zero;
            return "model=" + _model.name
                + ",active=" + _model.activeInHierarchy
                + ",renderers=" + renderers.Length
                + ",enabled=" + active
                + ",inFrustum=" + inFrustum
                + ",boundsCenter=" + (hasBounds ? combined.center.ToString("F2") : "none")
                + ",boundsSize=" + (hasBounds ? combined.size.ToString("F2") : "none")
                + ",viewport=" + viewport.ToString("F2")
                + ",camera=" + _cam.transform.position.ToString("F2")
                + ",mask=" + _cam.cullingMask;
        }

        /// <summary>彻底销毁本台(独立实例的视图销毁时调用,释放相机/RT/RawImage)。</summary>
        public void Dispose()
        {
            ClearStage();
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
            if (Application.isPlaying) Object.DontDestroyOnLoad(_root);
            _root.transform.position = _stagePos;

            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(_root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, CAMERA_Z);
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 透明底,UI 背景透出
            _cam.allowHDR = true;
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
            if (_isArt)
            {
                SetArtYaw(_artYaw + degrees); // 整模原生朝向(不镜像),拖拽直接加在绝对朝向上,与调参工具同源
                return;
            }
            _userYaw += _displayFlipped ? -degrees : degrees;
            if (_modelYaw != null)
                _modelYaw.localRotation = Quaternion.Euler(0f, _baseYaw + _userYaw, 0f);
        }

        /// <summary>拖拽结束回调:把当前朝向偏移吐到日志——拖到满意的角度后,把这个数填进
        /// configlogin 对应页的 NewModel.yaw,默认朝向就固定成这个角度。</summary>
        public void ReportUserYaw()
        {
            float offset = _isArt
                ? Mathf.Repeat(_artYaw - MODEL_YAW + 180f, 360f) - 180f // 整模:配置 yaw = 绝对朝向 - 180
                : Mathf.Repeat(_userYaw + 180f, 360f) - 180f;           // 归一到 ±180
            Shenxiao.Framework.Util.GameLog.Info("UI3D",
                "拖拽朝向偏移 {0}°(固定它:填进 configlogin 该页 NewModel.yaw)", Mathf.Round(offset));
        }

        /// <summary>RT 尺寸跟随容器(老客户端 createFromPool(parent.width, parent.height)),保证不拉伸变形。</summary>
        private void EnsureRenderTexture(RectTransform container)
        {
            int w = Mathf.Clamp(Mathf.RoundToInt(container.rect.width), 64, 2048);
            int h = Mathf.Clamp(Mathf.RoundToInt(container.rect.height), 64, 2048);
            if (_rt != null && _rt.width == w && _rt.height == h && _rt.format == MODEL_RT_FORMAT) return;
            if (_rt != null)
            {
                _cam.targetTexture = null;
                _rt.Release();
                Object.Destroy(_rt);
            }
            // 美术 Panda 材质包含 HDR 颜色和多层半透明。ARGB32 会在透明 RT 的中间结果阶段截断 HDR，
            // 上翼膜片会先褪色，再经 UGUI 合成变得不明显；必须保留带 Alpha 的半浮点缓冲。
            _rt = new RenderTexture(w, h, 16, MODEL_RT_FORMAT) { name = "UIModelStageRT" };
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
