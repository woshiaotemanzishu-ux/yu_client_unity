using System.Collections.Generic;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 宝石雕刻(15210/15211;自动循环 轮4 下半/4b)数据层:equip_pos(1..10) → 雕刻等级/经验/属性列表
    /// (对标老端 EquipModel.jewel_crave_data_list)。宝石本体(镶嵌槽 stone_list)不落在此处 —— 走
    /// <see cref="Bag.GoodsDynamicModel"/>(GoodsDetailVo.StoneList,15000/15001),此处只管"雕刻"这一独立子系统
    /// 的等级/经验/属性,对标 15210 字段名 refine_lv(勿与 15250 精炼的 refine 混,两套完全独立)。
    /// </summary>
    public sealed class EquipJewelModel
    {
        public static readonly EquipJewelModel Instance = new EquipJewelModel();
        private EquipJewelModel() { }

        /// <summary>15210 单个装备位雕刻信息(对标老端 jewel_crave_data_list[equip_pos] 单项)。</summary>
        public struct CraveInfo
        {
            public int RefineLv;
            public long Exp;
            public List<(int AttrId, long AttrVal)> Attrs;
        }

        private readonly Dictionary<int, CraveInfo> _craveMap = new Dictionary<int, CraveInfo>();

        /// <summary>15210 回包落库(对标老端 on15210 model.jewel_crave_data_list[equip_type]=scmd)。</summary>
        public void Apply15210(int equipPos, int refineLv, long exp, List<(int attrId, long attrVal)> attrs)
        {
            _craveMap[equipPos] = new CraveInfo { RefineLv = refineLv, Exp = exp, Attrs = attrs };
        }

        /// <summary>取指定装备位雕刻信息;未查询到 → null(对标未落库前的 jewel_crave_data_list[pos] undefined)。</summary>
        public CraveInfo? GetCrave(int equipPos) => _craveMap.TryGetValue(equipPos, out CraveInfo v) ? v : (CraveInfo?)null;

        public void Clear() => _craveMap.Clear();
    }
}
