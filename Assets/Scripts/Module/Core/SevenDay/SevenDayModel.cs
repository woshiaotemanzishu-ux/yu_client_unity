using System.Collections.Generic;

namespace Shenxiao.Module.Core.SevenDay
{
    /// <summary>
    /// 七天/合服七天登录数据(对标老客户端 SevenDayModel)。只承载 17500/17502 下发的奖励领取态,
    /// 供主界面三个图标(175 / 175@8 / 175_1)显隐判定用。
    /// 老端 icon 映射:openType "0@0"→175、"0@1"→175@8、"1@0"→175_1(iconToType 反向)。
    /// openType(act_type)=该 act_type 下「仍有未领奖励(status&lt;2)的最小 day 分组」= act_type@day_type;
    /// 全部领完则无(-1)。某图标显示当且仅当其对应 openType 命中(对标老端 frist_open_view)。
    /// 面板/领奖(17501/17503)等 UI 待用户验收,本期只做图标。
    /// </summary>
    public sealed class SevenDayModel
    {
        public static readonly SevenDayModel Instance = new SevenDayModel();
        private SevenDayModel() { }

        /// <summary>每个 act_type 一档 8 天(对标老端 limit_day=8):day_type = day_id / 8。</summary>
        public const int LIMIT_DAY = 8;

        // act_type(对标老端 SevenDayModel.act_type)
        public const int ACT_OPEN = 0;   // 七天登录(普通/8天版共用 act_type 0,靠 day_type 分 175 与 175@8)
        public const int ACT_MERGE = 1;  // 合服七天

        // 主界面图标类型(对标老端 SevenDayModel.icon)
        public const string ICON_OPEN = "175";     // openType "0@0"
        public const string ICON_EIGHT = "175@8";  // openType "0@1"
        public const string ICON_MERGE = "175_1";  // openType "1@0"

        // 每个 act_type 的当前进度天(scmd.current_day,仅 175@8 末日"明天可领"提示用)
        private readonly int[] _currentDay = { 0, 0 };
        // 每个 act_type 的 openType 的 day 分组(仍有未领奖励的最小 day_type;全领完 = -1)
        private readonly int[] _openDayType = { -1, -1 };

        /// <summary>
        /// 写入某 act_type 的信息(对标老端 SetSevenDayInfo + RefCurDayInfo):
        /// 据 reward_status 计算 openType 的 day 分组。dayIds/statuses 一一对应。
        /// </summary>
        public void SetInfo(int actType, int currentDay, IList<int> dayIds, IList<int> statuses)
        {
            if (actType < 0 || actType > 1) return;
            _currentDay[actType] = currentDay;
            _openDayType[actType] = ComputeOpenDayType(dayIds, statuses);
        }

        // 取「仍有未领奖励(status<2)的最小 day 分组」(day_id/LIMIT_DAY);全部领完返回 -1。
        // 对标老端 RefCurDayInfo:按 day_type 升序找到第一个含 status<2 的分组。
        private static int ComputeOpenDayType(IList<int> dayIds, IList<int> statuses)
        {
            int best = int.MaxValue;
            int n = dayIds.Count < statuses.Count ? dayIds.Count : statuses.Count;
            for (int i = 0; i < n; i++)
            {
                if (statuses[i] >= 2) continue;
                int dayType = dayIds[i] / LIMIT_DAY;
                if (dayType < best) best = dayType;
            }
            return best == int.MaxValue ? -1 : best;
        }

        /// <summary>
        /// 图标是否应显示(对标老端 frist_open_view:model.GetOpenType(act)==该 icon 对应 key)。
        /// 门槛(open_lv/open_day)由 ActivityIconManager 配置闸把控,这里只判"有没有可领档"。
        /// </summary>
        public bool IsIconOpen(string iconType)
        {
            switch (iconType)
            {
                case ICON_OPEN: return _openDayType[ACT_OPEN] == 0;   // "0@0"
                case ICON_EIGHT: return _openDayType[ACT_OPEN] == 1;  // "0@1"
                case ICON_MERGE: return _openDayType[ACT_MERGE] == 0; // "1@0"
                default: return false;
            }
        }

        /// <summary>
        /// 图标角标文字(对标老端:day_type>0 且 current_day==limit_day-1 时 SET_ICON_TEXT "明天可领")。
        /// 只有 175@8(openType "0@1",day_type=1>0)会命中。
        /// </summary>
        public string GetIconText(string iconType)
        {
            if (iconType == ICON_EIGHT && _currentDay[ACT_OPEN] == LIMIT_DAY - 1) return "明天可领";
            return "";
        }

        public void Reset()
        {
            _currentDay[ACT_OPEN] = 0;
            _currentDay[ACT_MERGE] = 0;
            _openDayType[ACT_OPEN] = -1;
            _openDayType[ACT_MERGE] = -1;
        }
    }
}
