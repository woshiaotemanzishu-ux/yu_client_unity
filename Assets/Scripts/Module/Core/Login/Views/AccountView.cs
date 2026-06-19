using System;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 账号调试登录界面(对标老客户端 login/AccountView.ts):开发用,手动输入账号/IP/端口/PID 等连调试服 ——
    /// 账号项列表(AccountItem)+ 服务器标签(_lb_server)+ 进入(m_enter_btn)/切账号(m_account)按钮 + IP 地址下拉(_scroller_address)。
    ///
    /// 降级:LoginModel/LoginManager/AppConst/socket 连接 + IP 列表配置未移植 → 地址下拉/账号项模板隐藏、
    /// 进入/切账号按钮点击打 TODO。事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class AccountView : AccountViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_AccountItem != null) _tpl_AccountItem.SetActive(false);
            if (_scroller_address != null) _scroller_address.gameObject.SetActive(false);
            BindBtn(m_enter_btn, () => GameLog.Info("Login", "账号调试登录 进入 → 待对接 LoginManager 连接协议(START_GAME_CONNECT)"));
            BindBtn(m_account, () => GameLog.Info("Login", "切换账号登录 → 待对接 LoginManager.StartAccountLogin"));
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
