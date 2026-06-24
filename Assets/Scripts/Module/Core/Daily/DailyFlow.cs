using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日活动流程:六标签窗(对标老端 DailyView extends BaseWindowComponent)。**走 BaseWindowSkinView 地基 + 多内容源**(标签内容跨模块,= 老端 viewClassModuleDic)。
    ///
    /// 老端 tabStrList=[每日任务/限时活动/无尽之海/资源找回/托管中心/我要变强];内容分散在 daily / brightSea / deposit 等模块。
    /// Unity 已写 每日任务/限时活动/资源找回/我要变强(daily,tab0/1/3/5)+ 无尽之海(brightSea,tab2)→ 这 5 标签开放;托管(deposit,tab4)未写 → disabled。
    /// 打开时按需去重加载各标签所属内容预制体,点标签把对应内容 reparent 进窗框内容区 _gp_item_con(懒加载缓存)。
    /// 二级 HUD 每日寻宝按钮(MainUIRouter "dailyfind")默认开 tab3。寻宝确认 DailyResFindTipsView 经 <see cref="OpenSub"/> 叠开。
    /// </summary>
    public static class DailyFlow
    {
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";
        private const string PRIMARY_MODULE = "daily";
        private const string PRIMARY_PREFAB = "DailyModule";

        // 老端 DailyView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "DailyTaskView", "DailyLimitActivityView", "BrightSeaEnterView",
            "DailyResFindView", "DepositView", "DailyStrongerView"
        };
        // 各标签内容所属 module/prefab(跨模块);disabled 标签不加载
        private static readonly string[] TabModule =
        {
            "daily", "daily", "brightSea", "daily", "deposit", "daily"
        };
        private static readonly string[] TabPrefab =
        {
            "DailyModule", "DailyModule", "BrightSeaModule", "DailyModule", "DepositModule", "DailyModule"
        };
        // 已写内容才开放;6/6 全开(每日任务/限时活动/无尽之海/资源找回/托管/我要变强)
        private static readonly bool[] TabEnabled = { true, true, true, true, true, true };
        private const int DefaultTab = 3;
        private const int BrightSeaTab = 2;

        private static GameObject _frameRoot;
        private static readonly Dictionary<string, GameObject> _contentRoots = new Dictionary<string, GameObject>();
        private static BaseWindowSkinView _window;
        private static bool _loading;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync(DefaultTab);
        }

        public static void ToggleBrightSea()
        {
            if (_window != null && _window.IsShown && _window.CurrentIndex == BrightSeaTab)
            {
                Close();
                return;
            }

            _ = OpenAsync(BrightSeaTab);
        }

        public static void Open() => _ = OpenAsync(DefaultTab);

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        /// <summary>打开每日模块内子窗(寻宝确认 DailyResFindTipsView…),在所有已加载内容源里按 View 子类名查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoots.Count == 0)
            {
                GameLog.Warn("Daily", "OpenSub({0}) 时每日模块未打开", viewTypeName);
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
            GameLog.Info("Daily", "每日子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync(int defaultTab)
        {
            if (_frameRoot != null)
            {
                if (_window != null) _window.Show();
                if (_window != null) _window.SelectTab(defaultTab);
                return;
            }

            if (_loading) return;
            _loading = true;

            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            _frameRoot = await ResManager.InstantiateAsync(frameKey, ViewManager.GetLayer(UILayer.Window));

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
                GameLog.Error("Daily", "每日六标签窗加载失败(frame 或主内容源缺失)");
                return;
            }
            _frameRoot.name = FRAME_PREFAB;

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("Daily", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
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
            _window.Configure(specs, defaultTab);
            GameLog.Info("Daily", "每日六标签窗打开(BaseWindowSkinView,默认 tab{0})", defaultTab);
        }

        /// <summary>从指定内容源 prefab 里把名为 viewName 的内容视图 reparent 进窗框内容区(保留原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentFrom(string prefabName, string viewName, RectTransform parent)
        {
            if (!_contentRoots.TryGetValue(prefabName, out GameObject root) || root == null)
            {
                GameLog.Warn("Daily", "内容源 [{0}] 未加载(标签 {1})", prefabName, viewName);
                return null;
            }
            Transform t = root.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Daily", "内容视图 [{0}] 不在 {1} 顶层", viewName, prefabName);
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
