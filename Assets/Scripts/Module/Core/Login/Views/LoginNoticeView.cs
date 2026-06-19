using System;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录公告界面(对标老客户端 login/LoginNoticeView.ts):页签(LoginNoticeBtnItem)+ 内容列表(LoginNoticeItem)
    /// + 测试服开服倒计时 + 已读关闭按钮,点击背景/已读关闭。
    ///
    /// 降级:LoginModel 公告配置/测试服数据 + LoopScrowViewMgr 未移植 → 页签/内容模板隐藏、测试服组隐藏、
    /// 列表空、_btn_read → Hide、OnShow 打 TODO。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class LoginNoticeView : LoginNoticeViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_LoginNoticeBtnItem != null) _tpl_LoginNoticeBtnItem.SetActive(false);
            if (_tpl_LoginNoticeItem != null) _tpl_LoginNoticeItem.SetActive(false);
            if (_gp_test_server != null) _gp_test_server.gameObject.SetActive(false);
            if (_lb_test_server != null) _lb_test_server.gameObject.SetActive(false);
            BindBtn(_btn_read, () => Hide());
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Login", "登录公告打开 → 待对接 LoginModel 公告配置/测试服数据");
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
