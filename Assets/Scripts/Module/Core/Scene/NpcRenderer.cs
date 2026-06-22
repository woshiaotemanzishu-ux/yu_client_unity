using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景 NPC 渲染层(对标老客户端 Scene.CreateNpc → Npc.InitNpc,见 yu_client/h5/src/scene/sceneobj/Npc.ts:92-169)。
    ///
    /// 职责:订阅数据层 <see cref="SceneManager"/> 的 NpcAdded/NpcRemoved/NpcChanged(12100/12103 落表后触发,
    /// 12020 任务标经 <see cref="SceneManager.SetNpcTaskIcon"/> → NpcChanged),把真实 NPC 加载成 3D 模型摆进
    /// <see cref="SceneCharacterStage"/> 合成台(与主角共用相机/RT),位置 = (npc 世界像素 - 主角世界像素)。
    /// 本类不碰协议/字节,只消费 vo;与 <see cref="MainRoleFlow"/> 同构(static flow + 一个 MonoBehaviour 驱动)。
    ///
    /// 资源来源(真实资源,绝不画假模型):
    ///   模型     = object/npc/model_clothe_{npcId}/model_clothe_{npcId}(转换产物,已入库 GameRes/object/npc/,git tracked)。
    ///   待机动作 = object/npc/action/{npcId}/idle。
    ///   key 形如 object/{module}/{name}/{name},与 RoleModelAssembler.Key 同一约定(转换后模型布局),
    ///   不是 GameResPath.GetObjectPath 的老端 .lh 路径(那条指向被忽略的 resource/object/.../objs/*.lh)。
    ///
    /// 名牌 / 缩放 / 朝向(本轮接入,数据来自 config_npc,经 <see cref="NpcConfigs"/>):
    ///   - 名字/称号:挂屏幕跟随名牌(称号 <title> 金、名字 青,对标 Npc.ts:99-107 NameBoard.SetName/SetNpcName);
    ///     缺名(config_npc.name 空)则不挂(降级,不写假名)。名牌走最小原生 TMP 屏幕跟随件 —— 老端 NameBoard.lh
    ///     的 Unity 转换产物(NameBoardBind)目前只有血条节点、无名字文本节点,且 NPC 经合成台 RT 合成(2D 名牌
    ///     无法直接叠到 RT 里的 3D 体),故按 DialogueView/TaskFinishView 同例做最小原生件,待 NameBoard 补名字节点后替换。
    ///   - 缩放:按 config_npc.icon_scale(对标 Npc.ts:109-110 this.scale = icon_scale)传入合成台 AddSceneCharacter。
    ///   - 朝向:按 config_npc.brith_rot(对标 Npc.ts:178-180 SetRotateY(brith_rot+90))解出模型 yaw,与主角朝向同语义。
    ///
    /// 已知 blocker(数据到了但资源缺,精确写明、不臆造):
    ///   - 头顶任务标 sprite:12020 的 task_icon 值已落 vo.TaskIcon 并经 NpcChanged 到达本层(见日志),
    ///     但任务标图标 sprite 的资源路径/映射待确认 → 暂只记日志,不挂假图标。
    ///   - NPC 立绘/头像(config_npc.image):对话弹层头像待接(走 ResManager),缺资源时写精确 blocker(P3 fallback)。
    /// </summary>
    public static class NpcRenderer
    {
        private const string ACTION_IDLE = "idle";

        private sealed class NpcView
        {
            public int NpcId;
            public NpcVo Vo;
            public Transform Tilt;   // SceneCharacterStage 里的配角 tilt(挂模型)
            public GameObject Model;
            public RectTransform NameplateRt; // 屏幕跟随名牌(名字/称号);无名 NPC 为 null
            public int Epoch;        // 清场代次:切场景/断线后过期的在途加载结果丢弃
            public bool Loaded;
        }

        private static readonly Dictionary<int, NpcView> _views = new Dictionary<int, NpcView>();
        private static GameObject _driverGo;
        private static int _epoch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager mgr = SceneManager.Instance;
            mgr.NpcAdded += OnNpcAdded;
            mgr.NpcRemoved += OnNpcRemoved;
            mgr.NpcChanged += OnNpcChanged;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, ClearAll);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnected);
        }

        private static void OnNetDisconnected()
        {
            // 游戏内自动重连:同主角一样保留 NPC,等重连后 12100 覆盖(避免重连闪一下);
            // 真正断开(非重连)才清场。
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            ClearAll();
        }

        private static async void OnNpcAdded(NpcVo vo)
        {
            if (vo == null) return;
            // 重复推送(同 NpcId 再来)→ 先撤旧再建,保证形象与最新 vo 一致。
            if (_views.TryGetValue(vo.NpcId, out NpcView dup)) RemoveView(dup);

            var view = new NpcView { NpcId = vo.NpcId, Vo = vo, Epoch = _epoch };
            _views[vo.NpcId] = view;
            EnsureDriver();

            // config_npc(名字/称号/缩放/朝向)按需加载:场景 NPC 可能先于对话子系统出现,这里独立兜底(EnsureLoaded 幂等)。
            await NpcConfigs.EnsureLoaded();
            if (IsStale(view)) return;
            NpcConfigs.NpcCfg cfg = NpcConfigs.Get(vo.NpcId);

            // is_show 语义(老端 SAP 可见性裁剪)未完全确认:本层先全渲染并记录 isShow,后续如确认 0=隐藏再加门。
            GameLog.Info("Scene", "npc render: id={0} pos=({1},{2}) isShow={3} name=\"{4}\" title=\"{5}\" iconScale={6} brithRot={7}",
                vo.NpcId, vo.X, vo.Y, vo.IsShow, cfg?.Name, cfg?.Title, cfg?.IconScale ?? 1f, cfg?.BrithRot ?? -1);

            string modelKey = NpcConfigs.GetModelKey(vo.NpcId, cfg, out string modelModule, out string modelResId);
            GameObject prefab = await ResManager.LoadAsync<GameObject>(modelKey);

            // 加载期间被移除/替换/清场:丢弃(尚未实例化,prefab 句柄由 ResManager 管理)。
            if (IsStale(view)) return;

            if (prefab == null)
            {
                // 数据到了但模型资源缺 → 精确 blocker,不画假模型冒充。
                GameLog.Error("Scene",
                    "npc model 缺失(blocker):id={0} key={1} — NPC 数据已到但模型未转换/未入库,形象不可见。" +
                    "处置:用 神霄/资源 转该 NPC 模型,或确认 Assets/GameRes/object/{2} 下资源 {3} 存在。",
                    vo.NpcId, modelKey, modelModule, modelResId);
                return;
            }

            GameObject model = Object.Instantiate(prefab);
            // 缩放:config_npc.icon_scale(对标 Npc.ts:109-110 this.scale = icon_scale);缺/<=0 用合成台默认 MODEL_SCALE。
            float iconScale = cfg != null && cfg.IconScale > 0f ? cfg.IconScale : -1f;
            Transform tilt = iconScale > 0f
                ? SceneCharacterStage.AddSceneCharacter(model, iconScale)
                : SceneCharacterStage.AddSceneCharacter(model);
            // 朝向:config_npc.brith_rot(对标 Npc.ts:178-180 SetRotateY(brith_rot+90));brith_rot==-1 保持待机默认朝向。
            ApplyBrithRot(model, cfg);
            view.Model = model;
            view.Tilt = tilt;
            view.Loaded = true;
            CreateNameplate(view, cfg); // 屏幕跟随名牌(名字/称号);无名则跳过(降级)
            UpdateViewPosition(view);   // 立即摆一次位(含名牌),driver 之后每帧维持

            await PlayIdle(model, modelModule, modelResId);
            if (IsStale(view)) return; // PlayIdle 期间被清场:模型已随 tilt 销毁,放弃后续

            GameLog.Info("Scene", "npc visible: id={0} model={1} res={2} pos=({3},{4})", vo.NpcId, modelKey, modelResId, vo.X, vo.Y);
        }

        private static void OnNpcRemoved(int npcId)
        {
            if (_views.TryGetValue(npcId, out NpcView view)) RemoveView(view);
        }

        private static void OnNpcChanged(NpcVo vo)
        {
            if (vo == null || !_views.TryGetValue(vo.NpcId, out NpcView view)) return;
            view.Vo = vo;
            // 任务标:值已到本层(链路通)。头顶任务标 sprite 待确认资源 → 先记日志,不挂假图标(blocker)。
            GameLog.Info("Scene", "npc task icon: id={0} taskIcon={1}(头顶任务标 sprite 资源待确认 → blocker)",
                vo.NpcId, vo.TaskIcon);
        }

        /// <summary>切场景/真正断线:清掉所有 NPC 视图(SceneManager.Clear 不逐个发 NpcRemoved,故这里整清)。</summary>
        private static void ClearAll()
        {
            _epoch++; // 让在途加载全部过期
            foreach (NpcView v in _views.Values)
            {
                SceneCharacterStage.RemoveSceneCharacter(v.Tilt);
                DestroyNameplate(v);
            }
            _views.Clear();
        }

        private static void RemoveView(NpcView view)
        {
            if (view == null) return;
            SceneCharacterStage.RemoveSceneCharacter(view.Tilt);
            DestroyNameplate(view);
            view.Loaded = false;
            if (_views.TryGetValue(view.NpcId, out NpcView cur) && cur == view)
            {
                _views.Remove(view.NpcId);
            }
        }

        /// <summary>视图是否已失效(被清场/移除/替换);在途 await 之后用它丢弃过期结果。</summary>
        private static bool IsStale(NpcView view)
        {
            return view == null || view.Epoch != _epoch
                || !_views.TryGetValue(view.NpcId, out NpcView cur) || cur != view;
        }

        /// <summary>每帧由 driver 调:NPC 在世界里静止,只需随主角移动重摆相对位(锚在地图上)。</summary>
        internal static void TickPositions()
        {
            if (_views.Count == 0) return;
            foreach (NpcView v in _views.Values)
            {
                UpdateViewPosition(v);
            }
        }

        private static void UpdateViewPosition(NpcView view)
        {
            if (view == null || !view.Loaded || view.Tilt == null) return;
            RoleModel role = RoleModel.Instance;
            Vector2 off = new Vector2(view.Vo.X - role.X, view.Vo.Y - role.Y);
            SceneCharacterStage.SetSceneCharacterPixelOffset(view.Tilt, off);
            UpdateNameplatePosition(view);
        }

        private static async Task PlayIdle(GameObject model, string module, string modelResId)
        {
            if (model == null) return;
            string actionKey = NpcConfigs.GetActionKey(module, modelResId, ACTION_IDLE);
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(actionKey);
            if (model == null) return; // 加载期间被销毁(Unity 重载 == null)
            if (clip == null)
            {
                GameLog.Warn("Scene", "npc idle 动作未转换,静态展示:res={0} key={1}", modelResId, actionKey);
                return;
            }
            Animation anim = model.GetComponent<Animation>();
            if (anim == null) anim = model.AddComponent<Animation>();
            if (anim.GetClip(ACTION_IDLE) == null) anim.AddClip(clip, ACTION_IDLE);
            anim.Play(ACTION_IDLE);
        }

        // ===================== 朝向(config_npc.brith_rot)=====================
        // 老端 Npc.SetRotate:assign_angle(=brith_rot)!=-1 时 SetRotateY(assign_angle + 90)(注释"加90方便策划填配置")。
        // SceneObj.SetRotateY 把角度化为方向向量 dir=(cos θ, sin θ),θ=(brith_rot+90)°(舞台坐标 x 右、y 下,与地图像素一致);
        // 本端再用与 MainRoleAgent.Face 完全相同的解算 yaw=Atan2(dir.x, -dir.y) 落到模型自身 yaw —— 保证 NPC 与主角朝向
        // 同一屏幕语义(主角朝向已实机校对过,NPC 自洽,无需再单独翻符号)。
        private static void ApplyBrithRot(GameObject model, NpcConfigs.NpcCfg cfg)
        {
            if (model == null || cfg == null || cfg.BrithRot == -1) return; // -1=默认朝向(合成台已置待机 yaw 180)
            float theta = (cfg.BrithRot + 90f) * Mathf.Deg2Rad;
            float dx = Mathf.Cos(theta);
            float dy = Mathf.Sin(theta);
            float yaw = Mathf.Atan2(dx, -dy) * Mathf.Rad2Deg;
            Vector3 e = model.transform.localEulerAngles;
            model.transform.localEulerAngles = new Vector3(e.x, yaw, e.z);
        }

        // ===================== 名牌(名字/称号,屏幕跟随)=====================
        // NPC 经合成台 RT 合成(3D 体在隔离区),2D 名牌无法直接叠进 RT,故名牌做成 Scene 层屏幕跟随 TMP:
        // NPC 屏幕位 = (npc 像素 - 相机像素)(与合成台/地图同一锚定口径,推导同 SceneCharacterStage 对主角的
        // SetMainRoleScreenOffset)—— 故名牌随地图/相机/NPC 一起锚在地图上。精确竖直贴合属"待真机微调"。
        private const float NAMEPLATE_HEAD_OFFSET = 150f; // 名牌相对脚底锚点上移(参考像素;头顶高度,实跑可调)

        private static RectTransform _nameplateRoot;
        private static TMP_FontAsset _font;
        private static Material _fontMat;

        private static void CreateNameplate(NpcView view, NpcConfigs.NpcCfg cfg)
        {
            if (view == null || cfg == null || string.IsNullOrEmpty(cfg.Name)) return; // 无名不挂(降级,不写假名)
            EnsureNameplateRoot();
            if (_nameplateRoot == null) return;

            var go = new GameObject($"Nameplate_{view.NpcId}", typeof(RectTransform));
            go.transform.SetParent(_nameplateRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // 锚屏幕中心:anchoredPosition 用(npc - 相机)像素
            rt.pivot = new Vector2(0.5f, 0f);                      // 底边贴在头顶偏移处
            rt.sizeDelta = new Vector2(260f, 64f);

            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = 22;
            t.alignment = TextAlignmentOptions.Bottom;
            t.richText = true;
            t.raycastTarget = false;            // 名牌不吃点击
            t.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyFont(t);
            // 称号 <title> 金(#fcf910)在上、名字 青(#c2fdfa)在下(对标 Npc.ts:99-107 NameBoard.SetName/SetNpcName 配色)。
            string title = string.IsNullOrEmpty(cfg.Title) ? "" : $"<color=#fcf910><{cfg.Title}></color>\n";
            t.text = title + $"<color=#c2fdfa>{cfg.Name}</color>";

            view.NameplateRt = rt;
        }

        private static void UpdateNameplatePosition(NpcView view)
        {
            if (view?.NameplateRt == null) return;
            Vector2 cam = SceneMapView.CameraPos;
            float sx = view.Vo.X - cam.x;
            float sy = -(view.Vo.Y - cam.y);
            view.NameplateRt.anchoredPosition = new Vector2(sx, sy + NAMEPLATE_HEAD_OFFSET);
        }

        private static void DestroyNameplate(NpcView view)
        {
            if (view?.NameplateRt == null) return;
            Object.Destroy(view.NameplateRt.gameObject);
            view.NameplateRt = null;
        }

        private static void EnsureNameplateRoot()
        {
            if (_nameplateRoot != null) return;
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null) return;

            var go = new GameObject("__NpcNameplates", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(sceneLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = -40; // 合成台 RT(-50)之上、HUD(默认 0)之下
            _nameplateRoot = rt;
        }

        // 复用场景里已打开文本的 TMP 字体(含中文字形),避免名牌豆腐块(同 DialogueView/TaskFinishView 的 TEMP 壳约定)。
        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _font = src.font; _fontMat = src.fontSharedMaterial; }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }

        private static void EnsureDriver()
        {
            if (_driverGo != null) return;
            _driverGo = new GameObject("__NpcRendererDriver");
            Object.DontDestroyOnLoad(_driverGo);
            _driverGo.AddComponent<NpcRendererDriver>();
        }
    }

    /// <summary>NPC 渲染层的每帧驱动(对标 MainRoleAgent 之于主角):只负责把静止 NPC 随主角移动重摆相对位。</summary>
    public sealed class NpcRendererDriver : MonoBehaviour
    {
        private void Update() => NpcRenderer.TickPositions();
    }
}
