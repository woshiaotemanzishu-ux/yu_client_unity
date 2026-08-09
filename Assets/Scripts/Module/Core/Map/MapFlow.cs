using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 地图合并模块编排。老端 MapEnterView 默认页是区域地图(index 0)，世界地图是 index 1。
    /// 当前 Prefab 尚缺外层可见页签，先在同一根上保证默认页、互斥切页、关闭和热重开生命周期正确。
    /// </summary>
    public static class MapFlow
    {
        private const string MODULE = "map";
        private const string PREFAB = "MapModule";

        private static GameObject _moduleRoot;
        private static AreaMapView _areaView;
        private static WorldMapView _worldView;
        private static BaseView _currentView;
        private static bool _loading;
        private static string _pendingViewType;

        public static void Toggle()
        {
            if (_currentView != null && _currentView.IsShown)
            {
                Close();
                return;
            }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void OpenArea() => _ = OpenAsync(nameof(AreaMapView));

        public static void OpenWorld() => _ = OpenAsync(nameof(WorldMapView));

        public static void Close()
        {
            if (_currentView != null) _currentView.Hide();
        }

        /// <summary>兼容模块内旧入口；只允许两个已烘焙页面，并始终互斥显示。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_moduleRoot == null)
            {
                _ = OpenAsync(viewTypeName);
                return;
            }

            BaseView target = ResolvePage(viewTypeName);
            if (target != null)
            {
                ShowExclusive(target);
                return;
            }
            GameLog.Warn("Map", "地图子页不存在: {0}", viewTypeName);
        }

        private static async Task OpenAsync(string viewTypeName = null)
        {
            if (_moduleRoot != null)
            {
                ShowExclusive(ResolvePage(viewTypeName) ?? _areaView);
                return;
            }

            if (_loading)
            {
                if (!string.IsNullOrEmpty(viewTypeName)) _pendingViewType = viewTypeName;
                return;
            }
            _loading = true;
            _pendingViewType = viewTypeName;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (root == null)
            {
                GameLog.Error("Map", "MapModule prefab load failed: {0}", key);
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = PREFAB;

            BaseView[] views = root.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView view in views) view.gameObject.SetActive(false);
            foreach (BaseView view in views)
            {
                if (view is AreaMapView area) _areaView = area;
                else if (view is WorldMapView world) _worldView = world;
            }

            if (_areaView == null || _worldView == null)
            {
                GameLog.Warn("Map", "MapModule 缺 AreaMapView/WorldMapView(重跑 map 回填)");
                return;
            }

            ShowExclusive(ResolvePage(_pendingViewType) ?? _areaView);
            _pendingViewType = null;
            GameLog.Info("Map", "地图打开(默认区域地图): {0}", key);
        }

        private static BaseView ResolvePage(string viewTypeName)
        {
            if (string.IsNullOrEmpty(viewTypeName) || viewTypeName == nameof(AreaMapView)) return _areaView;
            if (viewTypeName == nameof(WorldMapView)) return _worldView;
            return null;
        }

        private static void ShowExclusive(BaseView target)
        {
            if (target == null) return;
            if (_areaView != null && _areaView != target) _areaView.Hide();
            if (_worldView != null && _worldView != target) _worldView.Hide();
            _currentView = target;
            if (!target.IsShown) target.Show();
        }

        internal static void Reset()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _areaView = null;
            _worldView = null;
            _currentView = null;
            _loading = false;
            _pendingViewType = null;
        }
    }

    /// <summary>
    /// 老端两张客户端地图配置的只读门面。资源未同步时返回空集合，不伪造地图数据；后续资源到位后无需改 View。
    /// </summary>
    internal static class MapConfigs
    {
        internal sealed class WorldEntry
        {
            public int SceneId;
            public bool Open;
            public string Name;
            public string Image;
            public Vector2 RootPosition;
            public Vector2 LocatePosition;
            public int MinLevel;
            public int MaxLevel;
        }

        internal sealed class AreaPoint
        {
            public int MonsterId;
            public bool IsNpc;
            public int Level;
            public float X;
            public float Y;
        }

        private static readonly List<WorldEntry> World = new List<WorldEntry>();
        private static readonly Dictionary<int, List<AreaPoint>> Areas = new Dictionary<int, List<AreaPoint>>();
        private static Task _loading;
        private static bool _loaded;

        internal static IReadOnlyList<WorldEntry> WorldEntries => World;

        internal static IReadOnlyList<AreaPoint> GetAreaPoints(int sceneId) =>
            Areas.TryGetValue(sceneId, out List<AreaPoint> points) ? points : System.Array.Empty<AreaPoint>();

        internal static Task EnsureLoaded()
        {
            if (_loaded) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadAsync();
            return _loading;
        }

        private static async Task LoadAsync()
        {
            TextAsset worldAsset = await ResManager.LoadOptionalAsync<TextAsset>(
                GameResPath.GetClientConfigPath("ClientWorldMapConfig"));
            TextAsset areaAsset = await ResManager.LoadOptionalAsync<TextAsset>(
                GameResPath.GetClientConfigPath("ClientMapConfig"));
            if (worldAsset == null || areaAsset == null)
            {
                if (worldAsset != null) ResManager.Release(worldAsset);
                if (areaAsset != null) ResManager.Release(areaAsset);
                GameLog.Warn("Map", "ClientWorldMapConfig/ClientMapConfig 未进入 Unity 资源闭包，地图数据保持空态");
                return;
            }

            try
            {
                ParseWorld(JObject.Parse(worldAsset.text));
                ParseAreas(JObject.Parse(areaAsset.text));
                _loaded = true;
                GameLog.Info("Map", "地图配置加载完成: world={0} area={1}", World.Count, Areas.Count);
            }
            catch (System.Exception e)
            {
                World.Clear();
                Areas.Clear();
                GameLog.Error("Map", "地图配置解析失败: {0}", e.Message);
            }
            finally
            {
                ResManager.Release(worldAsset);
                ResManager.Release(areaAsset);
            }
        }

        private static void ParseWorld(JObject root)
        {
            World.Clear();
            if (!(root?["world_map"] is JObject table)) return;
            foreach (KeyValuePair<string, JToken> pair in table)
            {
                if (!(pair.Value is JObject row)) continue;
                Vector2 rootPosition = ReadVector(row["root_pos"]);
                Vector2 locatePosition = ReadVector(row["locate_pos"]);
                int[] levels = ReadPair(row["scene_lv"]);
                World.Add(new WorldEntry
                {
                    SceneId = ReadInt(row["scene_id"]),
                    Open = ReadBool(row["open_state"]),
                    Name = ReadString(row["scene_name"]),
                    Image = ReadString(row["img_source"]),
                    RootPosition = rootPosition,
                    LocatePosition = locatePosition,
                    MinLevel = levels[0],
                    MaxLevel = levels[1]
                });
            }
            World.Sort((a, b) => a.SceneId.CompareTo(b.SceneId));
        }

        private static void ParseAreas(JObject root)
        {
            Areas.Clear();
            if (root == null) return;
            foreach (KeyValuePair<string, JToken> pair in root)
            {
                if (!(pair.Value is JObject row) || !(row["point_list"] is JArray list)) continue;
                int sceneId = ReadInt(row["scene_id"]);
                if (sceneId <= 0) int.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out sceneId);
                if (sceneId <= 0) continue;
                var points = new List<AreaPoint>(list.Count);
                foreach (JToken token in list)
                {
                    if (!(token is JObject point)) continue;
                    points.Add(new AreaPoint
                    {
                        MonsterId = ReadInt(point["mon_id"]),
                        IsNpc = ReadBool(point["is_npc"]),
                        Level = ReadInt(point["lv"]),
                        X = ReadFloat(point["x"]),
                        Y = ReadFloat(point["y"])
                    });
                }
                Areas[sceneId] = points;
            }
        }

        private static Vector2 ReadVector(JToken token)
        {
            if (!(token is JArray values) || values.Count < 2) return Vector2.zero;
            return new Vector2(ReadFloat(values[0]), ReadFloat(values[1]));
        }

        private static int[] ReadPair(JToken token)
        {
            if (!(token is JArray values) || values.Count < 2) return new[] { 0, 0 };
            return new[] { ReadInt(values[0]), ReadInt(values[1]) };
        }

        private static int ReadInt(JToken token) => token == null ? 0
            : token.Type == JTokenType.Integer ? token.Value<int>()
            : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int value) ? value : 0;

        private static float ReadFloat(JToken token) => token == null ? 0f
            : token.Type == JTokenType.Integer || token.Type == JTokenType.Float ? token.Value<float>()
            : float.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float value) ? value : 0f;

        private static bool ReadBool(JToken token) => token != null && (token.Type == JTokenType.Boolean
            ? token.Value<bool>()
            : token.ToString() == "1" || bool.TryParse(token.ToString(), out bool value) && value);

        private static string ReadString(JToken token) => token == null || token.Type == JTokenType.Null
            ? string.Empty
            : token.ToString().Trim();
    }
}
