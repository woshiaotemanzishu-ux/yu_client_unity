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
        private static bool _busy;

        /// <summary>
        /// 用户协议勾选状态。对标老客户端 LoginEnterView.agreement_agree_state:
        /// 会话内存字段、不持久化(每次启动重置为未勾选);
        /// 点「踏入仙界」时未勾选 → 弹 LoginAlertView,同意即勾选。
        /// </summary>
        public static bool AgreementAgreed { get; private set; }

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
            }
            if (_bg == null || _loading == null || _login == null || _register == null || _enter == null || _select == null || _alert == null)
            {
                GameLog.Error("Login",
                    "LoginModule 缺业务窗口(bg={0} loading={1} login={2} register={3} enter={4} select={5} alert={6})——先在转换器点『回填 Bind 引用』",
                    _bg != null, _loading != null, _login != null, _register != null, _enter != null, _select != null, _alert != null);
                return;
            }

            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleList);

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

        /// <summary>登录/注册成功 → ③ 踏入仙界页(协议弹层由点「踏入仙界」触发,对标老客户端)。</summary>
        private static void EnterLobby()
        {
            GameLog.Info("Login", "账号就绪 player_id={0} 服务器数={1} 大区数={2}",
                LoginController.Instance.Model.PlayerId, LoginController.Instance.Model.Servers.Count,
                LoginController.Instance.Model.Areas.Count);
            _login.Hide();
            _register.Hide();
            _enter.Show();
        }

        public static void ShowAgreement()
        {
            _alert.ShowWith(
                onOk: OnAgreementOk,
                onCancel: OnAgreementCancel);
        }

        private static void OnAgreementOk()
        {
            AgreementAgreed = true;
            _enter.RefreshAgreement();
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
                ShowAgreement(); // 老客户端规则:未同意协议不能踏入仙界
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

        private static void OnRoleList(int roleCount)
        {
            GameLog.Info("Login", "—— ✅ 真实链路全通:加载 → 登录 → 选服 → 入口 → WebSocket → 角色列表(角色数={0})——", roleCount);
            if (_enter != null)
            {
                _enter.SetTip(roleCount > 0
                    ? $"连接成功,已有 {roleCount} 个角色(选角窗待接)"
                    : "连接成功,该服暂无角色(创角窗待接)");
            }
        }
    }
}
