using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;
using Shenxiao.Framework.Scene3D.Map;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.MapTools
{
    /// <summary>一张地图的转换盘点(列表/详情用)。SceneId 是场景,MapResId 是其底图资源(可能被多场景复用)。</summary>
    public struct MapStat
    {
        public int SceneId;
        public int MapResId;
        public bool HasBytes;
        public int MapWidth;
        public int MapHeight;
        public int TileSize;
        public int TileTotal;      // 源里 RRCC 命名的 .jxr 数(该 mapResId)
        public int TileConverted;  // 已转进 GameRes 的 RRCC .jpg 数
        public bool PreviewConverted;

        // 来自 config_scene.json 的场景元数据(名字/类型/出生点)
        public string Name;
        public int SceneType;
        public int BirthX;
        public int BirthY;

        public bool FullyConverted => TileTotal > 0 && TileConverted >= TileTotal;
        public bool PartiallyConverted => TileConverted > 0 && TileConverted < TileTotal;
    }

    /// <summary>config_scene.json 一条场景元数据(名字/类型/出生点/尺寸)。</summary>
    public struct SceneMeta
    {
        public bool Found;
        public string Name;
        public int Type;
        public int BirthX;
        public int BirthY;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// 地图瓦片离线转换:把 yu_client 的 .bytes/底图/瓦片(.jxr,实为 JPEG)批量拷进本项目 Assets/GameRes,
    /// 转成 Unity 可加载资源。纯编辑器期一次性操作;运行时只走本项目 Addressables,不碰老项目。
    /// 与 ResManager 的 #if UNITY_EDITOR 逐块现导兜底是同一件事的"提前批量版"——提前转好,兜底就永不触发,不卡。
    /// </summary>
    public static class MapTileConverter
    {
        private static string SrcMapRoot => Path.Combine(LayaUISettings.CdnResourceRoot, "game", "scene", "map");
        private static string GameResMapRoot =>
            Path.Combine(Application.dataPath, "GameRes", "resource", "game", "scene", "map");

        /// <summary>扫描所有"场景"(有 {id}/{id}.bytes 的目录),升序返回 sceneId。</summary>
        public static List<int> ScanScenes()
        {
            var ids = new List<int>();
            if (!Directory.Exists(SrcMapRoot)) return ids;
            foreach (string dir in Directory.GetDirectories(SrcMapRoot))
            {
                string name = Path.GetFileName(dir);
                if (!int.TryParse(name, out int id)) continue;
                if (File.Exists(Path.Combine(dir, name + ".bytes"))) ids.Add(id);
            }
            ids.Sort();
            return ids;
        }

        /// <summary>盘点一张地图的转换进度(解析 .bytes 拿 mapResId + 数源/产物瓦片)。</summary>
        public static MapStat Inspect(int sceneId)
        {
            var stat = new MapStat { SceneId = sceneId, MapResId = sceneId };
            SceneMapData mapData = ResolveBytes(sceneId, ref stat);

            string srcTile = Path.Combine(SrcMapRoot, stat.MapResId.ToString(), "tile");
            List<string> tileNames = GetActiveTileNames(mapData, srcTile);
            stat.TileTotal = tileNames.Count;

            string dstTile = Path.Combine(GameResMapRoot, stat.MapResId.ToString(), "tile");
            stat.TileConverted = CountTiles(dstTile, ".jpg", tileNames);
            stat.PreviewConverted = File.Exists(Path.Combine(dstTile, stat.MapResId + ".jpg"));

            SceneMeta meta = GetSceneMeta(sceneId);
            stat.Name = meta.Name;
            stat.SceneType = meta.Type;
            stat.BirthX = meta.BirthX;
            stat.BirthY = meta.BirthY;
            // .bytes 缺尺寸时,用 config_scene 的尺寸兜底
            if (stat.MapWidth == 0 && meta.Width > 0) stat.MapWidth = meta.Width;
            if (stat.MapHeight == 0 && meta.Height > 0) stat.MapHeight = meta.Height;
            return stat;
        }

        // ---------- 场景元数据(config_scene.json:名字/类型/出生点) ----------

        private static Dictionary<int, SceneMeta> _sceneMeta;

        /// <summary>取一条场景元数据(名字/类型/出生点)。首次调用解析 config_scene.json 并缓存。</summary>
        public static SceneMeta GetSceneMeta(int sceneId)
        {
            EnsureSceneMeta();
            return _sceneMeta.TryGetValue(sceneId, out SceneMeta m) ? m : default;
        }

        public static string SceneName(int sceneId)
        {
            string n = GetSceneMeta(sceneId).Name;
            return string.IsNullOrEmpty(n) ? "" : n;
        }

        /// <summary>重新加载场景元数据(改了 yu_client 路径/配置后用)。</summary>
        public static void ReloadSceneMeta() => _sceneMeta = null;

        private static void EnsureSceneMeta()
        {
            if (_sceneMeta != null) return;
            _sceneMeta = new Dictionary<int, SceneMeta>();
            string path = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "server", "config_scene.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[MapTileConverter] 找不到 config_scene.json(地图名将为空): " + path);
                return;
            }
            try
            {
                JObject cfg = JObject.Parse(File.ReadAllText(path));
                foreach (KeyValuePair<string, JToken> kv in cfg)
                {
                    if (!(kv.Value is JObject row)) continue;
                    int id = row.Value<int?>("id") ?? (int.TryParse(kv.Key, out int k) ? k : 0);
                    if (id == 0) continue;
                    _sceneMeta[id] = new SceneMeta
                    {
                        Found = true,
                        Name = row.Value<string>("name") ?? "",
                        Type = row.Value<int?>("type") ?? 0,
                        BirthX = row.Value<int?>("x") ?? 0,
                        BirthY = row.Value<int?>("y") ?? 0,
                        Width = row.Value<int?>("width") ?? 0,
                        Height = row.Value<int?>("height") ?? 0,
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MapTileConverter] 解析 config_scene.json 失败: " + e.Message);
            }
        }

        /// <summary>场景类型中文(对标 electron MapEditor.formatSceneType)。</summary>
        public static string SceneTypeLabel(int type)
        {
            switch (type)
            {
                case 0: return "主城";
                case 1: return "野外";
                case 2: return "副本/幻境";
                case 3: return "Boss场景";
                case 7: return "巅峰竞技";
                case 8: return "公会争霸";
                case 40: return "主线本";
                default: return "类型" + type;
            }
        }

        /// <summary>
        /// 转换一张地图:拷 .bytes + 底图 + 全部瓦片到 GameRes。onProgress 返回 true 表示用户取消。
        /// 已存在的 .jpg 跳过(增量,重复点不浪费)。返回最新盘点。
        /// </summary>
        public static MapStat Convert(int sceneId, Func<float, string, bool> onProgress,
            out int copied, out int skipped, out bool canceled)
        {
            return CopyMapAssets(sceneId, false, onProgress, out copied, out skipped, out canceled);
        }

        /// <summary>
        /// 从老客户端强制更新一张地图:覆盖 .bytes、底图和 .bytes 声明的有效瓦片。
        /// 只改资源文件本身,不删除/改写 Unity .meta,所以已有 GUID 与 Addressables 引用保持稳定。
        /// </summary>
        public static MapStat Update(int sceneId, Func<float, string, bool> onProgress,
            out int updated, out int skipped, out bool canceled)
        {
            return CopyMapAssets(sceneId, true, onProgress, out updated, out skipped, out canceled);
        }

        private static MapStat CopyMapAssets(int sceneId, bool overwrite,
            Func<float, string, bool> onProgress, out int copied, out int skipped, out bool canceled)
        {
            copied = 0;
            skipped = 0;
            canceled = false;

            MapStat stat = Inspect(sceneId);
            string mapResId = stat.MapResId.ToString();

            // 1) 场景 .bytes(按 sceneId)
            string bytesRel = sceneId + "/" + sceneId + ".bytes";
            CopyBytesAsset(Path.Combine(SrcMapRoot, bytesRel), "resource/game/scene/map/" + bytesRel, overwrite);

            // 2) 瓦片 + 底图(按 mapResId)。只处理 .bytes 声明的有效瓦片,忽略源目录历史残片。
            string srcTile = Path.Combine(SrcMapRoot, mapResId, "tile");
            if (!Directory.Exists(srcTile))
            {
                AssetDatabase.Refresh();
                return Inspect(sceneId);
            }

            var dataStat = new MapStat { SceneId = sceneId, MapResId = sceneId };
            SceneMapData mapData = ResolveBytes(sceneId, ref dataStat);
            List<string> tileNames = GetActiveTileNames(mapData, srcTile);
            var assets = new List<(string src, string name)>();
            assets.Add((Path.Combine(srcTile, mapResId + ".jpg"), mapResId));
            foreach (string tileName in tileNames)
                assets.Add((Path.Combine(srcTile, tileName + ".jxr"), tileName));

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    string src = assets[i].src;
                    string name = assets[i].name;

                    if (onProgress != null &&
                        onProgress((float)(i + 1) / assets.Count,
                            Path.GetFileName(src) + "  (" + (i + 1) + "/" + assets.Count + ")"))
                    {
                        canceled = true;
                        break;
                    }

                    if (!File.Exists(src))
                    {
                        skipped++;
                        continue;
                    }

                    string dstRel = "resource/game/scene/map/" + mapResId + "/tile/" + name + ".jpg";
                    string dstAbs = Path.Combine(Application.dataPath, "GameRes", dstRel.Replace('/', Path.DirectorySeparatorChar));
                    if (!overwrite && File.Exists(dstAbs)) { skipped++; continue; } // 转换模式:已有即跳过

                    byte[] bytes = File.ReadAllBytes(src);
                    if (bytes.Length < 3 || bytes[0] != 0xFF || bytes[1] != 0xD8 || bytes[2] != 0xFF)
                    {
                        skipped++; // 非 JPEG(真 .ktx 等),离线转换链不处理
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(dstAbs));
                    File.WriteAllBytes(dstAbs, bytes);
                    copied++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(); // 一次性导入(SpriteImporter 后处理器自动设 Sprite)
            }

            return Inspect(sceneId);
        }

        /// <summary>产物目录(供"定位/删除"):Assets 相对路径。</summary>
        public static string GameResMapDir(int mapResId) =>
            "Assets/GameRes/resource/game/scene/map/" + mapResId;

        /// <summary>老客户端地图源目录(供更新确认框显示)。</summary>
        public static string SourceMapDir(int mapResId) =>
            Path.Combine(SrcMapRoot, mapResId.ToString());

        private static SceneMapData ResolveBytes(int sceneId, ref MapStat stat)
        {
            string bytesPath = Path.Combine(SrcMapRoot, sceneId.ToString(), sceneId + ".bytes");
            if (!File.Exists(bytesPath)) return null;
            stat.HasBytes = true;
            try
            {
                SceneMapData d = MapDataParser.Parse(sceneId, File.ReadAllBytes(bytesPath));
                if (d != null)
                {
                    if (d.MapResId > 0) stat.MapResId = d.MapResId;
                    stat.MapWidth = d.MapWidth;
                    stat.MapHeight = d.MapHeight;
                    stat.TileSize = d.TileSize;
                }
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MapTileConverter] 解析 .bytes 失败 sceneId=" + sceneId + ": " + e.Message);
                return null;
            }
        }

        private static void CopyBytesAsset(string srcAbs, string rel, bool overwrite)
        {
            if (!File.Exists(srcAbs)) return;
            string dstAbs = Path.Combine(Application.dataPath, "GameRes", rel.Replace('/', Path.DirectorySeparatorChar));
            if (!overwrite && File.Exists(dstAbs)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(dstAbs));
            File.Copy(srcAbs, dstAbs, true);
            AssetDatabase.ImportAsset("Assets/GameRes/" + rel);
        }

        private static List<string> GetActiveTileNames(SceneMapData data, string srcTile)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (data?.Tiles != null)
            {
                foreach (SceneMapTileCoord tile in data.Tiles)
                    names.Add(tile.Y.ToString("00") + tile.X.ToString("00"));
            }

            // 没有可解析 .bytes 时保留旧工具的目录扫描兜底。
            if (names.Count == 0 && Directory.Exists(srcTile))
            {
                foreach (string f in Directory.EnumerateFiles(srcTile, "*.jxr"))
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (IsTileName(name)) names.Add(name);
                }
            }

            var result = new List<string>(names);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static int CountTiles(string dir, string ext)
        {
            if (!Directory.Exists(dir)) return 0;
            int n = 0;
            foreach (string f in Directory.EnumerateFiles(dir, "*" + ext))
            {
                if (IsTileName(Path.GetFileNameWithoutExtension(f))) n++;
            }
            return n;
        }

        private static int CountTiles(string dir, string ext, IReadOnlyList<string> tileNames)
        {
            if (!Directory.Exists(dir) || tileNames == null) return 0;
            int n = 0;
            foreach (string tileName in tileNames)
            {
                if (File.Exists(Path.Combine(dir, tileName + ext))) n++;
            }
            return n;
        }

        /// <summary>瓦片名 = 两位行 + 两位列(RRCC,4 位纯数字)。底图名是 mapResId,不算瓦片。</summary>
        private static bool IsTileName(string nameNoExt)
        {
            if (string.IsNullOrEmpty(nameNoExt) || nameNoExt.Length != 4) return false;
            foreach (char c in nameNoExt) if (c < '0' || c > '9') return false;
            return true;
        }
    }
}
