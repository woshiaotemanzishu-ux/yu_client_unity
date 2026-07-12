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
    /// 标签:限购/灵玉/绑玉/抢购/善缘/荣耀/冲霄/神陨禁区/天境/九天神祭/神霄御府(轮11 已接真数据:ShopModel/
    /// ShopController 落地,切标签经 <see cref="OnShopTab"/> 重发 15301;标签图标/背景已补齐,见
    /// <see cref="ShopTabUpIcons"/>/<see cref="WindowBg"/>)。
    /// 抢购/神秘/批量等专属视图(ShopVieView 已接真数据;ShopMysteriousView/ShopBulkPurchaseView 仍是死枝)
    /// 经 <see cref="OpenSub"/> 打开。
    /// 入口注册见 <see cref="ShopBootstrap"/>(MainUIRouter "shop");再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class ShopFlow
    {
        private const string CONTENT_MODULE = "shop";
        private const string CONTENT_PREFAB = "ShopModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 ShopView.tabStrList 标签文案(轮11 订正:老端 ShopView.ts:63 "灵玉",此前 Unity 端误写"勾玉")。
        private static readonly string[] ShopTabs =
        {
            "限购", "灵玉", "绑玉", "抢购", "善缘", "荣耀", "冲霄", "神陨禁区", "天境", "九天神祭", "神霄御府"
        };
        // 标签 index → ShopType(对标老端 tabStrList[i].shop_type;tab3 抢购不走 15301,走独立 64000/64001——
        // tab3 是 override 页,BaseWindowSkinView.SelectShared 的 override 分支不回调 _onSharedTab,
        // 天然不触发 OnShopTab;OnShopTab 内另有 TYPE_VIE 防御 return 兜底)。
        private static readonly int[] ShopTypes =
        {
            ShopModel.TYPE_LIMIT, ShopModel.TYPE_DIAMOND, ShopModel.TYPE_BIND_DIAMOND, ShopModel.TYPE_VIE,
            ShopModel.TYPE_GUILD, ShopModel.TYPE_HONER, ShopModel.TYPE_GHOST_WALK, ShopModel.TYPE_MEDAL_SHOP,
            ShopModel.TYPE_SINGLE_RANK, ShopModel.TYPE_LONGLANG_EX, ShopModel.TYPE_GOD_COURT,
        };
        // 标签图标(对标老端 tabStrList[i].icon_up_source/icon_down_source,原 Laya 图集键已按同名 png 转入
        // Assets/GameRes/resource/game/shop/texture/——冲霄/神陨禁区老端本就共用同一对 xz1/xz2 图,非本端遗漏)。
        private static readonly string[] ShopTabUpIcons =
        {
            "uisc_010_r2_c2", "uisc_010_r1_c2", "uisc_010_r3_c2", "uibqy_002_r1_c4", "uisc_010_11",
            "uirysc_002", "xz2", "xz2", "uikfdr_023_02", "uilwmb_012a", "uistjy_001_02",
        };
        private static readonly string[] ShopTabDownIcons =
        {
            "uisc_010_r2_c1", "uisc_010_r1_c1", "uisc_010_r3_c1", "uibqy_002_r1_c3", "uisc_010_10",
            "uirysc_001", "xz1", "xz1", "uikfdr_023_01", "uilwmb_012", "uistjy_001_01",
        };
        // 窗底大图(对标老端 bg_list:除九天神祭[LonglangEx]走 "ui_bg_1.jpg" 特例外均 "ui_Shop_bg1.jpg"。
        // ConfigureShared 背景是整窗单值,非逐标签——tab9[LonglangEx]专属 override 未接[r11_unity #8 TODO],
        // 本轮暂统一用 "ui_Shop_bg1.jpg",偏差记汇报)。
        private static readonly string WindowBg = GameResPath.GetBigBgPath("ui_Shop_bg1.jpg");
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static ShopCommonView _commonView;
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
            string[] upImages = new string[ShopTabUpIcons.Length];
            string[] downImages = new string[ShopTabDownIcons.Length];
            for (int i = 0; i < ShopTabUpIcons.Length; i++) upImages[i] = GameResPath.GetIcon("shop", ShopTabUpIcons[i]);
            for (int i = 0; i < ShopTabDownIcons.Length; i++) downImages[i] = GameResPath.GetIcon("shop", ShopTabDownIcons[i]);

            _window.Show();
            _window.ConfigureShared(ShopTabs.Length, ReparentCommon, OnShopTab, DefaultTab, null, overrides,
                ShopTabs, null, WindowBg, null, upImages, downImages);
            GameLog.Info("Shop", "商城多标签窗打开(BaseWindowSkinView 共享内容,{0} 标签,默认 tab{1} {2})", ShopTabs.Length, DefaultTab, ShopTabs[DefaultTab]);
        }

        private static void OnShopTab(int index)
        {
            // 老端 SwitchView → ShopCommonView.SetShopType(shop_type) 触发重拉。共享架构下每次选中都重发
            // 15301(本家族无 CD 表,重发无害,对标老端"每标签首次打开各自 InitEvents 里发一次"效果)。
            string name = index >= 0 && index < ShopTabs.Length ? ShopTabs[index] : index.ToString();
            if (index < 0 || index >= ShopTypes.Length)
            {
                GameLog.Warn("Shop", "切商城分类[{0}] 无对应 ShopType(index 越界)", name);
                return;
            }
            int shopType = ShopTypes[index];
            if (shopType == ShopModel.TYPE_VIE)
            {
                // 抢购(99)不在服务端 ?SHOP_TYPE_LIST(1-18) 白名单,发 15301 只会被静默吞掉(废包);
                // 老端 99 从不走 15301。正常路径 override 已拦截,此处防御直调/改 DefaultTab 的情况。
                GameLog.Info("Shop", "切商城分类[{0}] 抢购页走 64000 通道,跳过 15301", name);
                return;
            }
            ShopController.Instance.RequestShopType(shopType);
            _commonView?.SetShopType(shopType);
            GameLog.Info("Shop", "切商城分类[{0}] → shop_type={1}", name, shopType);
        }

        /// <summary>把内容源里的 ShopCommonView reparent 进窗框内容区(保留原始布局),返回其 BaseView。
        /// 懒建一次(BaseWindowSkinView._sharedContent 内部缓存,本方法只在首次选中共享内容标签时被调),
        /// 缓存实例供 <see cref="OnShopTab"/> 之后重喂 shop_type(对标 PetFlow._mainView 先例)。</summary>
        private static BaseView ReparentCommon(RectTransform parent)
        {
            BaseView v = ReparentNamed("ShopCommonView", parent);
            _commonView = v as ShopCommonView;
            return v;
        }

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
            _commonView = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            Reset();
            MainUIRoutePlaceholder.Show(CONTENT_MODULE);
        }
    }
}
