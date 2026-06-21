using System;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
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
        private const float TurnSmoothSpeed = 720f; // 转向角速度(度/秒);<=0 则瞬时转向

        // —— 自动接近 NPC(对标 Scene.MainRoleToNpc → MainRoleMove)——
        private const float ArrivalLogicDist = 2.5f; // 到达判定半径(逻辑格,老端 dist=2.5)
        private const float AutoMoveTimeout = 8f;    // 直线接近兜底超时(无 A* 绕障:到不了也要把对话开出来)
        private const float AutoStuckSeconds = 0.6f; // 连续无位移进展达此时长 → 判定卡死兜底
        private const float AutoStuckEpsilon = 0.5f; // 单帧像素位移进展阈值(< 此值视为无进展)

        private const string ActionIdle = "idle";
        private const string ActionRun = "run";

        /// <summary>当前主角驱动(MainRoleFlow 装配后唯一存在;清主角时置空)。任务/对话用它让主角朝 NPC 转向。</summary>
        public static MainRoleAgent Current { get; private set; }

        private Transform _modelTr;     // 模型子节点(用于转向)
        private Animation _anim;        // RoleModelAssembler 在模型根挂的 Animation

        private float _posX;            // 真实像素 X(real_pos.x)
        private float _posY;            // 真实像素 Y(real_pos.y)
        private float _sendTimer;
        private bool _moving;

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

        /// <summary>由 MainRoleFlow 在装配完成后初始化:传入模型子节点与出生坐标。</summary>
        public void Init(GameObject model, int spawnX, int spawnY)
        {
            Current = this;
            _modelTr = model != null ? model.transform : transform;
            _anim = model != null ? model.GetComponent<Animation>() : null;
            _posX = spawnX;
            _posY = spawnY;
            _moving = false;
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

        private void StepMove(SceneMapData map)
        {
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
            if (_moving) return;
            _moving = true;
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
            PlayAction(ActionIdle);
            // 对标 MainRole.QuitStateMove:松手补发一次最终坐标
            RoleModel role = RoleModel.Instance;
            SceneController.Instance.SendMoveRequest(role.X, role.Y, MoveTypeNormal, role.X, role.Y);
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
            _autoTargetX = targetX;
            _autoTargetY = targetY;
            _autoArriveLogic = arriveLogicDist > 0f ? arriveLogicDist : ArrivalLogicDist;

            // 已在范围内:不移动,直接转身 + 触发(对标 MainRoleToNpc 的 GetDistance<=dist+1 早退分支)。
            if (ReachedTarget())
            {
                _autoMoving = false;
                _onArrive = null;
                FaceTowardPixel(targetX, targetY);
                GameLog.Info("Scene", "MoveToNpc: 主角已在 NPC 附近,直接触发到达回调(开对话)");
                onArrive?.Invoke();
                return;
            }

            _onArrive = onArrive;
            _autoMoving = true;
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

        // 自动接近收尾:停步(idle + 补发最终坐标)→ 面向 NPC → 触发回调(对标到达 DoStand + SetDirection + SHOW_TASK)。
        private void FinishAutoMove(bool arrived, string reason)
        {
            _autoMoving = false;
            Action cb = _onArrive;
            _onArrive = null;

            StopMove();
            FaceTowardPixel(_autoTargetX, _autoTargetY);

            if (arrived)
                GameLog.Info("Scene", "MoveToNpc 到达 NPC 附近 → 触发到达回调(开对话)");
            else
                GameLog.Warn("Scene", "MoveToNpc 未抵达[{0}] → 直线接近无 A* 寻路,仍触发回调避免软锁(target=({1:F0},{2:F0}))",
                    reason, _autoTargetX, _autoTargetY);

            cb?.Invoke();
        }

        // 玩家手动介入:取消自动接近(丢弃到达回调,让位手动驱动;玩家可重新点任务再次自动接近)。
        private void CancelAutoMove(string why)
        {
            if (!_autoMoving) return;
            _autoMoving = false;
            _onArrive = null;
            GameLog.Info("Scene", "MoveToNpc 取消:{0}", why);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void PlayAction(string action)
        {
            if (_anim == null || string.IsNullOrEmpty(action)) return;
            if (_anim.GetClip(action) == null) return; // 未转换的动作静默跳过,不影响移动
            if (_anim.IsPlaying(action)) return;
            _anim.CrossFade(action, 0.15f);
        }
    }
}
