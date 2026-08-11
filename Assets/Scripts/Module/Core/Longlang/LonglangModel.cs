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
        public byte LastStrengthPosition { get; private set; }
        public ushort LastStrengthLevel { get; private set; }
        public uint ReplaceAckVersion { get; private set; }
        public uint UnloadAckVersion { get; private set; }
        public event Action Changed;

        public bool HasError => LastError != null;
        public bool HasEquipments => Equipments != null;
        public bool HasRating => Rating != null;
        public bool HasPreview => LastPreview != null;
        public bool HasSuitInfo => SuitInfo != null;

        public void ReplaceError(uint code, string args) { LastError = new ErrorSnapshot(code, args); Changed?.Invoke(); }
        public void ReplaceEquipments(IReadOnlyList<Equipment> items) { Equipments = new EquipmentSnapshot(items); Changed?.Invoke(); }
        public void ReplaceRating(uint rating) { Rating = new RatingSnapshot(rating); Changed?.Invoke(); }
        public void ReplacePreview(IReadOnlyList<SuitEntry> suits, uint code)
        {
            LastPreview = new PreviewSnapshot(suits, code);
            Changed?.Invoke();
        }
        public void ReplaceSuitInfo(IReadOnlyList<SuitEntry> suits) { SuitInfo = new SuitInfoSnapshot(suits); Changed?.Invoke(); }

        public void ApplyStrength(byte position, ushort level)
        {
            LastStrengthPosition = position;
            LastStrengthLevel = level;
            Changed?.Invoke();
        }

        public void ApplyReplaceAck() { ReplaceAckVersion++; Changed?.Invoke(); }
        public void ApplyUnloadAck() { UnloadAckVersion++; Changed?.Invoke(); }

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
            LastStrengthPosition = 0;
            LastStrengthLevel = 0;
            ReplaceAckVersion = 0;
            UnloadAckVersion = 0;
            Changed?.Invoke();
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
