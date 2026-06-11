using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>注册页:确定注册 → player_register + 自动登录;返回 → 登录页。</summary>
    public sealed class RegisterView : RegisterViewBind
    {
        protected override void OnInit()
        {
            UIUtil.AddClick(_Image1, OnClickConfirm); // confirmBtn 底图
            UIUtil.AddClick(_Label3, OnClickConfirm);
            UIUtil.AddClick(_Image2, OnClickReturn);  // returnBtn 底图
            UIUtil.AddClick(_Label4, OnClickReturn);
        }

        protected override void OnShow(object args)
        {
            account.text = string.Empty;
            password.text = string.Empty;
        }

        private void OnClickConfirm()
        {
            _ = LoginFlow.SubmitRegisterAsync(account.text, password.text);
        }

        private void OnClickReturn()
        {
            LoginFlow.ShowLogin();
        }
    }
}
