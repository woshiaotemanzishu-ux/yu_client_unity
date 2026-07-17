using System.Collections.Generic;
using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 定制活动跨服+榜(自动循环 轮17 P6)类型化数据段:KFGROUPBUY(88,33227/33228/33229/33230/33267)+
    /// 消费/鲜花榜(224xx,22400/22403/22405)。通用容器(ActList/Detail/ClaimResult/AllCount)在
    /// CustomActivityModel.cs(P1)。TopPlayer(22500-22505)补全数据不在本文件——TopPlayerModel.cs 不在本包
    /// 可写文件范围内(任务只放行 CustomActivityController.Kf.cs / CustomActivityModel.Kf.cs /
    /// CustomActKfRankCase.cs / TopPlayerController.cs 四个文件),22503/22504 的落地复用本文件之外、P1 已提供的
    /// CustomActivityModel.SetClaimResult/GetClaimResult 公开 API(baseType=TopPlayerModel.ACT_BASE_TYPE);
    /// 22505 的 GetWay 数据只能落在 TopPlayerController.cs 自己的私有字段里,详见该文件头注释——此处不重复。
    /// </summary>
    public sealed partial class CustomActivityModel
    {
        // ============================================================================================
        // §K1 跨服团购(KFGROUPBUY=88)。wire 全字段回 pt_332.erl 原文核(item_to_bin_20/21/22/23,
        // pt_332.erl:1922-1987;write 33227/33228/33229/33230:797-874;33267:1629-1641)。
        // ============================================================================================

        /// <summary>对标 pt_332.erl item_to_bin_20(1922-1934):33227 GpGoods 单条。</summary>
        public sealed class KfGroupBuyGrade
        {
            public int GradeId;
            public int FirstBuyCount;
            public int TailBuyCount;
            public int BuyNum;
        }

        public sealed class KfGroupBuyInfo
        {
            public int BaseType;
            public int SubType;
            public readonly List<KfGroupBuyGrade> GpGoods = new List<KfGroupBuyGrade>();
            public long LastShoutTime;
        }

        private readonly Dictionary<long, KfGroupBuyInfo> _kfGroupBuyInfo = new Dictionary<long, KfGroupBuyInfo>();

        /// <summary>33227 信息整份替换(对标老端 On33227 落 data.gp_goods/data.last_shout_time,ts:2284-2296)。</summary>
        public void SetKfGroupBuyInfo(KfGroupBuyInfo info) => _kfGroupBuyInfo[Key(info.BaseType, info.SubType)] = info;

        public KfGroupBuyInfo GetKfGroupBuyInfo(int baseType, int subType) =>
            _kfGroupBuyInfo.TryGetValue(Key(baseType, subType), out KfGroupBuyInfo v) ? v : null;

        /// <summary>33230(recv-only 购买数广播):原地更新已存在活动的对应档位 BuyNum,活动/档位不存在则忽略
        /// (对标老端 On33230:`if (data && data.gp_goods)` 才处理,ts:2319-2332)。</summary>
        public void UpdateKfGroupBuyCount(int baseType, int subType, int gradeId, int buyNum)
        {
            KfGroupBuyInfo info = GetKfGroupBuyInfo(baseType, subType);
            if (info == null) return;
            for (int i = 0; i < info.GpGoods.Count; i++)
            {
                if (info.GpGoods[i].GradeId == gradeId) { info.GpGoods[i].BuyNum = buyNum; return; }
            }
        }

        /// <summary>33267(喊话回执):仅更新已存在活动的 LastShoutTime,不存在则忽略(对标老端 On33267:
        /// `if (data) data.last_shout_time=...`,ts:2334-2345)。无论 Code 是否成功都执行——老端该处紧跟在
        /// 错误提示之后、没有 return/else 拦截,失败包同样会跑到这一行(ts 原文如此)。</summary>
        public void UpdateKfGroupBuyShoutTime(int baseType, int subType, long lastShoutTime)
        {
            KfGroupBuyInfo info = GetKfGroupBuyInfo(baseType, subType);
            if (info == null) return;
            info.LastShoutTime = lastShoutTime;
        }

        /// <summary>对标 pt_332.erl item_to_bin_22/23(1974-1987):FirstBuy/TailBuy 子数组元素仅 GapTime:32 一个字段。</summary>
        public sealed class KfGroupBuyRecord
        {
            public long RoleId;
            public string RoleName = "";
            public int ServerId;
            public int ServerNum;
            public int GradeId;
            public readonly List<long> FirstBuy = new List<long>();
            public long FirstBuyTime;
            public readonly List<long> TailBuy = new List<long>();
            public long TailBuyTime;
        }

        private readonly Dictionary<long, List<KfGroupBuyRecord>> _kfGroupBuyRecords = new Dictionary<long, List<KfGroupBuyRecord>>();

        /// <summary>33228 记录整份替换(对标老端 On33228 直接 Fire(UP_LOG_LIST,scmd) 无本地落地——Unity 数据层轮
        /// 仍落 Model 供后续 UI 直接读取,ts:2298-2301)。</summary>
        public void SetKfGroupBuyRecords(int baseType, int subType, List<KfGroupBuyRecord> list) =>
            _kfGroupBuyRecords[Key(baseType, subType)] = list;

        public IReadOnlyList<KfGroupBuyRecord> GetKfGroupBuyRecords(int baseType, int subType) =>
            _kfGroupBuyRecords.TryGetValue(Key(baseType, subType), out List<KfGroupBuyRecord> v) ? v : null;

        /// <summary>对标 pt.erl write_object_list(352-356):Type:8,GoodsId(ObjectTypeId):32,Num:32。</summary>
        public sealed class KfGroupBuyReward
        {
            public int Type;
            public long GoodsId;
            public long Num;
        }

        public sealed class KfGroupBuyBuyResult
        {
            public int Code;
            public int BaseType;
            public int SubType;
            public int GradeId;
            public int PurchaseType;
            public int BuyCount;
            public int BuyNum;
            public readonly List<KfGroupBuyReward> RewardList = new List<KfGroupBuyReward>();
        }

        private readonly Dictionary<long, KfGroupBuyBuyResult> _kfGroupBuyBuyResults = new Dictionary<long, KfGroupBuyBuyResult>();

        /// <summary>33229 购买回执:仅 Code==1 落地(对标老端 On33229:error_code!=1 走 ShowError 不落地任何数据,
        /// ts:2303-2317)。</summary>
        public void SetKfGroupBuyBuyResult(KfGroupBuyBuyResult result)
        {
            if (result.Code != 1) return;
            _kfGroupBuyBuyResults[Key(result.BaseType, result.SubType)] = result;
        }

        public KfGroupBuyBuyResult GetKfGroupBuyBuyResult(int baseType, int subType) =>
            _kfGroupBuyBuyResults.TryGetValue(Key(baseType, subType), out KfGroupBuyBuyResult v) ? v : null;

        // ============================================================================================
        // §K2 消费/鲜花榜(224xx)。定案(自动循环 轮17 P6,wire 回 pt_224.erl 原文逐字段核):
        // **22400/22403 实为跨服鲜花榜,22405 才是消费榜。** 证据:①CustomActivityController.ts 注册处 22400/
        // 22403 紧邻注释"//鲜花榜"(ts:2878-2880),22405 紧邻注释"////首发充值消费排行"(ts:2921-2922);
        // ②On22403 联动 FlowerrankModel.GetInstance().SetFlowerRankData(scmd)(ts:1911-1915);③22403 仅在
        // FlowerRankView 里 base_type==2(跨服)时才发,本服走独立协议 22401(FlowerRankView.ts:63-84,不在
        // 本轮号段范围)。**已更名**:Proto.cs 的三个常量已按本 §K2 语义定案改名为 KF_FLOWER_RANK_ERROR=22400/
        // KF_FLOWER_RANK_INFO=22403/CONSUME_RANK_INFO=22405(原 COST_RANK_* 前缀已废弃),与鲜花榜/消费榜的
        // 真实语义对应,本文件与 CustomActivityController.Kf.cs 均按新名引用。
        // Unity 全仓 grep FlowerrankModel/鲜花榜(2026-07-17 核实):无对应 Model,数据落本文件,见下方 TODO。
        // ============================================================================================

        /// <summary>对标 pt_224.erl item_to_bin_5(300-320):22403 RankList 单条。</summary>
        public sealed class FlowerRankRoleEntry
        {
            public long RoleId;
            public int ServerId;
            public int Zone;
            public int ServerNum;
            public string Name = "";
            public long FirstValue;
            public long Rank;
        }

        /// <summary>对标 pt_224.erl item_to_bin_6(321-331):22403 FigureList 单条,Figure 走 write_figure→
        /// 复用 Shenxiao.Common.Proto.FigureProto(与 Marriage/Chat/Team 等既有用法一致)。</summary>
        public sealed class FlowerRankFigureEntry
        {
            public long RoleId;
            public FigureProto Figure;
        }

        /// <summary>对标 pt_224.erl write(22403,...)(84-122):跨服鲜花榜数据。</summary>
        public sealed class FlowerRankInfo
        {
            public long Type;   // rank_type_id,老端 FlowerRankView.ts:63 FLOWER_TYPE=[371,372]
            public int SubType;
            public long SelRank;
            public long SelVal;
            public int SelZone;
            public long Sum;
            public int MaxLen;
            public long RankLimit;
            public readonly List<FlowerRankRoleEntry> RankList = new List<FlowerRankRoleEntry>();
            public readonly List<FlowerRankFigureEntry> FigureList = new List<FlowerRankFigureEntry>();
        }

        // TODO(联动待办,对标老端 On22403 的 FlowerrankModel.SetFlowerRankData 语义等价物,ts:1911-1915):
        // Unity 尚无独立 FlowerrankModel,本段数据暂落 CustomActivityModel;若后续新建 FlowerrankModel,应把
        // 本 §K2 迁移过去并保持 key 语义(Type=rank_type_id)。当前仅单槽缓存(与 TopPlayerModel.LatestRankInfo
        // 同款"最近一次视图数据"简化模式,数据层轮不做多 rank_type 并发缓存)。
        private FlowerRankInfo _flowerRankInfo;
        public void SetFlowerRankInfo(FlowerRankInfo info) => _flowerRankInfo = info;
        public FlowerRankInfo GetFlowerRankInfo() => _flowerRankInfo;

        /// <summary>对标 pt_224.erl item_to_bin_7(332-346):22405 RankList 单条。</summary>
        public sealed class CostRankRoleEntry
        {
            public long RoleId;
            public string Name = "";
            public long FirstValue;
            public long Rank;
        }

        /// <summary>对标 pt_224.erl write(22405,...)(124-155):消费榜/首发充值消费排行,自带 Code(与 22400
        /// 的通用错误码是两回事,22405 出错时 Code 内嵌本包直接可读)。</summary>
        public sealed class CostRankInfo
        {
            public int Code;
            public int Type;
            public int SubType;
            public long RankType;
            public long SelRank;
            public long SelVal;
            public long Sum;
            public int MaxLen;
            public long RankLimit;
            public readonly List<CostRankRoleEntry> RankList = new List<CostRankRoleEntry>();
        }

        private CostRankInfo _costRankInfo;
        public void SetCostRankInfo(CostRankInfo info) => _costRankInfo = info;
        public CostRankInfo GetCostRankInfo() => _costRankInfo;

        // ============================================================================================
        // §K3 生命周期(已挂钩):CustomActivityModel.cs(P1)的 Clear()〔轮17收口〕已在级联里调用 ClearKf(),
        // 断线/登出会随 Instance.Clear() 一并清空;ClearKf() 同时保留独立可调用,供 CustomActKfRankCase
        // 在断言前后自行复位用。
        // ============================================================================================
        public void ClearKf()
        {
            _kfGroupBuyInfo.Clear();
            _kfGroupBuyRecords.Clear();
            _kfGroupBuyBuyResults.Clear();
            _flowerRankInfo = null;
            _costRankInfo = null;
        }
    }
}
