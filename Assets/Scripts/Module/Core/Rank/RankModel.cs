using System.Collections.Generic;
using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Rank
{
    /// <summary>
    /// 排行榜数据层(自动循环 轮12 #12;纯数据层轮,对标老端 commonModel/RankModel.ts)。
    /// 只承载 22101(个人榜查询)一条数据线,按 rank_type 分槽存(14 种,枚举照 config_ranking)。
    ///
    /// **wire Sum 字段更正(轮12 blocker 修复)**:服务端 lib_common_rank_mod.erl 正常分支(:1220)与越界
    /// 分支(:1190)**都**把 Sum 字段位置填成客户端请求的 Len,不是 :1183 算出的真实 Sum=length(RankList)——
    /// 该字段在生产上恒为请求 len 的回声,从不反映真实总数,不能用于判断"是否越界"或驱动分页(旧实现的
    /// "只增不减/越界终止"防御建立在对服务端的误读上,已废弃)。<see cref="ApplySum"/> 现在只是存档展示。
    ///
    /// **分页续拉改为 config 驱动(对标老端 RankModel.ts:128-160 requestRankData)**:老端用
    /// config_ranking 的 rank_max 预排 ceil(rank_max/20) 页,与 wire sum 无关(老端 rank/ 目录零处读 .sum)。
    /// 本端同理:<see cref="RankTypeData.ConfiguredMax"/> 在 BeginQuery 时锁定为 RankConfigs.GetByType(type)
    /// .RankMax,RankController.On22101 续拉条件为 received&lt;ConfiguredMax(而非 received&lt;wire sum)。
    ///
    /// **分页节流**:老端 20 条/帧(oneMax)节流由 Laya 帧循环驱动;本端无同等"帧"概念,且轮1教训禁止用
    /// MonoBehaviour.Update 驱动业务行为,故把"帧"替换成"收到响应即续发下一页"(纯粹响应驱动,续拉逻辑
    /// 落在 RankController.On22101 内,可被 CliVerify 反射直接喂包驱动断言,无需真实等待)。
    ///
    /// **占位项入库**:服务端在真实数据不足 Len 条时用全 0(PlayerId=0)占位项凑满整页(util.erl:190)——
    /// 与老端一致,占位项照样 AppendItems 入库(渲染"虚位以待"留 UI 尾包),不做任何"疑似占位就丢弃"的过滤。
    /// </summary>
    public sealed class RankModel
    {
        public static readonly RankModel Instance = new RankModel();
        private RankModel() { }

        /// <summary>老端 oneMax:单次请求最大条数(20 条/帧续拉节流的等价语义)。</summary>
        public const int ONE_MAX = 20;

        // ===== rank_type 枚举(对标老端 RankModel.RANK_TYPE / 服务端 common_rank.hrl RANK_TYPE_*,
        // 14 种个人榜,数值与双端一致;show/排序权威表在 config_ranking,见 RankConfigs) =====
        public const int TYPE_GUILD = 100;        // 结社(公会)榜——22102 已死,本枚举仅供 config_ranking 交叉核对,不发起查询
        public const int TYPE_FIGHT = 200;        // 战力榜(默认 tab,老端 GAME_START selectTab=0)
        public const int TYPE_LEVEL = 300;        // 等级榜
        public const int TYPE_NOBILITY = 400;     // 成就榜(hrl 旧注释"爵位榜",天穹重新包装文案,以 config_ranking 为准)
        public const int TYPE_COMPETITION = 500;  // 竞技榜(问鼎云台,跨服;客户端无感知本服/跨服差异)
        public const int TYPE_HORSE = 601;        // 坐骑榜
        public const int TYPE_FLY_HORSE = 602;    // 飞骑榜
        public const int TYPE_WING = 603;         // 翅膀榜(垂神翼影)
        public const int TYPE_PARTNER = 604;      // 精灵榜(剑魄同修)
        public const int TYPE_SPIRIT = 605;       // 精灵榜(同修2,config_ranking 当前隐藏)
        public const int TYPE_GOD = 606;          // 圣器榜(古法符相)
        public const int TYPE_PYX = 607;          // 神兵榜(殒锋天刃)
        public const int TYPE_EQUIP = 608;        // 装备榜
        public const int TYPE_TOWER = 609;        // 爬塔榜
        public const int TYPE_ONHOOK = 700;       // 挂机收益榜

        /// <summary>22101 单条 rank_list 项(对标 pt_221.erl item_to_bin_0;Figure 复用
        /// <see cref="FigureProto"/>,与本协议专属尾部字段分开存)。</summary>
        public sealed class RankItemVo
        {
            public long PlayerId;
            public int PraiseNum;      // ⚠僵尸字段:22103/22104(膜拜)已死,该值永远是历史遗留值或0
            public FigureProto Figure;
            public long SelCombat;     // 64位(个人榜;22102 公会榜同名字段是32位,不通用)
            public long FirstValue;    // 64位;语义随 rank_type 变(战力=战力值/等级=等级/挂机=每分钟收益…)
            public int SecondValue;    // 32位;当前 UI 侧零消费(老端 RankView/RankItem 都不读),纯冗余 payload
            public int ThirdValue;     // 32位;同上零消费
            public int Rank;
        }

        /// <summary>某 rank_type 的累计查询状态。</summary>
        public sealed class RankTypeData
        {
            public int RoleRank;
            public long SelVal;    // 64位(自身原始值,战力榜等对应 first_value 语义)
            public int SelSecVal;
            /// <summary>wire sum 字段回声(⚠恒等于最近一次请求的 Len,非真实总数——服务端两分支皆如此,
            /// 见类注释),仅存档展示,不参与分页控制流。</summary>
            public int Sum;
            /// <summary>本轮分页总量上界,BeginQuery 时从 RankConfigs.GetByType(type).RankMax 锁定
            /// (对标老端 requestRankData 里的 info.rank_max);查不到配置时兜底单页(=ONE_MAX)终止。</summary>
            public int ConfiguredMax;
            public readonly List<RankItemVo> Items = new List<RankItemVo>();
            /// <summary>是否已判定拉完(达到 ConfiguredMax 或服务端回空提前终止)。</summary>
            public bool Complete;
        }

        private readonly Dictionary<int, RankTypeData> _data = new Dictionary<int, RankTypeData>();

        private RankTypeData GetOrCreate(int rankType)
        {
            if (!_data.TryGetValue(rankType, out RankTypeData d))
            {
                d = new RankTypeData();
                _data[rankType] = d;
            }
            return d;
        }

        /// <summary>新一轮查询起点(start==1 时由 Controller 调用):清空旧页,避免与上一轮榜单数据混叠,
        /// 并锁定本轮 config 驱动分页的总量上界(<paramref name="configuredMax"/>,来自 RankConfigs.RankMax)。</summary>
        public void BeginQuery(int rankType, int configuredMax)
        {
            RankTypeData d = GetOrCreate(rankType);
            d.Items.Clear();
            d.Sum = 0;
            d.ConfiguredMax = configuredMax;
            d.Complete = false;
        }

        public void ApplySelf(int rankType, int roleRank, long selVal, int selSecVal)
        {
            RankTypeData d = GetOrCreate(rankType);
            d.RoleRank = roleRank;
            d.SelVal = selVal;
            d.SelSecVal = selSecVal;
        }

        /// <summary>存档 wire sum 回声(⚠恒等于请求 Len,非真实总数,见类注释)——无条件覆盖,不做
        /// "只增不减"防御(该防御建立在对服务端字段序的误读上,已废弃;分页控制流改用 ConfiguredMax)。</summary>
        public void ApplySum(int rankType, int sum)
        {
            GetOrCreate(rankType).Sum = sum;
        }

        public void AppendItems(int rankType, List<RankItemVo> items)
        {
            if (items == null || items.Count == 0) return;
            GetOrCreate(rankType).Items.AddRange(items);
        }

        public void MarkComplete(int rankType) => GetOrCreate(rankType).Complete = true;

        public int GetItemCount(int rankType) => _data.TryGetValue(rankType, out RankTypeData d) ? d.Items.Count : 0;

        public int GetSum(int rankType) => _data.TryGetValue(rankType, out RankTypeData d) ? d.Sum : 0;

        public RankTypeData GetData(int rankType) => _data.TryGetValue(rankType, out RankTypeData d) ? d : null;

        public void Clear() => _data.Clear();
    }
}
