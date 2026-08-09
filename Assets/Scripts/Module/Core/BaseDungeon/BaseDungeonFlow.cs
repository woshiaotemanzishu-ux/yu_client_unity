using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.BaseDungeon
{
    /// <summary>限时塔入口编排；复用现有 BaseWindowSkin 与 DungeonTowerModule，不重建 Prefab。</summary>
    public static class BaseDungeonFlow
    {
        private const string ModuleName = "dungeontower";
        private const string ModulePrefab = "DungeonTowerModule";
        private const string FrameModule = "common";
        private const string FramePrefab = "BaseWindowSkin";

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static DungeonTowerView _view;
        private static DungeonTowerItemView _itemTemplate;
        private static bool _loading;
        private static bool _configured;
        private static int _configuredRound;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            Open();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_view != null) _view.Hide();
            if (_window != null) _window.Hide();
        }

        internal static void Reset()
        {
            Close();
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _view = null;
            _itemTemplate = null;
            _loading = false;
            _configured = false;
            _configuredRound = 0;
        }

        private static async Task OpenAsync()
        {
            BaseDungeonModel model = BaseDungeonModel.Instance;
            if (!model.GetTowerIconOpenState())
            {
                GameLog.Info("BaseDungeon", "限时塔入口当前不可用，重新请求 61117");
                BaseDungeonController.Instance.RequestStartup();
                return;
            }

            if (_window != null && _view != null)
            {
                ShowWindow();
                return;
            }
            if (_loading) return;

            _loading = true;
            try
            {
                Transform layer = ViewManager.GetLayer(UILayer.Window);
                _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(
                    BaseDungeonModel.TOWER_ICON_TYPE, "BaseDungeon",
                    GameResPath.GetUIPrefab(FrameModule, FramePrefab), layer);
                _contentRoot = _frameRoot != null
                    ? await MainUIRouteFallback.InstantiateOrShowAsync(
                        BaseDungeonModel.TOWER_ICON_TYPE, "BaseDungeon",
                        GameResPath.GetUIPrefab(ModuleName, ModulePrefab), layer)
                    : null;
            }
            catch (Exception e)
            {
                GameLog.Error("BaseDungeon", "DungeonTowerModule load failed: {0}", e.Message);
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null)
            {
                MainUIRouteFallback.ShowUnavailable(BaseDungeonModel.TOWER_ICON_TYPE, "BaseDungeon",
                    "DungeonTowerModule/BaseWindowSkin load failed");
                Reset();
                return;
            }

            _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            _view = _contentRoot.GetComponentInChildren<DungeonTowerView>(true);
            _itemTemplate = _contentRoot.GetComponentInChildren<DungeonTowerItemView>(true);
            if (_window == null || _view == null || _itemTemplate == null)
            {
                GameLog.Error("BaseDungeon", "DungeonTowerModule 缺业务 View/Item 绑定");
                Reset();
                return;
            }

            foreach (Transform child in _contentRoot.transform) child.gameObject.SetActive(false);
            _itemTemplate.gameObject.SetActive(false);
            _frameRoot.SetActive(false);
            ShowWindow();
        }

        private static void ShowWindow()
        {
            if (_window == null || _view == null) return;
            _window.Show();
            int roundValue = BaseDungeonModel.Instance.Round <= 0 ? 1 : BaseDungeonModel.Instance.Round;
            if (!_configured || _configuredRound != roundValue)
            {
                BaseDungeonModel model = BaseDungeonModel.Instance;
                string round = roundValue.ToString();
                _configured = true;
                _configuredRound = roundValue;
                _window.ConfigureShared(1, ReparentTower, null, 0, null, null,
                    new[] { model.GetLimitTowerName() },
                    new[] { GameResPath.GetIcon(ModuleName, "act_title_" + round) },
                    GameResPath.GetBigBgPath("ui_limit_tower_" + round + ".jpg"));
            }
            else
            {
                _window.SelectShared(0);
            }
        }

        internal static void RefreshWindowChrome()
        {
            if (_window == null || !_window.IsShown) return;
            int round = BaseDungeonModel.Instance.Round <= 0 ? 1 : BaseDungeonModel.Instance.Round;
            if (!_configured || _configuredRound != round) ShowWindow();
        }

        private static BaseView ReparentTower(RectTransform parent)
        {
            _view.transform.SetParent(parent, false);
            _view.gameObject.SetActive(true);
            return _view;
        }
    }
}
