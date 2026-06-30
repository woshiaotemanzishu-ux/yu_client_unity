using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// 把 FunctionOpenModule.prefab 挂到主界面层,只显示功能预告入口框(FunctionOpenIcon);
    /// 列表/弹窗按需由 MainUIRouter 打开(Stage 2)。对标老端 MainUIController 创建 FunctionOpenIcon 的编排,
    /// 挂载套路同 MainUIFlow(EVT_GAME_START 实例化 module → 关闭全部 view → 只 Show Icon)。
    /// </summary>
    public static class FunctionOpenFlow
    {
        private const string MODULE = "functionopen";
        private const string PREFAB = "FunctionOpenModule";

        private static GameObject _moduleRoot;
        private static bool _loading;
        private static int _token;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnGameStart() => _ = EnsureAsync();

        private static async Task EnsureAsync()
        {
            if (_moduleRoot != null) { _moduleRoot.SetActive(true); ShowIcon(_moduleRoot.transform); return; }
            if (_loading) return;
            _loading = true;
            int token = ++_token;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Main));
            _loading = false;
            if (token != _token) { if (root != null) ResManager.ReleaseInstance(root); return; }
            if (root == null) { GameLog.Error("FuncOpen", "FunctionOpenModule prefab load failed: {0}", key); return; }
            _moduleRoot = root;
            _moduleRoot.name = PREFAB;
            ShowIcon(_moduleRoot.transform);
            GameLog.Info("FuncOpen", "FunctionOpen module opened: {0}", key);
        }

        // 模块内含 Icon/ListView/ListItem/AutoView/TipView 多个 view;只显示入口图标,其余关闭(列表按需 Router 打开)。
        private static void ShowIcon(Transform root)
        {
            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);
            foreach (BaseView v in views)
                if (v is FunctionOpenIcon icon) { icon.Show(); return; }
            GameLog.Warn("FuncOpen", "FunctionOpenModule 缺 FunctionOpenIcon 业务组件(prefab 节点需回填业务 View 类:把 FunctionOpenIconBind 换成 FunctionOpenIcon)");
        }

        private static void OnDisconnected()
        {
            // 游戏内自动重连:保留模块(对标 MainUIFlow.OnDisconnected);否则断线一下功能预告框就没了、重连也不重建。
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                GameLog.Info("FuncOpen", "keep FunctionOpen during in-game reconnect");
                return;
            }
            ++_token;
            _loading = false;
            if (_moduleRoot == null) return;
            ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
        }
    }
}
