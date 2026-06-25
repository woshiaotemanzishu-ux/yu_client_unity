using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.AutoBrush;
using UnityEngine;

namespace Shenxiao.Module.Core.AutoBrush
{
    public static class AutoBrushFlow
    {
        private const string MODULE = "autoBrush";
        private const string PREFAB = "AutoBrushModule";

        private static GameObject _moduleRoot;
        private static AutoBrushMainView _mainView;
        private static AutoBrushResultView _resultView;
        private static bool _loading;

        public static void OpenMain()
        {
            _ = OpenMainAsync();
        }

        public static void OpenResult(IReadOnlyList<AutoBrushModel.RewardEntry> rewards, int coin, int exp)
        {
            _ = OpenResultAsync(rewards, coin, exp);
        }

        public static void CloseMain()
        {
            _mainView?.Hide();
        }

        public static void CloseResult()
        {
            _resultView?.Hide();
        }

        private static async Task OpenMainAsync()
        {
            if (!await EnsureLoaded()) return;
            _resultView?.Hide();
            _mainView?.Show();
        }

        private static async Task OpenResultAsync(IReadOnlyList<AutoBrushModel.RewardEntry> rewards, int coin, int exp)
        {
            if (!await EnsureLoaded()) return;
            _mainView?.Hide();
            _resultView?.Show(rewards, coin, exp);
        }

        private static async Task<bool> EnsureLoaded()
        {
            if (_moduleRoot != null && _mainView != null && _resultView != null) return true;
            if (_loading) return false;
            _loading = true;

            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("AutoBrush", "AutoBrushModule prefab load failed: {0}", key);
                return false;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            foreach (BaseView v in root.GetComponentsInChildren<BaseView>(true))
            {
                v.gameObject.SetActive(false);
            }

            AutoBrushMainViewBind mainBind = root.GetComponentInChildren<AutoBrushMainViewBind>(true);
            if (mainBind == null)
            {
                GameLog.Error("AutoBrush", "AutoBrushModule missing AutoBrushMainViewBind. Run autoBrush LayaUI convert + bind backfill.");
                return false;
            }

            AutoBrushResultViewBind bind = root.GetComponentInChildren<AutoBrushResultViewBind>(true);
            if (bind == null)
            {
                GameLog.Error("AutoBrush", "AutoBrushModule missing AutoBrushResultViewBind. Run autoBrush LayaUI convert + bind backfill.");
                return false;
            }

            _mainView = new AutoBrushMainView(mainBind);
            _resultView = new AutoBrushResultView(bind);
            return true;
        }

        internal static void Reset()
        {
            _mainView?.Hide();
            _resultView?.Hide();
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _mainView = null;
            _resultView = null;
            _loading = false;
        }
    }
}
