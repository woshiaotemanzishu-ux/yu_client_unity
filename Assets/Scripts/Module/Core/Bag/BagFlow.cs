using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包模块编排:五标签窗(对标老端 BagView extends BaseWindowComponent;主界面功能图标 → SwitchView "BagView")。
    /// **走 BaseWindowSkinView 地基**。打开 = 实例化共享窗框 BaseWindowSkin + 实例化 BagModule(内容源,先全隐藏)→ Configure(5 标签):
    /// 点标签把对应内容 reparent 进窗框内容区 _gp_item_con(懒加载缓存)。
    ///
    /// 老端 tabStrList=[背包/仓库/影骸战衣/启示圣铠/九天神祭],viewClassList=[BagComponentView/WarehouseView/HolySealView/RevelationEquipView/longlanguageView]。
    /// Unity 已写 背包/仓库(均在 BagModule,tab0/1)→ 开放(**仓库 WarehouseView 此前是孤立已写页,现经 tab1 可达**);
    /// 影骸战衣(holySeal)/启示圣铠(revelation)/九天神祭(longlanguage)跨模块未写 → disabled。
    /// 子窗(一键使用 OneKeyUseView/熔炼 BagSmeltView/扩展 ExpandBagView…)经 <see cref="ToggleSub"/>/<see cref="OpenSub"/>(背包面板按钮触发)。
    /// 入口注册见 <see cref="BagBootstrap"/>(MainUIRouter "bag");再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class BagFlow
    {
        private const string CONTENT_MODULE = "bag";
        private const string CONTENT_PREFAB = "BagModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 BagView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "BagComponentView", "WarehouseView", "HolySealView", "RevelationEquipView", "longlanguageView"
        };
        // 已写内容(BagModule 顶层有该视图 + View.cs 已写)才开放;tab2/3/4 跨模块未写
        private static readonly bool[] TabEnabled = { true, true, false, false, false };
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

        /// <summary>切换背包模块内子窗(一键使用/熔炼/扩展…),叠在背包面板上;已显则关、未显则开;按 View 子类名在内容源里查找。</summary>
        public static void ToggleSub(string viewTypeName)
        {
            BaseView v = FindSub(viewTypeName);
            if (v == null) { GameLog.Info("Bag", "背包子窗 [{0}] 未移植 View,待对接", viewTypeName); return; }
            if (v.IsShown) v.Hide(); else v.Show();
        }

        /// <summary>打开背包模块内子窗(叠主面板上);按 View 子类名查找。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            BaseView v = FindSub(viewTypeName);
            if (v == null) { GameLog.Info("Bag", "背包子窗 [{0}] 未移植 View,待对接", viewTypeName); return; }
            v.Show();
        }

        private static BaseView FindSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Bag", "OpenSub/ToggleSub({0}) 时背包模块未打开", viewTypeName);
                return null;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) return v;
            }
            return null;
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
            _frameRoot = await ResManager.InstantiateAsync(frameKey, ViewManager.GetLayer(UILayer.Window));
            _contentRoot = await ResManager.InstantiateAsync(contentKey, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Bag", "五标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Bag", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }

            var specs = new List<TabSpec>(TabContent.Length);
            for (int i = 0; i < TabContent.Length; i++)
            {
                string viewName = TabContent[i];
                bool enabled = TabEnabled[i];
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    ContentFactory = enabled ? (Func<RectTransform, BaseView>)(parent => ReparentContent(viewName, parent)) : null,
                });
            }

            _window.Show();
            _window.Configure(specs, DefaultTab);
            GameLog.Info("Bag", "背包五标签窗打开(BaseWindowSkinView,默认 tab{0} 背包)", DefaultTab);
        }

        /// <summary>把内容源里名为 viewName 的内容视图 reparent 进窗框内容区(保留其原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Bag", "内容视图 [{0}] 不在 BagModule 顶层", viewName);
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
    }
}
