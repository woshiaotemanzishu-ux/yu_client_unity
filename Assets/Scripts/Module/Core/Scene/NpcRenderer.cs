using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
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
    /// 已知 blocker(数据到了但资源缺,精确写明、不臆造):
    ///   - 名字/称号/icon_scale:老端来自 server 配置 config_npc(name/title/icon/icon_scale),
    ///     该配置尚未导入 Unity(GetServerConfigPath 下无 config_npc.json,ConfigManager 未加载)→ 暂用 NpcId
    ///     占位、缩放取默认、朝向取默认(brith_rot 同缺)。补法:把 config_npc 纳入配表流水线后,这里按
    ///     NpcId 查名/称号挂 NameBoard、按 icon 解析模型 id(多数 NPC icon==id,故当前直接用 NpcId 即真实模型)。
    ///   - 头顶任务标 sprite:12020 的 task_icon 值已落 vo.TaskIcon 并经 NpcChanged 到达本层(见日志),
    ///     但任务标图标 sprite 的资源路径/映射待确认 → 暂只记日志,不挂假图标。
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

            // is_show 语义(老端 SAP 可见性裁剪)未完全确认:本层先全渲染并记录 isShow,后续如确认 0=隐藏再加门。
            GameLog.Info("Scene", "npc render: id={0} pos=({1},{2}) isShow={3} args=\"{4}\"(名字/称号待 config_npc 导入)",
                vo.NpcId, vo.X, vo.Y, vo.IsShow, vo.Args);

            string modelKey = ModelKey(vo.NpcId);
            GameObject prefab = await ResManager.LoadAsync<GameObject>(modelKey);

            // 加载期间被移除/替换/清场:丢弃(尚未实例化,prefab 句柄由 ResManager 管理)。
            if (IsStale(view)) return;

            if (prefab == null)
            {
                // 数据到了但模型资源缺 → 精确 blocker,不画假模型冒充。
                GameLog.Error("Scene",
                    "npc model 缺失(blocker):id={0} key={1} — NPC 数据已到但模型未转换/未入库,形象不可见。" +
                    "处置:用 神霄/资源 转该 NPC 模型,或确认 Assets/GameRes/object/npc/model_clothe_{0} 存在。",
                    vo.NpcId, modelKey);
                return;
            }

            GameObject model = Object.Instantiate(prefab);
            Transform tilt = SceneCharacterStage.AddSceneCharacter(model);
            view.Model = model;
            view.Tilt = tilt;
            view.Loaded = true;
            UpdateViewPosition(view); // 立即摆一次位,driver 之后每帧维持

            await PlayIdle(model, vo.NpcId);
            if (IsStale(view)) return; // PlayIdle 期间被清场:模型已随 tilt 销毁,放弃后续

            GameLog.Info("Scene", "npc visible: id={0} model={1} pos=({2},{3})", vo.NpcId, modelKey, vo.X, vo.Y);
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
            }
            _views.Clear();
        }

        private static void RemoveView(NpcView view)
        {
            if (view == null) return;
            SceneCharacterStage.RemoveSceneCharacter(view.Tilt);
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
        }

        private static async Task PlayIdle(GameObject model, int npcId)
        {
            if (model == null) return;
            string actionKey = $"object/npc/action/{npcId}/{ACTION_IDLE}";
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(actionKey);
            if (model == null) return; // 加载期间被销毁(Unity 重载 == null)
            if (clip == null)
            {
                GameLog.Warn("Scene", "npc idle 动作未转换,静态展示:id={0} key={1}", npcId, actionKey);
                return;
            }
            Animation anim = model.GetComponent<Animation>();
            if (anim == null) anim = model.AddComponent<Animation>();
            if (anim.GetClip(ACTION_IDLE) == null) anim.AddClip(clip, ACTION_IDLE);
            anim.Play(ACTION_IDLE);
        }

        // 模型 key:object/{module}/{name}/{name}(转换后模型布局,同 RoleModelAssembler.Key 约定)。
        // 多数 NPC 的 config_npc.icon == id,故直接用 NpcId 即真实模型;icon!=id 的少数 NPC 待 config_npc 导入后用 icon。
        private static string ModelKey(int npcId)
            => $"object/npc/model_clothe_{npcId}/model_clothe_{npcId}";

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
