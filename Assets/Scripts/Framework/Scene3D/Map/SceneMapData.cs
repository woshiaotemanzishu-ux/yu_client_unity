using System.Collections.Generic;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>
    /// Parsed data from yu_client scene map .bytes.
    /// Keeps scene id and map resource id separate.
    /// </summary>
    public sealed class SceneMapData
    {
        /// <summary>逻辑格/真实像素比例,对标 SceneObj.LogicRealRatio2D(60,30)。</summary>
        public const int LogicRatioX = 60;
        public const int LogicRatioY = 30;

        /// <summary>寻路格阻挡位(对标 AstarManager ZONE_TYPE_BLOCK = 1)。</summary>
        private const int BlockBit = 1;

        /// <summary>安全区位(对标老端 AreaDataIndex.SafeType = 4;野外场景也有安全区格)。</summary>
        private const int SafeBit = 4;

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

        /// <summary>
        /// 真实像素坐标是否阻挡(对标 MapManager.IsBlockXY → AstarManager.GetZoneInfo 的静态寻路格判断):
        /// 先把像素坐标换算成逻辑格(/60,/30 向下取整),越界视为阻挡,否则取格值的阻挡位。
        /// 动态区域阻挡(DynamicBlock)属于后续场景对象生命周期范畴,本最小移动闭环不纳入。
        /// </summary>
        public bool IsBlockPixel(float realX, float realY)
        {
            int lx = (int)System.Math.Floor(realX / LogicRatioX);
            int ly = (int)System.Math.Floor(realY / LogicRatioY);
            if (lx < 0 || ly < 0 || lx >= GridColumns || ly >= GridRows) return true;
            if (WalkGrid == null || WalkGrid.Length == 0) return false;
            return (WalkGrid[lx, ly] & BlockBit) != 0;
        }

        /// <summary>
        /// 真实像素坐标是否处于安全区格(对标 SceneManager.IsSafePos → MapManager.IsAreaType SafeType):
        /// 换算逻辑格后取格值安全位;越界/无格数据视为非安全。
        /// </summary>
        public bool IsSafePixel(float realX, float realY)
        {
            int lx = (int)System.Math.Floor(realX / LogicRatioX);
            int ly = (int)System.Math.Floor(realY / LogicRatioY);
            if (lx < 0 || ly < 0 || lx >= GridColumns || ly >= GridRows) return false;
            if (WalkGrid == null || WalkGrid.Length == 0) return false;
            return (WalkGrid[lx, ly] & SafeBit) != 0;
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
