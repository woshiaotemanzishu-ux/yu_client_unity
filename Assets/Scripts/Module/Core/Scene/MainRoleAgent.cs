using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Scene3D.Map;
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

        private const string ActionIdle = "idle";
        private const string ActionRun = "run";

        private Transform _modelTr;     // 模型子节点(用于转向)
        private Animation _anim;        // RoleModelAssembler 在模型根挂的 Animation

        private float _posX;            // 真实像素 X(real_pos.x)
        private float _posY;            // 真实像素 Y(real_pos.y)
        private float _sendTimer;
        private bool _moving;

        /// <summary>由 MainRoleFlow 在装配完成后初始化:传入模型子节点与出生坐标。</summary>
        public void Init(GameObject model, int spawnX, int spawnY)
        {
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

            if (SceneInput.Active && SceneInput.HasDirection)
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
            float mx = dir.x * moveDist;
            float my = dir.y * moveDist;

            // 撞墙分轴滑动:整向 → 仅 X → 仅 Y(对标 MainRole.ts:794-819)
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

            if (!_moving)
            {
                _moving = true;
                PlayAction(ActionRun);
                _sendTimer = SendInterval; // 起步立即上报一次
            }

            Face(dir);
            SceneMapView.SetFocus(role.X, role.Y);
            SyncModelScreenOffset(); // 焦点(相机)已更新,随即把模型摆到 (role - camera) 的屏幕偏移上

            if (moved)
            {
                _sendTimer += dt;
                if (_sendTimer >= SendInterval)
                {
                    _sendTimer = 0f;
                    SceneController.Instance.SendMoveRequest(role.X, role.Y, MoveTypeNormal, role.X, role.Y);
                }
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

        private void PlayAction(string action)
        {
            if (_anim == null || string.IsNullOrEmpty(action)) return;
            if (_anim.GetClip(action) == null) return; // 未转换的动作静默跳过,不影响移动
            if (_anim.IsPlaying(action)) return;
            _anim.CrossFade(action, 0.15f);
        }
    }
}
