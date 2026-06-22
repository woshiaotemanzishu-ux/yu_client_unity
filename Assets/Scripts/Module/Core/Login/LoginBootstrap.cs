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
    /// </summary>
    public static class LoginBootstrap
    {
        private static AppConfig _smokeConfig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On<AppConfig>(GlobalEvent.EVT_FRAMEWORK_READY, OnFrameworkReady);
        }

        private static void OnFrameworkReady(AppConfig config)
        {
            EventDispatcher.Off<AppConfig>(GlobalEvent.EVT_FRAMEWORK_READY, OnFrameworkReady);
            LoginController.Instance.Setup(config);

            if (config.autoLoginSmokeTest && !string.IsNullOrEmpty(config.devAccount))
            {
                // 无 UI 冒烟(排查链路问题用);正常走下面的真实 UI 流程
                _smokeConfig = config;
                EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleListReceived);
                _ = RunChainSmokeTestAsync(config);
            }
            else
            {
                _smokeConfig = null;
                _ = LoginFlow.StartAsync(config);
            }
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
            AppConfig config = _smokeConfig;
            _smokeConfig = null;
            GameLog.Info("Login", "—— ✅ 登录链全通:HTTP登录 → 选服 → 入口 → WebSocket → 10000 回包(角色数={0})——", roleCount);
            if (config == null || !config.autoEnterFirstRoleSmokeTest) return;

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

        private static bool Check(LoginRequestResult result, string step)
        {
            if (result.success) return true;
            GameLog.Error("Login", "{0} 失败: {1}", step, result.message);
            return false;
        }
    }
}
