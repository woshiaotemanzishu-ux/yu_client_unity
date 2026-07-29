using System.Collections.Generic;

namespace Shenxiao.Module.Core.CycleimpActlist
{
    /// <summary>
    /// 循环冲榜 / 竞榜(对标老客户端 commonModel/CycleimpActlistModel)。承载主界面 _box_rank 竞榜展示所需的最小数据:
    /// 当前活动 type/subtype、起止/上榜时间、今日榜单(取榜首)。完整活动面板(22701 个人信息/领奖)后续切片。
    /// 由 <see cref="CycleimpActlistController"/> 按 22700/22702/22705/22706 填充;活动视图监听 EVT_CYCLEIMP_DATA 读展示切片。
    /// 22705 只保存独立原始推送，不接入旧端弹窗副作用。
    /// </summary>
    public sealed class CycleimpActlistModel
    {
        public static readonly CycleimpActlistModel Instance = new CycleimpActlistModel();
        private CycleimpActlistModel() { }

        public sealed class RankRoleVo
        {
            public int Rank;
            public int ServerId;
            public long RoleId;
            public string RoleName = "";
            public long RoleScore;
        }

        public sealed class RankProgressNotice
        {
            public readonly ushort RankType;
            public readonly ushort RankSubtype;
            public readonly uint Type;
            public readonly uint Rank;
            public readonly uint Value;

            public RankProgressNotice(ushort rankType, ushort rankSubtype, uint type, uint rank, uint value)
            {
                RankType = rankType;
                RankSubtype = rankSubtype;
                Type = type;
                Rank = rank;
                Value = value;
            }
        }

        /// <summary>当前开启的竞榜主类型(0/未设=无活动)。决定 _box_rank 走竞榜分支还是放行头号玩家分支。</summary>
        public int Type { get; private set; }
        public int Subtype { get; private set; }
        public long StartTime { get; private set; }
        public long EndTime { get; private set; }
        /// <summary>上榜截止时间(unix 秒),倒计时取它(对标老端 StartTimer(timer_info.upon_end_time))。</summary>
        public long UponEndTime { get; private set; }

        public bool HasActivity => Type != 0 && Subtype != 0;

        private readonly List<RankRoleVo> _nowRankList = new List<RankRoleVo>();
        public IReadOnlyList<RankRoleVo> NowRankList => _nowRankList;
        public bool HasRankProgressNotice { get; private set; }
        public RankProgressNotice LastRankProgressNotice { get; private set; }

        /// <summary>22700:活动起止时间 + type/subtype。</summary>
        public void SetActTime(int type, int subtype, long startTime, long endTime, long uponEndTime)
        {
            Type = type;
            Subtype = subtype;
            StartTime = startTime;
            EndTime = endTime;
            UponEndTime = uponEndTime;
        }

        /// <summary>22702:今日榜单。</summary>
        public void SetRankList(IEnumerable<RankRoleVo> list)
        {
            _nowRankList.Clear();
            if (list != null) _nowRankList.AddRange(list);
        }

        /// <summary>22706:榜首主动推送(只有一名 rank==1 时刷新榜首)。</summary>
        public void SetFirstHolder(RankRoleVo first)
        {
            if (first == null) return;
            for (int i = 0; i < _nowRankList.Count; i++)
            {
                if (_nowRankList[i].Rank == 1) { _nowRankList[i] = first; return; }
            }
            _nowRankList.Add(first);
        }

        /// <summary>榜首(rank==1 且有 role_id);无则 null(展示“虚位以待”)。</summary>
        public RankRoleVo GetFirstHolder()
        {
            for (int i = 0; i < _nowRankList.Count; i++)
            {
                RankRoleVo v = _nowRankList[i];
                if (v.Rank == 1 && v.RoleId != 0) return v;
            }
            return null;
        }

        /// <summary>22705:仅接收的原始榜单变化通知；零值也是有效完整快照。</summary>
        public void ReplaceRankProgressNotice(ushort rankType, ushort rankSubtype, uint type, uint rank, uint value)
        {
            LastRankProgressNotice = new RankProgressNotice(rankType, rankSubtype, type, rank, value);
            HasRankProgressNotice = true;
        }

        /// <summary>只清 22700/22702/22706 活动展示切片；不得跨清独立的 22705 推送切片。</summary>
        public void Clear()
        {
            Type = 0;
            Subtype = 0;
            StartTime = 0;
            EndTime = 0;
            UponEndTime = 0;
            _nowRankList.Clear();
        }

        public void Reset()
        {
            Clear();
            HasRankProgressNotice = false;
            LastRankProgressNotice = null;
        }
    }
}
