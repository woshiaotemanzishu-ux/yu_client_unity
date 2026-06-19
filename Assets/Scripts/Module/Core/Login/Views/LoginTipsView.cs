using System;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录提示窗(对标老客户端 login/LoginTipsView.ts):标题(_lb_title)+ 提示列表(LoginTipsItem)+ 关闭(_img_close)。
    ///
    /// 降级:提示内容(LoginModel)未移植 → 提示项模板隐藏、列表空、_img_close → Hide、OnShow 打 TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class LoginTipsView : LoginTipsViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_LoginTipsItem != null) _tpl_LoginTipsItem.SetActive(false);
            BindBtn(_img_close, () => Hide());
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Login", "登录提示窗打开 → 待对接 提示内容(LoginModel)");
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
