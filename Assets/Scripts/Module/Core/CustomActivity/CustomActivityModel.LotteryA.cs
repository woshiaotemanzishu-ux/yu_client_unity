using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// P2 抽奖A(自动循环 轮17,spec §3)类型化数据段:OPTIONALLOTTO=76(33128/29/33/34/35/39)/
    /// WISH_POOL=79(33141/42/44)/DESTINY_TURNTABLE=99(33238/39/40)/TURNTABLE_100=100(33241/42)。
    /// 字段序全部逐条回 pt_331.erl / pt_332.erl 的 write/item_to_bin_N 原文核对(文件:行见各注释),
    /// 非仅套用 r17_server_customactivity.md 侦察表(该表对 33128 RewardList 内部字段/33242 RewardList
    /// 内部字段两处失真,已订正,见 On33128/On33242 注释)。Key(baseType,subType) 复用 P1
    /// CustomActivityModel.cs 的 private static Key(同 partial class 内可见)。
    /// </summary>
    public sealed partial class CustomActivityModel
    {
        // ============================================================================================
        // 公共小件:Obj 三元组奖励(pt.erl:352-356 write_object_list 元素,Type:8,GoodsId:32,Num:32),
        // 本包(OPTIONALLOTTO 33134 Reward / WISH_POOL 33142 RewardList.Reward)共用。
        // ============================================================================================

        public sealed class LottoObjReward
        {
            public int Type;
            public long GoodsId;
            public long Num;
        }

        public static LottoObjReward ReadLottoObjReward(NetReader r) => new LottoObjReward
        {
            Type = r.ReadU8(), GoodsId = r.ReadU32(), Num = r.ReadU32(),
        };

        // ============================================================================================
        // §1 OPTIONALLOTTO(76):33128(界面)/33129(锁定)/33133(重置)/33134(抽奖)/33135(阶段奖)/33139(奖池)
        // ============================================================================================

        /// <summary>Pool 元素(item_to_bin_22/25/29 同构,pt_331.erl:2496-2506/2541-2551/2585-2595,
        /// Rare:8,Grade:16,Status:8),33128/33129回执/33133 三号共用。</summary>
        public sealed class LottoPoolEntry { public int Rare; public int Grade; public int Status; }

        public static LottoPoolEntry ReadLottoPoolEntry(NetReader r) => new LottoPoolEntry
        {
            Rare = r.ReadU8(), Grade = r.ReadU16(), Status = r.ReadU8(),
        };

        /// <summary>2 字段 Rare/Grade 形态(33129 C2S 请求 Pool 元素同构 pt_331.erl:84-88;33139 S2C 回包
        /// item_to_bin_32,pt_331.erl:2634-2642,同 2 字段,复用同一读法)。</summary>
        public sealed class LottoRareGradeEntry { public int Rare; public int Grade; }

        public static LottoRareGradeEntry ReadLottoRareGradeEntry(NetReader r) => new LottoRareGradeEntry
        {
            Rare = r.ReadU8(), Grade = r.ReadU16(),
        };

        /// <summary>Stage 元素(item_to_bin_23/30 同构,pt_331.erl:2507-2515/2596-2604,Grade:16,Status:8),
        /// 33128/33133 共用。</summary>
        public sealed class LottoStageEntry { public int Grade; public int Status; }

        public static LottoStageEntry ReadLottoStageEntry(NetReader r) => new LottoStageEntry
        {
            Grade = r.ReadU16(), Status = r.ReadU8(),
        };

        /// <summary>33128 RewardList 元素(item_to_bin_24,pt_331.erl:2516-2540):Grade:16,FormType:8,
        /// Name:str,Desc:str,Condition:str,Reward:str——字段序 Name→Desc→Condition→Reward,与
        /// 33238/33241 的 Reward 在 Condition **之前** 不同(逐号不可套模板,见 spec §8-②)。
        /// r17_server_customactivity.md 表格未展开该数组内部字段,本类按 .erl 原文订正补全。</summary>
        public sealed class LottoRewardEntry
        {
            public int Grade;
            public int FormType;
            public string Name = "";
            public string Desc = "";
            public string Condition = "";
            public string Reward = "";
        }

        public static LottoRewardEntry ReadLottoRewardEntry(NetReader r) => new LottoRewardEntry
        {
            Grade = r.ReadU16(), FormType = r.ReadU8(), Name = r.ReadString(), Desc = r.ReadString(),
            Condition = r.ReadString(), Reward = r.ReadString(),
        };

        /// <summary>33128 面板全量(pt_331.erl:788-827)。</summary>
        public sealed class LottoPanelData
        {
            public int BaseType;
            public int SubType;
            public int DrawTimes;
            public int Reset;
            public readonly List<LottoPoolEntry> Pool = new List<LottoPoolEntry>();
            public readonly List<LottoStageEntry> Stage = new List<LottoStageEntry>();
            public readonly List<LottoRewardEntry> RewardList = new List<LottoRewardEntry>();
        }

        private readonly Dictionary<long, LottoPanelData> _lottoPanels = new Dictionary<long, LottoPanelData>();

        public void SetLottoPanel(LottoPanelData d) => _lottoPanels[Key(d.BaseType, d.SubType)] = d;
        public LottoPanelData GetLottoPanel(int baseType, int subType) =>
            _lottoPanels.TryGetValue(Key(baseType, subType), out LottoPanelData d) ? d : null;

        /// <summary>33129 锁定奖池回执(pt_331.erl:829-848):BaseType,SubType,Pool[](item_to_bin_25,
        /// 3字段,**与 C2S 请求侧 2字段 Rare/Grade 不同结构**),ErrorCode **在末尾**。</summary>
        public sealed class LottoLockResult
        {
            public int BaseType;
            public int SubType;
            public int ErrorCode;
            public readonly List<LottoPoolEntry> Pool = new List<LottoPoolEntry>();
        }

        private readonly Dictionary<long, LottoLockResult> _lottoLockResults = new Dictionary<long, LottoLockResult>();

        public void SetLottoLockResult(LottoLockResult r) => _lottoLockResults[Key(r.BaseType, r.SubType)] = r;
        public LottoLockResult GetLottoLockResult(int baseType, int subType) =>
            _lottoLockResults.TryGetValue(Key(baseType, subType), out LottoLockResult r) ? r : null;

        /// <summary>33133 重置回执(pt_331.erl:927-959):BaseType,SubType,ErrorCode,DrawTimes,Reset,
        /// Pool[](item_to_bin_29),Stage[](item_to_bin_30)。</summary>
        public sealed class LottoResetResult
        {
            public int BaseType;
            public int SubType;
            public int ErrorCode;
            public int DrawTimes;
            public int Reset;
            public readonly List<LottoPoolEntry> Pool = new List<LottoPoolEntry>();
            public readonly List<LottoStageEntry> Stage = new List<LottoStageEntry>();
        }

        private readonly Dictionary<long, LottoResetResult> _lottoResetResults = new Dictionary<long, LottoResetResult>();

        public void SetLottoResetResult(LottoResetResult r) => _lottoResetResults[Key(r.BaseType, r.SubType)] = r;
        public LottoResetResult GetLottoResetResult(int baseType, int subType) =>
            _lottoResetResults.TryGetValue(Key(baseType, subType), out LottoResetResult r) ? r : null;

        /// <summary>33134 抽奖回执(pt_331.erl:961-981):BaseType,SubType,DrawTimes,ErrorCode,Grade,Rare,
        /// Reward(pt:write_object_list 直接展开,非嵌套结构体)。</summary>
        public sealed class LottoDrawResult
        {
            public int BaseType;
            public int SubType;
            public int DrawTimes;
            public int ErrorCode;
            public int Grade;
            public int Rare;
            public readonly List<LottoObjReward> Reward = new List<LottoObjReward>();
        }

        private readonly Dictionary<long, LottoDrawResult> _lottoDrawResults = new Dictionary<long, LottoDrawResult>();

        public void SetLottoDrawResult(LottoDrawResult r) => _lottoDrawResults[Key(r.BaseType, r.SubType)] = r;
        public LottoDrawResult GetLottoDrawResult(int baseType, int subType) =>
            _lottoDrawResults.TryGetValue(Key(baseType, subType), out LottoDrawResult r) ? r : null;

        /// <summary>33135 阶段奖励回执(pt_331.erl:983-995):BaseType,SubType,Grade,ErrorCode(**末尾**)。</summary>
        public sealed class LottoStageResult
        {
            public int BaseType;
            public int SubType;
            public int Grade;
            public int ErrorCode;
        }

        private readonly Dictionary<long, LottoStageResult> _lottoStageResults = new Dictionary<long, LottoStageResult>();

        public void SetLottoStageResult(LottoStageResult r) => _lottoStageResults[Key(r.BaseType, r.SubType)] = r;
        public LottoStageResult GetLottoStageResult(int baseType, int subType) =>
            _lottoStageResults.TryGetValue(Key(baseType, subType), out LottoStageResult r) ? r : null;

        /// <summary>33139 奖池(pt_331.erl:1040-1057):BaseType,SubType,Pool[](item_to_bin_32,**仅2字段**
        /// Rare/Grade,无 Status——与 33128/33129回执/33133 的 3字段 Pool 不同结构)。</summary>
        private readonly Dictionary<long, List<LottoRareGradeEntry>> _lottoRandomPools = new Dictionary<long, List<LottoRareGradeEntry>>();

        public void SetLottoRandomPool(int baseType, int subType, List<LottoRareGradeEntry> pool) =>
            _lottoRandomPools[Key(baseType, subType)] = pool;
        public List<LottoRareGradeEntry> GetLottoRandomPool(int baseType, int subType) =>
            _lottoRandomPools.TryGetValue(Key(baseType, subType), out List<LottoRareGradeEntry> p) ? p : null;

        // ============================================================================================
        // §2 WISH_POOL(79):33141(奖池)/33142(取奖池奖励)/33144(重置)
        // ============================================================================================

        /// <summary>RarePool 元素(item_to_bin_34,pt_331.erl:2684-2698):Grade:16,LuckyValue:16,
        /// FreeTimes:16,State:8,MaxLuckeyValue:16。</summary>
        public sealed class WishRarePoolEntry
        {
            public int Grade;
            public int LuckyValue;
            public int FreeTimes;
            public int State;
            public int MaxLuckyValue;
        }

        public static WishRarePoolEntry ReadWishRarePoolEntry(NetReader r) => new WishRarePoolEntry
        {
            Grade = r.ReadU16(), LuckyValue = r.ReadU16(), FreeTimes = r.ReadU16(), State = r.ReadU8(), MaxLuckyValue = r.ReadU16(),
        };

        /// <summary>33141 奖池(pt_331.erl:1076-1093):BaseType,SubType,RarePool[]。</summary>
        private readonly Dictionary<long, List<WishRarePoolEntry>> _wishPools = new Dictionary<long, List<WishRarePoolEntry>>();

        public void SetWishPool(int baseType, int subType, List<WishRarePoolEntry> pool) => _wishPools[Key(baseType, subType)] = pool;
        public List<WishRarePoolEntry> GetWishPool(int baseType, int subType) =>
            _wishPools.TryGetValue(Key(baseType, subType), out List<WishRarePoolEntry> p) ? p : null;

        /// <summary>33142 RewardList 元素(item_to_bin_35,pt_331.erl:2699-2709):Reward 是**嵌套 ObjectList**
        /// (pt:write_object_list,非单个 Obj),IsRare:8 尾随。</summary>
        public sealed class WishClaimRewardEntry
        {
            public readonly List<LottoObjReward> Reward = new List<LottoObjReward>();
            public int IsRare;
        }

        public static WishClaimRewardEntry ReadWishClaimRewardEntry(NetReader r)
        {
            var e = new WishClaimRewardEntry();
            e.Reward.AddRange(r.ReadArray(ReadLottoObjReward));
            e.IsRare = r.ReadU8();
            return e;
        }

        /// <summary>33142 取奖池奖励回执(pt_331.erl:1095-1122):BaseType,SubType,Grade,ErrorCode,
        /// RewardList[](item_to_bin_35),LuckyValue,FreeTimes,State。
        /// ⚠wire 争议见 Controller RequestWishPoolClaim 注释——本类按服务端权威 5 参结构落地,
        /// 与老端运行时"hhh"死分支截断行为(仅供参照,未纳入本类)无关,S2C 回执结构不受影响。</summary>
        public sealed class WishClaimResult
        {
            public int BaseType;
            public int SubType;
            public int Grade;
            public int ErrorCode;
            public readonly List<WishClaimRewardEntry> RewardList = new List<WishClaimRewardEntry>();
            public int LuckyValue;
            public int FreeTimes;
            public int State;
        }

        private readonly Dictionary<long, WishClaimResult> _wishClaimResults = new Dictionary<long, WishClaimResult>();

        public void SetWishClaimResult(WishClaimResult r) => _wishClaimResults[Key(r.BaseType, r.SubType)] = r;
        public WishClaimResult GetWishClaimResult(int baseType, int subType) =>
            _wishClaimResults.TryGetValue(Key(baseType, subType), out WishClaimResult r) ? r : null;

        /// <summary>33144 重置回执(pt_331.erl:1134-1154):BaseType,SubType,Grade,Code,LuckyValue,
        /// FreeTimes,State,MaxLuckeyValue(字段名原文即"Code"非"ErrorCode",老端 On33144 读 scmd.code)。</summary>
        public sealed class WishResetResult
        {
            public int BaseType;
            public int SubType;
            public int Grade;
            public int Code;
            public int LuckyValue;
            public int FreeTimes;
            public int State;
            public int MaxLuckyValue;
        }

        private readonly Dictionary<long, WishResetResult> _wishResetResults = new Dictionary<long, WishResetResult>();

        public void SetWishResetResult(WishResetResult r) => _wishResetResults[Key(r.BaseType, r.SubType)] = r;
        public WishResetResult GetWishResetResult(int baseType, int subType) =>
            _wishResetResults.TryGetValue(Key(baseType, subType), out WishResetResult r) ? r : null;

        // ============================================================================================
        // §3 DESTINY_TURNTABLE(99):33238(界面)/33239(recv-only积分推送)/33240(开抽)
        // ============================================================================================

        /// <summary>33238 RewardList 元素(item_to_bin_27,pt_332.erl:2043-2069):Grade:16,FormType:8,
        /// Status:8,Name:str,Desc:str,**Reward:str,Condition:str**——注意 Reward 在 Condition **之前**,
        /// 与 33128(item_to_bin_24,Condition 在 Reward 之前)顺序相反,不可套模板。</summary>
        public sealed class DestinyRewardEntry
        {
            public int Grade;
            public int FormType;
            public int Status;
            public string Name = "";
            public string Desc = "";
            public string Reward = "";
            public string Condition = "";
        }

        public static DestinyRewardEntry ReadDestinyRewardEntry(NetReader r) => new DestinyRewardEntry
        {
            Grade = r.ReadU16(), FormType = r.ReadU8(), Status = r.ReadU8(), Name = r.ReadString(), Desc = r.ReadString(),
            Reward = r.ReadString(), Condition = r.ReadString(),
        };

        /// <summary>DoublePoint 元素(item_to_bin_28,pt_332.erl:2070-2078):JumpId:32,IsBuy:8。</summary>
        public sealed class DestinyDoublePointEntry { public long JumpId; public int IsBuy; }

        public static DestinyDoublePointEntry ReadDestinyDoublePointEntry(NetReader r) => new DestinyDoublePointEntry
        {
            JumpId = r.ReadU32(), IsBuy = r.ReadU8(),
        };

        /// <summary>33238 面板全量(pt_332.erl:988-1024):BaseType,SubType,Turn:16,Point:32,NeedPoint:32,
        /// MaxTurn:16,RewardList[],DoublePoint[],Label:8。</summary>
        public sealed class DestinyPanelData
        {
            public int BaseType;
            public int SubType;
            public int Turn;
            public long Point;
            public long NeedPoint;
            public int MaxTurn;
            public readonly List<DestinyRewardEntry> RewardList = new List<DestinyRewardEntry>();
            public readonly List<DestinyDoublePointEntry> DoublePoint = new List<DestinyDoublePointEntry>();
            public int Label;
        }

        private readonly Dictionary<long, DestinyPanelData> _destinyPanels = new Dictionary<long, DestinyPanelData>();

        public void SetDestinyPanel(DestinyPanelData d) => _destinyPanels[Key(d.BaseType, d.SubType)] = d;
        public DestinyPanelData GetDestinyPanel(int baseType, int subType) =>
            _destinyPanels.TryGetValue(Key(baseType, subType), out DestinyPanelData d) ? d : null;

        /// <summary>33239 **recv-only** 积分推送(pt_332.erl:1026-1040,C2S 死号 read(33239,_)->{ok,[]},
        /// S2C 抽奖后主动推送):BaseType,SubType,Turn:16,Point:32,NeedPoint:32,无 ErrorCode 前导
        /// (对标老端 On33239:ts:2372-2383,`if (!actData) return`——本端若面板未缓存仍照常落地此推送记录,
        /// 只有"回写面板"这一步比照老端做 guard)。</summary>
        public sealed class DestinyPushInfo
        {
            public int BaseType;
            public int SubType;
            public int Turn;
            public long Point;
            public long NeedPoint;
        }

        private readonly Dictionary<long, DestinyPushInfo> _destinyPushInfos = new Dictionary<long, DestinyPushInfo>();

        public void SetDestinyPushInfo(DestinyPushInfo p) => _destinyPushInfos[Key(p.BaseType, p.SubType)] = p;
        public DestinyPushInfo GetDestinyPushInfo(int baseType, int subType) =>
            _destinyPushInfos.TryGetValue(Key(baseType, subType), out DestinyPushInfo p) ? p : null;

        /// <summary>33240 开抽回执(pt_332.erl:1042-1064):ErrorCode **在最前**,BaseType,SubType,GradeId:16,
        /// **Reward 走 write_string(pt:write_string),不是 write_object_list**(与 33134/33142 的
        /// ObjectList 奖励结构不同——本号奖励以纯文案字符串下发,pt_332.erl:1052 `pt:write_string(Reward)`),
        /// Turn:16,Point:32,NeedPoint:32。</summary>
        public sealed class DestinyDrawResult
        {
            public int ErrorCode;
            public int BaseType;
            public int SubType;
            public int GradeId;
            public string Reward = "";
            public int Turn;
            public long Point;
            public long NeedPoint;
        }

        private readonly Dictionary<long, DestinyDrawResult> _destinyDrawResults = new Dictionary<long, DestinyDrawResult>();

        public void SetDestinyDrawResult(DestinyDrawResult r) => _destinyDrawResults[Key(r.BaseType, r.SubType)] = r;
        public DestinyDrawResult GetDestinyDrawResult(int baseType, int subType) =>
            _destinyDrawResults.TryGetValue(Key(baseType, subType), out DestinyDrawResult r) ? r : null;

        // ============================================================================================
        // §4 TURNTABLE_100(100):33241(界面)/33242(recv-only推送)
        // ============================================================================================

        /// <summary>33241 RewardList 元素(item_to_bin_29,pt_332.erl:2079-2109):Grade:16,FormType:8,
        /// Status:8,Process:16,ReceiveTimes:16,Name:str,Desc:str,Condition:str,Reward:str
        /// (Condition 在 Reward 之前,同 33128 顺序,与 33238 相反)。</summary>
        public sealed class Turn100RewardEntry
        {
            public int Grade;
            public int FormType;
            public int Status;
            public int Process;
            public int ReceiveTimes;
            public string Name = "";
            public string Desc = "";
            public string Condition = "";
            public string Reward = "";
        }

        public static Turn100RewardEntry ReadTurn100RewardEntry(NetReader r) => new Turn100RewardEntry
        {
            Grade = r.ReadU16(), FormType = r.ReadU8(), Status = r.ReadU8(), Process = r.ReadU16(), ReceiveTimes = r.ReadU16(),
            Name = r.ReadString(), Desc = r.ReadString(), Condition = r.ReadString(), Reward = r.ReadString(),
        };

        /// <summary>33241 面板(pt_332.erl:1066-1083):BaseType,SubType,RewardList[]。</summary>
        private readonly Dictionary<long, List<Turn100RewardEntry>> _turn100Panels = new Dictionary<long, List<Turn100RewardEntry>>();

        public void SetTurn100Panel(int baseType, int subType, List<Turn100RewardEntry> list) => _turn100Panels[Key(baseType, subType)] = list;
        public List<Turn100RewardEntry> GetTurn100Panel(int baseType, int subType) =>
            _turn100Panels.TryGetValue(Key(baseType, subType), out List<Turn100RewardEntry> list) ? list : null;

        /// <summary>33242 RewardList 推送元素(item_to_bin_30,pt_332.erl:2110-2118):**Grade:16,Process:16**
        /// ——r17_server_customactivity.md 表格误记为"Grade:16,Status:8",经 .erl 原文(item_to_bin_30 结构体
        /// 字段名即 Grade/Process)与老端 On33242 消费字段(ts:2416-2434,`v1.process`)双重核实后订正,
        /// 本类按订正后的 Process:16 落地(spec §8-①"复杂号必须回 .erl 逐字段核"实证案例)。</summary>
        public sealed class Turn100PushEntry { public int Grade; public int Process; }

        public static Turn100PushEntry ReadTurn100PushEntry(NetReader r) => new Turn100PushEntry
        {
            Grade = r.ReadU16(), Process = r.ReadU16(),
        };

        /// <summary>33242 最近一次推送(对标老端 On33242 合并进 reward_list 按 Grade 匹配更新 Process;
        /// config 依赖的"Process 达标自动翻 Status=1"分支超出数据层范围,不镜像,TODO 交 UI 尾包)。</summary>
        private readonly Dictionary<long, List<Turn100PushEntry>> _turn100Pushes = new Dictionary<long, List<Turn100PushEntry>>();

        public void SetTurn100Push(int baseType, int subType, List<Turn100PushEntry> list) => _turn100Pushes[Key(baseType, subType)] = list;
        public List<Turn100PushEntry> GetTurn100Push(int baseType, int subType) =>
            _turn100Pushes.TryGetValue(Key(baseType, subType), out List<Turn100PushEntry> list) ? list : null;

        // ============================================================================================
        // §5 清空(已挂钩:CustomActivityModel.cs 的 Clear()〔轮17收口〕已在级联里调用 ClearLotteryA(),
        // 断线/登出会随 Instance.Clear() 一并清空;本方法同时保留独立可调用,供 CliVerify Case 单段复位用)。
        // ============================================================================================

        public void ClearLotteryA()
        {
            _lottoPanels.Clear();
            _lottoLockResults.Clear();
            _lottoResetResults.Clear();
            _lottoDrawResults.Clear();
            _lottoStageResults.Clear();
            _lottoRandomPools.Clear();
            _wishPools.Clear();
            _wishClaimResults.Clear();
            _wishResetResults.Clear();
            _destinyPanels.Clear();
            _destinyPushInfos.Clear();
            _destinyDrawResults.Clear();
            _turn100Panels.Clear();
            _turn100Pushes.Clear();
        }
    }
}
