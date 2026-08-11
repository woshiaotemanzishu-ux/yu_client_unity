using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>主界面「秘宝/通灵」共享总窗：BaseWindowSkin + 五个真实内容页。</summary>
    public static class RuneFlow
    {
        private const string RuneModule = "rune";
        private const string RunePrefab = "RuneModule";

        private static GameObject _frameRoot;
        private static GameObject _moduleRoot;
        private static BaseWindowSkinView _window;
        private static RuneMainUIView _mainView;
        private static RuneBagView _bagView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) Close();
            else Open();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            HideSubViews();
            if (_mainView != null && _mainView.IsShown) _mainView.Hide();
            if (_window != null) _window.Hide();
        }

        public static void OpenSub(string viewTypeName)
        {
            BaseView view = FindSub(viewTypeName);
            if (view == null)
            {
                GameLog.Error("Rune", "缺少子页: {0}", viewTypeName);
                return;
            }
            PrepareSubView(view);
            view.Show();
        }

        public static void ToggleSub(string viewTypeName)
        {
            BaseView view = FindSub(viewTypeName);
            if (view == null)
            {
                GameLog.Error("Rune", "缺少子页: {0}", viewTypeName);
                return;
            }
            if (view.IsShown) view.Hide();
            else
            {
                PrepareSubView(view);
                view.Show();
            }
        }

        public static void OpenRuneBag(int position, bool replace)
        {
            if (_bagView == null)
            {
                GameLog.Error("Rune", "RuneModule 缺 RuneBagView，需运行 RuneAssetPreflight 重绑");
                return;
            }
            PrepareSubView(_bagView);
            _bagView.Show(new RuneBagView.OpenArgs(Mathf.Clamp(position, 1, 10), replace));
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                await Task.WhenAll(RuneConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
                if (!RuneConfigs.IsLoaded)
                {
                    TipsManager.Toast("九霄劫魄配置加载失败");
                    return;
                }
                RuneController.Instance.Init();
                if (!await EnsureViewAsync()) return;

                _window.SetReturnAction(Close);
                _window.Show();
                _window.Configure(BuildTabs(), 0);
                RuneController.Instance.RequestStartup();
                RuneController.Instance.RequestRuneBag();
            }
            finally
            {
                _loading = false;
            }
        }

        private static IList<TabSpec> BuildTabs() => new[]
        {
            new TabSpec
            {
                Enabled = true,
                Label = "九霄劫魄",
                TitleImagePath = GameResPath.GetIcon("rune", "rune_title"),
                BackgroundImagePath = GameResPath.GetBigBgPath("ui_rare_bg.jpg"),
                ContentFactory = ReparentRune,
            },
            PendingTab(1, "山海物华阁", "monBook_001", "daily_bg.jpg"),
            PendingTab(2, "苍龙神章", "lung_title", "uilwmb_013a.jpg"),
            PendingTab(3, "荒祖遗骸", "uiss_001", "ui_rare_bg3.jpg"),
            PendingTab(4, "九霄冥饰", "uihs_001", "ui_rare_bg4.jpg"),
        };

        private static TabSpec PendingTab(int index, string label, string title, string background) =>
            new TabSpec
            {
                Enabled = true,
                Label = label,
                TitleImagePath = GameResPath.GetIcon("rune", title),
                BackgroundImagePath = GameResPath.GetBigBgPath(background),
                ContentFactory = parent => SecretTreasurePageRegistry.Create(index, parent),
                OpenCheck = () => SecretTreasurePageRegistry.IsRegistered(index),
                LockedToast = label + "正在按路线精修，尚未进入验收态",
            };

        private static async Task<bool> EnsureViewAsync()
        {
            if (_frameRoot != null && _moduleRoot != null && _window != null && _mainView != null)
                return true;
            Transform layer = ViewManager.GetLayer(UILayer.Window);
            Task<GameObject> frameTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("common", "BaseWindowSkin"), layer);
            Task<GameObject> runeTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab(RuneModule, RunePrefab), layer);
            await Task.WhenAll(frameTask, runeTask);
            _frameRoot = frameTask.Result;
            _moduleRoot = runeTask.Result;
            if (_frameRoot == null || _moduleRoot == null)
            {
                GameLog.Error("Rune", "秘宝窗口加载失败 frame={0} rune={1}", _frameRoot != null, _moduleRoot != null);
                ReleaseView();
                return false;
            }
            _frameRoot.name = "BaseWindowSkin(SecretTreasure)";
            _moduleRoot.name = "RuneModule(Runtime)";
            _window = _frameRoot.GetComponent<BaseWindowSkinView>()
                ?? _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            _mainView = _moduleRoot.GetComponentInChildren<RuneMainUIView>(true);
            _bagView = _moduleRoot.GetComponentInChildren<RuneBagView>(true);
            if (_window == null || _mainView == null)
            {
                GameLog.Error("Rune", "秘宝窗口缺 BaseWindowSkinView/RuneMainUIView");
                ReleaseView();
                return false;
            }
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                view.gameObject.SetActive(false);
            return true;
        }

        private static BaseView ReparentRune(RectTransform parent)
        {
            if (_mainView == null || parent == null) return null;
            _mainView.transform.SetParent(parent, false);
            RectTransform rect = _mainView.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            return _mainView;
        }

        private static BaseView FindSub(string viewTypeName)
        {
            if (_moduleRoot == null || string.IsNullOrEmpty(viewTypeName)) return null;
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                if (view.GetType().Name == viewTypeName) return view;
            return null;
        }

        private static void PrepareSubView(BaseView target)
        {
            if (target == null || _moduleRoot == null) return;
            _moduleRoot.transform.SetAsLastSibling();
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                // 子窗互斥，但主页面必须留在弹窗背后；老端属性/背包/兑换均为覆盖式子窗。
                if (view == target || view == _mainView || !view.IsShown) continue;
                view.Hide();
            }
        }

        private static void HideSubViews()
        {
            if (_moduleRoot == null) return;
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                if (view.IsShown) view.Hide();
        }

        private static void ReleaseView()
        {
            SecretTreasurePageRegistry.ReleaseCreated();
            _window?.SetReturnAction(null);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _frameRoot = null;
            _moduleRoot = null;
            _window = null;
            _mainView = null;
            _bagView = null;
        }

        internal static void Reset()
        {
            Close();
            ReleaseView();
            RuneController.Instance.Dispose();
            _loading = false;
        }
    }

    /// <summary>五页共享窗框的懒加载注册表；兄弟页完成后注册真实 ContentFactory。</summary>
    internal static class SecretTreasurePageRegistry
    {
        private static readonly Dictionary<int, Func<RectTransform, BaseView>> Factories =
            new Dictionary<int, Func<RectTransform, BaseView>>();
        private static readonly List<BaseView> Created = new List<BaseView>();

        public static void Register(int index, Func<RectTransform, BaseView> factory)
        {
            if (index <= 0 || index > 4 || factory == null) return;
            Factories[index] = factory;
        }

        public static bool IsRegistered(int index) => Factories.ContainsKey(index);

        public static BaseView Create(int index, RectTransform parent)
        {
            if (!Factories.TryGetValue(index, out Func<RectTransform, BaseView> factory)) return null;
            BaseView view = factory(parent);
            if (view != null && !Created.Contains(view)) Created.Add(view);
            return view;
        }

        public static void ReleaseCreated()
        {
            Created.Clear();
        }
    }
}
