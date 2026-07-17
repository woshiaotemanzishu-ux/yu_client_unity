using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.CustomActivity
{
    // P3 填充:见 spec §4。类型化数据段(GASHAPON/LUC_TREA_TWO/ONLINE_DRAW/LUC_TREA/FORTUNECAT/BIND_JAGE_WISH),
    // 由 CustomActivityController.LotteryB.cs 的 handler 落地。通用容器在 CustomActivityModel.cs(P1)。
    //
    // wire 逐字段回 yu_server\src\pt\pt_332.erl 原文核对(非仅侦察表 r17_server_customactivity.md,该表在本包
    // 3 处与 .erl 原文不符,均已按 .erl 原文订正,详见下方各号常量注释——原文行号随文标注):
    //   1) 33214(LUC_TREA 抽奖) Reward 字段不是侦察表写的扁平 ObjectType:8,GtypeId:32,Num:32 三元组数组,
    //      而是 item_to_bin_6({Grade,RewardList,Rare})——每条 Grade:16 + 嵌套 write_object_list(Len:16+三元组) +
    //      Rare:8(pt_332.erl:1744-1756,write 子句 481-500)。
    //   2) 33243/33244(LUC_TREA_TWO)"嵌套 GradeInfo" 实际是两个并列顶层数组,不是互相嵌套:GradeInfo(每条仅
    //      GradeId:16+Count:16,item_to_bin_31/34)与 RewardList/Reward 是同级字段,不是 GradeInfo 内部套
    //      RewardList(pt_332.erl:1104-1176,item_to_bin_31/32/33/34 定义 2119-2168)。33244 的 GradeList 每条
    //      仅 GradeId:16 单字段(item_to_bin_33,2153-2159),Reward 走标准 pt:write_object_list 三元组
    //      (ObjectType:8,GtypeId:32,Num:32),与 GradeList 各自独立,ErrorCode 是第 5 个顶层字段(BaseType,
    //      SubType,Times,AutoBuy,ErrorCode,...),既非"开头"也非侦察表标注的"第3"。
    //   3) 33224(FORTUNECAT 信息) RewardList 是 item_to_bin_17={GradeId:16,GoodsId:32,GoodsNum:32,IsHead:8}
    //      四字段(pt_332.erl:1879-1891),侦察表漏记 GradeId/IsHead 两个字段。
    // 33262(BIND_JAGE_WISH 开抽) C2S 侦察表误记为 5 字段,实际 pt_332.erl read(33262) 只读 Type:16,SubType:16
    // 两字段(pt_332.erl:234-237;Grade/Turn/Times 由服务端自行算,client 不传)——已按 read 原文订正,
    // 详见 CustomActivityController.LotteryB.cs RequestBindJageDraw 注释。
    public sealed partial class CustomActivityModel
    {
        // ============================================================================================
        // 共享:标准奖励三元组(pt.erl:352-356 write_object_list,Type:8,ObjectTypeId:32,Num:32)。
        // ============================================================================================

        public sealed class RewardTriple
        {
            public int Type;
            public int GoodsId;
            public int Num;
        }

        public static RewardTriple ReadRewardTriple(NetReader r) => new RewardTriple
        {
            Type = r.ReadU8(), GoodsId = r.ReadI32(), Num = r.ReadI32(),
        };

        // GASHAPON(103)/LUC_TREA_TWO(102) 共用的 {GradeId:16,Count:16} 二元结构(item_to_bin_31/34,完全同构)。
        public sealed class GradeCount
        {
            public int GradeId;
            public int Count;
        }

        public static GradeCount ReadGradeCount(NetReader r) => new GradeCount
        {
            GradeId = r.ReadU16(), Count = r.ReadU16(),
        };

        // ============================================================================================
        // §1 GASHAPON(103):33245 通用抽奖信息 / 33246 开抽(pt_332.erl:1178-1230/1231-1260)
        // ============================================================================================

        public sealed class GashaponDrawGrade { public int GradeId; public int IsNice; public int IsGet1; public string Reward = ""; }
        public sealed class GashaponGrandGrade { public int GradeId; public int IsGet2; public int NeedNum; public string Reward = ""; }
        public sealed class GashaponExchangeGrade { public int GradeId; public int NeedPoint; public string Reward = ""; }

        public sealed class GashaponInfo
        {
            public int BaseType;
            public int SubType;
            public long MaxLuck;
            public long CurrentLuck;
            public int PerLuck;
            public long TotalTimes;
            public string OneCost = "";
            public string TenCost = "";
            public readonly List<GashaponDrawGrade> DrawList = new List<GashaponDrawGrade>();
            public readonly List<GashaponGrandGrade> GrandList = new List<GashaponGrandGrade>();
            public readonly List<GashaponExchangeGrade> ExchangeList = new List<GashaponExchangeGrade>();
        }

        private readonly Dictionary<long, GashaponInfo> _gashaponInfo = new Dictionary<long, GashaponInfo>();
        public void SetGashaponInfo(GashaponInfo info) => _gashaponInfo[Key(info.BaseType, info.SubType)] = info;
        public GashaponInfo GetGashaponInfo(int baseType, int subType) =>
            _gashaponInfo.TryGetValue(Key(baseType, subType), out GashaponInfo v) ? v : null;

        public sealed class GashaponDrawRewardEntry { public int GradeId; public string Reward = ""; public int IsNice; } // item_to_bin_38

        public sealed class GashaponDrawResult
        {
            public int Code;
            public int BaseType;
            public int SubType;
            public int AutoBuy;
            public int LucencyField;
            public long CurrentLuck;
            public long CurrentTimes;
            public readonly List<GashaponDrawRewardEntry> RewardList = new List<GashaponDrawRewardEntry>();
        }

        private readonly Dictionary<long, GashaponDrawResult> _gashaponDraw = new Dictionary<long, GashaponDrawResult>();
        public void SetGashaponDrawResult(GashaponDrawResult result) => _gashaponDraw[Key(result.BaseType, result.SubType)] = result;
        public GashaponDrawResult GetGashaponDrawResult(int baseType, int subType) =>
            _gashaponDraw.TryGetValue(Key(baseType, subType), out GashaponDrawResult v) ? v : null;

        // ============================================================================================
        // §2 LUC_TREA_TWO(102):33243 幸运鉴宝2界面 / 33244 抽奖(pt_332.erl:1104-1134/1136-1176)
        // ============================================================================================

        public sealed class Luctrea2RewardConfig // item_to_bin_32
        {
            public int Grade;
            public int FormType;
            public string Name = "";
            public string Desc = "";
            public string Condition = "";
            public string Reward = "";
        }

        public sealed class Luctrea2Info
        {
            public int BaseType;
            public int SubType;
            public int DrawTime; // :16(与 33217 的 32 位 DrawTime 不同,勿混用同名变量类型)
            public int Turn;
            public readonly List<GradeCount> GradeInfo = new List<GradeCount>();
            public readonly List<Luctrea2RewardConfig> RewardList = new List<Luctrea2RewardConfig>();
        }

        private readonly Dictionary<long, Luctrea2Info> _luctrea2Info = new Dictionary<long, Luctrea2Info>();
        public void SetLuctrea2Info(Luctrea2Info info) => _luctrea2Info[Key(info.BaseType, info.SubType)] = info;
        public Luctrea2Info GetLuctrea2Info(int baseType, int subType) =>
            _luctrea2Info.TryGetValue(Key(baseType, subType), out Luctrea2Info v) ? v : null;

        public sealed class Luctrea2DrawResult
        {
            public int BaseType;
            public int SubType;
            public int Times;
            public int AutoBuy;
            public int Code;
            public readonly List<int> GradeList = new List<int>(); // item_to_bin_33,仅 GradeId 单字段
            public readonly List<RewardTriple> Reward = new List<RewardTriple>(); // 标准三元组(pt:write_object_list)
            public int DrawTime; // :16
            public int Turn;
            public readonly List<GradeCount> GradeInfo = new List<GradeCount>();
        }

        private readonly Dictionary<long, Luctrea2DrawResult> _luctrea2Draw = new Dictionary<long, Luctrea2DrawResult>();
        public void SetLuctrea2DrawResult(Luctrea2DrawResult result) => _luctrea2Draw[Key(result.BaseType, result.SubType)] = result;
        public Luctrea2DrawResult GetLuctrea2DrawResult(int baseType, int subType) =>
            _luctrea2Draw.TryGetValue(Key(baseType, subType), out Luctrea2DrawResult v) ? v : null;

        // ============================================================================================
        // §3 ONLINE_DRAW(81):33217 等级活跃抽奖界面信息 / 33266 物品期望战力(pt_332.erl:532-555/1615-1627)
        // ============================================================================================

        public sealed class OnlineDrawWinner // item_to_bin_7
        {
            public long RoleId;
            public FigureProto Figure;
        }

        public sealed class OnlineDrawInfo
        {
            public int Code;
            public int BaseType;
            public int SubType;
            public long DrawTime; // :32(与 33243/33244 的 16 位 DrawTime 不同)
            public int IsWinner;
            public readonly List<OnlineDrawWinner> WinnerList = new List<OnlineDrawWinner>();
        }

        private readonly Dictionary<long, OnlineDrawInfo> _onlineDrawInfo = new Dictionary<long, OnlineDrawInfo>();
        public void SetOnlineDrawInfo(OnlineDrawInfo info) => _onlineDrawInfo[Key(info.BaseType, info.SubType)] = info;
        public OnlineDrawInfo GetOnlineDrawInfo(int baseType, int subType) =>
            _onlineDrawInfo.TryGetValue(Key(baseType, subType), out OnlineDrawInfo v) ? v : null;

        /// <summary>物品期望战力查询结果(pt_332.erl:1615-1627,Type:16,Subtype:16,Power:64,Errcode:32 末尾)。
        /// 服务端不回显 GoodsId(read(33266) 的 GoodsId:64 仅用于服务端内部计算),按 (BaseType,SubType) 存最近
        /// 一次查询结果——UI 侧需自行记住发起查询时的 GoodsId 做展示匹配。</summary>
        public sealed class GoodsPowerResult
        {
            public int BaseType;
            public int SubType;
            public long Power;
            public int Code;
        }

        private readonly Dictionary<long, GoodsPowerResult> _goodsPower = new Dictionary<long, GoodsPowerResult>();
        public void SetGoodsPowerResult(GoodsPowerResult result) => _goodsPower[Key(result.BaseType, result.SubType)] = result;
        public GoodsPowerResult GetGoodsPowerResult(int baseType, int subType) =>
            _goodsPower.TryGetValue(Key(baseType, subType), out GoodsPowerResult v) ? v : null;

        // ============================================================================================
        // §4 LUC_TREA(80):33213 幸运抽奖界面 / 33214 抽奖(pt_332.erl:465-479/481-500)
        // ============================================================================================

        public sealed class LuctreaPoolData
        {
            public int BaseType;
            public int SubType;
            public readonly List<RewardTriple> Pool = new List<RewardTriple>(); // 标准三元组
            public int Code; // ErrorCode 在末尾(pt_332.erl:465-479)
        }

        private readonly Dictionary<long, LuctreaPoolData> _luctreaPool = new Dictionary<long, LuctreaPoolData>();
        public void SetLuctreaPool(LuctreaPoolData data) => _luctreaPool[Key(data.BaseType, data.SubType)] = data;
        public LuctreaPoolData GetLuctreaPool(int baseType, int subType) =>
            _luctreaPool.TryGetValue(Key(baseType, subType), out LuctreaPoolData v) ? v : null;

        /// <summary>item_to_bin_6({Grade,RewardList,Rare}):Grade:16 + 嵌套标准三元组数组(write_object_list) +
        /// Rare:8。不是侦察表写的扁平三元组数组——见本文件头注释订正记录 1)。</summary>
        public sealed class LuctreaRewardGroup
        {
            public int Grade;
            public readonly List<RewardTriple> RewardList = new List<RewardTriple>();
            public int Rare;
        }

        public sealed class LuctreaDrawResult
        {
            public int BaseType;
            public int SubType;
            public int Code; // ErrorCode 是第 3 个顶层字段(BaseType,SubType,ErrorCode,Reward),非开头非末尾
            public readonly List<LuctreaRewardGroup> Reward = new List<LuctreaRewardGroup>();
        }

        private readonly Dictionary<long, LuctreaDrawResult> _luctreaDraw = new Dictionary<long, LuctreaDrawResult>();
        public void SetLuctreaDrawResult(LuctreaDrawResult result) => _luctreaDraw[Key(result.BaseType, result.SubType)] = result;
        public LuctreaDrawResult GetLuctreaDrawResult(int baseType, int subType) =>
            _luctreaDraw.TryGetValue(Key(baseType, subType), out LuctreaDrawResult v) ? v : null;

        // ============================================================================================
        // §5 FORTUNECAT(87):33224 信息 / 33225 转盘(抽奖) / 33226 转盘记录(pt_332.erl:717-749/751-767/769-795)
        // ============================================================================================

        public sealed class FortunecatRound { public long Rounds; public long MaxNum; public long MinNum; public long RewardId; } // item_to_bin_16,RewardId:64

        /// <summary>item_to_bin_17,4 字段(GradeId:16,GoodsId:32,GoodsNum:32,IsHead:8)——侦察表漏记
        /// GradeId/IsHead,见本文件头注释订正记录 3)。</summary>
        public sealed class FortunecatRewardConfig { public int GradeId; public int GoodsId; public int GoodsNum; public int IsHead; }

        public sealed class FortunecatInfo
        {
            public int BaseType;
            public int SubType;
            public long Turns;
            public int CgoodsId;
            public int CgoodsNum;
            public readonly List<FortunecatRound> RoundsList = new List<FortunecatRound>();
            public readonly List<FortunecatRewardConfig> RewardList = new List<FortunecatRewardConfig>();
        }

        private readonly Dictionary<long, FortunecatInfo> _fortunecatInfo = new Dictionary<long, FortunecatInfo>();
        public void SetFortunecatInfo(FortunecatInfo info) => _fortunecatInfo[Key(info.BaseType, info.SubType)] = info;
        public FortunecatInfo GetFortunecatInfo(int baseType, int subType) =>
            _fortunecatInfo.TryGetValue(Key(baseType, subType), out FortunecatInfo v) ? v : null;

        public sealed class FortunecatDrawResult { public int Code; public int BaseType; public int SubType; public int GradeId; public int GoodsId; public int GoodsNum; }

        private readonly Dictionary<long, FortunecatDrawResult> _fortunecatDraw = new Dictionary<long, FortunecatDrawResult>();
        public void SetFortunecatDrawResult(FortunecatDrawResult result) => _fortunecatDraw[Key(result.BaseType, result.SubType)] = result;
        public FortunecatDrawResult GetFortunecatDrawResult(int baseType, int subType) =>
            _fortunecatDraw.TryGetValue(Key(baseType, subType), out FortunecatDrawResult v) ? v : null;

        public sealed class FortunecatRecordEntry { public long RoleId; public string RoleName = ""; public int GoodsId; public int GoodsNum; } // item_to_bin_18/19,同构

        public sealed class FortunecatRecord
        {
            public int BaseType;
            public int SubType;
            public readonly List<FortunecatRecordEntry> SelfList = new List<FortunecatRecordEntry>();
            public readonly List<FortunecatRecordEntry> GolbList = new List<FortunecatRecordEntry>();
        }

        private readonly Dictionary<long, FortunecatRecord> _fortunecatRecord = new Dictionary<long, FortunecatRecord>();
        public void SetFortunecatRecord(FortunecatRecord record) => _fortunecatRecord[Key(record.BaseType, record.SubType)] = record;
        public FortunecatRecord GetFortunecatRecord(int baseType, int subType) =>
            _fortunecatRecord.TryGetValue(Key(baseType, subType), out FortunecatRecord v) ? v : null;

        // ============================================================================================
        // §6 BIND_JAGE_WISH(127):33260 心愿单信息 / 33262 开抽 / 33263 免费礼(pt_332.erl:1526-1544/1552-1568/1570-1580)
        // ============================================================================================

        public sealed class BindJageInfo { public int Type; public int Subtype; public int FreeTimes; public int IsFirstRecharge; public int Turn; public int Times; public int FreeGiftStatus; }

        private readonly Dictionary<long, BindJageInfo> _bindJageInfo = new Dictionary<long, BindJageInfo>();
        public void SetBindJageInfo(BindJageInfo info) => _bindJageInfo[Key(info.Type, info.Subtype)] = info;
        public BindJageInfo GetBindJageInfo(int type, int subtype) =>
            _bindJageInfo.TryGetValue(Key(type, subtype), out BindJageInfo v) ? v : null;

        /// <summary>33262 开抽结果。C2S 只发 Type,SubType(见本文件头注释订正记录,pt_332.erl read(33262):234-237);
        /// Grade/Turn/Times 由服务端自算并回填,失败时 Grade 固定 0(pp_custom_act_list.erl:581)。Errcode 末尾。</summary>
        public sealed class BindJageDrawResult { public int Type; public int Subtype; public int Grade; public int Turn; public int Times; public int Code; }

        private readonly Dictionary<long, BindJageDrawResult> _bindJageDraw = new Dictionary<long, BindJageDrawResult>();
        public void SetBindJageDrawResult(BindJageDrawResult result) => _bindJageDraw[Key(result.Type, result.Subtype)] = result;
        public BindJageDrawResult GetBindJageDrawResult(int type, int subtype) =>
            _bindJageDraw.TryGetValue(Key(type, subtype), out BindJageDrawResult v) ? v : null;

        public sealed class BindJageFreeGiftResult { public int Type; public int Subtype; public int Code; }

        private readonly Dictionary<long, BindJageFreeGiftResult> _bindJageFreeGift = new Dictionary<long, BindJageFreeGiftResult>();
        public void SetBindJageFreeGiftResult(BindJageFreeGiftResult result) => _bindJageFreeGift[Key(result.Type, result.Subtype)] = result;
        public BindJageFreeGiftResult GetBindJageFreeGiftResult(int type, int subtype) =>
            _bindJageFreeGift.TryGetValue(Key(type, subtype), out BindJageFreeGiftResult v) ? v : null;

        // ============================================================================================
        // §7 生命周期(已挂钩):CustomActivityModel.cs 的 Clear()〔轮17收口〕已在级联里调用 ClearLotteryB(),
        // 本包 7 组字典随断线/登出 Instance.Clear() 一并清空;ClearLotteryB() 同时保留独立可调用,供
        // CliVerify Case 单段复位用。
        // ============================================================================================

        public void ClearLotteryB()
        {
            _gashaponInfo.Clear(); _gashaponDraw.Clear();
            _luctrea2Info.Clear(); _luctrea2Draw.Clear();
            _onlineDrawInfo.Clear(); _goodsPower.Clear();
            _luctreaPool.Clear(); _luctreaDraw.Clear();
            _fortunecatInfo.Clear(); _fortunecatDraw.Clear(); _fortunecatRecord.Clear();
            _bindJageInfo.Clear(); _bindJageDraw.Clear(); _bindJageFreeGift.Clear();
        }
    }
}
