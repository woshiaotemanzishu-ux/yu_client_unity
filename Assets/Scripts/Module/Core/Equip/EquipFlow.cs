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
    /// 装备模块编排:六标签窗(对标老端 EquipView extends BaseWindowComponent;MainFunc.Equip(=4) → OPEN_VIEW "EquipView")。
    /// **走 BaseWindowSkinView 地基,且示范「跨模块标签内容」**:各标签内容可来自不同 module 预制体(老端 viewClassModuleDic 同义)。
    ///
    /// 老端 tabStrList=[天殒淬炉/神兵淬炼/骸珀镶嵌/吞天洗魄/神屠九炼/不朽圣骸];内容分散在 equip / jewel / equipRefinement / equipArmor 等模块。
    /// Unity 已写:淬炉/淬炼/洗魄(equip,tab0/1/3)+ 神屠九炼(equipRefinement,tab4)→ 这 4 标签开放(默认 tab0);镶嵌(jewel inline)/圣骸(equipArmor)未写 → disabled。
    /// 打开时按需加载各标签所属内容预制体(去重),点标签把对应内容 reparent 进窗框内容区 _gp_item_con(懒加载缓存)。
    /// 入口 <see cref="EquipBootstrap"/>(MainUIRouter "equip");子窗(淬炉宗师等)经 <see cref="OpenSub"/>;再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class EquipFlow
    {
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string PRIMARY_MODULE = "equip";
        private const string PRIMARY_PREFAB = "EquipModule";

        // 老端 EquipView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "EquipStrenView", "EquipSmeltView", "EquipJewelView",
            "EquipWashView", "EquipRefinementView", "EquipArmorView"
        };
        // 各标签内容所属 module/prefab(跨模块);未写的标签随便填(disabled 不加载)
        private static readonly string[] TabModule =
        {
            "equip", "equip", "jewel", "equip", "equipRefinement", "equipArmor"
        };
        private static readonly string[] TabPrefab =
        {
            "EquipModule", "EquipModule", "JewelModule", "EquipModule", "EquipRefinementModule", "EquipArmorModule"
        };
        // 该标签内容是否已在 Unity 写好(写好才开放;其余 disabled,写完置 true 即开)
        private static readonly bool[] TabEnabled = { true, true, false, true, true, false };
        private const int DefaultTab = 0;

        private static GameObject _frameRoot;
        // 内容源根:prefab 名 → 实例(去重,多标签可共用一个 prefab)
        private static readonly Dictionary<string, GameObject> _contentRoots = new Dictionary<string, GameObject>();
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

        /// <summary>打开装备模块内子窗(淬炉宗师 EquipStrenMasterView、洗魄选材 EquipWashGoodsView…),在所有已加载内容源里按 View 子类名查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoots.Count == 0)
            {
                GameLog.Warn("Equip", "OpenSub({0}) 时装备模块未打开", viewTypeName);
                return;
            }
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root == null) continue;
                foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
                {
                    if (v.GetType().Name == viewTypeName) { v.Show(); return; }
                }
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
            _frameRoot = await ResManager.InstantiateAsync(frameKey, ViewManager.GetLayer(UILayer.Window));

            // 收集需加载的内容源(主源 + 各启用标签所属 prefab,去重)
            var needPrefab = new Dictionary<string, string> { { PRIMARY_PREFAB, PRIMARY_MODULE } };
            for (int i = 0; i < TabContent.Length; i++)
            {
                if (TabEnabled[i] && !needPrefab.ContainsKey(TabPrefab[i])) needPrefab[TabPrefab[i]] = TabModule[i];
            }
            foreach (KeyValuePair<string, string> kv in needPrefab)
            {
                string key = GameResPath.GetUIPrefab(kv.Value, kv.Key);
                GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                if (root != null)
                {
                    root.name = kv.Key;
                    foreach (Transform c in root.transform) c.gameObject.SetActive(false);
                    _contentRoots[kv.Key] = root;
                }
            }
            _loading = false;

            if (_frameRoot == null || !_contentRoots.ContainsKey(PRIMARY_PREFAB))
            {
                GameLog.Error("Equip", "装备六标签窗加载失败(frame 或主内容源缺失)");
                return;
            }
            _frameRoot.name = FRAME_PREFAB;

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
                string prefabName = TabPrefab[i];
                bool enabled = TabEnabled[i];
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    ContentFactory = enabled ? (Func<RectTransform, BaseView>)(parent => ReparentFrom(prefabName, viewName, parent)) : null,
                });
            }

            _window.Show();
            _window.Configure(specs, DefaultTab);
            GameLog.Info("Equip", "装备六标签窗打开(BaseWindowSkinView,默认 tab{0} 天殒淬炉)", DefaultTab);
        }

        /// <summary>从指定内容源 prefab 里把名为 viewName 的内容视图 reparent 进窗框内容区(保留原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentFrom(string prefabName, string viewName, RectTransform parent)
        {
            if (!_contentRoots.TryGetValue(prefabName, out GameObject root) || root == null)
            {
                GameLog.Warn("Equip", "内容源 [{0}] 未加载(标签 {1})", prefabName, viewName);
                return null;
            }
            Transform t = root.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Equip", "内容视图 [{0}] 不在 {1} 顶层", viewName, prefabName);
                return null;
            }
            t.SetParent(parent, false);
            t.gameObject.SetActive(true);
            return t.GetComponent<BaseView>();
        }

        internal static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            foreach (GameObject root in _contentRoots.Values)
            {
                if (root != null) ResManager.ReleaseInstance(root);
            }
            _contentRoots.Clear();
            _frameRoot = null;
            _window = null;
            _loading = false;
        }
    }
}
