using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.FirstBlood
{
    /// <summary>
    /// 首杀模块编排:按需打开/关闭首杀面板(对标老端 二级 HUD 首杀按钮 → FirstBloodMainView)。
    /// 打开 FirstBloodModule 后隐藏所有顶层窗口再只 Show FirstBloodMainView;奖励详情 FirstBloodRewardView 经 <see cref="OpenSub"/> 后续接。
    /// 主面板无关闭按钮 → 二级 HUD 按钮再点关闭(Toggle)。入口注册见 <see cref="FirstBloodBootstrap"/>(MainUIRouter "firstblood")。
    /// </summary>
    public static class FirstBloodFlow
    {
        private const string MODULE = "firstBlood";
        private const string PREFAB = "FirstBloodModule";

        private static GameObject _moduleRoot;
        private static FirstBloodMainView _mainView;
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

        /// <summary>打开首杀模块内子窗(奖励详情…),叠在主面板上;按 View 子类名查找。未移植查不到 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                GameLog.Warn("FirstBlood", "OpenSub({0}) 时首杀模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("FirstBlood", "首杀子窗 [{0}] 未移植 View,待对接", viewTypeName);
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
                GameLog.Error("FirstBlood", "FirstBloodModule prefab load failed: {0}", key);
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
                if (v is FirstBloodMainView fv) { _mainView = fv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("FirstBlood", "FirstBloodModule 缺 FirstBloodMainView(重跑 firstBlood 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("FirstBlood", "首杀面板打开: {0}", key);
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
