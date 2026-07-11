using System.Collections.Generic;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    /// <summary>
    /// 限时等级抢购数据(对标老客户端 LimitLevelShopModel,模块 612)。只承载 61200 下发的
    /// 抢购礼包列表(type/subtype/end_time),供主界面图标显隐判定用。服务端只下发"在开"的礼包,
    /// 故列表非空即视为有抢购活动在开(GetEntranceOpenState),对应主图标 61201 显示。
    ///
    /// 说明:老端每个礼包的真实图标类型来自 act_condition(erlang 串)里的 pic 字段(变体范围
    /// 61201..61225),经 ErlangParser 解析后 addIcon(cond.pic)。本期图标化只做主图标 61201,
    /// 暂不解析 act_condition;完整"一礼包一图标"的列表化待后续(见 Controller 的 TODO)。
    /// 购买/礼包详情(61201 买结果、61202 活动信息、61203 礼包配置)属玩法,均未移植。
    /// </summary>
    public sealed class LimitLevelShopModel
    {
        public static readonly LimitLevelShopModel Instance = new LimitLevelShopModel();
        private LimitLevelShopModel() { }

        /// <summary>
        /// 主界面图标类型(主图标,对标老端 Type2Icon 首项 "61201")。
        /// 注意:这是"图标配置 id",与网络协议 61201(购买回包)是不同 id 空间,数值巧合勿混。
        /// </summary>
        public const string ICON_TYPE = "61201";

        /// <summary>61200 下发的单个抢购礼包(仅保留图标判定所需字段;end_time 备后续定时用)。</summary>
        public readonly struct GiftEntry
        {
            public readonly int Type;
            public readonly int Subtype;
            public readonly long EndTime;

            public GiftEntry(int type, int subtype, long endTime)
            {
                Type = type;
                Subtype = subtype;
                EndTime = endTime;
            }
        }

        // 当前服务端下发的在开礼包列表(服务端只发在开的,故"存在即在开")。
        private readonly List<GiftEntry> _gifts = new List<GiftEntry>();

        public IReadOnlyList<GiftEntry> Gifts => _gifts;

        public void SetGiftList(List<GiftEntry> gifts)
        {
            _gifts.Clear();
            if (gifts != null) _gifts.AddRange(gifts);
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
            _gifts.Clear();
        }
    }
}
