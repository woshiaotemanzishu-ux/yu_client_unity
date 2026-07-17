using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.CustomActivity
{
    // P4 填充:见 spec §5。类型化数据段(摇钱树/FTVACTIVENESS/SAIBOTREASURE/绑钻转盘/RED_PACKET_RAIN/HOLYCALL),
    // 由 CustomActivityController.Festival.cs 的 handler 落地。通用容器在 CustomActivityModel.cs(P1)。
    //
    // wire 全部逐字段回 pt_331.erl/pt_332.erl 原文核对(write 子句 + item_to_bin_N 辅助函数),不采信 r17_server
    // 侦察表的简写("同结构"等)。本轮核出的与侦察表不同之处(供报告):
    //  - 33190 的 CumulateReward[] 结构是 {GradeId:16,Times:16,Reward=Obj[],Status:8}(item_to_bin_65),
    //    与 ShowList[] 的 {GradeId:16,IsRare:8,Reward=Obj[]}(item_to_bin_64)**不同**,侦察表"(同结构)"是误记。
    //  - 33165(赛博夺宝界面)Pool[] 元素(item_to_bin_43)字段序是 **Reward=Obj[] 在最前**,GradeId/IsRare/Sort/State
    //    在后,与常见"Reward 殿后"惯例相反,逐字段核对时容易套模板出错。
    //  - 33165 StageS[] 是三层嵌套:StageS[]→{Stage:8,GradeState[]→{GradeStage:8,GradeReward=Obj[],
    //    BuyReward=Obj[],StateStage:8,DiscountState:8}}(item_to_bin_44/45),侦察表未展开到这一层。
    //  - 33167 RewardList[] 元素(item_to_bin_46)比侦察表多一个尾字段 Sort:8。
    //  - 33132(转盘记录)RoleId 是 **32 位**(item_to_bin_28),非本框架内常见的 64 位 RoleId。
    // 事件粒度收敛铁律(spec §0):P4 不新增 GlobalEvent(GlobalEvent.cs 是共享文件,P1 独占),一律复用 P1 已定义的
    // EVT_CUSTOMACT_DETAIL_UPDATE(面板/推送类数据落地的通用信号,baseType/subType)与 EVT_CUSTOMACT_RESULT
    // (带 ErrorCode 的操作结果,baseType/subType/code);红包雨新波次推送(33158)按 spec 明确指示复用
    // EVT_CUSTOMACT_REDPACKET_WAVE(在 Controller.Festival.cs 里 Emit)。
    public sealed partial class CustomActivityModel
    {
        // ============================================================================================
        // 通用:Reward=Obj[] 三元组(pt.erl:352-356 write_object_list,Type:8+ObjectTypeId:32+Num:32)。
        // 命名加 Festival 前缀,避免与 P2/P3/P5/P6 各自复刻的同类读取器在同一个 partial class 内重名冲突。
        // ============================================================================================

        public sealed class FestivalRewardItem
        {
            public int Type;
            public int GoodsId;
            public int Num;
        }

        private static FestivalRewardItem ReadFestivalRewardItem(NetReader r) => new FestivalRewardItem
        {
            Type = r.ReadU8(), GoodsId = r.ReadI32(), Num = r.ReadI32(),
        };

        private static List<FestivalRewardItem> ReadFestivalRewardList(NetReader r) => r.ReadArray(ReadFestivalRewardItem);

        /// <summary>GradeId:16,IsRare:8,Reward=Obj[](item_to_bin_64/67/70/12/14/15 六处共用此结构)。</summary>
        public sealed class FestivalGradeRareReward
        {
            public int GradeId;
            public int IsRare;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
        }

        private static FestivalGradeRareReward ReadFestivalGradeRareReward(NetReader r)
        {
            var e = new FestivalGradeRareReward { GradeId = r.ReadU16(), IsRare = r.ReadU8() };
            e.Reward.AddRange(ReadFestivalRewardList(r));
            return e;
        }

        private static List<FestivalGradeRareReward> ReadFestivalGradeRareRewardList(NetReader r) => r.ReadArray(ReadFestivalGradeRareReward);

        /// <summary>GradeId:16,Times:16,Reward=Obj[],Status:8(item_to_bin_65/68/13 三处共用此结构)。</summary>
        public sealed class FestivalGradeTimesReward
        {
            public int GradeId;
            public int Times;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int Status;
        }

        private static FestivalGradeTimesReward ReadFestivalGradeTimesReward(NetReader r)
        {
            var e = new FestivalGradeTimesReward { GradeId = r.ReadU16(), Times = r.ReadU16() };
            e.Reward.AddRange(ReadFestivalRewardList(r));
            e.Status = r.ReadU8();
            return e;
        }

        private static List<FestivalGradeTimesReward> ReadFestivalGradeTimesRewardList(NetReader r) => r.ReadArray(ReadFestivalGradeTimesReward);

        // ============================================================================================
        // §1 摇钱树 MONEYTREE(50)/MOUNT_TURNTABLE(54)/MONEYTREE_SHOP(89):33190/33191/33192/33168/33231
        // ============================================================================================

        /// <summary>GradeId:16,Reward=Obj[],NeedScore:32,Num:16,MaxNum:16,ClearType:8(item_to_bin_66,33190 Shop[] 元素)。</summary>
        public sealed class MoneyTreeShopItem
        {
            public int GradeId;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int NeedScore;
            public int Num;
            public int MaxNum;
            public int ClearType;
        }

        /// <summary>对标 pt_331.erl write(33190,...)(:1916-1959):BaseType,SubType,ErrorCode,AllTimes,FreeTimes,
        /// ShowList[FestivalGradeRareReward],CumulateReward[FestivalGradeTimesReward],Score,Shop[MoneyTreeShopItem]。</summary>
        public sealed class MoneyTreePanelData
        {
            public int BaseType, SubType, ErrorCode, AllTimes, FreeTimes;
            public readonly List<FestivalGradeRareReward> ShowList = new List<FestivalGradeRareReward>();
            public readonly List<FestivalGradeTimesReward> CumulateReward = new List<FestivalGradeTimesReward>();
            public int Score;
            public readonly List<MoneyTreeShopItem> Shop = new List<MoneyTreeShopItem>();
        }

        /// <summary>对标 write(33191,...)(:1961-1986):ErrorCode 领先(**订正**:r17_server 侦察表标"首字段Code32=N"有误,
        /// 原文 Data 二进制里 ErrorCode 确实是第一个字段)。ErrorCode,BaseType,SubType,AllTimes,FreeTimes,
        /// RewardList[FestivalGradeRareReward],Score。</summary>
        public sealed class MoneyTreeDrawResult
        {
            public int ErrorCode, BaseType, SubType, AllTimes, FreeTimes;
            public readonly List<FestivalGradeRareReward> RewardList = new List<FestivalGradeRareReward>();
            public int Score;
        }

        /// <summary>对标 write(33192,...):ErrorCode,BaseType,SubType,CumulateReward[FestivalGradeTimesReward]。</summary>
        public sealed class MoneyTreeCumulateResult
        {
            public int ErrorCode, BaseType, SubType;
            public readonly List<FestivalGradeTimesReward> CumulateReward = new List<FestivalGradeTimesReward>();
        }

        /// <summary>对标 write(33168,...):BaseType,SubType,GradeId,ErrorCode,Reward=Obj[],Num,Score
        /// (注意 ErrorCode 在第 4 位,非开头也非末尾)。</summary>
        public sealed class MoneyTreeShopResult
        {
            public int BaseType, SubType, GradeId, ErrorCode;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int Num, Score;
        }

        /// <summary>对标 pt_332.erl write(33231,...):BaseType,SubType,Currency(无 ErrorCode,纯展示推送)。</summary>
        public sealed class MoneyTreeCurrency
        {
            public int BaseType, SubType, Currency;
        }

        private readonly Dictionary<long, MoneyTreePanelData> _moneyTreePanel = new Dictionary<long, MoneyTreePanelData>();
        private readonly Dictionary<long, MoneyTreeDrawResult> _moneyTreeDrawResult = new Dictionary<long, MoneyTreeDrawResult>();
        private readonly Dictionary<long, MoneyTreeCumulateResult> _moneyTreeCumulateResult = new Dictionary<long, MoneyTreeCumulateResult>();
        private readonly Dictionary<long, MoneyTreeShopResult> _moneyTreeShopResult = new Dictionary<long, MoneyTreeShopResult>();
        private readonly Dictionary<long, MoneyTreeCurrency> _moneyTreeCurrency = new Dictionary<long, MoneyTreeCurrency>();

        public static MoneyTreePanelData ReadMoneyTreePanel(NetReader r)
        {
            var d = new MoneyTreePanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), ErrorCode = r.ReadI32(),
                AllTimes = r.ReadU16(), FreeTimes = r.ReadU16(),
            };
            d.ShowList.AddRange(ReadFestivalGradeRareRewardList(r));
            d.CumulateReward.AddRange(ReadFestivalGradeTimesRewardList(r));
            d.Score = r.ReadI32();
            int shopCount = r.ReadU16();
            for (int i = 0; i < shopCount; i++)
            {
                d.Shop.Add(new MoneyTreeShopItem { GradeId = r.ReadU16() });
                MoneyTreeShopItem last = d.Shop[d.Shop.Count - 1];
                last.Reward.AddRange(ReadFestivalRewardList(r));
                // 结构体字段序:GradeId,Reward,NeedScore,Num,MaxNum,ClearType(item_to_bin_66)。
                var needScore = r.ReadI32(); var num = r.ReadU16(); var maxNum = r.ReadU16(); var clearType = r.ReadU8();
                last.NeedScore = needScore; last.Num = num; last.MaxNum = maxNum; last.ClearType = clearType;
            }
            return d;
        }

        public void SetMoneyTreePanel(MoneyTreePanelData d) => _moneyTreePanel[Key(d.BaseType, d.SubType)] = d;
        public MoneyTreePanelData GetMoneyTreePanel(int baseType, int subType) =>
            _moneyTreePanel.TryGetValue(Key(baseType, subType), out MoneyTreePanelData d) ? d : null;

        public static MoneyTreeDrawResult ReadMoneyTreeDrawResult(NetReader r)
        {
            var d = new MoneyTreeDrawResult
            {
                ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16(),
                AllTimes = r.ReadU16(), FreeTimes = r.ReadU16(),
            };
            d.RewardList.AddRange(ReadFestivalGradeRareRewardList(r));
            d.Score = r.ReadI32();
            return d;
        }

        public void SetMoneyTreeDrawResult(MoneyTreeDrawResult d) => _moneyTreeDrawResult[Key(d.BaseType, d.SubType)] = d;
        public MoneyTreeDrawResult GetMoneyTreeDrawResult(int baseType, int subType) =>
            _moneyTreeDrawResult.TryGetValue(Key(baseType, subType), out MoneyTreeDrawResult d) ? d : null;

        public static MoneyTreeCumulateResult ReadMoneyTreeCumulateResult(NetReader r)
        {
            var d = new MoneyTreeCumulateResult { ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16() };
            d.CumulateReward.AddRange(ReadFestivalGradeTimesRewardList(r));
            return d;
        }

        public void SetMoneyTreeCumulateResult(MoneyTreeCumulateResult d) => _moneyTreeCumulateResult[Key(d.BaseType, d.SubType)] = d;
        public MoneyTreeCumulateResult GetMoneyTreeCumulateResult(int baseType, int subType) =>
            _moneyTreeCumulateResult.TryGetValue(Key(baseType, subType), out MoneyTreeCumulateResult d) ? d : null;

        public static MoneyTreeShopResult ReadMoneyTreeShopResult(NetReader r)
        {
            var d = new MoneyTreeShopResult { BaseType = r.ReadU16(), SubType = r.ReadU16(), GradeId = r.ReadU16(), ErrorCode = r.ReadI32() };
            d.Reward.AddRange(ReadFestivalRewardList(r));
            d.Num = r.ReadU16();
            d.Score = r.ReadI32();
            return d;
        }

        public void SetMoneyTreeShopResult(MoneyTreeShopResult d) => _moneyTreeShopResult[Key(d.BaseType, d.SubType)] = d;
        public MoneyTreeShopResult GetMoneyTreeShopResult(int baseType, int subType) =>
            _moneyTreeShopResult.TryGetValue(Key(baseType, subType), out MoneyTreeShopResult d) ? d : null;

        public static MoneyTreeCurrency ReadMoneyTreeCurrency(NetReader r) => new MoneyTreeCurrency
        {
            BaseType = r.ReadU16(), SubType = r.ReadU16(), Currency = r.ReadI32(),
        };

        public void SetMoneyTreeCurrency(MoneyTreeCurrency d) => _moneyTreeCurrency[Key(d.BaseType, d.SubType)] = d;
        public MoneyTreeCurrency GetMoneyTreeCurrency(int baseType, int subType) =>
            _moneyTreeCurrency.TryGetValue(Key(baseType, subType), out MoneyTreeCurrency d) ? d : null;

        // ============================================================================================
        // §2 FTVACTIVENESS(56):33193/33194/33195/33196(recv-only 广播)
        // ============================================================================================

        /// <summary>GradeId:16,TriggerType:8,Param:str,Times:16,Reward=Obj[],Status:8(item_to_bin_69,33193 SerRewardList[] 元素)。</summary>
        public sealed class FtvActiveRewardEntry
        {
            public int GradeId;
            public int TriggerType;
            public string Param = "";
            public int Times;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int Status;
        }

        /// <summary>对标 write(33193,...):BaseType,SubType,PersonTimes,ServerTimes,SerRewardList[FtvActiveRewardEntry]
        /// (无 ErrorCode,纯查询回执)。</summary>
        public sealed class FtvActivePanelData
        {
            public int BaseType, SubType, PersonTimes, ServerTimes;
            public readonly List<FtvActiveRewardEntry> SerRewardList = new List<FtvActiveRewardEntry>();
        }

        /// <summary>对标 write(33194,...):ErrorCode,BaseType,SubType,CostType,RewardList[FestivalGradeRareReward],PersonTimes。</summary>
        public sealed class FtvActiveSubmitResult
        {
            public int ErrorCode, BaseType, SubType, CostType;
            public readonly List<FestivalGradeRareReward> RewardList = new List<FestivalGradeRareReward>();
            public int PersonTimes;
        }

        /// <summary>对标 write(33195,...):ErrorCode,BaseType,SubType,GradeId,Reward=Obj[]。</summary>
        public sealed class FtvActiveServerClaimResult
        {
            public int ErrorCode, BaseType, SubType, GradeId;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
        }

        /// <summary>对标 write(33196,...):BaseType,SubType,ServerTimes,IsAsk,TriggerTypeList[u8 标量](item_to_bin_71,
        /// recv-only 广播,老端触发类型通知)。</summary>
        public sealed class FtvActiveTriggerPush
        {
            public int BaseType, SubType, ServerTimes, IsAsk;
            public readonly List<int> TriggerTypeList = new List<int>();
        }

        private readonly Dictionary<long, FtvActivePanelData> _ftvActivePanel = new Dictionary<long, FtvActivePanelData>();
        private readonly Dictionary<long, FtvActiveSubmitResult> _ftvActiveSubmitResult = new Dictionary<long, FtvActiveSubmitResult>();
        private readonly Dictionary<long, FtvActiveServerClaimResult> _ftvActiveServerClaimResult = new Dictionary<long, FtvActiveServerClaimResult>();
        private readonly Dictionary<long, FtvActiveTriggerPush> _ftvActiveTriggerPush = new Dictionary<long, FtvActiveTriggerPush>();

        public static FtvActivePanelData ReadFtvActivePanel(NetReader r)
        {
            var d = new FtvActivePanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), PersonTimes = r.ReadU16(), ServerTimes = r.ReadU16(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                var e = new FtvActiveRewardEntry { GradeId = r.ReadU16(), TriggerType = r.ReadU8(), Param = r.ReadString(), Times = r.ReadU16() };
                e.Reward.AddRange(ReadFestivalRewardList(r));
                e.Status = r.ReadU8();
                d.SerRewardList.Add(e);
            }
            return d;
        }

        public void SetFtvActivePanel(FtvActivePanelData d) => _ftvActivePanel[Key(d.BaseType, d.SubType)] = d;
        public FtvActivePanelData GetFtvActivePanel(int baseType, int subType) =>
            _ftvActivePanel.TryGetValue(Key(baseType, subType), out FtvActivePanelData d) ? d : null;

        public static FtvActiveSubmitResult ReadFtvActiveSubmitResult(NetReader r)
        {
            var d = new FtvActiveSubmitResult { ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16(), CostType = r.ReadU8() };
            d.RewardList.AddRange(ReadFestivalGradeRareRewardList(r));
            d.PersonTimes = r.ReadU16();
            return d;
        }

        public void SetFtvActiveSubmitResult(FtvActiveSubmitResult d) => _ftvActiveSubmitResult[Key(d.BaseType, d.SubType)] = d;
        public FtvActiveSubmitResult GetFtvActiveSubmitResult(int baseType, int subType) =>
            _ftvActiveSubmitResult.TryGetValue(Key(baseType, subType), out FtvActiveSubmitResult d) ? d : null;

        public static FtvActiveServerClaimResult ReadFtvActiveServerClaimResult(NetReader r)
        {
            var d = new FtvActiveServerClaimResult { ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16(), GradeId = r.ReadU16() };
            d.Reward.AddRange(ReadFestivalRewardList(r));
            return d;
        }

        public void SetFtvActiveServerClaimResult(FtvActiveServerClaimResult d) => _ftvActiveServerClaimResult[Key(d.BaseType, d.SubType)] = d;
        public FtvActiveServerClaimResult GetFtvActiveServerClaimResult(int baseType, int subType) =>
            _ftvActiveServerClaimResult.TryGetValue(Key(baseType, subType), out FtvActiveServerClaimResult d) ? d : null;

        public static FtvActiveTriggerPush ReadFtvActiveTriggerPush(NetReader r)
        {
            var d = new FtvActiveTriggerPush
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), ServerTimes = r.ReadU16(), IsAsk = r.ReadU8(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++) d.TriggerTypeList.Add(r.ReadU8());
            return d;
        }

        public void SetFtvActiveTriggerPush(FtvActiveTriggerPush d) => _ftvActiveTriggerPush[Key(d.BaseType, d.SubType)] = d;
        public FtvActiveTriggerPush GetFtvActiveTriggerPush(int baseType, int subType) =>
            _ftvActiveTriggerPush.TryGetValue(Key(baseType, subType), out FtvActiveTriggerPush d) ? d : null;

        // ============================================================================================
        // §3 SAIBOTREASURE(58,赛博夺宝):33165/33166/33167 —— 本包嵌套最深(StageS→GradeState 三层)
        // ============================================================================================

        /// <summary>item_to_bin_43,33165 Pool[] 元素:**Reward=Obj[] 在最前**,GradeId:16,IsRare:8,Sort:8,State:8。</summary>
        public sealed class SaiboPoolItem
        {
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int GradeId, IsRare, Sort, State;
        }

        /// <summary>item_to_bin_45,33165 StageS[].GradeState[] 元素:GradeStage:8,GradeReward=Obj[],
        /// BuyReward=Obj[],StateStage:8,DiscountState:8。</summary>
        public sealed class SaiboGradeState
        {
            public int GradeStage;
            public readonly List<FestivalRewardItem> GradeReward = new List<FestivalRewardItem>();
            public readonly List<FestivalRewardItem> BuyReward = new List<FestivalRewardItem>();
            public int StateStage, DiscountState;
        }

        /// <summary>item_to_bin_44,33165 StageS[] 元素:Stage:8,GradeState[SaiboGradeState]。</summary>
        public sealed class SaiboStageItem
        {
            public int Stage;
            public readonly List<SaiboGradeState> GradeState = new List<SaiboGradeState>();
        }

        /// <summary>对标 write(33165,...):BaseType,SubType,Wave:8,AllTimes:16,TodayDrawtimes:16,
        /// Pool[SaiboPoolItem],StageS[SaiboStageItem]。</summary>
        public sealed class SaiboPanelData
        {
            public int BaseType, SubType, Wave, AllTimes, TodayDrawtimes;
            public readonly List<SaiboPoolItem> Pool = new List<SaiboPoolItem>();
            public readonly List<SaiboStageItem> StageS = new List<SaiboStageItem>();
        }

        /// <summary>对标 write(33166,...):ErrorCode,BaseType,SubType,Stage,GradeStage,Reward=Obj[],Buy
        /// (ErrorCode 开头,Buy 在尾,即 spec 提示的"ErrorCode 开头但含 Buy 尾字段")。</summary>
        public sealed class SaiboStageResult
        {
            public int ErrorCode, BaseType, SubType, Stage, GradeStage;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int Buy;
        }

        /// <summary>item_to_bin_46,33167 RewardList[] 元素:GradeId:16,IsRare:8,Reward=Obj[],Sort:8
        /// (比 r17_server 侦察表多一个尾字段 Sort,故不复用 FestivalGradeRareReward)。</summary>
        public sealed class SaiboDrawRewardEntry
        {
            public int GradeId, IsRare;
            public readonly List<FestivalRewardItem> Reward = new List<FestivalRewardItem>();
            public int Sort;
        }

        /// <summary>对标 write(33167,...):ErrorCode,BaseType,SubType,AllTimes,TodayDrawtimes,RewardList[SaiboDrawRewardEntry]。</summary>
        public sealed class SaiboDrawResult
        {
            public int ErrorCode, BaseType, SubType, AllTimes, TodayDrawtimes;
            public readonly List<SaiboDrawRewardEntry> RewardList = new List<SaiboDrawRewardEntry>();
        }

        private readonly Dictionary<long, SaiboPanelData> _saiboPanel = new Dictionary<long, SaiboPanelData>();
        private readonly Dictionary<long, SaiboStageResult> _saiboStageResult = new Dictionary<long, SaiboStageResult>();
        private readonly Dictionary<long, SaiboDrawResult> _saiboDrawResult = new Dictionary<long, SaiboDrawResult>();

        private static SaiboPoolItem ReadSaiboPoolItem(NetReader r)
        {
            var e = new SaiboPoolItem();
            e.Reward.AddRange(ReadFestivalRewardList(r)); // Reward 在最前,见类注释
            e.GradeId = r.ReadU16(); e.IsRare = r.ReadU8(); e.Sort = r.ReadU8(); e.State = r.ReadU8();
            return e;
        }

        private static SaiboGradeState ReadSaiboGradeState(NetReader r)
        {
            var e = new SaiboGradeState { GradeStage = r.ReadU8() };
            e.GradeReward.AddRange(ReadFestivalRewardList(r));
            e.BuyReward.AddRange(ReadFestivalRewardList(r));
            e.StateStage = r.ReadU8(); e.DiscountState = r.ReadU8();
            return e;
        }

        private static SaiboStageItem ReadSaiboStageItem(NetReader r)
        {
            var e = new SaiboStageItem { Stage = r.ReadU8() };
            e.GradeState.AddRange(r.ReadArray(ReadSaiboGradeState));
            return e;
        }

        public static SaiboPanelData ReadSaiboPanel(NetReader r)
        {
            var d = new SaiboPanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Wave = r.ReadU8(),
                AllTimes = r.ReadU16(), TodayDrawtimes = r.ReadU16(),
            };
            d.Pool.AddRange(r.ReadArray(ReadSaiboPoolItem));
            d.StageS.AddRange(r.ReadArray(ReadSaiboStageItem));
            return d;
        }

        public void SetSaiboPanel(SaiboPanelData d) => _saiboPanel[Key(d.BaseType, d.SubType)] = d;
        public SaiboPanelData GetSaiboPanel(int baseType, int subType) =>
            _saiboPanel.TryGetValue(Key(baseType, subType), out SaiboPanelData d) ? d : null;

        public static SaiboStageResult ReadSaiboStageResult(NetReader r)
        {
            var d = new SaiboStageResult
            {
                ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16(),
                Stage = r.ReadU8(), GradeStage = r.ReadU8(),
            };
            d.Reward.AddRange(ReadFestivalRewardList(r));
            d.Buy = r.ReadU8();
            return d;
        }

        public void SetSaiboStageResult(SaiboStageResult d) => _saiboStageResult[Key(d.BaseType, d.SubType)] = d;
        public SaiboStageResult GetSaiboStageResult(int baseType, int subType) =>
            _saiboStageResult.TryGetValue(Key(baseType, subType), out SaiboStageResult d) ? d : null;

        public static SaiboDrawResult ReadSaiboDrawResult(NetReader r)
        {
            var d = new SaiboDrawResult
            {
                ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16(),
                AllTimes = r.ReadU16(), TodayDrawtimes = r.ReadU16(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                var e = new SaiboDrawRewardEntry { GradeId = r.ReadU16(), IsRare = r.ReadU8() };
                e.Reward.AddRange(ReadFestivalRewardList(r));
                e.Sort = r.ReadU8();
                d.RewardList.Add(e);
            }
            return d;
        }

        public void SetSaiboDrawResult(SaiboDrawResult d) => _saiboDrawResult[Key(d.BaseType, d.SubType)] = d;
        public SaiboDrawResult GetSaiboDrawResult(int baseType, int subType) =>
            _saiboDrawResult.TryGetValue(Key(baseType, subType), out SaiboDrawResult d) ? d : null;

        // ============================================================================================
        // §4 绑钻转盘 TURNTABLE(28):33130(NTimesList+RewardList 双数组,不嵌套)/33131/33132
        // ============================================================================================

        public sealed class BindDiamondRewardItem { public int GoodsId, GoodsNum; }

        /// <summary>对标 write(33130,...):BaseType,SubType,TicketNum,TotalTickets,TotalLeftTickets,ChargeGold,
        /// NeedGold,NTimesList[u8 标量],RewardList[BindDiamondRewardItem]——**两个平级数组**,RewardList 不嵌套在
        /// NTimesList 元素内(逐字段核对 pt_331.erl:850-886,订正 r17_server 侦察表箭头写法带来的"嵌套"误读)。</summary>
        public sealed class BindDiamondPanelData
        {
            public int BaseType, SubType, TicketNum, TotalTickets, TotalLeftTickets, ChargeGold, NeedGold;
            public readonly List<int> NTimesList = new List<int>();
            public readonly List<BindDiamondRewardItem> RewardList = new List<BindDiamondRewardItem>();
        }

        /// <summary>对标 write(33131,...):BaseType,SubType,GoodsId,GoodsNum,NTimes,TicketNum,TotalLeftTickets
        /// (无 ErrorCode;C2S read(33131) 只有 [BaseType,SubType] 两个字段,服务端自行判定次数与花费)。</summary>
        public sealed class BindDiamondDrawResult
        {
            public int BaseType, SubType, GoodsId, GoodsNum, NTimes, TicketNum, TotalLeftTickets;
        }

        /// <summary>item_to_bin_28,33132 List[] 元素:RoleId **32 位**(非本框架常见的 64 位)+RoleName:str+
        /// GoodsId:32+GoodsNum:32+NTimes:8。</summary>
        public sealed class BindDiamondRecordEntry
        {
            public int RoleId;
            public string RoleName = "";
            public int GoodsId, GoodsNum, NTimes;
        }

        public sealed class BindDiamondRecordData
        {
            public int BaseType, SubType;
            public readonly List<BindDiamondRecordEntry> List = new List<BindDiamondRecordEntry>();
        }

        private readonly Dictionary<long, BindDiamondPanelData> _bindDiamondPanel = new Dictionary<long, BindDiamondPanelData>();
        private readonly Dictionary<long, BindDiamondDrawResult> _bindDiamondDrawResult = new Dictionary<long, BindDiamondDrawResult>();
        private readonly Dictionary<long, BindDiamondRecordData> _bindDiamondRecord = new Dictionary<long, BindDiamondRecordData>();

        public static BindDiamondPanelData ReadBindDiamondPanel(NetReader r)
        {
            var d = new BindDiamondPanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), TicketNum = r.ReadI32(), TotalTickets = r.ReadI32(),
                TotalLeftTickets = r.ReadI32(), ChargeGold = r.ReadI32(), NeedGold = r.ReadU16(),
            };
            int nTimesCount = r.ReadU16();
            for (int i = 0; i < nTimesCount; i++) d.NTimesList.Add(r.ReadU8());
            int rewardCount = r.ReadU16();
            for (int i = 0; i < rewardCount; i++) d.RewardList.Add(new BindDiamondRewardItem { GoodsId = r.ReadI32(), GoodsNum = r.ReadI32() });
            return d;
        }

        public void SetBindDiamondPanel(BindDiamondPanelData d) => _bindDiamondPanel[Key(d.BaseType, d.SubType)] = d;
        public BindDiamondPanelData GetBindDiamondPanel(int baseType, int subType) =>
            _bindDiamondPanel.TryGetValue(Key(baseType, subType), out BindDiamondPanelData d) ? d : null;

        public static BindDiamondDrawResult ReadBindDiamondDrawResult(NetReader r) => new BindDiamondDrawResult
        {
            BaseType = r.ReadU16(), SubType = r.ReadU16(), GoodsId = r.ReadI32(), GoodsNum = r.ReadI32(),
            NTimes = r.ReadU8(), TicketNum = r.ReadI32(), TotalLeftTickets = r.ReadI32(),
        };

        public void SetBindDiamondDrawResult(BindDiamondDrawResult d) => _bindDiamondDrawResult[Key(d.BaseType, d.SubType)] = d;
        public BindDiamondDrawResult GetBindDiamondDrawResult(int baseType, int subType) =>
            _bindDiamondDrawResult.TryGetValue(Key(baseType, subType), out BindDiamondDrawResult d) ? d : null;

        public static BindDiamondRecordData ReadBindDiamondRecord(NetReader r)
        {
            var d = new BindDiamondRecordData { BaseType = r.ReadU16(), SubType = r.ReadU16() };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                d.List.Add(new BindDiamondRecordEntry
                {
                    RoleId = r.ReadI32(), RoleName = r.ReadString(), GoodsId = r.ReadI32(), GoodsNum = r.ReadI32(), NTimes = r.ReadU8(),
                });
            }
            return d;
        }

        public void SetBindDiamondRecord(BindDiamondRecordData d) => _bindDiamondRecord[Key(d.BaseType, d.SubType)] = d;
        public BindDiamondRecordData GetBindDiamondRecord(int baseType, int subType) =>
            _bindDiamondRecord.TryGetValue(Key(baseType, subType), out BindDiamondRecordData d) ? d : null;

        // ============================================================================================
        // §5 RED_PACKET_RAIN(82,红包雨):33155/33157/33158 —— 老端/服务端本号无 BaseType 字段,一律只按
        // SubType 存取(不复用通用 Key(base,sub),下方三个字典改用 int subType 直接做 key)。
        // ============================================================================================

        /// <summary>item_to_bin_40,33155 WaveReceive[] 元素:Wave2:8,IsReceive:8,Rewards=Obj[]。</summary>
        public sealed class RedRainWaveReceive
        {
            public int Wave2, IsReceive;
            public readonly List<FestivalRewardItem> Rewards = new List<FestivalRewardItem>();
        }

        /// <summary>对标 write(33155,...):SubType,ActValue,Wave,StartTime,ClearType,WaveReceive[RedRainWaveReceive]
        /// (**无 BaseType**;C2S read(33155) 同样只读 SubType,老端发送 fmt 表核实为单参 "h",Core.cs 已按此发送)。</summary>
        public sealed class RedRainPanelData
        {
            public int SubType, ActValue, Wave, StartTime, ClearType;
            public readonly List<RedRainWaveReceive> WaveReceive = new List<RedRainWaveReceive>();
        }

        /// <summary>对标 write(33157,...):Errcode,SubType,Wave,Rewards=Obj[](同样无 BaseType)。</summary>
        public sealed class RedRainGrabResult
        {
            public int Errcode, SubType, Wave;
            public readonly List<FestivalRewardItem> Rewards = new List<FestivalRewardItem>();
        }

        /// <summary>对标 write(33158,...):SubType,Wave,StartTime(**recv-only** 3 字段,新波次开始推送)。服务端
        /// lib_red_envelopes_mod.erl:302 存在错用 16 字段调用本号的线上 bug(应为 33902),与本号定义结构无关,
        /// 客户端只按这里的 3 字段权威结构解析。</summary>
        public sealed class RedRainWavePush
        {
            public int SubType, Wave, StartTime;
        }

        private readonly Dictionary<int, RedRainPanelData> _redRainPanel = new Dictionary<int, RedRainPanelData>();
        private readonly Dictionary<int, RedRainGrabResult> _redRainGrabResult = new Dictionary<int, RedRainGrabResult>();
        private readonly Dictionary<int, RedRainWavePush> _redRainWavePush = new Dictionary<int, RedRainWavePush>();

        public static RedRainPanelData ReadRedRainPanel(NetReader r)
        {
            var d = new RedRainPanelData
            {
                SubType = r.ReadU16(), ActValue = r.ReadI32(), Wave = r.ReadU8(),
                StartTime = r.ReadI32(), ClearType = r.ReadU8(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                var e = new RedRainWaveReceive { Wave2 = r.ReadU8(), IsReceive = r.ReadU8() };
                e.Rewards.AddRange(ReadFestivalRewardList(r));
                d.WaveReceive.Add(e);
            }
            return d;
        }

        public void SetRedRainPanel(RedRainPanelData d) => _redRainPanel[d.SubType] = d;
        public RedRainPanelData GetRedRainPanel(int subType) => _redRainPanel.TryGetValue(subType, out RedRainPanelData d) ? d : null;

        public static RedRainGrabResult ReadRedRainGrabResult(NetReader r)
        {
            var d = new RedRainGrabResult { Errcode = r.ReadI32(), SubType = r.ReadU16(), Wave = r.ReadU8() };
            d.Rewards.AddRange(ReadFestivalRewardList(r));
            return d;
        }

        public void SetRedRainGrabResult(RedRainGrabResult d) => _redRainGrabResult[d.SubType] = d;
        public RedRainGrabResult GetRedRainGrabResult(int subType) => _redRainGrabResult.TryGetValue(subType, out RedRainGrabResult d) ? d : null;

        public static RedRainWavePush ReadRedRainWavePush(NetReader r) => new RedRainWavePush
        {
            SubType = r.ReadU16(), Wave = r.ReadU8(), StartTime = r.ReadI32(),
        };

        public void SetRedRainWavePush(RedRainWavePush d) => _redRainWavePush[d.SubType] = d;
        public RedRainWavePush GetRedRainWavePush(int subType) => _redRainWavePush.TryGetValue(subType, out RedRainWavePush d) ? d : null;

        // ============================================================================================
        // §6 HOLYCALL(67,神圣召唤):33221(四嵌套+RareDrawTimes 尾字段)/33222
        // ============================================================================================

        /// <summary>对标 pt_332.erl write(33221,...)(:633-676):BaseType,SubType,ErrorCode,AllTimes,FreeTimes,
        /// ShowList[FestivalGradeRareReward],CumulateReward[FestivalGradeTimesReward],RarePool[FestivalGradeRareReward],
        /// RareDrawTimes(**尾字段,不在任何数组内**)。</summary>
        public sealed class HolyCallPanelData
        {
            public int BaseType, SubType, ErrorCode, AllTimes, FreeTimes;
            public readonly List<FestivalGradeRareReward> ShowList = new List<FestivalGradeRareReward>();
            public readonly List<FestivalGradeTimesReward> CumulateReward = new List<FestivalGradeTimesReward>();
            public readonly List<FestivalGradeRareReward> RarePool = new List<FestivalGradeRareReward>();
            public int RareDrawTimes;
        }

        /// <summary>对标 write(33222,...):ErrorCode,BaseType,SubType,RareDrawTimes,RewardList[FestivalGradeRareReward]。</summary>
        public sealed class HolyCallRareDrawResult
        {
            public int ErrorCode, BaseType, SubType, RareDrawTimes;
            public readonly List<FestivalGradeRareReward> RewardList = new List<FestivalGradeRareReward>();
        }

        private readonly Dictionary<long, HolyCallPanelData> _holyCallPanel = new Dictionary<long, HolyCallPanelData>();
        private readonly Dictionary<long, HolyCallRareDrawResult> _holyCallRareDrawResult = new Dictionary<long, HolyCallRareDrawResult>();

        public static HolyCallPanelData ReadHolyCallPanel(NetReader r)
        {
            var d = new HolyCallPanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), ErrorCode = r.ReadI32(),
                AllTimes = r.ReadU16(), FreeTimes = r.ReadU16(),
            };
            d.ShowList.AddRange(ReadFestivalGradeRareRewardList(r));
            d.CumulateReward.AddRange(ReadFestivalGradeTimesRewardList(r));
            d.RarePool.AddRange(ReadFestivalGradeRareRewardList(r));
            d.RareDrawTimes = r.ReadU16();
            return d;
        }

        public void SetHolyCallPanel(HolyCallPanelData d) => _holyCallPanel[Key(d.BaseType, d.SubType)] = d;
        public HolyCallPanelData GetHolyCallPanel(int baseType, int subType) =>
            _holyCallPanel.TryGetValue(Key(baseType, subType), out HolyCallPanelData d) ? d : null;

        public static HolyCallRareDrawResult ReadHolyCallRareDrawResult(NetReader r)
        {
            var d = new HolyCallRareDrawResult { ErrorCode = r.ReadI32(), BaseType = r.ReadU16(), SubType = r.ReadU16(), RareDrawTimes = r.ReadU16() };
            d.RewardList.AddRange(ReadFestivalGradeRareRewardList(r));
            return d;
        }

        public void SetHolyCallRareDrawResult(HolyCallRareDrawResult d) => _holyCallRareDrawResult[Key(d.BaseType, d.SubType)] = d;
        public HolyCallRareDrawResult GetHolyCallRareDrawResult(int baseType, int subType) =>
            _holyCallRareDrawResult.TryGetValue(Key(baseType, subType), out HolyCallRareDrawResult d) ? d : null;

        // ============================================================================================
        // §7 生命周期(P4 段,已挂钩):共享的 CustomActivityModel.Clear()(P1,CustomActivityModel.cs)
        // 〔轮17收口〕已在级联里调用本方法,断线/登出会随 Instance.Clear() 一并清空;CliVerify Case 里同时
        // 保留直接调用本方法单段复位的用法。
        // ============================================================================================

        public void ClearFestival()
        {
            _moneyTreePanel.Clear(); _moneyTreeDrawResult.Clear(); _moneyTreeCumulateResult.Clear();
            _moneyTreeShopResult.Clear(); _moneyTreeCurrency.Clear();
            _ftvActivePanel.Clear(); _ftvActiveSubmitResult.Clear(); _ftvActiveServerClaimResult.Clear(); _ftvActiveTriggerPush.Clear();
            _saiboPanel.Clear(); _saiboStageResult.Clear(); _saiboDrawResult.Clear();
            _bindDiamondPanel.Clear(); _bindDiamondDrawResult.Clear(); _bindDiamondRecord.Clear();
            _redRainPanel.Clear(); _redRainGrabResult.Clear(); _redRainWavePush.Clear();
            _holyCallPanel.Clear(); _holyCallRareDrawResult.Clear();
        }
    }
}
