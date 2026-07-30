using System.Collections.Generic;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 周本(极·boss,DUN_TYPE=36 Polar,pt_508)数据层——对标老端 BaseDungeonModel 的
    /// polarInfo_[week_dun_id] / polarRank_[team_dun_id] 两张独立索引表，以及50805最后完整结算快照。
    /// ⚠r9 侦察结论:周本与 61xxx 通用副本是两条完全独立的数据/事件线,老端 GetDungeonInfo(Polar)
    /// 事实上走不到 DunStatesByType 那条路——**勿把周本状态塞进 DungeonModel.DunStatesByType[36]**。
    /// 收发在 DungeonController(50801/50802/50805)；结算只落 raw，不复用61003，也不驱动UI或本地发奖。
    /// </summary>
    public sealed class PolarModel
    {
        public static readonly PolarModel Instance = new PolarModel();
        private PolarModel() { }

        /// <summary>老端 polarRankMax=10(50802 固定查第 1~10 名;服务端 guard Rank1&lt;Rank2 且 Rank2≤30)。</summary>
        public const int RANK_MAX = 10;

        public sealed class SpecialInfoSnapshot
        {
            public uint DunId { get; }
            public byte DunType { get; }
            public byte PushType { get; }
            public string Content { get; }

            public SpecialInfoSnapshot(uint dunId, byte dunType, byte pushType, string content)
            {
                DunId = dunId;
                DunType = dunType;
                PushType = pushType;
                Content = content ?? string.Empty;
            }
        }

        /// <summary>61088 按 dun_id 保存最后一个完整原始包；局部 push_type=2 也不与旧 term 合并。</summary>
        public readonly Dictionary<uint, SpecialInfoSnapshot> SpecialInfoByDunId =
            new Dictionary<uint, SpecialInfoSnapshot>();

        public void ApplySpecialInfo(uint dunId, byte dunType, byte pushType, string content)
        {
            SpecialInfoByDunId[dunId] = new SpecialInfoSnapshot(dunId, dunType, pushType, content);
        }

        public bool TryGetSpecialInfo(uint dunId, out SpecialInfoSnapshot snapshot) =>
            SpecialInfoByDunId.TryGetValue(dunId, out snapshot);

        // ===================================================================================
        // 50801 周常本信息(对标老端 setPolarInfo → polarInfo_[week_dun_id])
        // ===================================================================================

        public sealed class BossRewardVo
        {
            public int BossId;
            /// <summary>周奖励领取状态。</summary>
            public int RewardSt;
        }

        public sealed class WeekInfoVo
        {
            public int WeekDunId;
            public int DunScore;
            /// <summary>单人通关状态。</summary>
            public int SingleSucc;
            /// <summary>组队通关状态。</summary>
            public int TeamSucc;
            public int HelpTimes;
            public List<BossRewardVo> BossReward = new List<BossRewardVo>();
        }

        private readonly Dictionary<int, WeekInfoVo> _weekInfos = new Dictionary<int, WeekInfoVo>();

        /// <summary>按 week_dun_id 索引的周本信息(50801 全量覆盖式落地)。</summary>
        public IReadOnlyDictionary<int, WeekInfoVo> WeekInfos => _weekInfos;

        public WeekInfoVo GetWeekInfo(int weekDunId) =>
            _weekInfos.TryGetValue(weekDunId, out WeekInfoVo v) ? v : null;

        /// <summary>50801 落地(对标老端 setPolarInfo:逐条按 week_dun_id 覆盖)。</summary>
        public void SetWeekInfos(List<WeekInfoVo> list)
        {
            if (list == null) return;
            foreach (WeekInfoVo vo in list)
                _weekInfos[vo.WeekDunId] = vo;
        }

        // ===================================================================================
        // 50802 榜单(对标老端 setPolarRank → polarRank_[team_dun_id])
        // ===================================================================================

        public sealed class RankRoleVo
        {
            public long RoleId;
            public string RoleName = "";
            public int ServerId;
            public int ServerNum;
        }

        public sealed class RankEntryVo
        {
            public int PassTime;
            public int Time;
            public int Rank;
            /// <summary>同一排名下的多个组队成员。</summary>
            public List<RankRoleVo> Roles = new List<RankRoleVo>();
        }

        public sealed class RankVo
        {
            public int TeamDunId;
            public int SelfRank;
            public int SelfPassTime;
            public List<RankEntryVo> Entries = new List<RankEntryVo>();
        }

        private readonly Dictionary<int, RankVo> _ranks = new Dictionary<int, RankVo>();

        public RankVo GetRank(int teamDunId) => _ranks.TryGetValue(teamDunId, out RankVo v) ? v : null;

        /// <summary>50802 落地(对标老端 setPolarRank:按 team_dun_id 整表覆盖)。</summary>
        public void SetRank(RankVo vo)
        {
            if (vo == null) return;
            _ranks[vo.TeamDunId] = vo;
        }

        // ===================================================================================
        // 50805 周本专属结算(S2C-only，逐包完整替换)
        // ===================================================================================

        public sealed class ObjectRewardVo
        {
            public byte Type { get; }
            public uint TypeId { get; }
            public uint Num { get; }

            public ObjectRewardVo(byte type, uint typeId, uint num)
            {
                Type = type;
                TypeId = typeId;
                Num = num;
            }
        }

        public sealed class SettlementRewardVo
        {
            public byte Type;
            public ushort Times;
            public List<ObjectRewardVo> Rewards = new List<ObjectRewardVo>();
        }

        public sealed class SettlementBossVo
        {
            public uint BossId;
            public byte RewardState;
            public List<ObjectRewardVo> Rewards = new List<ObjectRewardVo>();
        }

        public sealed class SettlementSnapshot
        {
            public byte ResultType;
            public uint DunId;
            public uint GoTime;
            public List<SettlementRewardVo> DungeonRewards = new List<SettlementRewardVo>();
            public List<SettlementBossVo> RoleBosses = new List<SettlementBossVo>();
        }

        public bool HasSettlement { get; private set; }
        public SettlementSnapshot Settlement { get; private set; }

        public void ReplaceSettlement(SettlementSnapshot snapshot)
        {
            Settlement = snapshot;
            HasSettlement = snapshot != null;
        }

        /// <summary>断线/登出清空。</summary>
        public void Clear()
        {
            _weekInfos.Clear();
            _ranks.Clear();
            SpecialInfoByDunId.Clear();
            HasSettlement = false;
            Settlement = null;
        }
    }
}
