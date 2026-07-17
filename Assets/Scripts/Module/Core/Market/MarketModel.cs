namespace Shenxiao.Module.Core.Market
{
    /// <summary>
    /// 市场(交易行)数据(对标老客户端 commonModel/MarketModel.ts,639行)。
    /// 15121 下发跨服市场开放时间 open_time;据此在两个主界面图标间切换:未到跨服开放时间
    /// 显示本服市场 151,已到显示跨服市场 151@1(对标老端 showIcon:is_kf?151@1:151)。
    /// 自动循环 轮19 扩展:补 15100-15120/15122(死号 15103/15104/15105/15107/15110/15113 不接,
    /// 见 Proto.cs 对应段注释)。type_cfg(config_goods_sell_subtype 等 4 张表)与所有面板 UI 仍不移植
    /// (留尾包),本轮只落数据层:列表/挂单/求购/记录/次数等字段与增删改查,均对标 MarketModel.ts
    /// 同名方法(行号见各成员注释)。既有图标相关字段/方法一行未删。
    /// </summary>
    public sealed class MarketModel
    {
        public static readonly MarketModel Instance = new MarketModel();
        private MarketModel() { }

        /// <summary>本服市场图标(对标老端 showIcon 的 "151")。</summary>
        public const string ICON_TYPE_LOCAL = "151";

        /// <summary>跨服市场图标(对标老端 showIcon 的 "151@1",loc1 网格)。</summary>
        public const string ICON_TYPE_KF = "151@1";

        // 15121 跨服市场开放时间戳(unix 秒,0 表示未配置/未开放)。对标老端 kf_open_time。
        public int KfOpenTime;

        // 跨服市场是否已开放(对标老端 kf_open = open_time>0 && serverTime>open_time)。
        public bool KfOpen;

        public void SetKfOpen(int openTime, bool kfOpen)
        {
            KfOpenTime = openTime;
            KfOpen = kfOpen;
        }

        /// <summary>当前应显示的图标(对标老端 showIcon:kf_open?151@1:151)。</summary>
        public string GetShowIconType()
        {
            return KfOpen ? ICON_TYPE_KF : ICON_TYPE_LOCAL;
        }

        /// <summary>当前应删除的另一图标(对标老端 showIcon 里 del_icon:kf_open?151:151@1)。</summary>
        public string GetHideIconType()
        {
            return KfOpen ? ICON_TYPE_LOCAL : ICON_TYPE_KF;
        }

        public void Reset()
        {
            KfOpenTime = 0;
            KfOpen = false;
            _goodsDic.Clear();
            _sellGoodsDic.Clear();
            ShelfGoodsInfo = null;
            RecordList = null;
            BuyTimesList = null;
            SeekAllPageTotal = 0;
            SeekAllPageNo = 0;
            SeekAllPageSize = 0;
            SeekListAll = null;
            SeekListMine = null;
        }

        // ======================================================================================
        // 自动循环 轮19 扩展:15100-15120/15122 数据层(死号 15103/15104/15105/15107/15110/15113 不接)。
        // 嵌套结构对标 pt_151.erl item_to_bin_1/3/5/7/8(EquipExtraAttr 二层嵌套)与 item_to_bin_10/11
        // (求购 SeekList,ServerNum:64 独例)。字段名沿用 erl 原文大写驼峰,数值统一用 int(64位id用long)。
        // ======================================================================================

        /// <summary>15102/15104(死)/15109 共享的商品结构(item_to_bin_1/3/5,三者字段完全同构)。</summary>
        public sealed class GoodsEntry
        {
            public long Id;
            public long PlayerId;
            public int TypeId;
            public int GoodsNum;
            public int Rating;
            public int OverallRating;
            public int UnitPrice;
            public int SellType;
            public System.Collections.Generic.List<EquipExtraAttrEntry> EquipExtraAttr;
        }

        /// <summary>装备额外属性(二层嵌套,item_to_bin_2/4/6/8,四处同构)。</summary>
        public sealed class EquipExtraAttrEntry
        {
            public int Color;
            public int TypeId;
            public int AttrId;
            public int AttrVal;
            public int PlusInterval;
            public int PlusUnit;
        }

        /// <summary>15112 交易记录条目(item_to_bin_7,9字段但与 GoodsEntry 形状不同——无 Id/PlayerId/SellType,
        /// 多 Tax/Price/Time)。</summary>
        public sealed class RecordEntry
        {
            public int TypeId;
            public int GoodsNum;
            public int Rating;
            public int OverallRating;
            public int Type;
            public int Tax;
            public int Price;
            public int Time;
            public System.Collections.Generic.List<EquipExtraAttrEntry> EquipExtraAttr;
        }

        /// <summary>15101 一级分类挂单数量条目({Subtype:32,SellNum:32})。</summary>
        public sealed class SellCountEntry
        {
            public int Subtype;
            public int SellNum;
        }

        /// <summary>15114 购买次数条目({Type:8,Times:8,TimesLimit:8})。</summary>
        public sealed class BuyTimesEntry
        {
            public int Type;
            public int Times;
            public int TimesLimit;
        }

        /// <summary>求购统一条目:对标老端 MarketModel.ts:615-626 附近 showIcon 之外的三处"求购列表"形状——
        /// 15118(item_to_bin_10,9字段含 SerId/ServerNum:64)/15119(item_to_bin_11,5字段)/15115 回执
        /// (Id,PlayerId,RoleName,TypeId,GoodsNum,UnitPrice,Time,7字段)三种不同宽度的结构。老端是弱类型
        /// JS/Lua,AddlzGoodInfoAll/AddlzGoodInfoMine(MarketModel.ts:464-480)直接把 15115 回执 unshift 进
        /// 15118/119 填充的 seek_list,字段集不完全一致。本端用同一 SeekEntry 统一收纳,15115/15119 来源的
        /// 条目里 SerId/ServerNum 缺省为 0(不臆造老端没有的数值,15119 来源额外缺 RoleName,留空串)。</summary>
        public sealed class SeekEntry
        {
            public long Id;
            public long SerId;
            public long ServerNum;
            public long PlayerId;
            public string RoleName = "";
            public int TypeId;
            public int GoodsNum;
            public int UnitPrice;
            public int Time;
        }

        // ---- 15101: type -> SellList(对标 MarketModel.ts:158-167 SetGoodsDic/GetGoodsDic) ----
        private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<SellCountEntry>> _goodsDic
            = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<SellCountEntry>>();

        public void SetGoodsDic(int type, System.Collections.Generic.List<SellCountEntry> list) => _goodsDic[type] = list;
        public System.Collections.Generic.List<SellCountEntry> GetGoodsDic(int type)
            => _goodsDic.TryGetValue(type, out var l) ? l : null;

        // ---- 15102: (type,subtype) -> GoodsList(对标 MarketModel.ts:138-148 SetSellGoodsInfo/GetSellGoodsInfo,
        //      key = type*10000+subtype) ----
        private readonly System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<GoodsEntry>> _sellGoodsDic
            = new System.Collections.Generic.Dictionary<long, System.Collections.Generic.List<GoodsEntry>>();

        private static long SellGoodsKey(int type, int subtype) => (long)type * 10000L + subtype;

        /// <summary>对标 MarketModel.ts:138-143 SetSellGoodsInfo:老端每次写入前把整桶 sell_goods_dic_
        /// 清空(ts:139 sell_goods_dic_=[])再写入单一 key——"单桶语义",同一时刻只有最近一次 15102
        /// 回包的 (type,subtype) 桶存活。本端镜像:写前 Clear() 整个 _sellGoodsDic,避免
        /// RemoveSellGoodInfo 的 subtype=0 兜底(ts:390-411)命中已过期的陈旧桶。</summary>
        public void SetSellGoodsInfo(int type, int subtype, System.Collections.Generic.List<GoodsEntry> list)
        {
            _sellGoodsDic.Clear();
            _sellGoodsDic[SellGoodsKey(type, subtype)] = list;
        }
        public System.Collections.Generic.List<GoodsEntry> GetSellGoodsInfo(int type, int subtype)
            => _sellGoodsDic.TryGetValue(SellGoodsKey(type, subtype), out var l) ? l : null;

        /// <summary>对标 MarketModel.ts:390-411 RemoveSellGoodInfo:先按(type,subtype)找,找不到退化到
        /// subtype=0 桶(老端兜底),命中后按 id 摘除首个匹配项。</summary>
        public void RemoveSellGoodInfo(int type, int subtype, long id)
        {
            var list = GetSellGoodsInfo(type, subtype) ?? GetSellGoodsInfo(type, 0);
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id == id) { list.RemoveAt(i); break; }
            }
        }

        // ---- 15109: 我的上架列表(对标 MarketModel.ts:150-156 SetShelfGoodsInfo/GetShelfGoodsInfo) ----
        public System.Collections.Generic.List<GoodsEntry> ShelfGoodsInfo;

        public void SetShelfGoodsInfo(System.Collections.Generic.List<GoodsEntry> list) => ShelfGoodsInfo = list;

        /// <summary>对标 MarketModel.ts:413-428 RemoveShelfGoodInfo:按 id 摘除首个匹配项;列表未加载(null)不动。
        /// 存档:老端 ts:418 循环游标从 i=1 起(`let i = 1`),而老端数组按 JS 0-based 语义存取,等于
        /// 首元素(下标 0)永远摘不掉——是老端运行时 bug,非有意设计(同一文件 RemovePlzGoodInfoAll 用
        /// i=0 起,两处不一致即是明证)。本端有意把循环归一成 0-based(见下方 for 起始 i=0),不复现该 bug。</summary>
        public void RemoveShelfGoodInfo(long id)
        {
            if (ShelfGoodsInfo == null) return;
            for (int i = 0; i < ShelfGoodsInfo.Count; i++)
            {
                if (ShelfGoodsInfo[i].Id == id) { ShelfGoodsInfo.RemoveAt(i); break; }
            }
        }

        // ---- 15112: 交易记录(对标老端 on15112 落地前 table.sort 按 time 排序仅供 UI 展示排序,
        //      本轮不移植面板,原始顺序落 Model,排序留 UI 尾包在消费侧做) ----
        public System.Collections.Generic.List<RecordEntry> RecordList;

        public void SetRecordList(System.Collections.Generic.List<RecordEntry> list) => RecordList = list;

        // ---- 15114: 购买次数(裸列表) ----
        public System.Collections.Generic.List<BuyTimesEntry> BuyTimesList;

        public void SetBuyTimesList(System.Collections.Generic.List<BuyTimesEntry> list) => BuyTimesList = list;

        // ---- 15118/15119: 求购列表(对标 MarketModel.ts:122-136 Set/GetSeekMineInfo、
        //      MarketModel.ts:130-136 Set/GetSeekAllInfo)。两个列表在首次对应协议回包前保持 null,
        //      对标老端"info 未拉取则 Add/Remove 直接 return"的守卫(ts:390-480 系列方法开头 if(!info)return)。 ----
        public int SeekAllPageTotal;
        public int SeekAllPageNo;
        public int SeekAllPageSize;
        public System.Collections.Generic.List<SeekEntry> SeekListAll;
        public System.Collections.Generic.List<SeekEntry> SeekListMine;

        public void SetSeekAllInfo(int pageTotal, int pageNo, int pageSize, System.Collections.Generic.List<SeekEntry> list)
        {
            SeekAllPageTotal = pageTotal;
            SeekAllPageNo = pageNo;
            SeekAllPageSize = pageSize;
            SeekListAll = list;
        }

        public void SetSeekMineInfo(System.Collections.Generic.List<SeekEntry> list) => SeekListMine = list;

        /// <summary>对标 MarketModel.ts:464-471 AddlzGoodInfoAll:未拉取过(null)不动;命中则头插。</summary>
        public void AddPlzGoodInfoAll(SeekEntry entry)
        {
            if (SeekListAll == null) return;
            SeekListAll.Insert(0, entry);
        }

        /// <summary>对标 MarketModel.ts:473-480 AddlzGoodInfoMine:未拉取过(null)不动;命中则头插。</summary>
        public void AddPlzGoodInfoMine(SeekEntry entry)
        {
            if (SeekListMine == null) return;
            SeekListMine.Insert(0, entry);
        }

        /// <summary>对标 MarketModel.ts:430-445 RemovePlzGoodInfoAll:按 id 摘除首个匹配项,未拉取(null)不动。</summary>
        public void RemovePlzGoodInfoAll(long id)
        {
            if (SeekListAll == null) return;
            for (int i = 0; i < SeekListAll.Count; i++)
            {
                if (SeekListAll[i].Id == id) { SeekListAll.RemoveAt(i); break; }
            }
        }

        /// <summary>对标 MarketModel.ts:447-462 RemovePlzGoodInfoMine:按 id 摘除首个匹配项,未拉取(null)不动。
        /// 存档:老端 ts:452 循环游标同样从 i=1 起(与 RemoveShelfGoodInfo ts:418 同款 quirk),首元素
        /// (下标 0)永远摘不掉,系 JS 运行时 bug,非有意设计。本端有意把循环归一成 0-based,不复现。</summary>
        public void RemovePlzGoodInfoMine(long id)
        {
            if (SeekListMine == null) return;
            for (int i = 0; i < SeekListMine.Count; i++)
            {
                if (SeekListMine[i].Id == id) { SeekListMine.RemoveAt(i); break; }
            }
        }
    }
}
