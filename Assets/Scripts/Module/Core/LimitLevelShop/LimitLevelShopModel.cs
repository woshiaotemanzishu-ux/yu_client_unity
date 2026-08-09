using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    /// <summary>
    /// 限时等级抢购数据(对标老客户端 LimitLevelShopModel,模块 612)。承载 61200 下发的
    /// 抢购礼包列表及页面所需状态，并缓存 61203 的档位配置。服务端只下发"在开"的礼包,
    /// 故列表非空即视为有抢购活动在开(GetEntranceOpenState),对应主图标 61201 显示。
    ///
    /// 说明:老端每个礼包的真实图标类型来自 act_condition(erlang 串)里的 pic 字段(变体范围
    /// 61201..61225),经 ErlangParser 解析后 addIcon(cond.pic)。Unity 同样保存该映射并逐礼包只读请求 61203。
    /// 61201 购买链仍按 hard-negative 保持未注册、未发送。
    /// </summary>
    public sealed class LimitLevelShopModel
    {
        public static readonly LimitLevelShopModel Instance = new LimitLevelShopModel();
        private LimitLevelShopModel() { }

        public sealed class GiftConfigEntry
        {
            public ushort Grade { get; } public string NormalCost { get; } public string Cost { get; } public string Show { get; }
            public string PageString { get; } public string Reward { get; } public string Condition { get; }
            public ushort RechargeId { get; } public ushort Discount { get; }
            public GiftConfigEntry(ushort grade, string normalCost, string cost, string show, string pageString, string reward, string condition, ushort rechargeId, ushort discount)
            { Grade = grade; NormalCost = normalCost; Cost = cost; Show = show; PageString = pageString; Reward = reward; Condition = condition; RechargeId = rechargeId; Discount = discount; }
        }
        public sealed class GiftConfigSnapshot
        {
            public ushort Type { get; } public ushort Subtype { get; } public bool Loaded { get; }
            public IReadOnlyList<GiftConfigEntry> Entries { get; }
            public GiftConfigSnapshot(ushort type, ushort subtype, IEnumerable<GiftConfigEntry> entries)
            { Type = type; Subtype = subtype; Loaded = true; Entries = new List<GiftConfigEntry>(entries ?? new GiftConfigEntry[0]).AsReadOnly(); }
        }
        private readonly Dictionary<(ushort type, ushort subtype), GiftConfigSnapshot> _giftConfigs = new Dictionary<(ushort, ushort), GiftConfigSnapshot>();
        public event Action DataChanged;
        public bool TryGetGiftConfig(ushort type, ushort subtype, out GiftConfigSnapshot snapshot) => _giftConfigs.TryGetValue((type, subtype), out snapshot);
        public void ApplyGiftConfig(ushort type, ushort subtype, List<GiftConfigEntry> entries)
        {
            _giftConfigs[(type, subtype)] = new GiftConfigSnapshot(type, subtype, entries);
            DataChanged?.Invoke();
        }
        public void ClearGiftConfigs() => _giftConfigs.Clear();

        /// <summary>
        /// 主界面图标类型(主图标,对标老端 Type2Icon 首项 "61201")。
        /// 注意:这是"图标配置 id",与网络协议 61201(购买回包)是不同 id 空间,数值巧合勿混。
        /// </summary>
        public const string ICON_TYPE = "61201";

        public readonly struct GradeState
        {
            public readonly ushort Grade;
            public readonly byte State;
            public GradeState(ushort grade, byte state) { Grade = grade; State = state; }
        }

        /// <summary>61200 下发的单个在开礼包；保留页面只读展示所需的完整状态。</summary>
        public sealed class GiftEntry
        {
            public ushort Type { get; }
            public ushort Subtype { get; }
            public long EndTime { get; }
            public IReadOnlyList<GradeState> GradeStates { get; }
            public IReadOnlyList<GradeState> OldGradeStates { get; }
            public string ActCondition { get; }
            public ushort OpenTimes { get; }
            public string IconType { get; }

            public GiftEntry(ushort type, ushort subtype, long endTime,
                IEnumerable<GradeState> gradeStates, IEnumerable<GradeState> oldGradeStates,
                string actCondition, ushort openTimes, string iconType)
            {
                Type = type;
                Subtype = subtype;
                EndTime = endTime;
                GradeStates = new List<GradeState>(gradeStates ?? Array.Empty<GradeState>()).AsReadOnly();
                OldGradeStates = new List<GradeState>(oldGradeStates ?? Array.Empty<GradeState>()).AsReadOnly();
                ActCondition = actCondition ?? "";
                OpenTimes = openTimes;
                IconType = string.IsNullOrEmpty(iconType) ? ICON_TYPE : iconType;
            }

            public byte GetState(ushort grade, bool old = false)
            {
                IReadOnlyList<GradeState> list = old ? OldGradeStates : GradeStates;
                for (int i = 0; i < list.Count; i++) if (list[i].Grade == grade) return list[i].State;
                return 0;
            }
        }

        // 当前服务端下发的在开礼包列表(服务端只发在开的,故"存在即在开")。
        private readonly List<GiftEntry> _gifts = new List<GiftEntry>();

        public IReadOnlyList<GiftEntry> Gifts => _gifts;

        public void SetGiftList(List<GiftEntry> gifts)
        {
            _gifts.Clear();
            if (gifts != null) _gifts.AddRange(gifts);
            DataChanged?.Invoke();
        }

        public GiftEntry FindByIcon(string iconType)
        {
            for (int i = 0; i < _gifts.Count; i++)
                if (string.Equals(_gifts[i].IconType, iconType, StringComparison.Ordinal)) return _gifts[i];
            return null;
        }

        /// <summary>
        /// 入口开启状态(对标老端 RefreshState:列表里有 vo 即 addIcon(vo 的 pic))。
        /// 服务端仅下发满足等级/时间窗的在开礼包,客户端只看列表是否非空。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return _gifts.Count > 0;
        }

        public void Reset()
        {
            ClearGiftConfigs();
            _gifts.Clear();
            DataChanged?.Invoke();
        }
    }
}
