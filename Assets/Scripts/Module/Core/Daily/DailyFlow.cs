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
    /// 每日活动流程:六标签窗(对标老端 DailyView extends BaseWindowComponent)。**首个走 BaseWindowSkinView 地基的分页大窗**。
    ///
    /// 打开 = 实例化共享窗框 BaseWindowSkin(标签条/内容区/关闭)+ 实例化 DailyModule(内容视图源,先全隐藏)→
    /// BaseWindowSkinView.Configure(6 标签):点某标签时把对应内容视图 reparent 进窗框内容区 _gp_item_con(懒加载+缓存,照抄老端 ChangeView)。
    /// 老端 tabStrList=[每日任务/限时活动/无尽之海/资源找回/托管中心/我要变强];Unity 已写内容仅 DailyResFindView(tab3)→
    /// 仅 tab3 开放(默认页),其余标签 disabled(内容视图未移植,点了日志降级),内容写好后改 TAB_ENABLED 即开。
    /// 二级 HUD 每日寻宝按钮(MainUIRouter "dailyfind")默认开 tab3。寻宝确认 DailyResFindTipsView 经 <see cref="OpenSub"/> 叠开。
    /// </summary>
    public static class DailyFlow
    {
        private const string CONTENT_MODULE = "daily";
        private const string CONTENT_PREFAB = "DailyModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 DailyView.viewClassList(标签索引 → 内容视图类名)
        private static readonly string[] TabContent =
        {
            "DailyTaskView", "DailyLimitActivityView", "BrightSeaEnterView",
            "DailyResFindView", "DepositView", "DailyStrongerView"
        };
        // 该标签内容视图是否已在 Unity 写好(写好才开放;其余 disabled,写完置 true 即开)
        private static readonly bool[] TabEnabled = { false, false, false, true, false, false };
        private const int DefaultTab = 3;

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

        /// <summary>打开每日模块内子窗(寻宝确认 DailyResFindTipsView…),按 View 子类名在内容源里查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Daily", "OpenSub({0}) 时每日模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Daily", "每日子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("Daily", "六标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            // 内容源全隐藏,内容按需 reparent 进窗框内容区
            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

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
                bool enabled = TabEnabled[i];
                specs.Add(new TabSpec
                {
                    Enabled = enabled,
                    ContentFactory = enabled ? (Func<RectTransform, BaseView>)(parent => ReparentContent(viewName, parent)) : null,
                });
            }

            _window.Show();
            _window.Configure(specs, DefaultTab);
            GameLog.Info("Daily", "每日六标签窗打开(BaseWindowSkinView,默认 tab{0} 资源找回)", DefaultTab);
        }

        /// <summary>把内容源里名为 viewName 的内容视图 reparent 进窗框内容区(保留其原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("Daily", "内容视图 [{0}] 不在 DailyModule 顶层", viewName);
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
