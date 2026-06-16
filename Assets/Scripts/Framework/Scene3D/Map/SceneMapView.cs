using System.Threading.Tasks;
using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>
    /// Runtime 2D map layer that mirrors yu_client MapManager's mini_scene_bg path.
    /// Tile pooling and high quality tile replacement are separate follow-up work.
    /// </summary>
    public static class SceneMapView
    {
        private const float SceneLayerYOffset = 100f;

        private static int _version;
        private static GameObject _root;
        private static Image _preview;
        private static Sprite _previewSprite;
        private static RectTransform _tileRoot;
        private static readonly Dictionary<string, TileEntry> _tiles = new Dictionary<string, TileEntry>();

        public static async Task ShowAsync(SceneMapData data, int focusX, int focusY)
        {
            if (data == null) return;

            int version = ++_version;
            EnsureRoot();
            if (_root == null || _preview == null) return;

            Sprite sprite = await ResManager.LoadAsync<Sprite>(GameResPath.GetSceneMapPreview(data.MapResId));
            if (version != _version)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }

            if (sprite == null)
            {
                GameLog.Error("SceneMap", "map preview load failed: sceneId={0} mapResId={1}", data.SceneId, data.MapResId);
                return;
            }

            if (_previewSprite != null && _previewSprite != sprite)
            {
                ResManager.Release(_previewSprite);
            }

            _previewSprite = sprite;
            _preview.sprite = sprite;
            _preview.enabled = true;
            _preview.SetNativeSize();

            RectTransform mapRt = _root.GetComponent<RectTransform>();
            mapRt.sizeDelta = new Vector2(data.MapWidth, data.MapHeight);

            RectTransform imageRt = _preview.rectTransform;
            imageRt.sizeDelta = new Vector2(data.MapWidth, data.MapHeight);

            Vector2 camera = ApplyCamera(data, focusX, focusY);
            _ = RefreshVisibleTilesAsync(data, camera.x, camera.y, version);
            GameLog.Info("SceneMap", "map preview ready: sceneId={0} mapResId={1} focus=({2},{3})",
                data.SceneId, data.MapResId, focusX, focusY);
        }

        public static void Clear()
        {
            ++_version;
            if (_previewSprite != null)
            {
                ResManager.Release(_previewSprite);
                _previewSprite = null;
            }

            if (_root != null)
            {
                ClearTiles();
                Object.Destroy(_root);
                _root = null;
                _preview = null;
                _tileRoot = null;
            }

            ResetSceneLayer();
        }

        private static void EnsureRoot()
        {
            if (_root != null) return;

            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null)
            {
                GameLog.Warn("SceneMap", "skip map preview: UILayer.Scene is not ready");
                return;
            }

            _root = new GameObject("__SceneMap", typeof(RectTransform));
            _root.transform.SetParent(sceneLayer, false);

            RectTransform rootRt = (RectTransform)_root.transform;
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = Vector2.zero;

            GameObject previewGo = new GameObject("MiniSceneBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewGo.transform.SetParent(_root.transform, false);

            RectTransform imageRt = (RectTransform)previewGo.transform;
            imageRt.anchorMin = new Vector2(0f, 1f);
            imageRt.anchorMax = new Vector2(0f, 1f);
            imageRt.pivot = new Vector2(0f, 1f);
            imageRt.anchoredPosition = Vector2.zero;

            _preview = previewGo.GetComponent<Image>();
            _preview.raycastTarget = false;
            _preview.enabled = false;

            GameObject tileRootGo = new GameObject("Tiles", typeof(RectTransform));
            tileRootGo.transform.SetParent(_root.transform, false);
            _tileRoot = (RectTransform)tileRootGo.transform;
            _tileRoot.anchorMin = new Vector2(0f, 1f);
            _tileRoot.anchorMax = new Vector2(0f, 1f);
            _tileRoot.pivot = new Vector2(0f, 1f);
            _tileRoot.anchoredPosition = Vector2.zero;
        }

        private static Vector2 ApplyCamera(SceneMapData data, int focusX, int focusY)
        {
            RectTransform sceneLayer = ViewManager.GetLayer(UILayer.Scene) as RectTransform;
            RectTransform canvasRt = sceneLayer != null ? sceneLayer.parent as RectTransform : null;
            RectTransform mapRt = _root.GetComponent<RectTransform>();
            if (sceneLayer == null || canvasRt == null || mapRt == null) return new Vector2(focusX, focusY);

            float stageWidth = canvasRt.rect.width;
            float stageHeight = canvasRt.rect.height;
            if (stageWidth <= 0f || stageHeight <= 0f)
            {
                stageWidth = Screen.width;
                stageHeight = Screen.height;
            }

            float halfWidth = stageWidth * 0.5f;
            float halfHeight = stageHeight * 0.5f;
            float cameraX = ClampCameraX(data.MapWidth, focusX, halfWidth);
            float cameraY = ClampCameraY(data.MapHeight, focusY, halfHeight);
            float layaLayerX = halfWidth - cameraX;
            float layaLayerY = halfHeight + SceneLayerYOffset - cameraY;

            sceneLayer.anchorMin = new Vector2(0f, 1f);
            sceneLayer.anchorMax = new Vector2(0f, 1f);
            sceneLayer.pivot = new Vector2(0f, 1f);
            sceneLayer.sizeDelta = new Vector2(data.MapWidth, data.MapHeight);
            sceneLayer.anchoredPosition = new Vector2(layaLayerX, -layaLayerY);

            mapRt.anchoredPosition = Vector2.zero;
            return new Vector2(cameraX, cameraY);
        }

        private static async Task RefreshVisibleTilesAsync(SceneMapData data, float cameraX, float cameraY, int version)
        {
            if (_tileRoot == null || data == null || data.TileSize <= 0) return;

            RectTransform sceneLayer = ViewManager.GetLayer(UILayer.Scene) as RectTransform;
            RectTransform canvasRt = sceneLayer != null ? sceneLayer.parent as RectTransform : null;
            float stageWidth = canvasRt != null && canvasRt.rect.width > 0f ? canvasRt.rect.width : Screen.width;
            float stageHeight = canvasRt != null && canvasRt.rect.height > 0f ? canvasRt.rect.height : Screen.height;

            int countX = Application.isEditor ? 12 : Mathf.FloorToInt(stageWidth / data.TileSize) + 2;
            int countY = Mathf.FloorToInt(stageHeight / data.TileSize) + 1;
            countX = Mathf.Max(1, countX);
            countY = Mathf.Max(1, countY);

            int centerCol = Mathf.CeilToInt(cameraX / data.TileSize);
            int centerRow = Mathf.CeilToInt(cameraY / data.TileSize);
            int startColOffset = Mathf.CeilToInt(countX * 0.5f) - 1;
            int startRowOffset = Mathf.CeilToInt(countY * 0.5f);
            if (cameraX % data.TileSize < 0.5f * data.TileSize) startColOffset++;
            if (cameraY % data.TileSize < 0.5f * data.TileSize) startRowOffset++;

            int startCol = centerCol - startColOffset;
            int startRow = centerRow - startRowOffset;
            int maxCol = Mathf.CeilToInt((float)data.MapWidth / data.TileSize);
            int maxRow = Mathf.CeilToInt((float)data.MapHeight / data.TileSize);
            int requested = 0;

            for (int row = startRow; row < startRow + countY; row++)
            {
                for (int col = startCol; col < startCol + countX; col++)
                {
                    if (row <= 0 || col <= 0 || row > maxRow || col > maxCol) continue;
                    requested++;
                    await LoadTileAsync(data, row, col, version);
                    if (version != _version) return;
                }
            }

            GameLog.Info("SceneMap", "map visible tiles requested: sceneId={0} mapResId={1} count={2}",
                data.SceneId, data.MapResId, requested);
        }

        private static async Task LoadTileAsync(SceneMapData data, int row, int col, int version)
        {
            string tileKey = row.ToString("00") + col.ToString("00");
            if (_tiles.ContainsKey(tileKey)) return;

            GameObject tileGo = new GameObject(tileKey, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tileGo.transform.SetParent(_tileRoot, false);
            RectTransform rt = (RectTransform)tileGo.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2((col - 1) * data.TileSize, -(row - 1) * data.TileSize);
            rt.sizeDelta = new Vector2(data.TileSize, data.TileSize);

            Image image = tileGo.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;

            TileEntry entry = new TileEntry(tileGo, image);
            _tiles.Add(tileKey, entry);

            Sprite sprite = await ResManager.LoadAsync<Sprite>(GameResPath.GetSceneMapTile(data.MapResId, row, col, ".jxr"));
            if (version != _version)
            {
                if (sprite != null) ResManager.Release(sprite);
                return;
            }

            if (sprite == null)
            {
                GameLog.Warn("SceneMap", "map tile load failed: mapResId={0} row={1} col={2}", data.MapResId, row, col);
                return;
            }

            entry.Sprite = sprite;
            image.sprite = sprite;
            image.enabled = true;
        }

        private static void ClearTiles()
        {
            foreach (TileEntry entry in _tiles.Values)
            {
                if (entry.Sprite != null) ResManager.Release(entry.Sprite);
                if (entry.Root != null) Object.Destroy(entry.Root);
            }
            _tiles.Clear();
        }

        private static float ClampCameraX(int mapWidth, int focusX, float halfWidth)
        {
            if (mapWidth <= halfWidth * 2f) return halfWidth;
            return Mathf.Clamp(focusX, halfWidth, mapWidth - halfWidth);
        }

        private static float ClampCameraY(int mapHeight, int focusY, float halfHeight)
        {
            float cameraCenterY = halfHeight + SceneLayerYOffset;
            if (mapHeight <= halfHeight * 2f) return cameraCenterY;
            return Mathf.Clamp(focusY, cameraCenterY, mapHeight - (halfHeight - SceneLayerYOffset));
        }

        private static void ResetSceneLayer()
        {
            RectTransform sceneLayer = ViewManager.GetLayer(UILayer.Scene) as RectTransform;
            if (sceneLayer == null) return;

            sceneLayer.anchorMin = Vector2.zero;
            sceneLayer.anchorMax = Vector2.one;
            sceneLayer.pivot = new Vector2(0.5f, 0.5f);
            sceneLayer.offsetMin = Vector2.zero;
            sceneLayer.offsetMax = Vector2.zero;
            sceneLayer.localScale = Vector3.one;
        }

        private sealed class TileEntry
        {
            public readonly GameObject Root;
            public readonly Image Image;
            public Sprite Sprite;

            public TileEntry(GameObject root, Image image)
            {
                Root = root;
                Image = image;
            }
        }
    }
}
