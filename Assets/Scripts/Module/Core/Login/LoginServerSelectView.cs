using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Login;

namespace Shenxiao.Module.Core.Login
{
    public sealed class LoginServerSelectView : LoginServerSelectViewBind
    {
        private readonly List<LoginServerInfo> _servers = new List<LoginServerInfo>();
        private bool _submitting;

        protected override void OnInit()
        {
            _txt_title.text = "Server";
            _txt_message.text = string.Empty;
            SetWaiting(false);
        }

        protected override void OnShow(object args)
        {
            _btn_back.onClick.AddListener(OnClickBack);
            _btn_enter.onClick.AddListener(OnClickEnter);
            _dd_server.onValueChanged.AddListener(OnServerChanged);
            RefreshServers();
        }

        protected override void OnHide()
        {
            _btn_back.onClick.RemoveListener(OnClickBack);
            _btn_enter.onClick.RemoveListener(OnClickEnter);
            _dd_server.onValueChanged.RemoveListener(OnServerChanged);
        }

        private async void OnClickBack()
        {
            ViewManager.Close<LoginServerSelectView>();
            await ViewManager.Open<LoginEntryView>();
        }

        private async void OnClickEnter()
        {
            if (_submitting) return;
            await SelectServerAsync();
        }

        private void OnServerChanged(int index)
        {
            if (index < 0 || index >= _servers.Count) return;
            LoginController.Instance.Model.SelectServer(_servers[index]);
            RefreshSelectedServer();
        }

        private void RefreshServers()
        {
            _servers.Clear();
            IReadOnlyList<LoginServerInfo> source = LoginController.Instance.Model.Servers;
            for (int i = 0; i < source.Count; i++)
            {
                _servers.Add(source[i]);
            }

            _dd_server.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < _servers.Count; i++)
            {
                LoginServerInfo server = _servers[i];
                string state = server.IsClosed ? " [维护]" : string.Empty;
                options.Add(new TMP_Dropdown.OptionData(server.DisplayName + state));
            }
            _dd_server.AddOptions(options);

            LoginModel model = LoginController.Instance.Model;
            int selectedIndex = FindSelectedIndex(model.SelectedServer);
            if (selectedIndex < 0 && _servers.Count > 0)
            {
                selectedIndex = 0;
                model.SelectServer(_servers[0]);
            }

            _dd_server.interactable = _servers.Count > 0;
            _btn_enter.interactable = _servers.Count > 0;
            if (selectedIndex >= 0) _dd_server.SetValueWithoutNotify(selectedIndex);
            RefreshSelectedServer();
        }

        private void RefreshSelectedServer()
        {
            LoginModel model = LoginController.Instance.Model;
            _txt_account.text = string.IsNullOrEmpty(model.Account) ? "Account" : model.Account;

            LoginServerInfo server = model.SelectedServer;
            if (server == null)
            {
                _txt_selected_server.text = "No server";
                _txt_message.text = "No server available";
                return;
            }

            _txt_selected_server.text = server.DisplayName;
            _txt_message.text = server.IsClosed ? "Server is under maintenance" : string.Empty;
        }

        private async Task SelectServerAsync()
        {
            SetSubmitting(true);
            LoginRequestResult result = await LoginController.Instance.SelectServerAsync(LoginController.Instance.Model.SelectedServer);
            SetSubmitting(false);

            if (!result.success)
            {
                TipsManager.Toast(result.message);
                _txt_message.text = result.message;
                return;
            }

            _txt_message.text = "服务器已选择，下一步进入角色选择";
            TipsManager.Toast("服务器已选择");
        }

        private int FindSelectedIndex(LoginServerInfo server)
        {
            if (server == null) return -1;
            for (int i = 0; i < _servers.Count; i++)
            {
                if (_servers[i].id == server.id) return i;
            }
            return -1;
        }

        private void SetSubmitting(bool submitting)
        {
            _submitting = submitting;
            _btn_back.interactable = !submitting;
            _btn_enter.interactable = !submitting;
            _dd_server.interactable = !submitting && _servers.Count > 0;
            SetWaiting(submitting);
        }

        private void SetWaiting(bool active)
        {
            _panel_waiting.gameObject.SetActive(active);
        }
    }
}
