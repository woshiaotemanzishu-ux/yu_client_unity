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
        // 正交半高(世界单位)= 参考分辨率高 / 200。老客户端场景相机 orthographicVerticalSize(全高)= stage.height*0.01,
        // 即 100 像素/世界单位;Unity 用的是半高,故 1280/200 = 6.4——与地图严格 1:1 锁定(地图也是 1 像素 = 1 参考像素)。
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
        public static void SetMainRole(GameObject model)
        {
            if (model == null) return;
            EnsureStage();
            EnsureView();

            if (_mainRoleTilt != null)
            {
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
            model.transform.localScale = new Vector3(MODEL_SCALE, MODEL_SCALE, MODEL_SCALE);

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

        /// <summary>清掉主角并隐藏合成画面(切场景/断线时调)。</summary>
        public static void Clear()
        {
            if (_mainRoleTilt != null) { Object.Destroy(_mainRoleTilt); _mainRoleTilt = null; _mainRole = null; }
            else if (_mainRole != null) { Object.Destroy(_mainRole); _mainRole = null; }
            if (_img != null)
            {
                _img.rectTransform.offsetMin = Vector2.zero; // 清掉上一张图遗留的相机偏移,下次上台从居中开始
                _img.rectTransform.offsetMax = Vector2.zero;
                _img.gameObject.SetActive(false);
            }
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
            _cam.orthographicSize = ORTHO_SIZE;
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = CAMERA_DISTANCE * 2f + 10f;
        }

        /// <summary>在 Scene 层建满屏 RawImage 展示 RT(自带 Canvas 压在地图之上、HUD 之下)。</summary>
        private static void EnsureView()
        {
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null) return;

            EnsureRenderTexture();

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
            if (_cam != null) _cam.targetTexture = _rt;
            if (_img != null) _img.texture = _rt;
        }
    }
}
