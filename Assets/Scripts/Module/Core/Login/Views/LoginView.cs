using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 账号密码登录页(老客户端真实链路的第二步,加载完成后显示)。
    /// 登录走 player_check_login + player_login;「注册」切到 RegisterView。
    /// </summary>
    public sealed class LoginView : LoginViewBind
    {
        private bool _remember = true;

        protected override void OnInit()
        {
            UIUtil.AddClick(_Image2, OnClickLogin);   // loginBtn 的按钮底图
            UIUtil.AddClick(_Label4, OnClickLogin);
            UIUtil.AddClick(_Image1, OnClickRegister); // registerBtn 的按钮底图
            UIUtil.AddClick(_Label3, OnClickRegister);
            UIUtil.AddClick(check_img, OnClickRemember);
            UIUtil.AddClick(check_label, OnClickRemember);
        }

        protected override void OnShow(object args)
        {
            SavedLoginInput saved = LoginController.Instance.LoadSavedInput();
            if (string.IsNullOrEmpty(account.text)) account.text = saved.account;
            if (string.IsNullOrEmpty(password.text)) password.text = saved.password;
            _remember = saved.remember;
            RefreshRemember();
        }

        public void SetBusy(bool busy)
        {
            if (_Label4 != null) _Label4.text = busy ? "登录中" : "登录";
        }

        private void OnClickLogin()
        {
            _ = LoginFlow.SubmitLoginAsync(account.text, password.text, _remember);
        }

        private void OnClickRegister()
        {
            LoginFlow.ShowRegister();
        }

        private void OnClickRemember()
        {
            _remember = !_remember;
            RefreshRemember();
        }

        private void RefreshRemember()
        {
            if (check_img != null) check_img.enabled = _remember;
        }
    }
}
