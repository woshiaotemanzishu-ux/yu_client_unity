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

        // 解析结果缓存:一次切图会解析两遍(LegacyPreloadService 预热 + SceneMapLoader),大图上万格的
        // 双重循环 + 大数组分配没必要重复跑,主线程同步解析是切图帧的尖刺之一。
        // TextAsset.bytes 每次访问都返回新副本,不能按数组引用判同 → 按 sceneId+字节长度 记最近几张。
        private const int CACHE_MAX = 4;
        private static readonly List<(int sceneId, int byteLen, SceneMapData data)> _recent
            = new List<(int, int, SceneMapData)>();

        public static SceneMapData Parse(int sceneId, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            for (int i = 0; i < _recent.Count; i++)
            {
                if (_recent[i].sceneId != sceneId || _recent[i].byteLen != bytes.Length) continue;
                var hit = _recent[i];
                if (i > 0)
                {
                    _recent.RemoveAt(i);
                    _recent.Insert(0, hit);
                }
                return hit.data;
            }

            SceneMapData parsed = ParseCore(sceneId, bytes);
            _recent.Insert(0, (sceneId, bytes.Length, parsed));
            if (_recent.Count > CACHE_MAX) _recent.RemoveAt(_recent.Count - 1);
            return parsed;
        }

        private static SceneMapData ParseCore(int sceneId, byte[] bytes)
        {
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
