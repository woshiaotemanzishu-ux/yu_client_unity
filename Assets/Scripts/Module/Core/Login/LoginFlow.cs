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
    /// 登录模块 UI 流程编排(对标 Laya LoginManager 的时序,模块合并 prefab 模型):
    ///
    ///   阶段① 加载页:只显示 LoginLoadingView,真实驱动进度
    ///        (Addressables 预下载 0~60% → HTTP player_login 60~90% → 收尾 100%)
    ///   阶段② 登录页:LoginBgView + LoginEnterView(服务器名已就绪)
    ///   阶段③ 选服/进服:换区弹 LoginSelectServerView;踏入仙界 → 入口解析 →
    ///        WebSocket → 10000 → 角色列表(创角/选角窗待接)
    ///
    /// 账号由 AppConfig.devAccount 顶替平台 SDK(与 Laya 正式流程一致:UI 无账号输入)。
    /// </summary>
    public static class LoginFlow
    {
        private static AppConfig _config;
        private static GameObject _moduleRoot;
        private static LoginBgView _bg;
        private static LoginEnterView _enter;
        private static LoginSelectServerView _select;
        private static LoginLoadingView _loading;
        private static bool _entering;

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
                else if (v is LoginEnterView enter) _enter = enter;
                else if (v is LoginSelectServerView select) _select = select;
                else if (v is LoginLoadingView loading) _loading = loading;
            }
            if (_bg == null || _enter == null || _select == null || _loading == null)
            {
                GameLog.Error("Login", "LoginModule 缺业务窗口(bg={0} enter={1} select={2} loading={3})——先在转换器点『回填 Bind 引用』",
                    _bg != null, _enter != null, _select != null, _loading != null);
                return;
            }

            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleList);

            // ---------- 阶段① 加载页 ----------
            _loading.Show();
            _loading.SetProgress(0f);

            await PreloadAsync();          // 0 ~ 0.6
            bool loginOk = await HttpLoginAsync();  // 0.6 ~ 0.9
            if (!loginOk) return;          // 失败留在加载页,文案已说明原因
            _loading.SetProgress(1f);
            await Task.Yield();

            // ---------- 阶段② 登录页 ----------
            _loading.Hide();
            _bg.Show();
            _enter.Show();
        }

        private static async Task PreloadAsync()
        {
            string[] keys = _config.preloadKeys;
            if (keys == null || keys.Length == 0)
            {
                _loading.SetProgress(0.6f);
                return;
            }
            try
            {
                long size = await ResManager.GetDownloadSize(keys);
                if (size > 0)
                {
                    GameLog.Info("Login", "预下载 {0} 项,共 {1} KB", keys.Length, size / 1024);
                    await ResManager.DownloadAsync(keys, p => _loading.SetProgress(p * 0.6f));
                }
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Login", "预下载跳过: {0}", e.Message);
            }
            _loading.SetProgress(0.6f);
        }

        private static async Task<bool> HttpLoginAsync()
        {
            _loading.SetProgress(0.6f, "登录账号 ...");
            Task<LoginRequestResult> loginTask = LoginController.Instance.DevLoginAsync(_config.devAccount);
            // 看门狗:服务器不可达时 TCP 层可能长时间无响应,别让玩家对着"登录账号..."干等
            Task finished = await Task.WhenAny(loginTask, Task.Delay(15000));
            if (finished != loginTask)
            {
                GameLog.Error("Login", "登录超时:{0} 不可达(浏览器开 {0} 应显示 API is ready;检查 yu_gm 服务/防火墙)", _config.gmApiUrl);
                _loading.SetProgress(0.6f, "登录超时:账号服务器不可达");
                return false;
            }
            LoginRequestResult result = loginTask.Result;
            if (!result.success)
            {
                GameLog.Error("Login", "HTTP 登录失败: {0}", result.message);
                _loading.SetProgress(0.6f, "登录失败: " + result.message);
                return false;
            }
            GameLog.Info("Login", "HTTP 登录通过 player_id={0} 服务器数={1}",
                LoginController.Instance.Model.PlayerId, LoginController.Instance.Model.Servers.Count);
            _loading.SetProgress(0.9f);
            return true;
        }

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
            if (_entering) return;
            _entering = true;
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
                _entering = false;
            }
        }

        private static void OnRoleList(int roleCount)
        {
            GameLog.Info("Login", "—— ✅ 真实链路全通:加载页 → 登录页 → 选服 → 入口 → WebSocket → 角色列表(角色数={0})——", roleCount);
            if (_enter != null)
            {
                _enter.SetTip(roleCount > 0
                    ? $"连接成功,已有 {roleCount} 个角色(选角窗待接)"
                    : "连接成功,该服暂无角色(创角窗待接)");
            }
        }
    }
}
