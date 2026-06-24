using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// Opens the old-client GuildHelpView entry from MainUI.
    /// Uses the generated GuildModule safely until a dedicated business View is backfilled.
    /// </summary>
    public static class GuildHelpFlow
    {
        private const string MODULE = "guild";
        private const string PREFAB = "GuildModule";

        private static GameObject _moduleRoot;
        private static GuildHelpViewBind _helpView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_helpView != null && _helpView.IsShown)
            {
                Close();
                return;
            }

            _ = OpenAsync();
        }

        public static void Open()
        {
            _ = OpenAsync();
        }

        public static void Close()
        {
            if (_helpView != null)
            {
                _helpView.Hide();
            }
        }

        private static async Task OpenAsync()
        {
            if (_moduleRoot != null)
            {
                ShowHelp();
                return;
            }

            if (_loading)
            {
                return;
            }

            _loading = true;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Guild", "GuildModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            foreach (BaseView view in root.GetComponentsInChildren<BaseView>(true))
            {
                view.gameObject.SetActive(false);
            }

            _helpView = root.GetComponentInChildren<GuildHelpViewBind>(true);
            if (_helpView == null)
            {
                GameLog.Warn("Guild", "GuildModule missing GuildHelpViewBind; rerun guild LayaUI convert/backfill.");
                return;
            }

            ShowHelp();
        }

        private static void ShowHelp()
        {
            if (_helpView == null)
            {
                return;
            }

            _helpView.Show();
            BindClose();
            GameLog.Info("Guild", "GuildHelpView opened from MainUI.");
        }

        private static void BindClose()
        {
            if (_helpView == null || _helpView._btn_close == null)
            {
                return;
            }

            if (_helpView.GetType() != typeof(GuildHelpViewBind))
            {
                return;
            }

            UIUtil.ClearClicks(_helpView._btn_close);
            UIUtil.AddClick(_helpView._btn_close, Close);
        }

        internal static void Reset()
        {
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
            }

            _moduleRoot = null;
            _helpView = null;
            _loading = false;
        }
    }
}
