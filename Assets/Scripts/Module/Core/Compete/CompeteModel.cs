using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Shenxiao.Module.Core.Compete
{
    /// <summary>
    /// 竞榜(赛事活动)数据(对标老客户端 commonModel/CompeteListModel)。承载 33800 下发的
    /// "正在开启的赛事活动列表"(玄鸢千寻/圣殿狮鹫/急速飞车/背饰… 100+ 种,由同一份列表驱动),
    /// 供主界面图标(338@type@subtype 家族)显隐用；33801/33802 另存按 type/subtype 键控的
    /// 不可变原始快照，不接 UI、配置、红点、抽奖或领奖语义。
    ///
    /// 图标类型解析:老端为 Trim(config_race_act_info[type@subtype].icon),该配置属数据侧,
    /// 本工程未导入 → 直接按字面量 "338@"+type+"@"+subtype 组装(已核对 configfunctionicon.json
    /// 内 338 家族键正是此格式,如 338@10@1 / 338@11@3 / 338@12@5)。老端 config.act_type==0
    /// (无单独图标)的活动本工程无从判定,改由 configfunctionicon 是否存在该键兜底:
    /// ActivityIconManager.AddIcon 对 GetFunctionIconCfg==null 直接 no-op,等价于"没配图标就不显示"。
    /// </summary>
    public sealed class CompeteModel
    {
        public static readonly CompeteModel Instance = new CompeteModel();

        public sealed class ObjectEntry
        {
            public byte Style { get; }
            public uint TypeId { get; }
            public uint Num { get; }

            public ObjectEntry(byte style, uint typeId, uint num)
            {
                Style = style;
                TypeId = typeId;
                Num = num;
            }
        }

        public sealed class StageEntry
        {
            public ushort Id { get; }
            public byte GotType { get; }

            public StageEntry(ushort id, byte gotType)
            {
                Id = id;
                GotType = gotType;
            }
        }

        public sealed class ViewSnapshot
        {
            public ushort Type { get; }
            public ushort Subtype { get; }
            public byte IsOpen { get; }
            public uint Score { get; }
            public uint TodayScore { get; }
            public IReadOnlyList<ObjectEntry> Cost { get; }
            public IReadOnlyList<ObjectEntry> TenCost { get; }
            public IReadOnlyList<ushort> RewardIds { get; }
            public IReadOnlyList<StageEntry> Stages { get; }
            public uint WorldLevel { get; }

            public ViewSnapshot(
                ushort type,
                ushort subtype,
                byte isOpen,
                uint score,
                uint todayScore,
                List<ObjectEntry> cost,
                List<ObjectEntry> tenCost,
                List<ushort> rewardIds,
                List<StageEntry> stages,
                uint worldLevel)
            {
                Type = type;
                Subtype = subtype;
                IsOpen = isOpen;
                Score = score;
                TodayScore = todayScore;
                Cost = new List<ObjectEntry>(cost ?? new List<ObjectEntry>()).AsReadOnly();
                TenCost = new List<ObjectEntry>(tenCost ?? new List<ObjectEntry>()).AsReadOnly();
                RewardIds = new List<ushort>(rewardIds ?? new List<ushort>()).AsReadOnly();
                Stages = new List<StageEntry>(stages ?? new List<StageEntry>()).AsReadOnly();
                WorldLevel = worldLevel;
            }
        }

        public sealed class RankEntry
        {
            public ushort Rank { get; }
            public uint ServerId { get; }
            public ulong RoleId { get; }
            public string RoleName { get; }
            public uint RoleScore { get; }

            public RankEntry(ushort rank, uint serverId, ulong roleId, string roleName, uint roleScore)
            {
                Rank = rank;
                ServerId = serverId;
                RoleId = roleId;
                RoleName = roleName;
                RoleScore = roleScore;
            }
        }

        public sealed class RankSnapshot
        {
            public ushort Type { get; }
            public ushort Subtype { get; }
            public uint Score { get; }
            public ushort Rank { get; }
            public IReadOnlyList<RankEntry> Entries { get; }

            public RankSnapshot(ushort type, ushort subtype, uint score, ushort rank, List<RankEntry> entries)
            {
                Type = type;
                Subtype = subtype;
                Score = score;
                Rank = rank;
                Entries = new List<RankEntry>(entries ?? new List<RankEntry>()).AsReadOnly();
            }
        }

        private CompeteModel()
        {
            _readOnlyViews = new ReadOnlyDictionary<uint, ViewSnapshot>(_views);
            _readOnlyRanks = new ReadOnlyDictionary<uint, RankSnapshot>(_ranks);
        }

        /// <summary>竞榜图标家族号(对标老端 CompeteListModel.IsBillboardAct 判定 "338")。</summary>
        public const string ICON_FAMILY = "338";

        /// <summary>33800 单条赛事活动(对标老端 item_to_bin_0 / SCMD act_list 元素)。</summary>
        public struct RaceActInfo
        {
            public int Type;        // 活动大类
            public int Subtype;     // 活动子类
            public int ShowId;      // 展示id
            public long StartTime;   // u32 开始时间戳
            public long EndTime;     // u32 结束时间戳
            public long BuyEndTime;  // u32 购买/参与结束时间戳(图标倒计时用,对标老端 addIcon 传的 buy_end_time)
        }

        private readonly List<RaceActInfo> _actList = new List<RaceActInfo>();
        private readonly Dictionary<uint, ViewSnapshot> _views = new Dictionary<uint, ViewSnapshot>();
        private readonly IReadOnlyDictionary<uint, ViewSnapshot> _readOnlyViews;
        private readonly Dictionary<uint, RankSnapshot> _ranks = new Dictionary<uint, RankSnapshot>();
        private readonly IReadOnlyDictionary<uint, RankSnapshot> _readOnlyRanks;

        public IReadOnlyList<RaceActInfo> ActList => _actList;
        public IReadOnlyDictionary<uint, ViewSnapshot> Views => _readOnlyViews;
        public IReadOnlyDictionary<uint, RankSnapshot> Ranks => _readOnlyRanks;

        public void SetActList(List<RaceActInfo> list)
        {
            _actList.Clear();
            if (list != null) _actList.AddRange(list);
        }

        public static uint MakeKey(ushort type, ushort subtype)
        {
            return ((uint)type << 16) | subtype;
        }

        public bool TryGetViewInfo(ushort type, ushort subtype, out ViewSnapshot snapshot)
        {
            return _views.TryGetValue(MakeKey(type, subtype), out snapshot);
        }

        public bool TryGetRankInfo(ushort type, ushort subtype, out RankSnapshot snapshot)
        {
            return _ranks.TryGetValue(MakeKey(type, subtype), out snapshot);
        }

        public void ReplaceViewInfo(ViewSnapshot snapshot)
        {
            if (snapshot == null) return;
            _views[MakeKey(snapshot.Type, snapshot.Subtype)] = snapshot;
        }

        public void ReplaceRankInfo(RankSnapshot snapshot)
        {
            if (snapshot == null) return;
            _ranks[MakeKey(snapshot.Type, snapshot.Subtype)] = snapshot;
        }

        /// <summary>
        /// 组装图标类型 "338@"+type+"@"+subtype(对标老端 Trim(config.icon) 解析出的 338 家族键)。
        /// config_race_act_info 数据侧未导入,故按字面量组装;是否真能显示由 configfunctionicon
        /// 是否存在该键 + open_lv/open_day 门槛在 ActivityIconManager 侧把关。
        /// </summary>
        public static string BuildIconType(int type, int subtype)
        {
            return ICON_FAMILY + "@" + type + "@" + subtype;
        }

        public void Reset()
        {
            _actList.Clear();
            _views.Clear();
            _ranks.Clear();
        }
    }
}
