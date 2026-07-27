using System;
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
    /// 登录模块 UI 流程编排(重构版:独立 LoginStage 外壳 + 6 个页面 prefab + 1 个连接等待层)。
    /// 链路严格对齐老客户端:
    ///   ① 加载页 LoadingView(真实资源下载进度)
    ///   ② 登录/注册页 LoginPanelView(登录⇄注册子面板)
    ///   ③ 登录成功 → ServerEnterView(显示当前服 + 踏入仙界 + 用户协议弹层)
    ///   ④ 点服务器名 → ServerSelectView 列表选服
    ///   ⑤ 踏入仙界 → get_server_info → WebSocket → 10000 → 选角 RoleSelectView / 创角 RoleCreateView → 进游戏
    /// 页面自身仍按 720x1280 原样渲染;LoginStage 只在外部补 Web 背景和居中视口。
    /// 协议弹层并进 ServerEnterView。
    /// </summary>
    public static class LoginFlow
    {
        private static AppConfig _config;
        private static LoginStage _stage;             // 全屏 Web 背景 + 居中 720x1280 视口
        private static LoginPanelView _loginPanel;     // ② 登录 + 注册
        private static LoadingView _loadingView;       // ① 加载
        private static ServerEnterView _enterView;     // ③ 踏入仙界 + 协议弹层
        private static ServerSelectView _selectView;   // ④ 选服
        private static RoleSelectView _selectRoleView; // ⑤ 选角
        private static RoleCreateView _createRoleView; // ⑤ 创角
        private static WaitforOpenViewLoading _waitConnectView; // ⑤ 解析入口/连服/等待 10000 的小转圈
        private static TaskCompletionSource<bool> _roleListCompletion;
        private static bool _awaitingRoleListResponse;
        private static bool _busy;

        private const int ConnectLoadingSource = 0x4C4F474E; // "LOGN",稳定标识 LoginFlow 的 loading 源
        private const int ConnectTimeoutMs = 15000;

        /// <summary>
        /// 用户协议勾选状态(会话内)。持久化按账号记录,对标老客户端:进入踏入仙界页瞬间,该账号同意过 →
        /// 自动勾选;否则立即弹协议层;弹层点同意 → 勾选 + 记录账号 + 直接进入游戏;拒绝 → 仅关闭。
        /// </summary>
        public static bool AgreementAgreed { get; private set; }

        private static string AgreedPrefKey => "login.agreed." + LoginController.Instance.Model.PlayerId;

        public static async Task StartAsync(AppConfig config)
        {
            _config = config;

            // LoginStage 自身铺满 Window 层;6 个原有页面只加载到固定 720x1280 视口内。
            Shenxiao.Framework.UI.BootOverlay.Report(0.90f, "正在加载登录界面…");
            _stage = await LoadStageAsync();
            if (_stage == null)
            {
                return;
            }

            _loginPanel = await LoadViewAsync<LoginPanelView>("LoginPanel");
            _loadingView = await LoadViewAsync<LoadingView>("LoadingView");
            Shenxiao.Framework.UI.BootOverlay.Report(0.94f, "正在加载登录界面…");
            _enterView = await LoadViewAsync<ServerEnterView>("ServerEnterView");
            _selectView = await LoadViewAsync<ServerSelectView>("ServerSelectView");
            _waitConnectView = await LoadViewAsync<WaitforOpenViewLoading>("WaitforOpenViewLoading", false);
            Shenxiao.Framework.UI.BootOverlay.Report(0.97f, "正在加载登录界面…");
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
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnectedDuringLogin);
            EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_ENTITIES_READY, OnSceneEntitiesReady);
            LegacyPreloadService.ProgressChanged += OnPreloadProgress;

            // ---------- ① 加载页 ----------
            _loadingView.Show();
            _loadingView.SetProgress(0f);
            await Task.Yield(); // 游戏自己的加载页渲染出第一帧后,页面 HTML 加载层才可撤(无缝交接)
            Shenxiao.Framework.UI.BootOverlay.Done();
            await PreloadAsync();
            _loadingView.SetProgress(1f);
            await Task.Yield();
            _loadingView.Hide();

            // ---------- ② 登录页 ----------
            ShowLogin();
        }

        /// <summary>加载登录外部舞台。它只负责 Web 背景和固定视口,不包含任何登录页内容。</summary>
        private static async Task<LoginStage> LoadStageAsync()
        {
            string key = GameResPath.GetUIPrefab("login", "LoginStage");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            LoginStage stage = go != null ? go.GetComponent<LoginStage>() : null;
            if (stage == null || stage.viewport == null)
            {
                GameLog.Error("Login", "LoginStage prefab 加载失败或缺 Viewport(key={0})。在「神霄/重构UI 生成器」里只重建 LoginStage", key);
                if (go != null) ResManager.ReleaseInstance(go);
                return null;
            }

            go.transform.SetAsFirstSibling();
            return stage;
        }

        /// <summary>按视图名实例化对应独立 prefab 并取组件(失败返回 null)。</summary>
        private static async Task<T> LoadViewAsync<T>(string viewName, bool required = true) where T : BaseView
        {
            string key = GameResPath.GetUIPrefab("login", viewName);
            GameObject go = await ResManager.InstantiateAsync(key, _stage.viewport);
            T view = go != null ? go.GetComponent<T>() : null;
            if (view == null)
            {
                if (required)
                    GameLog.Error("Login", "{0} prefab 加载失败(key={1})。先在「神霄/重构UI 生成器」里生成它", viewName, key);
                else
                    GameLog.Warn("Login", "{0} prefab 尚未生成(key={1})，连接过程暂退回文字提示", viewName, key);
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
            return await WithTimeout(task, what, _config != null ? _config.gmApiUrl : string.Empty,
                what + "超时:账号服务器不可达");
        }

        private static async Task<LoginRequestResult> WithTimeout(
            Task<LoginRequestResult> task, string what, string target, string timeoutMessage)
        {
            // ⚠ Task.Delay 在 WebGL 永不醒 → 超时保护会失效(接口挂了就永等);用跨平台 Delay。
            Task finished = await Task.WhenAny(task, TimeUtil.Delay(ConnectTimeoutMs));
            if (finished != task)
            {
                GameLog.Error("Login", "{0}超时:target={1}", what, target);
                return LoginRequestResult.Fail(timeoutMessage);
            }
            return await task;
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
            TaskCompletionSource<bool> roleListCompletion = null;
            try
            {
                ShowConnectLoading("正在获取服务器入口...");
                LoginRequestResult result = await WithTimeout(
                    LoginController.Instance.ResolveSelectedServerEndpointAsync(),
                    "解析服务器入口",
                    _config != null ? _config.gmApiUrl : string.Empty,
                    "获取服务器入口超时");
                if (!result.success)
                {
                    ShowConnectRetry("获取服务器入口失败。", result.message);
                    return;
                }

                // 必须在发 10000 前建 waiter：本机服回包可能快于 ConnectGameAsync 的主线程续体。
                roleListCompletion = new TaskCompletionSource<bool>();
                _roleListCompletion = roleListCompletion;
                _awaitingRoleListResponse = false;

                LoginServerInfo server = LoginController.Instance.Model.SelectedServer;
                string endpoint = server != null ? server.host + ":" + server.port : "unknown";
                ShowConnectLoading("正在连接服务器...");
                result = await WithTimeout(
                    LoginController.Instance.ConnectGameAsync(),
                    "连接游戏服",
                    endpoint,
                    "连接游戏服超时");
                if (!result.success)
                {
                    ClearRoleListWaiter(roleListCompletion);
                    await Shenxiao.Framework.Net.NetManager.DisconnectAsync();
                    ShowConnectRetry("服务器连接失败，请检查网络连接。", result.message);
                    return;
                }

                // 极快回包可能已由 OnRoleList 完成；此时不要把刚撤下的转圈再次显示出来。
                if (roleListCompletion.Task.IsCompleted)
                {
                    await roleListCompletion.Task;
                    return;
                }

                _awaitingRoleListResponse = true;
                ShowConnectLoading("正在获取角色数据...");
                Task finished = await Task.WhenAny(roleListCompletion.Task, TimeUtil.Delay(ConnectTimeoutMs));
                if (finished != roleListCompletion.Task)
                {
                    GameLog.Error("Login", "账号登录 10000 回包超时:target={0}", endpoint);
                    _awaitingRoleListResponse = false;
                    ClearRoleListWaiter(roleListCompletion);
                    await Shenxiao.Framework.Net.NetManager.DisconnectAsync();
                    ShowConnectRetry("服务器响应超时，请检查网络连接。", "15 秒内未收到角色列表");
                    return;
                }

                bool received = await roleListCompletion.Task;
                if (!received)
                {
                    ShowConnectRetry("与服务器的连接已中断，请检查网络连接。", "等待角色列表期间连接断开");
                }
            }
            catch (Exception e)
            {
                GameLog.Error("Login", "进入游戏连接流程异常: {0}", e);
                ClearRoleListWaiter(roleListCompletion);
                await Shenxiao.Framework.Net.NetManager.DisconnectAsync();
                ShowConnectRetry("服务器连接失败，请检查网络连接。", e.Message);
            }
            finally
            {
                ClearRoleListWaiter(roleListCompletion);
                if (!_awaitingRoleListResponse) HideConnectLoading();
                _busy = false;
            }
        }

        private static void ShowConnectLoading(string text)
        {
            if (_waitConnectView == null)
            {
                TipsManager.Toast(text);
                return;
            }

            _waitConnectView.SetText(text);
            _waitConnectView.Show(ConnectLoadingSource); // 同源重复 Show 会刷新 15s 过期时间
        }

        private static void HideConnectLoading()
        {
            _waitConnectView?.Hide(ConnectLoadingSource);
        }

        private static void ShowConnectRetry(string playerMessage, string detail)
        {
            HideConnectLoading();
            GameLog.Warn("Login", "{0} detail={1}", playerMessage, detail);
            TipsManager.Confirm(playerMessage + "\n\n是否重新连接？",
                () => { _ = EnterGameAsync(); });
        }

        private static void ClearRoleListWaiter(TaskCompletionSource<bool> expected)
        {
            if (expected == null || _roleListCompletion != expected) return;
            _roleListCompletion = null;
            _awaitingRoleListResponse = false;
        }

        /// <summary>角色列表到达 → 有角色进选角页,无角色进创角页(对标老客户端 On10000 分流)。</summary>
        private static void OnRoleList(int roleCount)
        {
            _awaitingRoleListResponse = false;
            HideConnectLoading();
            TaskCompletionSource<bool> completion = _roleListCompletion;
            _roleListCompletion = null;
            completion?.TrySetResult(true);
            _ = OnRoleListAsync(roleCount);
        }

        private static void OnNetDisconnectedDuringLogin()
        {
            if (!_awaitingRoleListResponse) return;

            _awaitingRoleListResponse = false;
            HideConnectLoading();
            TaskCompletionSource<bool> completion = _roleListCompletion;
            _roleListCompletion = null;
            completion?.TrySetResult(false);
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
            _awaitingRoleListResponse = false;
            _roleListCompletion = null;
            HideConnectLoading();
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
            HideConnectLoading();
            HideView(_loginPanel);
            HideView(_enterView);
            HideView(_selectView);
            HideView(_selectRoleView);
            HideView(_createRoleView);
            if (_loadingView == null) return; // 登录模块已退役(异常重入防御)
            _loadingView.Show();
            _loadingView.SetProgress(0f, "加载游戏资源");
            GameLog.Info("Login", "进入游戏成功,等待 GAME_START 资源接管完成");
        }

        private static void OnGameStart()
        {
            // 加载页留任到场景首屏就绪:GAME_START 时 12005 还没发,这里就撤页会让玩家
            // 盯着黑地图等瓦片/怪物蹦出来(实测 5-10s 裸奔)。等 EVT_SCENE_FIRST_SCREEN_READY 揭幕。
            HideView(_loginPanel);
            HideView(_enterView);
            HideView(_selectView);
            HideView(_selectRoleView);
            HideView(_createRoleView);
            HideConnectLoading();
            if (_loadingView != null) _loadingView.SetProgress(1f, "进入场景");
            _ = HideLoadingFallbackAsync();
            GameLog.Info("Login", "—— 🎉 GAME_START:登录模块退下,等场景首屏就绪揭幕 ——");
            // 老端"进包免下载"清单改为进游戏后后台静默预取(Boot 不再硬下 100MB,详见 LegacyPreloadService)。
            _ = LegacyPreloadService.BackgroundPrefetchLegacyAsync();
            MemoryReport.ScheduleAfterGameStart(); // Development 构建:world+30s/120s 内存归因 dump(Release no-op)
        }

        private static void OnSceneEntitiesReady()
        {
            // 首屏实体(主角/怪/NPC)全部立起才揭幕——只等瓦片的话玩家会盯着实体逐个蹦 3-5 秒。
            HideView(_loadingView);
            // 登录模块整体退役:六个登录 prefab(含 ui_Login_bg1/bg2/load_bg0 等 4 张 5MB 级大图与其
            // bundle)Hide 只是隐藏,实例与纹理整局驻留(MemReport 实测 ~21MB)。WebGL 断线=页面级重载+
            // 游戏内静默重连,本会话不会再回登录页,销毁归还。重复触发(每次切图)时字段已空,无害。
            ReleaseLoginViews();
        }

        private static void ReleaseLoginViews()
        {
            if (_stage == null && _loginPanel == null && _loadingView == null) return;
            ReleaseView(ref _loginPanel);
            ReleaseView(ref _enterView);
            ReleaseView(ref _selectView);
            ReleaseView(ref _selectRoleView);
            ReleaseView(ref _createRoleView);
            ReleaseView(ref _waitConnectView);
            ReleaseView(ref _loadingView);
            if (_stage != null)
            {
                ResManager.ReleaseInstance(_stage.gameObject);
                _stage = null;
            }
            EventDispatcher.Off(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnectedDuringLogin);
            _roleListCompletion = null;
            _awaitingRoleListResponse = false;
            LegacyPreloadService.ProgressChanged -= OnPreloadProgress;
            GameLog.Info("Login", "login views released(进世界,登录模块退役归还纹理/bundle)");
        }

        private static void ReleaseView<T>(ref T view) where T : BaseView
        {
            if (view == null) return;
            ResManager.ReleaseInstance(view.gameObject);
            view = null;
        }

        /// <summary>首屏就绪事件缺席(异常流程)时 12s 兜底撤加载页,别把玩家关在门外。</summary>
        private static async Task HideLoadingFallbackAsync()
        {
            await TimeUtil.Delay(12000);
            if (_loadingView != null && _loadingView.gameObject.activeInHierarchy)
            {
                GameLog.Warn("Login", "首屏就绪事件超时,加载页兜底撤下");
                HideView(_loadingView);
            }
        }

        private static void OnPreloadProgress(LegacyPreloadStage stage, float progress, string hint)
        {
            if (_loadingView == null || !_loadingView.gameObject.activeInHierarchy) return;
            _loadingView.SetProgress(progress, hint);
        }

        private static void HideAllViews()
        {
            HideConnectLoading();
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
