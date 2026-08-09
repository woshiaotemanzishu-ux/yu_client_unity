using System.Threading.Tasks;
using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.FriendInvite
{
    /// <summary>按需打开现有 FriendInviteModule；本轮只接可安全展示/关闭的只读主窗。</summary>
    public static class FriendInviteFlow
    {
        private const string Module = "friendInvite";
        private const string Prefab = "FriendInviteModule";
        private static GameObject _moduleRoot;
        private static FriendInviteMainView _mainView;
        private static bool _loading;
        private static int _generation;

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

            int generation = _generation;
            _loading = true;
            GameObject root = null;
            try
            {
                string key = GameResPath.GetUIPrefab(Module, Prefab);
                root = await MainUIRouteFallback.InstantiateOrShowAsync(
                    FriendInviteModel.ICON_TYPE, "FriendInvite", key, ViewManager.GetLayer(UILayer.Window));

                if (generation != _generation)
                {
                    if (root != null) ResManager.ReleaseInstance(root);
                    return;
                }
                if (root == null) return;

                _moduleRoot = root;
                root = null;
                _moduleRoot.name = Prefab;
                foreach (Transform child in _moduleRoot.transform) child.gameObject.SetActive(false);
                _mainView = _moduleRoot.GetComponentInChildren<FriendInviteMainView>(true);
                if (_mainView == null)
                {
                    GameLog.Warn("FriendInvite", "FriendInviteModule missing FriendInviteMainView");
                    MainUIRouteFallback.ShowUnavailable(FriendInviteModel.ICON_TYPE, "FriendInvite", "FriendInviteModule missing runtime main view");
                    Reset();
                    return;
                }
                if (generation != _generation)
                {
                    Reset();
                    return;
                }
                _mainView.Show();
            }
            catch (Exception ex)
            {
                if (root != null) ResManager.ReleaseInstance(root);
                if (generation == _generation) Reset();
                GameLog.Error("FriendInvite", "FriendInviteModule open failed: {0}", ex.Message);
            }
            finally
            {
                if (generation == _generation) _loading = false;
            }
        }

        internal static void Reset()
        {
            _generation++;
            FriendInviteMainView view = _mainView;
            GameObject root = _moduleRoot;
            _moduleRoot = null;
            _mainView = null;
            _loading = false;
            if (view != null) view.PrepareForRelease();
            if (root != null) ResManager.ReleaseInstance(root);
        }
    }
}
