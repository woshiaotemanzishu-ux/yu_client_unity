using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝入口：Basic 回包决定独立孕育页或共享窗框内的培养基线。</summary>
    public static class BabyFlow
    {
        private const string ContentModule = "baby";
        private const string ContentPrefab = "BabyModule";
        private const string FrameModule = "common";
        private const string FramePrefab = "BaseWindowSkin";
        private static readonly string[] Tabs = { "培养", "家庭", "皮肤" };
        private static readonly string WindowBg = GameResPath.GetBigBgPath("uibbsj_013.jpg");

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static GestateBabyView _gestateView;
        private static BabyFamilyView _familyView;
        private static BabyCultivateView _cultivateView;
        private static BabyIllusionView _illusionView;
        private static bool _loading;
        private static bool _listening;
        private static bool _windowConfigured;
        private static bool _illusionRedLoading;
        private static bool _cultivateRedLoading;

        public static void Toggle()
        {
            if (IsShown()) { Close(); return; }
            Open();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            HideViews();
            StopListening();
        }

        internal static void Reset()
        {
            Close();
            StopListening();
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _gestateView = null;
            _familyView = null;
            _cultivateView = null;
            _illusionView = null;
            _loading = false;
            _windowConfigured = false;
            _illusionRedLoading = false;
            _cultivateRedLoading = false;
        }

        private static async Task OpenAsync()
        {
            if (_contentRoot != null)
            {
                StartListening();
                BabyController.Instance.RequestStartup();
                DecideView();
                return;
            }
            if (_loading) return;

            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FrameModule, FramePrefab);
            string contentKey = GameResPath.GetUIPrefab(ContentModule, ContentPrefab);
            try
            {
                _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync("182", "Baby", frameKey,
                    ViewManager.GetLayer(UILayer.Window));
                _contentRoot = _frameRoot != null
                    ? await MainUIRouteFallback.InstantiateOrShowAsync("182", "Baby", contentKey,
                        ViewManager.GetLayer(UILayer.Window))
                    : null;
            }
            catch (Exception e)
            {
                GameLog.Error("Baby", "Baby module load failed: {0}", e.Message);
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null)
            {
                MainUIRouteFallback.ShowUnavailable("182", "Baby", "BabyModule/BaseWindowSkin load failed");
                Reset();
                return;
            }

            _frameRoot.name = FramePrefab;
            _contentRoot.name = ContentPrefab;
            _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            _gestateView = _contentRoot.GetComponentInChildren<GestateBabyView>(true);
            _familyView = _contentRoot.GetComponentInChildren<BabyFamilyView>(true);
            _cultivateView = _contentRoot.GetComponentInChildren<BabyCultivateView>(true);
            _illusionView = _contentRoot.GetComponentInChildren<BabyIllusionView>(true);
            if (_window == null || _gestateView == null || _familyView == null || _cultivateView == null || _illusionView == null)
            {
                GameLog.Error("Baby", "BabyModule missing required business view; run BabyBindUpgrader");
                Reset();
                return;
            }

            foreach (Transform child in _contentRoot.transform) child.gameObject.SetActive(false);
            _frameRoot.SetActive(false);
            StartListening();
            BabyController.Instance.RequestStartup();
            DecideView();
        }

        private static void StartListening()
        {
            if (_listening) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            _listening = true;
        }

        private static void StopListening()
        {
            if (!_listening) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            _listening = false;
        }

        private static void OnBabyUpdate(int command)
        {
            DecideView();
        }

        private static void OnBagUpdate()
        {
            RefreshCultivateTabRed();
            RefreshIllusionTabRed();
        }

        private static void DecideView()
        {
            BabyBasicInfo basic = BabyModel.Instance.Basic;
            if (basic == null)
            {
                HideViews();
                return;
            }

            if (!basic.IsActive)
            {
                if (_window != null) _window.Hide();
                if (_gestateView != null && !_gestateView.IsShown) _gestateView.Show();
                return;
            }

            if (_gestateView != null) _gestateView.Hide();
            if (_window == null || _cultivateView == null) return;
            _window.Show();
            if (!_windowConfigured)
            {
                _window.ConfigureShared(Tabs.Length, ReparentCultivate, null, 0,
                    index => index >= 0 && index < Tabs.Length,
                    new Dictionary<int, Func<RectTransform, BaseView>>
                    {
                        { 1, ReparentFamily },
                        { 2, ReparentIllusion }
                    },
                    Tabs, null, WindowBg);
                _windowConfigured = true;
            }
            else
            {
                int index = _window.CurrentIndex;
                _window.SelectShared(index >= 0 && index < Tabs.Length ? index : 0);
            }
            RefreshCultivateTabRed();
            RefreshIllusionTabRed();
        }

        private static void RefreshCultivateTabRed()
        {
            if (_window == null || !_windowConfigured) return;
            bool taskRed = BabyModel.Instance.HasClaimableRaiseTask();
            if (!BabyValueConfigs.IsLoaded || !BabyStageConfigs.IsLoaded)
            {
                _window.SetTabRed(0, taskRed);
                if (!_cultivateRedLoading) _ = EnsureCultivateRedConfigsAsync();
                return;
            }
            _window.SetTabRed(0, taskRed || BabyModel.Instance.HasStageUpgradeRed());
        }

        private static async Task EnsureCultivateRedConfigsAsync()
        {
            _cultivateRedLoading = true;
            try
            {
                await BabyValueConfigs.EnsureLoaded();
                await BabyStageConfigs.EnsureLoaded();
            }
            finally
            {
                _cultivateRedLoading = false;
            }
            if (_window != null && _windowConfigured) RefreshCultivateTabRed();
        }

        private static void RefreshIllusionTabRed()
        {
            if (_window == null || !_windowConfigured) return;
            if (!BabyFigureConfigs.IsLoaded || !BabyFigureStarConfigs.IsLoaded)
            {
                if (!_illusionRedLoading) _ = EnsureIllusionRedConfigsAsync();
                return;
            }
            _window.SetTabRed(2, BabyIllusionView.HasAnyRed());
        }

        private static async Task EnsureIllusionRedConfigsAsync()
        {
            _illusionRedLoading = true;
            try
            {
                await BabyFigureConfigs.EnsureLoaded();
                await BabyFigureStarConfigs.EnsureLoaded();
            }
            finally
            {
                _illusionRedLoading = false;
            }
            if (_window != null && _windowConfigured) RefreshIllusionTabRed();
        }

        private static BaseView ReparentCultivate(RectTransform parent)
        {
            _cultivateView.transform.SetParent(parent, false);
            _cultivateView.gameObject.SetActive(true);
            return _cultivateView;
        }

        private static BaseView ReparentFamily(RectTransform parent)
        {
            _familyView.transform.SetParent(parent, false);
            _familyView.gameObject.SetActive(true);
            return _familyView;
        }

        private static BaseView ReparentIllusion(RectTransform parent)
        {
            _illusionView.transform.SetParent(parent, false);
            _illusionView.gameObject.SetActive(true);
            return _illusionView;
        }

        private static void HideViews()
        {
            if (_gestateView != null) _gestateView.Hide();
            if (_window != null) _window.Hide();
        }

        private static bool IsShown()
        {
            return (_gestateView != null && _gestateView.IsShown)
                || (_window != null && _window.IsShown);
        }
    }
}
