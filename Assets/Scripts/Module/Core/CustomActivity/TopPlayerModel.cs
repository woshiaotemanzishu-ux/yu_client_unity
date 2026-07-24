using System.Collections.Generic;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 头号玩家(TOP_PLAYER, base_type=10)运行时数据。对标老客户端 CustomActivityModel 里
    /// base_type=10/sub_type=1 的 rank_info。仅承载主界面图标展示所需的最小数据:
    /// 某 rank_type 的榜首名、结束时间(倒计时)。活动名由 config_rush_rank 补。
    /// </summary>
    public sealed class TopPlayerModel
    {
        public static readonly TopPlayerModel Instance = new TopPlayerModel();
        private TopPlayerModel() { }

        public const int ACT_BASE_TYPE = 10; // ACT_ID.TOP_PLAYER
        public const int ACT_SUB_TYPE = 1;
        public const string ICON_TYPE = "331@10@0";

        public sealed class RankRoleVo
        {
            public long PlayerId;
            public string Name = "";
            public long FirstValue;
            public int Rank;
        }

        public sealed class RankInfo
        {
            public int RankType;
            public int Status;
            public long EndTime;       // unix 秒;0=无倒计时
            public int IsCombat;
            public readonly List<RankRoleVo> RankList = new List<RankRoleVo>();

            /// <summary>榜首名(rank==1);无则返回 null。</summary>
            public string FirstName()
            {
                for (int i = 0; i < RankList.Count; i++)
                    if (RankList[i].Rank == 1) return RankList[i].Name;
                return null;
            }
        }

        private readonly Dictionary<int, RankInfo> _rankInfoByType = new Dictionary<int, RankInfo>();
        private bool _hasGoalRed;

        public RankInfo LatestRankInfo { get; private set; }

        public void SetRankInfo(RankInfo info)
        {
            if (info == null) return;
            _rankInfoByType[info.RankType] = info;
            LatestRankInfo = info;
        }

        public RankInfo GetRankInfo(int rankType)
            => _rankInfoByType.TryGetValue(rankType, out RankInfo v) ? v : null;

        public void SetGoalRed(bool value) => _hasGoalRed = value;

        public bool HasEntranceRedDot()
        {
            if (_hasGoalRed) return true;
            foreach (KeyValuePair<int, RankInfo> pair in _rankInfoByType)
            {
                if (pair.Value != null && pair.Value.Status == 1) return true;
            }
            return false;
        }

        public void Reset()
        {
            _rankInfoByType.Clear();
            _hasGoalRed = false;
            LatestRankInfo = null;
        }
    }
}
