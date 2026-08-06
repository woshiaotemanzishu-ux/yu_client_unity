using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.UiComponent;
using UnityEngine;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>
    /// 场景称号 NameBoard 消费者。初始值来自主角/其他玩家 Figure.dsgt_id，增量来自 41105；
    /// 屏幕位置与地图/角色使用同一逻辑像素口径，CanvasScaler 会同时适配移动端和宽屏 Web。
    /// </summary>
    public static class SceneDesignationPresenter
    {
        private const long MainKey = long.MinValue;
        private const float AttachHeight = 180f;

        private sealed class Entry
        {
            public long Key;
            public bool IsMain;
            public RoleVo Role;
            public FigureProto Figure;
            public uint Id;
            public RectTransform Root;
            public NameBoard Board;
        }

        private sealed class Driver : MonoBehaviour
        {
            private void LateUpdate() => UpdatePositions();
        }

        private static readonly Dictionary<long, Entry> Entries = new Dictionary<long, Entry>();
        private static RectTransform _root;
        private static GameObject _driver;
        private static bool _installed;

        public static int VisibleCount
        {
            get
            {
                int count = 0;
                foreach (Entry entry in Entries.Values)
                    if (entry?.Root != null && entry.Root.gameObject.activeInHierarchy) count++;
                return count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
            => EnsureInstalled();

        /// <summary>幂等安装。41105 也会兜底调用，确保编辑器验收和运行时热重载不漏订阅。</summary>
        public static void EnsureInstalled()
        {
            if (_installed) return;
            _installed = true;
            SceneManager manager = SceneManager.Instance;
            manager.RoleAdded += OnRoleAdded;
            manager.RoleRemoved += OnRoleRemoved;
            manager.RoleDesignationChanged += OnRoleDesignationChanged;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_READY, RefreshMain);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, SyncScene);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, ClearAll);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        public static void ApplySceneNotice(ulong playerId, uint designationId)
        {
            long id = unchecked((long)playerId);
            RoleModel main = RoleModel.Instance;
            if (main.RoleId == id)
            {
                if (main.Figure != null) main.Figure.SetDesignationId(designationId);
                RefreshMain();
                return;
            }

            RoleVo role = SceneManager.Instance.GetRole(id);
            if (role?.Figure == null)
            {
                Remove(id);
                return;
            }
            role.Figure.SetDesignationId(designationId);
            RefreshRole(role);
        }

        private static void SyncScene()
        {
            RefreshMain();
            foreach (RoleVo role in SceneManager.Instance.AllRoles) RefreshRole(role);
        }

        private static void OnRoleAdded(RoleVo role) => RefreshRole(role);
        private static void OnRoleRemoved(long roleId) => Remove(roleId);
        private static void OnRoleDesignationChanged(RoleVo role) => RefreshRole(role);

        private static void RefreshMain()
        {
            RoleModel role = RoleModel.Instance;
            // 清理旧版本或热重载期间曾按普通 RoleVo 建出的主角副本。
            if (role.RoleId != 0) Remove(role.RoleId);
            Refresh(MainKey, true, null, role.Figure, role.Figure?.DesignationId ?? 0u);
        }

        private static void RefreshRole(RoleVo role)
        {
            if (role == null) return;
            long mainRoleId = RoleModel.Instance.RoleId;
            if (mainRoleId != 0 && role.RoleId == mainRoleId)
            {
                Remove(role.RoleId);
                return;
            }
            Refresh(role.RoleId, false, role, role.Figure, role.Figure?.DesignationId ?? 0u);
        }

        private static async void Refresh(long key, bool isMain, RoleVo role,
            FigureProto figure, uint designationId)
        {
            if (figure == null || designationId == 0)
            {
                Remove(key);
                return;
            }
            EnsureRoot();
            if (_root == null) return;

            if (!Entries.TryGetValue(key, out Entry entry))
            {
                var go = new GameObject(isMain ? "MainRoleDesignation" : "RoleDesignation_" + key,
                    typeof(RectTransform));
                go.transform.SetParent(_root, false);
                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(237f, 150f);
                NameBoard board = go.AddComponent<NameBoard>();
                board._gp_parent = rect;
                board.Show();
                entry = new Entry { Key = key, IsMain = isMain, Root = rect, Board = board };
                Entries[key] = entry;
            }
            entry.Role = role;
            entry.Figure = figure;
            entry.Id = designationId;
            UpdatePosition(entry);

            bool ready = await entry.Board.SetDesignationAsync(designationId, figure);
            if (!Entries.TryGetValue(key, out Entry current) || !ReferenceEquals(current, entry)) return;
            entry.Root.gameObject.SetActive(ready && ShouldShow(entry));
        }

        private static void UpdatePositions()
        {
            foreach (Entry entry in Entries.Values)
            {
                if (entry?.Root == null) continue;
                bool visible = entry.Board != null && entry.Board.DesignationId == entry.Id && ShouldShow(entry);
                if (entry.Root.gameObject.activeSelf != visible) entry.Root.gameObject.SetActive(visible);
                if (visible) UpdatePosition(entry);
            }
        }

        private static bool ShouldShow(Entry entry)
        {
            if (entry == null || entry.Id == 0 || entry.Figure == null) return false;
            RoleModel main = RoleModel.Instance;
            if (main.SceneId == 5502) return false;
            if (main.DunId >= 36001 && main.DunId < 37000) return false;
            return entry.Figure.MaskId == 0;
        }

        private static void UpdatePosition(Entry entry)
        {
            int x;
            int y;
            if (entry.IsMain)
            {
                x = RoleModel.Instance.X;
                y = RoleModel.Instance.Y;
            }
            else
            {
                if (entry.Role == null) return;
                x = entry.Role.X;
                y = entry.Role.Y;
            }

            Vector2 camera = SceneMapView.CameraPos;
            entry.Root.anchoredPosition = new Vector2(
                x - camera.x,
                -(y - camera.y) - SceneMapView.SceneLayerYOffset + AttachHeight);
        }

        private static void EnsureRoot()
        {
            if (_root != null) return;
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null) return;

            var go = new GameObject("__SceneDesignationNameBoards", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(sceneLayer, false);
            _root = (RectTransform)go.transform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = -39;

            if (_driver == null)
            {
                _driver = new GameObject("__SceneDesignationDriver");
                if (Application.isPlaying) Object.DontDestroyOnLoad(_driver);
                _driver.AddComponent<Driver>();
            }
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            ClearAll();
        }

        public static void ClearAll()
        {
            foreach (Entry entry in Entries.Values)
            {
                entry?.Board?.ClearDesignation();
                if (entry?.Root != null)
                {
                    entry.Root.gameObject.SetActive(false);
                    Object.Destroy(entry.Root.gameObject);
                }
            }
            Entries.Clear();
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
                Object.Destroy(_root.gameObject);
            }
            _root = null;
        }

        private static void Remove(long key)
        {
            if (!Entries.TryGetValue(key, out Entry entry)) return;
            Entries.Remove(key);
            entry.Board?.ClearDesignation();
            if (entry.Root != null)
            {
                entry.Root.gameObject.SetActive(false);
                Object.Destroy(entry.Root.gameObject);
            }
        }
    }
}
