using System.Collections.Generic;
namespace Shenxiao.Module.Core.Demon
{
    /// <summary>18301 原始服务端快照；不承担配置、战力或界面派生。</summary>
    public sealed class DemonModel
    {
        public sealed class Skill { public uint SkillId { get; } public ushort SkillLevel { get; } public uint Process { get; } public byte IsActive { get; } public Skill(uint id, ushort level, uint process, byte active) { SkillId = id; SkillLevel = level; Process = process; IsActive = active; } }
        public sealed class SlotSkill { public uint SkillId { get; } public ushort SkillLevel { get; } public byte Slot { get; } public byte Quality { get; } public ushort Sort { get; } public SlotSkill(uint id, ushort level, byte slot, byte quality, ushort sort) { SkillId = id; SkillLevel = level; Slot = slot; Quality = quality; Sort = sort; } }
        public sealed class Entry
        {
            public uint DemonId { get; } public ushort Level { get; } public uint Experience { get; } public byte Star { get; } public byte SlotNumber { get; } public IReadOnlyList<Skill> Skills { get; } public IReadOnlyList<SlotSkill> SlotSkills { get; }
            public Entry(uint id, ushort level, uint exp, byte star, byte slotNumber, List<Skill> skills, List<SlotSkill> slotSkills) { DemonId = id; Level = level; Experience = exp; Star = star; SlotNumber = slotNumber; Skills = (skills ?? new List<Skill>()).AsReadOnly(); SlotSkills = (slotSkills ?? new List<SlotSkill>()).AsReadOnly(); }
        }
        public sealed class ObjectEntry { public byte Style { get; } public uint TypeId { get; } public uint Count { get; } public ObjectEntry(byte style, uint typeId, uint count) { Style = style; TypeId = typeId; Count = count; } }
        public sealed class TalentShopEntry
        {
            public uint Id { get; } public uint GoodsId { get; } public uint Price { get; } public ushort Num { get; } public ushort CostNum { get; } public byte Discount { get; } public ushort CanBuyNum { get; } public ushort BuyNum { get; }
            public TalentShopEntry(uint id, uint goodsId, uint price, ushort num, ushort costNum, byte discount, ushort canBuyNum, ushort buyNum) { Id = id; GoodsId = goodsId; Price = price; Num = num; CostNum = costNum; Discount = discount; CanBuyNum = canBuyNum; BuyNum = buyNum; }
        }
        public static readonly DemonModel Instance = new DemonModel(); private readonly List<Entry> _demons = new List<Entry>(); private readonly List<uint> _fetters = new List<uint>(); private readonly List<byte> _paintings = new List<byte>(); private readonly List<ObjectEntry> _talentShopCost = new List<ObjectEntry>(); private readonly List<TalentShopEntry> _talentShop = new List<TalentShopEntry>(); private readonly IReadOnlyList<Entry> _readOnlyDemons; private readonly IReadOnlyList<uint> _readOnlyFetters; private readonly IReadOnlyList<byte> _readOnlyPaintings; private readonly IReadOnlyList<ObjectEntry> _readOnlyTalentShopCost; private readonly IReadOnlyList<TalentShopEntry> _readOnlyTalentShop; private DemonModel() { _readOnlyDemons = _demons.AsReadOnly(); _readOnlyFetters = _fetters.AsReadOnly(); _readOnlyPaintings = _paintings.AsReadOnly(); _readOnlyTalentShopCost = _talentShopCost.AsReadOnly(); _readOnlyTalentShop = _talentShop.AsReadOnly(); }
        public byte OpenState { get; private set; } public bool HasData { get; private set; } public bool HasFettersData { get; private set; } public bool HasPaintingsData { get; private set; } public bool HasBlessingData { get; private set; } public uint BlessingValue { get; private set; } public bool HasTalentShopSnapshot { get; private set; } public uint TalentShopRefreshTime { get; private set; } public ushort TalentShopRefreshNum { get; private set; } public IReadOnlyList<Entry> Demons => _readOnlyDemons; public IReadOnlyList<uint> Fetters => _readOnlyFetters; public IReadOnlyList<byte> Paintings => _readOnlyPaintings; public IReadOnlyList<ObjectEntry> TalentShopCost => _readOnlyTalentShopCost; public IReadOnlyList<TalentShopEntry> TalentShop => _readOnlyTalentShop;
        public bool TryGet(uint demonId, out Entry entry) { for (int i = 0; i < _demons.Count; i++) if (_demons[i].DemonId == demonId) { entry = _demons[i]; return true; } entry = null; return false; }
        public bool HasFetter(uint fetterId) { for (int i = 0; i < _fetters.Count; i++) if (_fetters[i] == fetterId) return true; return false; }
        public bool HasPainting(byte paintingId) { for (int i = 0; i < _paintings.Count; i++) if (_paintings[i] == paintingId) return true; return false; }
        public void Replace(byte openState, List<Entry> demons) { OpenState = openState; _demons.Clear(); if (demons != null) _demons.AddRange(demons); HasData = true; }
        public void ReplaceFetters(List<uint> fetters) { _fetters.Clear(); if (fetters != null) { var seen = new HashSet<uint>(); for (int i = 0; i < fetters.Count; i++) if (seen.Add(fetters[i])) _fetters.Add(fetters[i]); } HasFettersData = true; }
        public void ReplacePaintings(List<byte> paintings) { _paintings.Clear(); if (paintings != null) { var seen = new HashSet<byte>(); for (int i = 0; i < paintings.Count; i++) if (seen.Add(paintings[i])) _paintings.Add(paintings[i]); } HasPaintingsData = true; }
        public void ReplaceBlessing(uint value) { BlessingValue = value; HasBlessingData = true; }
        public void ReplaceTalentShop(uint refreshTime, ushort refreshNum, List<ObjectEntry> cost, List<TalentShopEntry> shop) { TalentShopRefreshTime = refreshTime; TalentShopRefreshNum = refreshNum; _talentShopCost.Clear(); _talentShop.Clear(); if (cost != null) _talentShopCost.AddRange(cost); if (shop != null) _talentShop.AddRange(shop); HasTalentShopSnapshot = true; }
        public void Reset() { OpenState = 0; _demons.Clear(); _fetters.Clear(); _paintings.Clear(); _talentShopCost.Clear(); _talentShop.Clear(); BlessingValue = 0; TalentShopRefreshTime = 0; TalentShopRefreshNum = 0; HasData = false; HasFettersData = false; HasPaintingsData = false; HasBlessingData = false; HasTalentShopSnapshot = false; }
    }
}
