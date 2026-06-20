using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临(九霄劫魄,主界面功能图标 res="232")模块编排:走 BaseWindowSkinView 多标签窗(**第四个走地基的分页大窗**)。
    ///
    /// 老端 GodBefallMainView 是 BaseItem1 自定义富视图(内含强化/升星/觉醒 channel),非 BaseWindowComponent;其原生容器较复杂未整窗移植。
    /// **务实承载**:GodBefallModule 已写 5 个顶层视图,其中 3 个为主页(神契装备/回收/合成,无关闭按钮)、Way/Tips 为弹窗(Way 带 _btn_close、Tips 经 OPEN_VIEW)。
    /// 此前仅「取首页」只 Show GodBefallEquipView → Recovery/Synthesis 已写却不可达(孤立)。本 Flow 用通用窗框承载 3 主页为标签,**让孤立页可达**;
    /// Way/Tips 经 <see cref="OpenSub"/> 弹出。原生 channel/容器细节属「效果」,待 实机/深读 校准。入口 <see cref="GodBefallBootstrap"/>(MainUIRouter "232")。
    /// </summary>
    public static class GodBefallFlow
    {
        private const string CONTENT_MODULE = "godBefall";
        private const string CONTENT_PREFAB = "GodBefallModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 3 主页(神契装备/回收/合成);Way/Tips 为弹窗 → 不作标签,走 OpenSub。
        private static readonly string[] TabContent =
        {
            "GodBefallEquipView", "GodBefallRecoveryView", "GodBefallSynthesisView"
        };
        private static readonly bool[] TabEnabled = { true, true, true };
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

        /// <summary>打开神祇模块内子窗/弹窗(途径 GodBefallWayView、秘闻 GodBefallTipsView…),按 View 子类名在内容源里查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("GodBefall", "OpenSub({0}) 时神祇模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("GodBefall", "神祇子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("GodBefall", "多标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Warn("GodBefall", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
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
            GameLog.Info("GodBefall", "神祇三标签窗打开(BaseWindowSkinView,默认 tab{0} 神契装备)", DefaultTab);
        }

        /// <summary>把内容源里名为 viewName 的内容视图 reparent 进窗框内容区(保留其原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentContent(string viewName, RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform t = _contentRoot.transform.Find(viewName);
            if (t == null)
            {
                GameLog.Warn("GodBefall", "内容视图 [{0}] 不在 GodBefallModule 顶层", viewName);
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
