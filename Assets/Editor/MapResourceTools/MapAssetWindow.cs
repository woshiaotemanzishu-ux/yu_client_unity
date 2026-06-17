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
                        _selected = id;
                        StatOf(id); // 选中即盘点该图
                        _entities = MapServerData.Available ? MapServerData.GetEntities(id) : null;
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

                DrawThumbnail(s);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("转换状态", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("瓦片", $"{StatusIcon(_selected)} {s.TileConverted}/{s.TileTotal} 已转进 Assets/GameRes");
                EditorGUILayout.LabelField("底图", s.PreviewConverted ? "● 已转" : "○ 未转");
                if (s.PartiallyConverted)
                    EditorGUILayout.HelpBox("只转了一部分:没转到的区域第一次进去仍会现导卡顿。点下面「转换」补齐。", MessageType.Warning);
                else if (s.FullyConverted)
                    EditorGUILayout.HelpBox("瓦片已全部转好。若仍卡,确认已「Addressable 自动分组」(运行时才走异步,不走编辑器兜底)。", MessageType.Info);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
                bool autoGroup = LayaUISettings.AutoGroupAfterConvert;
                bool newAutoGroup = EditorGUILayout.ToggleLeft("转换后顺便 Addressable 自动分组(推荐)", autoGroup);
                if (newAutoGroup != autoGroup) LayaUISettings.AutoGroupAfterConvert = newAutoGroup;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(s.TileConverted > 0 ? "转换(补齐缺失)" : "转换", GUILayout.Height(28f)))
                        ConvertSelected();
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

            Rect box = GUILayoutUtility.GetRect(10f, 300f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(box, new Color(0.12f, 0.12f, 0.12f, 1f));
            Rect fit = FitRect(box, tex.width, tex.height);
            GUI.DrawTexture(fit, tex, ScaleMode.ScaleToFit);

            int w = s.MapWidth, h = s.MapHeight;
            if (w > 0 && h > 0)
            {
                // 出生点
                if (s.BirthX > 0 || s.BirthY > 0) DrawMarker(fit, w, h, s.BirthX, s.BirthY, C_BIRTH, 4f);
                // 标注点(按图层)
                if (_entities != null)
                {
                    if (_lyReborn) DrawMarkers(fit, w, h, _entities.Reborns, C_REBORN, 2.5f);
                    if (_lyDoor) DrawMarkers(fit, w, h, _entities.Doors, C_DOOR, 3f);
                    if (_lyCollect) DrawMarkers(fit, w, h, _entities.Collects, C_COLLECT, 2.5f);
                    if (_lyMon) DrawMarkers(fit, w, h, _entities.Monsters, C_MON, 2.5f);
                    if (_lyNpc) DrawMarkers(fit, w, h, _entities.Npcs, C_NPC, 3f);
                    if (_lyBoss) DrawMarkers(fit, w, h, _entities.Bosses, C_BOSS, 4f);
                }
            }

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

        private void DrawMarkers(Rect fit, int w, int h, List<MapEntity> list, Color c, float size)
        {
            if (list == null) return;
            foreach (MapEntity e in list) DrawMarker(fit, w, h, e.X, e.Y, c, size);
        }

        private static void DrawMarker(Rect fit, int w, int h, int x, int y, Color c, float size)
        {
            float nx = Mathf.Clamp01((float)x / w);
            float ny = Mathf.Clamp01((float)y / h);
            float cx = fit.x + nx * fit.width;
            float cy = fit.y + ny * fit.height;
            EditorGUI.DrawRect(new Rect(cx - size, cy - size, size * 2f, size * 2f), c);
        }

        private static Rect FitRect(Rect box, float w, float h)
        {
            if (w <= 0f || h <= 0f) return box;
            float scale = Mathf.Min(box.width / w, box.height / h);
            float dw = w * scale, dh = h * scale;
            return new Rect(box.x + (box.width - dw) * 0.5f, box.y + (box.height - dh) * 0.5f, dw, dh);
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
