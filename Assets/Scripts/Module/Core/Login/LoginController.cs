using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Login
{
    public sealed class LoginController : BaseController
    {
        private const string PREF_ACCOUNT = "login.account";
        private const string PREF_PASSWORD = "login.password";
        private const string PREF_REMEMBER = "login.remember";
        private const string PREF_DEVICE_ID = "login.device_id";
        private static readonly LoginController _instance = new LoginController();

        private AppConfig _config;

        public static LoginController Instance => _instance;
        public LoginModel Model => LoginModel.Instance;

        private LoginController()
        {
        }

        public void Setup(AppConfig config)
        {
            _config = config;
            if (_config == null) return;

            GmApi.BaseUrl = _config.gmApiUrl;
            GmApi.LoginKey = _config.gmLoginKey;
            Init();
        }

        public SavedLoginInput LoadSavedInput()
        {
            bool remember = PrefsManager.GetBool(PREF_REMEMBER, true);
            string account = PrefsManager.GetString(PREF_ACCOUNT, string.Empty);
            string password = remember ? PrefsManager.GetString(PREF_PASSWORD, string.Empty) : string.Empty;

            if (string.IsNullOrEmpty(account) && _config != null)
            {
                account = _config.devAccount ?? string.Empty;
            }

            return new SavedLoginInput(account, password, remember);
        }

        public async Task<LoginRequestResult> LoginAsync(string account, string password, bool rememberPassword)
        {
            string trimmedAccount = (account ?? string.Empty).Trim();
            string trimmedPassword = (password ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedAccount) || string.IsNullOrEmpty(trimmedPassword))
            {
                return LoginRequestResult.Fail("请输入账号密码");
            }

            JObject checkInfo = await GmApi.PlayerCheckLogin(trimmedAccount, trimmedPassword);
            if (!IsOk(checkInfo, out string checkError))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(checkError) ? "账号或密码错误" : checkError);
            }

            string token = LoginModel.ReadString(checkInfo, "token", string.Empty);
            LoginRequestResult result = await PlayerLoginAsync(trimmedAccount, token);
            if (result.success)
            {
                SaveAccount(trimmedAccount, trimmedPassword, rememberPassword);
            }
            return result;
        }

        public async Task<LoginRequestResult> RegisterAsync(string account, string password, bool rememberPassword)
        {
            string trimmedAccount = (account ?? string.Empty).Trim();
            string trimmedPassword = (password ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedAccount) || string.IsNullOrEmpty(trimmedPassword))
            {
                return LoginRequestResult.Fail("请输入账号密码");
            }

            JObject registerInfo = await GmApi.PlayerRegister(trimmedAccount, trimmedPassword, GetPlatName());
            if (!IsOk(registerInfo, out string registerError))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(registerError) ? "注册失败" : registerError);
            }

            string token = LoginModel.ReadString(registerInfo, "token", string.Empty);
            LoginRequestResult result = await PlayerLoginAsync(trimmedAccount, token);
            if (result.success)
            {
                SaveAccount(trimmedAccount, trimmedPassword, rememberPassword);
            }
            return result;
        }

        public Task<LoginRequestResult> SelectServerAsync(LoginServerInfo server)
        {
            if (server == null || server.id <= 0)
            {
                return Task.FromResult(LoginRequestResult.Fail("请选择服务器"));
            }

            if (server.IsClosed)
            {
                string message = string.IsNullOrEmpty(server.closeDesc) ? "服务器维护中" : server.closeDesc;
                return Task.FromResult(LoginRequestResult.Fail(message));
            }

            Model.SelectServer(server);
            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_SERVER_SELECTED, server.id);
            GameLog.Info("Login", "selected server id={0}", server.id);
            return Task.FromResult(LoginRequestResult.Ok());
        }

        public async Task<LoginRequestResult> ResolveSelectedServerEndpointAsync()
        {
            LoginServerInfo server = Model.SelectedServer;
            if (server == null || server.id <= 0)
            {
                return LoginRequestResult.Fail("请选择服务器");
            }

            JObject info = await GmApi.GetServerInfo(Model.PlayerId, server.id, Model.Token);
            if (!IsOk(info, out string error))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(error) ? "获取服务器入口失败" : error);
            }

            Model.ApplySelectedServerInfo(info);
            GameLog.Info("Login", "resolved server endpoint id={0} host={1} port={2}", server.id, server.host, server.port);
            return LoginRequestResult.Ok();
        }

        protected override void Register()
        {
        }

        private async Task<LoginRequestResult> PlayerLoginAsync(string account, string token)
        {
            string deviceId = GetOrCreateDeviceId();
            string platName = GetPlatName();
            Model.ResetSession(account, platName, token);

            JObject loginInfo = await GmApi.PlayerLogin(account, platName, deviceId, token);
            if (!IsOk(loginInfo, out string loginError))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(loginError) ? "登录失败" : loginError);
            }

            Model.ApplyPlayerLoginInfo(loginInfo);
            if (Model.Servers.Count == 0)
            {
                JObject serverListInfo = await GmApi.GetServerList();
                if (!IsOk(serverListInfo, out string serverListError))
                {
                    return LoginRequestResult.Fail(string.IsNullOrEmpty(serverListError) ? "获取服务器列表失败" : serverListError);
                }
                Model.ApplyServerListInfo(serverListInfo);
            }

            if (Model.Servers.Count == 0)
            {
                return LoginRequestResult.Fail("没有可用服务器");
            }

            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_SUCCESS);
            return LoginRequestResult.Ok();
        }

        private string GetPlatName()
        {
            if (_config == null || string.IsNullOrEmpty(_config.platName)) return "unity";
            return _config.platName;
        }

        private static string GetOrCreateDeviceId()
        {
            string deviceId = PrefsManager.GetString(PREF_DEVICE_ID, string.Empty);
            if (!string.IsNullOrEmpty(deviceId)) return deviceId;

            deviceId = Guid.NewGuid().ToString("N");
            PrefsManager.SetString(PREF_DEVICE_ID, deviceId);
            return deviceId;
        }

        private static void SaveAccount(string account, string password, bool rememberPassword)
        {
            PrefsManager.SetString(PREF_ACCOUNT, account);
            PrefsManager.SetBool(PREF_REMEMBER, rememberPassword);
            if (rememberPassword) PrefsManager.SetString(PREF_PASSWORD, password);
            else PrefsManager.Remove(PREF_PASSWORD);
        }

        private static bool IsOk(JObject info, out string message)
        {
            message = string.Empty;
            if (info == null)
            {
                message = "网络请求失败";
                return false;
            }

            int ret = LoginModel.ReadInt(info, "ret", -1);
            message = LoginModel.ReadString(info, "msg", string.Empty);
            return ret == 0;
        }
    }

    public readonly struct SavedLoginInput
    {
        public readonly string account;
        public readonly string password;
        public readonly bool remember;

        public SavedLoginInput(string account, string password, bool remember)
        {
            this.account = account;
            this.password = password;
            this.remember = remember;
        }
    }

    public readonly struct LoginRequestResult
    {
        public readonly bool success;
        public readonly string message;

        private LoginRequestResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        public static LoginRequestResult Ok()
        {
            return new LoginRequestResult(true, string.Empty);
        }

        public static LoginRequestResult Fail(string message)
        {
            return new LoginRequestResult(false, message);
        }
    }
}
