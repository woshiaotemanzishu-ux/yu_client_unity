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
    /// 运行时 2D 地图层,移植自 yu_client MapManager(mini_scene_bg 低清底图 + 固定瓦片池滚动复用)。
    ///
    /// 流畅性三件套(对标老客户端,详见 Docs/Shenxiao地图加载重构方案.md):
    /// 1. 滚屏只移动地图自身的独立 Canvas(__SceneMap),不触发根 Canvas/HUD 重建(对标 CameraManager 只挪一个容器)。
    /// 2. 低清底图先铺满,高清瓦片异步在其上补齐——瓦片晚到也只是"略糊",绝不空白/卡住。
    /// 3. 固定瓦片池滚动复用(对标 MapManager.tile_list + UpdateTiles):池大小=视野+边距,
    ///    跨 tile 边界才刷新,移出视野的瓦片直接挪去新格复用,不 new/Destroy、不涨内存、不产生 GC 尖刺。
    /// 我们的元素:单飞加载泵把瓦片加载(及编辑器现导)分摊到多帧,避免一帧涌入几十块造成尖刺。
    /// </summary>
    public static class SceneMapView
    {
        private const float SceneLayerYOffset = 100f;
        /// <summary>地图 Canvas 排序值:压在 HUD/主界面之后(对标老客户端场景层在 UI 之下)。</summary>
        private const int MapSortingOrder = -100;

        private static int _version;
        private static GameObject _root;
        private static Image _preview;
        private static Sprite _previewSprite;
        private static RectTransform _tileRoot;

        private static SceneMapData _data;
        private static int _lastFocusX = int.MinValue;
        private static int _lastFocusY = int.MinValue;

        // —— 固定瓦片池(对标 MapManager.tile_list)——
        private static TileSlot[] _tilePool;
        private static int _poolCols;
        private static int _poolRows;
        private static int _lastStartCol = int.MinValue;
        private static int _lastStartRow = int.MinValue;

        // —— 加载泵(单飞顺序加载,分摊到多帧)——
        private static readonly Queue<TileLoadRequest> _loadQueue = new Queue<TileLoadRequest>();
        private static bool _pumping;

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

            _data = data;
            _lastFocusX = int.MinValue;
            _lastFocusY = int.MinValue;
            EnsureTilePool(data);
            SetFocus(focusX, focusY);
            GameLog.Info("SceneMap", "map preview ready: sceneId={0} mapResId={1} focus=({2},{3}) pool={4}x{5}",
                data.SceneId, data.MapResId, focusX, focusY, _poolCols, _poolRows);
        }

        /// <summary>
        /// 把相机焦点移到指定地图像素坐标(对标老客户端相机跟随主角:主角恒居屏幕中心,地图滚动)。
        /// 焦点未变整帧跳过;滚屏只移动独立地图 Canvas;瓦片仅在跨 tile 边界时才滚动复用(UpdateTiles 内部 early-out)。
        /// </summary>
        public static void SetFocus(int focusX, int focusY)
        {
            if (_root == null || _preview == null || _data == null) return;

            // 焦点(主角格子坐标)没变就整帧跳过(对标 CameraManager.UpdateCamera 的 _camera_pos 未变 early-out)。
            if (focusX == _lastFocusX && focusY == _lastFocusY) return;
            _lastFocusX = focusX;
            _lastFocusY = focusY;

            Vector2 camera = ApplyCamera(_data, focusX, focusY);
            UpdateTiles(camera.x, camera.y);
        }

        public static void Clear()
        {
            ++_version;
            _data = null;
            _lastFocusX = int.MinValue;
            _lastFocusY = int.MinValue;

            if (_previewSprite != null)
            {
                ResManager.Release(_previewSprite);
                _previewSprite = null;
            }

            if (_root != null)
            {
                ClearPool();
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

            // __SceneMap 自带 Canvas:成为独立的重建/合批边界。移动它(滚屏)只改它自己的变换矩阵,
            // 不会触发根 Canvas(HUD/主界面)重建——这是"移动相机不卡"的关键(对标 Laya 只挪场景层容器)。
            _root = new GameObject("__SceneMap", typeof(RectTransform), typeof(Canvas));
            _root.transform.SetParent(sceneLayer, false);

            Canvas mapCanvas = _root.GetComponent<Canvas>();
            mapCanvas.overrideSorting = true;
            mapCanvas.sortingOrder = MapSortingOrder; // 压在 HUD 之下

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

            // 只滚动地图自身的 Canvas(__SceneMap),sceneLayer 保持满屏静止——这样滚屏不触碰根 Canvas,
            // 不会每帧重建 HUD。对标 CameraManager:_scene_layer.x = h_w - _camera_pos.x(只挪一个容器)。
            float layaLayerX = halfWidth - cameraX;
            float layaLayerY = halfHeight + SceneLayerYOffset - cameraY;
            mapRt.anchoredPosition = new Vector2(layaLayerX, -layaLayerY);
            return new Vector2(cameraX, cameraY);
        }

        /// <summary>建立/复用固定瓦片池(对标 MapManager.InitData:池大小=视野+边距,该边距即屏幕外预取缓冲)。</summary>
        private static void EnsureTilePool(SceneMapData data)
        {
            if (_tileRoot == null || data == null || data.TileSize <= 0) return;
            int tileSize = data.TileSize;

            RectTransform sceneLayer = ViewManager.GetLayer(UILayer.Scene) as RectTransform;
            RectTransform canvasRt = sceneLayer != null ? sceneLayer.parent as RectTransform : null;
            float stageWidth = canvasRt != null && canvasRt.rect.width > 0f ? canvasRt.rect.width : Screen.width;
            float stageHeight = canvasRt != null && canvasRt.rect.height > 0f ? canvasRt.rect.height : Screen.height;

            // 对标 MapManager:onPC 取 12 列,否则 floor(w/tile)+2;行 floor(h/tile)+1。+2/+1 即屏幕外预取边距。
            int cols = Application.isEditor ? 12 : Mathf.FloorToInt(stageWidth / tileSize) + 2;
            int rows = Mathf.FloorToInt(stageHeight / tileSize) + 1;
            cols = Mathf.Max(1, cols);
            rows = Mathf.Max(1, rows);

            if (_tilePool != null && _poolCols == cols && _poolRows == rows)
            {
                ResetPool(); // 同尺寸:复用对象,只重置内容触发整屏重刷
                return;
            }

            ClearPool();
            _poolCols = cols;
            _poolRows = rows;
            _tilePool = new TileSlot[cols * rows];
            for (int i = 0; i < _tilePool.Length; i++)
            {
                var go = new GameObject("Tile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_tileRoot, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(tileSize, tileSize);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.enabled = false;
                _tilePool[i] = new TileSlot { Root = go, Image = img, Rt = rt };
            }
            _lastStartCol = int.MinValue;
            _lastStartRow = int.MinValue;
        }

        /// <summary>滚动复用可见瓦片(逐行逐列移植 MapManager.UpdateTiles)。仅在窗口起点格变化时才工作。</summary>
        private static void UpdateTiles(float cameraX, float cameraY)
        {
            if (_tilePool == null || _data == null) return;
            int tileSize = _data.TileSize;
            if (tileSize <= 0) return;

            int x = Mathf.CeilToInt(cameraX / tileSize);
            int y = Mathf.CeilToInt(cameraY / tileSize);
            int startCol = Mathf.CeilToInt(_poolCols * 0.5f) - 1;
            int startRow = Mathf.CeilToInt(_poolRows * 0.5f);
            if (cameraX % tileSize < 0.5f * tileSize) startCol++;
            if (cameraY % tileSize < 0.5f * tileSize) startRow++;
            startCol = x - startCol;
            startRow = y - startRow;

            // 窗口起点格未变 → 整个刷新跳过(对标 MapManager.ts:598)。
            if (startCol == _lastStartCol && startRow == _lastStartRow) return;

            int colLen = startCol + _poolCols - 1;
            int rowLen = startRow + _poolRows - 1;
            int lastColLen = _lastStartCol + _poolCols - 1;
            int lastRowLen = _lastStartRow + _poolRows - 1;
            bool first = _lastStartCol == int.MinValue || _lastStartRow == int.MinValue;

            int cacheIndex = 0;
            for (int i = startRow; i <= rowLen; i++)
            {
                for (int j = startCol; j <= colLen; j++)
                {
                    // 只处理"本次新进入视野"的格(原窗口外的)。
                    bool newlyEntered = first || i < _lastStartRow || i > lastRowLen || j < _lastStartCol || j > lastColLen;
                    if (!newlyEntered) continue;

                    // 找一个"已滚出新窗口"的池瓦片来复用(对标原 cache_index 单向扫描)。
                    for (int index = cacheIndex; index < _tilePool.Length; index++)
                    {
                        TileSlot t = _tilePool[index];
                        cacheIndex++;
                        if (t.Row < startRow || t.Row > rowLen || t.Col < startCol || t.Col > colLen)
                        {
                            AssignSlot(t, i, j);
                            break;
                        }
                    }
                }
            }

            _lastStartCol = startCol;
            _lastStartRow = startRow;
        }

        /// <summary>把一个池瓦片移到目标格并排队加载它的图(越界格保持隐藏,低清底图透出)。</summary>
        private static void AssignSlot(TileSlot slot, int row, int col)
        {
            slot.Row = row;
            slot.Col = col;
            int token = ++slot.Token;

            if (slot.Sprite != null) { ResManager.Release(slot.Sprite); slot.Sprite = null; }
            if (slot.Image != null) { slot.Image.sprite = null; slot.Image.enabled = false; }

            slot.Rt.anchoredPosition = new Vector2((col - 1) * _data.TileSize, -(row - 1) * _data.TileSize);

            int maxCol = Mathf.CeilToInt((float)_data.MapWidth / _data.TileSize);
            int maxRow = Mathf.CeilToInt((float)_data.MapHeight / _data.TileSize);
            if (row < 1 || col < 1 || row > maxRow || col > maxCol) return; // 越界:保持隐藏

            _loadQueue.Enqueue(new TileLoadRequest(slot, row, col, token, _version));
            PumpLoads();
        }

        /// <summary>单飞加载泵:一次只加载一块,把加载/编辑器现导分摊到多帧,避免一帧尖刺。</summary>
        private static async void PumpLoads()
        {
            if (_pumping) return;
            _pumping = true;
            try
            {
                while (_loadQueue.Count > 0)
                {
                    if (_data == null) break;
                    TileLoadRequest req = _loadQueue.Dequeue();
                    if (req.Version != _version || req.Slot.Token != req.Token) continue; // 槽已被复用/换图,丢弃

                    Sprite sprite = await ResManager.LoadAsync<Sprite>(
                        GameResPath.GetSceneMapTile(_data.MapResId, req.Row, req.Col, ".jxr"));

                    if (req.Version != _version || req.Slot.Token != req.Token)
                    {
                        if (sprite != null) ResManager.Release(sprite);
                        continue;
                    }
                    if (sprite == null) continue; // 缺图:保持隐藏,低清底图透出

                    req.Slot.Sprite = sprite;
                    req.Slot.Image.sprite = sprite;
                    req.Slot.Image.enabled = true;
                }
            }
            finally
            {
                _pumping = false;
            }
        }

        private static void ResetPool()
        {
            _loadQueue.Clear();
            if (_tilePool != null)
            {
                foreach (TileSlot t in _tilePool)
                {
                    if (t == null) continue;
                    t.Token++;
                    if (t.Sprite != null) { ResManager.Release(t.Sprite); t.Sprite = null; }
                    if (t.Image != null) { t.Image.sprite = null; t.Image.enabled = false; }
                    t.Row = int.MinValue;
                    t.Col = int.MinValue;
                }
            }
            _lastStartCol = int.MinValue;
            _lastStartRow = int.MinValue;
        }

        private static void ClearPool()
        {
            _loadQueue.Clear();
            if (_tilePool != null)
            {
                foreach (TileSlot t in _tilePool)
                {
                    if (t == null) continue;
                    if (t.Sprite != null) ResManager.Release(t.Sprite);
                    if (t.Root != null) Object.Destroy(t.Root);
                }
                _tilePool = null;
            }
            _poolCols = 0;
            _poolRows = 0;
            _lastStartCol = int.MinValue;
            _lastStartRow = int.MinValue;
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

        private sealed class TileSlot
        {
            public GameObject Root;
            public Image Image;
            public RectTransform Rt;
            public int Row = int.MinValue;
            public int Col = int.MinValue;
            public int Token;     // 每次复用 +1,过期加载据此丢弃
            public Sprite Sprite; // 当前贴图(换图前 Release)
        }

        private readonly struct TileLoadRequest
        {
            public readonly TileSlot Slot;
            public readonly int Row;
            public readonly int Col;
            public readonly int Token;
            public readonly int Version;

            public TileLoadRequest(TileSlot slot, int row, int col, int token, int version)
            {
                Slot = slot;
                Row = row;
                Col = col;
                Token = token;
                Version = version;
            }
        }
    }
}
