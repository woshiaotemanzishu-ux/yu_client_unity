using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日活动·资源找回流程:按需打开/关闭(对标老端 二级 HUD 每日寻宝按钮 → DailyView 标签3「资源找回」)。
    ///
    /// 老端 DailyView 是 BaseWindowComponent 六标签容器(每日任务/限时活动/无尽之海/资源找回/托管中心/我要变强);
    /// 该窗框架(标签条+内容栈)未移植 → 本入口降级:直接打开 tab3 内容页 DailyResFindView(即每日寻宝按钮的目标内容)。
    /// 打开 DailyModule 后隐藏所有顶层窗口再只 Show DailyResFindView;寻宝确认弹窗 DailyResFindTipsView 经 <see cref="OpenSub"/> 叠开。
    /// 内容页无关闭按钮 → 二级 HUD 按钮再点关闭(Toggle)。入口注册见 <see cref="DailyBootstrap"/>(MainUIRouter "dailyfind")。
    /// </summary>
    public static class DailyFlow
    {
        private const string MODULE = "daily";
        private const string PREFAB = "DailyModule";

        private static GameObject _moduleRoot;
        private static DailyResFindView _mainView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_mainView != null && _mainView.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_mainView != null) _mainView.Hide();
        }

        /// <summary>打开每日模块内子窗(寻宝确认 DailyResFindTipsView…),叠在内容页上;按 View 子类名查找。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Daily", "OpenSub({0}) 时每日模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Daily", "每日子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                if (_mainView != null) _mainView.Show();
                return;
            }

            if (_loading) return;
            _loading = true;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Daily", "DailyModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            foreach (Transform c in root.transform)
            {
                c.gameObject.SetActive(false);
            }

            foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
            {
                if (v is DailyResFindView dv) { _mainView = dv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Daily", "DailyModule 缺 DailyResFindView(重跑 daily 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("Daily", "每日·资源找回页打开: {0}", key);
        }

        internal static void Reset()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _mainView = null;
            _loading = false;
        }
    }
}
