using System;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 用户协议/隐私政策窗(对标老客户端 login/LoginUserAgreementView.ts):协议文本(_lb_content)+ 协议/隐私页签
    /// (_img_xieyi/_img_privacy)+ 关闭(_img_close)。
    ///
    /// 降级:协议/隐私配置(ClientConfig)未移植 → 内容待接、xieyi/privacy 页签点击打 TODO、_img_close → Hide。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class LoginUserAgreementView : LoginUserAgreementViewBind
    {
        protected override void OnInit()
        {
            BindBtn(_img_close, () => Hide());
            BindBtn(_img_xieyi, () => GameLog.Info("Login", "用户协议页签 → 待对接 协议配置"));
            BindBtn(_img_privacy, () => GameLog.Info("Login", "隐私政策页签 → 待对接 隐私配置"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Login", "用户协议窗打开 → 待对接 协议/隐私文本(ClientConfig)");
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
