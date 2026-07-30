using System.Collections.Generic;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包数据层(对标老客户端 commonModel/GoodsModel.ts 的 bag_goods_list / bag_goods_max_cell + commonModel/BagModel.ts)。
    /// 满背包协议 15010(pos=bag=4)回包经 <see cref="BagController"/> 解析后落此;<see cref="Views.BagComponentView"/> 据此铺格。
    ///
    /// 老端链路:GoodsController.On15010(pos==bag)→ goodsModel.bag_goods_max_cell=vo.max_cell + goodsModel.CreateBagList(vo.goods_list)
    /// (CreateBagList = ResetBagData 清空 + 逐项 UpdateBagGoods → AddGoodsToBag)。本类 <see cref="SetBagFull"/> 一比一对标
    /// 「满包全量 = 清空再装入」语义。主背包之外还缓存坐骑/伙伴装备与装备背包四容器
    /// (pos=22/32/23/33)，供 PetEquip 数据层读取；其它容器仍按原有专线分流。
    /// </summary>
    public sealed class BagModel
    {
        public static readonly BagModel Instance = new BagModel();
        private BagModel() { }

        /// <summary>背包槽位类型(对标 GoodsModel.GOODS_POS_TYPE.bag = 4)。请求满背包 SendFmt(15010,"h",POS_BAG)。</summary>
        public const int POS_BAG = 4;

        // ----- 物品容器槽位全表(Goods 协议扩容轮:抄自老端 GoodsModel.ts:305-343 GOODS_POS_TYPE,原表全量;
        // 本轮仅登记数值供 15002/15010 等按 pos 分流用,POS_BAG 保持既有兼容不变) -----
        public const int POS_EQUIP = 1;
        public const int POS_WAREHOUSE = 5;
        public const int POS_EQUIP_BAG = 7;
        public const int POS_ARTIFACT_BAG = 8;
        public const int POS_TREASURE_BAG = 10;
        public const int POS_RUNE_BAG = 11;
        public const int POS_RUNE = 12;
        public const int POS_SPIRIT = 13;
        public const int POS_SOUL = 14;
        public const int POS_SOUL_BAG = 15;
        public const int POS_BEAST = 16;
        public const int POS_BEAST_BAG = 17;
        public const int POS_HORSE = 22;
        public const int POS_HORSE_BAG = 32;
        public const int POS_PARTNER = 23;
        public const int POS_PARTNER_BAG = 33;
        public const int POS_UNREAL = 28;
        public const int POS_UNREAL_BAG = 29;
        public const int POS_HOLY_SEAL_BAG = 30;
        public const int POS_HOLY_SEAL = 31;
        public const int POS_LUNG_EQUIP = 34;
        public const int POS_LUNG_BAG = 35;
        public const int POS_BABY_EQUIP = 36;
        public const int POS_BABY_BAG = 37;
        public const int POS_GOD_EQUIP = 38;
        public const int POS_GOD_BAG = 39;
        public const int POS_REVELATION_EQUIP = 40;
        public const int POS_REVELATION_BAG = 41;
        public const int POS_DEMON_TALENT_BAG = 42;
        public const int POS_LONGLANG_BAG = 43;
        public const int POS_LONGLANG_EQUIP = 44;
        public const int POS_STAR_EQUIP_BAG = 45;
        public const int POS_STAR_EQUIP = 46;
        public const int POS_GOD_COURT_BAG = 47;
        public const int POS_GOD_COURT = 48;
        public const int POS_GUILD_RUNE = 49;

        /// <summary>背包物品(对标 GoodsModel.bag_goods_list;满包 15010 全量装入,顺序即服务端下发序)。</summary>
        public readonly List<BagGoods> BagGoodsList = new List<BagGoods>();

        /// <summary>角色当前已穿戴装备(pos=1)。保留服务端实例字段，槽位在读取时按 config_goods.equip_type 解析。</summary>
        private readonly List<BagGoods> _equipment = new List<BagGoods>();
        public IReadOnlyList<BagGoods> EquipmentGoodsList => _equipment;
        public bool HasEquipmentData { get; private set; }

        /// <summary>坐骑/伙伴装备四容器。只收 pos=22/32/23/33，不把其它容器混入主背包。</summary>
        private readonly Dictionary<int, List<BagGoods>> _petEquipContainers = new Dictionary<int, List<BagGoods>>();
        // pos36=宝宝已穿戴装备实例；pos37=待穿候选背包，二者不可混用。
        private readonly List<BagGoods> _babyEquip = new List<BagGoods>();
        private readonly List<BagGoods> _babyEquipBag = new List<BagGoods>();
        private static readonly IReadOnlyList<BagGoods> EmptyContainer = new BagGoods[0];

        /// <summary>各槽位容量(对标 GoodsModel.xxx_max_cell 系列字段;15002 扩容成功后按 pos 更新,见 BagController.On15002)。</summary>
        private readonly Dictionary<int, int> _maxCellByPos = new Dictionary<int, int>();

        /// <summary>背包容量(对标 GoodsModel.bag_goods_max_cell = vo.max_cell);等价 GetMaxCell(POS_BAG),旧字段保留兼容。</summary>
        public int MaxCell => GetMaxCell(POS_BAG);
        public int BabyEquipBagMaxCell => GetMaxCell(POS_BABY_BAG);
        public int BabyEquipMaxCell => GetMaxCell(POS_BABY_EQUIP);
        public bool HasBabyEquipData { get; private set; }
        public bool HasBabyEquipBagData { get; private set; }

        /// <summary>取任意槽位当前容量(未收到过 15010/15002 则 0)。</summary>
        public int GetMaxCell(int pos) => _maxCellByPos.TryGetValue(pos, out int v) ? v : 0;

        /// <summary>写入某槽位容量(对标 15002 成功回包 cell_num / 15010 max_cell)。</summary>
        public void SetMaxCell(int pos, int total) => _maxCellByPos[pos] = total;

        /// <summary>已用格子数(15010 cell_num)。</summary>
        public int CellNum { get; private set; }

        /// <summary>是否已收到过满背包 15010(未收到 = 无真实数据,只能空铺或显 blocker)。</summary>
        public bool HasData { get; private set; }

        /// <summary>特殊积分/代币(15008/15009,对标 GoodsModel.special_score_dic_:currency_id → num;
        /// 主货币 金/铜 走 13xxx 落 RoleModel,不在此)。</summary>
        public readonly Dictionary<int, long> SpecialScores = new Dictionary<int, long>();

        /// <summary>满背包全量(对标 GoodsModel.CreateBagList:ResetBagData 清空 + 逐项装入)。</summary>
        public void SetBagFull(int cellNum, int maxCell, List<BagGoods> goods)
        {
            BagGoodsList.Clear();
            if (goods != null) BagGoodsList.AddRange(goods);
            CellNum = cellNum;
            SetMaxCell(POS_BAG, maxCell);
            HasData = true;
        }

        /// <summary>
        /// 单件全字段增量(15017,对标 GoodsModel.UpdateBagGoods):已有 → num&lt;=0 删、否则整项替换
        /// (对标 CopyGoodsVo,15017 项为全字段);没有且 num&gt;0 → 追加(对标 AddGoodsToBag)。
        /// </summary>
        public void Upsert(BagGoods vo)
        {
            if (vo == null) return;
            int idx = BagGoodsList.FindIndex(g => g.GoodsId == vo.GoodsId);
            if (idx >= 0)
            {
                if (vo.GoodsNum <= 0) BagGoodsList.RemoveAt(idx);
                else BagGoodsList[idx] = vo;
            }
            else if (vo.GoodsNum > 0)
            {
                BagGoodsList.Add(vo);
            }
        }

        /// <summary>
        /// 数量增量(15018 {goods_id,goods_num,type_id},对标 UpdateBagGoods 的最小面):已有 → num&lt;=0 删、
        /// 否则仅改数量;没有且 num&gt;0 → 以最小字段新建兜底(新物品正常走 15017 全字段)。
        /// </summary>
        public void UpdateNum(long goodsId, int typeId, long num)
        {
            int idx = BagGoodsList.FindIndex(g => g.GoodsId == goodsId);
            if (idx >= 0)
            {
                if (num <= 0) BagGoodsList.RemoveAt(idx);
                else BagGoodsList[idx].GoodsNum = num;
            }
            else if (num > 0)
            {
                BagGoodsList.Add(new BagGoods { GoodsId = goodsId, TypeId = typeId, GoodsNum = num });
            }
        }

        /// <summary>是否为 PetEquip 使用的四个物品容器。</summary>
        public static bool IsPetEquipContainer(int pos)
        {
            return pos == POS_HORSE || pos == POS_HORSE_BAG || pos == POS_PARTNER || pos == POS_PARTNER_BAG;
        }

        /// <summary>
        /// 查询物品容器。跨模块契约：PetEquip 只传 22/32/23/33；兼容传 4 时返回既有主背包。
        /// 未收到全量时返回只读空表而非 null。
        /// </summary>
        public IReadOnlyList<BagGoods> GetContainer(int pos)
        {
            if (pos == POS_BAG) return BagGoodsList;
            if (pos == POS_EQUIP) return _equipment;
            if (pos == POS_BABY_EQUIP) return _babyEquip;
            if (pos == POS_BABY_BAG) return _babyEquipBag;
            return _petEquipContainers.TryGetValue(pos, out List<BagGoods> list) ? list : EmptyContainer;
        }

        /// <summary>15010 pos=1 全量。列表本身是协议事实源，不在配置尚未加载时提前丢弃槽位信息。</summary>
        internal void SetEquipmentFull(int maxCell, List<BagGoods> goods)
        {
            _equipment.Clear();
            if (goods != null) _equipment.AddRange(goods);
            SetMaxCell(POS_EQUIP, maxCell);
            HasEquipmentData = true;
        }

        /// <summary>15017 pos=1 全字段增量。goods_id 相同则替换，数量归零则移除。</summary>
        internal void UpsertEquipment(BagGoods vo) => UpsertList(_equipment, vo);

        /// <summary>15018 pos=1 数量增量。未知正数项先保留最小协议事实，等待后续全字段包补齐。</summary>
        internal void UpdateEquipmentNum(long goodsId, int typeId, long num) =>
            UpdateListNum(_equipment, goodsId, typeId, num);

        /// <summary>
        /// 按装备部位(1..10)取当前穿戴实例。老端全量以 config_goods.equip_type 为准；
        /// 配置尚未加载或缺项时退回服务端装备容器 cell，避免登录早到包被永久映射为空。
        /// </summary>
        public BagGoods GetEquipmentAt(int equipType)
        {
            for (int i = 0; i < _equipment.Count; i++)
            {
                BagGoods goods = _equipment[i];
                if (ResolveEquipmentType(goods) == equipType) return goods;
            }
            return null;
        }

        private static int ResolveEquipmentType(BagGoods goods)
        {
            if (goods == null) return 0;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic != null && basic.EquipType > 0) return basic.EquipType;
            return goods.Cell >= 1 && goods.Cell <= 10 ? goods.Cell : 0;
        }

        internal void SetBabyEquipFull(int maxCell, List<BagGoods> goods)
        {
            _babyEquip.Clear();
            if (goods != null) _babyEquip.AddRange(goods);
            SetMaxCell(POS_BABY_EQUIP, maxCell);
            HasBabyEquipData = true;
        }

        internal void UpsertBabyEquip(BagGoods vo) => UpsertList(_babyEquip, vo);
        internal void UpdateBabyEquipNum(long goodsId, int typeId, long num) => UpdateListNum(_babyEquip, goodsId, typeId, num);

        internal void SetBabyEquipBagFull(int maxCell, List<BagGoods> goods)
        {
            _babyEquipBag.Clear();
            if (goods != null) _babyEquipBag.AddRange(goods);
            SetMaxCell(POS_BABY_BAG, maxCell);
            HasBabyEquipBagData = true;
        }

        internal void UpsertBabyEquipBag(BagGoods vo) => UpsertList(_babyEquipBag, vo);
        internal void UpdateBabyEquipBagNum(long goodsId, int typeId, long num) => UpdateListNum(_babyEquipBag, goodsId, typeId, num);

        private static void UpsertList(List<BagGoods> list, BagGoods vo)
        {
            if (vo == null) return;
            int idx = list.FindIndex(g => g.GoodsId == vo.GoodsId);
            if (idx >= 0) { if (vo.GoodsNum <= 0) list.RemoveAt(idx); else list[idx] = vo; }
            else if (vo.GoodsNum > 0) list.Add(vo);
        }
        private static void UpdateListNum(List<BagGoods> list, long goodsId, int typeId, long num)
        {
            int idx = list.FindIndex(g => g.GoodsId == goodsId);
            if (idx >= 0) { if (num <= 0) list.RemoveAt(idx); else list[idx].GoodsNum = num; }
            else if (num > 0) list.Add(new BagGoods { GoodsId = goodsId, TypeId = typeId, GoodsNum = num });
        }

        /// <summary>按实例 goods_id 查容器物品；未找到返回 null。</summary>
        public BagGoods FindContainerGoods(int pos, long goodsId)
        {
            IReadOnlyList<BagGoods> list = GetContainer(pos);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GoodsId == goodsId) return list[i];
            }
            return null;
        }

        /// <summary>
        /// 16017 宠物装备打磨成功后的已穿戴实例同步。wornPos 只接受 22/23；找不到不造幽灵物品。
        /// 老端把回包 combat_power 写入 wear.overall_rating，故这里不改物品自身 CombatPower。
        /// </summary>
        public bool UpdatePetEquipState(int wornPos, long goodsId, int equipStage, int equipStar, long overallRating)
        {
            if (wornPos != POS_HORSE && wornPos != POS_PARTNER) return false;
            BagGoods goods = FindContainerGoods(wornPos, goodsId);
            if (goods == null) return false;
            goods.EquipStage = equipStage;
            goods.EquipStar = equipStar;
            goods.OverallRating = overallRating;
            return true;
        }

        /// <summary>15010 四容器全量：原子替换并记录 max_cell。仅供 BagController。</summary>
        internal void SetPetEquipContainerFull(int pos, int maxCell, List<BagGoods> goods)
        {
            if (!IsPetEquipContainer(pos)) return;
            _petEquipContainers[pos] = goods != null ? new List<BagGoods>(goods) : new List<BagGoods>();
            SortPetEquipBag(pos, _petEquipContainers[pos]);
            SetMaxCell(pos, maxCell);
        }

        /// <summary>15017 四容器全字段增量。已穿戴容器新增同 cell 物品时替换旧实例。</summary>
        internal void UpsertPetEquipContainer(int pos, BagGoods vo)
        {
            if (!IsPetEquipContainer(pos) || vo == null) return;
            if (!_petEquipContainers.TryGetValue(pos, out List<BagGoods> list))
            {
                list = new List<BagGoods>();
                _petEquipContainers[pos] = list;
            }

            int idx = list.FindIndex(g => g.GoodsId == vo.GoodsId);
            if (idx >= 0)
            {
                if (vo.GoodsNum <= 0) list.RemoveAt(idx);
                else list[idx] = vo;
                SortPetEquipBag(pos, list);
                return;
            }
            if (vo.GoodsNum <= 0) return;

            if ((pos == POS_HORSE || pos == POS_PARTNER) && vo.Cell > 0)
            {
                int cellIdx = list.FindIndex(g => g.Cell == vo.Cell);
                if (cellIdx >= 0)
                {
                    list[cellIdx] = vo;
                    return;
                }
            }
            list.Add(vo);
            SortPetEquipBag(pos, list);
        }

        /// <summary>
        /// 15018 四容器数量增量。该包没有 cell/评分/装备阶段等字段；与老端
        /// UpdateHorse/Partner*Goods 一致，未知 goods_id 且 num&gt;0 时先按包内 type_id 建最小项，
        /// 后续 15017/15010 再补齐全字段。
        /// </summary>
        internal void UpdatePetEquipContainerNum(int pos, long goodsId, int typeId, long num)
        {
            if (!IsPetEquipContainer(pos)) return;
            if (!_petEquipContainers.TryGetValue(pos, out List<BagGoods> list))
            {
                if (num <= 0) return;
                list = new List<BagGoods>();
                _petEquipContainers[pos] = list;
            }
            int idx = list.FindIndex(g => g.GoodsId == goodsId);
            if (idx >= 0)
            {
                if (num <= 0) list.RemoveAt(idx);
                else list[idx].GoodsNum = num;
            }
            else if (num > 0)
            {
                list.Add(new BagGoods { GoodsId = goodsId, TypeId = typeId, GoodsNum = num });
            }
            SortPetEquipBag(pos, list);
        }

        /// <summary>老端仅对 horse_bag/partner_bag 排序：品质降序，同品质评分降序。</summary>
        private static void SortPetEquipBag(int pos, List<BagGoods> list)
        {
            if ((pos != POS_HORSE_BAG && pos != POS_PARTNER_BAG) || list == null || list.Count < 2) return;
            list.Sort((a, b) =>
            {
                int color = b.Color.CompareTo(a.Color);
                return color != 0 ? color : b.Rating.CompareTo(a.Rating);
            });
        }

        /// <summary>取特殊积分(对标 GoodsModel.GetSpecialScore;无则 0)。</summary>
        public long GetSpecialScore(int currencyId)
        {
            return SpecialScores.TryGetValue(currencyId, out long v) ? v : 0;
        }

        /// <summary>取某 typeId 的背包持有总数(对标老端 GoodsModel.GetTypeGoodsNum;本端只落了满背包(POS_BAG),
        /// 仓库/其它容器未移植 → 只能统计背包内堆叠,材料预校验等场景够用)。</summary>
        public long GetTypeGoodsNum(int typeId)
        {
            long sum = 0;
            foreach (BagGoods g in BagGoodsList)
            {
                if (g.TypeId == typeId) sum += g.GoodsNum;
            }
            return sum;
        }

        /// <summary>断线/登出清空(对标 ResetBagData)。</summary>
        public void Clear()
        {
            BagGoodsList.Clear();
            _equipment.Clear();
            _petEquipContainers.Clear();
            _babyEquip.Clear();
            _babyEquipBag.Clear();
            HasBabyEquipData = false;
            HasBabyEquipBagData = false;
            HasEquipmentData = false;
            SpecialScores.Clear();
            _maxCellByPos.Clear();
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
        public int Bind;       // bind:c(0=不绑定;公会仓库捐献等"仅非绑定可用"场景过滤用,对标老端 GetShowEquips info.bind!=0)

        // —— 装备实例态(非装备物品恒 0/null;装备 tips「极品/强化」实例行用,待活服实装备 + 实例透传)——
        public int Stren;        // stren:h(强化等级)
        public int Level;        // level:h(实例需求等级)
        public long Rating;      // rating:i(评分)
        public long OverallRating; // overall_rating:i(总评分;PetEquip 16017 成功会同步已穿戴实例)
        public long CombatPower; // combat_power:i(战力)
        public int EquipStage;   // equipStage:c(坐骑/伙伴装备阶)
        public int EquipStar;    // equipStar:c(坐骑/伙伴装备星)
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
