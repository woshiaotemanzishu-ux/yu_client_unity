using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Map;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using UnityEngine;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>当前场景区域地图只读渲染；点击寻路/移动事务在获得运行态写授权前保持未接。</summary>
    public sealed class AreaMapView : AreaMapViewBind
    {
        private readonly List<AreaMapPonitItem> _points = new List<AreaMapPonitItem>();
        private readonly List<AreaMapMonItem> _monsters = new List<AreaMapMonItem>();
        private int _refreshVersion;
        private Sprite _mapSprite;

        protected override void OnInit()
        {
            if (_tpl_AreaMapMonItem != null) _tpl_AreaMapMonItem.SetActive(false);
            if (_tpl_AreaMapPonitItem != null) _tpl_AreaMapPonitItem.SetActive(false);
            if (_tpl_AreaMapWayPonitItem != null) _tpl_AreaMapWayPonitItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            _ = RefreshAsync(++_refreshVersion);
        }

        protected override void OnHide()
        {
            ++_refreshVersion;
            if (map_scroll != null) map_scroll.StopMovement();
            if (mon_scroll_group != null) mon_scroll_group.StopMovement();
            for (int i = 0; i < _points.Count; i++) _points[i].Hide();
            for (int i = 0; i < _monsters.Count; i++) _monsters[i].Hide();
        }

        protected override void OnDispose()
        {
            ++_refreshVersion;
            ReleaseMapSprite();
        }

        private async Task RefreshAsync(int version)
        {
            int sceneId = RoleModel.Instance.SceneId;
            await Task.WhenAll(MapConfigs.EnsureLoaded(), MonsterConfigs.EnsureLoaded(), NpcConfigs.EnsureLoaded());
            if (version != _refreshVersion || !IsShown) return;

            await LoadMapSpriteAsync(sceneId, version);
            if (version != _refreshVersion || !IsShown) return;

            Vector2 scale = GetMapScale(sceneId);
            RenderCurrentRole(scale);
            IReadOnlyList<MapConfigs.AreaPoint> entries = MapConfigs.GetAreaPoints(sceneId);
            EnsurePointCount(entries.Count);
            EnsureMonsterCount(entries.Count);
            int renderCount = Mathf.Min(entries.Count, Mathf.Min(_points.Count, _monsters.Count));
            for (int i = 0; i < renderCount; i++)
            {
                MapConfigs.AreaPoint data = entries[i];
                string name = ResolveName(data);

                AreaMapPonitItem point = _points[i];
                point.SetData(name);
                RectTransform pointRoot = point.transform as RectTransform;
                if (pointRoot != null)
                {
                    pointRoot.anchoredPosition = new Vector2(
                        data.X / scale.x - 42f,
                        -(data.Y / scale.y - 63f));
                }
                point.Show();

                AreaMapMonItem monster = _monsters[i];
                monster.SetData(name, data.IsNpc ? string.Empty : "Lv." + data.Level, null);
                monster.Show();
            }
            for (int i = renderCount; i < _points.Count; i++) _points[i].Hide();
            for (int i = renderCount; i < _monsters.Count; i++) _monsters[i].Hide();

            if (mon_scroll_group != null)
            {
                mon_scroll_group.horizontalNormalizedPosition = 0f;
                mon_scroll_group.verticalNormalizedPosition = 1f;
            }
            CenterOnCurrentRole(scale);
        }

        private async Task LoadMapSpriteAsync(int sceneId, int version)
        {
            if (map_img == null || sceneId <= 0) return;
            Sprite sprite = await ResManager.LoadOptionalAsync<Sprite>(GameResPath.GetAreaMapImg(sceneId));
            if (version != _refreshVersion || !IsShown)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }
            if (sprite == null) return;
            ReleaseMapSprite();
            _mapSprite = sprite;
            map_img.sprite = sprite;
            map_img.enabled = true;
        }

        private Vector2 GetMapScale(int sceneId)
        {
            SceneMapData current = SceneMapLoader.Current;
            float imageWidth = map_img != null && map_img.rectTransform.rect.width > 0f ? map_img.rectTransform.rect.width : 1292f;
            float imageHeight = map_img != null && map_img.rectTransform.rect.height > 0f ? map_img.rectTransform.rect.height : 1138f;
            if (current == null || current.SceneId != sceneId || current.MapWidth <= 0 || current.MapHeight <= 0)
                return Vector2.one;
            return new Vector2(current.MapWidth / imageWidth, current.MapHeight / imageHeight);
        }

        private void RenderCurrentRole(Vector2 scale)
        {
            if (role_name != null) role_name.text = RoleModel.Instance.Name;
            if (main_role_point == null) return;
            main_role_point.rectTransform.anchoredPosition = new Vector2(
                RoleModel.Instance.X / Mathf.Max(0.0001f, scale.x),
                -RoleModel.Instance.Y / Mathf.Max(0.0001f, scale.y));
        }

        private void CenterOnCurrentRole(Vector2 scale)
        {
            if (map_scroll == null || map_scroll.content == null || map_scroll.viewport == null) return;
            float x = RoleModel.Instance.X / Mathf.Max(0.0001f, scale.x);
            float y = RoleModel.Instance.Y / Mathf.Max(0.0001f, scale.y);
            float horizontalRange = Mathf.Max(1f, map_scroll.content.rect.width - map_scroll.viewport.rect.width);
            float verticalRange = Mathf.Max(1f, map_scroll.content.rect.height - map_scroll.viewport.rect.height);
            map_scroll.horizontalNormalizedPosition = Mathf.Clamp01((x - map_scroll.viewport.rect.width * 0.5f) / horizontalRange);
            map_scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01((y - map_scroll.viewport.rect.height * 0.5f) / verticalRange);
        }

        private void EnsurePointCount(int count)
        {
            if (_tpl_AreaMapPonitItem == null || item_con == null) return;
            while (_points.Count < count)
            {
                GameObject clone = Instantiate(_tpl_AreaMapPonitItem, item_con, false);
                clone.name = "AreaMapPoint_" + _points.Count;
                AreaMapPonitItem item = clone.GetComponent<AreaMapPonitItem>();
                if (item == null)
                {
                    Destroy(clone);
                    GameLog.Error("Map", "AreaMapPonitItem template missing component");
                    break;
                }
                _points.Add(item);
            }
        }

        private void EnsureMonsterCount(int count)
        {
            RectTransform content = mon_scroll_group != null ? mon_scroll_group.content : null;
            if (_tpl_AreaMapMonItem == null || content == null) return;
            while (_monsters.Count < count)
            {
                GameObject clone = Instantiate(_tpl_AreaMapMonItem, content, false);
                clone.name = "AreaMapMonster_" + _monsters.Count;
                AreaMapMonItem item = clone.GetComponent<AreaMapMonItem>();
                if (item == null)
                {
                    Destroy(clone);
                    GameLog.Error("Map", "AreaMapMonItem template missing component");
                    break;
                }
                RectTransform rt = clone.transform as RectTransform;
                if (rt != null) rt.anchoredPosition = new Vector2(_monsters.Count * 142f, 0f);
                _monsters.Add(item);
            }
            content.sizeDelta = new Vector2(Mathf.Max(content.sizeDelta.x, count * 142f), content.sizeDelta.y);
        }

        private static string ResolveName(MapConfigs.AreaPoint point)
        {
            if (point.IsNpc)
            {
                NpcConfigs.NpcCfg npc = NpcConfigs.Get(point.MonsterId);
                return npc?.Name ?? string.Empty;
            }
            MonsterConfigs.MonCfg monster = MonsterConfigs.Get(point.MonsterId);
            return monster?.Name ?? string.Empty;
        }

        private void ReleaseMapSprite()
        {
            if (_mapSprite == null) return;
            if (map_img != null && map_img.sprite == _mapSprite) map_img.sprite = null;
            ResManager.Release(_mapSprite);
            _mapSprite = null;
        }

        private void OnDestroy()
        {
            ReleaseMapSprite();
        }
    }
}
