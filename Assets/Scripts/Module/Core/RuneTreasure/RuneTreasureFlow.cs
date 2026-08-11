using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Rune;
using UnityEngine;

namespace Shenxiao.Module.Core.RuneTreasure
{
    public static class RuneTreasureFlow
    {
        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static RuneTreasureMainView _view;
        private static bool _loading;

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_view != null && _view.IsShown) _view.Hide();
            if (_window != null) _window.Hide();
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                if (!await EnsureViewAsync()) return;
                _window.SetReturnAction(ReturnToRune);
                _window.Show();
                _window.Configure(BuildTabs(), 1);
            }
            finally { _loading = false; }
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_window != null && _view != null) return true;
            Transform layer = ViewManager.GetLayer(UILayer.Window);
            Task<GameObject> frame = ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "BaseWindowSkin"), layer);
            Task<GameObject> content = ResManager.InstantiateAsync(
                "resource/game/runeTreasure/prefab/RuneTreasureMainView.prefab", layer);
            await Task.WhenAll(frame, content);
            _frameRoot = frame.Result;
            _contentRoot = content.Result;
            if (_frameRoot == null || _contentRoot == null) { Release(); return false; }
            _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            _view = _contentRoot.GetComponentInChildren<RuneTreasureMainView>(true);
            if (_window == null || _view == null) { Release(); return false; }
            _view.gameObject.SetActive(false);
            return true;
        }

        private static BaseView Reparent(RectTransform parent)
        {
            _view.transform.SetParent(parent, false);
            RectTransform rect = _view.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return _view;
        }

        private static TabSpec[] BuildTabs() => new[]
        {
            MissingTab("神兵夺宝", "rt_icon_title", "uizbxb_002a_720x1222.jpg",
                "神兵夺宝等待同账号运行快照与后台Unity首次烤制"),
            new TabSpec
            {
                Enabled = true,
                Label = "太初召劫",
                TitleImagePath = GameResPath.GetIcon("runeTreasure", "uizbxb_021"),
                BackgroundImagePath = GameResPath.GetBigBgPath("uizbxb_002d.jpg"),
                ContentFactory = Reparent,
            },
            MissingTab("凌霄夺宝", "rt_icon_title", "uizbxb_002b_720x1222.jpg",
                "凌霄夺宝等待运行快照与首次烤制，并需核对371级开放态"),
            MissingTab("天尊夺宝", "rt_icon_title", "uizbxb_002c_720x1222.jpg",
                "天尊夺宝等待运行快照与首次烤制，并需核对500级开放态"),
        };

        private static TabSpec MissingTab(string label, string title, string background, string reason) =>
            new TabSpec
            {
                Enabled = true,
                Label = label,
                TitleImagePath = GameResPath.GetIcon("runeTreasure", title),
                BackgroundImagePath = GameResPath.GetBigBgPath(background),
                OpenCheck = () => false,
                LockedToast = reason,
            };

        private static void ReturnToRune()
        {
            Close();
            RuneFlow.Open();
        }

        private static void Release()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _view = null;
        }
    }
}
