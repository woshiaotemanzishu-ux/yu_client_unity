using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Preload;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录模块 UI 流程编排(重构版:6 个自包含独立 prefab,不再用老 LoginModule 大合并 prefab)。
    /// 链路严格对齐老客户端:
    ///   ① 加载页 LoadingView(真实资源下载进度)
    ///   ② 登录/注册页 LoginPanelView(登录⇄注册子面板)
    ///   ③ 登录成功 → ServerEnterView(显示当前服 + 踏入仙界 + 用户协议弹层)
    ///   ④ 点服务器名 → ServerSelectView 列表选服
    ///   ⑤ 踏入仙界 → get_server_info → WebSocket → 10000 → 选角 RoleSelectView / 创角 RoleCreateView → 进游戏
    /// 每个页面背景自含,不再有独立 LoginBgView;协议弹层并进 ServerEnterView。
    /// </summary>
    public static class LoginFlow
    {
        private static AppConfig _config;
        private static LoginPanelView _loginPanel;     // ② 登录 + 注册
        private static LoadingView _loadingView;       // ① 加载
        private static ServerEnterView _enterView;     // ③ 踏入仙界 + 协议弹层
        private static ServerSelectView _selectView;   // ④ 选服
        private static RoleSelectView _selectRoleView; // ⑤ 选角
        private static RoleCreateView _createRoleView; // ⑤ 创角
        private static bool _busy;

        /// <summary>
        /// 用户协议勾选状态(会话内)。持久化按账号记录,对标老客户端:进入踏入仙界页瞬间,该账号同意过 →
        /// 自动勾选;否则立即弹协议层;弹层点同意 → 勾选 + 记录账号 + 直接进入游戏;拒绝 → 仅关闭。
        /// </summary>
        public static bool AgreementAgreed { get; private set; }

        private static string AgreedPrefKey => "login.agreed." + LoginController.Instance.Model.PlayerId;

        public static async Task StartAsync(AppConfig config)
        {
            _config = config;

            // 6 个独立 prefab 加载到 Window 层(键 prefabs/ui/login/<view>,编辑器有 prefab 兜底)。
            _loginPanel = await LoadViewAsync<LoginPanelView>("LoginPanel");
            _loadingView = await LoadViewAsync<LoadingView>("LoadingView");
            _enterView = await LoadViewAsync<ServerEnterView>("ServerEnterView");
            _selectView = await LoadViewAsync<ServerSelectView>("ServerSelectView");
            _selectRoleView = await LoadViewAsync<RoleSelectView>("RoleSelectView");
            _createRoleView = await LoadViewAsync<RoleCreateView>("RoleCreateView");
            if (_loginPanel == null || _loadingView == null || _enterView == null
                || _selectView == null || _selectRoleView == null || _createRoleView == null)
            {
                GameLog.Error("Login", "登录页 prefab 缺失——在「神霄/重构UI 生成器」面板里把 Login 各页都生成一遍");
                return;
            }

            _loginPanel.LoginSubmit = (a, p, r) => SubmitLoginAsync(a, p, r);
            _loginPanel.RegisterSubmit = (a, p) => SubmitRegisterAsync(a, p);

            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleList);
            EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            LegacyPreloadService.ProgressChanged += OnPreloadProgress;

            // ---------- ① 加载页 ----------
            _loadingView.Show();
            _loadingView.SetProgress(0f);
            await PreloadAsync();
            _loadingView.SetProgress(1f);
            await Task.Yield();
            _loadingView.Hide();

            // ---------- ② 登录页 ----------
            ShowLogin();
        }

        /// <summary>按视图名实例化对应独立 prefab 并取组件(失败返回 null)。</summary>
        private static async Task<T> LoadViewAsync<T>(string viewName) where T : BaseView
        {
            string key = GameResPath.GetUIPrefab("login", viewName);
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            T view = go != null ? go.GetComponent<T>() : null;
            if (view == null)
            {
                GameLog.Error("Login", "{0} prefab 加载失败(key={1})。先在「神霄/重构UI 生成器」里生成它", viewName, key);
                return null;
            }
            view.gameObject.SetActive(false);
            return view;
        }

        private static async Task PreloadAsync()
        {
            await LegacyPreloadService.PreloadBootAsync(_config.preloadKeys,
                (p, label) => _loadingView.SetProgress(p, label));
        }

        // ---------------------------------------------------------------- ② 登录/注册

        public static void ShowLogin()
        {
            if (_loginPanel == null) return;
            _loginPanel.Show();        // 显示登录面板并置顶
            _loginPanel.ShowLogin();   // 切登录子面板
        }

        public static void ShowRegister()
        {
            _loginPanel?.ShowRegister();
        }

        public static async Task SubmitLoginAsync(string account, string password, bool remember)
        {
            if (_busy) return;
            _busy = true;
            _loginPanel.SetBusy(true);
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
                TipsManager.Toast("恭喜登录成功");   // 对标老端 LoginState:321
                EnterLobby();
            }
            finally
            {
                _busy = false;
                _loginPanel.SetBusy(false);
            }
        }

        public static async Task SubmitRegisterAsync(string account, string password)
        {
            if (_busy) return;
            _busy = true;
            _loginPanel.SetBusy(true);   // 与登录一致:置 View._busy,让 OnClickConfirm 的防重入生效
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
                TipsManager.Toast("恭喜注册成功");   // 对标老端 RegisterState:105
                EnterLobby();
            }
            finally
            {
                _busy = false;
                _loginPanel.SetBusy(false);
            }
        }

        /// <summary>登录/注册成功 → ③ 踏入仙界页;协议勾选态按账号回读,未同意则由 OnShow 自动弹协议层。</summary>
        private static void EnterLobby()
        {
            GameLog.Info("Login", "账号就绪 player_id={0} 服务器数={1} 大区数={2}",
                LoginController.Instance.Model.PlayerId, LoginController.Instance.Model.Servers.Count,
                LoginController.Instance.Model.Areas.Count);
            _loginPanel.Hide();
            // 该账号是否同意过协议(持久化,对标老客户端):同意过→自动勾选;否则进入页 OnShow 自动弹协议层。
            AgreementAgreed = Shenxiao.Common.Prefs.PrefsManager.GetBool(AgreedPrefKey, false);
            _enterView.Show();   // OnShow 会 RefreshAgreement + 未同意时自动弹协议弹层
        }

        public static void ShowAgreement()
        {
            GameLog.Info("Login", "弹出用户协议弹层(ServerEnterView 内置)");
            _enterView?.ShowAgreementAlert();
        }

        /// <summary>协议弹层 同意/不同意 的结果:同意→勾选 + 按账号持久化;不同意→不勾选。不勾选无法踏入仙界。</summary>
        public static void SetAgreement(bool agreed)
        {
            AgreementAgreed = agreed;
            // 仅正式流程(_enterView 已就绪)按账号落库;编辑器预览态不写。
            if (agreed && _enterView != null)
                Shenxiao.Common.Prefs.PrefsManager.SetBool(AgreedPrefKey, true);
            _enterView?.RefreshAgreement();
        }

        private static void TipsToLoginPage(string message)
        {
            TipsManager.Toast(message);   // 对标老端 Message.show(err_msg);Toast 内部自带 GameLog
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
            _selectView?.Show();   // ServerSelectView.OnShow 会自动 Refresh
        }

        public static async Task SelectServerAsync(LoginServerInfo server)
        {
            LoginRequestResult result = await LoginController.Instance.SelectServerAsync(server);
            if (!result.success)
            {
                GameLog.Warn("Login", "选服失败: {0}", result.message);
                TipsManager.Toast(result.message);   // 选到维护服等失败给玩家反馈(老端失败也走 Message)
                return;
            }
            TipsManager.Toast("切换成功");   // 对标老 ClickServer:Message.show("切换成功")
            _selectView.Hide();              // 关闭选服面板(对标 Close)
            _enterView.RefreshServer();      // 踏入仙界页刷新当前服(对标 CHANGE_CUR_SERVER_ID → UpdateServerName)
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
                _enterView.SetTip("解析服务器入口 ...");
                LoginRequestResult result = await LoginController.Instance.ResolveSelectedServerEndpointAsync();
                if (!result.success)
                {
                    _enterView.SetTip("入口解析失败: " + result.message);
                    return;
                }

                _enterView.SetTip("连接游戏服 ...");
                result = await LoginController.Instance.ConnectGameAsync();
                if (!result.success)
                {
                    _enterView.SetTip("连接失败: " + result.message);
                    return;
                }
                _enterView.SetTip("已连接,等待角色数据 ...");
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>角色列表到达 → 有角色进选角页,无角色进创角页(对标老客户端 On10000 分流)。</summary>
        private static void OnRoleList(int roleCount)
        {
            _ = OnRoleListAsync(roleCount);
        }

        private static async Task OnRoleListAsync(int roleCount)
        {
            GameLog.Info("Login", "角色列表到达(角色数={0})→ {1}", roleCount, roleCount > 0 ? "选角页" : "创角页");
            _loadingView.Show();
            _loadingView.SetProgress(0f, "加载角色资源");
            await LegacyPreloadService.PreloadRoleSelectionAsync((p, label) => _loadingView.SetProgress(p, label));
            _loadingView.Hide();

            _enterView.RefreshServer();
            _enterView.Hide();
            _selectRoleView.Hide();
            _createRoleView.Hide();
            if (roleCount > 0) _selectRoleView.Show();  // OnShow 自动 Refresh
            else _createRoleView.Show();                // OnShow 自动 Refresh
        }

        /// <summary>选角页空槽的「创建角色」入口。</summary>
        public static void ShowCreateRole()
        {
            _selectRoleView.Hide();
            _createRoleView.Show();
        }

        /// <summary>创角页返回(有角色时):回选角页(对标老客户端 _img_return 分支)。</summary>
        public static void ShowSelectRole()
        {
            _createRoleView.Hide();
            _selectRoleView.Show();
        }

        /// <summary>选角/创角页的返回:回到踏入仙界页(断开游戏服重选)。</summary>
        public static void BackToEnter()
        {
            _selectRoleView.Hide();
            _createRoleView.Hide();
            LoginController.Instance.ClearInGameReconnectState();
            _ = NetManagerDisconnect();
            _enterView.Show();
        }

        private static async Task NetManagerDisconnect()
        {
            await Shenxiao.Framework.Net.NetManager.DisconnectAsync();
        }

        private static void OnGameEntered()
        {
            // 10004 成功后:隐藏所有登录页,起加载页等 GAME_START 资源接管完成。
            HideView(_loginPanel);
            HideView(_enterView);
            HideView(_selectView);
            HideView(_selectRoleView);
            HideView(_createRoleView);
            _loadingView.Show();
            _loadingView.SetProgress(0f, "加载游戏资源");
            GameLog.Info("Login", "进入游戏成功,等待 GAME_START 资源接管完成");
        }

        private static void OnGameStart()
        {
            HideAllViews();
            GameLog.Info("Login", "—— 🎉 GAME_START:登录模块退下,主城/场景接管 ——");
        }

        private static void OnPreloadProgress(LegacyPreloadStage stage, float progress, string hint)
        {
            if (_loadingView == null || !_loadingView.gameObject.activeInHierarchy) return;
            _loadingView.SetProgress(progress, hint);
        }

        private static void HideAllViews()
        {
            HideView(_loadingView);
            HideView(_loginPanel);
            HideView(_enterView);
            HideView(_selectView);
            HideView(_selectRoleView);
            HideView(_createRoleView);
        }

        private static void HideView(BaseView view)
        {
            if (view != null) view.Hide();
        }
    }
}
