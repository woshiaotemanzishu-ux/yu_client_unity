using System.Collections.Generic;

namespace Shenxiao.Module.Core.CustomActivity
{
    // LIST_DUOBAO=116 夺宝积分墙(自动循环 轮21 PF 补漏批;pt_332.erl 33252/33253/33254;老端独立
    // commonController/ListDuobaoController.ts + commonModel/ListDuobaoModel.ts,不是主
    // CustomActivityController.ts/CustomActivityModel.ts 的一部分)。base_type/sub_type 语义与本文件
    // 其余 P2-P6 家族同构(sub_type 由 33101 列表里 base_type==116 的条目落地时确定,全程只有一个活跃
    // 子活动,对标老端 ListDuobaoModel.sub_type 单值存储),故并入同一 partial class 复用 ACT_ID 常量/
    // RewardObj/Key 等基建,不新建独立单例。类型化数据由 CustomActivityController.List.cs 的 handler 落地。
    public sealed partial class CustomActivityModel
    {
        /// <summary>33252 阶段奖励条目(item_to_bin_40,pt_332.erl:2234-2246):GradeId:16, IsRare:8,
        /// Reward:ObjectList(嵌套,复用 P5 <see cref="ReadRewardObjList"/>)。</summary>
        public struct ListDuobaoStageReward
        {
            public int GradeId;
            public int IsRare;
            public List<RewardObj> Reward;
        }

        /// <summary>33252 阶段完成状态(item_to_bin_41,pt_332.erl:2247-2255):Id:16, GotType:8。</summary>
        public struct ListDuobaoStageState
        {
            public int Id;
            public int GotType;
        }

        /// <summary>33253 服内排行条目(item_to_bin_42,pt_332.erl:2256-2272):Rank:16, ServerId:32,
        /// RoleId:64, RoleName:s, RoleScore:32。</summary>
        public struct ListDuobaoRankEntry
        {
            public int Rank;
            public int ServerId;
            public long RoleId;
            public string RoleName;
            public int RoleScore;
        }

        /// <summary>33253 跨服排行条目(item_to_bin_43,pt_332.erl:2273-2287):Rank:16, ServerId:32,
        /// ServerName:s, ServerScore:32。</summary>
        public struct ListDuobaoServerRankEntry
        {
            public int Rank;
            public int ServerId;
            public string ServerName;
            public int ServerScore;
        }

        /// <summary>33252 回包落地(对标老端 ListDuobaoModel.SetData)。</summary>
        public sealed class ListDuobaoStageInfo
        {
            public int SubType;
            public int Score;
            public int TodayScore;
            public string Condition = "";
            public readonly List<ListDuobaoStageReward> RewardList = new List<ListDuobaoStageReward>();
            public readonly List<ListDuobaoStageState> StageList = new List<ListDuobaoStageState>();
            public int WorldLv;
        }

        /// <summary>33253 回包落地(对标老端 ListDuobaoModel.SetRankData)。</summary>
        public sealed class ListDuobaoRankInfo
        {
            public int SubType;
            public int Score;
            public int Rank;
            public readonly List<ListDuobaoRankEntry> RankList = new List<ListDuobaoRankEntry>();
            public int ServerScore;
            public int ServerRank;
            public readonly List<ListDuobaoServerRankEntry> ServerRankList = new List<ListDuobaoServerRankEntry>();
        }

        /// <summary>当前唯一活跃夺宝子活动的 sub_type(对标老端 ListDuobaoModel.sub_type,由
        /// CustomActivityController.List.cs 在 33101 列表里扫到 base_type==116 的条目时设置);未确定 → -1。</summary>
        public int ListDuobaoSubType { get; private set; } = -1;

        public ListDuobaoStageInfo ListDuobaoStage { get; private set; }
        public ListDuobaoRankInfo ListDuobaoRank { get; private set; }

        public void SetListDuobaoSubType(int subType) => ListDuobaoSubType = subType;
        public void SetListDuobaoStage(ListDuobaoStageInfo info) => ListDuobaoStage = info;
        public void SetListDuobaoRank(ListDuobaoRankInfo info) => ListDuobaoRank = info;

        public void ClearList()
        {
            ListDuobaoSubType = -1;
            ListDuobaoStage = null;
            ListDuobaoRank = null;
        }
    }
}
