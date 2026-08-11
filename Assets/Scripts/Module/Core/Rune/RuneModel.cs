using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>九霄劫魄权威运行态。协议快照与符文背包均在写入时复制，View 只读消费。</summary>
    public sealed class RuneModel
    {
        public static readonly RuneModel Instance = new RuneModel();
        private RuneModel() { }

        public sealed class RuneAttrVo
        {
            public int AttrId { get; }
            public long AttrNum { get; }
            public int AwakeLv { get; }
            public long AwakeExp { get; }
            public long NextPower { get; }
            public long CurPower { get; }

            public RuneAttrVo(int attrId, long attrNum, int awakeLv, long awakeExp,
                long nextPower, long curPower)
            {
                AttrId = attrId;
                AttrNum = attrNum;
                AwakeLv = awakeLv;
                AwakeExp = awakeExp;
                NextPower = nextPower;
                CurPower = curPower;
            }
        }

        public sealed class SlotVo
        {
            public int PosId;
            public bool IfOpen;
            public long GoodsId;
            public int GoodsTypeId;
            public int Color;
            public int Lv;
            public IReadOnlyList<RuneAttrVo> Attrs = Array.Empty<RuneAttrVo>();

            public bool IsWorn => GoodsId > 0;
        }

        public sealed class ObjectEntry
        {
            public byte Style { get; }
            public uint TypeId { get; }
            public uint Count { get; }

            public ObjectEntry(byte style, uint typeId, uint count)
            {
                Style = style;
                TypeId = typeId;
                Count = count;
            }
        }

        public sealed class DungeonLevelSnapshot
        {
            public ushort Level { get; }
            public DungeonLevelSnapshot(ushort level) => Level = level;
        }

        public sealed class ComposePreviewSnapshot
        {
            public uint Code { get; }
            public uint Level { get; }

            public ComposePreviewSnapshot(uint code, uint level)
            {
                Code = code;
                Level = level;
            }
        }

        public sealed class DecomposePreviewSnapshot
        {
            public uint Code { get; }
            public ulong Experience { get; }
            public IReadOnlyList<ObjectEntry> Result { get; }

            public DecomposePreviewSnapshot(uint code, ulong experience, List<ObjectEntry> result)
            {
                Code = code;
                Experience = experience;
                Result = Freeze(result);
            }
        }

        public sealed class DismantlePreviewSnapshot
        {
            public uint Code { get; }
            public IReadOnlyList<ObjectEntry> Result { get; }

            public DismantlePreviewSnapshot(uint code, List<ObjectEntry> result)
            {
                Code = code;
                Result = Freeze(result);
            }
        }

        public readonly struct BagExtraAttrVo
        {
            public int Color { get; }
            public int AttrTypeId { get; }
            public int AttrId { get; }
            public long AttrVal { get; }
            public long PlusInterval { get; }
            public long PlusUnit { get; }

            public BagExtraAttrVo(int color, int attrTypeId, int attrId, long attrVal,
                long plusInterval, long plusUnit)
            {
                Color = color;
                AttrTypeId = attrTypeId;
                AttrId = attrId;
                AttrVal = attrVal;
                PlusInterval = plusInterval;
                PlusUnit = plusUnit;
            }
        }

        public readonly struct BagAdditionAttrVo
        {
            public int AttrType { get; }
            public long AttrValue { get; }
            public int Color { get; }
            public long CombatPower { get; }

            public BagAdditionAttrVo(int attrType, long attrValue, int color, long combatPower)
            {
                AttrType = attrType;
                AttrValue = attrValue;
                Color = color;
                CombatPower = combatPower;
            }
        }

        public readonly struct BagAwakeAttrVo
        {
            public int AttrType { get; }
            public int AwakeLv { get; }
            public long AwakeExp { get; }

            public BagAwakeAttrVo(int attrType, int awakeLv, long awakeExp)
            {
                AttrType = attrType;
                AwakeLv = awakeLv;
                AwakeExp = awakeExp;
            }
        }

        /// <summary>
        /// rune_bag(pos=11) 的不可变保真 DTO。构造时复制三组嵌套集合，不反向持有 BagGoods/List。
        /// skill_id/skill_lv 不在当前 BagController 权威解析结果中，本轮明确不伪造。
        /// </summary>
        public sealed class BagGoodsVo
        {
            public long GoodsId { get; }
            public int TypeId { get; }
            public long Num { get; }
            public int Level { get; }
            public int Color { get; }
            public int Bind { get; }
            public int Cell { get; }
            public int Stren { get; }
            public long Rating { get; }
            public long OverallRating { get; }
            public long CombatPower { get; }
            public uint ExpireTime { get; }
            public int EquipStage { get; }
            public int EquipStar { get; }
            public IReadOnlyList<BagExtraAttrVo> ExtraAttrs { get; }
            public IReadOnlyList<BagAdditionAttrVo> AdditionAttrs { get; }
            public IReadOnlyList<BagAwakeAttrVo> AwakeList { get; }

            public BagGoodsVo(long goodsId, int typeId, long num, int level, int color,
                int bind, int cell, int stren, long rating, long overallRating,
                long combatPower, uint expireTime, int equipStage, int equipStar,
                IEnumerable<BagExtraAttrVo> extraAttrs,
                IEnumerable<BagAdditionAttrVo> additionAttrs,
                IEnumerable<BagAwakeAttrVo> awakeList)
            {
                GoodsId = goodsId;
                TypeId = typeId;
                Num = num;
                Level = level;
                Color = color;
                Bind = bind;
                Cell = cell;
                Stren = stren;
                Rating = rating;
                OverallRating = overallRating;
                CombatPower = combatPower;
                ExpireTime = expireTime;
                EquipStage = equipStage;
                EquipStar = equipStar;
                ExtraAttrs = Freeze(extraAttrs);
                AdditionAttrs = Freeze(additionAttrs);
                AwakeList = Freeze(awakeList);
            }

        }

        public readonly List<SlotVo> Slots = new List<SlotVo>();
        public readonly List<BagGoodsVo> RuneBagGoods = new List<BagGoodsVo>();

        public int RunePoint { get; private set; }
        public int RuneChip { get; private set; }
        public int SkillLv { get; private set; }
        public long SumPower { get; private set; }
        public bool HasData { get; private set; }
        public bool HasRuneBag { get; private set; }

        public DungeonLevelSnapshot DungeonLevel { get; private set; }
        public ComposePreviewSnapshot ComposePreview { get; private set; }
        public DecomposePreviewSnapshot DecomposePreview { get; private set; }
        public DismantlePreviewSnapshot DismantlePreview { get; private set; }

        public event Action Changed;
        public event Action<bool> BagChanged;
        public event Action<long> UpgradeSucceeded;
        public event Action<int> WearSucceeded;
        public event Action ExchangeSucceeded;

        public SlotVo GetSlot(int posId) => Slots.Find(slot => slot.PosId == posId);

        public void Apply16700(int runePoint, int runeChip, int skillLv,
            List<SlotVo> slots, long sumPower)
        {
            RunePoint = runePoint;
            RuneChip = runeChip;
            SkillLv = skillLv;
            SumPower = sumPower;
            Slots.Clear();
            if (slots != null) Slots.AddRange(slots);
            HasData = true;
            Changed?.Invoke();
        }

        // 兼容既有CliVerify；新协议路径必须显式传 skillLv。
        public void Apply16700(int runePoint, int runeChip, List<SlotVo> slots, long sumPower) =>
            Apply16700(runePoint, runeChip, SkillLv, slots, sumPower);

        public void Apply16701(int posId, long newGoodsId, int newGoodsTypeId)
        {
            SlotVo slot = GetSlot(posId);
            if (slot == null) return;
            BagGoodsVo bag = RuneBagGoods.Find(item => item.GoodsId == newGoodsId);
            slot.GoodsId = newGoodsId;
            slot.GoodsTypeId = newGoodsTypeId;
            if (bag != null)
            {
                slot.GoodsTypeId = bag.TypeId > 0 ? bag.TypeId : newGoodsTypeId;
                slot.Color = bag.Color;
                slot.Lv = bag.Level;
                var attrs = new List<RuneAttrVo>(bag.AwakeList.Count);
                for (int i = 0; i < bag.AwakeList.Count; i++)
                {
                    BagAwakeAttrVo awake = bag.AwakeList[i];
                    long attrNum = 0;
                    for (int j = 0; j < bag.ExtraAttrs.Count; j++)
                    {
                        BagExtraAttrVo extra = bag.ExtraAttrs[j];
                        if (extra.AttrId != awake.AttrType) continue;
                        attrNum = extra.AttrVal;
                        break;
                    }
                    attrs.Add(new RuneAttrVo(awake.AttrType, attrNum, awake.AwakeLv,
                        awake.AwakeExp, 0, 0));
                }
                slot.Attrs = Array.AsReadOnly(attrs.ToArray());
            }
            Changed?.Invoke();
            WearSucceeded?.Invoke(posId);
        }

        public void ApplyUpgradeSuccess(long goodsId, int runePoint)
        {
            RunePoint = runePoint;
            SlotVo slot = Slots.Find(value => value.GoodsId == goodsId);
            if (slot != null) slot.Lv++;
            Changed?.Invoke();
            UpgradeSucceeded?.Invoke(goodsId);
        }

        public void ApplyExchangeSuccess(int runeChip)
        {
            RuneChip = Math.Max(0, runeChip);
            Changed?.Invoke();
            ExchangeSucceeded?.Invoke();
        }

        public void ReplaceDungeonLevel(ushort level) =>
            DungeonLevel = new DungeonLevelSnapshot(level);

        public void ReplaceComposePreview(uint code, uint level) =>
            ComposePreview = new ComposePreviewSnapshot(code, level);

        public void ReplaceDecomposePreview(uint code, ulong experience, List<ObjectEntry> result) =>
            DecomposePreview = new DecomposePreviewSnapshot(code, experience, result);

        public void ReplaceDismantlePreview(uint code, List<ObjectEntry> result) =>
            DismantlePreview = new DismantlePreviewSnapshot(code, result);

        public void ClearReadSnapshots()
        {
            DungeonLevel = null;
            ComposePreview = null;
            DecomposePreview = null;
            DismantlePreview = null;
        }

        public void SetRuneBag(IEnumerable<BagGoodsVo> values)
        {
            RuneBagGoods.Clear();
            if (values != null) RuneBagGoods.AddRange(values);
            HasRuneBag = true;
            BagChanged?.Invoke(true);
            Changed?.Invoke();
        }

        public void UpsertRuneBag(BagGoodsVo value)
        {
            if (value == null || value.GoodsId <= 0) return;
            int index = RuneBagGoods.FindIndex(item => item.GoodsId == value.GoodsId);
            if (value.Num <= 0)
            {
                if (index >= 0) RuneBagGoods.RemoveAt(index);
            }
            else if (index >= 0) RuneBagGoods[index] = value;
            else RuneBagGoods.Add(value);
            HasRuneBag = true;
            BagChanged?.Invoke(false);
            Changed?.Invoke();
        }

        public void UpdateRuneBagNum(long goodsId, int typeId, long num)
        {
            int index = RuneBagGoods.FindIndex(item => item.GoodsId == goodsId);
            if (num <= 0)
            {
                if (index >= 0) RuneBagGoods.RemoveAt(index);
            }
            else if (index >= 0)
            {
                BagGoodsVo old = RuneBagGoods[index];
                RuneBagGoods[index] = new BagGoodsVo(old.GoodsId,
                    typeId > 0 ? typeId : old.TypeId, num, old.Level, old.Color,
                    old.Bind, old.Cell, old.Stren, old.Rating, old.OverallRating,
                    old.CombatPower, old.ExpireTime, old.EquipStage, old.EquipStar,
                    old.ExtraAttrs, old.AdditionAttrs, old.AwakeList);
            }
            HasRuneBag = true;
            BagChanged?.Invoke(false);
            Changed?.Invoke();
        }

        public void Clear()
        {
            Slots.Clear();
            RuneBagGoods.Clear();
            RunePoint = 0;
            RuneChip = 0;
            SkillLv = 0;
            SumPower = 0;
            HasData = false;
            HasRuneBag = false;
            ClearReadSnapshots();
        }

        private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source) =>
            Array.AsReadOnly(source == null ? Array.Empty<T>() : new List<T>(source).ToArray());
    }
}
