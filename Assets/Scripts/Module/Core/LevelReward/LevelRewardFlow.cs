using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励模块编排:按需打开/关闭等级奖励面板(对标老端 二级 HUD 等级奖励按钮 → LevelRewardView)。
    /// 打开 LevelRewardModule 后隐藏所有顶层窗口再只 Show LevelRewardView。主面板无关闭按钮 → 二级 HUD 按钮再点关闭(Toggle)。
    /// 入口注册见 <see cref="LevelRewardBootstrap"/>(MainUIRouter "levelreward")。
    /// </summary>
    public static class LevelRewardFlow
    {
        private const string MODULE = "levelReward";
        private const string PREFAB = "LevelRewardModule";

        private static GameObject _moduleRoot;
        private static LevelRewardView _mainView;
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
                GameLog.Error("LevelReward", "LevelRewardModule prefab load failed: {0}", key);
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
                if (v is LevelRewardView lv) { _mainView = lv; break; }
            }

            if (_mainView == null)
            {
                GameLog.Warn("LevelReward", "LevelRewardModule 缺 LevelRewardView(重跑 levelReward 流水线:转换+回填)");
                return;
            }

            _mainView.Show();
            GameLog.Info("LevelReward", "等级奖励面板打开: {0}", key);
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
