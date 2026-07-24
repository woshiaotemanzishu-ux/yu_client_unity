using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Daily;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.ActivityForeshow
{
    /// <summary>
    /// 活动预告/日历数据(对标老客户端 commonModel/ActivityForeshowManager)。承载预告图标显隐、
    /// 日历时间窗和倒计时所需的最小状态，不做完整活动日历面板/提示弹窗。
    ///
    /// 老端预告管理器的图标由两个条件驱动:①客户端 XianShiActivity 日历时间窗(time_region,判定
    /// UnOpen/Show/CountDown/InProgress/Finish);②各活动自己的开启态检查(CheckActivityOpenState)。
    /// 配置型限时活动统一读取 Unity 已同步的 config_ac 日历字段；无 config_ac 行的入口自然保持隐藏。
    /// 玩法另有服务端开关/截止时间时，由对应协议更新状态，再通过同一 ActivityIconManager 模板显示。
    /// </summary>
    public sealed class ActivityForeshowModel
    {
        public static readonly ActivityForeshowModel Instance = new ActivityForeshowModel();
        private ActivityForeshowModel() { }

        /// <summary>领地夺宝 预告图标(对标老端 FORE_TYPE.SNATCHTREASURE)。</summary>
        public const string ICON_SNATCH_TREASURE = "652@31@0";

        public readonly struct ScheduleDisplay
        {
            public readonly bool Visible;
            public readonly long EndTime;
            public readonly string Text;

            public ScheduleDisplay(bool visible, long endTime = 0, string text = "")
            {
                Visible = visible;
                EndTime = endTime;
                Text = text ?? "";
            }
        }

        /// <summary>
        /// 用 config_ac 的等级/开服日/星期/time_region 计算预告图标。对标老端 ActivityForeshowManager:
        /// 开始前 60 分钟出现，未开始时显示“HH:mm开启”，开始后以本场结束时间倒计时，结束即撤下。
        /// 这里只消费活动日历配置，不读取玩法快照；玩法开关与 UI 预告保持数据边界独立。
        /// </summary>
        public ScheduleDisplay EvaluateSchedule(string iconType)
        {
            if (!TryParseActivityKey(iconType, out int module, out int moduleSub, out int acSub))
                return default;

            JObject cfg = DailyConfigs.GetAc(module, moduleSub, acSub);
            if (cfg == null) return default;

            int level = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;
            if (level < DailyConfigs.ReadInt(cfg, "start_lv") || level > DailyConfigs.ReadInt(cfg, "end_lv"))
                return default;

            DateTime now = TimeUtil.NowServerLocal();
            int nowOfDay = now.Hour * 3600 + now.Minute * 60 + now.Second;
            List<(int startH, int startM, int endH, int endM)> regions =
                DailyConfigs.ParseTimeRegion(DailyConfigs.ReadString(cfg, "time_region"));
            foreach ((int startH, int startM, int endH, int endM) region in regions)
            {
                int start = region.startH * 3600 + region.startM * 60;
                int end = region.endH * 3600 + region.endM * 60;
                bool crossesMidnight = end <= start;
                int compareNow = nowOfDay;
                DateTime scheduleDay = now;
                DateTime startDate = now.Date;
                if (crossesMidnight && compareNow < end)
                {
                    compareNow += 86400;
                    scheduleDay = now.AddDays(-1);
                    startDate = now.Date.AddDays(-1);
                }
                if (!IsConfiguredDay(cfg, scheduleDay)) continue;

                int compareEnd = crossesMidnight ? end + 86400 : end;

                if (compareNow >= compareEnd) continue;
                if (compareNow < start - 3600) continue;

                // NowServerLocal() 的墙钟值来自 UTC 时间加 8 小时，但 DateTime.Kind 仍会保留 Utc。
                // DateTimeOffset 禁止给 Kind=Utc 的 DateTime 再指定 +08:00，必须先明确它是“无时区的
                // 服务器本地墙钟”，否则任一进入显示窗口的活动都会抛异常并中断统一预告扫描。
                DateTime endLocal = DateTime.SpecifyKind(
                    startDate.AddSeconds(compareEnd), DateTimeKind.Unspecified);
                long endTime = new DateTimeOffset(endLocal, TimeSpan.FromHours(TimeUtil.SERVER_ZONE_HOURS))
                    .ToUnixTimeSeconds();
                if (compareNow < start)
                    return new ScheduleDisplay(true, endTime, string.Format("{0}:{1:00}开启", region.startH, region.startM));
                return new ScheduleDisplay(true, endTime);
            }
            return default;
        }

        /// <summary>该图标是否有可由通用预告系统接管的 config_ac 时段配置。</summary>
        public bool HasSchedule(string iconType)
        {
            if (!TryParseActivityKey(iconType, out int module, out int moduleSub, out int acSub)) return false;
            JObject cfg = DailyConfigs.GetAc(module, moduleSub, acSub);
            return cfg != null
                   && DailyConfigs.ParseTimeRegion(DailyConfigs.ReadString(cfg, "time_region")).Count > 0;
        }

        private static bool TryParseActivityKey(string iconType, out int module, out int moduleSub, out int acSub)
        {
            module = moduleSub = acSub = 0;
            string[] parts = (iconType ?? "").Split('@');
            return parts.Length == 3
                   && int.TryParse(parts[0], out module)
                   && int.TryParse(parts[1], out moduleSub)
                   && int.TryParse(parts[2], out acSub);
        }

        private static bool IsConfiguredDay(JObject cfg, DateTime day)
        {
            int openDay = ServerTimeModel.GetOpenServerDay();
            int mergeDay = ServerTimeModel.GetMergeServerDay();
            List<(int start, int end)> openRanges =
                DailyConfigs.ParseDayRanges(DailyConfigs.ReadString(cfg, "open_day"));
            List<(int start, int end)> mergeRanges =
                DailyConfigs.ParseDayRanges(DailyConfigs.ReadString(cfg, "merge_day"));
            List<(int start, int end)> ranges = mergeDay > 0 && mergeRanges.Count > 0 ? mergeRanges : openRanges;
            int serverDay = mergeDay > 0 && mergeRanges.Count > 0 ? mergeDay : openDay;
            if (ranges.Count > 0)
            {
                bool inRange = false;
                foreach ((int start, int end) range in ranges)
                {
                    if (serverDay >= range.start && serverDay <= range.end) { inRange = true; break; }
                }
                if (!inRange) return false;
            }

            List<int> weeks = DailyConfigs.ParseIntArray(DailyConfigs.ReadString(cfg, "week"));
            int weekday = (int)day.DayOfWeek;
            if (weekday == 0) weekday = 7;
            if (weeks.Count > 0 && !weeks.Contains(weekday)) return false;

            List<int> months = DailyConfigs.ParseIntArray(DailyConfigs.ReadString(cfg, "month"));
            if (months.Count > 0 && !months.Contains(day.Month - 1)) return false;

            List<(int year, int month0, int date)> dates =
                DailyConfigs.ParseDateList(DailyConfigs.ReadString(cfg, "time"));
            if (dates.Count > 0)
            {
                bool matches = false;
                foreach ((int year, int month0, int date) value in dates)
                {
                    if (value.year == day.Year && value.month0 == day.Month - 1 && value.date == day.Day)
                    {
                        matches = true;
                        break;
                    }
                }
                if (!matches) return false;
            }
            return true;
        }

        // ---- 领地夺宝 65208 时间信息(对标老端 SnatchTreasureModel.timeMsgData = {dunid, end_time}) ----
        public bool HasSnatchInfo;   // 是否已收到 65208
        public int SnatchDunId;      // 当前领地夺宝副本 id(dun_id)
        public long SnatchEndTime;   // 本轮结束时间戳(unix 秒),0 表示无有效会话

        public void SetSnatchTimeMsg(int dunId, long endTime)
        {
            HasSnatchInfo = true;
            SnatchDunId = dunId;
            SnatchEndTime = endTime;
        }

        /// <summary>
        /// 领地夺宝预告图标是否应显示。对标老端 SnatchTreasureModel.checkOpen():
        ///   let flag = true; if (timeMsgData && end_time != 0 && serverTime >= end_time) flag = false; return flag;
        /// 老端 checkOpen 默认 true(含 end_time==0),因真正的显隐主闸是 XianShiActivity 日历时间窗(本期未移植)。
        /// 脱离日历后以 65208 的 end_time 作为唯一驱动:仅当存在未来 end_time(领地夺宝会话进行中)才显示;
        /// end_time==0 或已过期均不显示,避免无时间窗时常驻误显。
        /// </summary>
        public bool GetSnatchOpenState()
        {
            return HasSnatchInfo && SnatchEndTime != 0 && TimeUtil.NowSec() < SnatchEndTime;
        }

        public void Reset()
        {
            HasSnatchInfo = false;
            SnatchDunId = 0;
            SnatchEndTime = 0;
        }
    }
}
