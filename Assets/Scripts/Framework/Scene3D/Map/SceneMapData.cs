using System.Collections.Generic;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>
    /// Parsed data from yu_client scene map .bytes.
    /// Keeps scene id and map resource id separate.
    /// </summary>
    public sealed class SceneMapData
    {
        public int SceneId { get; private set; }
        public int MapResId { get; internal set; }
        public int TileSize { get; internal set; }
        public int MapHeight { get; internal set; }
        public int MapWidth { get; internal set; }
        public uint TileDataSize { get; internal set; }
        public uint MaskDataSize { get; internal set; }
        public int GridColumns { get; internal set; }
        public int GridRows { get; internal set; }
        public IReadOnlyList<SceneMapTileCoord> Tiles { get; internal set; }
        public sbyte[,] WalkGrid { get; internal set; }
        public IReadOnlyList<SceneMapArea> Areas { get; internal set; }

        public SceneMapData(int sceneId)
        {
            SceneId = sceneId;
            MapResId = sceneId;
            Tiles = new List<SceneMapTileCoord>();
            WalkGrid = new sbyte[0, 0];
            Areas = new List<SceneMapArea>();
        }
    }

    public struct SceneMapTileCoord
    {
        public uint X;
        public uint Y;

        public SceneMapTileCoord(uint x, uint y)
        {
            X = x;
            Y = y;
        }
    }

    public sealed class SceneMapArea
    {
        public uint Id;
        public uint Type;
        public IReadOnlyList<SceneMapGridCoord> Cells;

        public SceneMapArea()
        {
            Cells = new List<SceneMapGridCoord>();
        }
    }

    public struct SceneMapGridCoord
    {
        public uint X;
        public uint Y;

        public SceneMapGridCoord(uint x, uint y)
        {
            X = x;
            Y = y;
        }
    }
}
