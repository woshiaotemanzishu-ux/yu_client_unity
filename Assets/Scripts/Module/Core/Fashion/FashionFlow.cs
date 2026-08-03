using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.MainUI;
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
        private static readonly string[] TitleTexts = { "时装", "时装", "时装", "时装" };
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
        private static bool _loading;
        private static byte _requestedDressType = DressView.BubbleType;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync(0);
        }

        public static void Open() => _ = OpenAsync(0);
        public static void Open(int tabIndex) => _ = OpenAsync(tabIndex);

        /// <summary>从设置等入口直达装扮页，并选中气泡/相框/头像中的指定类型。</summary>
        public static void OpenDress(byte dressType)
        {
            _requestedDressType = dressType;
            _ = OpenAsync(2);
        }

        public static void Close()
        {
            if (_levelView != null) _levelView.Hide();
            if (_window != null) _window.Hide();
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
            if (tabIndex < 0 || tabIndex >= Tabs.Length) tabIndex = 0;

            if (_frameRoot != null && _window != null)
            {
                if (tabIndex == 2) _dressView?.SetInitialType(_requestedDressType);
                _window.Show();
                _window.SelectShared(tabIndex);
                return;
            }

            if (_loading) return;
            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            string dressKey = GameResPath.GetUIPrefab(DRESS_MODULE, DRESS_PREFAB);
            try
            {
                Transform layer = ViewManager.GetLayer(UILayer.Window);
                _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Fashion", frameKey, layer);
                _contentRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Fashion", contentKey, layer);
                _dressRoot = await MainUIRouteFallback.InstantiateOrShowAsync(DRESS_MODULE, "Dress", dressKey, layer);
            }
            catch (Exception exception)
            {
                GameLog.Error("Fashion", "时装窗口加载异常: {0}", exception.Message);
                ShowPlaceholderAndReset();
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null || _dressRoot == null)
            {
                GameLog.Error("Fashion", "时装窗口加载失败 frame={0} fashion={1} dress={2}", frameKey, contentKey, dressKey);
                ShowPlaceholderAndReset();
                return;
            }

            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;
            _dressRoot.name = DRESS_PREFAB;
            foreach (Transform child in _contentRoot.transform) child.gameObject.SetActive(false);
            foreach (Transform child in _dressRoot.transform) child.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Fashion", "BaseWindowSkin 缺 BaseWindowSkinView");
                ShowPlaceholderAndReset();
                return;
            }

            _window.Show();
            var overrides = new Dictionary<int, Func<RectTransform, BaseView>>
            {
                [2] = ReparentDress,
                [3] = ReparentSuit,
            };
            _window.ConfigureShared(Tabs.Length, ReparentFashion, OnFashionTab, tabIndex,
                null, overrides, Tabs, null, null, TitleTexts, null, null, WindowBackgrounds);
            GameLog.Info("Fashion", "当前版时装窗口打开，默认 tab={0}({1})", tabIndex, Tabs[tabIndex]);
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
            if (_levelView != null) UnityEngine.Object.Destroy(_levelView.gameObject);
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
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            MainUIRouteFallback.ShowUnavailable(CONTENT_MODULE, "Fashion", "FashionModule/BaseWindowSkin/DressModule load failed");
            Reset();
        }
    }
}
