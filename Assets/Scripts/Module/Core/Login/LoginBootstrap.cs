using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录模块引导。框架就绪后初始化 LoginController;
    /// AppConfig.autoLoginSmokeTest 开启时自动跑通整条登录链(L.5 验收用):
    ///   HTTP player_login(自动注册)→ 选服 → get_server_info → WebSocket → 10000 → 角色列表。
    /// UI 接入(LayaUI 转换产物)在链路验收后进行。
    ///
    /// PlaySmoke 批处理通道(-shenxiaoPlaySmoke 命令行参数,见 GetCommandLineForcesSmoke,
    /// 用法照 RuntimeUiCaptureTool.GetCommandLineDelaySeconds 的参数读取模式):等价于同时打开
    /// AppConfig.autoLoginSmokeTest + autoEnterFirstRoleSmokeTest,且不改 asset——仅批处理场景专用。
    /// </summary>
    public static class LoginBootstrap
    {
        // 冒烟专用创角:老端服务端拒纯数字名,固定用纯中文;重名(10003 result==3)重试一次换名。
        private const string SMOKE_CREATE_ROLE_BASE_NAME = "冒烟测试";
        private const string SMOKE_CREATE_ROLE_RETRY_PREFIX = "冒烟";
        private const string SMOKE_CREATE_ROLE_RETRY_CHARS = "甲乙丙丁戊己庚辛";
        private const int SMOKE_CREATE_ROLE_CAREER = 1;
        private const int SMOKE_CREATE_ROLE_SEX = 1;

        private static AppConfig _smokeConfig;
        // -shenxiaoPlaySmoke 命令行强制:等价 autoEnterFirstRoleSmokeTest=true,不落 asset,仅本进程生效。
        private static bool _smokeForceEnterFirstRole;
        private static bool _smokeCreateRoleRetried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On<AppConfig>(GlobalEvent.EVT_FRAMEWORK_READY, OnFrameworkReady);
        }

        private static void OnFrameworkReady(AppConfig config)
        {
            EventDispatcher.Off<AppConfig>(GlobalEvent.EVT_FRAMEWORK_READY, OnFrameworkReady);
            LoginController.Instance.Setup(config);

            bool smokeForced = GetCommandLineForcesSmoke();
            bool smokeOn = (config.autoLoginSmokeTest || smokeForced) && !string.IsNullOrEmpty(config.devAccount);
            if (smokeOn)
            {
                // 无 UI 冒烟(排查链路问题用);正常走下面的真实 UI 流程
                _smokeConfig = config;
                _smokeForceEnterFirstRole = smokeForced;
                EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleListReceived);
                EventDispatcher.On<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateRoleResultForSmoke);
                _ = RunChainSmokeTestAsync(config);
            }
            else
            {
                _smokeConfig = null;
                _ = LoginFlow.StartAsync(config);
            }
        }

        /// <summary>
        /// PlaySmoke 批处理专用开关:命令行含 -shenxiaoPlaySmoke 时,等价于
        /// autoLoginSmokeTest + autoEnterFirstRoleSmokeTest 同时为 true。不读 AppConfig.asset,
        /// 只在这一次进程里生效(照 RuntimeUiCaptureTool 的命令行参数读取模式)。
        /// </summary>
        private static bool GetCommandLineForcesSmoke()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-shenxiaoPlaySmoke", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static async Task RunChainSmokeTestAsync(AppConfig config)
        {
            LoginController login = LoginController.Instance;
            GameLog.Info("Login", "—— 登录链冒烟开始 account={0} api={1} ——", config.devAccount, config.gmApiUrl);

            // 第15轮驱动:smoke 模式下启用 Round15 Combo副技能测试
            if (config.enableRound15ComboTest)
            {
                Shenxiao.Module.Core.Scene.SceneController.EnableRound15ComboTest = true;
                GameLog.Info("Login", "★ Round15 Combo副技能测试已启用(进游戏后自动驱动)");
            }

            // 第18轮驱动:smoke 模式下启用 Round18 连续击杀测试
            if (config.enableRound18ContinuousKill)
            {
                Shenxiao.Module.Core.Scene.SceneController.EnableRound18ContinuousKill = true;
                GameLog.Info("Login", "★ Round18 连续击杀测试已启用(combo回包若目标hp>0则自动继续)");
            }

            LoginRequestResult result = await login.DevLoginAsync(config.devAccount);
            if (!Check(result, "① HTTP 登录(player_login)")) return;
            GameLog.Info("Login", "① HTTP 登录通过: player_id={0} 服务器数={1} 上次服={2}",
                login.Model.PlayerId, login.Model.Servers.Count, login.Model.LastServerId);

            LoginServerInfo server = login.Model.SelectedServer;
            if (server == null)
            {
                GameLog.Error("Login", "② 选服失败: 没有可用服务器");
                return;
            }
            result = await login.SelectServerAsync(server);
            if (!Check(result, "② 选服")) return;

            result = await login.ResolveSelectedServerEndpointAsync();
            if (!Check(result, "③ 解析服务器入口(get_server_info)")) return;
            GameLog.Info("Login", "③ 入口: {0}:{1}", server.host, server.port);

            result = await login.ConnectGameAsync();
            if (!Check(result, "④ 连接游戏服 + 发送 10000")) return;
            GameLog.Info("Login", "④ 已连接,等待角色列表回包 ...");
        }

        private static void OnRoleListReceived(int roleCount)
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleListReceived);
            GameLog.Info("Login", "—— ✅ 登录链全通:HTTP登录 → 选服 → 入口 → WebSocket → 10000 回包(角色数={0})——", roleCount);

            AppConfig config = _smokeConfig;
            bool enterFirstRole = config != null && (config.autoEnterFirstRoleSmokeTest || _smokeForceEnterFirstRole);
            if (config == null || !enterFirstRole)
            {
                _smokeConfig = null;
                EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateRoleResultForSmoke);
                return;
            }

            if (roleCount == 0)
            {
                // 0 角色:冒烟自动创角(对标老端 LoginCreateRoleView 手动创角分支)。
                // _smokeConfig 保留到 OnCreateRoleResultForSmoke 处理完;EVT_GAME_CREATE_ROLE_RESULT 已在
                // OnFrameworkReady 里订阅。
                GameLog.Info("Login", "auto-create-role smoke: role list empty, creating name={0} career={1} sex={2}",
                    SMOKE_CREATE_ROLE_BASE_NAME, SMOKE_CREATE_ROLE_CAREER, SMOKE_CREATE_ROLE_SEX);
                LoginController.Instance.SendCreateRole(SMOKE_CREATE_ROLE_BASE_NAME, SMOKE_CREATE_ROLE_CAREER, SMOKE_CREATE_ROLE_SEX);
                return;
            }

            _smokeConfig = null;
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateRoleResultForSmoke);
            var roles = LoginModel.Instance.Roles;
            if (roles.Count <= 0)
            {
                GameLog.Warn("Login", "auto-enter smoke skipped: role list is empty");
                return;
            }

            GameRoleInfo role = roles[0];
            GameLog.Info("Login", "auto-enter smoke: role_id={0} name={1}", role.roleId, role.DisplayName);
            LoginController.Instance.EnterGameWithRole(role.roleId);
        }

        /// <summary>
        /// 冒烟创角结果(10003 result;LoginController.OnCreateRole 已转发 EVT_GAME_CREATE_ROLE_RESULT)。
        /// result==1 成功:对标老端 On10003 result==1 分支——不重拉角色列表(10000),直接 Fire TRY_LOGIN_GAME
        /// 用新 role_id 发 10004;LoginController.OnCreateRole 已经在 result==1 时自己调用
        /// EnterGameWithRole(roleId, true),这里只需清理冒烟订阅状态。
        /// result==3(角色名称已被使用,对标老端 On10003 result==3)重试一次换名:
        /// 「冒烟」+「甲乙丙丁戊己庚辛」随机取 2 字。其余失败码(2/4/5/6/7/8)直接放弃,打日志收场。
        /// </summary>
        private static void OnCreateRoleResultForSmoke(int result)
        {
            if (_smokeConfig == null) return;

            if (result == 1)
            {
                GameLog.Info("Login", "auto-create-role smoke: 创角成功 result=1,LoginController 已自动 EnterGameWithRole");
                _smokeConfig = null;
                _smokeCreateRoleRetried = false;
                EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateRoleResultForSmoke);
                return;
            }

            if (result == 3 && !_smokeCreateRoleRetried)
            {
                _smokeCreateRoleRetried = true;
                string retryName = SMOKE_CREATE_ROLE_RETRY_PREFIX + RandomRetrySuffix();
                GameLog.Warn("Login", "auto-create-role smoke: 重名(result=3),换名重试 name={0}", retryName);
                LoginController.Instance.SendCreateRole(retryName, SMOKE_CREATE_ROLE_CAREER, SMOKE_CREATE_ROLE_SEX);
                return;
            }

            GameLog.Error("Login", "auto-create-role smoke: 创角失败放弃 result={0} retried={1}", result, _smokeCreateRoleRetried);
            _smokeConfig = null;
            _smokeCreateRoleRetried = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateRoleResultForSmoke);
        }

        private static string RandomRetrySuffix()
        {
            int i0 = UnityEngine.Random.Range(0, SMOKE_CREATE_ROLE_RETRY_CHARS.Length);
            int i1 = UnityEngine.Random.Range(0, SMOKE_CREATE_ROLE_RETRY_CHARS.Length);
            return string.Concat(SMOKE_CREATE_ROLE_RETRY_CHARS[i0], SMOKE_CREATE_ROLE_RETRY_CHARS[i1]);
        }

        private static bool Check(LoginRequestResult result, string step)
        {
            if (result.success) return true;
            GameLog.Error("Login", "{0} 失败: {1}", step, result.message);
            return false;
        }
    }
}
