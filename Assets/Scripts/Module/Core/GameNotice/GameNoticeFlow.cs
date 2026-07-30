using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.GameNotice
{
    /// <summary>复用已转换 GameNoticeModule 的登录/游戏内公告窗口编排。</summary>
    public static class GameNoticeFlow
    {
        private const string MODULE = "gamenotice";
        private const string PREFAB = "GameNoticeModule";

        private static GameObject _moduleRoot;
        private static GameNoticeView _view;
        private static bool _loading;
        private static GameNoticeMode _pendingMode;

        public static void OpenLogin() => _ = OpenAsync(GameNoticeMode.Login);
        public static void OpenInside() => _ = OpenAsync(GameNoticeMode.Inside);

        public static void ToggleInside()
        {
            if (_view != null && _view.IsShown)
            {
                Close();
                return;
            }
            OpenInside();
        }

        public static void Close()
        {
            _view?.Hide();
        }

        private static async Task OpenAsync(GameNoticeMode mode)
        {
            _pendingMode = mode;
            if (_view != null)
            {
                _view.Show(mode);
                return;
            }
            if (_loading) return;
            _loading = true;

            GameObject root = null;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            try
            {
                root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
            }
            catch (Exception e)
            {
                GameLog.Error("GameNotice", "公告 prefab 加载异常 key={0}: {1}", key, e.Message);
            }
            finally
            {
                _loading = false;
            }

            if (root == null)
            {
                GameLog.Error("GameNotice", "公告 prefab 加载失败 key={0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;
            _view = root.GetComponentInChildren<GameNoticeView>(true);
            if (_view == null)
            {
                GameLog.Error("GameNotice", "GameNoticeModule 缺 GameNoticeView");
                ResManager.ReleaseInstance(root);
                _moduleRoot = null;
                return;
            }

            _view.gameObject.SetActive(false);
            _view.Show(_pendingMode);
        }

        internal static void Reset()
        {
            _view?.Hide();
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _loading = false;
        }
    }
}
