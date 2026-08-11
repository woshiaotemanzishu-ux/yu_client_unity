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
        public byte LastStrengthPosition { get; private set; }
        public ushort LastStrengthLevel { get; private set; }
        public uint LastPillTypeId { get; private set; }
        public ushort LastPillNum { get; private set; }
        public uint LastPillCode { get; private set; }
        public uint ReplaceAckVersion { get; private set; }

        public event Action Changed;

        public void ReplaceError(uint errorCode, string errorArgs)
        {
            HasError = true;
            LastErrorCode = errorCode;
            LastErrorArgs = errorArgs;
            Changed?.Invoke();
        }

        public void ReplaceRating(uint totalRating)
        {
            HasRating = true;
            TotalRating = totalRating;
            Changed?.Invoke();
        }

        public void ReplaceEquipSnapshot(List<EquipEntry> entries)
        {
            HasEquipSnapshot = true;
            EquipSnapshot = (entries ?? new List<EquipEntry>()).AsReadOnly();
            Changed?.Invoke();
        }

        public void ReplacePillSnapshot(List<PillEntry> entries)
        {
            HasPillSnapshot = true;
            PillSnapshot = (entries ?? new List<PillEntry>()).AsReadOnly();
            Changed?.Invoke();
        }

        public void ReplaceSuitPreview(List<SuitEntry> entries, uint code)
        {
            HasSuitPreview = true;
            SuitPreview = (entries ?? new List<SuitEntry>()).AsReadOnly();
            SuitPreviewCode = code;
            Changed?.Invoke();
        }

        public void ReplaceSuitSnapshot(List<SuitEntry> entries)
        {
            HasSuitSnapshot = true;
            SuitSnapshot = (entries ?? new List<SuitEntry>()).AsReadOnly();
            Changed?.Invoke();
        }

        public void ApplyStrength(byte position, ushort strength)
        {
            LastStrengthPosition = position;
            LastStrengthLevel = strength;
            Changed?.Invoke();
        }

        public void ApplyReplaceAck()
        {
            ReplaceAckVersion++;
            Changed?.Invoke();
        }

        public void ApplyPillUse(uint goodsTypeId, ushort num, uint code)
        {
            LastPillTypeId = goodsTypeId;
            LastPillNum = num;
            LastPillCode = code;
            Changed?.Invoke();
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
            LastStrengthPosition = 0;
            LastStrengthLevel = 0;
            LastPillTypeId = 0;
            LastPillNum = 0;
            LastPillCode = 0;
            ReplaceAckVersion = 0;
            Changed?.Invoke();
        }
    }
}
