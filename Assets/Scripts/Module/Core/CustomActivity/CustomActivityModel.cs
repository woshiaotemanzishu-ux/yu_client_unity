using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 定制活动(331xx/332xx+225xx补全+224xx+159xx,自动循环 轮17)框架数据层通用容器。partial:本文件(P1)
    /// 只放框架级通用段(活动列表/单活动通用详情/领奖回执/全服计数);P2-P6 各自的类型化子数据段在
    /// CustomActivityModel.LotteryA.cs/.LotteryB.cs/.Festival.cs/.Biz.cs/.Kf.cs(P1 预建空壳)。
    ///
    /// 对标老端 commonModel/CustomActivityModel.ts 的数据子集:SaveActInfo/AddActInfo/DeleteActInfo
    /// (Model.ts:349-399,仅落地存储,老端的红点/图标/RequireActInfo 联动等 UI 侧特判本轮不镜像——纯数据层轮,
    /// UI 消费方留尾包,同 15a/15b/16 先例)。33104 的 reward_list 字段序已用 pt_331.erl:2236 item_to_bin_3 +
    /// ClientProtocol.json "33104" 双源互证订正为 8 字段(Grade/FormType/Status/ReceiveTimes/Name/Desc/
    /// Condition/Reward),早期侦察表"Type:8,Value:32"系误记 33107(消费统计,死号)的结构,见 Proto.cs
    /// CUSTOM_ACT_DETAIL 常量注释。
    /// </summary>
    public sealed partial class CustomActivityModel
    {
        public static readonly CustomActivityModel Instance = new CustomActivityModel();
        private CustomActivityModel() { }

        // ============================================================================================
        // §1 活动列表(33101/33102/33103,item_to_bin_0/1 同构,item_to_bin_2 仅 BaseType+SubType)
        // ============================================================================================

        /// <summary>对标 pt_331.erl item_to_bin_0/item_to_bin_1(33101 List / 33102 List 同构)。</summary>
        public sealed class ActEntry
        {
            public int BaseType;
            public int SubType;
            public int ActType;
            public int ShowId;
            public int Wlv;
            public string Name = "";
            public string Desc = "";
            public string Condition = "";
            public long Stime;
            public long Etime;
        }

        /// <summary>按 pt_331.erl item_to_bin_0 字段序读取单条(33101/33102 共用,字段完全一致)。</summary>
        public static ActEntry ReadActEntry(NetReader r) => new ActEntry
        {
            BaseType = r.ReadU16(),
            SubType = r.ReadU16(),
            ActType = r.ReadU8(),
            ShowId = r.ReadU16(),
            Wlv = r.ReadU16(),
            Name = r.ReadString(),
            Desc = r.ReadString(),
            Condition = r.ReadString(),
            Stime = r.ReadU32(),
            Etime = r.ReadU32(),
        };

        // key = BaseType*1000000L+SubType(SubType 实测均 < 1000000,老端 sub_type 多为个位/两位数字典键,足够避免碰撞)。
        private readonly Dictionary<long, ActEntry> _actList = new Dictionary<long, ActEntry>();
        public IReadOnlyDictionary<long, ActEntry> ActList => _actList;

        private static long Key(int baseType, int subType) => (long)baseType * 1000000L + subType;

        /// <summary>对标老端 SaveActInfo(Model.ts:349-399,仅落地部分):清空重建整份列表。
        /// 33101 On33101 已有自己的 _cachedList 用于图标增量对比,本方法复用同一份解析结果落 Model,
        /// 不重新读取 NetReader(避免游标二次消费)。</summary>
        public void SaveActList(IReadOnlyList<ActEntry> list)
        {
            _actList.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                ActEntry e = list[i];
                _actList[Key(e.BaseType, e.SubType)] = e;
            }
        }

        /// <summary>对标老端 AddActInfo(Model.ts:417-470,仅落地部分):增量合并。</summary>
        public void AddActEntries(IReadOnlyList<ActEntry> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ActEntry e = list[i];
                _actList[Key(e.BaseType, e.SubType)] = e;
            }
        }

        /// <summary>对标老端 DeleteActInfo:按 (BaseType,SubType) 移除。</summary>
        public void RemoveActEntries(IReadOnlyList<(int BaseType, int SubType)> keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                _actList.Remove(Key(keys[i].BaseType, keys[i].SubType));
            }
        }

        public ActEntry GetActEntry(int baseType, int subType) =>
            _actList.TryGetValue(Key(baseType, subType), out ActEntry e) ? e : null;

        // ============================================================================================
        // §2 单活动通用详情(33104,per(base,sub) 通用容器)
        // ============================================================================================

        /// <summary>对标 pt_331.erl item_to_bin_3(33104 RewardList 单条,8 字段)。</summary>
        public sealed class DetailReward
        {
            public int Grade;
            public int FormType;
            public int Status;
            public int ReceiveTimes;
            public string Name = "";
            public string Desc = "";
            public string Condition = "";
            public string Reward = "";
        }

        public sealed class DetailData
        {
            public int BaseType;
            public int SubType;
            public readonly List<DetailReward> RewardList = new List<DetailReward>();
        }

        private readonly Dictionary<long, DetailData> _details = new Dictionary<long, DetailData>();

        public void SetDetail(int baseType, int subType, List<DetailReward> rewardList)
        {
            var d = new DetailData { BaseType = baseType, SubType = subType };
            d.RewardList.AddRange(rewardList);
            _details[Key(baseType, subType)] = d;
        }

        public DetailData GetDetail(int baseType, int subType) =>
            _details.TryGetValue(Key(baseType, subType), out DetailData d) ? d : null;

        // ============================================================================================
        // §3 通用领取/操作结果(33105)
        // ============================================================================================

        public sealed class ClaimResult
        {
            public int BaseType;
            public int SubType;
            public int Grade;
            public int Code;
        }

        private readonly Dictionary<long, ClaimResult> _claimResults = new Dictionary<long, ClaimResult>();

        public void SetClaimResult(int baseType, int subType, int grade, int code)
        {
            _claimResults[Key(baseType, subType)] = new ClaimResult
            {
                BaseType = baseType, SubType = subType, Grade = grade, Code = code,
            };
        }

        public ClaimResult GetClaimResult(int baseType, int subType) =>
            _claimResults.TryGetValue(Key(baseType, subType), out ClaimResult c) ? c : null;

        // ============================================================================================
        // §4 全服计数(33106,按 BaseType+SubType+ModId+CounterId 四元键——同一活动可能有多组计数器)
        // ============================================================================================

        public sealed class AllCountEntry
        {
            public int BaseType;
            public int SubType;
            public int ModId;
            public int CounterId;
            public int Count;
            public int Grade;
        }

        private readonly Dictionary<(int BaseType, int SubType, int ModId, int CounterId), AllCountEntry> _allCounts =
            new Dictionary<(int, int, int, int), AllCountEntry>();

        public void SetAllCount(AllCountEntry entry)
        {
            _allCounts[(entry.BaseType, entry.SubType, entry.ModId, entry.CounterId)] = entry;
        }

        public AllCountEntry GetAllCount(int baseType, int subType, int modId, int counterId) =>
            _allCounts.TryGetValue((baseType, subType, modId, counterId), out AllCountEntry e) ? e : null;

        // ============================================================================================
        // §5 生命周期
        // ============================================================================================

        /// <summary>断线/登出清空(对标 Marriage/Boss 先例)。P2-P6 各 partial 段的类型化数据由各自
        /// ClearXxx() 清空,在此统一级联(轮17收口,主控单点挂接)。</summary>
        public void Clear()
        {
            _actList.Clear();
            _details.Clear();
            _claimResults.Clear();
            _allCounts.Clear();
            ClearLotteryA();
            ClearLotteryB();
            ClearFestival();
            ClearBiz();
            ClearKf();
        }
    }
}
