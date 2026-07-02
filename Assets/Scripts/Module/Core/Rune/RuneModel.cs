using System.Collections.Generic;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 灵魄镶嵌数据层(对标老端 commonModel 里 pt_167 相关字段;服务端 yu_server rune)。
    /// 与同目录既有「九霄劫魄」转换 UI(RuneFlow/RuneMainUIView 等,另一套已转换模块)完全独立、互不干扰——
    /// 本类只服务第19轮工单 B:灵魄镶嵌最小闭环(16700 全量/16701 镶嵌 + 符文背包 15010 rune_bag pos)。
    /// 主线 100990(ctype33)=镶嵌一次(孔位1无条件开放,if_open 由服务端 16700 回包给出)。
    /// </summary>
    public sealed class RuneModel
    {
        public static readonly RuneModel Instance = new RuneModel();
        private RuneModel() { }

        /// <summary>符文槽位(字段名对照 ClientProtocol.json "16700" rune_list 单项)。</summary>
        public sealed class SlotVo
        {
            public int PosId;         // pos_id:c
            public bool IfOpen;       // if_open:c
            public long GoodsId;      // goods_id:l(0=未镶嵌)
            public int GoodsTypeId;   // goods_type_id:i
            public int Color;         // color:c
            public int Lv;            // lv:h

            public bool IsWorn => GoodsId > 0;
        }

        /// <summary>符文背包(货物 loc=rune_bag,pos=11)单项——由 <see cref="SetRuneBag"/> 落库(controller 解 15010 回填)。</summary>
        public readonly struct BagGoodsVo
        {
            public readonly long GoodsId;
            public readonly int TypeId;
            public readonly long Num;
            public BagGoodsVo(long goodsId, int typeId, long num) { GoodsId = goodsId; TypeId = typeId; Num = num; }
        }

        public readonly List<SlotVo> Slots = new List<SlotVo>();
        public int RunePoint;    // rune_point:i
        public int RuneChip;     // rune_chip:i
        public long SumPower;    // rune_sum_power:l
        public bool HasData { get; private set; }

        public readonly List<BagGoodsVo> RuneBagGoods = new List<BagGoodsVo>();
        public bool HasRuneBag { get; private set; }

        public SlotVo GetSlot(int posId) => Slots.Find(s => s.PosId == posId);

        /// <summary>16700 全量(对标 On16700:清空重建槽位)。</summary>
        public void Apply16700(int runePoint, int runeChip, List<SlotVo> slots, long sumPower)
        {
            RunePoint = runePoint;
            RuneChip = runeChip;
            SumPower = sumPower;
            Slots.Clear();
            if (slots != null) Slots.AddRange(slots);
            HasData = true;
        }

        /// <summary>16701 镶嵌成功回包套值(对标 On16701 成功分支):目标槽位 goods_id/goods_type_id 覆盖。</summary>
        public void Apply16701(int posId, long newGoodsId, int newGoodsTypeId)
        {
            SlotVo slot = GetSlot(posId);
            if (slot == null) return;
            slot.GoodsId = newGoodsId;
            slot.GoodsTypeId = newGoodsTypeId;
        }

        /// <summary>16702 强化成功回包套值(对标 On16702 code==1 分支):消耗后的 rune_point(符文经验)。</summary>
        public void ApplyRunePoint(int runePoint)
        {
            RunePoint = runePoint;
        }

        /// <summary>符文背包落库(15010 pos==rune_bag 的 goods_list;经 BagController 例外分支转存,见该文件第19轮工单注释)。</summary>
        public void SetRuneBag(List<BagGoodsVo> list)
        {
            RuneBagGoods.Clear();
            if (list != null) RuneBagGoods.AddRange(list);
            HasRuneBag = true;
        }

        public void Clear()
        {
            Slots.Clear();
            RunePoint = 0;
            RuneChip = 0;
            SumPower = 0;
            HasData = false;
            RuneBagGoods.Clear();
            HasRuneBag = false;
        }
    }
}
