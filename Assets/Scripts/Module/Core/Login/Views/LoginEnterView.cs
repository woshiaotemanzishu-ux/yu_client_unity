using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录入口窗(对标 Laya LoginEnterView):显示当前服务器,点击换区开选服窗,
    /// 「踏入仙界」触发 HTTP 入口解析 + 连接游戏服。
    /// </summary>
    public sealed class LoginEnterView : LoginEnterViewBind
    {
        protected override void OnInit()
        {
            UIUtil.AddClick(_img_enter, OnClickEnter);
            UIUtil.AddClick(_img_search_server_bg, OnClickChangeServer);
            UIUtil.AddClick(_lb_cur_server_name, OnClickChangeServer);
            UIUtil.AddClick(_lb_tips, OnClickChangeServer);
            UIUtil.AddClick(_img_agreement_check_bg, OnClickAgreement);
            UIUtil.AddClick(_img_anreement_check, OnClickAgreement);
        }

        protected override void OnShow(object args)
        {
            RefreshServer();
            RefreshAgreement();
        }

        public void RefreshAgreement()
        {
            if (_img_anreement_check != null)
            {
                _img_anreement_check.enabled = LoginFlow.AgreementAgreed;
            }
        }

        private void OnClickAgreement()
        {
            LoginFlow.ToggleAgreement();
        }

        public void RefreshServer()
        {
            LoginServerInfo server = LoginModel.Instance.SelectedServer;
            _lb_cur_server_name.text = server != null ? server.DisplayName : "未选服";
            _lb_tips.text = "(点击换区)";
        }

        public void SetTip(string text)
        {
            _lb_tips.text = text;
        }

        private void OnClickEnter()
        {
            _ = LoginFlow.EnterGameAsync();
        }

        private void OnClickChangeServer()
        {
            LoginFlow.OpenServerSelect();
        }
    }
}
