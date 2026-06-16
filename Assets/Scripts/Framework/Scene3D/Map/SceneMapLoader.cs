using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>
    /// Loads and parses scene map data. Protocol orchestration belongs to Module code.
    /// </summary>
    public static class SceneMapLoader
    {
        private static int _loadVersion;

        public static SceneMapData Current { get; private set; }

        public static async Task<SceneMapData> LoadAsync(int sceneId)
        {
            int version = ++_loadVersion;
            string key = GameResPath.GetSceneMapData(sceneId);
            TextAsset bytesAsset = await ResManager.LoadAsync<TextAsset>(key);
            if (version != _loadVersion)
            {
                return null;
            }

            if (bytesAsset == null)
            {
                GameLog.Error("SceneMap", "map bytes load failed: sceneId={0} key={1}", sceneId, key);
                return null;
            }

            SceneMapData data = MapDataParser.Parse(sceneId, bytesAsset.bytes);
            Current = data;
            GameLog.Info("SceneMap",
                "map data ready: sceneId={0} mapResId={1} size={2}x{3} tileSize={4} tiles={5}",
                data.SceneId, data.MapResId, data.MapWidth, data.MapHeight, data.TileSize, data.Tiles.Count);
            return data;
        }

        public static void Clear()
        {
            ++_loadVersion;
            Current = null;
        }
    }
}
