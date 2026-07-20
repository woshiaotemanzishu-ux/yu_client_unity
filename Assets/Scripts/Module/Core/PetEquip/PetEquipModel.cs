using System.Collections.Generic;

namespace Shenxiao.Module.Core.PetEquip
{
    /// <summary>侍魂装备协议状态，对标老端 PetEquipModel 的 type_id → PetEquipInfo 缓存。</summary>
    public sealed class PetEquipModel
    {
        public sealed class PetEquipItem
        {
            public int PosId;
            public int PosLevel;
            public int Stage;
            public int Star;
            public long PosPoint;
            public long GoodsId;
            public int GoodsTypeId;
        }

        public sealed class PetEquipInfo
        {
            public int TypeId;
            public long CombatPower;
            public List<PetEquipItem> Items;
        }

        public static readonly PetEquipModel Instance = new PetEquipModel();
        private readonly Dictionary<int, PetEquipInfo> _infoByType = new Dictionary<int, PetEquipInfo>();

        private PetEquipModel() { }

        public PetEquipInfo Get(int typeId)
            => _infoByType.TryGetValue(typeId, out PetEquipInfo info) ? info : null;

        public PetEquipItem GetByGoodsId(int typeId, long goodsId)
        {
            PetEquipInfo info = Get(typeId);
            if (info?.Items == null) return null;
            foreach (PetEquipItem item in info.Items)
            {
                if (item.GoodsId == goodsId) return item;
            }
            return null;
        }

        /// <summary>16014 成功回包按 type_id 整体替换。</summary>
        public void ApplyInfo(int typeId, long combatPower, List<PetEquipItem> items)
        {
            _infoByType[typeId] = new PetEquipInfo
            {
                TypeId = typeId,
                CombatPower = combatPower,
                Items = items ?? new List<PetEquipItem>()
            };
        }

        /// <summary>16016 成功回包只修改命中的 goods_id；返回是否命中及等级是否真变化。</summary>
        public bool TryApplyStrengthen(int typeId, long goodsId, long exp, int level, long combatPower,
            out bool levelChanged)
        {
            levelChanged = false;
            PetEquipInfo info = Get(typeId);
            if (info?.Items == null) return false;
            foreach (PetEquipItem item in info.Items)
            {
                if (item.GoodsId != goodsId) continue;
                levelChanged = item.PosLevel != level;
                item.PosPoint = exp;
                item.PosLevel = level;
                info.CombatPower = combatPower;
                return true;
            }
            return false;
        }

        /// <summary>16017 成功回包只修改命中的 goods_id。</summary>
        public bool TryApplyPolish(int typeId, long goodsId, int stage, int star, long exp, int level,
            long combatPower)
        {
            PetEquipInfo info = Get(typeId);
            if (info?.Items == null) return false;
            foreach (PetEquipItem item in info.Items)
            {
                if (item.GoodsId != goodsId) continue;
                item.Stage = stage;
                item.Star = star;
                item.PosPoint = exp;
                item.PosLevel = level;
                info.CombatPower = combatPower;
                return true;
            }
            return false;
        }

        public void Clear() => _infoByType.Clear();
    }
}
