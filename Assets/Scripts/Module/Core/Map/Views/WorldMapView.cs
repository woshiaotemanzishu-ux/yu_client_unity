using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Map;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>世界地图只读渲染；场景切换事务由运行态授权后另行接入。</summary>
    public sealed class WorldMapView : WorldMapViewBind
    {
        private readonly List<WorldMapItem> _items = new List<WorldMapItem>();
        private int _refreshVersion;
        private Sprite _backgroundSprite;

        protected override void OnInit()
        {
            if (_tpl_WorldMapItem != null) _tpl_WorldMapItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            _ = RefreshAsync(++_refreshVersion);
        }

        protected override void OnHide()
        {
            ++_refreshVersion;
            if (scroll != null) scroll.StopMovement();
            for (int i = 0; i < _items.Count; i++) _items[i].Hide();
        }

        protected override void OnDispose()
        {
            ++_refreshVersion;
            ReleaseBackground();
        }

        private async Task RefreshAsync(int version)
        {
            await MapConfigs.EnsureLoaded();
            if (version != _refreshVersion || !IsShown) return;

            await LoadBackgroundAsync(version);
            if (version != _refreshVersion || !IsShown) return;

            IReadOnlyList<MapConfigs.WorldEntry> entries = MapConfigs.WorldEntries;
            EnsureItemCount(entries.Count);
            int currentScene = RoleModel.Instance.SceneId;
            Vector2 currentRoot = Vector2.zero;
            for (int i = 0; i < _items.Count; i++)
            {
                bool visible = i < entries.Count;
                WorldMapItem item = _items[i];
                if (!visible)
                {
                    item.Hide();
                    continue;
                }

                MapConfigs.WorldEntry entry = entries[i];
                item.Show();
                item.SetData(entry, entry.SceneId == currentScene, null);
                if (entry.SceneId == currentScene) currentRoot = entry.RootPosition;
            }

            if (scroll != null && scroll.content != null && scroll.viewport != null && currentRoot != Vector2.zero)
            {
                float range = Mathf.Max(1f, scroll.content.rect.width - scroll.viewport.rect.width);
                scroll.horizontalNormalizedPosition = Mathf.Clamp01((currentRoot.x - 300f) / range);
            }
        }

        private void EnsureItemCount(int count)
        {
            if (_tpl_WorldMapItem == null || scene_con == null) return;
            while (_items.Count < count)
            {
                GameObject clone = Instantiate(_tpl_WorldMapItem, scene_con, false);
                clone.name = "WorldMapItem_" + _items.Count;
                WorldMapItem item = clone.GetComponent<WorldMapItem>();
                if (item == null)
                {
                    Destroy(clone);
                    GameLog.Error("Map", "WorldMapItem template missing component");
                    break;
                }
                _items.Add(item);
            }
        }

        private async Task LoadBackgroundAsync(int version)
        {
            if (map_bg == null || _backgroundSprite != null) return;
            Sprite sprite = await ResManager.LoadOptionalAsync<Sprite>(GameResPath.GetIconJpg("map", "big_bg"));
            if (version != _refreshVersion || !IsShown)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }
            if (sprite == null) return;
            _backgroundSprite = sprite;
            map_bg.sprite = sprite;
            map_bg.enabled = true;
        }

        private void ReleaseBackground()
        {
            if (_backgroundSprite == null) return;
            if (map_bg != null && map_bg.sprite == _backgroundSprite) map_bg.sprite = null;
            ResManager.Release(_backgroundSprite);
            _backgroundSprite = null;
        }

        private void OnDestroy()
        {
            ReleaseBackground();
        }
    }
}
