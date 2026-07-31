using System.Collections.Generic;
using System.Linq;
using Shenxiao.Editor.LayaUI;
using Shenxiao.EditorTools.AddrSetup;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.MapTools
{
    /// <summary>
    /// 地图资源管理(列表 + 边看边转)。左:所有场景地图清单 + 转换状态;右:选中地图详情 + 转换/定位/删除。
    /// 解决"没去过的新区域第一次进很卡":那是运行时逐块现导瓦片(编辑器兜底,同步)。这里提前一次性
    /// 把整张图的瓦片转进 Assets/GameRes 并分组,运行时改走 Addressables 异步 → 不再现导,不卡。
    /// 风格对标「资产管理」,但地图是瓦片文件夹(无 .lh/prefab),单独成窗不影响模型/特效线。
    /// </summary>
    public sealed class MapAssetWindow : EditorWindow
    {
        private readonly List<int> _sceneIds = new List<int>();
        private readonly Dictionary<int, MapStat> _stats = new Dictionary<int, MapStat>();
        private string _scanError;
        private string _search = "";
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private int _selected = -1;

        // 标注点(选中场景)+ 图层开关
        private SceneEntities _entities;
        private bool _lyMon = true, _lyCollect = true, _lyNpc = true, _lyDoor = true, _lyBoss = true, _lyReborn = true;
        private bool _entityListFoldout;

        // 编辑模式
        private bool _editMode;
        private bool _dirty;
        private List<MapEntity> _selList; // 选中实体所在列表(null + _selBirth=true 表示出生点)
        private int _selIdx = -1;
        private bool _selBirth;
        private bool _dragging;
        private int _birthX, _birthY;

        // 画布缩放/平移(组内局部坐标)
        private float _zoom = 1f;
        private Vector2 _pan;
        private bool _panning;
        private float _mapScale;
        private Vector2 _mapOrigin;
        private Vector2 _localSize;

        private static readonly Color C_MON = new Color(0.91f, 0.30f, 0.24f);
        private static readonly Color C_COLLECT = new Color(0.10f, 0.74f, 0.61f);
        private static readonly Color C_NPC = new Color(0.18f, 0.80f, 0.44f);
        private static readonly Color C_DOOR = new Color(0.20f, 0.60f, 0.86f);
        private static readonly Color C_BOSS = new Color(0.61f, 0.35f, 0.71f);
        private static readonly Color C_REBORN = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color C_BIRTH = new Color(1f, 0.85f, 0.10f);

        private static GUIStyle _rowStyle;
        private static GUIStyle RowStyle => _rowStyle ??= new GUIStyle(EditorStyles.label)
        { richText = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 4, 0, 0) };

        [MenuItem("神霄/资源/地图资源", priority = 23)]
        public static void Open()
        {
            var w = GetWindow<MapAssetWindow>("地图资源");
            w.minSize = new Vector2(720f, 460f);
        }

        private void OnEnable() => Rescan();

        private void Rescan()
        {
            _scanError = null;
            _sceneIds.Clear();
            _stats.Clear();
            _selected = -1;
            _entities = null;
            ClearSelection();
            _dirty = false;
            MapTileConverter.ReloadSceneMeta();
            MapServerData.Reload();
            try { _sceneIds.AddRange(MapTileConverter.ScanScenes()); }
            catch (System.Exception e) { _scanError = e.Message; }
        }

        private MapStat StatOf(int sceneId)
        {
            if (!_stats.TryGetValue(sceneId, out MapStat s))
            {
                s = MapTileConverter.Inspect(sceneId);
                _stats[sceneId] = s;
            }
            return s;
        }

        private void LoadEntities(int sceneId)
        {
            _entities = MapServerData.Available ? MapServerData.GetEntities(sceneId) : null;
            _birthX = _entities != null ? _entities.BirthX : 0;
            _birthY = _entities != null ? _entities.BirthY : 0;
            if (_birthX == 0 && _birthY == 0 && _stats.TryGetValue(sceneId, out MapStat st))
            {
                _birthX = st.BirthX;
                _birthY = st.BirthY;
            }
            ClearSelection();
            _dirty = false;
            _zoom = 1f;
            _pan = Vector2.zero;
        }

        private void ClearSelection()
        {
            _selList = null;
            _selIdx = -1;
            _selBirth = false;
            _dragging = false;
        }

        private static bool ConfirmDiscard() =>
            EditorUtility.DisplayDialog("未保存的修改", "当前地图标注有未保存的修改,切换会丢弃。继续?", "丢弃", "取消");

        private void OnGUI()
        {
            if (!LayaUISettings.ValidateClientRoot(out string err))
            {
                EditorGUILayout.HelpBox(err + "\n在「资产管理」窗口左下角「改路径...」设置 yu_client 仓库根。", MessageType.Error);
                return;
            }
            if (_scanError != null)
            {
                EditorGUILayout.HelpBox("扫描地图失败:\n" + _scanError, MessageType.Error);
                if (GUILayout.Button("重试")) Rescan();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawListPane();
                DrawDetailPane();
            }
        }

        private void DrawListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(300f)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"场景地图 {_sceneIds.Count}", EditorStyles.boldLabel);
                    if (GUILayout.Button("重新扫描", GUILayout.Width(80f))) Rescan();
                }
                EditorGUILayout.LabelField("○未转  ◐部分  ●已转  ·未盘点", EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("盘点全部状态(慢)", GUILayout.Height(20f))) InspectAll();
                    if (GUILayout.Button("一键转换全部", GUILayout.Height(20f))) ConvertAll();
                }

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                string q = (_search ?? "").Trim();
                IEnumerable<int> shown = string.IsNullOrEmpty(q)
                    ? _sceneIds
                    : _sceneIds.Where(id => id.ToString().Contains(q) || MapTileConverter.SceneName(id).Contains(q));

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                foreach (int id in shown)
                {
                    string icon = StatusIcon(id);
                    string name = MapTileConverter.SceneName(id);
                    string extra = "";
                    if (_stats.TryGetValue(id, out MapStat st))
                    {
                        if (st.MapResId != id) extra += $"  <color=#888888>→{st.MapResId}</color>";
                        if (st.TileTotal > 0) extra += $"  <color=#888888>{st.TileConverted}/{st.TileTotal}</color>";
                    }
                    string nameTag = string.IsNullOrEmpty(name) ? "" : $"  {name}";
                    string label = $"{icon} {id}{nameTag}{extra}";
                    Rect row = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
                    if (_selected == id) EditorGUI.DrawRect(row, new Color(0.24f, 0.49f, 0.91f, 0.35f));
                    if (GUI.Button(row, label, RowStyle) && _selected != id)
                    {
                        if (_dirty && !ConfirmDiscard()) continue;
                        _selected = id;
                        StatOf(id); // 选中即盘点该图
                        LoadEntities(id);
                        GUI.FocusControl(null);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawDetailPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selected < 0)
                {
                    EditorGUILayout.HelpBox(
                        "左边选一张地图。\n\n出生点地图卡顿:选中你出生的场景 id(看运行日志 [Scene] 12005 ok: sceneId=…),\n点「转换并分组」,把它的瓦片一次性转好,之后进图任何位置都不再现导、不卡。",
                        MessageType.Info);
                    return;
                }

                MapStat s = StatOf(_selected);
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                string title = string.IsNullOrEmpty(s.Name) ? $"场景 {s.SceneId}" : $"{s.Name}  ({s.SceneId})";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("类型", MapTileConverter.SceneTypeLabel(s.SceneType));
                EditorGUILayout.LabelField("底图资源 mapResId", s.MapResId + (s.MapResId != s.SceneId ? "(复用,与场景id不同)" : ""));
                if (s.MapWidth > 0)
                    EditorGUILayout.LabelField("尺寸", $"{s.MapWidth} x {s.MapHeight}");
                if (s.HasBytes)
                    EditorGUILayout.LabelField("瓦片大小", s.TileSize + " px");
                else
                    EditorGUILayout.HelpBox("没有该场景的 .bytes(可能不是独立场景,或源缺失)。", MessageType.Warning);
                EditorGUILayout.LabelField("出生点", $"({s.BirthX}, {s.BirthY})");

                DrawEditToolbar();
                DrawThumbnail(s);
                if (_editMode) DrawEditPanel();

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("转换状态", EditorStyles.boldLabel);
                string tileGlyph = s.FullyConverted ? "●" : s.PartiallyConverted ? "◐" : "○";
                EditorGUILayout.LabelField("瓦片", $"{tileGlyph} {s.TileConverted}/{s.TileTotal} 已转进 Assets/GameRes");
                EditorGUILayout.LabelField("底图", s.PreviewConverted ? "● 已转" : "○ 未转");
                if (s.PartiallyConverted)
                    EditorGUILayout.HelpBox("只转了一部分:没转到的区域第一次进去仍会现导卡顿。点下面「转换」补齐。", MessageType.Warning);
                else if (s.FullyConverted)
                    EditorGUILayout.HelpBox("瓦片已全部转好。若仍卡,确认已「Addressable 自动分组」(运行时才走异步,不走编辑器兜底)。", MessageType.Info);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
                bool autoGroup = LayaUISettings.AutoGroupAfterConvert;
                bool newAutoGroup = EditorGUILayout.ToggleLeft("转换/更新后顺便 Addressable 自动分组(推荐)", autoGroup);
                if (newAutoGroup != autoGroup) LayaUISettings.AutoGroupAfterConvert = newAutoGroup;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(s.TileConverted > 0 ? "转换(补齐缺失)" : "转换", GUILayout.Height(28f)))
                        ConvertSelected();
                    if (GUILayout.Button("从老端更新", GUILayout.Height(28f)))
                        UpdateSelected();
                    using (new EditorGUI.DisabledScope(s.TileConverted == 0 && !s.PreviewConverted))
                    {
                        if (GUILayout.Button("定位产物", GUILayout.Height(28f)))
                        {
                            string dir = MapTileConverter.GameResMapDir(s.MapResId);
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(dir);
                            if (obj != null) { EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
                        }
                        if (GUILayout.Button("删产物", GUILayout.Height(28f))
                            && EditorUtility.DisplayDialog("删除地图产物",
                                MapTileConverter.GameResMapDir(s.MapResId) + "\n删除已转的瓦片/底图(源不动,可随时重转)。", "删", "算了"))
                        {
                            AssetDatabase.DeleteAsset(MapTileConverter.GameResMapDir(s.MapResId));
                            _stats[_selected] = MapTileConverter.Inspect(_selected);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ConvertSelected()
        {
            int sceneId = _selected;
            int copied, skipped;
            bool canceled;
            MapStat st;
            try
            {
                st = MapTileConverter.Convert(sceneId,
                    (f, msg) => EditorUtility.DisplayCancelableProgressBar("转换地图 " + sceneId, msg, f),
                    out copied, out skipped, out canceled);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            _stats[sceneId] = st;

            if (!canceled && LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[MapAsset] Addressable 分组失败: " + e.Message); }
            }

            EditorUtility.DisplayDialog("转换地图",
                $"场景 {sceneId}(底图 {st.MapResId})\n本次新转 {copied} 张,跳过 {skipped}\n瓦片合计 {st.TileConverted}/{st.TileTotal}" +
                (canceled ? "\n(中途取消)" : (LayaUISettings.AutoGroupAfterConvert ? "\n已执行 Addressable 自动分组" : "")),
                "好");
        }

        private void UpdateSelected()
        {
            int sceneId = _selected;
            MapStat before = StatOf(sceneId);
            if (!EditorUtility.DisplayDialog("从老端更新地图",
                    $"将用老客户端资源强制覆盖场景 {sceneId}(底图 {before.MapResId})。\n\n" +
                    $"源: {MapTileConverter.SourceMapDir(before.MapResId)}\n" +
                    $"目标: {MapTileConverter.GameResMapDir(before.MapResId)}\n\n" +
                    "会更新 .bytes、缩略图和 .bytes 声明的有效瓦片；已有 Unity .meta/GUID 保留，历史多余瓦片不删除。",
                    "更新", "取消"))
                return;

            int updated, skipped;
            bool canceled;
            MapStat st;
            try
            {
                st = MapTileConverter.Update(sceneId,
                    (f, msg) => EditorUtility.DisplayCancelableProgressBar("从老端更新地图 " + sceneId, msg, f),
                    out updated, out skipped, out canceled);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            _stats[sceneId] = st;

            if (!canceled && LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[MapAsset] Addressable 分组失败: " + e.Message); }
            }

            EditorUtility.DisplayDialog("从老端更新地图",
                $"场景 {sceneId}(底图 {st.MapResId})\n本次更新 {updated} 张资源,跳过 {skipped}\n" +
                $"有效瓦片 {st.TileConverted}/{st.TileTotal}" +
                (canceled ? "\n(中途取消)" : (LayaUISettings.AutoGroupAfterConvert ? "\n已执行 Addressable 自动分组" : "")),
                "好");
        }

        /// <summary>缩略图(底图)+ 出生点 + NPC/怪/采集/传送门/Boss/复活 标注 + 实体清单。</summary>
        private void DrawThumbnail(MapStat s)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("缩略图 + 标注点", EditorStyles.boldLabel);

            DrawServerDataBar();
            DrawLayerToggles();

            if (!s.PreviewConverted)
            {
                EditorGUILayout.HelpBox("底图未转,转换后这里显示缩略图与标注。", MessageType.None);
                DrawEntityList();
                return;
            }
            string path = "Assets/GameRes/resource/game/scene/map/" + s.MapResId + "/tile/" + s.MapResId + ".jpg";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                EditorGUILayout.LabelField("缩略图加载失败: " + path, EditorStyles.miniLabel);
                DrawEntityList();
                return;
            }

            int w = s.MapWidth, h = s.MapHeight;

            // 缩放/平移工具条
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"缩放 {_zoom * 100f:0}%", EditorStyles.miniLabel, GUILayout.Width(80f));
                if (GUILayout.Button("－", GUILayout.Width(28f))) _zoom = Mathf.Clamp(_zoom * 0.8f, 0.2f, 10f);
                if (GUILayout.Button("＋", GUILayout.Width(28f))) _zoom = Mathf.Clamp(_zoom * 1.25f, 0.2f, 10f);
                if (GUILayout.Button("适应", GUILayout.Width(48f))) { _zoom = 1f; _pan = Vector2.zero; }
                EditorGUILayout.LabelField("滚轮缩放 · 右键/中键拖动平移" + (_editMode ? " · 左键点/拖标注" : " · 左键拖动平移"),
                    EditorStyles.miniLabel);
            }

            // 大画布(铺满宽度,尽量高);用 Group 裁剪,组内局部坐标
            float canvasH = Mathf.Max(360f, position.height - 380f);
            Rect box = GUILayoutUtility.GetRect(10f, canvasH, GUILayout.ExpandWidth(true));
            GUI.BeginGroup(box);
            Rect local = new Rect(0f, 0f, box.width, box.height);
            EditorGUI.DrawRect(local, new Color(0.10f, 0.10f, 0.10f, 1f));

            if (w > 0 && h > 0)
            {
                ComputeTransform(local, w, h);
                GUI.DrawTexture(new Rect(_mapOrigin.x, _mapOrigin.y, w * _mapScale, h * _mapScale), tex, ScaleMode.StretchToFill);

                int bx = _entities != null ? _birthX : s.BirthX;
                int by = _entities != null ? _birthY : s.BirthY;
                if (bx > 0 || by > 0) DrawMarker(bx, by, C_BIRTH, 4f);
                if (_entities != null)
                {
                    if (_lyReborn) DrawMarkers(_entities.Reborns, C_REBORN, 3f);
                    if (_lyDoor) DrawMarkers(_entities.Doors, C_DOOR, 3.5f);
                    if (_lyCollect) DrawMarkers(_entities.Collects, C_COLLECT, 3f);
                    if (_lyMon) DrawMarkers(_entities.Monsters, C_MON, 3f);
                    if (_lyNpc) DrawMarkers(_entities.Npcs, C_NPC, 3.5f);
                    if (_lyBoss) DrawMarkers(_entities.Bosses, C_BOSS, 4.5f);
                }
                if (_editMode && _entities != null) DrawSelectionHighlight();
                HandleCanvasInput(local, w, h);
            }
            GUI.EndGroup();

            DrawEntityList();
        }

        /// <summary>yu_server 路径状态条:可用→显示来源;不可用→提示 + 改路径。</summary>
        private void DrawServerDataBar()
        {
            if (MapServerData.Available)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("标注来源: yu_server data_scene.erl / data_boss.erl + config_mon/npc",
                        EditorStyles.miniLabel);
                    if (GUILayout.Button("改路径", GUILayout.Width(60f), GUILayout.Height(18f))) PickServerRoot();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未找到 yu_server data_scene.erl,无法显示 NPC/怪/门/Boss 标注。\n当前: "
                    + MapServerData.SceneErlPath, MessageType.Warning);
                if (GUILayout.Button("设置 yu_server 路径", GUILayout.Height(20f))) PickServerRoot();
            }
        }

        private void PickServerRoot()
        {
            string p = EditorUtility.OpenFolderPanel("选 yu_server 仓库根目录", MapServerData.ServerRoot, "");
            if (string.IsNullOrEmpty(p)) return;
            MapServerData.ServerRoot = p; // setter 内部 Reload
            if (_selected >= 0) _entities = MapServerData.Available ? MapServerData.GetEntities(_selected) : null;
        }

        private void DrawLayerToggles()
        {
            if (_entities == null) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                _lyMon = LayerToggle(_lyMon, C_MON, "怪", _entities.Monsters.Count);
                _lyCollect = LayerToggle(_lyCollect, C_COLLECT, "采集", _entities.Collects.Count);
                _lyNpc = LayerToggle(_lyNpc, C_NPC, "NPC", _entities.Npcs.Count);
                _lyDoor = LayerToggle(_lyDoor, C_DOOR, "门", _entities.Doors.Count);
                _lyBoss = LayerToggle(_lyBoss, C_BOSS, "Boss", _entities.Bosses.Count);
                _lyReborn = LayerToggle(_lyReborn, C_REBORN, "复活", _entities.Reborns.Count);
            }
        }

        private static bool LayerToggle(bool on, Color c, string label, int count)
        {
            Color old = GUI.color;
            GUI.color = on ? c : new Color(c.r, c.g, c.b, 0.4f);
            bool v = GUILayout.Toggle(on, $"{label} {count}", "Button", GUILayout.Height(20f));
            GUI.color = old;
            return v;
        }

        private void DrawEntityList()
        {
            if (_entities == null || _entities.Total == 0) return;
            _entityListFoldout = EditorGUILayout.Foldout(_entityListFoldout, $"标注清单({_entities.Total})", true);
            if (!_entityListFoldout) return;
            using (new EditorGUI.IndentLevelScope())
            {
                if (_lyBoss) ListEntities("Boss", _entities.Bosses);
                if (_lyNpc) ListEntities("NPC", _entities.Npcs);
                if (_lyMon) ListEntities("怪", _entities.Monsters);
                if (_lyCollect) ListEntities("采集", _entities.Collects);
                if (_lyDoor) ListEntities("传送门", _entities.Doors);
                if (_lyReborn) ListEntities("复活点", _entities.Reborns);
            }
        }

        private static void ListEntities(string label, List<MapEntity> list)
        {
            if (list == null || list.Count == 0) return;
            EditorGUILayout.LabelField($"{label}({list.Count})", EditorStyles.miniBoldLabel);
            int cap = Mathf.Min(list.Count, 60);
            for (int i = 0; i < cap; i++)
            {
                MapEntity e = list[i];
                string idTxt = e.Id > 0 ? e.Id.ToString() : "";
                string info = string.IsNullOrEmpty(e.Info) ? "" : "  " + e.Info;
                EditorGUILayout.LabelField($"  {idTxt} {e.Name}  ({e.X},{e.Y}){info}", EditorStyles.miniLabel);
            }
            if (list.Count > cap)
                EditorGUILayout.LabelField($"  …还有 {list.Count - cap} 个", EditorStyles.miniLabel);
        }

        // 世界(地图像素)↔ 画布组内局部坐标
        private void ComputeTransform(Rect local, int w, int h)
        {
            _localSize = new Vector2(local.width, local.height);
            float baseScale = Mathf.Min(local.width / w, local.height / h);
            _mapScale = baseScale * _zoom;
            float cw = w * _mapScale, ch = h * _mapScale;
            _mapOrigin = new Vector2((local.width - cw) * 0.5f + _pan.x, (local.height - ch) * 0.5f + _pan.y);
        }

        private Vector2 WorldToLocal(int x, int y) =>
            new Vector2(_mapOrigin.x + x * _mapScale, _mapOrigin.y + y * _mapScale);

        private void DrawMarkers(List<MapEntity> list, Color c, float size)
        {
            if (list == null) return;
            foreach (MapEntity e in list) DrawMarker(e.X, e.Y, c, size);
        }

        private void DrawMarker(int x, int y, Color c, float size)
        {
            Vector2 p = WorldToLocal(x, y);
            if (p.x < -size || p.y < -size || p.x > _localSize.x + size || p.y > _localSize.y + size) return; // 视野外裁剪
            EditorGUI.DrawRect(new Rect(p.x - size, p.y - size, size * 2f, size * 2f), c);
        }

        // ==================== 编辑 ====================

        private void DrawEditToolbar()
        {
            if (!MapServerData.Available || _entities == null) return;
            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool em = GUILayout.Toggle(_editMode, _editMode ? "编辑中" : "编辑模式", "Button",
                    GUILayout.Height(22f), GUILayout.Width(90f));
                if (em != _editMode) { _editMode = em; ClearSelection(); }
                using (new EditorGUI.DisabledScope(!_dirty))
                {
                    if (GUILayout.Button("保存到 data_scene.erl", GUILayout.Height(22f))) SaveScene();
                    if (GUILayout.Button("还原", GUILayout.Height(22f), GUILayout.Width(56f))) LoadEntities(_selected);
                }
            }
            if (_editMode)
                EditorGUILayout.HelpBox("在缩略图上点标注点选中、拖动移动;右下属性可精调坐标/增删。保存写回 yu_server data_scene.erl(自动 .bak 备份)。",
                    MessageType.Info);
        }

        private IEnumerable<(List<MapEntity> list, bool on)> EditableLayers()
        {
            yield return (_entities.Npcs, _lyNpc);
            yield return (_entities.Monsters, _lyMon);
            yield return (_entities.Collects, _lyCollect);
            yield return (_entities.Doors, _lyDoor);
            yield return (_entities.Reborns, _lyReborn);
        }

        /// <summary>画布交互(组内局部坐标):滚轮缩放(向光标)+ 平移 +(编辑模式)点选/拖动标注。</summary>
        private void HandleCanvasInput(Rect local, int w, int h)
        {
            Event ev = Event.current;
            bool inside = local.Contains(ev.mousePosition);

            if (ev.type == EventType.ScrollWheel && inside)
            {
                float nz = Mathf.Clamp(_zoom * (ev.delta.y < 0f ? 1.1f : 0.9f), 0.2f, 10f);
                ZoomTowardCursor(nz, ev.mousePosition, local, w, h);
                ev.Use();
                Repaint();
                return;
            }

            switch (ev.type)
            {
                case EventType.MouseDown:
                    if (!inside) break;
                    if (_editMode && ev.button == 0)
                    {
                        PickMarker(ev.mousePosition);
                        if (_selBirth || (_selList != null && _selIdx >= 0)) _dragging = true;
                        else _panning = true; // 点空白处=平移
                    }
                    else { _panning = true; } // 右键/中键(或非编辑左键)平移
                    ev.Use();
                    Repaint();
                    break;
                case EventType.MouseDrag:
                    if (_dragging)
                    {
                        int wx = Mathf.Clamp(Mathf.RoundToInt((ev.mousePosition.x - _mapOrigin.x) / _mapScale), 0, w);
                        int wy = Mathf.Clamp(Mathf.RoundToInt((ev.mousePosition.y - _mapOrigin.y) / _mapScale), 0, h);
                        SetSelectedPos(wx, wy);
                        ev.Use();
                        Repaint();
                    }
                    else if (_panning)
                    {
                        _pan += ev.delta;
                        ev.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (_dragging || _panning) { _dragging = false; _panning = false; ev.Use(); }
                    break;
            }
        }

        private void ZoomTowardCursor(float newZoom, Vector2 pivot, Rect local, int w, int h)
        {
            // 保持光标下的世界点不动:先用当前变换求该世界点,改 zoom 后调 _pan 使其屏幕位置不变
            float wx = (pivot.x - _mapOrigin.x) / _mapScale;
            float wy = (pivot.y - _mapOrigin.y) / _mapScale;
            _zoom = newZoom;
            float baseScale = Mathf.Min(local.width / w, local.height / h);
            float ns = baseScale * _zoom;
            _pan.x = pivot.x - wx * ns - (local.width - w * ns) * 0.5f;
            _pan.y = pivot.y - wy * ns - (local.height - h * ns) * 0.5f;
        }

        private void PickMarker(Vector2 mouse)
        {
            float best = 12f;
            List<MapEntity> bestList = null;
            int bestIdx = -1;
            bool bestBirth = false;

            if (Vector2.Distance(WorldToLocal(_birthX, _birthY), mouse) < best) { best = Vector2.Distance(WorldToLocal(_birthX, _birthY), mouse); bestBirth = true; }

            foreach ((List<MapEntity> list, bool on) in EditableLayers())
            {
                if (!on || list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    float d = Vector2.Distance(WorldToLocal(list[i].X, list[i].Y), mouse);
                    if (d < best) { best = d; bestBirth = false; bestList = list; bestIdx = i; }
                }
            }
            _selBirth = bestBirth;
            _selList = bestList;
            _selIdx = bestIdx;
        }

        private void SetSelectedPos(int wx, int wy)
        {
            if (_selBirth) { _birthX = wx; _birthY = wy; _dirty = true; }
            else if (_selList != null && _selIdx >= 0 && _selIdx < _selList.Count)
            {
                MapEntity e = _selList[_selIdx];
                e.X = wx; e.Y = wy;
                _selList[_selIdx] = e;
                _dirty = true;
            }
        }

        private void DrawSelectionHighlight()
        {
            Vector2 sp;
            if (_selBirth) sp = WorldToLocal(_birthX, _birthY);
            else if (_selList != null && _selIdx >= 0 && _selIdx < _selList.Count)
                sp = WorldToLocal(_selList[_selIdx].X, _selList[_selIdx].Y);
            else return;

            float r = 6f;
            var c = Color.white;
            EditorGUI.DrawRect(new Rect(sp.x - r, sp.y - r, r * 2f, 1f), c);
            EditorGUI.DrawRect(new Rect(sp.x - r, sp.y + r, r * 2f, 1f), c);
            EditorGUI.DrawRect(new Rect(sp.x - r, sp.y - r, 1f, r * 2f), c);
            EditorGUI.DrawRect(new Rect(sp.x + r, sp.y - r, 1f, r * 2f + 1f), c);
        }

        private void DrawEditPanel()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("编辑选中", EditorStyles.boldLabel);

            if (_selBirth)
            {
                EditorGUILayout.LabelField("类型", "出生点");
                EditorGUI.BeginChangeCheck();
                int x = EditorGUILayout.IntField("X", _birthX);
                int y = EditorGUILayout.IntField("Y", _birthY);
                if (EditorGUI.EndChangeCheck()) { _birthX = x; _birthY = y; _dirty = true; }
            }
            else if (_selList != null && _selIdx >= 0 && _selIdx < _selList.Count)
            {
                MapEntity e = _selList[_selIdx];
                EditorGUILayout.LabelField("类型", KindLabel(e.Kind));
                EditorGUILayout.LabelField("名称", e.Name);
                EditorGUI.BeginChangeCheck();
                int id = EditorGUILayout.IntField(e.Kind == MapEntityKind.Door ? "目标场景" : "ID", e.Id);
                int x = EditorGUILayout.IntField("X", e.X);
                int y = EditorGUILayout.IntField("Y", e.Y);
                int type = e.Type, group = e.Group, p1 = e.P1, p2 = e.P2;
                string action = e.Action ?? "";
                if (e.Kind == MapEntityKind.Monster || e.Kind == MapEntityKind.Collect)
                {
                    type = EditorGUILayout.IntField("战斗类型", type);
                    group = EditorGUILayout.IntField("分组", group);
                }
                if (e.Kind == MapEntityKind.Npc) action = EditorGUILayout.TextField("动画", action);
                if (e.Kind == MapEntityKind.Door)
                {
                    p1 = EditorGUILayout.IntField("目标X", p1);
                    p2 = EditorGUILayout.IntField("目标Y", p2);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    e.Id = id; e.X = x; e.Y = y; e.Type = type; e.Group = group; e.Action = action; e.P1 = p1; e.P2 = p2;
                    _selList[_selIdx] = e;
                    _dirty = true;
                }
                if (e.Kind == MapEntityKind.Boss)
                    EditorGUILayout.HelpBox("Boss 在 data_boss.erl,本编辑器只读不写。", MessageType.None);
                else if (GUILayout.Button("删除该标注"))
                {
                    _selList.RemoveAt(_selIdx);
                    ClearSelection();
                    _dirty = true;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("在缩略图上点一个标注点选中(或拖动移动);也可用下面按钮新增。", MessageType.Info);
            }

            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("＋怪")) AddEntity(MapEntityKind.Monster);
                if (GUILayout.Button("＋NPC")) AddEntity(MapEntityKind.Npc);
                if (GUILayout.Button("＋传送门")) AddEntity(MapEntityKind.Door);
                if (GUILayout.Button("＋复活点")) AddEntity(MapEntityKind.Reborn);
            }
        }

        private void AddEntity(MapEntityKind kind)
        {
            if (_entities == null) return;
            var e = new MapEntity
            {
                Kind = kind, X = _birthX, Y = _birthY,
                Name = kind == MapEntityKind.Npc ? "NPC" : kind == MapEntityKind.Door ? "传送门" : kind == MapEntityKind.Reborn ? "复活点" : "怪",
                Action = kind == MapEntityKind.Npc ? "stat3" : "",
            };
            List<MapEntity> list =
                kind == MapEntityKind.Npc ? _entities.Npcs :
                kind == MapEntityKind.Door ? _entities.Doors :
                kind == MapEntityKind.Reborn ? _entities.Reborns : _entities.Monsters;
            list.Add(e);
            _selBirth = false;
            _selList = list;
            _selIdx = list.Count - 1;
            _dirty = true;
        }

        private void SaveScene()
        {
            if (_entities == null) return;
            if (!EditorUtility.DisplayDialog("保存到 data_scene.erl",
                $"将把场景 {_selected} 的标注(怪/NPC/传送门/复活/出生点)写回 yu_server data_scene.erl。\n会先自动备份 .bak。继续?",
                "保存", "取消"))
                return;

            string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            MapSceneWriter.Result r = MapSceneWriter.SaveScene(_selected, _entities, _birthX, _birthY, ts);
            if (r.Ok)
            {
                _dirty = false;
                MapServerData.Reload();
                LoadEntities(_selected);
                EditorUtility.DisplayDialog("保存成功", "已写回 data_scene.erl\n备份: " + r.BackupPath, "好");
            }
            else
            {
                EditorUtility.DisplayDialog("保存失败", r.Message, "好");
            }
        }

        private static string KindLabel(MapEntityKind k)
        {
            switch (k)
            {
                case MapEntityKind.Monster: return "怪";
                case MapEntityKind.Collect: return "采集";
                case MapEntityKind.Npc: return "NPC";
                case MapEntityKind.Door: return "传送门";
                case MapEntityKind.Boss: return "Boss";
                case MapEntityKind.Reborn: return "复活点";
                default: return k.ToString();
            }
        }

        /// <summary>一键转换全部缺失/部分的场景(已全转的跳过)。大批量,带强确认 + 进度 + 可取消。</summary>
        private void ConvertAll()
        {
            // 先确保每张图都盘点过,才能判断缺失
            InspectAll();
            var todo = new List<int>();
            foreach (int id in _sceneIds)
                if (_stats.TryGetValue(id, out MapStat st) && !st.FullyConverted) todo.Add(id);

            if (todo.Count == 0)
            {
                EditorUtility.DisplayDialog("一键转换全部", "所有场景瓦片都已转完,无需转换。", "好");
                return;
            }
            if (!EditorUtility.DisplayDialog("一键转换全部",
                $"将转换 {todo.Count} 张未转/部分转的地图(已全转的跳过)。\n" +
                "这是大批量操作,会写入很多瓦片文件、耗时较久,可中途取消。继续?", "开始转换", "取消"))
                return;

            int done = 0;
            bool canceled = false;
            try
            {
                for (int i = 0; i < todo.Count; i++)
                {
                    int id = todo[i];
                    if (EditorUtility.DisplayCancelableProgressBar("一键转换全部地图",
                            $"场景 {id}  ({i + 1}/{todo.Count})", (float)i / todo.Count))
                    {
                        canceled = true;
                        break;
                    }
                    _stats[id] = MapTileConverter.Convert(id,
                        (f, msg) => EditorUtility.DisplayCancelableProgressBar(
                            "一键转换全部地图",
                            $"场景 {id} ({i + 1}/{todo.Count}) - {msg}",
                            (i + f) / todo.Count),
                        out _, out _, out bool oneCanceled);
                    done++;
                    if (oneCanceled) { canceled = true; break; }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[MapAsset] Addressable 分组失败: " + e.Message); }
            }
            EditorUtility.DisplayDialog("一键转换全部",
                $"完成 {done}/{todo.Count}" + (canceled ? "(已取消剩余)" : "") +
                (LayaUISettings.AutoGroupAfterConvert ? "\n已执行 Addressable 自动分组" : ""), "好");
        }

        private void InspectAll()
        {
            try
            {
                for (int i = 0; i < _sceneIds.Count; i++)
                {
                    int id = _sceneIds[i];
                    if (EditorUtility.DisplayCancelableProgressBar("盘点地图转换状态",
                            $"{id}  ({i + 1}/{_sceneIds.Count})", (float)(i + 1) / _sceneIds.Count))
                        break;
                    _stats[id] = MapTileConverter.Inspect(id);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private string StatusIcon(int sceneId)
        {
            if (!_stats.TryGetValue(sceneId, out MapStat s)) return "·";
            if (s.FullyConverted) return "<color=#5fd35f>●</color>";
            if (s.PartiallyConverted) return "<color=#e0c050>◐</color>";
            return "<color=#c05050>○</color>";
        }
    }
}
