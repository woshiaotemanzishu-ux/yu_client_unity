using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.Proto;
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

            // 对标 Laya GAME_CONNECT:SendFmtToGame(10000, "iiss", pid, time_stamp, account_id, plat_name)。
            // 关键:account_id = get_server_info 的 accname(游戏服按它认账号,发 player_id 会被当成
            // 另一个空账号 → 满角色的号进了创角页);time_stamp 同样优先用服务器下发的 time。
            int pid = server.pid > 0 ? server.pid : 1;
            string accountId = !string.IsNullOrEmpty(server.accname)
                ? server.accname
                : Model.PlayerId.ToString();
            long timeStamp = long.TryParse(server.time, out long svrTime) && svrTime > 0
                ? svrTime
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SendFmt(Proto.ACCOUNT_LOGIN, "iiss", pid, timeStamp, accountId, Model.PlatName);
            GameLog.Info("Login", "已发送账号登录协议 pid={0} accname={1} time={2} plat={3}",
                pid, accountId, timeStamp, Model.PlatName);
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
            RegisterProtocal(Proto.CREATE_ROLE, OnCreateRole);
            RegisterProtocal(Proto.ENTER_GAME, OnEnterGame);
            RegisterProtocal(Proto.HEARTBEAT, OnHeartbeat);
            RegisterProtocal(Proto.NAME_VERIFY, OnNameVerify);
        }

        /// <summary>
        /// 10000 回包(对标 yu_client LoginController.On10000):
        /// "clihi" = career, 服务器时间(毫秒), 开服时间, 角色数, 注册数;
        /// 逐角色 "l" role_id + "c" state + FigureProto 外观块 + "c" reward_id。
        /// </summary>
        private void OnAccountLogin(NetReader reader)
        {
            object[] head = reader.ReadFmt("clihi");
            long serverTimeMs = (long)head[1];
            int roleCount = (ushort)head[3];

            var roles = new List<GameRoleInfo>(roleCount);
            for (int i = 0; i < roleCount; i++)
            {
                var role = new GameRoleInfo();
                role.roleId = reader.ReadU64();
                role.state = reader.ReadU8();
                role.figure = FigureProto.Read(reader);
                role.rewardId = reader.ReadU8();
                roles.Add(role);
            }
            Model.SetRoles(roles);

            GameLog.Info("Login", "★ 10000 回包: 角色数={0} 服务器时间={1}", roleCount, serverTimeMs);
            for (int i = 0; i < roles.Count; i++)
            {
                GameLog.Info("Login", "  角色[{0}] id={1} {2} 职业={3} 等级={4} {5}转",
                    i, roles[i].roleId, roles[i].DisplayName, roles[i].Career, roles[i].Level, roles[i].Turn);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_ROLE_LIST, roleCount);
        }

        /// <summary>
        /// 创角请求(对标 TRY_CREATE_ROLE 的 10003 "cccsslsscscc")。
        /// 打点/邀请/模拟器等渠道字段按首登默认值发送。
        /// </summary>
        public void SendCreateRole(string roleName, int career, int sex)
        {
            SendFmt(Proto.CREATE_ROLE, "cccsslsscscc",
                0, career, sex, roleName, Model.PlatName,
                0L,        // inviter_id
                "",        // plat_account
                "",        // ta_distinct_id
                0,         // is_simulator
                "",        // ta_device_id
                0, 0);     // create_role_change_career / change_name
            GameLog.Info("Login", "发送创角: name={0} career={1} sex={2}", roleName, career, sex);
        }

        /// <summary>选角进入游戏(对标 TRY_LOGIN_GAME 的 10004 "lsisisscscsh")。</summary>
        public void EnterGameWithRole(long roleId)
        {
            // 对标老客户端 cookie LAST_LOGIN_ROLE_ID:选角页下次默认选中它
            Shenxiao.Common.Prefs.PrefsManager.SetString(LoginSelectRoleView.PREF_LAST_ROLE_ID, roleId.ToString());
            long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SendFmt(Proto.ENTER_GAME, "lsisisscscsh",
                roleId, "开发", timeStamp, "", 1, Model.PlatName,
                "",   // plat_account
                0,    // is_whitelist
                "",   // ta_distinct_id
                0,    // is_simulator
                "",   // ta_device_id
                0);   // scene_id(重连续接用,首登 0)
            GameLog.Info("Login", "发送进入游戏: role_id={0}", roleId);
        }

        /// <summary>10003 回包 "cl":result + role_id。result==1 直接进游戏(对标 On10003)。</summary>
        private void OnCreateRole(NetReader reader)
        {
            object[] data = reader.ReadFmt("cl");
            int result = (byte)data[0];
            long roleId = (long)data[1];
            GameLog.Info("Login", "创角结果 result={0} role_id={1}", result, roleId);
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, result);
            if (result == 1)
            {
                EnterGameWithRole(roleId);
            }
        }

        /// <summary>10004 回包 "c":result==1 进入游戏成功(对标 On10004,后续 GAME_START 主城接管)。</summary>
        private void OnEnterGame(NetReader reader)
        {
            int result = reader.ReadU8();
            if (result == 1)
            {
                GameLog.Info("Login", "🎉 进入游戏成功(10004),主城/场景流程待接");
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_ENTERED);
            }
            else
            {
                GameLog.Warn("Login", "进入游戏失败 result={0}", result);
            }
        }

        private void OnHeartbeat(NetReader reader)
        {
            // 心跳回包无须处理
        }

        /// <summary>10007 回包 "c":角色名验证结果(对标老客户端 On10007;此前误标为踢线)。</summary>
        private void OnNameVerify(NetReader reader)
        {
            int code = reader.ReadU8();
            string msg = code switch
            {
                1 => "验证成功",
                2 => "验证失败",
                4 => "角色名称已被使用",
                5 => "含非法字符",
                6 => "名称长度需 1~5",
                _ => "未知结果 " + code,
            };
            GameLog.Info("Login", "角色名验证(10007): {0}", msg);
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
