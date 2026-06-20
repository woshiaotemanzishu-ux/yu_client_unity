using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备模块编排:六标签窗(对标老端 EquipView extends BaseWindowComponent;主界面 MainFunc.Equip(=4) → OPEN_VIEW "EquipView")。
    /// **第二个走 BaseWindowSkinView 地基的分页大窗**(模板复制自 DailyFlow,仅改 4 个常量 + 内容源)。
    ///
    /// 打开 = 实例化共享窗框 BaseWindowSkin + 实例化 EquipModule(内容源,先全隐藏)→ Configure(6 标签):点标签把对应内容视图
    /// reparent 进窗框内容区 _gp_item_con(懒加载缓存)。老端 tabStrList=[天殒淬炉/神兵淬炼/骸珀镶嵌/吞天洗魄/神屠九炼/不朽圣骸];
    /// Unity 已写内容 淬炉/淬炼/洗魄(tab0/1/3)→ 这三标签开放(默认 tab0),镶嵌/九炼/圣骸 disabled(EquipJewel/Refinement/Armor 独立模块未移植,写好置 true 即开)。
    /// 入口注册见 <see cref="EquipBootstrap"/>(MainUIRouter "equip");子窗(淬炉宗师 EquipStrenMasterView…)经 <see cref="OpenSub"/>。再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class EquipFlow
    {
        private const string CONTENT_MODULE = "equip";
        private const string CONTENT_PREFAB = "EquipModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 EquipView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "EquipStrenView", "EquipSmeltView", "EquipJewelView",
            "EquipWashView", "EquipRefinementView", "EquipArmorView"
        };
        // 该标签内容视图是否已在 Unity 写好(写好才开放;其余 disabled,写完置 true 即开)
        private static readonly bool[] TabEnabled = { true, true, false, true, false, false };
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

        /// <summary>打开装备模块内子窗(淬炉宗师 EquipStrenMasterView、洗魄选材 EquipWashGoodsView…),按 View 子类名在内容源里查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Equip", "OpenSub({0}) 时装备模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Equip", "装备子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("Equip", "六标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Equip", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
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
            GameLog.Info("Equip", "装备六标签窗打开(BaseWindowSkinView,默认 tab{0} 天殒淬炉)", DefaultTab);
        }

        /// <summary>把内容源里名为 viewName 的内容视图 reparent 进窗框内容区(保留其原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Equip", "内容视图 [{0}] 不在 EquipModule 顶层", viewName);
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
