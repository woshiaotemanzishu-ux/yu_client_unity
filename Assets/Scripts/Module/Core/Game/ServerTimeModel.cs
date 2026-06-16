using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 开服/合服时间(对标老客户端 serverTime/ServerTimeModel)。
    /// 数据来自 10201:open_time(开服 0 点)、merge_start_time(合服)。
    /// 当前只承载功能开放判定要用的开服天数,后续活动图标/周期榜等再按需扩。
    /// </summary>
    public static class ServerTimeModel
    {
        /// <summary>开服 0 点时间(unix 秒,10201 的 open_time)。</summary>
        public static long OpenTime { get; private set; }

        /// <summary>合服开始时间(unix 秒,10201 的 merge_start_time)。</summary>
        public static long MergeStartTime { get; private set; }

        public static void SetServerTime(long openTimeSec, long mergeStartTimeSec)
        {
            OpenTime = openTimeSec;
            MergeStartTime = mergeStartTimeSec;
        }

        public static void Reset()
        {
            OpenTime = 0;
            MergeStartTime = 0;
        }

        /// <summary>对标 ServerTimeModel.GetOpenServerDay: ceil((now - open_time)/86400),不足一天算一天。</summary>
        public static int GetOpenServerDay()
        {
            if (OpenTime <= 0) return 0;
            long gap = TimeUtil.NowSec() - OpenTime;
            if (gap < 0) gap = 0;
            return (int)System.Math.Ceiling(gap / 86400.0);
        }
    }
}
