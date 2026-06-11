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

        /// <summary>
        /// 开发/冒烟登录:跳过账号密码校验,直接走 player_login(yu_gm 侧账号不存在会自动注册)。
        /// 对应 Laya 平台 SDK 登录后的同一条路。
        /// </summary>
        public async Task<LoginRequestResult> DevLoginAsync(string account)
        {
            string trimmedAccount = (account ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedAccount))
            {
                return LoginRequestResult.Fail("请输入账号");
            }
            return await PlayerLoginAsync(trimmedAccount, string.Empty);
        }

        /// <summary>
        /// 连接已解析好入口的游戏服并发送账号登录协议(10000)。
        /// 角色列表回包由 OnAccountLogin 处理,完成后发 EVT_GAME_ROLE_LIST。
        /// </summary>
        public async Task<LoginRequestResult> ConnectGameAsync()
        {
            LoginServerInfo server = Model.SelectedServer;
            if (server == null || string.IsNullOrEmpty(server.host) || server.port <= 0)
            {
                return LoginRequestResult.Fail("服务器入口未解析,先调 ResolveSelectedServerEndpointAsync");
            }

            string url = $"ws://{server.host}:{server.port}";
            try
            {
                await NetManager.ConnectAsync(url);
            }
            catch (Exception e)
            {
                GameLog.Error("Login", "连接游戏服失败 {0}: {1}", url, e.Message);
                return LoginRequestResult.Fail("连接游戏服失败: " + e.Message);
            }

            if (_config != null)
            {
                NetManager.ConfigureHeartbeat(Proto.HEARTBEAT, _config.heartbeatIntervalSec);
            }

            // 对标 Laya GAME_CONNECT 后的 SendFmtToGame(10000, "iiss", pid, 时间戳, account_id, plat_name)
            int pid = server.pid > 0 ? server.pid : 1;
            long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SendFmt(Proto.ACCOUNT_LOGIN, "iiss", pid, timeStamp, Model.PlayerId.ToString(), Model.PlatName);
            GameLog.Info("Login", "已发送账号登录协议 pid={0} account_id={1} plat={2}", pid, Model.PlayerId, Model.PlatName);
            return LoginRequestResult.Ok();
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
            RegisterProtocal(Proto.ACCOUNT_LOGIN, OnAccountLogin);
            RegisterProtocal(Proto.HEARTBEAT, OnHeartbeat);
            RegisterProtocal(Proto.KICK_NOTIFY, OnKickNotify);
        }

        /// <summary>
        /// 10000 回包(对标 yu_client LoginController.On10000):
        /// "clihi" = career, 服务器时间(毫秒), 开服时间, 角色数, 注册数;
        /// 然后逐角色 "l" role_id + "c" state + FigureProtoVo 外观块 + "c" reward_id。
        /// 外观块解析在选角 UI 阶段做(TODO),这里读到第一个 role_id 即可证明链路,
        /// 剩余字节安全丢弃(每帧独立缓冲,不影响后续协议)。
        /// </summary>
        private void OnAccountLogin(NetReader reader)
        {
            object[] head = reader.ReadFmt("clihi");
            byte career = (byte)head[0];
            long serverTimeMs = (long)head[1];
            uint openTime = (uint)head[2];
            int roleCount = (ushort)head[3];
            uint registerNum = (uint)head[4];

            long firstRoleId = 0;
            byte firstRoleState = 0;
            if (roleCount > 0 && reader.Remaining >= 9)
            {
                firstRoleId = reader.ReadU64();
                firstRoleState = reader.ReadU8();
            }

            GameLog.Info("Login",
                "★ 游戏服 10000 回包: 角色数={0} 首角色id={1}(state={2}) career={3} 服务器时间={4} 开服时间={5} 注册数={6}",
                roleCount, firstRoleId, firstRoleState, career, serverTimeMs, openTime, registerNum);
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_ROLE_LIST, roleCount);
        }

        private void OnHeartbeat(NetReader reader)
        {
            // 心跳回包无须处理
        }

        private void OnKickNotify(NetReader reader)
        {
            GameLog.Warn("Login", "收到顶号/踢线通知(10007)");
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
