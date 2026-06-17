using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景输入状态(对标老客户端 SceneManager 的 curr_click_start_pos / curr_click_move_dist /
    /// curr_joystick_dir,见 Scene.ts 的 MOUSE_DOWN/MOVE/UP 处理)。
    ///
    /// 约定:<see cref="Dir"/> 用"舞台坐标系"——x 向右、y 向下(与老客户端 Laya 舞台、地图像素一致),
    /// 已归一化;不在按压或落在死区内时为零。UIJoyStick 读它画摇杆,MainRoleAgent 读它驱动移动。
    /// 由 <see cref="SceneInputDriver"/> 每帧写入,二者解耦不走事件(每帧轮询即可)。
    /// </summary>
    public static class SceneInput
    {
        /// <summary>移动死区(像素):拖动距离小于此值视为原地点按,不产生方向(防抖)。</summary>
        public const float MoveDeadZone = 12f;

        /// <summary>当前是否正在场景里按住(落在 UI 按钮上的按压不计入)。</summary>
        public static bool Active { get; private set; }

        /// <summary>按下点(Unity 屏幕坐标,y 向上)。摇杆底盘跟手定位用。</summary>
        public static Vector2 StartScreen { get; private set; }

        /// <summary>当前点(Unity 屏幕坐标,y 向上)。</summary>
        public static Vector2 CurrentScreen { get; private set; }

        /// <summary>归一化方向(舞台坐标:x 右、y 下);未超出死区时为零。</summary>
        public static Vector2 Dir { get; private set; }

        /// <summary>拖动距离(像素,未截断)。摇杆头偏移按它截断到最大半径。</summary>
        public static float MoveDist { get; private set; }

        /// <summary>有有效移动方向(超出死区)。</summary>
        public static bool HasDirection => Dir.sqrMagnitude > 0f;

        public static void Begin(Vector2 screen)
        {
            Active = true;
            StartScreen = screen;
            CurrentScreen = screen;
            Dir = Vector2.zero;
            MoveDist = 0f;
        }

        public static void Move(Vector2 screen)
        {
            if (!Active) return;
            CurrentScreen = screen;

            // 屏幕增量翻 y 得到舞台增量(屏幕 y 向上 → 舞台 y 向下);对标 Scene.ts:488-490。
            Vector2 stageDelta = new Vector2(screen.x - StartScreen.x, StartScreen.y - screen.y);
            float dist = stageDelta.magnitude;
            MoveDist = dist;
            Dir = dist >= MoveDeadZone ? stageDelta / dist : Vector2.zero;
        }

        public static void End()
        {
            Active = false;
            Dir = Vector2.zero;
            MoveDist = 0f;
        }
    }
}
