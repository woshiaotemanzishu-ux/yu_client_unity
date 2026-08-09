using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainHelpAlert : BossDomainHelpAlertBind
    {
        private Action _confirmed;
        private bool _checked;

        protected override void OnInit()
        {
            if (_cancel_btn != null) UIUtil.AddClick(_cancel_btn, Hide);
            if (_close_btn != null) UIUtil.AddClick(_close_btn, Hide);
            if (_ok_btn != null) UIUtil.AddClick(_ok_btn, Confirm);
            if (gp_check != null) UIUtil.AddClick(gp_check, ToggleCheck);
        }

        public void ShowAssistConfirm(Action confirmed)
        {
            _confirmed = confirmed;
            _checked = false;
            if (gp_check != null) gp_check.gameObject.SetActive(true);
            UpdateCheck();
            Show();
        }

        protected override void OnHide() => _confirmed = null;

        private void ToggleCheck()
        {
            _checked = !_checked;
            UpdateCheck();
        }

        private void UpdateCheck()
        {
            if (check != null) check.gameObject.SetActive(_checked);
        }

        private void Confirm()
        {
            Action callback = _confirmed;
            Hide();
            callback?.Invoke();
        }
    }
}
