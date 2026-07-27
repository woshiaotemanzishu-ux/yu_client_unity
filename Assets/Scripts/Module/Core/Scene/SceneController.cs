using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Preload;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Scene protocol controller. Mirrors old-client SceneController 12005 entry path.
    /// 协议层:解析 12xxx 后落到 <see cref="SceneManager"/>(数据层),渲染层订阅 SceneManager 事件建模。
    /// </summary>
    public sealed class SceneController : BaseController
    {
        public static readonly SceneController Instance = new SceneController();

        private int _loadVersion;
        /// <summary>收到 12002 快照时的 _loadVersion——实体就绪探针据此确认"本场景的快照已到"。</summary>
        private int _snapshotLoadVersion = -1;
        /// <summary>第15轮驱动标志(由 LoginBootstrap 在 smoke 模式下设置)。</summary>
        public static bool EnableRound15ComboTest { get; set; }

        /// <summary>第18轮连续击杀标志(由 LoginBootstrap 在 smoke 模式下设置)。</summary>
        public static bool EnableRound18ContinuousKill { get; set; }

        /// <summary>第21轮任务点击杀标志:12002无怪时发移动请求触发九宫格,等 MonsterAdded 再攻击。</summary>
        public static bool EnableRound21TaskKillTest { get; set; }

        /// <summary>第18轮连续击杀状态:当前正在击杀的目标怪id(0=无)。</summary>
        private int _round18TargetMonster;

        /// <summary>第21轮:是否已订阅 MonsterAdded 等待怪出现(防重复订阅)。</summary>
        private bool _round21MonitoringMonsters;

        private SceneController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.SC_MOVE, On12001);
            RegisterProtocal(Proto.SC_LOAD_SCENE, On12002);
            RegisterProtocal(Proto.SC_ROLE_ADD, On12003);
            RegisterProtocal(Proto.SC_ROLE_REMOVE, On12004);
            RegisterProtocal(Proto.SC_CHANGE_SCENE, On12005);
            RegisterProtocal(Proto.SC_ENTITY_DELETE, On12006);
            RegisterProtocal(Proto.SC_MONSTER_ADD, On12007);
            RegisterProtocal(Proto.SC_SCENE_MOVE, On12008);
            RegisterProtocal(Proto.SC_HP_UPDATE, On12009);
            RegisterProtocal(Proto.SC_HIDE, On12070);
            RegisterProtocal(Proto.SC_GHOST, On12071);
            RegisterProtocal(Proto.SC_GROUP, On12072);
            RegisterProtocal(Proto.SC_PK_STATUS, On12074);
            RegisterProtocal(Proto.SC_SHOW, On12075);
            RegisterProtocal(Proto.SC_SPEED, On12082);
            RegisterProtocal(Proto.SC_RENAME, On12086);
            RegisterProtocal(Proto.SC_VIEW_ROLE_REFRESH, On12011);
            RegisterProtocal(Proto.SC_VIEW_OBJ_REFRESH, On12012);
            RegisterProtocal(Proto.SC_DROP_LIST, On12018);
            RegisterProtocal(Proto.SC_NPC_ICON_REFRESH, On12020);
            RegisterProtocal(Proto.SC_NPC_LIST, On12100);
            RegisterProtocal(Proto.SC_NPC_DYNAMIC, On12103);

            // ---- 自动循环 轮18 PK5:场景散件(120xx 补全,pt_120.erl;详见各 On12xxx 方法注释) ----
            // 死号严禁注册:12089(老端 RegisterProtocal 已注释=真死)/12091(pt_120.erl 全文无
            // write(12091) 子句,服务端只会发 cmd=0 空包,双端事实死,见 r18_server_scene §重大发现)。
            RegisterProtocal(Proto.SC_DUMMY_ENTER, On12015);
            RegisterProtocal(Proto.SC_DROP_SPAWN, On12017);
            RegisterProtocal(Proto.SC_BOSS_OWNER, On12022);
            RegisterProtocal(Proto.SC_MONSTER_TALK, On12023);
            RegisterProtocal(Proto.SC_DROP_PICK_CONFIRM, On12024);
            RegisterProtocal(Proto.SC_BOSS_HURT_LIST, On12025);
            RegisterProtocal(Proto.SC_BOSS_HURT_ADD, On12026);
            RegisterProtocal(Proto.SC_BOSS_HURT_REMOVE, On12027);
            RegisterProtocal(Proto.SC_BOSS_ASSIST_CHANGE, On12028);
            RegisterProtocal(Proto.SC_AREA_MARK, On12030);
            RegisterProtocal(Proto.SC_HP_CHANGE, On12036);
            RegisterProtocal(Proto.SC_ASSIST_LIST, On12043);
            RegisterProtocal(Proto.SC_ASSIST_ADD, On12044);
            RegisterProtocal(Proto.SC_ASSIST_REMOVE, On12045);
            RegisterProtocal(Proto.SC_FIGURE_CHANGE, On12078);
            RegisterProtocal(Proto.SC_MONSTER_ATTR_UPDATE, On12080);
            RegisterProtocal(Proto.SC_REVIVE_COMPLETE, On12083);
            RegisterProtocal(Proto.SC_SAFE_AREA_STATE, On12085);
            RegisterProtocal(Proto.SC_PLAYER_COUNT, On12087);
            RegisterProtocal(Proto.SC_SIMPLE_USER_LIST, On12088);
            RegisterProtocal(Proto.SC_GUILD_ID_CHANGE, On12090);
            RegisterProtocal(Proto.SC_MONSTER_BUFF_BATCH, On12092);
        }

        public void RequestEnterScene(RoleModel role)
        {
            if (role == null || !role.HasBaseInfo)
            {
                GameLog.Warn("Scene", "skip 12005: role base info is not ready");
                return;
            }

            SendFmt(Proto.SC_CHANGE_SCENE, "iicchh", role.DunId, role.SceneId, 0, 0, 0, 0);
            GameLog.Info("Scene", "request 12005: dunId={0} sceneId={1}", role.DunId, role.SceneId);
        }

        /// <summary>
        /// 跨场景切换请求(对标老端 SceneController.ts:1066 onRequestChangeScene → REQUEST_CHANGE_SCENE
        /// → SendFmtToGame(12005,"iicchh", 0, scene_id, 0, send_type, pos_x, pos_y))。飞鞋/任务跨场景走此路:
        /// dun_id=0、send_type=1(飞鞋)、带目标落点 pos。服务端回 12005 由既有 <see cref="On12005"/> 加载新场景
        /// (落点最终由服务端 12005 回包给定);任务流的"到达后续接"由 TaskModel 监听场景就绪事件重跑 DoTask 完成。
        /// </summary>
        public void RequestChangeScene(int sceneId, int x, int y, int sendType = 1)
        {
            if (sceneId <= 0) { GameLog.Warn("Scene", "RequestChangeScene 非法 sceneId={0}", sceneId); return; }
            SendFmt(Proto.SC_CHANGE_SCENE, "iicchh", 0, sceneId, 0, sendType, Floor0(x), Floor0(y));
            GameLog.Info("Scene", "request 12005 跨场景: sceneId={0} pos=({1},{2}) sendType={3}", sceneId, x, y, sendType);
        }

        /// <summary>
        /// 主角移动上报(对标 SceneController.ts:1042 moveRequestHandler → SendFmtToGame(12001,"ihhchhhh"))。
        /// </summary>
        public void SendMoveRequest(int curX, int curY, int moveType, int targetX, int targetY, int startX = 0, int startY = 0)
        {
            int sceneId = RoleModel.Instance.SceneId;
            SendFmt(Proto.SC_MOVE, "ihhchhhh",
                Floor0(sceneId),
                Floor0(curX), Floor0(curY),
                Floor0(moveType),
                Floor0(targetX), Floor0(targetY),
                Floor0(startX), Floor0(startY));
        }

        private static int Floor0(int v) => v < 0 ? 0 : v;

        public override void Dispose()
        {
            // 清理 Round21 九宫格监听(断线/切场景时取消订阅,防旧场景怪触发新场景攻击)
            if (_round21MonitoringMonsters)
            {
                SceneManager.Instance.MonsterAdded -= OnRound21MonsterAdded;
                _round21MonitoringMonsters = false;
            }
            _round18TargetMonster = 0;

            bool keepMap = LoginController.Instance.CanAutoReconnectInGame;
            ++_loadVersion;
            if (keepMap)
            {
                GameLog.Info("Scene", "keep scene map during in-game reconnect");
            }
            else
            {
                SceneManager.Instance.Clear();
                SceneMiscModel.Instance.Clear(); // PK5:场景散件杂项状态随场景对象表一起清(Boss榜/求助列表/区域标记等)
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED);
                SceneMapLoader.Clear();
                SceneMapView.Clear();
            }
            base.Dispose();
        }

        // ===================== 移动 =====================

        /// <summary>
        /// 12001(S2C)移动广播。读 "hhlc"(x, y, role_id, move_flag);move_flag != Normal 时再读 "hh" 起飞坐标。
        /// role_id==主角 → 空处理(本地插值,不纠正,与老客户端一致);其他玩家 → 驱动其 VO 移动(渲染层插值)。
        /// </summary>
        private void On12001(NetReader reader)
        {
            int x = reader.ReadU16();
            int y = reader.ReadU16();
            long roleId = reader.ReadU64();
            int moveFlag = reader.ReadU8();

            if (moveFlag != MoveType.Normal)
            {
                if (reader.Remaining >= 4) { reader.ReadU16(); reader.ReadU16(); } // start_fly_x/y(轻功/瞬移起飞点,本期暂不用)
                else GameLog.Warn("Scene", "12001 flag={0} 缺起飞坐标(remaining={1}B)", moveFlag, reader.Remaining);
            }

            if (roleId == RoleModel.Instance.RoleId) return; // 主角本地推进,不纠正
            SceneManager.Instance.MoveRole(roleId, x, y, moveFlag);
        }

        /// <summary>12008 通用对象位置同步 "hhi"(x, y, instance_id)。</summary>
        private void On12008(NetReader reader)
        {
            int x = reader.ReadU16();
            int y = reader.ReadU16();
            long id = reader.ReadU32();
            SceneManager.Instance.MoveSceneObj(id, x, y);
        }

        /// <summary>12009 血量更新 "lll"(obj_id, hp, hpLim)。</summary>
        private void On12009(NetReader reader)
        {
            long objId = reader.ReadU64();
            long hp = reader.ReadU64();
            long hpLim = reader.ReadU64();
            SceneManager.Instance.ApplyHp(objId, hp, hpLim);
        }

        // —— 场景对象字段广播(pt_120):Sign(c) + Id(l) + 值。先更新对应玩家 VO 字段;
        //    怪物的同类字段广播(HideFlag/GhostMode/WarGroup)待补。渲染层订阅 EVT_SCENE_ROLE_STATE 刷新表现。
        private void On12070(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int v = r.ReadU8(); SetRoleField(id, vo => vo.Hide = v); }      // 隐身
        private void On12071(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int v = r.ReadU8(); SetRoleField(id, vo => vo.Ghost = v); }     // 幽灵
        private void On12072(NetReader r) { r.ReadU8(); long id = r.ReadU64(); long g = r.ReadU64(); SetRoleField(id, vo => vo.Group = g); }   // 分组
        private void On12074(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int v = r.ReadU8(); if (id == RoleModel.Instance.RoleId) { SetMainRolePkStatus(v); return; } SetRoleField(id, vo => vo.PkStatus = v); }  // PK 状态(主角走 RoleModel)
        private void On12075(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int v = r.ReadU8(); SetRoleField(id, vo => vo.Show = v); }      // 展示状态
        private void On12082(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int spd = r.ReadU16(); SetRoleField(id, vo => vo.Speed = spd); } // 移动速度
        // 改名(无 sign 前缀)。轮5 修:自己改名分流对标 On12074 —— 自己走 RoleModel(EquipmentView/RoleFlow
        // 等主角自身 UI 依赖它)+ EVT_ROLE_INFO_UPDATE;42601 提交成功那侧刻意不直接改 Figure.Name,
        // 就是靠这条既有 12086 广播路径统一落地(见 RoleController.On42601 注释,勿双改)。
        private void On12086(NetReader r)
        {
            long id = r.ReadU64();
            string name = r.ReadString();
            if (id == RoleModel.Instance.RoleId)
            {
                if (RoleModel.Instance.Figure != null) RoleModel.Instance.Figure.name = name;
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
                return;
            }
            SetRoleField(id, vo => { if (vo.Figure != null) vo.Figure.name = name; });
        }

        private static void SetRoleField(long roleId, System.Action<RoleVo> set)
        {
            RoleVo vo = SceneManager.Instance.GetRole(roleId);
            if (vo == null) return;
            set(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_ROLE_STATE);
        }

        /// <summary>PK5 新增:怪物字段广播的镜像版(对标 SetRoleField,复用 MonsterVo 既有字段,
        /// 不自带 Emit——调用方按各自协议号发 EVT_SCENE_MISC_UPDATE,见 On12080/On12090)。</summary>
        private static void SetMonsterField(int monsterId, System.Action<MonsterVo> set)
        {
            MonsterVo vo = SceneManager.Instance.GetMonster(monsterId);
            if (vo == null) return;
            set(vo);
        }

        // ===================== 进入/切换场景 =====================

        private void On12005(NetReader reader)
        {
            object[] data = reader.ReadFmt("ihhiicc");
            int instanceId = ToInt(data[0]);
            int x = ToInt(data[1]);
            int y = ToInt(data[2]);
            int errorCode = ToInt(data[3]);
            int dunId = ToInt(data[4]);
            int callbackType = ToInt(data[5]);
            int sendType = ToInt(data[6]);

            if (instanceId == 0)
            {
                GameLog.Warn("Scene", "12005 failed: errorCode={0} callback={1} sendType={2}", errorCode, callbackType, sendType);
                return;
            }

            RoleModel role = RoleModel.Instance;
            int prevSceneId = role.SceneId;
            int prevDunId = role.DunId;
            role.SceneId = instanceId;
            role.X = x;
            role.Y = y;
            role.DunId = dunId;

            // 12005 已切到服务端权威落点，必须同步 MainRoleAgent 自己维护的浮点坐标并废弃上一场景的
            // 自动接近回调。否则地图就绪前的短窗口仍会从旧坐标继续追上一只怪，视觉上表现为进副本后先折返。
            MainRoleAgent.Current?.ApplyAuthoritativeScenePosition(x, y);

            // 真正换场景(场景实例或副本状态变化)才压黑幕过渡;同场景位置校正类 12005 不闪屏。
            // 没有过渡时"角色瞬移+全场实体重刷"会被玩家误读成断线重连(第24轮 test.log 实证)。
            // 黑幕不在这里拉:同图切换(打大妖/进出同图副本)老端是无感的——是否真换图要等地图数据
            // 解析出 mapResId 才知道,决策移到 LoadSceneMapAsync(同图=不拉幕,角色/瓦片全复用)。
            // 退副本回野外给个明确提示,否则任务自动流(通关→61002→12005)的切换毫无预兆。
            if (prevDunId != 0 && dunId == 0) Shenxiao.Common.Tips.TipsManager.Toast("副本结束,返回野外");

            // 进入新场景:清空上一场景的对象表(对标老客户端 ClearAllVo),再由 12100/12002 重新填充。
            SceneManager.Instance.Clear();
            SceneMiscModel.Instance.Clear(); // PK5:同上,新场景的 Boss榜/求助列表/区域标记等杂项状态一并重置
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);

            GameLog.Info("Scene", "12005 ok: sceneId={0} dunId={1} pos=({2},{3})", instanceId, dunId, x, y);
            _ = LoadSceneMapAsync(instanceId);
        }

        private async Task LoadSceneMapAsync(int sceneId)
        {
            int version = ++_loadVersion;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                // Addressables.LoadResourceLocationsAsync blocks in editor non-Play mode; bypass map render.
                SendFmt(Proto.SC_NPC_LIST, "i", sceneId);
                GameLog.Info("Scene", "request 12100: editor harness bypass sceneId={0}", sceneId);
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MAP_READY);
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_FIRST_SCREEN_READY);
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_ENTITIES_READY);
                return;
            }
#endif
            RoleModel role = RoleModel.Instance;
            await LegacyPreloadService.PreloadSceneMapAsync(sceneId, role.X, role.Y);
            SceneMapData data = await SceneMapLoader.LoadAsync(sceneId);
            if (version != _loadVersion) return;   // 更新的加载已接管(含黑幕的隐藏权)
            if (data == null)
            {
                SceneTransitionMask.Hide();   // 地图加载失败别黑屏卡死(另有 8s 自动兜底)
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_FIRST_SCREEN_READY); // 加载页同样别卡死
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_ENTITIES_READY);
                return;
            }

            // 真换图才拉黑幕(盖住底图/瓦片整屏重铺);同图(副本进出)对齐老端无感,不黑一下。
            if (!SceneMapView.IsSameMapShown(data)) SceneTransitionMask.Show();
            await SceneMapView.ShowAsync(data, role.X, role.Y);
            if (version != _loadVersion) return;

            SendFmt(Proto.SC_NPC_LIST, "i", sceneId);
            GameLog.Info("Scene", "request 12100: local map loaded sceneId={0}", sceneId);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MAP_READY);
            // 首屏瓦片真正画完再揭幕:此前在瓦片"入队"后就揭,12005→出怪全程裸奔(进世界盯黑地图数秒)。
            // 12100 已在上面发出,这段等待不拖慢出怪管线;5s 兜底防慢网压幕(黑幕自身另有 8s 兜底)。
            await SceneMapView.WaitTilesIdleAsync(5000);
            if (version != _loadVersion) return;
            SceneTransitionMask.Hide();
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_FIRST_SCREEN_READY);
            _ = EmitEntitiesReadyAsync(version);
        }

        /// <summary>
        /// 首屏实体就绪探针:主角就绪 + 12002 快照已到 + 首批怪/NPC 全部立起(条件稳定 0.25s 防
        /// 快照解析中途的假空窗)→ EVT_SCENE_ENTITIES_READY;8s 兜底防慢网把加载页锁死。
        /// 首次进世界的加载页(LoginFlow)等这个信号才揭幕,免得玩家盯着实体逐个蹦出来。
        /// </summary>
        private async Task EmitEntitiesReadyAsync(int version)
        {
            double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 8.0;
            double stableSince = -1.0;
            while (UnityEngine.Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (version != _loadVersion) return; // 新加载接管(含事件发放权)
                double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
                bool ready = _snapshotLoadVersion == version
                             && MainRoleAgent.Current != null
                             && MonsterRenderer.PendingSpawns == 0
                             && NpcRenderer.PendingSpawns == 0;
                if (ready)
                {
                    if (stableSince < 0) stableSince = now;
                    if (now - stableSince >= 0.25) break;
                }
                else
                {
                    stableSince = -1.0;
                }
                await Task.Yield();
            }
            if (version != _loadVersion) return;
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_ENTITIES_READY);
        }

        // ===================== 12002 场景快照 =====================

        /// <summary>
        /// 12002 场景快照:玩家(h+12003×N)→怪物(h+12007×N)→伙伴(h+12013×N)→其他(h+12014×N)→
        /// 假人(h+12015×N)→区域标记(h+cc×N)。对标 yu_server pt_120 write(12002)/老客户端 On12002。
        /// 伙伴/其他/假人本期只跳读保持字节对齐(不建 VO);玩家/怪物落 SceneManager。
        /// </summary>
        private void On12002(NetReader reader)
        {
            try
            {
                int players = reader.ReadU16();
                for (int i = 0; i < players; i++) ParseRole(reader);

                int monsters = reader.ReadU16();
                for (int i = 0; i < monsters; i++) ParseMonster(reader);

                int partners = reader.ReadU16();
                for (int i = 0; i < partners; i++) SkipPartner(reader);

                int others = reader.ReadU16();
                for (int i = 0; i < others; i++) SkipOther(reader);

                int fakes = reader.ReadU16();
                for (int i = 0; i < fakes; i++) SkipDummy(reader);

                SkipAreaMark(reader); // 12030 内联:动态区域标记

                GameLog.Info("Scene", "12002 快照: 玩家={0} 怪物/采集={1} 伙伴={2} 其他={3} 假人={4} remaining={5}B",
                    players, monsters, partners, others, fakes, reader.Remaining);
                _snapshotLoadVersion = _loadVersion;
                EventDispatcher.Emit(GlobalEvent.EVT_SCENE_SNAPSHOT_READY);

                // 第15轮 Combo副技能取证驱动:smoke 自动进游戏后,在场景快照接收到怪物时自动驱动普攻并捕获副技能 damage>0
                TryAutoStartRound15ComboTest();
            }
            catch (Exception e)
            {
                GameLog.Error("Scene", "12002 解析错位(字段顺序与服务端不一致?): {0}", e.Message);
            }

            SendFmt(Proto.SC_DROP_LIST);
            SendFmt(Proto.SC_NPC_ICON_REFRESH);
            GameLog.Info("Scene", "request 12018/12020: drop list + npc icon");
        }

        private void TryAutoStartRound15ComboTest()
        {
            if (!EnableRound15ComboTest) return; // 驱动未启用

            if (SceneManager.Instance.MonsterCount == 0)
            {
                // 第21轮:快照无怪时，发 12001 移动请求让服务端更新九宫格，等 MonsterAdded 到来再攻击。
                if (!EnableRound21TaskKillTest || _round21MonitoringMonsters) return;
                _round21MonitoringMonsters = true;
                SceneManager.Instance.MonsterAdded += OnRound21MonsterAdded;
                // 发 12001 C2S 朝任务点(5463,2678)移动:服务端九宫格更新后会推送 12012/12007 下发 10001001 怪。
                RoleModel role = RoleModel.Instance;
                GameLog.Info("Scene", "★ [Round21] 12002快照无怪,向任务点(5463,2678)发12001移动请求(curPos=({0},{1})),等待九宫格推送 10001001 怪",
                    role.X, role.Y);
                SendMoveRequest(role.X, role.Y, 0, 5463, 2678);
                return;
            }

            GameLog.Info("Scene", "★ [Round15] 检测到怪物({0}只),延迟 1000ms 后驱动普攻", SceneManager.Instance.MonsterCount);
            _round18TargetMonster = 0;
            _ = TriggerRound15AttackAsync();
        }

        /// <summary>
        /// 第21轮:九宫格推送怪到达时触发攻击(对标 DoGotoSceneTask OnArriveTaskPoint → 技能释放)。
        /// 编辑器非 Play 态无 MonoBehaviour.Update,主角不能真实移动;将主角位置暂设为怪物坐标,
        /// 使距离判定在攻击范围内,允许 SceneCombat.MainRoleAttackTarget 直接释放而不触发 MoveToNpc。
        /// </summary>
        private void OnRound21MonsterAdded(MonsterVo vo)
        {
            SceneManager.Instance.MonsterAdded -= OnRound21MonsterAdded;
            _round21MonitoringMonsters = false;
            if (!EnableRound15ComboTest) return;

            // 将主角逻辑位置对齐到怪物坐标(编辑器 harness 无真实移动驱动;仅影响距离判定)
            RoleModel.Instance.X = vo.X;
            RoleModel.Instance.Y = vo.Y;
            GameLog.Info("Scene", "★ [Round21] 九宫格下发怪: type={0} ins={1} pos=({2},{3}) hp={4}/{5},主角位置对齐,延迟1000ms后驱动普攻",
                vo.TypeId, vo.InstanceId, vo.X, vo.Y, vo.Hp, vo.HpLim);
            _round18TargetMonster = 0;
            _ = TriggerRound15AttackAsync();
        }

        /// <summary>
        /// 第15轮 / 第18轮循环击杀驱动。engage+combo 后如 Round18 启用且目标 hp>0,自动继续下一轮。
        /// </summary>
        private async Task TriggerRound15AttackAsync()
        {
            try
            {
                await TimeUtil.Delay(1000); // 让怪物渲染就位
                if (SceneManager.Instance.MonsterCount == 0) return;

                const int attackSkill = 59100001; // 普攻御剑一式
                GameLog.Info("Scene", "★ [Round15] 驱动普攻 skill={0};combo 副技能会在 200ms 后自动补发", attackSkill);
                SceneCombat.Instance.MainRoleAttackTarget(attackSkill, 1);

                GameLog.Info("Scene", "★ [Round15] 普攻已发送,观察日志:");
                GameLog.Info("Scene", "   - SendMainSkillAttack(59100001) = engage 20001");
                GameLog.Info("Scene", "   - [200ms 后] SendComboAfterDelayAsync → SendMainSkillAttack(59100002) = combo 副技能 20001(damage>0 预期)");
            }
            catch (Exception e)
            {
                GameLog.Warn("Scene", "[Round15] 驱动异常: {0}", e.Message);
            }
        }

        /// <summary>
        /// 第18轮:20001 S2C 回包后由 FightController 调用,检查是否继续击杀。
        /// combo 已补发且目标 hp>0 时,延迟后自动继续普攻(循环直到 hp==0)。
        /// </summary>
        public void OnRound18FightResult(int lastSkillId, int targetMonsterId, bool targetAlive)
        {
            if (!EnableRound15ComboTest || !EnableRound18ContinuousKill) return;

            // 只处理 combo(59100002) 回包
            bool isCombo = (lastSkillId == 59100002);
            if (!isCombo) return;

            if (!targetAlive)
            {
                // 目标已死:清目标状态;若场景还有可攻击怪则继续击杀下一只(对标 Round21 击杀 3 只 10001001)。
                _round18TargetMonster = 0;
                if (SceneManager.Instance.MonsterCount > 0)
                {
                    GameLog.Info("Scene", "★ [Round18] 目标 {0} 已死,场景还有 {1} 只怪,延迟后继续击杀",
                        targetMonsterId, SceneManager.Instance.MonsterCount);
                    _ = TriggerRound15AttackAsync();
                }
                return;
            }

            _round18TargetMonster = targetMonsterId;
            GameLog.Info("Scene", "★ [Round18] combo 已补发,目标 {0} hp>0,延迟 500ms 后继续击杀", targetMonsterId);
            _ = ContinueKillAfterDelayAsync();
        }

        private async Task ContinueKillAfterDelayAsync()
        {
            try
            {
                await TimeUtil.Delay(500);
                if (_round18TargetMonster == 0) return;

                MonsterVo target = SceneManager.Instance.GetMonster(_round18TargetMonster);
                if (target == null || target.Hp <= 0)
                {
                    GameLog.Info("Scene", "★ [Round18] 目标 {0} 已不在/已死,停止击杀", _round18TargetMonster);
                    _round18TargetMonster = 0;
                    return;
                }

                GameLog.Info("Scene", "★ [Round18] 继续击杀 skill=59100001 目标={0} hp={1}", _round18TargetMonster, target.Hp);
                SceneCombat.Instance.MainRoleAttackTarget(59100001, 1);
            }
            catch (Exception e)
            {
                GameLog.Warn("Scene", "[Round18] 继续击杀异常: {0}", e.Message);
            }
        }

        private void On12003(NetReader reader) => ParseRole(reader);

        private void On12004(NetReader reader)
        {
            long roleId = reader.ReadU64();
            SceneManager.Instance.RemoveRole(roleId);
        }

        private void On12006(NetReader reader)
        {
            long instanceId = reader.ReadU32();
            SceneManager.Instance.DeleteSceneObj(instanceId);
        }

        private void On12007(NetReader reader) => ParseMonster(reader);

        /// <summary>12011 九宫格玩家增删:h+12003×N(加) + h+l×N(删)。</summary>
        private void On12011(NetReader reader)
        {
            int add = reader.ReadU16();
            for (int i = 0; i < add; i++) ParseRole(reader);
            int remove = reader.ReadU16();
            for (int i = 0; i < remove; i++) SceneManager.Instance.RemoveRole(reader.ReadU64());
        }

        /// <summary>12012 九宫格对象增删:怪物/伙伴/其他/假人(各 h+块×N) + i×N(删)。</summary>
        private void On12012(NetReader reader)
        {
            int monsters = reader.ReadU16();
            for (int i = 0; i < monsters; i++) ParseMonster(reader);
            int partners = reader.ReadU16();
            for (int i = 0; i < partners; i++) SkipPartner(reader);
            int others = reader.ReadU16();
            for (int i = 0; i < others; i++) SkipOther(reader);
            int fakes = reader.ReadU16();
            for (int i = 0; i < fakes; i++) SkipDummy(reader);
            int remove = reader.ReadU16();
            for (int i = 0; i < remove; i++) SceneManager.Instance.DeleteSceneObj(reader.ReadU32());
        }

        // ===================== NPC / 掉落 =====================

        private void On12100(NetReader reader)
        {
            int sceneId = (int)reader.ReadU32();
            int npcCount = reader.ReadU16();
            for (int i = 0; i < npcCount; i++) ParseNpc(reader);
            GameLog.Info("Scene", "12100 npc list: sceneId={0} count={1} remaining={2}B", sceneId, npcCount, reader.Remaining);

            if (sceneId != RoleModel.Instance.SceneId)
            {
                GameLog.Warn("Scene", "skip 12002: npc scene mismatch current={0} reply={1}", RoleModel.Instance.SceneId, sceneId);
                return;
            }

            SendFmt(Proto.SC_LOAD_SCENE);
            GameLog.Info("Scene", "request 12002: npc list loaded sceneId={0}", sceneId);
        }

        private void On12103(NetReader reader)
        {
            int npcCount = reader.ReadU16();
            for (int i = 0; i < npcCount; i++) ParseNpc(reader);
            GameLog.Info("Scene", "12103 dynamic npc: count={0} remaining={1}B", npcCount, reader.Remaining);
        }

        private void On12018(NetReader reader)
        {
            if (reader.Remaining < 2)
            {
                GameLog.Warn("Scene", "12018 drop list payload too short: {0}B", reader.Remaining);
                return;
            }

            int dropCount = reader.ReadU16();
            for (int i = 0; i < dropCount; i++)
            {
                var vo = new DropVo();
                vo.ReadFromProtocal(reader);
                SceneManager.Instance.AddDrop(vo);
            }
            GameLog.Info("Scene", "12018 drop list: count={0} remaining={1}B", dropCount, reader.Remaining);
        }

        private void On12020(NetReader reader)
        {
            if (reader.Remaining < 2)
            {
                GameLog.Warn("Scene", "12020 npc icon payload too short: {0}B", reader.Remaining);
                return;
            }

            int count = reader.ReadU16();
            for (int i = 0; i < count && reader.Remaining >= 5; i++)
            {
                int npcId = (int)reader.ReadU32();
                int icon = reader.ReadU8();
                SceneManager.Instance.SetNpcTaskIcon(npcId, icon);
            }
            GameLog.Info("Scene", "12020 npc icon refresh: count={0} remaining={1}B", count, reader.Remaining);
        }

        // ===================== 解析/跳读 辅助 =====================

        private static void ParseRole(NetReader reader)
        {
            var vo = new RoleVo();
            vo.ReadFromProtocal(reader);
            // 主角自块:老端 SceneManager.CreateRole 里 roleId==自己 → mainrole_vo.ChangeFromVo(同步 pk_status 等)。
            // 本端主角状态在 RoleModel,此处只取 PK 模式(HudTop 战斗模式图标/切换弹窗高亮依赖它)。
            if (vo.RoleId == RoleModel.Instance.RoleId)
            {
                SetMainRolePkStatus(vo.PkStatus);
            }
            SceneManager.Instance.AddRole(vo);
        }

        /// <summary>主角 PK 模式落 RoleModel 并广播(仅在值变化时);对标老端 ChangeVar("pk_status")。</summary>
        private static void SetMainRolePkStatus(int pkStatus)
        {
            if (RoleModel.Instance.PkStatus == pkStatus) return;
            RoleModel.Instance.PkStatus = pkStatus;
            EventDispatcher.Emit(GlobalEvent.EVT_PK_STATUS_CHANGED);
        }

        private static void ParseMonster(NetReader reader)
        {
            var vo = new MonsterVo();
            vo.ReadFromProtocal(reader);
            SceneManager.Instance.AddMonster(vo);
        }

        private static void ParseNpc(NetReader reader)
        {
            var vo = new NpcVo();
            vo.ReadFromProtocal(reader);
            SceneManager.Instance.AddNpc(vo);
        }

        /// <summary>伙伴 12013(对标 pt_120 binary_12013):跳读以保持字节对齐,本期不建 VO。</summary>
        private static void SkipPartner(NetReader r)
        {
            r.ReadU16();   // x
            r.ReadU16();   // y
            r.ReadU32();   // id
            r.ReadU64();   // hp
            r.ReadU64();   // hpLim
            r.ReadString();// name
            r.ReadU16();   // lv
            r.ReadU16();   // speed
            r.ReadU8();    // career
            r.ReadU8();    // hide
            r.ReadU64();   // group
            int lvModel = r.ReadU16();
            for (int i = 0; i < lvModel; i++) { r.ReadU8(); r.ReadU32(); } // part, modelId
            r.ReadU32();   // iconTexture
            r.ReadU64();   // ownerId
        }

        /// <summary>一般场景物 12014(对标 pt_120 binary_12014):跳读保持对齐。</summary>
        private static void SkipOther(NetReader r)
        {
            r.ReadU16();   // x
            r.ReadU16();   // y
            r.ReadU32();   // id
            r.ReadString();// name
            r.ReadU32();   // body
            r.ReadString();// iconEffect
            r.ReadU16();   // speed
            r.ReadU8();    // isBeClicked
            r.ReadU64();   // teamId
            r.ReadU64();   // playerId
        }

        /// <summary>假人 12015(对标 pt_120 binary_12015):含 figure 块,跳读保持对齐。</summary>
        private static void SkipDummy(NetReader r)
        {
            r.ReadU32();        // id
            r.ReadU16();        // 占位 0
            r.ReadU16();        // serverId
            r.ReadU16();        // serverNum
            FigureProto.Read(r);// figure 块
            r.ReadU16();        // x
            r.ReadU16();        // y
            r.ReadU64();        // hp
            r.ReadU64();        // hpLim
            r.ReadU16();        // speed
            r.ReadU8();         // hide
            r.ReadU8();         // ghost
            r.ReadU64();        // group
        }

        /// <summary>动态区域标记(pt_120 pack_area_mark):h(count) + (areaId:c, clientType:c)×count。</summary>
        private static void SkipAreaMark(NetReader r)
        {
            if (r.Remaining < 2) return;
            int n = r.ReadU16();
            for (int i = 0; i < n && r.Remaining >= 2; i++) { r.ReadU8(); r.ReadU8(); }
        }

        private static int ToInt(object value) => Convert.ToInt32(value);

        // ===================== PK5 场景散件(120xx 补全,自动循环 轮18) =====================
        // wire 权威=pt_120.erl(无 ClientProtocol schema)。字段顺序均已逐字段核对 pt_120.erl
        // write(12xxx) 原文 + 老客户端 yu_client/h5/src/scene/SceneController.ts 对应 On12xxx。
        // 数据落点原则:能复用既有 SceneManager 容器/RoleVo/MonsterVo/DropVo 字段的一律复用
        // (不新增 Vo 专属字段,保持"只许写 SceneController.cs + Scene\ 下新增文件"边界);
        // 没地方放的落 SceneMiscModel(新文件)。落地后统一发 EVT_SCENE_MISC_UPDATE(参数 protoId)。

        /// <summary>12015 假人进场(单条推送)。pt_120.erl:185-186,553-564(binary_12015):
        /// Id:32, 0:16(保留位), SerId:16, SerNum:16, Figure(write_figure), X:16, Y:16, Hp:64,
        /// HpLim:64, Speed:16, Hide:8, Ghost:8, Group:64。老端 On12015(SceneController.ts:697-704)
        /// 把假人整条塞进 scene_mgr 的角色表(scene_mgr.AddRoleVo),不建独立假人表;本端镜像
        /// 同一处理:直接构一个 RoleVo(其余 RoleVo 专属字段——Platform/ServerName/TeamId 等——
        /// 假人不携带,保持默认值)灌进 SceneManager,复用既有 AddRole 容器/事件。m8存档:老端同样有
        /// `if(!scene_mgr.can_receive_scene_protocal) return;` 门控(ts:701-702,晚于 ReadFakeProtacal
        /// 之后才判断,不影响游标),本端未镜像该门控,留档不补。m8存档②:老端 RoleVo.ReadFakeProtacal
        /// 首段用 ReadFmt("ishh")按 [role_id, plat_name(字符串!), server_unique_id, server_id] 四元解构
        /// (RoleVo.ts:172-186),与本端/wire 权威 Id:32+0:16(保留位)+SerId:16+SerNum:16 四字段实际不对应
        /// ——老端把"保留位"当字符串长度前缀读,恰好保留位恒为0时(0长度字符串)才不多吃字节、后两个
        /// h 才能巧合对上真正的 SerId/SerNum;一旦该保留位不为0(plat_name 非空场景),老端这里就会
        /// 错位读飞。本端严格按 wire 权威四字段顺序读,不复刻这个巧合对齐的隐患。</summary>
        private void On12015(NetReader reader)
        {
            long id = reader.ReadU32();
            reader.ReadU16(); // 保留位(恒 0)
            var vo = new RoleVo
            {
                RoleId = id,
                ServerId = reader.ReadU16(),
                ServerNum = reader.ReadU16(),
            };
            vo.Figure = FigureProto.Read(reader);
            vo.X = reader.ReadU16();
            vo.Y = reader.ReadU16();
            vo.Hp = reader.ReadU64();
            vo.HpLim = reader.ReadU64();
            vo.Speed = reader.ReadU16();
            vo.Hide = reader.ReadU8();
            vo.Ghost = reader.ReadU8();
            vo.Group = reader.ReadU64();
            SceneManager.Instance.AddRole(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_DUMMY_ENTER);
            GameLog.Info("Scene", "12015 假人进场: id={0} pos=({1},{2})", id, vo.X, vo.Y);
        }

        /// <summary>12017 掉落包生成(触发老端 DEAL_WITH_SCENE_DROP_LIST_VO)。pt_120.erl:189-193 +
        /// lib_goods_drop.erl:1703-1709(元素17字段):MonId:32, Time:16, Scene:32, ListNum:16,
        /// 元素×N{DropId:64,DTType:8,GoodsTypeId:32,GoodsNum:32,RoleId:64,ServerId:16,TeamId:64,
        /// Camp:16,GuildId:64,DX:16,DY:16,DropEff:s,DropIcon:s,PickTime:32,ExpireTime:32,DropWay:8,
        /// Alloc:8}, X:16, Y:16, Boss:8。
        /// 与既有 12018/<see cref="DropVo"/> 打通:前 13 字段(DropId..DropIcon)+PickTime 与既有
        /// DropVo 对应字段逐个同义(见 DropVo.ReadFromProtocal);外层 MonId/X/Y 写回
        /// DropVo.MonId/MonPosX/MonPosY(同一怪物掉落批次共用一份来源坐标,与 DropVo 该三字段的
        /// 既定语义一致)。DropVo 目前没有 ExpireTime/DropWay/Alloc 三个字段的位置(12018/老端
        /// SceneController.ts:361-446 字段表里也没有),按本轮"只许写 SceneController.cs+新文件"
        /// 边界不改 DropVo.cs——这 3 个字段仍按位宽读出保游标对齐,只是不落地(已知缺口,留待
        /// 拾取限时/分配方式表现需要时再评估给 DropVo 加字段)。</summary>
        private void On12017(NetReader reader)
        {
            int monId = (int)reader.ReadU32();
            int time = reader.ReadU16();
            int scene = (int)reader.ReadU32();
            List<DropVo> drops = reader.ReadArray(r =>
            {
                var vo = new DropVo
                {
                    DropId = r.ReadU64(),
                    DropType = r.ReadU8(),
                    TypeId = (int)r.ReadU32(),
                    DropNum = (int)r.ReadU32(),
                    RoleId = r.ReadU64(),
                    ServerId = r.ReadU16(),
                    TeamId = r.ReadU64(),
                    Camp = r.ReadU16(),
                    GuildId = r.ReadU64(),
                    X = r.ReadU16(),
                    Y = r.ReadU16(),
                    DropEffect = r.ReadString(),
                    PutIcon = r.ReadString(),
                    PickUpTimeMs = (int)r.ReadU32(),
                };
                r.ReadU32(); // ExpireTime:32 —— DropVo 无对应字段,读出保游标,不落地(见方法头注释)
                r.ReadU8();  // DropWay:8    —— 同上
                r.ReadU8();  // Alloc:8      —— 同上
                return vo;
            });
            int x = reader.ReadU16();
            int y = reader.ReadU16();
            int boss = reader.ReadU8();

            foreach (DropVo vo in drops)
            {
                vo.MonId = monId;
                vo.MonPosX = x;
                vo.MonPosY = y;
                SceneManager.Instance.AddDrop(vo);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_DROP_SPAWN);
            GameLog.Info("Scene", "12017 掉落生成: monId={0} time={1} scene={2} count={3} pos=({4},{5}) boss={6}",
                monId, time, scene, drops.Count, x, y, boss);
        }

        /// <summary>12022 Boss 归属变更(按伤害最高)。pt_120.erl:222-223:PlayerId:64, BossFlag:8。
        /// 复用既有 RoleVo.BossOwner 字段(与 12003 binary_to_12003 的 bl_who 同一字段)。</summary>
        private void On12022(NetReader reader)
        {
            long playerId = reader.ReadU64();
            int bossFlag = reader.ReadU8();
            SetRoleField(playerId, vo => vo.BossOwner = bossFlag);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_BOSS_OWNER);
            GameLog.Info("Scene", "12022 Boss归属: playerId={0} flag={1}", playerId, bossFlag);
        }

        /// <summary>12023 怪物说话(气泡)。pt_120.erl:226-228:AutoId:32, Msg:string。</summary>
        private void On12023(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            string msg = reader.ReadString();
            SceneMiscModel.Instance.LastMonsterTalk = new SceneMiscModel.MonsterTalkInfo { AutoId = autoId, Msg = msg };
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_MONSTER_TALK);
            GameLog.Info("Scene", "12023 怪物喊话: autoId={0} msg={1}", autoId, msg);
        }

        /// <summary>12024 开始拾取掉落确认——注册但**自空处理**(镜像老端 SceneController.ts:494-498
        /// On12024 三行读完即弃,零消费)。pt_120.erl:231-232:DropId:64, RoleId:64, DropEndTime:64。
        /// 只读完保游标对齐,不落 Model、不发事件。</summary>
        private void On12024(NetReader reader)
        {
            long dropId = reader.ReadU64();
            long roleId = reader.ReadU64();
            long dropEndTime = reader.ReadU64();
            GameLog.Info("Scene", "12024 拾取确认(自空,镜像老端): dropId={0} roleId={1} dropEndTime={2}", dropId, roleId, dropEndTime);
        }

        /// <summary>12025 Boss 伤害榜全量(C2S 查询回执)。pt_120.erl:46-47(read"i" AutoId),235-242
        /// (write):AutoId:32, ConfigId:32, List[u16×{RoleId:64,Name:s,ServerId:16,ServerNum:16,
        /// ServerName:s,TeamId:64,TeamPos:8,Hurt:64,AssistId:64}]。</summary>
        private void On12025(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            List<SceneMiscModel.BossHurtEntry> list = reader.ReadArray(r => new SceneMiscModel.BossHurtEntry
            {
                RoleId = r.ReadU64(),
                Name = r.ReadString(),
                ServerId = r.ReadU16(),
                ServerNum = r.ReadU16(),
                ServerName = r.ReadString(),
                TeamId = r.ReadU64(),
                TeamPos = r.ReadU8(),
                Hurt = r.ReadU64(),
                AssistId = r.ReadU64(),
            });
            SceneMiscModel.Instance.BossHurtAutoId = autoId;
            SceneMiscModel.Instance.BossHurtConfigId = configId;
            SceneMiscModel.Instance.BossHurtList.Clear();
            SceneMiscModel.Instance.BossHurtList.AddRange(list);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_BOSS_HURT_LIST);
            GameLog.Info("Scene", "12025 Boss伤害榜全量: autoId={0} configId={1} count={2}", autoId, configId, list.Count);
        }

        /// <summary>C2S 查询 Boss 伤害榜。pt_120.erl:46-47 read(12025,"i")。</summary>
        public void RequestBossHurtList(int autoId) => SendFmt(Proto.SC_BOSS_HURT_LIST, "i", autoId);

        /// <summary>12026 增加怪物伤害(单条,S2C only)。pt_120.erl:245-248:字段同 12025 元素,
        /// 前缀 AutoId:32,ConfigId:32。按 RoleId 在 BossHurtList 里 upsert。</summary>
        private void On12026(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            var entry = new SceneMiscModel.BossHurtEntry
            {
                RoleId = reader.ReadU64(),
                Name = reader.ReadString(),
                ServerId = reader.ReadU16(),
                ServerNum = reader.ReadU16(),
                ServerName = reader.ReadString(),
                TeamId = reader.ReadU64(),
                TeamPos = reader.ReadU8(),
                Hurt = reader.ReadU64(),
                AssistId = reader.ReadU64(),
            };
            SceneMiscModel.Instance.BossHurtAutoId = autoId;
            SceneMiscModel.Instance.BossHurtConfigId = configId;
            List<SceneMiscModel.BossHurtEntry> list = SceneMiscModel.Instance.BossHurtList;
            int idx = list.FindIndex(e => e.RoleId == entry.RoleId);
            if (idx >= 0) list[idx] = entry; else list.Add(entry);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_BOSS_HURT_ADD);
            GameLog.Info("Scene", "12026 Boss伤害增量: roleId={0} hurt={1}", entry.RoleId, entry.Hurt);
        }

        /// <summary>12027 去掉怪物伤害(S2C only)。pt_120.erl:251-253:AutoId:32, ConfigId:32,
        /// RoleIdList[u16×{RoleId:64}]。</summary>
        private void On12027(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            List<long> removeIds = reader.ReadArray(r => (long)r.ReadU64());
            SceneMiscModel.Instance.BossHurtAutoId = autoId;
            SceneMiscModel.Instance.BossHurtConfigId = configId;
            SceneMiscModel.Instance.BossHurtList.RemoveAll(e => removeIds.Contains(e.RoleId));
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_BOSS_HURT_REMOVE);
            GameLog.Info("Scene", "12027 Boss伤害移除: count={0}", removeIds.Count);
        }

        /// <summary>12028 玩家协助id更改(S2C only)。pt_120.erl:256-258:AutoId:32, ConfigId:32,
        /// ChangeIds[u16×{RoleId:64,AssistId:64}]。更新 BossHurtList 里对应记录的 AssistId 字段。</summary>
        private void On12028(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            List<(long RoleId, long AssistId)> changes = reader.ReadArray(r => (r.ReadU64(), r.ReadU64()));
            SceneMiscModel.Instance.BossHurtAutoId = autoId;
            SceneMiscModel.Instance.BossHurtConfigId = configId;
            foreach ((long roleId, long assistId) in changes)
            {
                SceneMiscModel.BossHurtEntry entry = SceneMiscModel.Instance.BossHurtList.Find(e => e.RoleId == roleId);
                if (entry != null) entry.AssistId = assistId;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_BOSS_ASSIST_CHANGE);
            GameLog.Info("Scene", "12028 协助id变更: count={0}", changes.Count);
        }

        /// <summary>12030 动态区域标记(独立推送)。pt_120.erl:261-263,546-550(pack_area_mark):
        /// AreaMarkList[u16×{AreaId:8,ClientType:8}]。与 12002 快照尾部内嵌的同结构块
        /// (<see cref="SkipAreaMark"/>)是两个不同调用点,本号整表替换存量。</summary>
        private void On12030(NetReader reader)
        {
            List<SceneMiscModel.AreaMarkEntry> marks = reader.ReadArray(r => new SceneMiscModel.AreaMarkEntry
            {
                AreaId = r.ReadU8(),
                ClientType = r.ReadU8(),
            });
            SceneMiscModel.Instance.AreaMarks.Clear();
            SceneMiscModel.Instance.AreaMarks.AddRange(marks);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_AREA_MARK);
            GameLog.Info("Scene", "12030 动态区域标记: count={0}", marks.Count);
        }

        /// <summary>12036 血量变化广播(战斗表现核心)。pt_120.erl:288-291(7参子句补0补到9参):
        /// Sign:8, Id:64, Hp:64, HpLim:64, IsMinus:8, Change:64, BuffId:16, SourceSign:8, SourceId:64。
        /// Hp/HpLim 复用既有 SceneManager.ApplyHp(与 12009 同一路径,按数值先怪后人试探——老端
        /// On12036 按 Sign 精确路由到 Monster/Role,Unity 暂沿用既有 12009 试探式简化版,足以保证
        /// 血量数值正确并顺带触发 MonsterHpChanged/RoleHpChanged;精确按 Sign 分流留待渲染层需要
        /// 吸血/反弹/流血特效来源对象时再补)。Change/BuffId/SourceSign/SourceId 等"表现"专属字段
        /// 落 SceneMiscModel,飘字/特效消费留档(老端 SceneController.ts:553-616 On12036)。m8存档:
        /// 老端本号读完字段后有 `if(!scene_mgr.can_receive_scene_protocal) return;` 门控(ts:566-567),
        /// 本端未镜像该门控(Unity 侧无对应"暂停接收场景协议"开关),留档不补。</summary>
        private void On12036(NetReader reader)
        {
            int sign = reader.ReadU8();
            long id = reader.ReadU64();
            long hp = reader.ReadU64();
            long hpLim = reader.ReadU64();
            int isMinus = reader.ReadU8();
            long change = reader.ReadU64();
            int buffId = reader.ReadU16();
            int sourceSign = reader.ReadU8();
            long sourceId = reader.ReadU64();

            SceneManager.Instance.ApplyHp(id, hp, hpLim);

            SceneMiscModel.Instance.LastHpChange = new SceneMiscModel.HpChangeInfo
            {
                Sign = sign,
                Id = id,
                Hp = hp,
                HpLim = hpLim,
                IsMinus = isMinus,
                Change = change,
                BuffId = buffId,
                SourceSign = sourceSign,
                SourceId = sourceId,
            };
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_HP_CHANGE);
            GameLog.Info("Scene", "12036 HP变化: sign={0} id={1} hp={2}/{3} change={4}({5}) buffId={6} source=({7},{8})",
                sign, id, hp, hpLim, change, isMinus == 1 ? "-" : "+", buffId, sourceSign, sourceId);
        }

        /// <summary>12043 怪物:玩家求助列表全量(C2S 查询)。pt_120.erl:78-79(read"i" AutoId),
        /// 316-323(write):AutoId:32, ConfigId:32, List[u16×{AssistId:64,RoleId:64,Name:s,
        /// ServerId:16,ServerNum:16,ServerName:s}]。</summary>
        private void On12043(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            List<SceneMiscModel.AssistEntry> list = reader.ReadArray(r => new SceneMiscModel.AssistEntry
            {
                AssistId = r.ReadU64(),
                RoleId = r.ReadU64(),
                Name = r.ReadString(),
                ServerId = r.ReadU16(),
                ServerNum = r.ReadU16(),
                ServerName = r.ReadString(),
            });
            SceneMiscModel.Instance.AssistAutoId = autoId;
            SceneMiscModel.Instance.AssistConfigId = configId;
            SceneMiscModel.Instance.AssistList.Clear();
            SceneMiscModel.Instance.AssistList.AddRange(list);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_ASSIST_LIST);
            GameLog.Info("Scene", "12043 求助列表全量: autoId={0} configId={1} count={2}", autoId, configId, list.Count);
        }

        /// <summary>C2S 查询怪物的玩家求助列表。pt_120.erl:78-79 read(12043,"i")。</summary>
        public void RequestAssistList(int autoId) => SendFmt(Proto.SC_ASSIST_LIST, "i", autoId);

        /// <summary>12044 增加玩家求助(S2C only)。pt_120.erl:326-329:字段同 12043 元素,前缀
        /// AutoId:32,ConfigId:32。按 AssistId 在 AssistList 里 upsert。</summary>
        private void On12044(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            var entry = new SceneMiscModel.AssistEntry
            {
                AssistId = reader.ReadU64(),
                RoleId = reader.ReadU64(),
                Name = reader.ReadString(),
                ServerId = reader.ReadU16(),
                ServerNum = reader.ReadU16(),
                ServerName = reader.ReadString(),
            };
            SceneMiscModel.Instance.AssistAutoId = autoId;
            SceneMiscModel.Instance.AssistConfigId = configId;
            List<SceneMiscModel.AssistEntry> list = SceneMiscModel.Instance.AssistList;
            int idx = list.FindIndex(e => e.AssistId == entry.AssistId);
            if (idx >= 0) list[idx] = entry; else list.Add(entry);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_ASSIST_ADD);
            GameLog.Info("Scene", "12044 求助新增: assistId={0} roleId={1}", entry.AssistId, entry.RoleId);
        }

        /// <summary>12045 删除玩家求助(S2C only)。pt_120.erl:332-333:AutoId:32, ConfigId:32,
        /// DelAssistId:64。</summary>
        private void On12045(NetReader reader)
        {
            int autoId = (int)reader.ReadU32();
            int configId = (int)reader.ReadU32();
            long delAssistId = reader.ReadU64();
            SceneMiscModel.Instance.AssistAutoId = autoId;
            SceneMiscModel.Instance.AssistConfigId = configId;
            SceneMiscModel.Instance.AssistList.RemoveAll(e => e.AssistId == delAssistId);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_ASSIST_REMOVE);
            GameLog.Info("Scene", "12045 求助删除: assistId={0}", delAssistId);
        }

        /// <summary>12078 婚姻名/转职等 Figure 变更广播(含主角自身分支)。pt_120.erl:363-365:
        /// Id:64, Figure(write_figure)。老端 On12078(SceneController.ts:729-738)只摘 marriage_name
        /// 字段,且主角 career 变化时额外触发 mainrole_vo.changeCareer(转职换装全套表现)。本端整块
        /// 替换 Figure(RoleVo.Figure/RoleModel.Figure 本就是同一 FigureProto 类型,信息量只多不少),
        /// 对标既有 On12086 的主角/他人双分支写法;换装表现联动(TODO)留后续渲染层接入。</summary>
        private void On12078(NetReader reader)
        {
            long id = reader.ReadU64();
            FigureProto figure = FigureProto.Read(reader);
            if (id == RoleModel.Instance.RoleId)
            {
                RoleModel.Instance.Figure = figure;
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            }
            else
            {
                SetRoleField(id, vo => vo.Figure = figure);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_FIGURE_CHANGE);
            GameLog.Info("Scene", "12078 Figure变更: id={0} name={1}", id, figure?.name);
        }

        /// <summary>12080 怪物属性更新(S2C only)。pt_120.erl:371-373:Id:32, Attrs[u16×{Type:8,
        /// Value:32}]。老端 On12080(SceneController.ts:618-633)只处理 Type==3→can_attack,
        /// 其余 Type 一律忽略;复用既有 MonsterVo.CanAttack 字段。</summary>
        private void On12080(NetReader reader)
        {
            int id = (int)reader.ReadU32();
            List<(int Type, int Value)> attrs = reader.ReadArray(r => ((int)r.ReadU8(), (int)r.ReadU32()));
            foreach ((int type, int value) in attrs)
            {
                if (type == 3) SetMonsterField(id, vo => vo.CanAttack = value);
                else GameLog.Info("Scene", "12080 怪物属性变更(未映射 type,老端同样忽略): id={0} type={1} value={2}", id, type, value);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_MONSTER_ATTR_UPDATE);
        }

        /// <summary>12083 复活完成。pt_120.erl:383-385:ReviveType:8,ScenceId:32,X:16,Y:16,
        /// ScenceName:s,Hp:64,Gold:32,BGold:32,AttProtectedTime:16(9 字段;ReviveType 1=原地复活
        /// 2=换场景复活)。老端 On12083(SceneController.ts:655-677)除落数据外还直接改
        /// mainrole_vo 位置/血量、按 ReviveType 触发原地/换场景复活,并发 RELIVE_COMPLETE +
        /// REQUEST_RELIVE_TIMES(复活次数查询,20009/20017 家族)。本轮只落 SceneMiscModel+发
        /// EVT_SCENE_MISC_UPDATE,不联动 Relive 模块、不直接改 RoleModel 位置(避免绕开 12005
        /// 正常换图流程造成地图/角色状态不同步)——TODO:待 Relive 模块(20009/20017 家族)
        /// 落地后再对接消费(RELIVE_COMPLETE 事件 + 剩余复活次数请求)。</summary>
        private void On12083(NetReader reader)
        {
            int reviveType = reader.ReadU8();
            int sceneId = (int)reader.ReadU32();
            int x = reader.ReadU16();
            int y = reader.ReadU16();
            string sceneName = reader.ReadString();
            long hp = reader.ReadU64();
            int gold = (int)reader.ReadU32();
            int bGold = (int)reader.ReadU32();
            int attProtectedTime = reader.ReadU16();

            SceneMiscModel.Instance.LastRevive = new SceneMiscModel.ReviveInfo
            {
                ReviveType = reviveType,
                SceneId = sceneId,
                X = x,
                Y = y,
                SceneName = sceneName,
                Hp = hp,
                Gold = gold,
                BGold = bGold,
                AttProtectedTime = attProtectedTime,
            };
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_REVIVE_COMPLETE);
            GameLog.Info("Scene", "12083 复活完成: type={0} scene={1}({2}) pos=({3},{4}) hp={5} gold={6}/{7} protect={8}",
                reviveType, sceneId, sceneName, x, y, hp, gold, bGold, attProtectedTime);
        }

        /// <summary>12085 安全区状态(**GapMap"小飞鞋"标注订正**:实为区域安全状态九宫格回显广播,
        /// 非小飞鞋;小飞鞋归 12033/AutoFight 13300 家族)。pt_120.erl:82-83(read"c" Type),392-393
        /// (write):PlayerId:64, Type:8(pp_scene.erl:232-235 按区域广播,PlayerId 不一定是自己)。
        /// RoleVo 没有 SafeAreaState 字段(老端 role_vo.ChangeVar 那个槽位),落 SceneMiscModel
        /// 按 PlayerId 记录,自己的镜像另存一份方便 HUD 直接读。</summary>
        private void On12085(NetReader reader)
        {
            long playerId = reader.ReadU64();
            int type = reader.ReadU8();
            SceneMiscModel.Instance.SafeAreaStateByPlayer[playerId] = type;
            if (playerId == RoleModel.Instance.RoleId) SceneMiscModel.Instance.MainRoleSafeAreaState = type;
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_SAFE_AREA_STATE);
            GameLog.Info("Scene", "12085 安全区状态: playerId={0} type={1}", playerId, type);
        }

        /// <summary>C2S 上报区域安全状态(老端发送点绑定 SAFE_AREA_CHANGE 事件,
        /// SceneController.ts:1153-1155)。Unity 侧暂无对应事件源(区域触发器未接线),先提供方法
        /// 占位,TODO 待区域触发逻辑落地后接线。pt_120.erl:82-83 read(12085,"c")。</summary>
        public void RequestSafeAreaState(int type) => SendFmt(Proto.SC_SAFE_AREA_STATE, "c", type);

        /// <summary>12087 场景玩家计数。pt_120.erl:85-86(read"h" SceneId),400-401(write):
        /// SceneId:16, Num:16。老端消费方=BossModel.UPDATE_PLAYER_NUM(SceneController.ts:814-818),
        /// 该模块本轮不在 Unity 范围内,落 SceneMiscModel 占位,TODO 待 BossModel 移植后接线。</summary>
        private void On12087(NetReader reader)
        {
            int sceneId = reader.ReadU16();
            int num = reader.ReadU16();
            SceneMiscModel.Instance.PlayerCountSceneId = sceneId;
            SceneMiscModel.Instance.PlayerCountNum = num;
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_PLAYER_COUNT);
            GameLog.Info("Scene", "12087 场景人数: sceneId={0} num={1}", sceneId, num);
        }

        /// <summary>C2S 请求场景玩家计数(老端发送点绑定 SCENE_PALYER_COUNT 事件,
        /// SceneController.ts:1147-1149)。Unity 侧暂无对应事件源,先提供方法占位,TODO 待接线。</summary>
        public void RequestPlayerCount(int sceneId) => SendFmt(Proto.SC_PLAYER_COUNT, "h", sceneId);

        /// <summary>12088 场景内简单用户列表。pt_120.erl:88-89(read 裸,无参),403-406(write),
        /// 703-709(pack_simple_user):Users[u16×{Platform:s,ServerNum:16,Id:64,Sex:8,Realm:8,
        /// Career:8,Lv:16,Name:s,Picture:s,PictureVer:32}]。B7裁决:老端全仓零引用(既无发送点、也未
        /// RegisterProtocal 挂 recv,照 17241-43/33903 死号先例),但服务端 read/write 双活——按"服务端
        /// 可能发、老端不管"的口径只保留防御接收(解析落地,不炸不丢字节对齐),已删除
        /// <c>RequestSimpleUserList</c> 发送方法,本端不主动发起该请求。</summary>
        private void On12088(NetReader reader)
        {
            List<SceneMiscModel.SimpleUserEntry> list = reader.ReadArray(r => new SceneMiscModel.SimpleUserEntry
            {
                Platform = r.ReadString(),
                ServerNum = r.ReadU16(),
                Id = r.ReadU64(),
                Sex = r.ReadU8(),
                Realm = r.ReadU8(),
                Career = r.ReadU8(),
                Lv = r.ReadU16(),
                Name = r.ReadString(),
                Picture = r.ReadString(),
                PictureVer = (int)r.ReadU32(),
            });
            SceneMiscModel.Instance.SimpleUsers.Clear();
            SceneMiscModel.Instance.SimpleUsers.AddRange(list);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_SIMPLE_USER_LIST);
            GameLog.Info("Scene", "12088 场景简单用户列表: count={0}", list.Count);
        }

        /// <summary>12090 公会id字段广播(S2C only)。pt_120.erl:439-440:Sign:8,Id:64,GuildId:64
        /// (SceneBaseType:1怪 2人 5假人)。Monster 分支复用既有 MonsterVo.GuildId(12007
        /// binary_12007 本就带这个字段);Role/Fake_Role 分支 RoleVo 没有 GuildId 槽位(12003
        /// binary_to_12003 没带这字段),落 SceneMiscModel.GuildIdByRole 占位(留待 RoleVo 加字段
        /// 时再打通)。m8存档:老端 On12090(SceneController.ts:861-873)有 TS 解构 bug——
        /// `let target_type, id, guild_id = this.ReadFmt("cll")` 漏了数组解构的中括号,
        /// 实际只有 guild_id 被赋值,target_type/id 恒为 undefined,导致后续两条 if 分支恒不命中、
        /// vo 恒为 null,老端这个协议事实上从未真正更新过 guild_id(自空)。本端按 wire 格式正确读三
        /// 字段并联动 Monster/Role 分支,是修复型偏差,不复刻该 bug。</summary>
        private void On12090(NetReader reader)
        {
            int sign = reader.ReadU8();
            long id = reader.ReadU64();
            long guildId = reader.ReadU64();
            if (sign == 1) // Monster(SceneBaseType.Monster=1)
            {
                SetMonsterField((int)id, vo => vo.GuildId = guildId);
            }
            else // Role(2) / Fake_Role(5) 等
            {
                SceneMiscModel.Instance.GuildIdByRole[id] = guildId;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_GUILD_ID_CHANGE);
            GameLog.Info("Scene", "12090 公会id变更: sign={0} id={1} guildId={2}", sign, id, guildId);
        }

        /// <summary>12092 怪物 Buff 批量(响应 C2S 变长数组[GoodsId:64])。pt_120.erl:442-446 +
        /// mod_scene_agent_cast.erl:515-528(AerBuffList = lib_skill_buff:pack_buff(...)):
        /// Len:16 数组[{Id:64, BuffList}],BuffList 本身自带 16位数量前缀(lib_skill_buff.erl:22-35
        /// pack_buff 末子句 `&lt;&lt;L:16,Data/binary&gt;&gt;`),元素结构 hhiccIIl 与既有
        /// <see cref="FightVo.BuffInfo"/> 完全一致(K/EffectId/SkillId/SkillLv/Stack/Int/Float1/T
        /// ↔ IconType/BuffEffectId/Id/Level/Diejia/Integer/Decimals/Period)——r18_server_scene
        /// 侦察标注的"预编码二进制内部结构待深挖"在本轮已核实,直接复用 BuffInfo,不新增重复结构体。</summary>
        private void On12092(NetReader reader)
        {
            List<(long Id, List<FightVo.BuffInfo> Buffs)> list = reader.ReadArray(r =>
            {
                long id = r.ReadU64();
                List<FightVo.BuffInfo> buffs = r.ReadArray(ReadMonsterBuffInfo);
                return (id, buffs);
            });
            foreach ((long id, List<FightVo.BuffInfo> buffs) in list)
            {
                SceneMiscModel.Instance.MonsterBuffs[id] = buffs;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MISC_UPDATE, Proto.SC_MONSTER_BUFF_BATCH);
            GameLog.Info("Scene", "12092 怪物Buff批量: count={0}", list.Count);
        }

        /// <summary>buff:hhiccIIl(与 AssistVo.ReadBuff/FightVo.BuffInfo 同结构,独立复制一份避免
        /// 跨类依赖私有方法,与既有 AssistVo 的做法一致)。</summary>
        private static FightVo.BuffInfo ReadMonsterBuffInfo(NetReader r)
        {
            FightVo.BuffInfo buff;
            buff.IconType = r.ReadU16();
            buff.BuffEffectId = r.ReadU16();
            buff.Id = (int)r.ReadU32();
            buff.Level = r.ReadU8();
            buff.Diejia = r.ReadU8();
            buff.Integer = r.ReadI32();
            buff.Decimals = r.ReadI32();
            buff.Period = r.ReadU64();
            return buff;
        }

        /// <summary>
        /// 12092 C2S 请求怪物/场景对象 Buff 批量(对标老端 REQUEST_MONSTER_BUFF 事件触发点,
        /// SceneController.ts:1156-1163:`if(ins_id){ WriteBegin(12092); WriteFMT("h",1); WriteFMT("l",ins_id); SendToGame(); }`)。
        /// pt_120.erl:94-100 read(12092) 为变长数组[GoodsId:64](u16 计数前缀+定长元素)。B5修复:此前
        /// "框架无变长数组写原语"是误判——SendFmt 本就接受运行时拼出的格式串(同轮 GodBefallController.
        /// RequestSmartSynthesis(44016)/RequestTypeStrengthen(44018) 先例:先拼 "h"+"l"×N 字符串再调用),
        /// 改为真发送。空/null 列表镜像老端 if(ins_id) 空值不发的守卫。
        /// </summary>
        public void RequestMonsterBuffList(IReadOnlyList<long> goodsIds)
        {
            if (goodsIds == null || goodsIds.Count == 0) return; // 镜像老端 if(ins_id):空值不发
            var fmt = new StringBuilder("h");
            var args = new List<object> { goodsIds.Count };
            foreach (long id in goodsIds)
            {
                fmt.Append('l');
                args.Add(id);
            }
            SendFmt(Proto.SC_MONSTER_BUFF_BATCH, fmt.ToString(), args.ToArray());
            GameLog.Info("Scene", "request 12092: 怪物Buff批量查询 count={0}", goodsIds.Count);
        }
    }
}
