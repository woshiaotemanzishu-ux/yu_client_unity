using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Scene;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 摇杆视图。对标老客户端 UIJoyStick.ts:视图对象常驻并保持事件绑定(InitMainUI 里 new+Open),
    /// 但摇杆图默认不显示——按下场景空白处才显示并跟手(老客户端靠 SceneManager.curr_joystick_dir +
    /// ShowJoyStick/HideJoyStick 切换,见 MainUIController.ts:486-498)。
    ///
    /// 这里直接读 <see cref="SceneInput"/>(SceneInputDriver 写入)驱动显隐与构图:
    /// - 只隐藏可见的摇杆图根 _gp_root,不关整个 GameObject(后者会切掉对象事件接线,见进游戏链路 2.3);
    /// - 底盘 _gp_root 跟手定位到按下点,摇杆头 _img_middle_circle 沿方向偏移并截断到最大半径,
    ///   旋转量 1:1 端口 UIJoyStick.ts:48-67(Laya 顺时针角度→Unity 逆时针取负,真机再核朝向)。
    /// </summary>
    public sealed class UIJoyStick : UIJoyStickBind
    {
        private const float MaxRadius = 98f - 44.5f; // 53.5,对标 UIJoyStick.ts:48
        private const float KnobAngleBase = -50f;    // 对标 UIJoyStick.ts 的旋转基准 -50

        private RectTransform _rootParent;
        private Canvas _canvas;
        private Vector2 _knobRest;

        protected override void OnInit()
        {
            _rootParent = _gp_root.parent as RectTransform;
            _knobRest = _img_middle_circle.rectTransform.anchoredPosition;
            // 摇杆图默认隐藏(移动输入前不显示);视图本身由 MainUIFlow 正常 Show 并常驻。
            _gp_root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsShown) return;

            if (SceneInput.Active)
            {
                if (!_gp_root.gameObject.activeSelf) _gp_root.gameObject.SetActive(true);
                UpdateVisual();
            }
            else if (_gp_root.gameObject.activeSelf)
            {
                _gp_root.gameObject.SetActive(false);
            }
        }

        private void UpdateVisual()
        {
            // 底盘跟手:把 _gp_root 移到按下点(屏幕坐标→父节点局部坐标)。对标 UpdatePos。
            if (_rootParent != null)
            {
                if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
                Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _canvas.worldCamera
                    : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _rootParent, SceneInput.StartScreen, cam, out Vector2 startLocal))
                {
                    _gp_root.anchoredPosition = startLocal;
                }
            }

            // 摇杆头偏移:dir 为舞台空间(x 右、y 下),UI 局部 y 向上需翻转 y;距离截断到最大半径。
            Vector2 dir = SceneInput.Dir;
            float dist = Mathf.Min(MaxRadius, SceneInput.MoveDist);
            _img_middle_circle.rectTransform.anchoredPosition =
                _knobRest + new Vector2(dir.x * dist, -dir.y * dist);

            if (dir.sqrMagnitude > 0f)
            {
                _img_middle_circle.rectTransform.localEulerAngles =
                    new Vector3(0f, 0f, -LayaKnobRotation(dir));
            }
        }

        /// <summary>1:1 端口 UIJoyStick.ts:51-67(入参 dir 为舞台坐标 x 右、y 下)。</summary>
        private static float LayaKnobRotation(Vector2 dir)
        {
            float radian = Mathf.Abs(Mathf.Atan(dir.y / dir.x)) * Mathf.Rad2Deg;
            if (dir.y <= 0f && dir.x > 0f) return KnobAngleBase - radian;
            if (dir.y <= 0f && dir.x < 0f) return KnobAngleBase - 180f + radian;
            if (dir.y >= 0f && dir.x < 0f) return KnobAngleBase - 180f - radian;
            return KnobAngleBase + radian; // dir.y >= 0 && dir.x > 0
        }
    }
}
