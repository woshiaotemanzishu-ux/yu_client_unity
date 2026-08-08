using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Suit;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Resonance
{
    /// <summary>角色页“共鸣”入口：共享四页签窗框 + 单份可编辑 SuitModule 内容 Prefab。</summary>
    public static class ResonanceFlow
    {
        private const string ContentModule = "suit";
        private const string ContentPrefab = "SuitModule";
        private const string FrameModule = "common";
        private const string FramePrefab = "BaseWindowSkin";
        private const string PopupContainerName = "ResonancePopupLayer";
        private const string PreviewMaskName = "ResonancePreviewMask";
        private const string ReturnMaskName = "ResonanceReturnMask";

        private static readonly string[] Labels =
        {
            "妖魂共鸣", "战魂共鸣", "万物共鸣", "饰物共鸣",
        };

        private static readonly string[] Titles =
        {
            GameResPath.GetIcon("suit", "uitz_001"),
            GameResPath.GetIcon("suit", "uitz_001"),
            GameResPath.GetIcon("suit", "uitz_001"),
            GameResPath.GetIcon("suit", "uitz_001"),
        };

        private static readonly string Background = GameResPath.GetBigBgPath("ui_merge_bg.jpg");

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static RectTransform _popupContainer;
        private static BaseWindowSkinView _window;
        private static ResonanceMainView _mainView;
        private static EquipSuitPreviewTipsBind _previewView;
        private static EquipSuitReturnViewBind _returnView;
        private static GameObject _previewMask;
        private static GameObject _returnMask;
        private static bool _loading;
        private static int _requestedTab;

        public static bool IsOpen => _window != null && _window.IsShown;

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public static void Open() => _ = OpenAsync(0);

        public static void Open(int tabIndex) => _ = OpenAsync(tabIndex);

        public static void Close()
        {
            _mainView?.ClosePopups();
            if (_mainView != null && _mainView.IsShown) _mainView.Hide();
            if (_window != null && _window.IsShown) _window.Hide();
        }

        private static async Task OpenAsync(int tabIndex)
        {
            _requestedTab = Mathf.Clamp(tabIndex, 0, Labels.Length - 1);
            if (_frameRoot != null && _window != null && _mainView != null)
            {
                _window.Show();
                if (!_mainView.IsShown) _mainView.Show();
                _window.SelectShared(_requestedTab);
                return;
            }

            if (_loading) return;
            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FrameModule, FramePrefab);
            string contentKey = GameResPath.GetUIPrefab(ContentModule, ContentPrefab);
            try
            {
                Transform windowLayer = ViewManager.GetLayer(UILayer.Window);
                Task<GameObject> frameTask = MainUIRouteFallback.InstantiateOrShowAsync(
                    ContentModule, "Resonance", frameKey, windowLayer);
                Task<GameObject> contentTask = MainUIRouteFallback.InstantiateOrShowAsync(
                    ContentModule, "Resonance", contentKey, windowLayer);
                await Task.WhenAll(frameTask, contentTask);
                _frameRoot = frameTask.Result;
                _contentRoot = contentTask.Result;
            }
            catch (Exception exception)
            {
                GameLog.Error("Resonance", "window load failed: {0}", exception.Message);
                ShowPlaceholderAndReset();
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Resonance", "window load returned null frame={0} content={1}", frameKey, contentKey);
                ShowPlaceholderAndReset();
                return;
            }

            _frameRoot.name = FramePrefab;
            _contentRoot.name = ContentPrefab;
            foreach (Transform child in _contentRoot.transform) child.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Error("Resonance", "BaseWindowSkinView missing");
                ShowPlaceholderAndReset();
                return;
            }

            _popupContainer = FindRect(_contentRoot.transform, PopupContainerName);
            if (_popupContainer == null)
            {
                GameLog.Error("Resonance", "prefab-owned popup container missing: {0}", PopupContainerName);
                ShowPlaceholderAndReset();
                return;
            }
            _popupContainer.SetParent(ViewManager.GetLayer(UILayer.Popup), false);
            Stretch(_popupContainer);
            _popupContainer.gameObject.SetActive(true);

            _window.Show();
            _window.SetReturnAction(Close);
            _window.ConfigureShared(Labels.Length, ReparentMain, OnTabSelected, _requestedTab,
                null, null, Labels, Titles, Background);
            GameLog.Info("Resonance", "opened tab={0}({1})", _requestedTab, Labels[_requestedTab]);
        }

        private static BaseView ReparentMain(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            EquipSuitMianViewBind mainBind = _contentRoot.GetComponentInChildren<EquipSuitMianViewBind>(true);
            _mainView = mainBind != null ? mainBind.GetComponent<ResonanceMainView>() : null;
            _previewView = _popupContainer != null
                ? _popupContainer.GetComponentInChildren<EquipSuitPreviewTipsBind>(true) : null;
            _returnView = _popupContainer != null
                ? _popupContainer.GetComponentInChildren<EquipSuitReturnViewBind>(true) : null;
            Transform previewMask = _popupContainer != null ? Find(_popupContainer, PreviewMaskName) : null;
            Transform returnMask = _popupContainer != null ? Find(_popupContainer, ReturnMaskName) : null;
            _previewMask = previewMask != null ? previewMask.gameObject : null;
            _returnMask = returnMask != null ? returnMask.gameObject : null;
            if (_mainView == null || _previewView == null || _returnView == null
                || _previewMask == null || _returnMask == null)
            {
                GameLog.Error("Resonance", "required prefab views/masks missing main={0} preview={1} return={2} pm={3} rm={4}",
                    _mainView != null, _previewView != null, _returnView != null,
                    _previewMask != null, _returnMask != null);
                return null;
            }

            _mainView.transform.SetParent(parent, false);
            _mainView.Configure(_previewView, _returnView, _previewMask, _returnMask);
            _mainView.gameObject.SetActive(true);
            return _mainView;
        }

        private static void OnTabSelected(int index)
        {
            _requestedTab = Mathf.Clamp(index, 0, Labels.Length - 1);
            _mainView?.SetTab(_requestedTab);
        }

        public static void Reset()
        {
            Close();
            if (_popupContainer != null && _contentRoot != null)
                _popupContainer.SetParent(_contentRoot.transform, false);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _popupContainer = null;
            _window = null;
            _mainView = null;
            _previewView = null;
            _returnView = null;
            _previewMask = null;
            _returnMask = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            MainUIRouteFallback.ShowUnavailable(ContentModule, "Resonance", "SuitModule/BaseWindowSkin incomplete");
            Reset();
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            Transform found = Find(root, name);
            return found as RectTransform;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
