using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录背景层(对标老客户端 LoginBgView.ts)。data-only:
    ///
    /// 结构(全屏背景 _img_bg / 版本号 _lb_version / 防沉迷 _lb_down / 版权 _lb_copy_right)
    /// 已由快照烤进 prefab,这里【只绑数据】——不运行时 new 节点、不摆位置
    /// (老端的 ApplyLoginBgSize/anchor/pivot/width/height 这些场景布局已烤进 prefab,删)。
    ///
    /// 阶段切换对标 TS:
    ///   ChangeRoleStatus(进创角/选角):隐藏版权 + 背景换 ui_Login_bg2(樱花底);
    ///   RestoreLoginStatus(回登录/踏入仙界):恢复版权 + denglu_bg(龙图)。
    /// 这两个入口由 LoginFlow 直接调用(对标 TS 里 LoginManager OPEN_VIEW 事件分流)。
    ///
    /// 真·运行时的东西(begin_login 网络流程 / 预热资源 / 樱花树石台 3D 立绘 / 背景缓动)
    /// 不在本视图:网络流程已收口到 LoginFlow,3D 展示链归 .lh 转换线。本视图只管换图与版权。
    /// </summary>
    public sealed class LoginBgView : LoginBgViewBind
    {
        // 老端 UpdateView 默认底图(GetLoginBgPath 在无平台 login_bg 时返回它);ChangeRoleStatus 用樱花底。
        private const string BG_LOGIN = "resource/game/scene/dragonBones/denglu/denglu_bg.jpg";
        private const string BG_ROLE = "resource/game/login/other/ui_Login_bg2.jpg";

        protected override void OnInit()
        {
            // 老端 InitEvent:点背景触发 SDK 登录回调(click_bg_can_login_sdk)。
            // SDK 登录是平台真·运行时,本端无对应平台桥;先把点击命中体接好,占位日志。
            Image bg = BgImage();
            if (bg != null) UIUtil.AddClick(bg, OnClickBg);
        }

        protected override void OnShow(object args)
        {
            // 默认进入即登录阶段:龙图 + 版权可见(对标 UpdateView + SetCopyRight)。
            RestoreLoginStatus();
        }

        /// <summary>进入创角/选角阶段(对标 TS ChangeRoleStatus):隐藏版权,背景换樱花底。</summary>
        public void ChangeRoleStatus()
        {
            if (_lb_copy_right != null) _lb_copy_right.gameObject.SetActive(false);
            Image bg = BgImage();
            if (bg != null) _ = ResManager.SetImageAsync(bg, BG_ROLE, coverScreen: true);
        }

        /// <summary>回到登录/踏入仙界阶段(对标 TS UpdateView 背景 + SetCopyRight 版权恢复)。</summary>
        public void RestoreLoginStatus()
        {
            if (_lb_copy_right != null) _lb_copy_right.gameObject.SetActive(true);
            Image bg = BgImage();
            if (bg != null) _ = ResManager.SetImageAsync(bg, BG_LOGIN, coverScreen: true);
        }

        /// <summary>
        /// 点背景:老端走平台 SDK 登录回调(LoginBySdkCallbak)。本端无平台 SDK 桥,留占位。
        /// 接好命中体后,平台线接 SDK 时把回调填进来即可(不改本视图结构)。
        /// </summary>
        private void OnClickBg()
        {
            GameLog.Info("Login", "点击登录背景(老端在此触发平台 SDK 登录回调;本端平台桥未接,忽略)");
        }

        // ——— _img_bg 没绑上时按名兜底(烤制 prefab 节点名与老端一致 _img_bg)———
        private Image BgImage() => _img_bg != null ? _img_bg : transform.Find("_img_bg")?.GetComponent<Image>();
    }
}
