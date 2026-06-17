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

        private const string ActionIdle = "idle";
        private const string ActionRun = "run";

        private Transform _modelTr;     // 模型子节点(用于转向)
        private Animation _anim;        // RoleModelAssembler 在模型根挂的 Animation
        private float _baseModelYaw;    // 模型初始朝向(场景倾斜下的默认 yaw)

        private float _posX;            // 真实像素 X(real_pos.x)
        private float _posY;            // 真实像素 Y(real_pos.y)
        private float _sendTimer;
        private bool _moving;

        /// <summary>由 MainRoleFlow 在装配完成后初始化:传入模型子节点与出生坐标。</summary>
        public void Init(GameObject model, int spawnX, int spawnY)
        {
            _modelTr = model != null ? model.transform : transform;
            _anim = model != null ? model.GetComponent<Animation>() : null;
            _baseModelYaw = _modelTr.localEulerAngles.y;
            _posX = spawnX;
            _posY = spawnY;
            _moving = false;
            _sendTimer = 0f;
            PlayAction(ActionIdle);
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

        /// <summary>朝向:按水平分量左右翻面(2.5D 表现下最稳的近似;3D 自由转向待真机调）。</summary>
        private void Face(Vector2 dir)
        {
            if (_modelTr == null || Mathf.Abs(dir.x) < 0.2f) return;
            float yaw = dir.x >= 0f ? _baseModelYaw : _baseModelYaw + 180f;
            Vector3 e = _modelTr.localEulerAngles;
            _modelTr.localEulerAngles = new Vector3(e.x, yaw, e.z);
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
