using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 九霄劫魄(符文)模块编排:按需打开/关闭符文主面板(对标老端 主 HUD 秘宝图标 → SecretTreasureMainView tab0=九霄劫魄 RuneMainUIView)。
    ///
    /// 秘宝容器 SecretTreasureMainView 是 shared-prefab、未随模块生成 → 取默认首页 <c>RuneMainUIView</c> 作 HUD「秘宝」入口
    /// (同 equip/godBefall 取首页范式;其余秘宝分页 山海物华阁/苍龙神章/荒祖遗骸/九霄冥饰 属各自模块,后续生成)。
    /// 打开 RuneModule 后隐藏所有顶层窗口再只 Show RuneMainUIView;符文子窗(转化/技能/觉醒/属性/分解)经 <see cref="OpenSub"/> 叠主面板上。
    /// 入口注册见 <see cref="RuneBootstrap"/>(MainUIRouter "treasure")。主面板无关闭按钮 → 秘宝图标再点关闭(Toggle)。
    /// </summary>
    public static class RuneFlow
    {
        private const string MODULE = "rune";
        private const string PREFAB = "RuneModule";

        private static GameObject _moduleRoot;
        private static RuneMainUIView _mainView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_mainView != null && _mainView.IsShown) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_moduleRoot == null) return;
            foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (view.IsShown) view.Hide();
            }
        }

        /// <summary>打开符文模块内子窗(转化/技能/觉醒/属性/分解…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Rune", "OpenSub({0}) 时符文模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Rune", "符文子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        /// <summary>切换符文模块内子窗:已显则关、未显则开(供主面板按钮再点关闭;部分子窗无关闭按钮靠此收口)。</summary>
        public static void ToggleSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("Rune", "ToggleSub({0}) 时符文模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName)
                {
                    if (v.IsShown) v.Hide(); else v.Show();
                    return;
                }
            }
            GameLog.Info("Rune", "符文子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
            GameObject root = await MainUIRouteFallback.InstantiateOrShowAsync("treasure", "Rune", key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Rune", "RuneModule prefab load failed: {0}", key);
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
                if (v is RuneMainUIView rv) { _mainView = rv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("Rune", "RuneModule 缺 RuneMainUIView(重跑 rune 流水线:转换+回填)");
                MainUIRouteFallback.ShowUnavailable("treasure", "Rune", "RuneModule missing RuneMainUIView");
                Reset();
                return;
            }

            _mainView.Show();
            GameLog.Info("Rune", "九霄劫魄面板打开: {0}", key);
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
