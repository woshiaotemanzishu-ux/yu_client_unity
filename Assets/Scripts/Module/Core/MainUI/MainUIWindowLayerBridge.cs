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
        private static readonly List<Transform> SnapshotOrder = new List<Transform>();
        private static BridgeDriver _driver;
        private static int _openWindowCount;
        private static int _retryFramesRemaining;
        private static bool _installed;
        private static bool _restorePending;
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
            _restorePending = false;
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
                // BaseView.InternalHide invokes OnHide before clearing IsShown and deactivating
                // the GameObject. Checking synchronously here would see the closing window itself
                // as still shown and leave the MainUI head/map hidden forever.
                _restorePending = true;
                EnsureDriver();
            }
        }

        private static bool RaiseBars()
        {
            Transform windowLayer = ViewManager.GetLayer(UILayer.Window);
            if (windowLayer == null) return false;

            MainUITopView top = FindSceneView<MainUITopView>();
            ApplyBaseWindowTopState(top);
            MainUIDownView down = FindSceneView<MainUIDownView>();

            Transform topCarrier = top != null ? ResolveCarrier(top.transform, windowLayer) : null;
            Transform downCarrier = down != null ? ResolveCarrier(down.transform, windowLayer) : null;

            // Capture every original sibling index before moving either carrier. Moving HudTop
            // first shifts HudNavBar's live index by one and would otherwise restore it above
            // HudChatBar after the base window closes.
            CaptureSnapshot(topCarrier);
            CaptureSnapshot(downCarrier);
            MoveToWindowLayer(topCarrier, windowLayer);
            MoveToWindowLayer(downCarrier, windowLayer);
            return top != null && down != null;
        }

        private static void TickRetry()
        {
            if (_restorePending)
            {
                _restorePending = false;
                if (!HasShownBaseWindow())
                {
                    Restore();
                    return;
                }

                _openWindowCount = 1;
                _retryFramesRemaining = MaxRetryFrames;
            }

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

        private static void CaptureSnapshot(Transform t)
        {
            if (t == null || Snapshots.ContainsKey(t)) return;

            Snapshots[t] = new ParentSnapshot
            {
                Parent = t.parent,
                SiblingIndex = t.GetSiblingIndex(),
            };
            SnapshotOrder.Add(t);
        }

        private static void MoveToWindowLayer(Transform t, Transform windowLayer)
        {
            if (t == null || windowLayer == null) return;

            if (t.parent != windowLayer)
            {
                t.SetParent(windowLayer, false);
            }
            t.SetAsLastSibling();
        }

        /// <summary>
        /// 搬运单位解析:主界面区域化(有界 root)后,视图节点(MainUITopView/MainUIDownView)是 Stretch
        /// 填满各自 Hud* 区域根的——把视图节点【单独】搬进全屏 Window 层会让它撑满全屏,中心锚的子内容
        /// 全部跑到屏幕中央(人物面板开/关后底部导航条跑到屏中的根因)。故搬运单位=区域根(视图节点的
        /// 直接父级,自带有界锚定,挂到全屏层位置天然不变,即便还原时序出岔也不会跑位)。
        /// 兜底:父级缺失/已在目标层/本身是全屏 Stretch 容器(模块根、层根——搬它会把整个 HUD 拎走)时,
        /// 退回搬视图节点自身(兼容旧的全屏壳结构)。
        /// </summary>
        private static Transform ResolveCarrier(Transform viewT, Transform windowLayer)
        {
            Transform parent = viewT.parent;
            if (parent == null || parent == windowLayer) return viewT;
            var prt = parent as RectTransform;
            if (prt == null) return viewT;
            if (prt.anchorMin == Vector2.zero && prt.anchorMax == Vector2.one) return viewT; // 全屏容器,不是有界区域根
            return parent;
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
            _restorePending = false;
            _retryFramesRemaining = 0;
            RestoreTopVisibility();

            for (int i = 0; i < SnapshotOrder.Count; i++)
            {
                Transform t = SnapshotOrder[i];
                if (t == null || !Snapshots.TryGetValue(t, out ParentSnapshot snapshot)) continue;
                if (t == null || snapshot.Parent == null) continue;

                t.SetParent(snapshot.Parent, false);
                int index = Mathf.Clamp(snapshot.SiblingIndex, 0, snapshot.Parent.childCount - 1);
                t.SetSiblingIndex(index);
            }
            Snapshots.Clear();
            SnapshotOrder.Clear();
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
