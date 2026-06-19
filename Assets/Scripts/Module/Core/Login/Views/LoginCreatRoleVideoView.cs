using System;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 建角色开场视频界面(对标老客户端 login/LoginCreatRoleVideoView.ts):全屏播放开场视频(_box_video),
    /// 跳过按钮(_box_skip)结束 → 进 LoginLoadingView。
    ///
    /// 降级:GameVideo 视频系统未移植 → 视频容器空、OnShow 打 TODO;_box_skip → Hide(对标 PlayEnded 后关闭)。
    /// 事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class LoginCreatRoleVideoView : LoginCreatRoleVideoViewBind
    {
        protected override void OnInit()
        {
            BindBtn(_box_skip, () => Hide());
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Login", "建角色开场视频 → 待对接 GameVideo 视频播放(现可点跳过关闭)");
        }

        private void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
