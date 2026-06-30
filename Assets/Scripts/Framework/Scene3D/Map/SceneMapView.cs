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
        // 相机焦点(主角像素)在屏幕上的竖直落点 = 中心下方这么多像素(对标老客户端 CameraManager._scene_layer_y_off=100)。
        // SceneCharacterStage(另一程序集)也要用它把主角"脚底"对齐到同一处,故设为 public 供跨程序集引用
        // (internal 跨不了 asmdef;本工程 Framework / Common 是不同程序集,对标 CameraPos 也是 public)。避免两处各写一个 100。
        public const float SceneLayerYOffset = 100f;
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

        /// <summary>
        /// 最近一次焦点解算后的(夹边后)相机像素坐标。主角合成台据此把模型摆到「逻辑像素 (role.X,role.Y)
        /// 在地图上实际被画出的屏幕位置」上:屏幕偏移 = (role.X - Camera.x, role.Y - Camera.y)。
        /// 地图内部(未夹边)该偏移恒为 0;靠近地图边缘相机夹紧时随之增大,主角滑向屏幕边缘
        /// (对标老客户端 CameraManager.UpdateCamera:角色挂世界层、相机夹边后角色滑向屏幕边)。
        /// </summary>
        public static Vector2 CameraPos { get; private set; }

        // —— 固定瓦片池(对标 MapManager.tile_list)——
        private static TileSlot[] _tilePool;
        private static int _poolCols;
        private static int _poolRows;
        private static int _lastStartCol = int.MinValue;
        private static int _lastStartRow = int.MinValue;

        // —— 加载泵(小并发顺序加载,分摊到多帧)——
        // 老实现是单飞(一次只加载一块),首屏要把近百块串成近百帧 → 几秒才铺满,即使全是本地图。
        // 改成有界并发:同时最多 MaxConcurrentTileLoads 块在飞,首屏铺满时间≈除以并发数;
        // 并发数不取太大,避免编辑器现导/解码在一帧内涌入造成尖刺(对标老客户端"分摊到多帧"的初衷)。
        private const int MaxConcurrentTileLoads = 6;
        private static readonly Queue<TileLoadRequest> _loadQueue = new Queue<TileLoadRequest>();
        private static int _activeTileLoads;
        private static bool _pumpDispatching;

        // 正在加载中的瓦片(键=格,值=发起时地图版本):同一格只允许一个在飞的 LoadAsync。
        // 不去重的话,并发下同一格可能被两个 worker 同时 LoadAsync——Addressables 对同 key 返回同一
        // Sprite 但两个不同 handle,而 ResManager 以 asset 为键记 handle、后者覆盖前者 → 前一个 handle
        // 永远收不回 → 句柄泄漏、纹理整局常驻。落地后按"当前哪个槽要这格"上色,故无需记 slot/token。
        private static readonly Dictionary<long, int> _inFlightTiles = new Dictionary<long, int>();

        // —— 瓦片 sprite 缓存(键=格 (row,col)):滚出视野的瓦片图保留在缓存里,走回原区域直接同步复用,
        //    不再变模糊重新加载(解决"跑出一个屏再回来,地块重新加载"的问题)。LRU 上限封顶内存,
        //    sprite 归缓存所有,Release 只发生在 LRU 淘汰或整图清理时;瓦片槽只引用、不持有。
        //    上限随视野自适应(EnsureTilePool 里按池大小算),编辑器约 200+、真机更小,内存有界。——
        private static readonly Dictionary<long, Sprite> _tileCache = new Dictionary<long, Sprite>();
        private static readonly LinkedList<long> _tileCacheLru = new LinkedList<long>(); // 头=最近用,尾=最久未用
        private static int _tileCacheCap = 192;

        public static async Task ShowAsync(SceneMapData data, int focusX, int focusY)
        {
            if (data == null) return;

            // 同一张图已经在显示(重进/断线重连进同一场景)→ 不重刷瓦片:保留已加载的底图与瓦片,
            // 只更新相机焦点(SetFocus 内部 focus 未变会整帧跳过)。这就是"地图缓存":重连不再整屏变模糊重载。
            // 不 ++_version、不 ResetPool、不重载底图,沿用现有 Addressable sprite 引用(避免 Release→refcount 归零→卸载重载)。
            if (IsSameMapShown(data))
            {
                _data = data; // 换上新解析的数据对象(WalkGrid 等最新),但尺寸一致,瓦片布局不动
                SetFocus(focusX, focusY);
                GameLog.Info("SceneMap", "map already shown, reuse tiles (no reload): sceneId={0} mapResId={1}",
                    data.SceneId, data.MapResId);
                return;
            }

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
            CameraPos = camera;
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

        /// <summary>
        /// 当前是否已在显示同一张图。判等只看**地图资源 id(MapResId)+ 尺寸**,**不看 SceneId**——
        /// 对标老端 MapManager 的 is_same_res_id(只比 .bytes 尾部的 resId,完全不看场景实例 id)。
        /// 关键:主线副本(如大妖 sceneId=10200,dunId=50000)复用野外(sceneId=10000)的同一地图资源(mapResId 同为 10000),
        /// 若把 SceneId 也算进判等,野外→副本因 SceneId 变了会被判"不同图"→整屏重载 1548 瓦片(主线程卡顿,
        /// 实测进副本时把网络 keepalive 饿死触发 force-close)。去掉 SceneId 比较后:同 mapResId+同尺寸 → 命中复用,
        /// 不重载、不卡顿、无黑屏,实现"同地图只换实例名"的无缝切换。
        /// </summary>
        private static bool IsSameMapShown(SceneMapData data)
        {
            return _data != null
                && _root != null && _preview != null && _previewSprite != null && _preview.enabled
                && _tilePool != null
                && _data.MapResId == data.MapResId
                && _data.MapWidth == data.MapWidth
                && _data.MapHeight == data.MapHeight
                && _data.TileSize == data.TileSize;
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

            // 缓存上限 = 视野瓦片数的 ~3 倍(覆盖来回走动的邻近区域),封顶 256 防内存膨胀。
            _tileCacheCap = Mathf.Clamp(cols * rows * 3, 96, 256);

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

        /// <summary>把一个池瓦片移到目标格(越界格保持隐藏);缓存命中即同步贴图,否则排队加载。</summary>
        private static void AssignSlot(TileSlot slot, int row, int col)
        {
            slot.Row = row;
            slot.Col = col;

            slot.Sprite = null; // 不 Release:sprite 归缓存所有,本槽只是换引用
            if (slot.Image != null) { slot.Image.sprite = null; slot.Image.enabled = false; }

            slot.Rt.anchoredPosition = new Vector2((col - 1) * _data.TileSize, -(row - 1) * _data.TileSize);

            int maxCol = Mathf.CeilToInt((float)_data.MapWidth / _data.TileSize);
            int maxRow = Mathf.CeilToInt((float)_data.MapHeight / _data.TileSize);
            if (row < 1 || col < 1 || row > maxRow || col > maxCol) return; // 越界:保持隐藏

            // 缓存命中:走回加载过的区域,直接同步贴图,不变模糊、不重新加载、不入队。
            Sprite cached = TileCacheGet(row, col);
            if (cached != null)
            {
                slot.Sprite = cached;
                slot.Image.sprite = cached;
                slot.Image.enabled = true;
                return;
            }

            // 该格已在加载(本版本):不再重复 LoadAsync,落地后由 PaintSlotsWantingTile 给当前持有该格的槽上色。
            long key = TileKey(row, col);
            if (_inFlightTiles.TryGetValue(key, out int inflightVersion) && inflightVersion == _version) return;

            _inFlightTiles[key] = _version;
            _loadQueue.Enqueue(new TileLoadRequest(row, col, _version));
            PumpLoads();
        }

        /// <summary>
        /// 加载泵的调度器:在并发上限内从队列拉起瓦片加载 worker。同步派发(无 await),
        /// _pumpDispatching 防止 worker 在 finally 里同步回调造成递归重入。
        /// 所有 worker 的续体都在 Unity 主线程上协作执行,故共享状态(队列/缓存/计数)无需加锁。
        /// </summary>
        private static void PumpLoads()
        {
            if (_pumpDispatching) return;
            _pumpDispatching = true;
            try
            {
                while (_activeTileLoads < MaxConcurrentTileLoads && _loadQueue.Count > 0)
                {
                    TileLoadRequest req = _loadQueue.Dequeue();
                    _activeTileLoads++;
                    _ = LoadOneTileAsync(req); // fire-and-forget;finally 里自减并补位
                }
            }
            finally
            {
                _pumpDispatching = false;
            }
        }

        /// <summary>加载单块瓦片(把加载/编辑器现导分摊到多帧)。完成后自减并发计数并补位下一块。</summary>
        private static async Task LoadOneTileAsync(TileLoadRequest req)
        {
            long key = TileKey(req.Row, req.Col);
            try
            {
                if (_data == null) return;
                if (req.Version != _version) return;            // 换图/清图:整批作废

                // 入队到处理之间可能已被缓存(快速来回):命中即同步贴,免一次异步加载。
                Sprite cached = TileCacheGet(req.Row, req.Col);
                if (cached != null)
                {
                    PaintSlotsWantingTile(req.Row, req.Col, cached);
                    return;
                }

                Sprite sprite = await ResManager.LoadAsync<Sprite>(
                    GameResPath.GetSceneMapTile(_data.MapResId, req.Row, req.Col, ".jxr"));

                if (req.Version != _version) // 加载期间换了图:这块属旧图,别污染缓存
                {
                    if (sprite != null) ResManager.Release(sprite);
                    return;
                }
                if (sprite == null) return; // 缺图:保持隐藏,低清底图透出

                Sprite owned = TileCachePut(req.Row, req.Col, sprite); // 进缓存(归缓存所有)
                // 给"当前持有该格"的槽上色:可能不是发起加载时的那个槽(中途滚动复用过)。
                // 与 TileCachePut 之间无 await,LRU 淘汰不会在此夹缝释放本块。
                PaintSlotsWantingTile(req.Row, req.Col, owned);
            }
            finally
            {
                // 仅当 in-flight 标记仍属本版本时清除(换图后该格可能已被新版本重新登记,勿误删)。
                if (_inFlightTiles.TryGetValue(key, out int v) && v == req.Version) _inFlightTiles.Remove(key);
                _activeTileLoads--;
                if (_loadQueue.Count > 0) PumpLoads(); // 还有排队的就补位,保持并发饱和
            }
        }

        /// <summary>把 sprite 贴到当前正持有 (row,col) 这格的池槽(通常恰好一个)。</summary>
        private static void PaintSlotsWantingTile(int row, int col, Sprite sprite)
        {
            if (_tilePool == null || sprite == null) return;
            foreach (TileSlot t in _tilePool)
            {
                if (t == null || t.Row != row || t.Col != col) continue;
                t.Sprite = sprite;
                if (t.Image != null) { t.Image.sprite = sprite; t.Image.enabled = true; }
            }
        }

        // —— 瓦片 sprite 缓存(LRU)——

        private static long TileKey(int row, int col) => ((long)row << 32) | (uint)col;

        /// <summary>缓存命中则标记最近使用并返回 sprite;否则 null。</summary>
        private static Sprite TileCacheGet(int row, int col)
        {
            long key = TileKey(row, col);
            if (_tileCache.TryGetValue(key, out Sprite s) && s != null)
            {
                _tileCacheLru.Remove(key);
                _tileCacheLru.AddFirst(key);
                return s;
            }
            return null;
        }

        /// <summary>把刚加载的 sprite 放进缓存并返回缓存里的实例(重复加载则丢弃新的、复用旧的)。</summary>
        private static Sprite TileCachePut(int row, int col, Sprite sprite)
        {
            long key = TileKey(row, col);
            if (_tileCache.TryGetValue(key, out Sprite existing) && existing != null)
            {
                if (sprite != null && sprite != existing) ResManager.Release(sprite);
                _tileCacheLru.Remove(key);
                _tileCacheLru.AddFirst(key);
                return existing;
            }
            if (sprite == null) return null;
            _tileCache[key] = sprite;
            _tileCacheLru.Remove(key); // 防御:避免极端情况下出现重复 LRU 节点
            _tileCacheLru.AddFirst(key);
            TrimTileCache();
            return sprite;
        }

        /// <summary>超出上限时淘汰最久未用的瓦片(跳过当前仍被可见槽显示的格,避免瞬间变白)。</summary>
        private static void TrimTileCache()
        {
            while (_tileCache.Count > _tileCacheCap && _tileCacheLru.Count > 0)
            {
                LinkedListNode<long> node = _tileCacheLru.Last;
                while (node != null && IsTileVisible(node.Value)) node = node.Previous;
                if (node == null) break; // 全在显示(cap 远大于池,实际不会发生):本次放弃淘汰
                long key = node.Value;
                _tileCacheLru.Remove(node);
                if (_tileCache.TryGetValue(key, out Sprite s))
                {
                    _tileCache.Remove(key);
                    if (s != null) ResManager.Release(s);
                }
            }
        }

        private static bool IsTileVisible(long key)
        {
            if (_tilePool == null) return false;
            foreach (TileSlot t in _tilePool)
            {
                if (t != null && t.Sprite != null && TileKey(t.Row, t.Col) == key) return true;
            }
            return false;
        }

        /// <summary>清空瓦片缓存并释放所有 sprite(换图/整图清理时)。</summary>
        private static void ClearTileCache()
        {
            foreach (Sprite s in _tileCache.Values)
            {
                if (s != null) ResManager.Release(s);
            }
            _tileCache.Clear();
            _tileCacheLru.Clear();
        }

        private static void ResetPool()
        {
            _loadQueue.Clear();
            _inFlightTiles.Clear(); // 旧版本在飞的加载作废(版本号已变,worker 会自行 early-out)
            ClearTileCache(); // 换的是另一张图,旧缓存作废
            if (_tilePool != null)
            {
                foreach (TileSlot t in _tilePool)
                {
                    if (t == null) continue;
                    t.Sprite = null; // 不 Release(已由 ClearTileCache 统一释放)
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
            _inFlightTiles.Clear();
            ClearTileCache(); // sprite 归缓存,统一在此释放
            if (_tilePool != null)
            {
                foreach (TileSlot t in _tilePool)
                {
                    if (t == null) continue;
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
            public Sprite Sprite; // 当前贴图
        }

        private readonly struct TileLoadRequest
        {
            public readonly int Row;
            public readonly int Col;
            public readonly int Version;

            public TileLoadRequest(int row, int col, int version)
            {
                Row = row;
                Col = col;
                Version = version;
            }
        }
    }
}
