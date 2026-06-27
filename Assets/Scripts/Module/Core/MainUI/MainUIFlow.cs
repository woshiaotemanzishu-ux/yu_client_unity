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
            EventDispatcher.On<long, long>(GlobalEvent.EVT_COMBAT_POWER_UP, OnCombatPowerUp);
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

            ReleaseFightingUp();

            if (_moduleRoot == null) return;
            ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
        }

        // ---------- 战力提升弹层(对标老端 MainUIController 绑 mainRoleVo "fighting" 变化 → FightingUpView.Open) ----------
        // FightingUpView 是独立 prefab(不在 MainUIModule 内、不进 FirstPass),首次需要时按需实例化并缓存复用。

        private static FightingUpView _fightingUpView;
        private static bool _fightingUpLoading;
        private static long _pendingOldFight;
        private static long _pendingNewFight;
        private static bool _hasPendingFight;

        /// <summary>收到战力上升事件:弹/刷新「战力提升」窗(对标老端 change_fighting_func)。</summary>
        private static void OnCombatPowerUp(long oldFight, long newFight)
        {
            _ = ShowFightingUpAsync(oldFight, newFight);
        }

        private static async Task ShowFightingUpAsync(long oldFight, long newFight)
        {
            if (oldFight <= 0 || newFight <= oldFight) return;   // 对标老端 old_fight != 0 && new > old

            if (_fightingUpView != null)
            {
                ShowFightingUpInstance(oldFight, newFight);
                return;
            }

            // 加载期间又来新的战力变化:保留最早的旧值、取最新的新值,加载完成后一次性刷新(简化老端的逐次累加)。
            _pendingOldFight = _hasPendingFight ? _pendingOldFight : oldFight;
            _pendingNewFight = newFight;
            _hasPendingFight = true;
            if (_fightingUpLoading) return;

            _fightingUpLoading = true;
            string key = GameResPath.GetUIPrefab(MODULE, "FightingUpView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _fightingUpLoading = false;

            if (go == null)
            {
                GameLog.Warn("MainUI", "FightingUpView 预制加载失败: {0}(检查 addressable/转换)", key);
                _hasPendingFight = false;
                return;
            }

            _fightingUpView = go.GetComponent<FightingUpView>();
            if (_fightingUpView == null)
            {
                GameLog.Warn("MainUI", "FightingUpView 预制缺 FightingUpView 组件(重跑 mainUI 回填)");
                ResManager.ReleaseInstance(go);
                _hasPendingFight = false;
                return;
            }

            long o = _pendingOldFight, n = _pendingNewFight;
            _hasPendingFight = false;
            ShowFightingUpInstance(o, n);
        }

        private static void ShowFightingUpInstance(long oldFight, long newFight)
        {
            if (_fightingUpView == null) return;
            if (_fightingUpView.IsShown)
                _fightingUpView.SetData(oldFight, newFight);   // 已开:刷新数值(对标 SetAnimation)
            else
                _fightingUpView.Show(new FightUpData { OldFight = oldFight, NewFight = newFight });
        }

        private static void ReleaseFightingUp()
        {
            _hasPendingFight = false;
            if (_fightingUpView == null) return;
            GameObject go = _fightingUpView.gameObject;
            _fightingUpView = null;
            if (go != null) ResManager.ReleaseInstance(go);
        }
    }
}
