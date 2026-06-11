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
    /// 登录模块 UI 流程编排(模块合并 prefab 的运行时模型):
    /// 整个 LoginModule.prefab 一次实例化,子窗口由本类 Show/Hide。
    ///
    /// 流程(对标 Laya LoginStateManager,账号由 AppConfig.devAccount 顶替平台 SDK):
    ///   启动 → BgView+EnterView → 后台 player_login(自动注册,拿服务器列表)
    ///   → 点换区开 SelectServerView(真实列表) → 点「踏入仙界」
    ///   → get_server_info → WebSocket → 10000 → 角色列表 → 提示结果(创角/选角窗待接)。
    /// </summary>
    public static class LoginFlow
    {
        private static AppConfig _config;
        private static GameObject _moduleRoot;
        private static LoginBgView _bg;
        private static LoginEnterView _enter;
        private static LoginSelectServerView _select;
        private static bool _entering;

        public static async Task StartAsync(AppConfig config)
        {
            _config = config;
            string key = GameResPath.GetUIPrefab("login", "LoginModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            if (_moduleRoot == null)
            {
                GameLog.Error("Login", "LoginModule prefab 加载失败(key={0})。先跑 LayaUI 转换 + Addressables 分组,Editor 下有 fallback", key);
                return;
            }

            // 模块内全部窗口先隐藏,再按流程打开
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
                if (v is LoginBgView bg) _bg = bg;
                else if (v is LoginEnterView enter) _enter = enter;
                else if (v is LoginSelectServerView select) _select = select;
            }
            if (_bg == null || _enter == null || _select == null)
            {
                GameLog.Error("Login", "LoginModule 缺业务窗口(bg={0} enter={1} select={2})——回填 Bind 后业务类才会挂上,先在转换器点『回填 Bind 引用』",
                    _bg != null, _enter != null, _select != null);
                return;
            }

            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_ROLE_LIST, OnRoleList);

            _bg.Show();
            _enter.Show();
            _enter.SetTip("登录中 ...");

            LoginRequestResult result = await LoginController.Instance.DevLoginAsync(_config.devAccount);
            if (!result.success)
            {
                GameLog.Error("Login", "HTTP 登录失败: {0}", result.message);
                _enter.SetTip("登录失败: " + result.message);
                return;
            }
            GameLog.Info("Login", "HTTP 登录通过 player_id={0} 服务器数={1}",
                LoginController.Instance.Model.PlayerId, LoginController.Instance.Model.Servers.Count);
            _enter.RefreshServer();
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
            GameLog.Info("Login", "—— ✅ 真实链路全通:UI → HTTP 登录 → 选服 → 入口 → WebSocket → 角色列表(角色数={0})——", roleCount);
            if (_enter != null)
            {
                _enter.SetTip(roleCount > 0
                    ? $"连接成功,已有 {roleCount} 个角色(选角窗待接)"
                    : "连接成功,该服暂无角色(创角窗待接)");
            }
        }
    }
}
