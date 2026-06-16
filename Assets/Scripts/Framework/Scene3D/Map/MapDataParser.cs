using System;
using System.Collections.Generic;
using System.IO;

namespace Shenxiao.Framework.Scene3D.Map
{
    /// <summary>
    /// Parser for yu_client MapManager.ts MapElement.LoadData .bytes layout.
    /// This class is data-only and must not depend on Unity scene objects.
    /// </summary>
    public static class MapDataParser
    {
        private const int LOGIC_RATIO_X = 60;
        private const int LOGIC_RATIO_Y = 30;

        public static SceneMapData Parse(int sceneId, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                SceneMapData data = new SceneMapData(sceneId)
                {
                    TileSize = reader.ReadInt32(),
                    MapHeight = reader.ReadInt32(),
                    MapWidth = reader.ReadInt32()
                };

                int tileCount = reader.ReadInt32();
                if (tileCount < 0)
                {
                    throw new InvalidDataException("scene map tile count is negative");
                }

                data.TileDataSize = reader.ReadUInt32();
                data.MaskDataSize = reader.ReadUInt32();

                List<SceneMapTileCoord> tiles = new List<SceneMapTileCoord>(tileCount);
                for (int i = 0; i < tileCount; i++)
                {
                    tiles.Add(new SceneMapTileCoord(reader.ReadUInt32(), reader.ReadUInt32()));
                }
                data.Tiles = tiles;

                data.GridColumns = (data.MapWidth + LOGIC_RATIO_X - 1) / LOGIC_RATIO_X;
                data.GridRows = (data.MapHeight + LOGIC_RATIO_Y - 1) / LOGIC_RATIO_Y;
                data.WalkGrid = ReadWalkGrid(reader, data.GridColumns, data.GridRows);

                if (Remaining(stream) >= sizeof(uint))
                {
                    data.MapResId = unchecked((int)reader.ReadUInt32());
                }

                if (Remaining(stream) >= sizeof(uint))
                {
                    data.Areas = ReadAreas(reader, stream);
                }

                return data;
            }
        }

        private static sbyte[,] ReadWalkGrid(BinaryReader reader, int columns, int rows)
        {
            if (columns < 0 || rows < 0)
            {
                throw new InvalidDataException("scene map grid size is negative");
            }

            sbyte[,] grid = new sbyte[columns, rows];
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    grid[x, y] = reader.ReadSByte();
                }
            }
            return grid;
        }

        private static IReadOnlyList<SceneMapArea> ReadAreas(BinaryReader reader, Stream stream)
        {
            uint areaCount = reader.ReadUInt32();
            List<SceneMapArea> areas = new List<SceneMapArea>((int)Math.Min(areaCount, int.MaxValue));
            for (uint i = 0; i < areaCount; i++)
            {
                if (Remaining(stream) < sizeof(uint) * 3L)
                {
                    throw new InvalidDataException("scene map dynamic area header is truncated");
                }

                SceneMapArea area = new SceneMapArea
                {
                    Id = reader.ReadUInt32(),
                    Type = reader.ReadUInt32()
                };

                uint cellCount = reader.ReadUInt32();
                List<SceneMapGridCoord> cells = new List<SceneMapGridCoord>((int)Math.Min(cellCount, int.MaxValue));
                for (uint j = 0; j < cellCount; j++)
                {
                    if (Remaining(stream) < sizeof(uint) * 2L)
                    {
                        throw new InvalidDataException("scene map dynamic area cell is truncated");
                    }

                    cells.Add(new SceneMapGridCoord(reader.ReadUInt32(), reader.ReadUInt32()));
                }

                area.Cells = cells;
                areas.Add(area);
            }

            return areas;
        }

        private static long Remaining(Stream stream)
        {
            return stream.Length - stream.Position;
        }
    }
}
