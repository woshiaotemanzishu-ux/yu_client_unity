using System.Collections.Generic;

namespace Shenxiao.Module.Core.StarEquip
{
    /// <summary>
    /// 星宿核心(pp_constellation_equip,pt_232 23200-23209+23250-23257,共 17 号)数据层。
    /// 对标老端 StarEquipModel.ts 的数据子集(不含 UI 派生逻辑:红点/tab 列表/OPEN_VIEW 弹窗等留尾包,
    /// 见 StarEquipController 类注释)。锻造(chc/StarForge,PK2)的配置读取合用 <see cref="StarEquipConfigs"/>,
    /// 但数据落地走它自己的 StarForgeModel,本类不掺锻造状态。
    ///
    /// 23204(单星宿总属性查询)按主控裁决1 **不发不收**(killlist),本类不建对应存储。
    /// </summary>
    public sealed class StarEquipModel
    {
        public static readonly StarEquipModel Instance = new StarEquipModel();
        private StarEquipModel() { }

        // ============================================================================================
        // §0 wire 公共形态(与 chc/StarForge 共享同一套 attr_list / 星宿加成 / 设计属性嵌套结构)
        // ============================================================================================

        /// <summary>标准 attr_list wire 元素(pt.erl write_attr_list:AttrId:16,AttrVal:32;服务端写出时会过滤
        /// Value&lt;=0 的项,读侧无需关心该过滤,原样落地)。</summary>
        public sealed class AttrEntry { public int AttrId; public long AttrVal; }

        /// <summary>"StarAttrCfg" wire 元素(pt_232.erl item_to_bin_8/11/13,6 字段:AttrId:16,AttrVal:32,
        /// PlusInterval:8,PlusUnit:32,Color:8,TypeId:8)。语义是装备的"加成"(addition)列表,字段顺序与
        /// Bag/Equip 家族的 EquipExtraAttr(Color 在前)不同,不可混用,故单独建类型。</summary>
        public sealed class AdditionAttrEntry
        {
            public int AttrId;
            public long AttrVal;
            public int PlusInterval;
            public long PlusUnit;
            public int Color;
            public int TypeId;
        }

        /// <summary>"SendDsgt"(设计属性/称号套装预览)wire 元素(item_to_bin_7/10/12):
        /// DsgtId:32,DsgtNum:16,DsgtSuit(attr_list),DsgtAttr(attr_list)。</summary>
        public sealed class DsgtEntry
        {
            public int DsgtId;
            public int DsgtNum;
            public readonly List<AttrEntry> DsgtSuit = new List<AttrEntry>();
            public readonly List<AttrEntry> DsgtAttr = new List<AttrEntry>();
        }

        /// <summary>23250/23254 共用的完整属性预览形态(get_tips_msg / do_handle(23254) 同源写出)。
        /// StrenAttr/EvoluAttr/MasterAttr/SpiritAttr 来自 lib_constellation_forge_api:get_forge_attr_detail,
        /// 是锻造(PK2)系统的贡献值,本轮只如实落地,不解读。</summary>
        public sealed class TipsPreview
        {
            public long GoodsAutoId;
            public long TargetGoodsAutoId; // 23254 独有;23250 恒为 0(wire 无此字段)
            public long Score;
            public readonly List<DsgtEntry> SendDsgt = new List<DsgtEntry>();
            public readonly List<AdditionAttrEntry> StarAttrCfg = new List<AdditionAttrEntry>();
            public readonly List<AttrEntry> StarAttr = new List<AttrEntry>();
            public int SuitNum;
            public readonly List<AttrEntry> SuitAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> BaseAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> ExtraAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> StrenAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> EvoluAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> MasterAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> SpiritAttr = new List<AttrEntry>();
            public long BaseRating;
        }

        /// <summary>23255 精简预览形态(goods_type_id 维度,无实际穿戴实例,不含 SuitAttr/锻造四段属性——
        /// 对照 pt_232.erl write(23255,...) 字段序核实,比 TipsPreview 少 5 个字段)。</summary>
        public sealed class TypeTipsPreview
        {
            public long GoodsTypeId;
            public long Score;
            public readonly List<DsgtEntry> SendDsgt = new List<DsgtEntry>();
            public readonly List<AdditionAttrEntry> StarAttrCfg = new List<AdditionAttrEntry>();
            public readonly List<AttrEntry> StarAttr = new List<AttrEntry>();
            public int SuitNum;
            public readonly List<AttrEntry> BaseAttr = new List<AttrEntry>();
            public readonly List<AttrEntry> ExtraAttr = new List<AttrEntry>();
            public long BaseRating;
        }

        // ============================================================================================
        // §1 总览(23201)+ 单页解锁(23253)
        // ============================================================================================

        /// <summary>23201 ItemList 单条(item_to_bin_0:Page:32,Power:64,NormalNum:8,SpecialNum:8,
        /// Attr(attr_list),IsActive:8)。</summary>
        public sealed class PageItem
        {
            public int Page;
            public long Power;
            public int NormalNum;
            public int SpecialNum;
            public readonly List<AttrEntry> Attr = new List<AttrEntry>();
            public int IsActive;
        }

        private readonly Dictionary<int, PageItem> _pageInfo = new Dictionary<int, PageItem>();
        public IReadOnlyDictionary<int, PageItem> PageInfo => _pageInfo;

        /// <summary>23201 TotalStar 字段是 **u16**(pt_232.erl write(23201,...):TotalStar:16,不是 32 位),
        /// 老端 model.totalStar 直接落地此值。</summary>
        public int TotalStar { get; private set; }
        public bool HasOverview { get; private set; }

        /// <summary>23201 全量落地(老端逐条覆盖 pageInfo[vo.page]=vo,ts:297-318;弹窗/红点派生逻辑属 UI 层,
        /// 本轮不移植,见 StarEquipController 类注释)。返回落地前的 TotalStar,供调用方做 diff 判断
        /// (对标老端 On23201 在赋值前比较 model.totalStar!=scmd.total_star,ts:150)。</summary>
        public int SetOverview(int totalStar, List<PageItem> items)
        {
            int old = TotalStar;
            if (items != null)
            {
                foreach (PageItem it in items) _pageInfo[it.Page] = it;
            }
            TotalStar = totalStar;
            HasOverview = true;
            return old;
        }

        /// <summary>23253 解锁成功后原地置位(对标老端 ts:390-393 list.is_active=1)。</summary>
        public void MarkPageActive(int page)
        {
            if (_pageInfo.TryGetValue(page, out PageItem it)) it.IsActive = 1;
        }

        /// <summary>对标老端 StarEquipModel.StarEquipIsActive(page):该页存在且 is_active!=0。
        /// PK2(StarForge/chc)通过本方法判断星宿页解锁态(chcModel.ts:304/350/396 UpdateEquipHandle 调用点)。</summary>
        public bool IsPageActive(int page) => _pageInfo.TryGetValue(page, out PageItem it) && it.IsActive != 0;

        // ============================================================================================
        // §2 星级大师(23205 查询 / 23206 升级 / 23251 推送)
        // ============================================================================================

        /// <summary>23205/23251 共用形态(两号 wire 字段序完全相同:Level:16,MaxLevel:16,Star:16,Power:32,
        /// 均无 Code 前导帧)。</summary>
        public sealed class StarMasterInfo { public int Level; public int MaxLevel; public int Star; public long Power; }

        /// <summary>23205 查询结果(手动/登录链请求)。</summary>
        public StarMasterInfo StarMaster { get; private set; }
        public void SetStarMaster(StarMasterInfo info) => StarMaster = info;

        /// <summary>23251 被动推送(星数变化时服务端主动下发,如穿脱装备后 send_item→notify_client_star)。</summary>
        public StarMasterInfo StarPush { get; private set; }
        public void SetStarPush(StarMasterInfo info) => StarPush = info;

        /// <summary>23206 升级结果原地更新(仅 code==1 时调用方应传入;本类只管落地不判 code)。</summary>
        public void ApplyStarMasterUp(int level, long power)
        {
            if (StarMaster == null) StarMaster = new StarMasterInfo();
            StarMaster.Level = level;
            StarMaster.Power = power;
        }

        // ============================================================================================
        // §3 吞噬(23207 信息 / 23208 勾选 / 23209 执行)
        // ============================================================================================

        /// <summary>23207 全量形态:Level:16,Exp:32,Power:32,Color:8,Star:8。</summary>
        public sealed class DevourInfo
        {
            public int Level;
            public long Exp;
            public long Power;
            public int Color;
            public int Star;
        }

        public DevourInfo Devour { get; private set; }
        public bool HasDevourInfo { get; private set; }

        /// <summary>23207 全量落地。</summary>
        public void SetDevourInfo(DevourInfo info) { Devour = info; HasDevourInfo = true; }

        /// <summary>23208 成功后原地更新 Color/Star(wire 里 Color/Star 就是回声值,老端 ts:314-315 直接覆盖)。
        /// Devour 尚未初始化时防御性新建(不强依赖调用顺序,同 Marriage RingInfo 先例)。</summary>
        public void ApplyDevourTab(int color, int star)
        {
            if (Devour == null) Devour = new DevourInfo();
            Devour.Color = color;
            Devour.Star = star;
        }

        /// <summary>23209 成功响应形态(Level:16,Exp:32,Power:32,**无 Color/Star 字段**——吞噬执行不改选中
        /// 品质/星级筛选,老端 ts:326-328 也只写 level/exp/power 三项)。</summary>
        public sealed class DevourResult { public int Level; public long Exp; public long Power; }

        /// <summary>23209 成功后原地更新 Level/Exp/Power,Color/Star 保持不变(对标老端 On23209,ts:323-332)。</summary>
        public void ApplyDevourResult(DevourResult r)
        {
            if (Devour == null) Devour = new DevourInfo();
            Devour.Level = r.Level;
            Devour.Exp = r.Exp;
            Devour.Power = r.Power;
        }

        // ============================================================================================
        // §4 属性预览(23250 装备tips / 23254 蜕变对比 / 23255 类型tips)
        // ============================================================================================

        /// <summary>23250 最近一次预览(老端单槽缓存,无 key,新请求直接覆盖)。</summary>
        public TipsPreview LastPreview { get; private set; }
        public void SetLastPreview(TipsPreview p) => LastPreview = p;

        /// <summary>23254 蜕变/属性转移对比预览(老端 model.transfromCache,单槽覆盖)。</summary>
        public TipsPreview LastTransformPreview { get; private set; }
        public void SetLastTransformPreview(TipsPreview p) => LastTransformPreview = p;

        /// <summary>23255 按 goods_type_id 分桶缓存(老端 model.typePreviewCache[scmd.goods_id],ts:418)。</summary>
        private readonly Dictionary<long, TypeTipsPreview> _typePreviewCache = new Dictionary<long, TypeTipsPreview>();
        public IReadOnlyDictionary<long, TypeTipsPreview> TypePreviewCache => _typePreviewCache;
        public void SetTypePreview(long goodsTypeId, TypeTipsPreview p) => _typePreviewCache[goodsTypeId] = p;

        // ============================================================================================
        // §5 合成(23252)+ 合成次数(23256)+ 蜕变执行(23257)
        // ============================================================================================

        public sealed class ComposeRewardEntry { public long GoodsId; public long GoodsTypeId; }

        /// <summary>23252 最近一次成功结果(code==1 或 1500080 时落地,对标老端 COM_SUCCESS)。</summary>
        public int LastComposeRuleId { get; private set; }
        public readonly List<ComposeRewardEntry> LastComposeReward = new List<ComposeRewardEntry>();

        public void SetComposeSuccess(int ruleId, List<ComposeRewardEntry> list)
        {
            LastComposeRuleId = ruleId;
            LastComposeReward.Clear();
            if (list != null) LastComposeReward.AddRange(list);
        }

        /// <summary>23256 单个 compose_id 的次数/倒计时信息(老端 model.comSpNumList[scmd.compose_id],分桶存)。</summary>
        public sealed class ComposeTimeInfo { public int ComposeId; public int Times; public int Index; public int Num; }
        private readonly Dictionary<int, ComposeTimeInfo> _composeTime = new Dictionary<int, ComposeTimeInfo>();
        public IReadOnlyDictionary<int, ComposeTimeInfo> ComposeTime => _composeTime;
        public void SetComposeTime(ComposeTimeInfo info) => _composeTime[info.ComposeId] = info;

        // ============================================================================================

        public void Clear()
        {
            _pageInfo.Clear();
            TotalStar = 0;
            HasOverview = false;

            StarMaster = null;
            StarPush = null;

            Devour = null;
            HasDevourInfo = false;

            LastPreview = null;
            LastTransformPreview = null;
            _typePreviewCache.Clear();

            LastComposeRuleId = 0;
            LastComposeReward.Clear();
            _composeTime.Clear();
        }
    }
}
