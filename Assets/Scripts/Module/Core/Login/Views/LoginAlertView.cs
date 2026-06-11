using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 用户协议提示弹窗(对标 Laya LoginAlertView):富文本内容已烘焙,
    /// 同意/拒绝/关闭回调由 LoginFlow 注入。
    /// </summary>
    public sealed class LoginAlertView : LoginAlertViewBind
    {
        private Action _onOk;
        private Action _onCancel;

        protected override void OnInit()
        {
            UIUtil.AddClick(_img_ok, OnClickOk);
            UIUtil.AddClick(_lb_ok, OnClickOk);
            UIUtil.AddClick(_img_cancel, OnClickCancel);
            UIUtil.AddClick(_lb_cancel, OnClickCancel);
            UIUtil.AddClick(_img_close, OnClickCancel);
        }

        public void ShowWith(Action onOk, Action onCancel)
        {
            _onOk = onOk;
            _onCancel = onCancel;
            Show();
        }

        private void OnClickOk()
        {
            Hide();
            _onOk?.Invoke();
        }

        private void OnClickCancel()
        {
            Hide();
            _onCancel?.Invoke();
        }
    }
}
