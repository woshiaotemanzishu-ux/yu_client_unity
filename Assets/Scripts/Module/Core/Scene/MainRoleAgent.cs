using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 主角移动驱动(对标老客户端 MainRole.UpdateStateMove + Character/Role 的待机/跑动切换):
    /// 每帧读 <see cref="SceneInput"/> 的摇杆方向,按移动速度推进真实像素坐标,撞墙时按 X/Y 分轴滑动,
    /// 写回 <see cref="RoleModel"/> 并让相机跟随(地图在脚下滚动 = 跑动表现),播放 run/idle 动作并转向,
    /// 期间约每 0.5s 上报一次 12001(对标 MainRole.ts:546-598 的 0.5s 节流),松手时补发一次最终坐标。
    ///
    /// 说明:本工程地图是 UGUI 层、相机跟随靠滚动场景层实现(SceneMapView),主角恒居屏幕中心;
    /// 3D 主角模型在 UGUI 地图上的精确合成属于"待真机验证"项(见进度 2026-06-15),此处只负责
    /// 数据/动作/朝向/相机跟随这条可验证的逻辑线。
    /// </summary>
    public sealed class MainRoleAgent : MonoBehaviour
    {
        private const float MoveSpeed = 250f;       // Character.ts:63 move_speed = 250(像素/秒)
        private const float MaxDeltaTime = 0.04f;   // MainRole.ts:746 单帧步进上限
        private const float SendInterval = 0.5f;    // MainRole.ts:547 上报节流
        private const int MoveTypeNormal = 0;       // SceneConfig NORMOL_MOVE
        private static readonly float TurnSmoothSpeed = 720f; // 转向角速度(度/秒);<=0 则瞬时转向

        // —— 自动接近 NPC(对标 Scene.MainRoleToNpc → MainRoleMove)——
        private const float ArrivalLogicDist = 2.5f; // 到达判定半径(逻辑格,老端 dist=2.5)
        private const float AutoMoveTimeout = 8f;    // 直线接近兜底超时(无 A* 绕障:到不了也要把对话开出来)
        private const float AutoStuckSeconds = 0.6f; // 连续无位移进展达此时长 → 判定卡死兜底
        private const float AutoStuckEpsilon = 0.5f; // 单帧像素位移进展阈值(< 此值视为无进展)

        private const string ActionIdle = "idle";
        private const string ActionRun = "run";
        private const string ActionJump = "jump";
        private const string ActionCollect = "collect"; // 蹲下采集动作(对标老端 Role.EnterStateCollect 的 PlayAction("collect"))
        private const string ActionDeath = "death";     // 死亡动作(对标老端 Character.ts:562 EnterStateDead → PlayAction("death"),
                                                         // 同名常量已见于 MonsterRenderer.ACTION_DEATH,主角/怪共用同一动作名约定)
        private const float TaskJumpReportDistance = 300f;

        /// <summary>当前主角驱动(MainRoleFlow 装配后唯一存在;清主角时置空)。任务/对话用它让主角朝 NPC 转向。</summary>
        public static MainRoleAgent Current { get; private set; }

        private Transform _modelTr;     // 模型子节点(用于转向)
        private Animation _anim;        // 老拼装管线在模型根挂的 Animation(混合容器根上没有,此时为 null)
        private ReplaceableRoleModel _driver; // model_replacement 命中时 BuildAsync 返回容器上的混合驱动器;动作一律优先走它
        private GameObject _model;
        private int _career;
        private int _sex;
        private int _clotheRes;
        private int _actionVersion;
        private Vector3 _modelBaseLocalPos;
        private int _jumpActionCursor;
        private int _levelUpEffectCount;

        private float _posX;            // 真实像素 X(real_pos.x)
        private float _posY;            // 真实像素 Y(real_pos.y)
        private float _sendTimer;
        private bool _moving;
        private bool _collecting;       // 采集态(对标老端 PoseState.COLLECT):蹲下采集动作进行中,直到 QuitCollect/起步打断

        // —— 自动接近目标(直线 + 分轴滑行,无 A*;对标 MainRoleToNpc 走到 NPC 身边再触发)——
        private bool _autoMoving;
        private float _autoTargetX;
        private float _autoTargetY;
        private float _autoArriveLogic;
        private Action _onArrive;
        private float _autoElapsed;
        private float _autoStuckTime;
        private float _autoLastX;
        private float _autoLastY;
        private bool _autoInvokeOnFail = true;

        /// <summary>由 MainRoleFlow 在装配完成后初始化:传入模型子节点与出生坐标。</summary>
        public void Init(GameObject model, int spawnX, int spawnY, int career, int sex, int clotheRes)
        {
            Current = this;
            _model = model;
            _modelTr = model != null ? model.transform : transform;
            _anim = model != null ? model.GetComponent<Animation>() : null;
            _driver = model != null ? model.GetComponent<ReplaceableRoleModel>() : null;
            _career = career;
            _sex = sex;
            _clotheRes = clotheRes;
            _modelBaseLocalPos = _modelTr != null ? _modelTr.localPosition : Vector3.zero;
            _posX = spawnX;
            _posY = spawnY;
            _actionVersion++;
            _moving = false;
            SetAutoFindWayState(false);
            _sendTimer = 0f;
            PlayAction(ActionIdle);
            SyncModelScreenOffset(); // 出生点可能就在地图边缘:按相机夹边量先把模型摆到正确屏幕位
        }

        /// <summary>
        /// 让主角模型在屏幕上对齐它的逻辑格:把 (role.X - cameraX, role.Y - cameraY) 推给合成台。
        /// 地图内部相机跟随主角,偏移为 0(模型居中,沿用经验落点);靠近边缘相机夹紧后偏移增大,
        /// 模型随之滑向屏幕边缘——这样「画出来的主角」始终压在它真正占用的逻辑格上,而非恒居屏幕中心
        /// (恒居中心正是之前"看着像走进墙里"的根因:碰撞用逻辑格判定一直是对的,只是模型画歪了)。
        /// </summary>
        private void SyncModelScreenOffset()
        {
            RoleModel role = RoleModel.Instance;
            Vector2 cam = SceneMapView.CameraPos;
            SceneCharacterStage.SetMainRoleScreenOffset(new Vector2(role.X - cam.x, role.Y - cam.y));
        }

        private void Update()
        {
            SceneMapData map = SceneMapLoader.Current;
            if (map == null) return;

            CheckCrossSafeArea(map);

            // 战斗演出冻结(大妖来袭横幅期间):停下并锁住一切移动/寻路,玩家停在战斗前的位置(对标老端
            // ShowBossBornEffect 期间 STOPAUTOFIGHT 连移动一并停)。一处早退同时挡住手动摇杆 StepMove 与自动接近 AutoStep。
            if (AutoFightModel.Instance.CombatFreeze)
            {
                if (_moving) StopMove();              // 立即收脚 → idle + 补发最终坐标,钉在原地
                _autoMoving = false; _onArrive = null; // 放弃在途自动接近(解冻后由战斗循环按正面站位重新接近)
                return;
            }

            bool hasManual = SceneInput.Active && SceneInput.HasDirection;

            // 自动接近进行中:玩家一推摇杆即取消自动、让位手动;否则本帧由自动驱动。
            if (_autoMoving)
            {
                if (hasManual) CancelAutoMove("玩家推摇杆,自动接近 NPC 让位手动");
                else { AutoStep(map); return; }
            }

            if (hasManual)
            {
                StepMove(map);
            }
            else if (_moving)
            {
                StopMove();
            }
        }

        // —— 安全区进出检测(对标老端 MainRole.CheckIsCrossSafeArea + RoleVo.safe_area_state)——
        // 1=非安全场景内的安全区格 2=非安全区格 3=安全场景(整场安全,老端此分支不飘字)。
        // 静态:跨场景/换模型不复位(对标 vo 挂账号),只有状态翻转才飘字;登录后首帧从 0 出发必飘一次。
        private static int _safeAreaState;

        private void CheckCrossSafeArea(SceneMapData map)
        {
            if (!MainUIConfigs.IsSceneLoaded) return;   // 场景表未就绪不判定(启动预加载已含,正常必就绪)

            MainUIConfigs.SceneCfg cfg = MainUIConfigs.GetSceneCfg(map.SceneId);
            if (cfg != null && cfg.Subtype == 1)
            {
                _safeAreaState = 3;
                return;
            }
            if (map.IsSafePixel(_posX, _posY))
            {
                if (_safeAreaState == 1) return;
                _safeAreaState = 1;
                TipsManager.Toast("进入安全区");
            }
            else
            {
                if (_safeAreaState == 2) return;
                _safeAreaState = 2;
                TipsManager.Toast("走出安全区");
            }
        }

        private void StepMove(SceneMapData map)
        {
            // 手动摇杆会打断任务跳跃等异步寻路；普通自动接近走 AutoStep，不经过这里。
            SetAutoFindWayState(false);
            Vector2 dir = SceneInput.Dir; // 舞台坐标:x 右、y 下,与地图像素一致
            float dt = Mathf.Min(Time.deltaTime, MaxDeltaTime);
            float moveDist = MoveSpeed * dt;

            bool moved = Advance(map, dir.x * moveDist, dir.y * moveDist);

            RoleModel role = RoleModel.Instance;
            BeginMoveAnim();
            Face(dir);
            SceneMapView.SetFocus(role.X, role.Y);
            SyncModelScreenOffset(); // 焦点(相机)已更新,随即把模型摆到 (role - camera) 的屏幕偏移上

            if (moved) ThrottledSend(role, dt);
        }

        /// <summary>
        /// 单步推进内核:整向 → 仅 X → 仅 Y 分轴撞墙滑动(对标 MainRole.ts:794-819),按真实像素步进并写回
        /// <see cref="RoleModel"/> 逻辑格;返回本帧是否真的发生了位移(供上报节流与卡死检测判断)。
        /// 手动摇杆(<see cref="StepMove"/>)与自动接近(<see cref="AutoStep"/>)共用此内核,行为完全一致。
        /// </summary>
        private bool Advance(SceneMapData map, float mx, float my)
        {
            bool moved = true;
            if (!map.IsBlockPixel(_posX + mx, _posY + my))
            {
                _posX += mx;
                _posY += my;
            }
            else if (!map.IsBlockPixel(_posX + mx, _posY))
            {
                _posX += mx;
            }
            else if (!map.IsBlockPixel(_posX, _posY + my))
            {
                _posY += my;
            }
            else
            {
                moved = false;
            }

            RoleModel role = RoleModel.Instance;
            role.X = Mathf.Max(0, Mathf.FloorToInt(_posX));
            role.Y = Mathf.Max(0, Mathf.FloorToInt(_posY));
            return moved;
        }

        // 进入跑动态:切 run 动作 + 起步立即上报一次(对标手动起步)。已在跑动则不重复。
        private void BeginMoveAnim()
        {
            // 采集中起步 → 打断采集(对标老端 CollectBarView 监听 MAINROLE_MOVE_EVENT_IMME 取消采集)。
            // 仅置态 + 发事件由 CollectController 向服务端发取消;动作随即被下面的 run 覆盖,无需先回 idle。
            if (_collecting)
            {
                _collecting = false;
                EventDispatcher.Emit(GlobalEvent.EVT_COLLECT_MOVE_CANCEL);
            }
            if (_moving) return;
            _moving = true;
            _actionVersion++;
            EffectBinder.ClearTag(_model, "action");
            ResetModelVisualOffset();
            PlayAction(ActionRun);
            _sendTimer = SendInterval; // 起步立即上报一次
        }

        // 移动上报节流(0.5s 一次,对标 MainRole.ts:547)。
        private void ThrottledSend(RoleModel role, float dt)
        {
            _sendTimer += dt;
            if (_sendTimer >= SendInterval)
            {
                _sendTimer = 0f;
                SceneController.Instance.SendMoveRequest(role.X, role.Y, MoveTypeNormal, role.X, role.Y);
            }
        }

        private void StopMove()
        {
            _moving = false;
            ResetModelVisualOffset();
            PlayAction(ActionIdle);
            // 对标 MainRole.QuitStateMove:松手补发一次最终坐标
            RoleModel role = RoleModel.Instance;
            SceneController.Instance.SendMoveRequest(role.X, role.Y, MoveTypeNormal, role.X, role.Y);
        }

        /// <summary>
        /// 副本进入/入场/结算演出统一收脚。取消在途自动接近和异步动作，清掉动作粒子并回到 idle；
        /// 若角色确实在移动则补发一次最终坐标，避免服务端仍认为角色在跑。
        /// </summary>
        public void StopForPresentation()
        {
            _actionVersion++;
            _autoMoving = false;
            SetAutoFindWayState(false);
            _onArrive = null;
            _autoInvokeOnFail = true;
            if (_collecting)
            {
                _collecting = false;
                EventDispatcher.Emit(GlobalEvent.EVT_COLLECT_MOVE_CANCEL);
            }
            if (_model != null) EffectBinder.ClearTag(_model, "action");
            ResetModelVisualOffset();
            if (_moving) StopMove();
            else PlayAction(ActionIdle);
        }

        /// <summary>
        /// 接受 12005 的服务端权威落点，并同时废弃上一场景遗留的自动接近、动作与内部浮点坐标。
        /// 不回发 12001；新地图就绪后由 MainRoleFlow.Init 负责相机与屏幕位置的最终同步。
        /// </summary>
        public void ApplyAuthoritativeScenePosition(int x, int y)
        {
            _actionVersion++;
            _posX = Mathf.Max(0, x);
            _posY = Mathf.Max(0, y);
            _sendTimer = 0f;
            _moving = false;
            _collecting = false;
            _autoMoving = false;
            SetAutoFindWayState(false);
            _onArrive = null;
            _autoElapsed = 0f;
            _autoStuckTime = 0f;
            _autoInvokeOnFail = true;
            if (_model != null) EffectBinder.ClearTag(_model, "action");
            ResetModelVisualOffset();
            PlayAction(ActionIdle);
        }

        /// <summary>
        /// 全向转向:让主角连续朝向移动方向(对标老客户端 atan2 全向转身,非左右翻面)。
        /// 输入 dir 为舞台坐标(x 右、y 下,已归一化)。合成台相机看向世界 +Z、俯角 24°:
        /// 屏右=世界+X、屏下(朝相机)=世界-Z;模型美术正脸朝本地 -Z(故 yaw=180 静止背对相机)。
        /// 令正脸朝世界 V=(dir.x,0,-dir.y) 解得 yaw=Atan2(-dir.x,dir.y);该式在屏上方向自动给出 180°,
        /// 与 SceneCharacterStage.SetMainRole 的基准 Euler(0,180,0) 自洽(故 Face 内不再叠加基准 yaw)。
        /// 若实跑发现左右反/上下反/整体差 180°,翻对应参数符号或整体 +180(见进度文档验证清单)。
        /// </summary>
        private void Face(Vector2 dir)
        {
            if (_modelTr == null || dir.sqrMagnitude < 0.0001f) return; // 无方向(死区/松手)保持当前朝向

            // 实跑(2026-06-17)确认上下+左右皆反 → 模型美术朝向与初判相反,整体 +180°(两参同时取反)。
            // 屏上跑(dir=(0,-1))→yaw 0、屏下→180、右→90、左→-90:连续朝向移动方向。
            float yaw = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
            Vector3 e = _modelTr.localEulerAngles;
            if (TurnSmoothSpeed <= 0f)
            {
                _modelTr.localEulerAngles = new Vector3(e.x, yaw, e.z); // 瞬时转向
            }
            else
            {
                // 沿最短弧平滑到目标 yaw(对标老客户端 >10° 分帧平滑转身)
                float newY = Mathf.MoveTowardsAngle(e.y, yaw, TurnSmoothSpeed * Time.deltaTime);
                _modelTr.localEulerAngles = new Vector3(e.x, newY, e.z);
            }
        }

        /// <summary>
        /// 主角瞬时朝某像素坐标转身(对标老端 Scene.MainRoleToNpc 的 main_role.SetDirection(npcpos.getDir(rolepos)))。
        /// 任务点击找 NPC 时调用——"任务驱动角色发生动作"的最小可见行为(完整走到 NPC 的寻路/直线移动见下一轮 P2)。
        /// dir 用舞台坐标(x 右、y 下,与地图像素一致),与 <see cref="Face"/> 同一 yaw 解算,但瞬时落位。
        /// 之后玩家推摇杆时 Update→Face 会接管朝向,不冲突。
        /// </summary>
        public void FaceTowardPixel(float targetX, float targetY)
        {
            if (_modelTr == null) return;
            RoleModel role = RoleModel.Instance;
            Vector2 dir = new Vector2(targetX - role.X, targetY - role.Y);
            if (dir.sqrMagnitude < 0.0001f) return; // 与目标同格:保持当前朝向
            dir.Normalize();
            float yaw = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
            Vector3 e = _modelTr.localEulerAngles;
            _modelTr.localEulerAngles = new Vector3(e.x, yaw, e.z);
        }

        // ===================== 自动接近目标 NPC(对标 Scene.MainRoleToNpc → MainRoleMove)=====================

        /// <summary>
        /// 主角自动走到目标像素点附近,到达后触发 <paramref name="onArrive"/>(对标老端 Scene.MainRoleToNpc:
        /// 走到 NPC 身边 dist≤2.5 逻辑格、停下转身后才 Fire(SHOW_TASK))。
        ///
        /// 本端无 A* 寻路,用直线方向 + <see cref="Advance"/> 分轴撞墙滑行逼近,因此**必有兜底**:卡死
        /// (连续无位移进展)或超时(沿墙滑行抵达不了)也会触发回调把对话开出来,绝不软锁(任务包 P1 硬约束)。
        /// 玩家中途推摇杆 → <see cref="CancelAutoMove"/> 取消自动、不触发回调(让位手动,可重新点任务)。
        /// </summary>
        /// <param name="targetX">目标真实像素 X(NpcVo.X,与主角同一坐标系)。</param>
        /// <param name="targetY">目标真实像素 Y(NpcVo.Y)。</param>
        /// <param name="arriveLogicDist">到达判定半径(逻辑格;<=0 用默认 2.5)。</param>
        /// <param name="onArrive">到达或兜底后回调(对话入口 ShowTask)。</param>
        public void MoveToNpc(float targetX, float targetY, float arriveLogicDist, Action onArrive)
        {
            MoveToNpcInternal(targetX, targetY, arriveLogicDist, onArrive, true);
        }

        public void MoveToNpcStrict(float targetX, float targetY, float arriveLogicDist, Action onArrive)
        {
            MoveToNpcInternal(targetX, targetY, arriveLogicDist, onArrive, false);
        }

        private void MoveToNpcInternal(float targetX, float targetY, float arriveLogicDist, Action onArrive, bool invokeOnFail)
        {
            _autoTargetX = targetX;
            _autoTargetY = targetY;
            _autoArriveLogic = arriveLogicDist > 0f ? arriveLogicDist : ArrivalLogicDist;
            _autoInvokeOnFail = invokeOnFail;

            // 已在范围内:不移动,直接转身 + 触发(对标 MainRoleToNpc 的 GetDistance<=dist+1 早退分支)。
            if (ReachedTarget())
            {
                _autoMoving = false;
                SetAutoFindWayState(false);
                _onArrive = null;
                FaceTowardPixel(targetX, targetY);
                // 回调语义由调用方决定(对话/杀怪/采集),别在这写死"开对话"——曾把杀怪链误导成对话链。
                GameLog.Info("Scene", "MoveToNpc: 主角已在目标点附近,直接触发到达回调");
                onArrive?.Invoke();
                return;
            }

            _onArrive = onArrive;
            _autoMoving = true;
            SetAutoFindWayState(true);
            _autoElapsed = 0f;
            _autoStuckTime = 0f;
            _autoLastX = _posX;
            _autoLastY = _posY;
            GameLog.Info("Scene", "MoveToNpc: 自动直线接近目标 ({0:F0},{1:F0}),到达半径={2} 逻辑格", targetX, targetY, _autoArriveLogic);
        }

        // 自动接近单帧:逼近 → 到达/卡死/超时三选一收尾,收尾必触发回调(避免软锁)。
        private void AutoStep(SceneMapData map)
        {
            RoleModel role = RoleModel.Instance;
            if (ReachedTarget()) { FinishAutoMove(true, null); return; }

            float dt = Mathf.Min(Time.deltaTime, MaxDeltaTime);
            _autoElapsed += dt;

            Vector2 dir = new Vector2(_autoTargetX - _posX, _autoTargetY - _posY);
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            float moveDist = MoveSpeed * dt;

            bool moved = Advance(map, dir.x * moveDist, dir.y * moveDist);

            BeginMoveAnim();
            Face(dir);
            SceneMapView.SetFocus(role.X, role.Y);
            SyncModelScreenOffset();
            if (moved) ThrottledSend(role, dt);

            // 卡死检测:连续无像素位移进展(被墙挡死、无 A* 绕障)累计到阈值即兜底触发。
            float progressed = Mathf.Abs(_posX - _autoLastX) + Mathf.Abs(_posY - _autoLastY);
            _autoLastX = _posX;
            _autoLastY = _posY;
            if (progressed < AutoStuckEpsilon) _autoStuckTime += dt; else _autoStuckTime = 0f;

            if (ReachedTarget()) { FinishAutoMove(true, null); return; }
            if (_autoStuckTime >= AutoStuckSeconds) { FinishAutoMove(false, "卡死(直线被挡且无 A* 绕障)"); return; }
            if (_autoElapsed >= AutoMoveTimeout) { FinishAutoMove(false, "超时(沿墙滑行未抵达)"); return; }
        }

        /// <summary>到达判定:像素差换算到逻辑格(/60、/30)后求欧氏距离 ≤ 半径(对标老端 logic 距离 dist=2.5)。</summary>
        private bool ReachedTarget()
        {
            float lx = (_autoTargetX - _posX) / SceneMapData.LogicRatioX;
            float ly = (_autoTargetY - _posY) / SceneMapData.LogicRatioY;
            return lx * lx + ly * ly <= _autoArriveLogic * _autoArriveLogic;
        }

        public bool IsDirectPathBlockedTo(float targetX, float targetY)
        {
            SceneMapData map = SceneMapLoader.Current;
            if (map == null) return false;

            float dx = targetX - _posX;
            float dy = targetY - _posY;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance < 1f) return false;

            int samples = Mathf.Max(1, Mathf.CeilToInt(distance / 20f));
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                if (map.IsBlockPixel(_posX + dx * t, _posY + dy * t)) return true;
            }

            return false;
        }

        // 自动接近收尾:停步(idle + 补发最终坐标)→ 面向 NPC → 触发回调(对标到达 DoStand + SetDirection + SHOW_TASK)。
        private void FinishAutoMove(bool arrived, string reason)
        {
            _autoMoving = false;
            SetAutoFindWayState(false);
            Action cb = _onArrive;
            bool invokeOnFail = _autoInvokeOnFail;
            _onArrive = null;
            _autoInvokeOnFail = true;

            StopMove();
            FaceTowardPixel(_autoTargetX, _autoTargetY);

            if (arrived)
                GameLog.Info("Scene", "MoveToNpc 到达 NPC 附近 → 触发到达回调(开对话)");
            else if (invokeOnFail)
                GameLog.Warn("Scene", "MoveToNpc 未抵达[{0}] → 使用非严格兜底回调(target=({1:F0},{2:F0}))",
                    reason, _autoTargetX, _autoTargetY);
            else
                GameLog.Warn("Scene", "MoveToNpc 未抵达[{0}] → 严格模式不触发到达回调(target=({1:F0},{2:F0}))",
                    reason, _autoTargetX, _autoTargetY);

            if (arrived || invokeOnFail) cb?.Invoke();
        }

        // 玩家手动介入:取消自动接近(丢弃到达回调,让位手动驱动;玩家可重新点任务再次自动接近)。
        private void CancelAutoMove(string why)
        {
            if (!_autoMoving) return;
            _autoMoving = false;
            SetAutoFindWayState(false);
            _onArrive = null;
            _autoInvokeOnFail = true;
            GameLog.Info("Scene", "MoveToNpc 取消:{0}", why);
        }

        /// <summary>主角是否处于采集态(蹲下采集动作进行中)。</summary>
        public bool IsCollecting => _collecting;

        /// <summary>
        /// 进入采集态、循环播放蹲下采集动作(对标老端 Scene.MainRoleDoCollect → Role.DoCollect →
        /// EnterStateCollect 的 PlayAction("collect"))。由 <see cref="CollectController"/> 在服务端回 20008 flag=1
        /// (START)时调用。停下任何在途移动/自动接近;玩家起步会经 <see cref="BeginMoveAnim"/> 自动打断采集。
        /// </summary>
        public void DoCollect()
        {
            _autoMoving = false;
            SetAutoFindWayState(false);
            _onArrive = null;
            _moving = false;
            _collecting = true;
            _ = PlayCollectAsync();
        }

        /// <summary>退出采集态、回到待机(对标老端 Scene.MainRoleQuitCollect → Role.QuitCollect → DoStand)。
        /// 由 <see cref="CollectController"/> 在采集完成/取消时调用;非采集态调用无副作用(不强行打断跑动)。</summary>
        public void QuitCollect()
        {
            if (!_collecting) return;
            _collecting = false;
            _actionVersion++;            // 让在途 PlayCollectAsync 续体过期,避免再覆盖 idle
            if (!_moving) PlayAction(ActionIdle);
        }

        private async Task PlayCollectAsync()
        {
            try
            {
                if (_model == null || (_anim == null && _driver == null)) return;
                int version = ++_actionVersion;
                EffectBinder.ClearTag(_model, "action");
                ResetModelVisualOffset();

                await RoleModelAssembler.PrepareRoleActions(_model, _career, _clotheRes, new[] { ActionCollect });
                if (version != _actionVersion || _model == null || !_collecting) return;

                if (!TryPlayActionLoop(ActionCollect, 0.12f))
                {
                    // 采集动作未转换/缺片段:不影响采集协议链,仅蹲下表现缺失(优雅降级,不报错刷屏)。
                    GameLog.Warn("Scene", "collect 动作缺失或未转换(蹲下表现降级),采集逻辑继续: action={0}", ActionCollect);
                }
            }
            catch (Exception ex)
            {
                GameLog.Warn("Scene", "play collect action failed: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 播主角技能表现。<paramref name="hitMonsterIds"/> = 本次攻击的真实目标怪实例列表(SceneCombat 发包同源,
        /// 首个为主目标),供"受击者/落点"类特效(pos_type 1/3/4/6/12/13)落到怪物身上;null/空 = 无目标(施法者
        /// 特效照播,目标特效跳过,对标老端 PlayParticle 目标循环为空)。
        /// </summary>
        public void PlaySkill(int skillId, IReadOnlyList<int> hitMonsterIds = null)
        {
            _ = PlaySkillAsync(skillId, hitMonsterIds);
        }

        public void PlayMoveAnim(int moveAnim, int targetX, int targetY)
        {
            if (moveAnim <= 0) return;
            _ = PlayMoveAnimAsync(moveAnim, targetX, targetY);
        }

        public void TaskJumpTo(int targetX, int targetY, int jumpType, Action onArrive)
        {
            if (jumpType <= 0)
            {
                MoveToNpcStrict(targetX, targetY, ArrivalLogicDist, onArrive);
                return;
            }
            _ = TaskJumpToAsync(jumpType, targetX, targetY, onArrive);
        }

        private async Task TaskJumpToAsync(int jumpType, int targetX, int targetY, Action onArrive)
        {
            try
            {
                if (_model == null || (_anim == null && _driver == null))
                {
                    MoveToNpcStrict(targetX, targetY, ArrivalLogicDist, onArrive);
                    return;
                }

                await OtherFightConfigs.EnsureLoaded();
                int version = ++_actionVersion;
                _moving = false;
                _autoMoving = false;
                SetAutoFindWayState(true);
                _onArrive = null;
                EffectBinder.ClearTag(_model, "action");
                ResetModelVisualOffset();
                FaceTowardPixel(targetX, targetY);

                int startX = Mathf.RoundToInt(_posX);
                int startY = Mathf.RoundToInt(_posY);

                if (!OtherFightConfigs.TryGetJumpMotion(jumpType, _career,
                    out float hspeed, out float vspeed, out float gravity, out float fallStay))
                {
                    GameLog.Warn("Scene", "task jump motion config missing jumpType={0} career={1}", jumpType, _career);
                    MoveToNpcStrict(targetX, targetY, ArrivalLogicDist, onArrive);
                    return;
                }

                string playedAction = null;
                IReadOnlyList<string> configuredActions = OtherFightConfigs.GetJumpActionList(_career, _sex);
                int actionStart = configuredActions.Count > 0 ? _jumpActionCursor % configuredActions.Count : 0;
                _jumpActionCursor++;
                for (int i = 0; i < configuredActions.Count; i++)
                {
                    string candidate = configuredActions[(actionStart + i) % configuredActions.Count];
                    await RoleModelAssembler.PrepareRoleActions(_model, _career, _clotheRes, new[] { candidate });
                    if (version != _actionVersion || _model == null) return;
                    if (HasActionClip(candidate))
                    {
                        playedAction = candidate;
                        break;
                    }
                }

                if (playedAction == null)
                {
                    GameLog.Warn("Scene", "task jump action config/clip missing jumpType={0} career={1} sex={2}",
                        jumpType, _career, _sex);
                    MoveToNpcStrict(targetX, targetY, ArrivalLogicDist, onArrive);
                    return;
                }

                float movieSpeedOff = OtherFightConfigs.GetTaskJumpMovieSpeedOff(_career, _sex, playedAction);
                SceneController.Instance.SendMoveRequest(startX, startY, MoveType.TaskJump, targetX, targetY, startX, startY);
                List<Vector2Int> jumpTargets = BuildTaskJumpTargets(jumpType, startX, startY, targetX, targetY);
                for (int i = 0; i < jumpTargets.Count; i++)
                {
                    Vector2Int jumpTarget = jumpTargets[i];
                    bool isLastSegment = i == jumpTargets.Count - 1;
                    float segmentFallStay = isLastSegment ? fallStay : 0f;
                    float segmentDuration = GetTaskJumpDuration(vspeed, gravity, segmentFallStay);
                    float actionSpeed = GetActionPlaybackSpeed(playedAction, segmentDuration, movieSpeedOff);
                    TryPlayAction(playedAction, i == 0 ? 0.08f : 0.02f, true, actionSpeed);
                    await RunConfiguredJumpToAsync(jumpType, jumpTarget.x, jumpTarget.y, version, true, startX, startY,
                        hspeed, vspeed, gravity, segmentFallStay, true);
                    if (version != _actionVersion || _model == null) return;
                }

                ResetModelVisualOffset();
                PlayAction(ActionIdle);
                SetAutoFindWayState(false);
                onArrive?.Invoke();
            }
            catch (Exception ex)
            {
                GameLog.Warn("Scene", "task jump failed jumpType={0}: {1}", jumpType, ex.Message);
                ResetModelVisualOffset();
                MoveToNpcStrict(targetX, targetY, ArrivalLogicDist, onArrive);
            }
        }

        private static List<Vector2Int> BuildTaskJumpTargets(int jumpType, int startX, int startY, int targetX, int targetY)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            if (jumpType == 4)
            {
                result.Add(new Vector2Int(Mathf.RoundToInt((startX + targetX) * 0.5f),
                    Mathf.RoundToInt((startY + targetY) * 0.5f)));
            }
            else if (jumpType == 5)
            {
                result.Add(new Vector2Int(Mathf.RoundToInt(startX + (targetX - startX) / 3f),
                    Mathf.RoundToInt(startY + (targetY - startY) / 3f)));
                result.Add(new Vector2Int(Mathf.RoundToInt(startX + (targetX - startX) * 2f / 3f),
                    Mathf.RoundToInt(startY + (targetY - startY) * 2f / 3f)));
            }

            result.Add(new Vector2Int(targetX, targetY));
            return result;
        }

        private async Task PlaySkillAsync(int skillId, IReadOnlyList<int> hitMonsterIds = null)
        {
            try
            {
                if (_model == null || (_anim == null && _driver == null)) return;
                await SkillMovieConfigs.EnsureLoaded();

                string actionName = SkillMovieConfigs.GetActionName(skillId);
                IReadOnlyList<SkillMovieParticle> particles = SkillMovieConfigs.GetParticles(skillId);
                if (string.IsNullOrEmpty(actionName) && (particles == null || particles.Count == 0))
                {
                    GameLog.Warn("Scene", "skill movie missing skill={0}", skillId);
                    return;
                }

                int version = ++_actionVersion;
                _moving = false;
                _autoMoving = false;
                SetAutoFindWayState(false);
                EffectBinder.ClearTag(_model, "action");

                GameObject actionEffectHost = _model;

                if (!string.IsNullOrEmpty(actionName))
                {
                    await RoleModelAssembler.PrepareRoleActions(_model, _career, _clotheRes, new[] { actionName });
                    if (version != _actionVersion || _model == null) return;
                    if (!await TryPlayActionAsync(actionName, 0.08f, true))
                    {
                        GameLog.Warn("Scene", "skill action missing skill={0} action={1}", skillId, actionName);
                    }
                    if (version != _actionVersion || _model == null) return;

                    // 混合角色的 idle/run 与 attack/skill 可能是不同的子模型。必须等动作切换完成后，
                    // 再把刀光挂到本次攻击真正显示的模型；从容器根递归找 root 会优先命中隐藏的 idle。
                    actionEffectHost = GetActiveActionEffectHost();
                    await EffectBinder.AttachAction(actionEffectHost, "role", _clotheRes.ToString(), actionName);
                }

                PlaySkillParticles(skillId, particles, version, hitMonsterIds, actionEffectHost);

                float wait = Mathf.Max(GetActionLength(actionName), SkillMovieConfigs.GetConfiguredDurationSeconds(skillId));
                if (wait > 0f)
                {
                    await TimeUtil.Delay(Mathf.RoundToInt(wait * 1000f));
                    if (version == _actionVersion && !_moving && _model != null) PlayAction(ActionIdle);
                }
            }
            catch (Exception ex)
            {
                GameLog.Warn("Scene", "play skill visual failed skill={0}: {1}", skillId, ex.Message);
            }
        }

        private async Task PlayMoveAnimAsync(int moveAnim, int targetX, int targetY)
        {
            try
            {
                if (_model == null || (_anim == null && _driver == null)) return;
                await OtherFightConfigs.EnsureLoaded();
                int version = ++_actionVersion;
                _moving = false;
                _autoMoving = false;
                SetAutoFindWayState(false);
                EffectBinder.ClearTag(_model, "action");
                ResetModelVisualOffset();

                string playedAction = null;
                foreach (string candidate in MoveAnimCandidates(moveAnim))
                {
                    await RoleModelAssembler.PrepareRoleActions(_model, _career, _clotheRes, new[] { candidate });
                    if (version != _actionVersion || _model == null) return;
                    if (TryPlayAction(candidate, 0.08f, true))
                    {
                        playedAction = candidate;
                        break;
                    }
                }

                if (playedAction == null)
                {
                    GameLog.Warn("Scene", "move_anim action missing moveAnim={0}", moveAnim);
                    ApplyServerPosition(targetX, targetY);
                    return;
                }

                if (targetX > 0 || targetY > 0)
                {
                    await RunConfiguredJumpToAsync(moveAnim, targetX, targetY, version);
                }
                else
                {
                    float duration = GetActionLength(playedAction);
                    if (duration > 0f) await TimeUtil.Delay(Mathf.RoundToInt(duration * 1000f));
                }

                if (version == _actionVersion && !_moving && _model != null)
                {
                    ResetModelVisualOffset();
                    PlayAction(ActionIdle);
                }
            }
            catch (Exception ex)
            {
                GameLog.Warn("Scene", "play move_anim failed moveAnim={0}: {1}", moveAnim, ex.Message);
                ResetModelVisualOffset();
            }
        }

        private void PlaySkillParticles(int skillId, IReadOnlyList<SkillMovieParticle> particles, int version,
            IReadOnlyList<int> hitMonsterIds, GameObject actionEffectHost)
        {
            if (particles == null || particles.Count == 0) return;
            for (int i = 0; i < particles.Count; i++)
            {
                SkillMovieParticle particle = particles[i];
                if (particle == null || string.IsNullOrEmpty(particle.Res)) continue;
                _ = PlaySkillParticleAsync(skillId, particle, version, hitMonsterIds, actionEffectHost);
            }
        }

        /// <summary>
        /// 按 pos_type 路由单条技能特效(对标老端 FightMovieInfo.PlayParticle:627-719 分支;此前全部糊在
        /// 施法者身上 → 职业3/4 的命中/落点特效叠在自己脚下,视觉全乱):
        ///   · 0(攻击者挂骨骼)/2(攻击者坐标)→ 挂主角模型(pos2 老端定格世界坐标,本端挂模型会短暂跟随,
        ///     特效寿命 ≤3s 影响很小,记录为已知近似);
        ///   · 1/3/12(每个受击者身上/坐标/中心)→ 逐个目标怪挂 <see cref="MonsterRenderer.PlayHitParticle"/>;
        ///   · 4(受击者中心)/6(攻击点)/13(有目标同3)→ 主目标怪(攻击点=主目标坐标,与 20001 发包同源);
        ///     13 无目标回落攻击者(对标老端 13 的 else→2 分支)。
        ///   · 其余(5 等)老端 default 不播 → 跳过。
        /// dir_type(特效朝向)未接,记录为后续。
        /// </summary>
        private async Task PlaySkillParticleAsync(int skillId, SkillMovieParticle particle, int version,
            IReadOnlyList<int> hitMonsterIds, GameObject actionEffectHost)
        {
            try
            {
                if (particle.StartTime > 0f)
                    await TimeUtil.Delay(Mathf.RoundToInt(particle.StartTime * 1000f));
                if (version != _actionVersion || _model == null) return;

                bool hasTargets = hitMonsterIds != null && hitMonsterIds.Count > 0;
                switch (particle.PosType)
                {
                    case 1:
                    case 3:
                    case 12:
                        if (!hasTargets) return; // 无目标:老端目标循环为空,自然不播
                        for (int i = 0; i < hitMonsterIds.Count; i++)
                            MonsterRenderer.PlayHitParticle(hitMonsterIds[i], particle);
                        return;
                    case 4:
                    case 6:
                        if (!hasTargets) return;
                        MonsterRenderer.PlayHitParticle(hitMonsterIds[0], particle); // 中心/攻击点=主目标(发包同源)
                        return;
                    case 13:
                        if (hasTargets) { MonsterRenderer.PlayHitParticle(hitMonsterIds[0], particle); return; }
                        break; // 无目标 → 老端回落 pos2(攻击者),继续走下面
                    case 5:
                        return; // 老端 default 分支不播
                }

                GameObject host = actionEffectHost != null ? actionEffectHost : GetActiveActionEffectHost();
                GameObject effect = await EffectBinder.AttachOne(host, "root", "skills_effect", particle.Res, "action", false);
                if (effect == null) return;
                if (particle.Scale > 0f && Mathf.Abs(particle.Scale - 1f) > 0.001f)
                    effect.transform.localScale = Vector3.one * particle.Scale;

                if (particle.PlayTimeLen > 0f)
                {
                    EffectBinder.PlayEffect(effect);
                    UnityEngine.Object.Destroy(effect, particle.PlayTimeLen);
                }
                else
                {
                    EffectBinder.PlayOneShot(effect);
                }
            }
            catch (Exception ex)
            {
                GameLog.Warn("Scene", "play skill particle failed skill={0} res={1}: {2}", skillId, particle.Res, ex.Message);
            }
        }

        public void PlayLevelUpEffect()
        {
            _ = PlayLevelUpEffectAsync();
        }

        private async Task PlayLevelUpEffectAsync()
        {
            if (_model == null) return;
            if (_levelUpEffectCount >= 2) return;

            _levelUpEffectCount++;
            try
            {
                string key = GameResPath.GetEffectPrefabPath("other_effect", "effect_xemlvup");
                GameObject prefab = await ResManager.LoadAsync<GameObject>(key);
                Transform host = GetDetachedEffectHost();
                if (prefab == null || host == null)
                {
                    GameLog.Warn("Scene", "level up effect missing or host destroyed: {0}", key);
                    ResManager.Release(prefab); // 宿主没了也归还这次引用(prefab==null 时为空操作)
                    return;
                }

                GameObject effect = UnityEngine.Object.Instantiate(prefab, host);
                LoadedAssetReleaser.Track(effect, prefab);
                effect.name = "__fx_levelup_effect_xemlvup";
                effect.transform.localPosition = Vector3.zero;
                effect.transform.localRotation = Quaternion.identity;
                effect.transform.localScale = Vector3.one;
                if (effect != null) EffectBinder.PlayOneShot(effect);
                await TimeUtil.Delay(1000);
            }
            catch (Exception ex)
            {
                GameLog.Warn("Scene", "play level up effect failed: {0}", ex.Message);
            }
            finally
            {
                _levelUpEffectCount = Mathf.Max(0, _levelUpEffectCount - 1);
            }
        }

        private Transform GetDetachedEffectHost()
        {
            if (_modelTr != null && _modelTr.parent != null) return _modelTr.parent;
            if (_modelTr != null) return _modelTr;
            return transform;
        }

        private async Task RunConfiguredJumpToAsync(int moveAnim, int targetX, int targetY, int version,
            bool reportTaskJump = false, int taskStartX = 0, int taskStartY = 0,
            float configuredHSpeed = 0f, float configuredVSpeed = 0f, float configuredGravity = 0f,
            float configuredFallStay = -1f, bool useTaskJumpDuration = false)
        {
            float startX = _posX;
            float startY = _posY;
            float dx = targetX - startX;
            float dy = targetY - startY;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            int jumpType = NormalizeJumpType(moveAnim);
            float hspeed = configuredHSpeed > 0f ? configuredHSpeed : OtherFightConfigs.GetJumpHSpeed(jumpType, 900f);
            float vspeed = configuredVSpeed > 0f ? configuredVSpeed : OtherFightConfigs.GetJumpVSpeed(jumpType, 1200f);
            float gravity = configuredGravity > 0f ? configuredGravity : OtherFightConfigs.GetJumpGravitySpeed(jumpType, 2500f);
            float moveTime = useTaskJumpDuration
                ? GetTaskJumpMoveTime(vspeed, gravity)
                : (hspeed > 0f ? distance / hspeed : GetActionLength(ActionJump));
            moveTime = Mathf.Max(0.05f, moveTime);

            float upTime = gravity > 0f ? vspeed / gravity : 0f;
            float maxHeight = gravity > 0f ? 0.5f * gravity * upTime * upTime : 0f;

            float elapsed = 0f;
            float pendingReportDistance = 0f;
            float lastFrameX = startX;
            float lastFrameY = startY;
            while (elapsed < moveTime)
            {
                await Task.Yield();
                if (version != _actionVersion || _model == null) return;

                elapsed += Mathf.Max(Time.deltaTime, 0.016f);
                float t = Mathf.Clamp01(elapsed / moveTime);
                int x = Mathf.RoundToInt(Mathf.Lerp(startX, targetX, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(startY, targetY, t));
                ApplyServerPosition(x, y);
                ApplyJumpVisualHeight(4f * maxHeight * t * (1f - t));

                if (reportTaskJump)
                {
                    float stepX = x - lastFrameX;
                    float stepY = y - lastFrameY;
                    pendingReportDistance += Mathf.Sqrt(stepX * stepX + stepY * stepY);
                    lastFrameX = x;
                    lastFrameY = y;
                    if (pendingReportDistance >= TaskJumpReportDistance)
                    {
                        SceneController.Instance.SendMoveRequest(x, y, MoveType.TaskJumpUpdatePos,
                            targetX, targetY, taskStartX, taskStartY);
                        pendingReportDistance = 0f;
                    }
                }
            }

            ApplyServerPosition(targetX, targetY);
            if (reportTaskJump)
            {
                SceneController.Instance.SendMoveRequest(targetX, targetY, MoveType.TaskJumpUpdatePos,
                    targetX, targetY, taskStartX, taskStartY);
            }
            ResetModelVisualOffset();

            float stay = configuredFallStay >= 0f ? configuredFallStay : OtherFightConfigs.GetJumpFallStayTime(jumpType, _career, 0.1f);
            if (stay > 0f) await TimeUtil.Delay(Mathf.RoundToInt(stay * 1000f));
        }

        private static float GetTaskJumpDuration(float vspeed, float gravity, float fallStay)
        {
            return GetTaskJumpMoveTime(vspeed, gravity) + Mathf.Max(0f, fallStay);
        }

        private static float GetTaskJumpMoveTime(float vspeed, float gravity)
        {
            if (vspeed <= 0f || gravity <= 0f) return 0.05f;
            float upTime = vspeed / gravity;
            float maxHeight = 0.5f * gravity * upTime * upTime;
            float downTime = Mathf.Sqrt(2f * maxHeight / gravity);
            return Mathf.Max(0.05f, upTime + downTime);
        }

        private float GetActionPlaybackSpeed(string action, float duration, float movieSpeedOff)
        {
            float clipLength = GetActionLength(action);
            float ratio = Mathf.Max(0.01f, 1f + movieSpeedOff);
            if (clipLength <= 0f || duration <= 0f) return ratio;
            return Mathf.Max(0.01f, clipLength / duration * ratio);
        }

        private IEnumerable<string> MoveAnimCandidates(int moveAnim)
        {
            IReadOnlyList<string> configured = OtherFightConfigs.GetJumpActionList(_career, _sex);
            if (configured.Count > 0)
            {
                int start = _jumpActionCursor % configured.Count;
                _jumpActionCursor++;
                for (int i = 0; i < configured.Count; i++)
                    yield return configured[(start + i) % configured.Count];
            }

        }

        private static int NormalizeJumpType(int moveAnim)
        {
            return moveAnim >= 1 && moveAnim <= 5 ? moveAnim : 1;
        }

        private void ApplyJumpVisualHeight(float heightPixels)
        {
            if (_modelTr == null) return;
            _modelTr.localPosition = _modelBaseLocalPos + new Vector3(0f, Mathf.Max(0f, heightPixels) / 100f, 0f);
        }

        private void ResetModelVisualOffset()
        {
            if (_modelTr != null) _modelTr.localPosition = _modelBaseLocalPos;
        }

        private void ApplyServerPosition(int targetX, int targetY)
        {
            if (targetX <= 0 && targetY <= 0) return;
            RoleModel role = RoleModel.Instance;
            if (targetX > 0)
            {
                _posX = targetX;
                role.X = targetX;
            }
            if (targetY > 0)
            {
                _posY = targetY;
                role.Y = targetY;
            }
            SceneMapView.SetFocus(role.X, role.Y);
            SyncModelScreenOffset();
        }

        private float GetActionLength(string action)
        {
            if (string.IsNullOrEmpty(action)) return 0f;
            if (_driver != null) return _driver.GetLength(action); // 新实例首播前=0,外层节拍自会兜底
            if (_anim == null) return 0f;
            AnimationClip clip = _anim.GetClip(action);
            return clip != null ? clip.length : 0f;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                SetAutoFindWayState(false);
                Current = null;
            }
            _model = null;
        }

        private static void SetAutoFindWayState(bool active)
        {
            AutoFightModel.Instance.SetAutoFindWay(active);
        }

        private void PlayAction(string action)
        {
            TryPlayAction(action, 0.15f, false);
        }

        /// <summary>播放主角死亡动作(对标老端 MainRole.DoDead → Character.EnterStateDead → PlayAction("death"))。
        /// ReliveController 收到 EVT_ROLE_DEAD 时调用;没有 death 动作素材时静默跳过(TryPlayAction 门禁一致),
        /// 返回 false 供调用方按需 log TODO,不硬造动画。</summary>
        public bool PlayDeadAnim()
        {
            return TryPlayAction(ActionDeath, 0.15f, true);
        }

        /// <summary>复活成功后恢复待机(对标老端 Character.Revived → DoStand)。</summary>
        public void PlayReviveIdle()
        {
            PlayAction(ActionIdle);
        }

        private bool HasActionClip(string action)
        {
            if (string.IsNullOrEmpty(action)) return false;
            if (_driver != null) return _driver.CanPlay(action); // 混合模型:老件未建时先放行,真播缺再静默跳过
            return _anim != null && _anim.GetClip(action) != null;
        }

        private bool TryPlayAction(string action, float fade, bool restart, float speed = 1f)
        {
            if (string.IsNullOrEmpty(action)) return false;
            if (_driver != null) return _driver.Play(action, restart, speed); // 混合模型:按清单新老互切
            if (_anim == null) return false;
            if (_anim.GetClip(action) == null) return false; // 未转换的动作静默跳过,不影响移动
            if (!restart && _anim.IsPlaying(action)) return true;
            if (restart) _anim.Stop(action);
            AnimationState state = _anim[action];
            if (state != null) state.speed = Mathf.Max(0.01f, speed);
            _anim.CrossFade(action, fade);
            return true;
        }

        /// <summary>
        /// 技能专用的可等待动作切换。普通移动允许异步切换并保持上一帧，但技能特效必须等混合模型
        /// 明确切到 attack/skill 实例后再挂载，否则同名 root 会落到隐藏的待机实例。
        /// </summary>
        private async Task<bool> TryPlayActionAsync(string action, float fade, bool restart, float speed = 1f)
        {
            if (string.IsNullOrEmpty(action)) return false;
            if (_driver == null) return TryPlayAction(action, fade, restart, speed);
            if (!_driver.CanPlay(action)) return false;

            await _driver.PlayAsync(action, restart, speed);
            return _driver != null && _driver.ActiveModel != null;
        }

        private GameObject GetActiveActionEffectHost()
        {
            if (_driver != null && _driver.ActiveModel != null) return _driver.ActiveModel;
            return _model;
        }

        /// <summary>循环播放动作(采集等需持续到外部停止的动作)。设 WrapMode.Loop 后 CrossFade。</summary>
        private bool TryPlayActionLoop(string action, float fade)
        {
            if (string.IsNullOrEmpty(action)) return false;
            if (_driver != null) return _driver.Play(action, restart: false, speed: 1f, forceLoop: true);
            if (_anim == null) return false;
            if (_anim.GetClip(action) == null) return false;
            AnimationState state = _anim[action];
            if (state != null) state.wrapMode = WrapMode.Loop;
            _anim.CrossFade(action, fade);
            return true;
        }
    }
}
