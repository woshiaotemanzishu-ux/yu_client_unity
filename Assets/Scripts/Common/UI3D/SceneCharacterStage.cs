using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 场景内 3D 角色合成台(Laya Scene3D 层在 2D 场景之上合成的 Unity 对等物)。
    ///
    /// 为什么需要它:本工程根 Canvas 是 <see cref="RenderMode.ScreenSpaceOverlay"/>,UGUI 地图(不透明)
    /// 会盖住主相机渲染的任何 3D 物体——主角直接放世界里永远被地图挡住、看不见。沿用本工程
    /// <see cref="UIModelStage"/> 已验证的「专用相机 → RenderTexture → RawImage」套路:把场景角色摆在远离
    /// 原点的隔离区,用一台正交相机渲到 RT,再把 RT 贴成 <see cref="UILayer.Scene"/> 层里一张满屏 RawImage,
    /// 排序压在地图(sortingOrder -100)之上、HUD(Main 层)之下。
    ///
    /// 当前最小目标:让主角「可见 + 播动作 + 朝向 + 地图在脚下滚动」。主角恒居屏幕中心(相机跟随模型),
    /// 故角色摆在台中心、相机对准台中心即可。NPC/怪物后续按 (世界坐标 - 主角焦点) 偏移摆进同一个台,
    /// 共用这台相机一次性合成(可扩展处已留 <see cref="CharsRoot"/>)。
    ///
    /// 取景三个常量(相机俯角 / 正交半高 / 角色落地 Y)属经验值,需 Unity 实跑微调(对标进度里
    /// 「3D 主角在 UGUI 地图上的精确合成 = 待真机验证」)。
    /// </summary>
    public static class SceneCharacterStage
    {
        // 隔离区:远离世界原点,且避开 UIModelStage 的 (500,-500,500),两台 RT 相机互不串画面。
        private static readonly Vector3 STAGE_POS = new Vector3(3000f, -3000f, 3000f);

        // —— 取景经验值(待 Unity 实跑微调)——
        private const float CAMERA_DISTANCE = 30f;   // 相机到角色距离(正交下只影响裁剪,不影响大小)
        // 相机俯角:对齐老客户端场景相机——其 transform.rotation=(0,180,0) 即【俯角 0°(平视)】(ResManager.Preload3DScene)。
        // 2.5D 立体感全部来自模型后倾(见 MODEL_TILT),相机本身不俯视。
        // (之前的 24° 俯角是另一种近似:会让投影落点与逻辑像素竖直错位,且观感与老版不一致,故改回 0°。)
        private const float CAMERA_PITCH = 0f;
        // 正交半高(世界单位)基准值:参考分辨率高 1280 / 200 = 6.4,仅作画布高度取不到时的兜底。
        // ⚠ 不能写死用它:老端场景相机是随舞台高度自适应的(orthographicVerticalSize = stage.height*0.01,
        // 舞台变尺寸时 CameraManager 重设),CanvasScaler(720×1280,match=0.5) 只有恰好 9:16 时画布高才=1280。
        // 写死 6.4 曾导致非 9:16 下 RT 内容整体缩放 k=画布高/1280:偏离屏幕中心的 NPC 被径向错位,
        // 主角走近时错位连续收敛到 0,观感="NPC 滑回原位";名牌(画布单位 1:1)与模型分离同因。
        // 现由 SyncProjection() 按实际画布高度动态设置(9:16 时结果与 6.4 完全一致)。
        private const float ORTHO_SIZE = 6.4f;
        // 模型 2.5D 倾角(绕世界 X 轴,挂父容器;朝向 yaw 挂模型自身,净旋转 = Rx(MODEL_TILT)*Ry(yaw))。
        // 【符号经实机实测确定,勿轻易翻回 +38】相机平视 forward=+Z;要"俯视角色"必须让头朝相机倾(模型 up 的 Z 分量为负):
        //   +38 → up=(0,0.79,+0.62) 头朝 +Z(远离相机)→ 变成"仰视角色、脚朝镜头/脚朝上"(就是踩坑现象);
        //   -38 → up=(0,0.79,-0.62) 头朝 -Z(朝相机)→ 正确俯视。等价于老客户端 Ry(180)*Rx(38)(数学已验证)。
        private const float MODEL_TILT = -38f;
        // 角色模型自身缩放(大小的最终微调旋钮)。ORTHO_SIZE 已与地图 1:1 锁定、不应再动它来调大小;
        // 角色相对世界的大小请改这里。对标老客户端 default_model_scale = 1.1(其 .lh 模型),Unity 导入模型素体尺寸不同,
        // 先取 1.0,实跑对比老版后微调(偏大调小、偏小调大)。
        private const float MODEL_SCALE = 1.0f;
        // 100 像素 = 1 世界单位(对标老客户端 orthoVerticalSize = stageH*0.01;与 ORTHO_SIZE 同源)。
        private const float PIXELS_PER_UNIT = 100f;
        // 模型根点若不在脚底,用它做竖直微调(屏幕参考像素;>0 把脚底再往下挪)。默认 0:
        // 先按"脚底 = 地图焦点落点(SceneMapView.SceneLayerYOffset)"对齐,实跑发现脚没踩在逻辑格上再调这一个值。
        private const float FEET_TUNE_PX = 0f;
        private const int SORTING_ORDER = -50;       // 地图(-100)之上、HUD(默认 0)之下

        private static GameObject _root;
        private static Camera _cam;
        private static RenderTexture _rt;
        private static RawImage _img;
        private static Transform _charsRoot;
        private static GameObject _mainRole;
        private static GameObject _mainRoleTilt; // 主角的 2.5D 后倾父容器(模型挂其下;销毁它即连模型一起清掉)

        /// <summary>场景角色挂载根(隔离区中心)。NPC/怪物后续按相对主角的偏移摆进来。</summary>
        public static Transform CharsRoot
        {
            get { EnsureStage(); return _charsRoot; }
        }

        /// <summary>
        /// 把装配好的主角模型放上合成台:置于台中心、落地、面向相机。返回后由 MainRoleAgent 继续驱动
        /// (转向改模型 yaw、播动作读模型上的 Animation),本台只负责「被看见」。
        /// </summary>
        public static void SetMainRole(GameObject model, float modelScale = MODEL_SCALE)
        {
            if (model == null) return;
            EnsureStage();
            EnsureView();

            if (_mainRoleTilt != null)
            {
                _mainRoleTilt.SetActive(false); // Destroy 延迟到帧末；先退台，避免旧档案干扰新模型的渲染口径
                Object.Destroy(_mainRoleTilt); // 连同其下旧模型一起销毁
            }
            _mainRole = model;

            // 2.5D 后倾容器(对标老客户端 modelObj_transform 的世界 X 轴后倾 Rx(38)):
            // 倾斜挂父容器、朝向(yaw)挂模型自身 → 净旋转 = Rx(MODEL_TILT) * Ry(yaw),无论朝向如何都恒定朝屏幕后倾,
            // 不会出现"侧身时往旁边倒"的问题。落地点偏移也放在该容器上(处于未倾斜的 _charsRoot 帧内,竖直位置可控)。
            GameObject tiltGo = new GameObject("MainRoleTilt");
            _mainRoleTilt = tiltGo;
            tiltGo.transform.SetParent(_charsRoot, false);
            // 竖直对齐:把"脚底"放到地图画出该逻辑像素的同一屏幕处。地图把焦点(主角像素)画在屏幕中心下方
            // SceneMapView.SceneLayerYOffset 像素处,故脚底也要在中心下方同样的像素量;按 100px/单位换算到世界。
            // 横向已由 SyncModelScreenOffset 的 (role - camera) 跟随,这里只定竖直中性落点(相机平视,-Y = 屏幕下方)。
            float feetDropWorld = (SceneMapView.SceneLayerYOffset + FEET_TUNE_PX) / PIXELS_PER_UNIT;
            tiltGo.transform.localPosition = new Vector3(0f, -feetDropWorld, 0f);
            tiltGo.transform.localRotation = Quaternion.Euler(MODEL_TILT, 0f, 0f);
            tiltGo.transform.localScale = Vector3.one;

            model.transform.SetParent(tiltGo.transform, false);
            model.transform.localPosition = Vector3.zero;
            // 默认朝向:美术正脸朝本地 -Z,旋 180°(对标 SceneRotateY = 180)后静止时背对相机(默认待机姿态);
            // 移动时由 MainRoleAgent.Face 改模型自身 yaw 覆盖(后倾在父容器上,Face 改 yaw 不受影响)。
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = new Vector3(modelScale, modelScale, modelScale);

            UpdateArtAmbient();
            if (_img != null) _img.gameObject.SetActive(true);
        }

        /// <summary>
        /// 把主角模型在屏幕上平移到「逻辑像素 (role.X,role.Y) 在地图上被画出的位置」。
        /// 传入相对相机焦点的像素偏移 <paramref name="pixelOffset"/> = (role.X - cameraX, role.Y - cameraY)
        /// (舞台坐标:x 右、y 下)。地图内部(相机未夹边)该偏移为 0,模型保持原经验落点不动;
        /// 靠近地图边缘相机夹紧、焦点不再跟随时,偏移随之增大,主角滑向屏幕边缘——对标老客户端
        /// 角色挂世界层、相机夹边后角色滑向屏幕边(CameraManager.UpdateCamera / WorldToViewPort)。
        ///
        /// 实现:整块满屏 RawImage(承载主角 RT)按 (dx,-dy) 平移即得屏幕上 (右,下) 位移
        /// (UGUI y 向上,与屏幕/地图 y 向下相反故 y 取反)。当前台上只有主角,平移整图即可;
        /// 后续接入 NPC/怪物时应改为按各自世界坐标摆进台内(见 <see cref="CharsRoot"/> 注释)。
        /// 注:此处不改主角在台内的「落地点」经验值(GROUND_FRACTION / 老版 +100 偏移),只追加相机偏移量;
        /// 落地点与逻辑格的精确竖直对齐仍属「待真机微调」项。
        /// </summary>
        public static void SetMainRoleScreenOffset(Vector2 pixelOffset)
        {
            if (_img == null) return; // 合成画面尚未建立(主角未上台):忽略
            RectTransform rt = _img.rectTransform;
            Vector2 d = new Vector2(pixelOffset.x, -pixelOffset.y);
            rt.offsetMin = d;
            rt.offsetMax = d;
        }

        // ===================== 场景配角(NPC / 怪物)=====================
        // 与主角共用同一台相机 / RT / 满屏 RawImage:配角作为 _charsRoot 下的独立 tilt 子节点摆进来,
        // 一次性合成。水平/竖直基线与 SetMainRole 完全一致(屏右=世界+X、屏下=世界-Y、100px/单位、
        // 落地点 = SceneLayerYOffset 像素),故偏移 (0,0) 时与主角同高同列。相机夹边的整图偏移由主角那张
        // RawImage 统一承载(配角同处一图,自动随之滑动 → 配角净屏幕位 = 自身世界像素 - 相机 = 锚在地图上)。

        /// <summary>
        /// 把一个场景配角(NPC/怪物)模型加进合成台,返回其 tilt 容器(调用方持有:用
        /// <see cref="SetSceneCharacterPixelOffset"/> 每帧按"配角世界像素 - 主角世界像素"摆位,
        /// 用 <see cref="RemoveSceneCharacter"/> 销毁)。不影响主角自身那条路径。
        /// </summary>
        public static Transform AddSceneCharacter(GameObject model, float modelScale = MODEL_SCALE)
        {
            if (model == null) return null;
            EnsureStage();
            EnsureView();

            GameObject tiltGo = new GameObject("SceneCharTilt");
            tiltGo.transform.SetParent(_charsRoot, false);
            tiltGo.transform.localRotation = Quaternion.Euler(MODEL_TILT, 0f, 0f); // 与主角同一 2.5D 后倾
            tiltGo.transform.localScale = Vector3.one;

            model.transform.SetParent(tiltGo.transform, false);
            model.transform.localPosition = Vector3.zero;
            // 默认朝向同主角待机(美术正脸朝本地 -Z,旋 180 后背对相机)。NPC 的 brith_rot 需 config_npc,
            // 该配置未导入前用默认朝向(见 NpcRenderer 的 blocker 说明)。
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = new Vector3(modelScale, modelScale, modelScale);

            UpdateArtAmbient();
            if (_img != null) _img.gameObject.SetActive(true);
            return tiltGo.transform;
        }

        /// <summary>
        /// 按"配角世界像素 - 主角世界像素"(舞台坐标:x 右、y 下)把配角摆到台内对应位置。
        /// 竖直基线与主角一致,故偏移 (0,0) 时配角与主角同一落地点。
        /// </summary>
        public static void SetSceneCharacterPixelOffset(Transform sceneCharTilt, Vector2 pixelOffsetFromRole)
        {
            if (sceneCharTilt == null) return;
            float feetDropWorld = (SceneMapView.SceneLayerYOffset + FEET_TUNE_PX) / PIXELS_PER_UNIT;
            float wx = pixelOffsetFromRole.x / PIXELS_PER_UNIT;
            float wy = -pixelOffsetFromRole.y / PIXELS_PER_UNIT; // 屏下(像素 y 增大)= 世界 -Y
            sceneCharTilt.localPosition = new Vector3(wx, -feetDropWorld + wy, 0f);
        }

        /// <summary>销毁一个场景配角(连同其下模型)。</summary>
        public static void RemoveSceneCharacter(Transform sceneCharTilt)
        {
            if (sceneCharTilt == null) return;
            sceneCharTilt.gameObject.SetActive(false);
            Object.Destroy(sceneCharTilt.gameObject);
            UpdateArtAmbient();
        }

        /// <summary>清掉主角并隐藏合成画面(切场景/断线时调)。</summary>
        public static void Clear()
        {
            if (_mainRoleTilt != null)
            {
                _mainRoleTilt.SetActive(false);
                Object.Destroy(_mainRoleTilt);
                _mainRoleTilt = null;
                _mainRole = null;
            }
            else if (_mainRole != null) { Object.Destroy(_mainRole); _mainRole = null; }
            if (_ambientHeld) { ArtAmbient.Release(); _ambientHeld = false; } // Destroy 延后生效,这里直接放光
            ConfigureArtRender(null);
            if (_img != null)
            {
                _img.rectTransform.offsetMin = Vector2.zero; // 清掉上一张图遗留的相机偏移,下次上台从居中开始
                _img.rectTransform.offsetMax = Vector2.zero;
                _img.gameObject.SetActive(false);
            }
        }

        // —— 场景环境光(用户定案:场景内也像创角/UI台一样给新模型上亮平光):
        // 台上有任何带渲染档案的新模型(激活中)→ 持有 ArtAmbient;老模型全是 unlit 不吃光,零影响。
        private static bool _ambientHeld;

        private static void UpdateArtAmbient()
        {
            ArtModelRenderProfile profile = _charsRoot != null
                ? _charsRoot.GetComponentInChildren<ArtModelRenderProfile>(false)
                : null;
            bool need = profile != null;
            ConfigureArtRender(profile);
            if (need == _ambientHeld) return;
            _ambientHeld = need;
            if (need) ArtAmbient.Retain();
            else ArtAmbient.Release();
        }

        /// <summary>
        /// 与 UIModelStage/创角整模共用同一渲染档案：按档案切换 renderer，并按需强制
        /// Depth/Opaque Texture；带档案的模型贴回 UGUI 时统一使用预乘合成材质。
        /// </summary>
        private static void ConfigureArtRender(ArtModelRenderProfile profile)
        {
            ArtModelRenderProfile.ApplyToCamera(_cam, profile);
            EnsureRenderTexture();
            if (_img != null)
                _img.material = profile != null ? UIModelStage.CompositeMaterial() : null;
        }

        private static void EnsureStage()
        {
            if (_root != null) return;
            _root = new GameObject("__SceneCharStage");
            Object.DontDestroyOnLoad(_root);
            _root.transform.position = STAGE_POS;

            var charsGo = new GameObject("Chars");
            charsGo.transform.SetParent(_root.transform, false);
            _charsRoot = charsGo.transform;

            var camGo = new GameObject("SceneCharCamera");
            camGo.transform.SetParent(_root.transform, false);
            // 正前方略偏上,带俯角看向台中心。
            Vector3 offset = Quaternion.Euler(CAMERA_PITCH, 0f, 0f) * new Vector3(0f, 0f, -CAMERA_DISTANCE);
            camGo.transform.localPosition = offset;
            camGo.transform.LookAt(_charsRoot, Vector3.up);

            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 透明底,地图透出
            _cam.orthographic = true;
            _cam.orthographicSize = ORTHO_SIZE; // 随后 SyncProjection 按实际画布高度覆盖
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = CAMERA_DISTANCE * 2f + 10f;
            SyncProjection();
        }

        /// <summary>
        /// 合成相机投影随实际画布高度自适应(对标老端 camera.orthographicVerticalSize = stage.height*0.01,
        /// 舞台变尺寸时重设)。保证任意分辨率下"100 画布像素 = 1 世界单位"成立,场景角色与地图像素严格 1:1
        /// 锚定(NPC 落位不随宽高比漂移)、名牌与模型重合。画布高度取不到时兜底参考分辨率 1280(=旧常量行为)。
        /// 调用时机:建台(EnsureStage)/建视图(EnsureView)/RT 重建(EnsureRenderTexture,自带屏幕尺寸变化检测)。
        /// </summary>
        private static void SyncProjection()
        {
            if (_cam == null) return;
            float canvasH = 0f;
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer != null)
            {
                Canvas canvas = sceneLayer.GetComponentInParent<Canvas>();
                if (canvas != null) canvasH = ((RectTransform)canvas.transform).rect.height;
            }
            if (canvasH <= 0f) canvasH = 2f * ORTHO_SIZE * PIXELS_PER_UNIT; // 1280
            _cam.orthographicSize = canvasH / (2f * PIXELS_PER_UNIT);
        }

        /// <summary>在 Scene 层建满屏 RawImage 展示 RT(自带 Canvas 压在地图之上、HUD 之下)。</summary>
        private static void EnsureView()
        {
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null) return;

            EnsureRenderTexture();
            SyncProjection();

            if (_img != null && _img.transform.parent == sceneLayer)
            {
                _img.texture = _rt;
                return;
            }
            if (_img != null) Object.Destroy(_img.gameObject);

            var go = new GameObject("__SceneChars",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(sceneLayer, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SORTING_ORDER;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _img = go.GetComponent<RawImage>();
            _img.raycastTarget = false; // 不吃点击,场景输入照常落到下面的输入板
            _img.texture = _rt;
            _img.material = _ambientHeld ? UIModelStage.CompositeMaterial() : null;
        }

        private static void EnsureRenderTexture()
        {
            int w = Mathf.Clamp(Screen.width, 64, 4096);
            int h = Mathf.Clamp(Screen.height, 64, 4096);
            if (_rt != null && _rt.width == w && _rt.height == h) return;
            if (_rt != null)
            {
                if (_cam != null) _cam.targetTexture = null;
                _rt.Release();
                Object.Destroy(_rt);
            }
            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "SceneCharStageRT" };
            ClearRenderTexture(_rt);
            if (_cam != null) _cam.targetTexture = _rt;
            if (_img != null) _img.texture = _rt;
            SyncProjection(); // 屏幕尺寸变化 → 画布高度大概率也变了,随 RT 重建一起重算投影
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
