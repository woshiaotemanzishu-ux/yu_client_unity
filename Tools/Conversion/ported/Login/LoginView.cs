using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 账号密码登录页(对标老客户端 login/LoginView.ts)。
    ///
    /// data-only:背景/输入框/按钮/勾选框等结构已烤进 prefab,这里【只绑数据】——
    /// 不再运行时 new 节点、不再摆位置(老端的 GetComponents/GetChildrenByNames 由 Bind 取代)。
    /// 链路:loginBtn → player_check_login + player_login(经 LoginFlow.SubmitLoginAsync);
    /// registerBtn → 切到 RegisterView;checkBtn → 记住密码开关(对标老端 SelectFun 换勾选图)。
    /// 进入时从本地存档回填上次账号/密码(对标老端 CookieWrapper 的 LOGIN_USER_NAME/PASSWORD/REMEMBER)。
    /// </summary>
    public sealed class LoginView : LoginViewBind
    {
        // 对标老端 SelectFun:勾选 com_ui_gx1,未勾选 com_ui_gx(老端在 alert 包,Unity 解析到 common/texture)。
        private const string CHECK_ON = "resource/game/common/texture/com_ui_gx1.png";
        private const string CHECK_OFF = "resource/game/common/texture/com_ui_gx.png";

        private bool _remember = true;

        protected override void OnInit()
        {
            // OnInit 只做一次的常驻初始化;点击监听放 OnShow 里重绑(先清后加,防叠加)。
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess: InitEvent + UpdateView。点击每次重绑前先 ClearClicks 防叠加(对标样板 BindBakedCareers)。
            BindClicks();

            // 老端 UpdateView:从 cookie 回填账号/记住密码状态/密码。
            SavedLoginInput saved = LoginController.Instance.LoadSavedInput();
            if (string.IsNullOrEmpty(account.text)) account.text = saved.account;
            _remember = saved.remember;
            if (_remember && string.IsNullOrEmpty(password.text)) password.text = saved.password;
            RefreshRemember(); // 老端 SelectFun
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
        }

        public void SetBusy(bool busy)
        {
            // 老端无此态,Unity 侧 LoginFlow 用它在登录中改按钮文案;保留。
            if (_Label4 != null) _Label4.text = busy ? "登录中" : "登录";
        }

        /// <summary>把点击绑到烤进 prefab 的按钮内图/文字上(loginBtn/registerBtn/checkBtn 三个盒的子图)。</summary>
        private void BindClicks()
        {
            // loginBtn 盒:内图 _Image2 + 文字 _Label4(对标老端 Util.AddClickEvent(this.loginBtn))。
            ClearAndAddClick(_Image2, OnClickLogin);
            ClearAndAddClick(_Label4, OnClickLogin);
            // registerBtn 盒:内图 _Image1 + 文字 _Label3。
            ClearAndAddClick(_Image1, OnClickRegister);
            ClearAndAddClick(_Label3, OnClickRegister);
            // checkBtn 盒:勾选图 check_img + 文字 check_label。
            ClearAndAddClick(check_img, OnClickRemember);
            ClearAndAddClick(check_label, OnClickRemember);
        }

        private static void ClearAndAddClick(UnityEngine.UI.Graphic target, System.Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target); // 每次 OnShow 重绑前先清,防监听叠加(对标样板)
            UIUtil.AddClick(target, onClick);
        }

        private void OnClickLogin()
        {
            // 老端先做账号/密码空判 + Message 提示;Unity 侧 LoginController.LoginAsync 内已做空判与提示,这里直接提交。
            _ = LoginFlow.SubmitLoginAsync(account.text, password.text, _remember);
        }

        private void OnClickRegister()
        {
            // 老端:Close 当前页 + 切 Enum_LoginState.Register + 打开 RegisterView。Unity 侧由 LoginFlow 收口。
            LoginFlow.ShowRegister();
        }

        private void OnClickRemember()
        {
            // 老端 checkBtn:翻转 check_select_ 后 SelectFun。
            _remember = !_remember;
            RefreshRemember();
        }

        /// <summary>对标老端 SelectFun:按记住态换勾选图(走 ResManager.SetImageAsync,不改 enabled)。</summary>
        private void RefreshRemember()
        {
            if (check_img == null) return;
            _ = ResManager.SetImageAsync(check_img, _remember ? CHECK_ON : CHECK_OFF, nativeSize: false);
        }
    }
}
