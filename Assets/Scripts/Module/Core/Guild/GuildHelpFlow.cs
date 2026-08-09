using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// Opens the old-client GuildHelpView entry from MainUI and delegates data/interaction to
    /// <see cref="GuildHelpRuntime"/> while preserving GuildModule.prefab as the visual fact source.
    /// </summary>
    public static class GuildHelpFlow
    {
        private const string MODULE = "guild";
        private const string PREFAB = "GuildModule";

        private static GameObject _moduleRoot;
        private static GuildHelpViewBind _helpView;
        private static GuildHelpTipsViewBind _tipsView;
        private static GuildHelpRuntime _runtime;
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
            _runtime?.Close();
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
            // 老端 GuildHelpView/GuildHelpTipsView 都在 Activity 层；Unity 等价层为 Popup，必须高于主窗。
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
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

            _tipsView = root.GetComponentInChildren<GuildHelpTipsViewBind>(true);
            if (_tipsView == null)
            {
                GameLog.Warn("Guild", "GuildModule missing GuildHelpTipsViewBind; GuildHelp confirmation remains unavailable.");
            }
            _runtime = new GuildHelpRuntime(_helpView, _tipsView);

            ShowHelp();
        }

        private static void ShowHelp()
        {
            if (_helpView == null)
            {
                return;
            }

            _runtime?.Show();
            GameLog.Info("Guild", "GuildHelpView opened from MainUI with 40405/40031/18916 consumers.");
        }

        internal static void Reset()
        {
            _runtime?.Dispose();
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
            }

            _moduleRoot = null;
            _helpView = null;
            _tipsView = null;
            _runtime = null;
            _loading = false;
        }
    }
}
