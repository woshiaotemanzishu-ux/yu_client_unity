using System.Threading.Tasks;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录模块 UI 流程编排,严格对齐老客户端链路:
    ///
    ///   ① 加载页 LoginLoadingView(真实资源下载进度)
    ///   ② 登录/注册页 LoginView ⇄ RegisterView(输入账号密码,背景 LoginBgView)
    ///   ③ 登录成功 → LoginEnterView(显示当前服,踏入仙界)
    ///   ④ 点服务器名 → LoginSelectServerView 列表选服
    ///   ⑤ 踏入仙界 → get_server_info → WebSocket → 10000 → 选角/创角(待接)→ 进游戏
    /// </summary>
    public static class LoginFlow
    {
        private static AppConfig _config;
        private static GameObject _moduleRoot;
        private static LoginBgView _bg;
        private static LoginLoadingView _loading;
        private static LoginView _login;
        private static RegisterView _register;
        private static LoginEnterView _enter;
        private static LoginSelectServerView _select;
        private static LoginAlertView _alert;
        private static LoginSelectRoleView _selectRole;
        private static LoginCreateRoleView _createRole;
        private static bool _busy;

        /// <summary>
        /// 用户协议勾选状态(会话内)。持久化按账号记录,对标老客户端
        /// LoginEnterView.InitAgreementAgreeState 的 cookie LOCAL_ACCOUNT_INFO:
        /// 进入踏入仙界页瞬间,该账号同意过 → 自动勾选;否则立即弹 LoginAlertView;
        /// 弹层点同意 → 勾选 + 记录账号 + 直接进入游戏;拒绝 → 仅关闭。
        /// </summary>
        public static bool AgreementAgreed { get; private set; }

        private static string AgreedPrefKey => "login.agreed." + LoginController.Instance.Model.PlayerId;

        public static async Task StartAsync(AppConfig config)
        {
            _config = config;
            string key = GameResPath.GetUIPrefab("login", "LoginModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (_moduleRoot == null)
            {
                GameLog.Error("Login", "LoginModule prefab 加载失败(key={0})。先跑 LayaUI 转换 + 回填", key);
                return;
            }

            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
                if (v is LoginBgView bg) _bg = bg;
                else if (v is LoginLoadingView loading) _loading = loading;
                else if (v is LoginView login) _login = login;
                else if (v is RegisterView register) _register = register;
                else if (v is LoginEnterView enter) _enter = enter;
                else if (v is LoginSelectServerView select) _select = select;
                else if (v is LoginAlertView alert) _alert = alert;
                else if (v is LoginSelectRoleView selectRole) _selectRole = selectRole;
                else if (v is LoginCreateRoleView createRole) _createRole = createRole;
            }
            if (_bg == null || _loading == null || _login == null || _register == null || _enter == null
                || _select == null || _alert == null || _selectRole == null || _createRole == null)
            {
                GameLog.Error("Login",
                    "LoginModule 缺业务窗口(bg={0} loading={1} login={2} register={3} enter={4} select={5} alert={6} selectRole={7} createRole={8})——重跑 login 流水线(转换+回填)",
                    _bg != null, _loading != null, _login != null, _register != null, _enter != null,
                    _select != null, _alert != null, _selectRole != null, _createRole != null);
                return;
            }

            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleList);
            EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);

            // 确定性层级:背景永远垫底;其余窗口靠 BaseView.Show() 置顶,
            // 弹出顺序即渲染顺序(Hierarchy 里可见:Show 的窗口跳到最后一位)
            _bg.transform.SetAsFirstSibling();

            // ---------- ① 加载页 ----------
            _loading.Show();
            _loading.SetProgress(0f);
            await PreloadAsync();
            _loading.SetProgress(1f);
            await Task.Yield();
            _loading.Hide();

            // ---------- ② 登录页 ----------
            _bg.Show();
            ShowLogin();
        }

        private static async Task PreloadAsync()
        {
            string[] keys = _config.preloadKeys;
            if (keys == null || keys.Length == 0)
            {
                return;
            }
            try
            {
                long size = await ResManager.GetDownloadSize(keys);
                if (size > 0)
                {
                    GameLog.Info("Login", "预下载 {0} 项,共 {1} KB", keys.Length, size / 1024);
                    await ResManager.DownloadAsync(keys, p => _loading.SetProgress(p));
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Login", "预下载跳过: {0}", e.Message);
            }
        }

        // ---------------------------------------------------------------- ② 登录/注册

        public static void ShowLogin()
        {
            _register.Hide();
            _login.Show();
        }

        public static void ShowRegister()
        {
            _login.Hide();
            _register.Show();
        }

        public static async Task SubmitLoginAsync(string account, string password, bool remember)
        {
            if (_busy) return;
            _busy = true;
            _login.SetBusy(true);
            try
            {
                Task<LoginRequestResult> task = LoginController.Instance.LoginAsync(account, password, remember);
                LoginRequestResult result = await WithTimeout(task, "登录");
                if (!result.success)
                {
                    GameLog.Warn("Login", "登录失败: {0}", result.message);
                    TipsToLoginPage(result.message);
                    return;
                }
                EnterLobby();
            }
            finally
            {
                _busy = false;
                _login.SetBusy(false);
            }
        }

        public static async Task SubmitRegisterAsync(string account, string password)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Task<LoginRequestResult> task = LoginController.Instance.RegisterAsync(account, password, true);
                LoginRequestResult result = await WithTimeout(task, "注册");
                if (!result.success)
                {
                    GameLog.Warn("Login", "注册失败: {0}", result.message);
                    TipsToLoginPage(result.message);
                    return;
                }
                EnterLobby();
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>登录/注册成功 → ③ 踏入仙界页;进入瞬间按账号决定协议弹层(对标老客户端)。</summary>
        private static void EnterLobby()
        {
            GameLog.Info("Login", "账号就绪 player_id={0} 服务器数={1} 大区数={2}",
                LoginController.Instance.Model.PlayerId, LoginController.Instance.Model.Servers.Count,
                LoginController.Instance.Model.Areas.Count);
            _login.Hide();
            _register.Hide();
            _enter.Show();

            if (Shenxiao.Common.Prefs.PrefsManager.GetBool(AgreedPrefKey, false))
            {
                AgreementAgreed = true;   // 该账号同意过:自动勾选,不弹
                _enter.RefreshAgreement();
            }
            else
            {
                AgreementAgreed = false;  // 新账号:进入瞬间弹协议层
                _enter.RefreshAgreement();
                ShowAgreement();
            }
        }

        public static void ShowAgreement()
        {
            GameLog.Info("Login", "弹出用户协议弹层(LoginAlertView 激活并置顶)");
            _alert.ShowWith(
                onOk: OnAgreementOk,
                onCancel: OnAgreementCancel);
        }

        private static void OnAgreementOk()
        {
            // 老客户端 AGREE_LOGIN_ALERT:勾选 + 记录该账号 + 直接进入游戏
            AgreementAgreed = true;
            Shenxiao.Common.Prefs.PrefsManager.SetBool(AgreedPrefKey, true);
            _enter.RefreshAgreement();
            _ = EnterGameAsync();
        }

        private static void OnAgreementCancel()
        {
            AgreementAgreed = false;
            _enter.RefreshAgreement();
        }

        public static void ToggleAgreement()
        {
            AgreementAgreed = !AgreementAgreed;
            _enter.RefreshAgreement();
        }

        private static void TipsToLoginPage(string message)
        {
            // TODO:接 TipsSystem 的 Toast;现阶段用日志 + 标题文案
            GameLog.Warn("Login", "提示: {0}", message);
        }

        private static async Task<LoginRequestResult> WithTimeout(Task<LoginRequestResult> task, string what)
        {
            Task finished = await Task.WhenAny(task, Task.Delay(15000));
            if (finished != task)
            {
                GameLog.Error("Login", "{0}超时:{1} 不可达(浏览器应能打开并显示 API is ready)", what, _config.gmApiUrl);
                return LoginRequestResult.Fail(what + "超时:账号服务器不可达");
            }
            return task.Result;
        }

        // ---------------------------------------------------------------- ④ 选服 / ⑤ 进服

        public static void OpenServerSelect()
        {
            if (_select != null) _select.Show();
        }

        public static async Task SelectServerAsync(LoginServerInfo server)
        {
            LoginRequestResult result = await LoginController.Instance.SelectServerAsync(server);
            if (!result.success)
            {
                GameLog.Warn("Login", "选服失败: {0}", result.message);
                return;
            }
            _select.Hide();
            _enter.RefreshServer();
        }

        public static async Task EnterGameAsync()
        {
            if (!AgreementAgreed)
            {
                GameLog.Info("Login", "踏入仙界被拦截:协议未勾选 → 弹协议层(老客户端规则)");
                ShowAgreement();
                return;
            }
            if (_busy) return;
            _busy = true;
            try
            {
                _enter.SetTip("解析服务器入口 ...");
                LoginRequestResult result = await LoginController.Instance.ResolveSelectedServerEndpointAsync();
                if (!result.success)
                {
                    _enter.SetTip("入口解析失败: " + result.message);
                    return;
                }

                _enter.SetTip("连接游戏服 ...");
                result = await LoginController.Instance.ConnectGameAsync();
                if (!result.success)
                {
                    _enter.SetTip("连接失败: " + result.message);
                    return;
                }
                _enter.SetTip("已连接,等待角色数据 ...");
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>角色列表到达 → 有角色进选角页,无角色进创角页(对标老客户端 On10000 分流)。</summary>
        private static void OnRoleList(int roleCount)
        {
            GameLog.Info("Login", "角色列表到达(角色数={0})→ {1}", roleCount, roleCount > 0 ? "选角页" : "创角页");
            _enter.RefreshServer();
            _enter.Hide();
            if (roleCount > 0) _selectRole.Show();
            else _createRole.Show();
        }

        /// <summary>选角/创角页的返回:回到踏入仙界页(断开游戏服重选)。</summary>
        public static void BackToEnter()
        {
            _selectRole.Hide();
            _createRole.Hide();
            _ = NetManagerDisconnect();
            _enter.Show();
        }

        private static async Task NetManagerDisconnect()
        {
            await Shenxiao.Framework.Net.NetManager.DisconnectAsync();
        }

        private static void OnGameEntered()
        {
            GameLog.Info("Login", "—— 🎉 全链路终点:已进入游戏,登录模块使命完成,主城/场景接管(待接)——");
        }
    }
}
