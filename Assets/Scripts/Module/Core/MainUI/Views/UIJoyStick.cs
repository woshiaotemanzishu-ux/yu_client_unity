using Shenxiao.Generated.UI.MainUI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 摇杆视图。对标老客户端 UIJoyStick.ts:视图对象常驻并保持事件绑定(InitMainUI 里
    /// new+Open,UIJoyStick.ts:700-703),但摇杆图默认不显示——移动输入出现后才由
    /// ShowJoyStick/HideJoyStick 按 SceneManager.curr_joystick_dir 切换(:74-85)。
    ///
    /// 这里只隐藏可见的摇杆图根 _gp_root(内含底图/箭头/中心圆,见 UIJoyStick.json),
    /// 不关整个 GameObject——后者会连带切掉对象的事件接线
    /// (见 Docs/Shenxiao进游戏链路.md 2.3 的明确约束)。
    /// </summary>
    public sealed class UIJoyStick : UIJoyStickBind
    {
        protected override void OnInit()
        {
            // 摇杆图默认隐藏(移动输入前不显示);视图本身由 MainUIFlow 正常 Show 并常驻。
            _gp_root.gameObject.SetActive(false);
        }
    }
}
