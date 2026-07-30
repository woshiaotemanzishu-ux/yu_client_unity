using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.HolySeal
{
    public sealed class HolySealModel
    {
        public sealed class EquipEntry
        {
            public EquipEntry(byte pos, ulong goodsId, ushort strength)
            {
                Pos = pos;
                GoodsId = goodsId;
                Strength = strength;
            }

            public byte Pos { get; }
            public ulong GoodsId { get; }
            public ushort Strength { get; }
        }

        public sealed class PillEntry
        {
            public PillEntry(uint goodsTypeId, ushort num, ushort limit)
            {
                GoodsTypeId = goodsTypeId;
                Num = num;
                Limit = limit;
            }

            public uint GoodsTypeId { get; }
            public ushort Num { get; }
            public ushort Limit { get; }
        }

        public sealed class SuitEntry
        {
            public SuitEntry(uint suitId, ushort num)
            {
                SuitId = suitId;
                Num = num;
            }

            public uint SuitId { get; }
            public ushort Num { get; }
        }

        public static readonly HolySealModel Instance = new HolySealModel();

        private HolySealModel() { }

        public bool HasError { get; private set; }
        public uint LastErrorCode { get; private set; }
        public string LastErrorArgs { get; private set; }
        public bool HasRating { get; private set; }
        public uint TotalRating { get; private set; }
        public bool HasEquipSnapshot { get; private set; }
        public IReadOnlyList<EquipEntry> EquipSnapshot { get; private set; } = Array.Empty<EquipEntry>();
        public bool HasPillSnapshot { get; private set; }
        public IReadOnlyList<PillEntry> PillSnapshot { get; private set; } = Array.Empty<PillEntry>();
        public bool HasSuitPreview { get; private set; }
        public IReadOnlyList<SuitEntry> SuitPreview { get; private set; } = Array.Empty<SuitEntry>();
        public uint SuitPreviewCode { get; private set; }
        public bool HasSuitSnapshot { get; private set; }
        public IReadOnlyList<SuitEntry> SuitSnapshot { get; private set; } = Array.Empty<SuitEntry>();

        public void ReplaceError(uint errorCode, string errorArgs)
        {
            HasError = true;
            LastErrorCode = errorCode;
            LastErrorArgs = errorArgs;
        }

        public void ReplaceRating(uint totalRating)
        {
            HasRating = true;
            TotalRating = totalRating;
        }

        public void ReplaceEquipSnapshot(List<EquipEntry> entries)
        {
            HasEquipSnapshot = true;
            EquipSnapshot = (entries ?? new List<EquipEntry>()).AsReadOnly();
        }

        public void ReplacePillSnapshot(List<PillEntry> entries)
        {
            HasPillSnapshot = true;
            PillSnapshot = (entries ?? new List<PillEntry>()).AsReadOnly();
        }

        public void ReplaceSuitPreview(List<SuitEntry> entries, uint code)
        {
            HasSuitPreview = true;
            SuitPreview = (entries ?? new List<SuitEntry>()).AsReadOnly();
            SuitPreviewCode = code;
        }

        public void ReplaceSuitSnapshot(List<SuitEntry> entries)
        {
            HasSuitSnapshot = true;
            SuitSnapshot = (entries ?? new List<SuitEntry>()).AsReadOnly();
        }

        public void Reset()
        {
            HasError = false;
            LastErrorCode = 0;
            LastErrorArgs = null;
            HasRating = false;
            TotalRating = 0;
            HasEquipSnapshot = false;
            EquipSnapshot = Array.Empty<EquipEntry>();
            HasPillSnapshot = false;
            PillSnapshot = Array.Empty<PillEntry>();
            HasSuitPreview = false;
            SuitPreview = Array.Empty<SuitEntry>();
            SuitPreviewCode = 0;
            HasSuitSnapshot = false;
            SuitSnapshot = Array.Empty<SuitEntry>();
        }
    }
}
