using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录背景层。阶段切换对标老客户端 LoginBgView.ts:
    ///   ChangeRoleStatus(进创角/选角):隐藏版权 + 背景换 ui_Login_bg2(樱花底);
    ///   UpdateView(回登录/踏入仙界):恢复版权 + denglu_bg(龙图)。
    /// 樱花树/石台/角色立绘是 3D 展示链(SetRoleModel),归 .lh 转换线。
    /// </summary>
    public sealed class LoginBgView : LoginBgViewBind
    {
        private const string BG_LOGIN = "resource/game/scene/dragonBones/denglu/denglu_bg.jpg";
        private const string BG_ROLE = "resource/game/login/other/ui_Login_bg2.jpg";

        /// <summary>进入创角/选角阶段(对标 ChangeRoleStatus)。</summary>
        public void ChangeRoleStatus()
        {
            _lb_copy_right.gameObject.SetActive(false);
            _ = ResManager.SetImageAsync(_img_bg, BG_ROLE, true);
        }

        /// <summary>回到登录/踏入仙界阶段(对标 UpdateView 的背景与版权恢复)。</summary>
        public void RestoreLoginStatus()
        {
            _lb_copy_right.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(_img_bg, BG_LOGIN, true);
        }
    }
}
