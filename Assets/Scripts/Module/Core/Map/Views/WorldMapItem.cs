using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>世界地图城市共享条目；只渲染配置与当前场景，写事务通过回调显式注入。</summary>
    public sealed class WorldMapItem : WorldMapItemBind
    {
        private Action _onClick;
        private Sprite _citySprite;
        private int _loadVersion;
        private bool _isCurrent;
        private float _markerBaseY;
        private float _markerStartTime;

        protected override void OnInit()
        {
            if (city_img != null)
            {
                city_img.raycastTarget = true;
                UIUtil.AddClick(city_img, () => _onClick?.Invoke());
            }
        }

        internal void SetData(MapConfigs.WorldEntry data, bool isCurrent, Action onClick)
        {
            if (data == null) return;
            _onClick = onClick;
            _isCurrent = isCurrent;
            RectTransform root = transform as RectTransform;
            if (root != null) root.anchoredPosition = new Vector2(data.RootPosition.x, -data.RootPosition.y);

            if (label_con != null) label_con.gameObject.SetActive(data.Open);
            if (city_name != null) city_name.text = data.Open ? data.Name : string.Empty;
            if (city_lv != null) city_lv.text = data.Open ? FormatLevelRange(data.MinLevel, data.MaxLevel) : string.Empty;
            if (location != null)
            {
                location.gameObject.SetActive(isCurrent);
                location.anchoredPosition = new Vector2(data.LocatePosition.x, -data.LocatePosition.y);
                _markerBaseY = location.anchoredPosition.y;
                _markerStartTime = Time.unscaledTime;
            }
            _ = LoadCitySpriteAsync(data.Image, ++_loadVersion);
        }

        protected override void OnHide()
        {
            ++_loadVersion;
            _onClick = null;
            _isCurrent = false;
        }

        protected override void OnDispose()
        {
            ++_loadVersion;
            ReleaseCitySprite();
        }

        private void Update()
        {
            if (!_isCurrent || location == null || !location.gameObject.activeInHierarchy) return;
            float offset = Mathf.PingPong((Time.unscaledTime - _markerStartTime) * 60f, 30f);
            Vector2 position = location.anchoredPosition;
            position.y = _markerBaseY + offset;
            location.anchoredPosition = position;
        }

        private async Task LoadCitySpriteAsync(string image, int version)
        {
            if (string.IsNullOrEmpty(image) || city_img == null) return;
            string key = GameResPath.GetFilePath("map/world_map_img", image);
            Sprite sprite = await ResManager.LoadOptionalAsync<Sprite>(key);
            if (version != _loadVersion || !IsShown)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }
            if (sprite == null) return;
            ReleaseCitySprite();
            _citySprite = sprite;
            city_img.sprite = sprite;
            city_img.enabled = true;
        }

        private void ReleaseCitySprite()
        {
            if (_citySprite == null) return;
            if (city_img != null && city_img.sprite == _citySprite) city_img.sprite = null;
            ResManager.Release(_citySprite);
            _citySprite = null;
        }

        private void OnDestroy()
        {
            ReleaseCitySprite();
        }

        private static string FormatLevelRange(int min, int max) => FormatMin(min) + " - " + FormatMax(max);

        private static string FormatMin(int value) => value > 370 && value < 999 ? "神创" + (value - 370) : "Lv." + value;

        private static string FormatMax(int value) => value > 370 && value < 999 ? "神创" + (value - 370) : value.ToString();
    }
}
