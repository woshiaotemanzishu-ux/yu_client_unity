using System;
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
        private void On12074(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int v = r.ReadU8(); SetRoleField(id, vo => vo.PkStatus = v); }  // PK 状态
        private void On12075(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int v = r.ReadU8(); SetRoleField(id, vo => vo.Show = v); }      // 展示状态
        private void On12082(NetReader r) { r.ReadU8(); long id = r.ReadU64(); int spd = r.ReadU16(); SetRoleField(id, vo => vo.Speed = spd); } // 移动速度
        private void On12086(NetReader r) { long id = r.ReadU64(); string name = r.ReadString(); SetRoleField(id, vo => { if (vo.Figure != null) vo.Figure.name = name; }); } // 改名(无 sign 前缀)

        private static void SetRoleField(long roleId, System.Action<RoleVo> set)
        {
            RoleVo vo = SceneManager.Instance.GetRole(roleId);
            if (vo == null) return;
            set(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_ROLE_STATE);
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
            role.SceneId = instanceId;
            role.X = x;
            role.Y = y;
            role.DunId = dunId;

            // 进入新场景:清空上一场景的对象表(对标老客户端 ClearAllVo),再由 12100/12002 重新填充。
            SceneManager.Instance.Clear();
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
                return;
            }
#endif
            RoleModel role = RoleModel.Instance;
            await LegacyPreloadService.PreloadSceneMapAsync(sceneId, role.X, role.Y);
            SceneMapData data = await SceneMapLoader.LoadAsync(sceneId);
            if (version != _loadVersion || data == null) return;

            await SceneMapView.ShowAsync(data, role.X, role.Y);
            if (version != _loadVersion) return;

            SendFmt(Proto.SC_NPC_LIST, "i", sceneId);
            GameLog.Info("Scene", "request 12100: local map loaded sceneId={0}", sceneId);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MAP_READY);
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
                await Task.Delay(1000); // 让怪物渲染就位
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
                await Task.Delay(500);
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
            SceneManager.Instance.AddRole(vo);
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
    }
}
