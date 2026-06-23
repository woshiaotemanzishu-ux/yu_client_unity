using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Mirrors the old BaseWindowComponent behavior: base windows keep the top money bar
    /// and bottom function bar visible above the window chrome.
    /// </summary>
    public static class MainUIWindowLayerBridge
    {
        private const int MaxRetryFrames = 60;

        private struct ParentSnapshot
        {
            public Transform Parent;
            public int SiblingIndex;
        }

        private struct TopVisibilitySnapshot
        {
            public MainUITopView View;
            public bool HeadVisible;
            public bool MapVisible;
            public bool IconVisible;
        }

        private static readonly Dictionary<Transform, ParentSnapshot> Snapshots = new Dictionary<Transform, ParentSnapshot>();
        private static BridgeDriver _driver;
        private static int _openWindowCount;
        private static int _retryFramesRemaining;
        private static bool _installed;
        private static bool _hasTopVisibilitySnapshot;
        private static TopVisibilitySnapshot _topVisibility;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_installed) return;

            _installed = true;
            EventDispatcher.On(GlobalEvent.EVT_BASE_WINDOW_OPENED, OnBaseWindowOpened);
            EventDispatcher.On(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnBaseWindowClosed);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, Restore);
            EnsureDriver();
        }

        /// <summary>
        /// Editor RunCommand harnesses may reset static state while Play objects stay alive.
        /// Runtime installs automatically; harnesses can call this idempotent entry explicitly.
        /// </summary>
        public static void EnsureInstalled() => Install();

        private static void OnBaseWindowOpened()
        {
            _openWindowCount++;
            _retryFramesRemaining = MaxRetryFrames;
            EnsureDriver();
            if (RaiseBars()) _retryFramesRemaining = 0;
        }

        private static void OnBaseWindowClosed()
        {
            if (_openWindowCount > 0) _openWindowCount--;
            if (_openWindowCount == 0)
            {
                if (HasShownBaseWindow())
                {
                    _openWindowCount = 1;
                    _retryFramesRemaining = MaxRetryFrames;
                    EnsureDriver();
                    if (RaiseBars()) _retryFramesRemaining = 0;
                    return;
                }

                Restore();
            }
        }

        private static bool RaiseBars()
        {
            Transform windowLayer = ViewManager.GetLayer(UILayer.Window);
            if (windowLayer == null) return false;

            MainUITopView top = FindSceneView<MainUITopView>();
            ApplyBaseWindowTopState(top);
            MoveToWindowLayer(top, windowLayer);
            MainUIDownView down = FindSceneView<MainUIDownView>();
            MoveToWindowLayer(down, windowLayer);
            return top != null && down != null;
        }

        private static void TickRetry()
        {
            if (_openWindowCount <= 0)
            {
                if (!HasShownBaseWindow()) return;
                _openWindowCount = 1;
                _retryFramesRemaining = MaxRetryFrames;
            }

            if (_retryFramesRemaining <= 0) return;

            _retryFramesRemaining--;
            if (RaiseBars()) _retryFramesRemaining = 0;
        }

        private static bool HasShownBaseWindow()
        {
            BaseWindowSkinView[] views = Object.FindObjectsByType<BaseWindowSkinView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                BaseWindowSkinView view = views[i];
                if (view != null &&
                    view.IsShown &&
                    view.gameObject.activeInHierarchy &&
                    view.gameObject.scene.IsValid())
                {
                    return true;
                }
            }

            return false;
        }

        private static T FindSceneView<T>() where T : Component
        {
            T[] views = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                T view = views[i];
                if (view != null && view.gameObject.scene.IsValid())
                {
                    return view;
                }
            }
            return null;
        }

        private static void MoveToWindowLayer(Component view, Transform windowLayer)
        {
            if (view == null || windowLayer == null) return;

            Transform t = view.transform;
            if (!Snapshots.ContainsKey(t))
            {
                Snapshots[t] = new ParentSnapshot
                {
                    Parent = t.parent,
                    SiblingIndex = t.GetSiblingIndex(),
                };
            }

            if (t.parent != windowLayer)
            {
                t.SetParent(windowLayer, false);
            }
            t.SetAsLastSibling();
        }

        private static void ApplyBaseWindowTopState(MainUITopView top)
        {
            if (top == null) return;

            if (!_hasTopVisibilitySnapshot || _topVisibility.View != top)
            {
                _topVisibility = new TopVisibilitySnapshot
                {
                    View = top,
                    HeadVisible = top.HeadVisible,
                    MapVisible = top.MapVisible,
                    IconVisible = top.IconVisible,
                };
                _hasTopVisibilitySnapshot = true;
            }

            top.SetHeadVisible(false);
            top.SetMapVisible(false);
        }

        private static void Restore()
        {
            _retryFramesRemaining = 0;
            RestoreTopVisibility();

            foreach (KeyValuePair<Transform, ParentSnapshot> kv in Snapshots)
            {
                Transform t = kv.Key;
                ParentSnapshot snapshot = kv.Value;
                if (t == null || snapshot.Parent == null) continue;

                t.SetParent(snapshot.Parent, false);
                int index = Mathf.Clamp(snapshot.SiblingIndex, 0, snapshot.Parent.childCount - 1);
                t.SetSiblingIndex(index);
            }
            Snapshots.Clear();
            _openWindowCount = 0;
        }

        private static void RestoreTopVisibility()
        {
            if (!_hasTopVisibilitySnapshot) return;

            MainUITopView top = _topVisibility.View;
            if (top != null)
            {
                top.SetBaseWindowTopVisible(
                    _topVisibility.HeadVisible,
                    _topVisibility.MapVisible,
                    _topVisibility.IconVisible);
            }

            _hasTopVisibilitySnapshot = false;
            _topVisibility = default;
        }

        private static void EnsureDriver()
        {
            if (_driver != null) return;

            GameObject go = new GameObject("MainUIWindowLayerBridge");
            Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<BridgeDriver>();
        }

        private sealed class BridgeDriver : MonoBehaviour
        {
            private void Update()
            {
                TickRetry();
            }
        }
    }
}
