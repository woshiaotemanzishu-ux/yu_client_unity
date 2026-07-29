using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Longlang
{
    /// <summary>龙语622家族的原始只读状态；各协议切片互不交叉修改。</summary>
    public sealed class LonglangModel
    {
        public sealed class ErrorSnapshot
        {
            public uint Code { get; }
            public string Args { get; }

            public ErrorSnapshot(uint code, string args)
            {
                Code = code;
                Args = args ?? string.Empty;
            }
        }

        public sealed class Equipment
        {
            public byte Position { get; }
            public ulong GoodsId { get; }
            public ushort Strength { get; }

            public Equipment(byte position, ulong goodsId, ushort strength)
            {
                Position = position;
                GoodsId = goodsId;
                Strength = strength;
            }
        }

        public sealed class EquipmentSnapshot
        {
            public IReadOnlyList<Equipment> Items { get; }
            public EquipmentSnapshot(IReadOnlyList<Equipment> items) => Items = Freeze(items);
        }

        public sealed class RatingSnapshot
        {
            public uint Rating { get; }
            public RatingSnapshot(uint rating) => Rating = rating;
        }

        public sealed class SuitEntry
        {
            public uint SuitId { get; }
            public ushort Num { get; }

            public SuitEntry(uint suitId, ushort num)
            {
                SuitId = suitId;
                Num = num;
            }
        }

        public sealed class PreviewSnapshot
        {
            public IReadOnlyList<SuitEntry> Suits { get; }
            public uint Code { get; }
            public bool IsValid => Code == 1;

            public PreviewSnapshot(IReadOnlyList<SuitEntry> suits, uint code)
            {
                Suits = Freeze(suits);
                Code = code;
            }
        }

        public sealed class SuitInfoSnapshot
        {
            public IReadOnlyList<SuitEntry> Suits { get; }
            public SuitInfoSnapshot(IReadOnlyList<SuitEntry> suits) => Suits = Freeze(suits);
        }

        public static readonly LonglangModel Instance = new LonglangModel();
        private LonglangModel() { }

        public ErrorSnapshot LastError { get; private set; }
        public EquipmentSnapshot Equipments { get; private set; }
        public RatingSnapshot Rating { get; private set; }
        public PreviewSnapshot LastPreview { get; private set; }
        public SuitInfoSnapshot SuitInfo { get; private set; }

        public bool HasError => LastError != null;
        public bool HasEquipments => Equipments != null;
        public bool HasRating => Rating != null;
        public bool HasPreview => LastPreview != null;
        public bool HasSuitInfo => SuitInfo != null;

        public void ReplaceError(uint code, string args) => LastError = new ErrorSnapshot(code, args);
        public void ReplaceEquipments(IReadOnlyList<Equipment> items) => Equipments = new EquipmentSnapshot(items);
        public void ReplaceRating(uint rating) => Rating = new RatingSnapshot(rating);
        public void ReplacePreview(IReadOnlyList<SuitEntry> suits, uint code) =>
            LastPreview = new PreviewSnapshot(suits, code);
        public void ReplaceSuitInfo(IReadOnlyList<SuitEntry> suits) => SuitInfo = new SuitInfoSnapshot(suits);

        /// <summary>兼容老端字典语义：同一部位重复时，wire中最后一项生效。</summary>
        public bool TryGetEquipment(byte position, out Equipment equipment)
        {
            IReadOnlyList<Equipment> items = Equipments?.Items;
            if (items != null)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (items[i].Position != position) continue;
                    equipment = items[i];
                    return true;
                }
            }
            equipment = null;
            return false;
        }

        public void Reset()
        {
            LastError = null;
            Equipments = null;
            Rating = null;
            LastPreview = null;
            SuitInfo = null;
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

