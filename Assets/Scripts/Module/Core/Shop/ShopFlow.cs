using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商城模块编排:多标签窗(对标老端 ShopView extends BaseWindowComponent;主 HUD 商城按钮)。**走 BaseWindowSkinView 共享内容模式**。
    ///
    /// 老端 ShopView 各标签 viewClassList 多为同一 ShopCommonView(仅 shop_type 不同),抢购 tab 为 ShopVieView。
    /// Unity 已写 ShopCommonView → 本窗 11 标签共用它(ConfigureShared:懒建一次,点标签重喂 shop_type)。
    /// 标签:限购/勾玉/绑玉/抢购/善缘/荣耀/冲霄/神陨禁区/天境/九天神祭/神霄御府。
    /// 降级:ShopModel/商品数据未移植 → 切标签仅日志(shop_type 待对接);抢购/神秘/批量等专属视图(ShopVieView/ShopMysteriousView/ShopBulkPurchaseView)经 <see cref="OpenSub"/> 后续接。
    /// 入口注册见 <see cref="ShopBootstrap"/>(MainUIRouter "shop");再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class ShopFlow
    {
        private const string CONTENT_MODULE = "shop";
        private const string CONTENT_PREFAB = "ShopModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 ShopView.tabStrList 的 shop_type 标签(仅用于日志;图标式标签 sprite 待美术)
        private static readonly string[] ShopTabs =
        {
            "限购", "勾玉", "绑玉", "抢购", "善缘", "荣耀", "冲霄", "神陨禁区", "天境", "九天神祭", "神霄御府"
        };
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static bool _loading;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        /// <summary>打开商城模块内子窗(抢购 ShopVieView/神秘 ShopMysteriousView/批量 ShopBulkPurchaseView…),按 View 子类名在内容源查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Shop", "OpenSub({0}) 时商城模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Shop", "商城子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_frameRoot != null)
            {
                if (_window != null) _window.Show();
                return;
            }

            if (_loading) return;
            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            try
            {
                _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Shop", frameKey, ViewManager.GetLayer(UILayer.Window));
                _contentRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Shop", contentKey, ViewManager.GetLayer(UILayer.Window));
            }
            catch (Exception e)
            {
                GameLog.Error("Shop", "Shop window load exception frame={0} content={1} error={2}", frameKey, contentKey, e.Message);
                ShowPlaceholderAndReset();
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Shop", "商城多标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                ShowPlaceholderAndReset();
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                ShowPlaceholderAndReset();
                GameLog.Warn("Shop", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }

            // 共享内容模式:多数标签共用 ShopCommonView(切标签重喂 shop_type);抢购(tab3)用专属 ShopVieView(override)。
            var overrides = new Dictionary<int, Func<RectTransform, BaseView>> { { 3, ReparentVie } };
            _window.Show();
            _window.ConfigureShared(ShopTabs.Length, ReparentCommon, OnShopTab, DefaultTab, null, overrides);
            GameLog.Info("Shop", "商城多标签窗打开(BaseWindowSkinView 共享内容,{0} 标签,默认 tab{1} {2})", ShopTabs.Length, DefaultTab, ShopTabs[DefaultTab]);
        }

        private static void OnShopTab(int index)
        {
            // 老端 SwitchView → ShopCommonView.SetShopType(shop_type)。ShopModel/商品数据未移植 → 仅日志。
            string name = index >= 0 && index < ShopTabs.Length ? ShopTabs[index] : index.ToString();
            GameLog.Info("Shop", "切商城分类[{0}] → 待对接 ShopModel(重喂 shop_type)", name);
        }

        /// <summary>把内容源里的 ShopCommonView reparent 进窗框内容区(保留原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentCommon(RectTransform parent) => ReparentNamed("ShopCommonView", parent);

        /// <summary>抢购标签专属:把 ShopVieView reparent 进窗框内容区。</summary>
        private static BaseView ReparentVie(RectTransform parent) => ReparentNamed("ShopVieView", parent);

        private static BaseView ReparentNamed(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Shop", "{0} 不在 ShopModule 顶层", viewName);
                return null;
            }
            t.SetParent(parent, false);
            t.gameObject.SetActive(true);
            return t.GetComponent<BaseView>();
        }

        internal static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            Reset();
            MainUIRoutePlaceholder.Show(CONTENT_MODULE);
        }
    }
}
