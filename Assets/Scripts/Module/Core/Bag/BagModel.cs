using System.Collections.Generic;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包数据层(对标老客户端 commonModel/GoodsModel.ts 的 bag_goods_list / bag_goods_max_cell + commonModel/BagModel.ts)。
    /// 满背包协议 15010(pos=bag=4)回包经 <see cref="BagController"/> 解析后落此;<see cref="Views.BagComponentView"/> 据此铺格。
    ///
    /// 老端链路:GoodsController.On15010(pos==bag)→ goodsModel.bag_goods_max_cell=vo.max_cell + goodsModel.CreateBagList(vo.goods_list)
    /// (CreateBagList = ResetBagData 清空 + 逐项 UpdateBagGoods → AddGoodsToBag)。本类 <see cref="SetBagFull"/> 一比一对标
    /// 「满包全量 = 清空再装入」语义。只保留显示所需字段(type_id/goods_num/color/cell + goods_id 主键),装备态(强化/星级/
    /// 觉醒/附加属性)按 ClientProtocol.json 顺序读过但暂不留(待装备 tips/配置移植)。
    /// </summary>
    public sealed class BagModel
    {
        public static readonly BagModel Instance = new BagModel();
        private BagModel() { }

        /// <summary>背包槽位类型(对标 GoodsModel.GOODS_POS_TYPE.bag = 4)。请求满背包 SendFmt(15010,"h",POS_BAG)。</summary>
        public const int POS_BAG = 4;

        /// <summary>背包物品(对标 GoodsModel.bag_goods_list;满包 15010 全量装入,顺序即服务端下发序)。</summary>
        public readonly List<BagGoods> BagGoodsList = new List<BagGoods>();

        /// <summary>背包容量(对标 GoodsModel.bag_goods_max_cell = vo.max_cell)。</summary>
        public int MaxCell { get; private set; }

        /// <summary>已用格子数(15010 cell_num)。</summary>
        public int CellNum { get; private set; }

        /// <summary>是否已收到过满背包 15010(未收到 = 无真实数据,只能空铺或显 blocker)。</summary>
        public bool HasData { get; private set; }

        /// <summary>满背包全量(对标 GoodsModel.CreateBagList:ResetBagData 清空 + 逐项装入)。</summary>
        public void SetBagFull(int cellNum, int maxCell, List<BagGoods> goods)
        {
            BagGoodsList.Clear();
            if (goods != null) BagGoodsList.AddRange(goods);
            CellNum = cellNum;
            MaxCell = maxCell;
            HasData = true;
        }

        /// <summary>断线/登出清空(对标 ResetBagData)。</summary>
        public void Clear()
        {
            BagGoodsList.Clear();
            MaxCell = 0;
            CellNum = 0;
            HasData = false;
        }
    }

    /// <summary>
    /// 背包物品(对标 15010 goods_list 单项;字段名照抄 ClientProtocol.json)。显示主用 4 字段 + 主键;
    /// 装备实例态(强化/评分 + 极品/附加/觉醒 3 数组)在 <see cref="BagController.ReadGoods"/> 按序读出后暂存,
    /// 供装备 tips 实例属性(对标 EquipToolTips equip_extra_attr/stren)——本轮只持有不显示(显示路径需活服实装备 + 实例透传,见任务包 blocker)。
    /// type_id → <see cref="Common.GoodsModel"/> 还原真实图标/名/品质/基础属性。
    /// </summary>
    public sealed class BagGoods
    {
        public long GoodsId;   // goods_id:l(实例唯一主键)
        public int TypeId;     // type_id:i(配置主键 → config_goods)
        public long GoodsNum;  // goods_num:i(堆叠数量)
        public int Color;      // color:c(品质 0..8)
        public int Cell;       // cell:h(格子序号)

        // —— 装备实例态(非装备物品恒 0/null;装备 tips「极品/强化」实例行用,待活服实装备 + 实例透传)——
        public int Stren;        // stren:h(强化等级)
        public int Level;        // level:h(实例需求等级)
        public long Rating;      // rating:i(评分)
        public long CombatPower; // combat_power:i(战力)
        public List<EquipExtraAttr> ExtraAttrs;     // equip_extra_attr(极品属性,对标 EquipToolTips SetBestPro)
        public List<EquipAdditionAttr> AdditionAttrs; // addition_attrlist(附加属性)
        public List<EquipAwakeAttr> AwakeList;        // awake_list(觉醒)

        /// <summary>是否带任一装备实例属性(供日志/未来实例行判定;无则只能显 config 基础属性)。</summary>
        public bool HasInstanceAttr =>
            (ExtraAttrs != null && ExtraAttrs.Count > 0) ||
            (AdditionAttrs != null && AdditionAttrs.Count > 0) ||
            (AwakeList != null && AwakeList.Count > 0);
    }

    /// <summary>极品属性(equip_extra_attr 单项,字段照抄 ClientProtocol.json "15010")。</summary>
    public struct EquipExtraAttr { public int Color; public int AttrTypeId; public int AttrId; public long AttrVal; public int PlusInterval; public long PlusUnit; }

    /// <summary>附加属性(addition_attrlist 单项)。</summary>
    public struct EquipAdditionAttr { public int AttrType; public long AttrValue; public int Color; public long CombatPower; }

    /// <summary>觉醒属性(awake_list 单项)。</summary>
    public struct EquipAwakeAttr { public int AttrType; public long AwakeLv; public long AwakeExp; }
}
