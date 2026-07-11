using System.Collections.Generic;

namespace Shenxiao.Module.Core.Compete
{
    /// <summary>
    /// 竞榜(赛事活动)数据(对标老客户端 commonModel/CompeteListModel)。承载 33800 下发的
    /// "正在开启的赛事活动列表"(玄鸢千寻/圣殿狮鹫/急速飞车/背饰… 100+ 种,由同一份列表驱动),
    /// 供主界面图标(338@type@subtype 家族)显隐用。本期只做图标,面板/榜单/抽奖/积分等
    /// (33801-33807)待用户验收,不移植。
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
        private CompeteModel() { }

        /// <summary>竞榜图标家族号(对标老端 CompeteListModel.IsBillboardAct 判定 "338")。</summary>
        public const string ICON_FAMILY = "338";

        /// <summary>33800 单条赛事活动(对标老端 item_to_bin_0 / SCMD act_list 元素)。</summary>
        public struct RaceActInfo
        {
            public int Type;        // 活动大类
            public int Subtype;     // 活动子类
            public int ShowId;      // 展示id
            public int StartTime;   // 开始时间戳
            public int EndTime;     // 结束时间戳
            public int BuyEndTime;  // 购买/参与结束时间戳(图标倒计时用,对标老端 addIcon 传的 buy_end_time)
        }

        private readonly List<RaceActInfo> _actList = new List<RaceActInfo>();

        public IReadOnlyList<RaceActInfo> ActList => _actList;

        public void SetActList(List<RaceActInfo> list)
        {
            _actList.Clear();
            if (list != null) _actList.AddRange(list);
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
        }
    }
}
