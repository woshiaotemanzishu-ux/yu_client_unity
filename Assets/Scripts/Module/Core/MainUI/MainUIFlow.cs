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
            typeof(MainUIRankViewBind),
            typeof(MainUINoticeViewBind), // 通知位(老端 _box_notice 簇,拆自 Secondary;槽位式独立区域 HudNotice)
            typeof(Shenxiao.Generated.UI.FunctionOpen.FunctionOpenIconBind), // 功能预告框(HudFuncOpen 区域;原 FunctionOpenFlow 单独挂载已退役)
            typeof(MainUISkillViewBind),
            typeof(MainUIChatViewBind),
            typeof(MainUISecondaryViewBind),
            typeof(MainUITaskTeamViewBind),
            typeof(MainUIDownViewBind),
            typeof(MainUIAutoBrushViewBind),
            typeof(MainUIFoldViewBind), // 折叠太极协调器(总装层),需 Show 才能绑定点击
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
            ReleaseCollectBarView();
            ReleaseReliveView();

            if (_moduleRoot == null) return;
            ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
        }

        // ---------- 采集进度条(独立 prefab mainUI/CollectBarView,不在 MainUIModule 内,采集事件触发时按需加载缓存) ----------
        // 对标老端 CollectBarView 由采集事件 new+Open;Unity 转换产物是独立 prefab(同 FightingUpView),挂 Main 层。

        private static CollectBarView _collectBarView;
        private static bool _collectBarLoading;

        /// <summary>取采集进度条实例(独立 prefab mainUI/CollectBarView,首次按需加载缓存,默认隐藏)。加载中/失败返回 null。</summary>
        public static async Task<CollectBarView> EnsureCollectBarViewAsync()
        {
            if (_collectBarView != null) return _collectBarView;
            if (_collectBarLoading) return null;

            _collectBarLoading = true;
            int token = _requestToken; // 加载在途遇断线/teardown(OnDisconnected ++_requestToken)→ 弃用本次结果
            string key = GameResPath.GetUIPrefab(MODULE, "CollectBarView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Main));
            _collectBarLoading = false;

            if (token != _requestToken)
            {
                // 加载期间断线:释放刚实例化的对象,不缓存陈旧实例(否则会跨重连存活、泄漏)。
                if (go != null) ResManager.ReleaseInstance(go);
                return null;
            }

            if (go == null)
            {
                GameLog.Warn("MainUI", "CollectBarView 预制加载失败: {0}(检查 addressable/转换)", key);
                return null;
            }

            _collectBarView = go.GetComponent<CollectBarView>();
            if (_collectBarView == null)
            {
                GameLog.Warn("MainUI", "CollectBarView 预制缺 CollectBarView 组件(重跑 mainUI 回填)");
                ResManager.ReleaseInstance(go);
                return null;
            }

            go.SetActive(false); // 默认隐藏,等采集 START 再 Show(对标老端 display_obj.visible=false)
            return _collectBarView;
        }

        /// <summary>已加载的采集进度条(未加载返回 null),供采集取消/完成时同步隐藏,不触发加载。</summary>
        public static CollectBarView CollectBarViewOrNull => _collectBarView;

        private static void ReleaseCollectBarView()
        {
            _collectBarLoading = false;
            if (_collectBarView == null) return;
            GameObject go = _collectBarView.gameObject;
            _collectBarView = null;
            if (go != null) ResManager.ReleaseInstance(go);
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

        // ---------- 复活倒计时窗(对标老端 MainUIReliveView;独立 prefab,不在 MainUIModule 内,
        //            事件驱动按需加载缓存,同 CollectBarView/FightingUpView 既定模式) ----------
        // prefab 由 Assets/Editor/UiCreator/MainUI/MainUIReliveCreator.cs 生成(从 HudOverlayCombat 捆包
        // 抽取既有 BuildRelive 子树);真机包前需跑一次「神霄/资源/Addressable 自动分组」注册地址。

        private static MainUIReliveView _reliveView;
        private static bool _reliveLoading;

        /// <summary>打开复活倒计时窗(ReliveController.OpenReliveWindow 兜底分支调用)。</summary>
        public static async Task ShowReliveAsync()
        {
            if (_reliveView != null) { _reliveView.Show(); return; }
            if (_reliveLoading) return;

            _reliveLoading = true;
            int token = _requestToken;
            string key = GameResPath.GetUIPrefab(MODULE, "MainUIReliveView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _reliveLoading = false;

            if (token != _requestToken)
            {
                if (go != null) ResManager.ReleaseInstance(go);
                return;
            }
            if (go == null)
            {
                GameLog.Warn("MainUI", "MainUIReliveView 预制加载失败: {0}(检查 Addressable 自动分组是否已跑)", key);
                return;
            }

            _reliveView = go.GetComponent<MainUIReliveView>();
            if (_reliveView == null)
            {
                GameLog.Warn("MainUI", "MainUIReliveView 预制缺 MainUIReliveView 组件(重跑 MainUIReliveCreator)");
                ResManager.ReleaseInstance(go);
                return;
            }
            _reliveView.Show();
        }

        private static void ReleaseReliveView()
        {
            _reliveLoading = false;
            if (_reliveView == null) return;
            GameObject go = _reliveView.gameObject;
            _reliveView = null;
            if (go != null) ResManager.ReleaseInstance(go);
        }
    }
}
