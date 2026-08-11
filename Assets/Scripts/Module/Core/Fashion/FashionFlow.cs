using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 当前版时装容器：时装 / 发饰共享 FashionMainView，装扮使用 DressView，套装使用 FashionSuitView。
    /// 外层结构、页签语义和每页背景对标老端 FashionBaseView。
    /// </summary>
    public static class FashionFlow
    {
        private const string CONTENT_MODULE = "fashion";
        private const string CONTENT_PREFAB = "FashionModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string DRESS_MODULE = "dress";
        private const string DRESS_PREFAB = "DressModule";

        private static readonly string[] Tabs = { "时装", "发饰", "装扮", "套装" };
        private static readonly int[] TabPosId = { 1, 3 };
        private static readonly string TitleImage = GameResPath.GetIcon("fashion", "fashion_icon_image");
        private static readonly string[] WindowBackgrounds =
        {
            GameResPath.GetBigBgPath("ui_role_bg3.jpg"),
            GameResPath.GetBigBgPath("ui_role_bg3.jpg"),
            GameResPath.GetBigBgPath("ui_role_bg4.jpg"),
            GameResPath.GetBigBgPath("ui_role_bg7.jpg"),
        };

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static GameObject _dressRoot;
        private static BaseWindowSkinView _window;
        private static FashionMainView _mainView;
        private static FashionSuitView _suitView;
        private static FashionLevelView _levelView;
        private static DressView _dressView;
        private static Sprite _titleSprite;
        private static int _titleRequestId;
        private static bool _titleLoading;
        private static readonly Dictionary<string, Sprite> PreloadedBackgrounds =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static int _backgroundRequestId;
        private static bool _backgroundLoading;
        private static Task<bool> _rootLoadTask;
        private static GameObject _preloadStage;
        private static int _rootLoadGeneration;
        private static int _openRequestId;
        private static byte _requestedDressType = DressView.BubbleType;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync(0);
        }

        public static void Open() => _ = OpenAsync(0);
        public static void Open(int tabIndex) => _ = OpenAsync(tabIndex);

        /// <summary>
        /// 预热 Fashion 自有标题和四个页签实际使用的三张背景。资源由本 Flow 持有一份引用，
        /// 让 BaseWindowSkin 真正开窗时命中 ResManager 缓存，避免 350ms 仍显示空标题/过渡背景。
        /// </summary>
        public static void PreloadChrome()
        {
            PreloadFashionTitle();
            PreloadFashionBackgrounds();
        }

        /// <summary>
        /// Pre-instantiates the Fashion frame/content/Dress roots under an inactive staging root.
        /// Nothing is attached to the visible Window layer until all three roots are ready.
        /// </summary>
        public static void PreloadWindow()
        {
            PreloadChrome();
            if (_frameRoot != null && _contentRoot != null && _dressRoot != null) return;
            _ = EnsureRootsLoadedAsync();
        }

        /// <summary>从设置等入口直达装扮页，并选中气泡/相框/头像中的指定类型。</summary>
        public static void OpenDress(byte dressType)
        {
            _requestedDressType = dressType;
            _ = OpenAsync(2);
        }

        public static void Close()
        {
            ++_openRequestId;
            if (_levelView != null) _levelView.Hide();
            // 窗框与重挂进去的内容页不是同一 BaseView 生命周期。只 Hide 窗框不会触发
            // FashionMainView.OnHide，事件订阅、模型预览和页内成功特效都会残留。
            if (_mainView != null && _mainView.IsShown) _mainView.Hide();
            if (_suitView != null && _suitView.IsShown) _suitView.Hide();
            if (_dressView != null && _dressView.IsShown) _dressView.Hide();
            if (_window != null) _window.Hide();
            // 老端 FashionBaseView.BaseWindowCloseFunc 在普通返回时切回 MainFunc.Role。
            // BaseWindowManager 只负责互斥隐藏，不会自动恢复上一个窗，因此这里显式还原返回链。
            RoleFlow.Open();
        }

        public static void OpenLevel(int posId)
        {
            if (posId != 1) return;
            FashionLevelView view = EnsureLevelView();
            if (view == null)
            {
                GameLog.Warn("Fashion", "FashionModule 缺 FashionLevelView/FasBagItemRenderer 业务组件");
                return;
            }
            view.Show(posId);
        }

        private static async Task OpenAsync(int tabIndex)
        {
            int openRequestId = ++_openRequestId;
            if (tabIndex < 0 || tabIndex >= Tabs.Length) tabIndex = 0;
            PreloadChrome();
            if (tabIndex == 0) FashionPreviewCache.PrewarmDefault();

            if (_frameRoot != null && _window != null)
            {
                if (tabIndex == 2) _dressView?.SetInitialType(_requestedDressType);
                _window.Show();
                _window.SetReturnAction(Close);
                _window.SelectShared(tabIndex);
                ApplyFashionChrome();
                return;
            }

            bool rootsLoaded = await EnsureRootsLoadedAsync();
            if (openRequestId != _openRequestId) return;
            if (!rootsLoaded || _frameRoot == null || _contentRoot == null || _dressRoot == null)
            {
                GameLog.Error("Fashion", "时装窗口加载失败 frame={0} fashion={1} dress={2}",
                    FRAME_PREFAB, CONTENT_PREFAB, DRESS_PREFAB);
                ShowPlaceholderAndReset();
                return;
            }

            // Multiple entry clicks may await the same preload task. The first continuation performs
            // the one-time handoff; later continuations only select their requested tab.
            if (_window != null)
            {
                if (tabIndex == 2) _dressView?.SetInitialType(_requestedDressType);
                _window.Show();
                _window.SetReturnAction(Close);
                _window.SelectShared(tabIndex);
                ApplyFashionChrome();
                return;
            }

            // All roots leave the inactive staging parent only after the batch is complete. There is
            // no await between this handoff and ConfigureShared, so the shared frame cannot render a
            // stale title/background/content shell in an intermediate Canvas frame.
            Transform layer = ViewManager.GetLayer(UILayer.Window);
            _frameRoot.transform.SetParent(layer, false);
            _contentRoot.transform.SetParent(layer, false);
            _dressRoot.transform.SetParent(layer, false);
            if (_preloadStage != null)
            {
                UnityEngine.Object.Destroy(_preloadStage);
                _preloadStage = null;
            }

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Fashion", "BaseWindowSkin 缺 BaseWindowSkinView");
                ShowPlaceholderAndReset();
                return;
            }

            _window.Show();
            _window.SetReturnAction(Close);
            var overrides = new Dictionary<int, Func<RectTransform, BaseView>>
            {
                [2] = ReparentDress,
                [3] = ReparentSuit,
            };
            _window.ConfigureShared(Tabs.Length, ReparentFashion, OnFashionTab, tabIndex,
                null, overrides, Tabs, null, null, null, null, null, WindowBackgrounds);
            ApplyFashionChrome();
            GameLog.Info("Fashion", "当前版时装窗口打开，默认 tab={0}({1})", tabIndex, Tabs[tabIndex]);
        }

        private static Task<bool> EnsureRootsLoadedAsync()
        {
            if (_frameRoot != null && _contentRoot != null && _dressRoot != null)
                return Task.FromResult(true);
            if (_rootLoadTask != null) return _rootLoadTask;
            int generation = _rootLoadGeneration;
            Task<bool> task = LoadRootsIntoHiddenStageAsync(generation);
            _rootLoadTask = task;
            _ = ClearRootLoadTaskAsync(task, generation);
            return task;
        }

        private static async Task ClearRootLoadTaskAsync(Task<bool> task, int generation)
        {
            try { await task; }
            finally
            {
                if (generation == _rootLoadGeneration && ReferenceEquals(_rootLoadTask, task))
                    _rootLoadTask = null;
            }
        }

        private static async Task<bool> LoadRootsIntoHiddenStageAsync(int generation)
        {
            GameObject stage = EnsurePreloadStage();
            if (stage == null) return false;

            GameObject frame = null;
            GameObject content = null;
            GameObject dress = null;
            try
            {
                string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
                string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
                string dressKey = GameResPath.GetUIPrefab(DRESS_MODULE, DRESS_PREFAB);
                Task<GameObject> frameTask = InstantiateHiddenRootAsync(frameKey, stage.transform);
                Task<GameObject> contentTask = InstantiateHiddenRootAsync(contentKey, stage.transform);
                Task<GameObject> dressTask = InstantiateHiddenRootAsync(dressKey, stage.transform);
                await Task.WhenAll(frameTask, contentTask, dressTask);
                frame = frameTask.Result;
                content = contentTask.Result;
                dress = dressTask.Result;

                if (generation != _rootLoadGeneration)
                {
                    ReleaseRoot(frame);
                    ReleaseRoot(content);
                    ReleaseRoot(dress);
                    return false;
                }
                if (frame == null || content == null || dress == null)
                {
                    ReleaseRoot(frame);
                    ReleaseRoot(content);
                    ReleaseRoot(dress);
                    return false;
                }

                frame.name = FRAME_PREFAB;
                content.name = CONTENT_PREFAB;
                dress.name = DRESS_PREFAB;
                frame.SetActive(false);
                content.SetActive(false);
                dress.SetActive(false);
                foreach (Transform child in content.transform) child.gameObject.SetActive(false);
                foreach (Transform child in dress.transform) child.gameObject.SetActive(false);
                _frameRoot = frame;
                _contentRoot = content;
                _dressRoot = dress;
                return true;
            }
            catch (Exception exception)
            {
                ReleaseRoot(frame);
                ReleaseRoot(content);
                ReleaseRoot(dress);
                GameLog.Error("Fashion", "时装窗口隐藏预载异常: {0}", exception.Message);
                return false;
            }
        }

        private static GameObject EnsurePreloadStage()
        {
            if (_preloadStage != null) return _preloadStage;
            Transform layer = ViewManager.GetLayer(UILayer.Window);
            if (layer == null) return null;
            _preloadStage = new GameObject("__FashionPreloadStage", typeof(RectTransform));
            _preloadStage.transform.SetParent(layer, false);
            _preloadStage.SetActive(false);
            return _preloadStage;
        }

        private static async Task<GameObject> InstantiateHiddenRootAsync(string key, Transform parent)
        {
            try
            {
                return await ResManager.InstantiateAsync(key, parent);
            }
            catch (Exception exception)
            {
                GameLog.Error("Fashion", "时装隐藏预载单项失败 key={0}: {1}", key, exception.Message);
                return null;
            }
        }

        private static void ReleaseRoot(GameObject root)
        {
            if (root != null) ResManager.ReleaseInstance(root);
        }

        /// <summary>
        /// 老端 FashionBaseView 使用固定 fashion_icon_image 标题且没有说明入口。标题属于本模块，
        /// 不经 BaseWindowSkin 每次切页的 SetImageAsync 重复加引用；这里单次缓存并在 Reset 释放。
        /// </summary>
        private static void ApplyFashionChrome()
        {
            BaseWindowSkinView window = _window;
            if (window == null) return;
            if (window._img_instruction != null) window._img_instruction.gameObject.SetActive(false);
            if (window._img_title == null) return;

            window._img_title.gameObject.SetActive(true);
            if (_titleSprite != null)
            {
                ApplyTitleSprite(window, _titleSprite);
                return;
            }

            // 禁止在资源 ready 前闪回 BaseWindowSkin 的通用占位标题。
            window._img_title.enabled = false;
            PreloadFashionTitle();
        }

        private static void PreloadFashionTitle()
        {
            if (_titleSprite != null || _titleLoading) return;
            _titleLoading = true;
            int requestId = ++_titleRequestId;
            _ = LoadFashionTitleAsync(requestId);
        }

        private static async Task LoadFashionTitleAsync(int requestId)
        {
            Sprite sprite;
            try
            {
                sprite = await ResManager.LoadAsync<Sprite>(TitleImage);
            }
            catch (Exception exception)
            {
                if (requestId == _titleRequestId) _titleLoading = false;
                GameLog.Warn("Fashion", "标题预热失败: {0}", exception.Message);
                return;
            }
            if (requestId != _titleRequestId)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }

            _titleLoading = false;
            if (sprite == null) return;
            _titleSprite = sprite;
            ApplyTitleSprite(_window, sprite);
        }

        private static void PreloadFashionBackgrounds()
        {
            if (_backgroundLoading) return;
            var missing = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in WindowBackgrounds)
            {
                if (string.IsNullOrEmpty(key) || !seen.Add(key) || PreloadedBackgrounds.ContainsKey(key)) continue;
                missing.Add(key);
            }
            if (missing.Count == 0) return;

            _backgroundLoading = true;
            int requestId = ++_backgroundRequestId;
            _ = PreloadFashionBackgroundsAsync(missing, requestId);
        }

        private static async Task PreloadFashionBackgroundsAsync(List<string> keys, int requestId)
        {
            var tasks = new List<Task>(keys.Count);
            foreach (string key in keys) tasks.Add(PreloadFashionBackgroundAsync(key, requestId));
            await Task.WhenAll(tasks);
            if (requestId == _backgroundRequestId) _backgroundLoading = false;
        }

        private static async Task PreloadFashionBackgroundAsync(string key, int requestId)
        {
            Sprite sprite;
            try
            {
                sprite = await ResManager.LoadAsync<Sprite>(key);
            }
            catch (Exception exception)
            {
                GameLog.Warn("Fashion", "背景预热失败 key={0}: {1}", key, exception.Message);
                return;
            }
            if (requestId != _backgroundRequestId)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }
            if (sprite == null) return;
            if (PreloadedBackgrounds.ContainsKey(key))
            {
                ResManager.Release(sprite);
                return;
            }
            PreloadedBackgrounds[key] = sprite;
        }

        private static void ApplyTitleSprite(BaseWindowSkinView window, Sprite sprite)
        {
            if (window == null || window._img_title == null || sprite == null) return;
            window._img_title.sprite = sprite;
            window._img_title.enabled = true;
            window._img_title.SetNativeSize();
        }

        private static void OnFashionTab(int index)
        {
            int posId = index >= 0 && index < TabPosId.Length ? TabPosId[index] : 1;
            if (_mainView == null) return;
            _mainView.SetPos(posId);
            GameLog.Info("Fashion", "切页签[{0}] -> pos={1}", Tabs[index], posId);
        }

        private static BaseView ReparentFashion(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform colorTemplate = _contentRoot.transform.Find("FashionColorItem");
            foreach (BaseView view in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (!(view is FashionMainView fashion)) continue;
                if (colorTemplate != null) fashion.SetColorTemplate(colorTemplate.gameObject);
                fashion.transform.SetParent(parent, false);
                fashion.gameObject.SetActive(true);
                _mainView = fashion;
                return fashion;
            }
            GameLog.Warn("Fashion", "FashionModule 缺 FashionMainView");
            return null;
        }

        private static BaseView ReparentDress(RectTransform parent)
        {
            if (_dressRoot == null) return null;
            foreach (BaseView view in _dressRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (!(view is DressView dress)) continue;
                dress.SetInitialType(_requestedDressType);
                dress.transform.SetParent(parent, false);
                dress.gameObject.SetActive(true);
                _dressView = dress;
                return dress;
            }
            GameLog.Warn("Fashion", "DressModule 缺 DressView 业务组件（需运行 DressBindUpgrader）");
            return null;
        }

        private static BaseView ReparentSuit(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform tabTemplate = _contentRoot.transform.Find("FashionSuitTabItem");
            Transform goodsTemplate = _contentRoot.transform.Find("FashionSuitGoodsItem");
            foreach (BaseView view in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (!(view is FashionSuitView suit)) continue;
                suit.SetTemplates(tabTemplate != null ? tabTemplate.gameObject : null,
                    goodsTemplate != null ? goodsTemplate.gameObject : null,
                    suit._tpl_BaseAwardItem);
                suit.transform.SetParent(parent, false);
                suit.gameObject.SetActive(true);
                _suitView = suit;
                return suit;
            }
            GameLog.Warn("Fashion", "FashionModule 缺 FashionSuitView");
            return null;
        }

        private static FashionLevelView EnsureLevelView()
        {
            if (_levelView != null) return _levelView;
            if (_contentRoot == null) return null;
            Transform itemTemplate = _contentRoot.transform.Find("FasBagItemRenderer");
            foreach (BaseView view in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (!(view is FashionLevelView level)) continue;
                if (level._tpl_FasBagItemRenderer == null && itemTemplate != null)
                    level._tpl_FasBagItemRenderer = itemTemplate.gameObject;
                level.transform.SetParent(ViewManager.GetLayer(UILayer.Popup), false);
                level.gameObject.SetActive(false);
                _levelView = level;
                return level;
            }
            return null;
        }

        public static void Reset()
        {
            FashionPreviewCache.Reset();
            ++_openRequestId;
            ++_rootLoadGeneration;
            _rootLoadTask = null;
            ++_titleRequestId;
            _titleLoading = false;
            if (_titleSprite != null) ResManager.Release(_titleSprite);
            _titleSprite = null;
            ++_backgroundRequestId;
            _backgroundLoading = false;
            foreach (Sprite sprite in PreloadedBackgrounds.Values)
                if (sprite != null) ResManager.Release(sprite);
            PreloadedBackgrounds.Clear();
            if (_levelView != null) UnityEngine.Object.Destroy(_levelView.gameObject);
            _window?.SetReturnAction(null);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            if (_dressRoot != null) ResManager.ReleaseInstance(_dressRoot);
            _frameRoot = null;
            _contentRoot = null;
            _dressRoot = null;
            _window = null;
            _mainView = null;
            _suitView = null;
            _levelView = null;
            _dressView = null;
            if (_preloadStage != null) UnityEngine.Object.Destroy(_preloadStage);
            _preloadStage = null;
        }

        private static void ShowPlaceholderAndReset()
        {
            MainUIRouteFallback.ShowUnavailable(CONTENT_MODULE, "Fashion", "FashionModule/BaseWindowSkin/DressModule load failed");
            Reset();
        }
    }
}
