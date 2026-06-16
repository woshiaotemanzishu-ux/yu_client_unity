using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Opens MainUI after role data is ready, matching the old client's GAME_START handoff.
    /// 只做编排:实例化 MainUIModule → 收集模块内所有 BaseView → 全部关闭 →
    /// 按老客户端 InitMainUI 顺序 Show 首批视图。各视图自身的初始状态
    /// (对标老客户端 LoadSuccess)落在 Views/ 下的业务 View 子类的 OnInit 里。
    /// </summary>
    public static class MainUIFlow
    {
        private const string MODULE = "mainUI";
        private const string PREFAB = "MainUIModule";

        /// <summary>
        /// 首批打开的视图,顺序对齐老客户端 MainUIController.InitMainUI(MainUIController.ts:651-707);
        /// Show 顺序即渲染顺序(BaseView.Show 置顶)。事件驱动的弹层/特效视图默认关闭,
        /// 不在此列(本轮不动 manifest/转换器的默认关闭逻辑)。
        /// 用生成 Bind 类型匹配:业务 View 子类继承自对应 Bind,回填前后都能命中。
        /// </summary>
        private static readonly Type[] FirstPassViews =
        {
            typeof(MainUITopViewBind),
            typeof(MainUIActivityViewBind),
            typeof(MainUISkillViewBind),
            typeof(MainUIChatViewBind),
            typeof(MainUISecondaryViewBind),
            typeof(MainUITaskTeamViewBind),
            typeof(MainUIDownViewBind),
            typeof(MainUIAutoBrushViewBind),
            typeof(UIJoyStickBind),
        };

        private static GameObject _moduleRoot;
        private static bool _loading;
        private static int _requestToken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnGameStart()
        {
            _ = ShowAsync();
        }

        private static async Task ShowAsync()
        {
            if (_moduleRoot != null)
            {
                _moduleRoot.SetActive(true);
                ShowFirstPassViews(_moduleRoot.transform);
                return;
            }

            if (_loading) return;
            _loading = true;
            int token = ++_requestToken;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Main));
            _loading = false;

            if (token != _requestToken)
            {
                if (root != null)
                {
                    ResManager.ReleaseInstance(root);
                }
                return;
            }

            if (root == null)
            {
                GameLog.Error("MainUI", "MainUIModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;
            ShowFirstPassViews(_moduleRoot.transform);
            GameLog.Info("MainUI", "MainUI module opened: {0}", key);
        }

        /// <summary>
        /// 对齐 LoginFlow:收集模块内所有 BaseView 并全部关闭,再按老客户端 InitMainUI
        /// 顺序 Show 首批视图(走 BaseView.Show → OnInit,各视图自身初始状态在其
        /// 业务 View 子类的 OnInit 里完成)。事件驱动视图保持关闭。
        /// 不用 transform.Find 取子窗口;不在此处改 Bind 字段(归各业务 View)。
        /// </summary>
        private static void ShowFirstPassViews(Transform root)
        {
            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
            }

            foreach (Type bindType in FirstPassViews)
            {
                BaseView view = FindView(views, bindType);
                if (view == null)
                {
                    GameLog.Warn("MainUI", "MainUIModule 缺首批视图: {0}(重跑 mainUI 流水线:转换+回填)", bindType.Name);
                    continue;
                }
                view.Show();
            }
        }

        private static BaseView FindView(BaseView[] views, Type bindType)
        {
            foreach (BaseView v in views)
            {
                if (v != null && bindType.IsInstanceOfType(v))
                {
                    return v;
                }
            }
            return null;
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                GameLog.Info("MainUI", "keep MainUI during in-game reconnect");
                return;
            }

            ++_requestToken;
            _loading = false;

            if (_moduleRoot == null) return;
            ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
        }
    }
}
