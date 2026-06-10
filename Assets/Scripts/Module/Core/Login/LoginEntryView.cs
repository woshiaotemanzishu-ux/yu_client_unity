using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    public sealed class LoginEntryView : LoginEntryViewBind
    {
        private bool _submitting;

        protected override void OnInit()
        {
            _txt_first_load.text = "First load may take a while.";
            _txt_load_progress.text = "loading...0%";
            _txt_version_tip.text = "Healthy play reminder";
            ShowAccountPanel();
            SetWaiting(false, string.Empty);
        }

        protected override void OnShow(object args)
        {
            SavedLoginInput input = LoginController.Instance.LoadSavedInput();
            if (string.IsNullOrEmpty(_input_account.text)) _input_account.text = input.account;
            if (string.IsNullOrEmpty(_input_password.text)) _input_password.text = input.password;
            _chk_remember.isOn = input.remember;

            _btn_login.onClick.AddListener(OnClickLogin);
            _btn_register.onClick.AddListener(OnClickRegister);
            _btn_register_confirm.onClick.AddListener(OnClickRegisterConfirm);
            _btn_register_return.onClick.AddListener(OnClickRegisterReturn);
        }

        protected override void OnHide()
        {
            _btn_login.onClick.RemoveListener(OnClickLogin);
            _btn_register.onClick.RemoveListener(OnClickRegister);
            _btn_register_confirm.onClick.RemoveListener(OnClickRegisterConfirm);
            _btn_register_return.onClick.RemoveListener(OnClickRegisterReturn);
        }

        private async void OnClickLogin()
        {
            if (_submitting) return;
            await SubmitLoginAsync();
        }

        private void OnClickRegister()
        {
            ShowRegisterPanel();
        }

        private async void OnClickRegisterConfirm()
        {
            if (_submitting) return;
            await SubmitRegisterAsync();
        }

        private void OnClickRegisterReturn()
        {
            ShowAccountPanel();
        }

        private async Task SubmitLoginAsync()
        {
            SetSubmitting(true, "Logging in");
            LoginRequestResult result = await LoginController.Instance.LoginAsync(
                _input_account.text,
                _input_password.text,
                _chk_remember.isOn);
            SetSubmitting(false, string.Empty);

            if (!result.success)
            {
                TipsManager.Toast(result.message);
                return;
            }

            TipsManager.Toast("登录成功");
            await OpenServerSelectAsync();
        }

        private async Task SubmitRegisterAsync()
        {
            SetSubmitting(true, "Registering");
            LoginRequestResult result = await LoginController.Instance.RegisterAsync(
                _input_register_account.text,
                _input_register_password.text,
                _chk_remember.isOn);
            SetSubmitting(false, string.Empty);

            if (!result.success)
            {
                TipsManager.Toast(result.message);
                return;
            }

            TipsManager.Toast("注册成功");
            await OpenServerSelectAsync();
        }

        private async Task OpenServerSelectAsync()
        {
            ViewManager.Close<LoginEntryView>();
            await ViewManager.Open<LoginServerSelectView>();
        }

        private void ShowAccountPanel()
        {
            _panel_loading.gameObject.SetActive(false);
            _panel_account.gameObject.SetActive(true);
            _panel_register.gameObject.SetActive(false);
        }

        private void ShowRegisterPanel()
        {
            _panel_loading.gameObject.SetActive(false);
            _panel_account.gameObject.SetActive(false);
            _panel_register.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(_input_register_account.text)) _input_register_account.text = _input_account.text;
            if (string.IsNullOrEmpty(_input_register_password.text)) _input_register_password.text = _input_password.text;
        }

        private void SetSubmitting(bool submitting, string waitingText)
        {
            _submitting = submitting;
            _btn_login.interactable = !submitting;
            _btn_register.interactable = !submitting;
            _btn_register_confirm.interactable = !submitting;
            _btn_register_return.interactable = !submitting;
            SetWaiting(submitting, waitingText);
        }

        private void SetWaiting(bool active, string waitingText)
        {
            _panel_waiting.gameObject.SetActive(active);
            if (!string.IsNullOrEmpty(waitingText)) _txt_waiting.text = waitingText;
        }
    }
}
