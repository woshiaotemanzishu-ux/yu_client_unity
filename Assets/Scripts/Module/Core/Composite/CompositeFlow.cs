using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary>
    /// 合成模块编排:十标签窗(对标老端 CompositeView extends BaseWindowComponent;MainFunc.Composite(=9) → OPEN_COMPOSITE_VIEW)。
    /// **走 BaseWindowSkinView 地基**(单内容源:所有标签内容都是 CompositeModule 顶层视图,无需跨模块)。
    ///
    /// 老端 tabStrList=[道具合成/熔魄铸魂/影骸战衣合成/遗骸铸合/九霄冥饰合成/天骨铸灵/启示铠铸/灵宠炼合/御府铸仪/天殒铸灵]。
    /// Unity 已写 7 页(道具/影骸/九霄冥饰/天骨/灵宠/御府/天殒)→ 开放;熔魄铸魂(CompositeRune,shared-prefab)/遗骸铸合(GodBeast,inline)/启示铠铸(Revelation,无场景)未写 → disabled。
    /// 点标签把对应内容 reparent 进窗框内容区 _gp_item_con(懒加载缓存)。入口 <see cref="CompositeBootstrap"/>(MainUIRouter "composite"/"red");
    /// 子窗(装备合成 CompositeEquipView、排行 CompositeRankView、各 Select/Resolve 弹窗)经 <see cref="OpenSub"/>;再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class CompositeFlow
    {
        private const string CONTENT_MODULE = "composite";
        private const string CONTENT_PREFAB = "CompositeModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 CompositeView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "CompositeGoodsView", "CompositeRuneView", "CompositeHolySealView",
            "GodBeastCompositeView", "CompositeUnrealView", "GodBefallCompositeView",
            "CompositeRevelationView", "CompositeGuardView", "GodCourtComView",
            "CompositeLonglangView"
        };
        // 已写内容(CompositeModule 顶层有该视图 + View.cs 已写)才开放
        private static readonly bool[] TabEnabled =
        {
            true, false, true, false, true, true, false, true, true, true
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

        /// <summary>打开合成模块内子窗(装备合成 CompositeEquipView、排行 CompositeRankView、各 Select/Resolve 弹窗…),按 View 子类名在内容源里查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Composite", "OpenSub({0}) 时合成模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Composite", "合成子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("Composite", "十标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Composite", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
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
            GameLog.Info("Composite", "合成十标签窗打开(BaseWindowSkinView,默认 tab{0} 道具合成)", DefaultTab);
        }

        /// <summary>把内容源里名为 viewName 的内容视图 reparent 进窗框内容区(保留其原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Composite", "内容视图 [{0}] 不在 CompositeModule 顶层", viewName);
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
