using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.CustomActivity
{
    // P5 商业礼包族(自动循环 轮17):ZERO_MALL=36/FTVINVEST=62/VIPGIFT=71/DAILYSUPPLY=61/NAMEVERIFY=69/
    // 批量兑换/QUESTIONNAIRE=90/MANY_RECHARGE=107/冲级/ADVERTISEMENT=111/RED_ENVELOPE_REBATE=117/CARNIVAL=118/
    // TIRED_CHARGE_POLITE=121/OVER_VIEW=126/RARE_SURFACE=128/33197获奖记录/33115完美情缘/33216封测返还/
    // 15955-15960充值统计。类型化数据段,由 CustomActivityController.Biz.cs 的 handler 落地(通用容器在 P1
    // CustomActivityModel.cs)。字段序全部逐号回 pt_331.erl/pt_332.erl/pt_159.erl 原文 write/item_to_bin_N 定义核对
    // (非仅 r17_server_customactivity.md 侦察表——本轮核对时发现该侦察表 2 处字段序误记,已订正,见下方
    // ZeroMallRewardItem/OverViewRewardItem 注释)。
    //
    // Dictionary key 复用 P1 CustomActivityModel.cs 里的 private static long Key(baseType,subType)(partial
    // class 内 private 成员跨文件可见,不重复定义)。
    public sealed partial class CustomActivityModel
    {
        // ============================================================================================
        // 通用 Reward 三元组(pt:write_object_list,pt.erl:352-356,u16计数前缀 + {Type:8,GoodsId:32,Num:32})。
        // ============================================================================================
        public struct RewardObj
        {
            public int Type;
            public int GoodsId;
            public int Num;
        }

        public static RewardObj ReadRewardObj(NetReader r) => new RewardObj
        {
            Type = r.ReadU8(), GoodsId = (int)r.ReadU32(), Num = (int)r.ReadU32(),
        };

        public static List<RewardObj> ReadRewardObjList(NetReader r) => r.ReadArray(ReadRewardObj);

        // ============================================================================================
        // §B1 ZERO_MALL=36(33136/33137/33138)
        // ============================================================================================

        /// <summary>对标 pt_331.erl item_to_bin_31(pt_331.erl:2605-2633)——**订正**:r17_server_customactivity.md
        /// 侦察表误记为"Grade:16,RewardList(Obj),Rare:8",原文实际与 33104 的 item_to_bin_3 **7字段同形但
        /// ReceiveTime 为 32 位**(33104 对应字段是 ReceiveTimes:16;本号 ReceiveTime:32,pt_331.erl:2627,
        /// 双源:ClientProtocol.json "33136".reward_list[].receive_time="i" 与 .erl 原文互证)——并非"完全同构",
        /// 已回 .erl 原文订正为 8 字段:Grade/FormType/Status/ReceiveTime(32位)/Name/Desc/Condition/Reward。</summary>
        public sealed class ZeroMallRewardItem
        {
            public int Grade;
            public int FormType;
            public int Status;
            public int ReceiveTime; // 32 位(item_to_bin_31,pt_331.erl:2627),非 33104 DetailReward.ReceiveTimes 的 16 位
            public string Name = "";
            public string Desc = "";
            public string Condition = "";
            public string Reward = "";
        }

        public sealed class ZeroMallPanel
        {
            public int SubType;
            public readonly List<ZeroMallRewardItem> RewardList = new List<ZeroMallRewardItem>();
        }

        private readonly Dictionary<int, ZeroMallPanel> _zeroMallPanels = new Dictionary<int, ZeroMallPanel>();

        public void SetZeroMallPanel(ZeroMallPanel panel) => _zeroMallPanels[panel.SubType] = panel;
        public ZeroMallPanel GetZeroMallPanel(int subType) => _zeroMallPanels.TryGetValue(subType, out ZeroMallPanel p) ? p : null;

        // ============================================================================================
        // §B2 FTVINVEST=62(33211 升级落地 + 33212)
        // ============================================================================================

        /// <summary>对标 pt_332.erl write(33211)(pt_332.erl:424-443):升级现有 On33211 后落地(此前只读丢)。
        /// item_to_bin_5 = 单字段 Lv:8(pt_332.erl:1737-1743,非元组)。</summary>
        public sealed class FtvInvestInfo
        {
            public int BaseType;
            public int SubType;
            public readonly List<int> Investments = new List<int>(); // 每档 Lv(u8)
            public long BuyTime;
        }

        private readonly Dictionary<long, FtvInvestInfo> _ftvInvestInfos = new Dictionary<long, FtvInvestInfo>();
        public void SetFtvInvestInfo(FtvInvestInfo info) => _ftvInvestInfos[Key(info.BaseType, info.SubType)] = info;
        public FtvInvestInfo GetFtvInvestInfo(int baseType, int subType) =>
            _ftvInvestInfos.TryGetValue(Key(baseType, subType), out FtvInvestInfo v) ? v : null;

        /// <summary>对标 pt_332.erl write(33212)(pt_332.erl:445-463),RewardList 走标准 pt:write_object_list。</summary>
        public sealed class FtvInvestBuyResult
        {
            public int BaseType;
            public int SubType;
            public int Lv;
            public int LoginDays;
            public readonly List<RewardObj> RewardList = new List<RewardObj>();
        }

        private readonly Dictionary<long, FtvInvestBuyResult> _ftvInvestBuyResults = new Dictionary<long, FtvInvestBuyResult>();
        public void SetFtvInvestBuyResult(FtvInvestBuyResult r) => _ftvInvestBuyResults[Key(r.BaseType, r.SubType)] = r;
        public FtvInvestBuyResult GetFtvInvestBuyResult(int baseType, int subType) =>
            _ftvInvestBuyResults.TryGetValue(Key(baseType, subType), out FtvInvestBuyResult v) ? v : null;

        // ============================================================================================
        // §B3 VIPGIFT=71(33215),NowCost 走标准 pt:write_object_list(pt_332.erl:502-518)。
        // ============================================================================================
        public sealed class VipGiftInfo
        {
            public int BaseType;
            public int SubType;
            public int Grade;
            public readonly List<RewardObj> NowCost = new List<RewardObj>();
        }

        private readonly Dictionary<long, VipGiftInfo> _vipGiftInfos = new Dictionary<long, VipGiftInfo>();
        public void SetVipGiftInfo(VipGiftInfo v) => _vipGiftInfos[Key(v.BaseType, v.SubType)] = v;
        public VipGiftInfo GetVipGiftInfo(int baseType, int subType) =>
            _vipGiftInfos.TryGetValue(Key(baseType, subType), out VipGiftInfo v) ? v : null;

        // ============================================================================================
        // §B4 DAILYSUPPLY=61(33209)。wire 双向均无 BaseType/SubType(pt_332.erl:416-422,read(33209,_)->{ok,[]}),
        // 全局单值。
        // ============================================================================================
        public int DailySupplyLiveness { get; private set; }
        public void SetDailySupplyLiveness(int liveness) => DailySupplyLiveness = liveness;

        // ============================================================================================
        // §B5 NAMEVERIFY=69(33169)。读写均空包(pt_331.erl:1507-1511),无字段可落地,只记最近一次确认时间戳。
        // ============================================================================================
        public long NameVerifyConfirmedAt { get; private set; }
        public void MarkNameVerifyConfirmed(long nowSec) => NameVerifyConfirmedAt = nowSec;

        // ============================================================================================
        // §B6 批量兑换(FTVSHOP/FTVEXCHANGE/ATLISTPURCHASE 共用,33179)。
        // 对标 pt_331.erl write(33179)(pt_331.erl:1734-1748):**ErrorCode,Num,BaseType,SubType,Grade** 顺序。
        // ============================================================================================
        public sealed class BatchExchangeResult
        {
            public int ErrorCode;
            public int Num;
            public int BaseType;
            public int SubType;
            public int Grade;
        }

        private BatchExchangeResult _lastBatchExchange;
        public void SetLastBatchExchange(BatchExchangeResult r) => _lastBatchExchange = r;
        public BatchExchangeResult GetLastBatchExchange() => _lastBatchExchange;

        // ============================================================================================
        // §B7 问卷调查 QUESTIONNAIRE=90(33236)。pt_332.erl write(33236):961-969,ErrorCode,QuestionType。
        // ============================================================================================
        public sealed class QuestionnaireResult
        {
            public int ErrorCode;
            public int QuestionType;
        }

        private QuestionnaireResult _lastQuestionnaire;
        public void SetLastQuestionnaire(QuestionnaireResult r) => _lastQuestionnaire = r;
        public QuestionnaireResult GetLastQuestionnaire() => _lastQuestionnaire;

        // ============================================================================================
        // §B8 MANY_RECHARGE=107(33247),pt_332.erl write(33247):1260-1270,无 Code。
        // ============================================================================================
        public sealed class ManyRechargeInfo
        {
            public int BaseType;
            public int SubType;
            public int Times;
        }

        private readonly Dictionary<long, ManyRechargeInfo> _manyRechargeInfos = new Dictionary<long, ManyRechargeInfo>();
        public void SetManyRechargeInfo(ManyRechargeInfo v) => _manyRechargeInfos[Key(v.BaseType, v.SubType)] = v;
        public ManyRechargeInfo GetManyRechargeInfo(int baseType, int subType) =>
            _manyRechargeInfos.TryGetValue(Key(baseType, subType), out ManyRechargeInfo v) ? v : null;

        // ============================================================================================
        // §B9 冲级礼包(33248)。pt_332.erl write(33248):1272-1280,无 BaseType/SubType/Code,全局单值。
        // ============================================================================================
        public sealed class LevelRushGiftInfo
        {
            public int MinTime;
            public int MaxTime;
        }

        private LevelRushGiftInfo _levelRushGift;
        public void SetLevelRushGift(LevelRushGiftInfo v) => _levelRushGift = v;
        public LevelRushGiftInfo GetLevelRushGift() => _levelRushGift;

        // ============================================================================================
        // §B10 ADVERTISEMENT=111(33250),item_to_bin_39(pt_332.erl:2225-2233)={GradeId:32,CdTime:32}。
        // ============================================================================================
        public sealed class AdCdItem
        {
            public int GradeId;
            public int CdTime;
        }

        public sealed class AdCdList
        {
            public int BaseType;
            public int SubType;
            public readonly List<AdCdItem> CdLists = new List<AdCdItem>();
        }

        private readonly Dictionary<long, AdCdList> _adCdLists = new Dictionary<long, AdCdList>();
        public void SetAdCdList(AdCdList v) => _adCdLists[Key(v.BaseType, v.SubType)] = v;
        public AdCdList GetAdCdList(int baseType, int subType) =>
            _adCdLists.TryGetValue(Key(baseType, subType), out AdCdList v) ? v : null;

        // ============================================================================================
        // §B11 头号玩家提示(33251,331 家族内部冲榜上报,与 225xx pp_rush_rank 是两套)。
        // pt_332.erl write(33251):1309-1323,无 Code,无 BaseType/SubType(独立 RushRankId 命名空间)。
        // **recv-only 防御**(spec §6/老端 fmt 表未见发送分支,C2S read(33251,_)->{ok,[]} 且 handle 内部 skip
        // 不回写,pp handle "疑似给别的模块用")——只注册解析,不提供 Request 方法。
        // ============================================================================================
        public sealed class RushRankTopPlayerInfo
        {
            public int RushRankId;
            public int Type;
            public int Rank;
            public int Value;
            public int SubValue;
        }

        private RushRankTopPlayerInfo _lastRushRankTopPlayer;
        public void SetLastRushRankTopPlayer(RushRankTopPlayerInfo v) => _lastRushRankTopPlayer = v;
        public RushRankTopPlayerInfo GetLastRushRankTopPlayer() => _lastRushRankTopPlayer;

        // ============================================================================================
        // §B12 RED_ENVELOPE_REBATE=117(33255 升级落地 12 字段 + 33256 提现)。
        // ============================================================================================

        /// <summary>对标 pt_332.erl write(33255)(pt_332.erl:1413-1443):升级现有 On33255 后全字段落地
        /// (此前只用 IsQuality/EndTime 算图标,其余 10 字段读了即丢)。</summary>
        public sealed class RedEnvelopeRebateInfo
        {
            public int Type;
            public int Subtype;
            public int IsQuality;
            public long StartTime;
            public long EndTime;
            public int LoginMoney;
            public int RechargeMoney;
            public int LoginStatus;
            public int RechargeStatus;
            public int LoginWithdrawal;
            public int RechargeWithdrawal;
            public int LoginGlobalTimes;
            public int RechargeGlobalTimes;
        }

        private readonly Dictionary<long, RedEnvelopeRebateInfo> _redEnvelopeRebateInfos = new Dictionary<long, RedEnvelopeRebateInfo>();
        public void SetRedEnvelopeRebateInfo(RedEnvelopeRebateInfo v) => _redEnvelopeRebateInfos[Key(v.Type, v.Subtype)] = v;
        public RedEnvelopeRebateInfo GetRedEnvelopeRebateInfo(int type, int subtype) =>
            _redEnvelopeRebateInfos.TryGetValue(Key(type, subtype), out RedEnvelopeRebateInfo v) ? v : null;

        /// <summary>对标 pt_332.erl write(33256)(pt_332.erl:1445-1463):Errcode 是**第3字段**(非开头/末尾)。</summary>
        public sealed class RedEnvelopeWithdrawResult
        {
            public int Type;
            public int Subtype;
            public int Errcode;
            public int LoginMoney;
            public int RechargeMoney;
            public int LoginStatus;
            public int RechargeStatus;
        }

        private readonly Dictionary<long, RedEnvelopeWithdrawResult> _redEnvelopeWithdrawResults = new Dictionary<long, RedEnvelopeWithdrawResult>();
        public void SetRedEnvelopeWithdrawResult(RedEnvelopeWithdrawResult v) => _redEnvelopeWithdrawResults[Key(v.Type, v.Subtype)] = v;
        public RedEnvelopeWithdrawResult GetRedEnvelopeWithdrawResult(int type, int subtype) =>
            _redEnvelopeWithdrawResults.TryGetValue(Key(type, subtype), out RedEnvelopeWithdrawResult v) ? v : null;

        // ============================================================================================
        // §B13 CARNIVAL=118(33258),item_to_bin_45(pt_332.erl:2299-2307)={Grade:16,Process:32}。
        // ============================================================================================
        public sealed class CarnivalTaskItem
        {
            public int Grade;
            public int Process;
        }

        public sealed class CarnivalTaskInfo
        {
            public int Type;
            public int Subtype;
            public readonly List<CarnivalTaskItem> TaskList = new List<CarnivalTaskItem>();
        }

        private readonly Dictionary<long, CarnivalTaskInfo> _carnivalTaskInfos = new Dictionary<long, CarnivalTaskInfo>();
        public void SetCarnivalTaskInfo(CarnivalTaskInfo v) => _carnivalTaskInfos[Key(v.Type, v.Subtype)] = v;
        public CarnivalTaskInfo GetCarnivalTaskInfo(int type, int subtype) =>
            _carnivalTaskInfos.TryGetValue(Key(type, subtype), out CarnivalTaskInfo v) ? v : null;

        // ============================================================================================
        // §B14 TIRED_CHARGE_POLITE=121(33259)。pt_332.erl item_to_bin_46/47(pt_332.erl:2308-2348):
        // List[]→{Grade:16,Condition:str,Name:str,Desc:str,RewardList[]→{FormType:8,Status:8,Reward:str}}。
        // ============================================================================================
        public sealed class TiredChargeRewardItem
        {
            public int FormType;
            public int Status;
            public string Reward = "";
        }

        public sealed class TiredChargeGradeItem
        {
            public int Grade;
            public string Condition = "";
            public string Name = "";
            public string Desc = "";
            public readonly List<TiredChargeRewardItem> RewardList = new List<TiredChargeRewardItem>();
        }

        public sealed class TiredChargePoliteInfo
        {
            public int BaseType;
            public int SubType;
            public int RechargeNum;
            public int IsRecharge;
            public readonly List<TiredChargeGradeItem> List = new List<TiredChargeGradeItem>();
        }

        private readonly Dictionary<long, TiredChargePoliteInfo> _tiredChargePoliteInfos = new Dictionary<long, TiredChargePoliteInfo>();
        public void SetTiredChargePoliteInfo(TiredChargePoliteInfo v) => _tiredChargePoliteInfos[Key(v.BaseType, v.SubType)] = v;
        public TiredChargePoliteInfo GetTiredChargePoliteInfo(int baseType, int subType) =>
            _tiredChargePoliteInfos.TryGetValue(Key(baseType, subType), out TiredChargePoliteInfo v) ? v : null;

        // ============================================================================================
        // §B15 OVER_VIEW=126(33264)。**订正**:r17_server_customactivity.md 侦察表误记 RewardList 元素为
        // "Style:16,GoodsId:32,Num:32"(那是 33257 item_to_bin_44 的结构);33264 实际调用 item_to_bin_48
        // (pt_332.erl:2349-2361)= {Grade:16,FormType:8,Reward:str}(Reward 是字符串,非对象三元组),已回
        // .erl 原文订正。
        // ============================================================================================
        public sealed class OverViewRewardItem
        {
            public int Grade;
            public int FormType;
            public string Reward = "";
        }

        public sealed class OverViewRewardInfo
        {
            public int BaseType;
            public int SubType;
            public readonly List<OverViewRewardItem> RewardList = new List<OverViewRewardItem>();
        }

        private readonly Dictionary<long, OverViewRewardInfo> _overViewRewardInfos = new Dictionary<long, OverViewRewardInfo>();
        public void SetOverViewRewardInfo(OverViewRewardInfo v) => _overViewRewardInfos[Key(v.BaseType, v.SubType)] = v;
        public OverViewRewardInfo GetOverViewRewardInfo(int baseType, int subType) =>
            _overViewRewardInfos.TryGetValue(Key(baseType, subType), out OverViewRewardInfo v) ? v : null;

        // ============================================================================================
        // §B16 RARE_SURFACE=128(33265,被 wxOneMoney 复用=通用分档领取)。pt_332.erl write(33265):1601-1613,
        // Errcode 在**末尾**。
        // ============================================================================================
        public sealed class RareSurfaceClaimResult
        {
            public int Type;
            public int Subtype;
            public int Grade;
            public int Errcode;
        }

        private readonly Dictionary<long, RareSurfaceClaimResult> _rareSurfaceClaimResults = new Dictionary<long, RareSurfaceClaimResult>();
        public void SetRareSurfaceClaimResult(RareSurfaceClaimResult v) => _rareSurfaceClaimResults[Key(v.Type, v.Subtype)] = v;
        public RareSurfaceClaimResult GetRareSurfaceClaimResult(int type, int subtype) =>
            _rareSurfaceClaimResults.TryGetValue(Key(type, subtype), out RareSurfaceClaimResult v) ? v : null;

        // ============================================================================================
        // §B17 通用奖励列表推送(33257,recv-only,被 ≥3 个活动模块复用)。item_to_bin_44(pt_332.erl:2288-2298)
        // = {Style:16,GoodsId:32,Num:32}。
        // ============================================================================================
        public sealed class RewardListPushItem
        {
            public int Style;
            public int GoodsId;
            public int Num;
        }

        public sealed class RewardListPush
        {
            public int Type;
            public int Subtype;
            public readonly List<RewardListPushItem> RewardList = new List<RewardListPushItem>();
        }

        private readonly Dictionary<long, RewardListPush> _rewardListPushes = new Dictionary<long, RewardListPush>();
        public void SetRewardListPush(RewardListPush v) => _rewardListPushes[Key(v.Type, v.Subtype)] = v;
        public RewardListPush GetRewardListPush(int type, int subtype) =>
            _rewardListPushes.TryGetValue(Key(type, subtype), out RewardListPush v) ? v : null;

        // ============================================================================================
        // §B18 活动通用获奖记录(33197)。item_to_bin_72/73(pt_331.erl:3248-3277,两者同构)=
        // {RoleId:64,Name:str,RewardList:ObjectList}。LogList+SelfList 同结构,三层嵌套(顶层→数组→ObjectList)。
        // ============================================================================================
        public sealed class WinLogEntry
        {
            public long RoleId;
            public string Name = "";
            public readonly List<RewardObj> RewardList = new List<RewardObj>();
        }

        public sealed class WinLogData
        {
            public int BaseType;
            public int SubType;
            public readonly List<WinLogEntry> LogList = new List<WinLogEntry>();
            public readonly List<WinLogEntry> SelfList = new List<WinLogEntry>();
        }

        private readonly Dictionary<long, WinLogData> _winLogs = new Dictionary<long, WinLogData>();
        public void SetWinLog(WinLogData v) => _winLogs[Key(v.BaseType, v.SubType)] = v;
        public WinLogData GetWinLog(int baseType, int subType) =>
            _winLogs.TryGetValue(Key(baseType, subType), out WinLogData v) ? v : null;

        // ============================================================================================
        // §B19 完美情缘 actMarriage=25(33115)。item_to_bin_13(pt_331.erl:2389-2397)=
        // {WeddingTypeId:8,WeddingTimes:16}。命名 CustomActMarriage* 避免与 Marriage 模块(172xx,完整婚姻
        // 系统)混淆——本号只是 CustomActivity 框架内 actMarriage(25)的活动条目包装,与 172xx 无协议关联。
        // ============================================================================================
        public sealed class CustomActMarriageWeddingType
        {
            public int WeddingTypeId;
            public int WeddingTimes;
        }

        public sealed class CustomActMarriageInfo
        {
            public int SubType;
            public int Opr;
            public int IfGetReward;
            public readonly List<CustomActMarriageWeddingType> WeddingTypeList = new List<CustomActMarriageWeddingType>();
        }

        private CustomActMarriageInfo _customActMarriage;
        public void SetCustomActMarriageInfo(CustomActMarriageInfo v) => _customActMarriage = v;
        public CustomActMarriageInfo GetCustomActMarriageInfo() => _customActMarriage;

        // ============================================================================================
        // §B20 封测充值返还 BETA_ACT=77(33216)。pt_332.erl write(33216):520-530,无 Code,无 BaseType/SubType。
        // ============================================================================================
        public sealed class BetaRechargeReturnInfo
        {
            public int Gold;
            public int ReturnGold;
            public int LoginDays;
        }

        private BetaRechargeReturnInfo _betaRechargeReturn;
        public void SetBetaRechargeReturn(BetaRechargeReturnInfo v) => _betaRechargeReturn = v;
        public BetaRechargeReturnInfo GetBetaRechargeReturn() => _betaRechargeReturn;

        // ============================================================================================
        // §B21 嗨点 HOTPOINT(33140)。**防御 recv 不发送**(spec §1/§6,pp_custom_act.erl:632-639 handler 空转
        // 恒 {ok,Player} 不回写,33101 列表层已整体过滤 HI_POINT——本号在真实环境几乎不可能被触发)。
        // item_to_bin_33(pt_331.erl:2643-2682)结构复杂(14字段)但因为是死路径,只做安全解析不落地存储
        // (无消费方,避免过度设计一条实际不会走到的数据通道)。
        // ============================================================================================

        // ============================================================================================
        // §B22 充值统计 15955-15960(pt_159.erl)。
        // ============================================================================================

        /// <summary>item_to_bin_6(pt_159.erl:371-395)={Id:16,State:8,Val:32,Max:32,RewardList:ObjectList,
        /// Condition:str,Desc:str}。</summary>
        public sealed class DailyAccumInfoItem
        {
            public int Id;
            public int State;
            public int Val;
            public int Max;
            public readonly List<RewardObj> RewardList = new List<RewardObj>();
            public string Condition = "";
            public string Desc = "";
        }

        public sealed class DailyAccumInfo
        {
            public int SubType;
            public int Num;
            public readonly List<DailyAccumInfoItem> RewardInfos = new List<DailyAccumInfoItem>();
        }

        private readonly Dictionary<int, DailyAccumInfo> _dailyAccumInfos = new Dictionary<int, DailyAccumInfo>();
        public void SetDailyAccumInfo(DailyAccumInfo v) => _dailyAccumInfos[v.SubType] = v;
        public DailyAccumInfo GetDailyAccumInfo(int subType) => _dailyAccumInfos.TryGetValue(subType, out DailyAccumInfo v) ? v : null;

        /// <summary>item_to_bin_7(pt_159.erl:396-422)= DailyAccumInfoItem + GoldNum:64(位置在 Max 之后、
        /// RewardList 之前)。</summary>
        public sealed class DailyAccumRewardItem
        {
            public int Id;
            public int State;
            public int Val;
            public int Max;
            public long GoldNum;
            public readonly List<RewardObj> RewardList = new List<RewardObj>();
            public string Condition = "";
            public string Desc = "";
        }

        public sealed class DailyAccumReward
        {
            public int SubType;
            public readonly List<DailyAccumRewardItem> RewardList = new List<DailyAccumRewardItem>();
        }

        private readonly Dictionary<int, DailyAccumReward> _dailyAccumRewards = new Dictionary<int, DailyAccumReward>();
        public void SetDailyAccumReward(DailyAccumReward v) => _dailyAccumRewards[v.SubType] = v;
        public DailyAccumReward GetDailyAccumReward(int subType) => _dailyAccumRewards.TryGetValue(subType, out DailyAccumReward v) ? v : null;

        /// <summary>15957(某活动类型充值总额)/15958(节日活动·充值有礼充值金额)共用形状,分开存储避免混号。</summary>
        public sealed class ActRechargeInfo
        {
            public int Type;
            public int SubType;
            public int TotalGold;
        }

        private readonly Dictionary<long, ActRechargeInfo> _actRecharges = new Dictionary<long, ActRechargeInfo>();
        public void SetActRecharge(ActRechargeInfo v) => _actRecharges[Key(v.Type, v.SubType)] = v;
        public ActRechargeInfo GetActRecharge(int type, int subType) =>
            _actRecharges.TryGetValue(Key(type, subType), out ActRechargeInfo v) ? v : null;

        private readonly Dictionary<long, ActRechargeInfo> _politeRecharges = new Dictionary<long, ActRechargeInfo>();
        public void SetPoliteRecharge(ActRechargeInfo v) => _politeRecharges[Key(v.Type, v.SubType)] = v;
        public ActRechargeInfo GetPoliteRecharge(int type, int subType) =>
            _politeRecharges.TryGetValue(Key(type, subType), out ActRechargeInfo v) ? v : null;

        /// <summary>15959 当天充值金额,无 Type/SubType,全局单值。</summary>
        public int TodayRechargeGold { get; private set; }
        public void SetTodayRechargeGold(int gold) => TodayRechargeGold = gold;

        /// <summary>15960 几天前的充值金额列表,item_to_bin_8(pt_159.erl:423-430)={Time:32,TotalGold:32}。</summary>
        public sealed class RechargeHistoryItem
        {
            public int Time;
            public int TotalGold;
        }

        private readonly List<RechargeHistoryItem> _rechargeHistory = new List<RechargeHistoryItem>();
        public void SetRechargeHistory(List<RechargeHistoryItem> list)
        {
            _rechargeHistory.Clear();
            _rechargeHistory.AddRange(list);
        }
        public IReadOnlyList<RechargeHistoryItem> GetRechargeHistory() => _rechargeHistory;

        // ============================================================================================
        // §B23 生命周期(已挂钩):CustomActivityModel.cs 的 Clear()〔轮17收口〕已在级联里调用 ClearBiz(),
        // 断线/登出会随 Instance.Clear() 一并清空;本方法同时保留独立可调用,供 CliVerify Case 单段复位用。
        // ============================================================================================
        public void ClearBiz()
        {
            _zeroMallPanels.Clear();
            _ftvInvestInfos.Clear();
            _ftvInvestBuyResults.Clear();
            _vipGiftInfos.Clear();
            DailySupplyLiveness = 0;
            NameVerifyConfirmedAt = 0;
            _lastBatchExchange = null;
            _lastQuestionnaire = null;
            _manyRechargeInfos.Clear();
            _levelRushGift = null;
            _adCdLists.Clear();
            _lastRushRankTopPlayer = null;
            _redEnvelopeRebateInfos.Clear();
            _redEnvelopeWithdrawResults.Clear();
            _carnivalTaskInfos.Clear();
            _tiredChargePoliteInfos.Clear();
            _overViewRewardInfos.Clear();
            _rareSurfaceClaimResults.Clear();
            _rewardListPushes.Clear();
            _winLogs.Clear();
            _customActMarriage = null;
            _betaRechargeReturn = null;
            _dailyAccumInfos.Clear();
            _dailyAccumRewards.Clear();
            _actRecharges.Clear();
            _politeRecharges.Clear();
            TodayRechargeGold = 0;
            _rechargeHistory.Clear();
        }
    }
}
