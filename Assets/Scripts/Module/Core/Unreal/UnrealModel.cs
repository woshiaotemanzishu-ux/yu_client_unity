using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.Unreal
{
    /// <summary>幻饰 149xx 只读原始状态；所有列表保留服务端 wire 顺序和重复项。</summary>
    public sealed class UnrealModel
    {
        public sealed class ErrorSnapshot
        {
            public uint Code { get; }
            public string Message { get; }

            public ErrorSnapshot(uint code, string message)
            {
                Code = code;
                Message = message ?? string.Empty;
            }
        }

        public sealed class StrengthSnapshot
        {
            public uint Result { get; }
            public byte Cell { get; }
            public ushort Level { get; }
            public uint Point { get; }

            public StrengthSnapshot(uint result, byte cell, ushort level, uint point)
            {
                Result = result;
                Cell = cell;
                Level = level;
                Point = point;
            }
        }

        public sealed class PreviewAttr
        {
            public byte Color { get; }
            public byte TypeId { get; }
            public ushort AttrId { get; }
            public uint AttrValue { get; }
            public byte PlusInterval { get; }
            public uint PlusUnit { get; }

            public PreviewAttr(byte color, byte typeId, ushort attrId, uint attrValue,
                byte plusInterval, uint plusUnit)
            {
                Color = color;
                TypeId = typeId;
                AttrId = attrId;
                AttrValue = attrValue;
                PlusInterval = plusInterval;
                PlusUnit = plusUnit;
            }
        }

        public sealed class PreviewSnapshot
        {
            public ulong GoodsId { get; }
            public uint OverallRating { get; }
            public IReadOnlyList<PreviewAttr> Attrs { get; }

            public PreviewSnapshot(ulong goodsId, uint overallRating, IReadOnlyList<PreviewAttr> attrs)
            {
                GoodsId = goodsId;
                OverallRating = overallRating;
                Attrs = Freeze(attrs);
            }
        }

        public sealed class UnlockSnapshot
        {
            public IReadOnlyList<byte> Cells { get; }

            public UnlockSnapshot(IReadOnlyList<byte> cells)
            {
                Cells = Freeze(cells);
            }
        }

        public static readonly UnrealModel Instance = new UnrealModel();

        private readonly Dictionary<byte, StrengthSnapshot> _strengthByCell =
            new Dictionary<byte, StrengthSnapshot>();
        private readonly Dictionary<ulong, PreviewSnapshot> _stagePreviews =
            new Dictionary<ulong, PreviewSnapshot>();
        private readonly Dictionary<ulong, PreviewSnapshot> _decomposePreviews =
            new Dictionary<ulong, PreviewSnapshot>();
        private readonly IReadOnlyDictionary<byte, StrengthSnapshot> _strengthView;
        private readonly IReadOnlyDictionary<ulong, PreviewSnapshot> _stagePreviewView;
        private readonly IReadOnlyDictionary<ulong, PreviewSnapshot> _decomposePreviewView;

        private UnrealModel()
        {
            _strengthView = new ReadOnlyDictionary<byte, StrengthSnapshot>(_strengthByCell);
            _stagePreviewView = new ReadOnlyDictionary<ulong, PreviewSnapshot>(_stagePreviews);
            _decomposePreviewView = new ReadOnlyDictionary<ulong, PreviewSnapshot>(_decomposePreviews);
        }

        public ErrorSnapshot LastError { get; private set; }
        public UnlockSnapshot UnlockedCells { get; private set; }
        public IReadOnlyDictionary<byte, StrengthSnapshot> StrengthByCell => _strengthView;
        public IReadOnlyDictionary<ulong, PreviewSnapshot> StagePreviews => _stagePreviewView;
        public IReadOnlyDictionary<ulong, PreviewSnapshot> DecomposePreviews => _decomposePreviewView;
        public bool HasError => LastError != null;
        public bool HasUnlockedCells => UnlockedCells != null;

        public void ReplaceError(uint code, string message)
        {
            LastError = new ErrorSnapshot(code, message);
        }

        public void ReplaceStrength(uint result, byte cell, ushort level, uint point)
        {
            _strengthByCell[cell] = new StrengthSnapshot(result, cell, level, point);
        }

        public void ReplaceStagePreview(ulong goodsId, uint overallRating, IReadOnlyList<PreviewAttr> attrs)
        {
            _stagePreviews[goodsId] = new PreviewSnapshot(goodsId, overallRating, attrs);
        }

        public void ReplaceDecomposePreview(ulong goodsId, uint overallRating, IReadOnlyList<PreviewAttr> attrs)
        {
            _decomposePreviews[goodsId] = new PreviewSnapshot(goodsId, overallRating, attrs);
        }

        public void ReplaceUnlockedCells(IReadOnlyList<byte> cells)
        {
            UnlockedCells = new UnlockSnapshot(cells);
        }

        public bool TryGetStrength(byte cell, out StrengthSnapshot snapshot) =>
            _strengthByCell.TryGetValue(cell, out snapshot);

        public bool TryGetStagePreview(ulong goodsId, out PreviewSnapshot snapshot) =>
            _stagePreviews.TryGetValue(goodsId, out snapshot);

        public bool TryGetDecomposePreview(ulong goodsId, out PreviewSnapshot snapshot) =>
            _decomposePreviews.TryGetValue(goodsId, out snapshot);

        public void Reset()
        {
            LastError = null;
            UnlockedCells = null;
            _strengthByCell.Clear();
            _stagePreviews.Clear();
            _decomposePreviews.Clear();
        }

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }
}
