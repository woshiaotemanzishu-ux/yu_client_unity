using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 开服/合服时间(对标老客户端 serverTime/ServerTimeModel)。
    /// 数据来自 10201:open_time(开服 0 点,秒)、merge_time/merge_start_time(合服,秒)、merge_count;
    /// 10000 只补写 open_time(裁决5,老端 LoginController.ts:275-276)。
    /// 轮20:补齐 DAY_CHANGE/HOUR_REFRESH 驱动(<see cref="TryFireEvent"/>,严格镜像
    /// yu_client\h5\src\serverTime\ServerTimeModel.ts:47-65,唯一调用者是 GameStartController.On10201,
    /// 对标老端 InitServerTime 的唯一调用点 ServerTimeModel.ts:39)。不加本地 ticker(裁决3)。
    /// </summary>
    public static class ServerTimeModel
    {
        /// <summary>开服 0 点时间(unix 秒,10201/10000 的 open_time)。</summary>
        public static long OpenTime { get; private set; }

        /// <summary>合服时间(unix 秒,10201 的 merge_time)。对标老端 ServerTimeModel.ts:12——老端存了即弃
        /// (全仓无消费方),本端同样只存不建业务读取口。</summary>
        public static long MergeTime { get; private set; }

        /// <summary>合服开始时间(unix 秒,10201 的 merge_start_time)。</summary>
        public static long MergeStartTime { get; private set; }

        /// <summary>10201 的 merge_count。⚠老端字段注释写"登录天数",但 GetLoginDay()
        /// (ServerTimeModel.ts:86-88)返回的其实是另一个全仓零赋值字段 login_day(ServerTimeModel.ts:15)——
        /// merge_count 与"登录天数"文本命名对不上,是老端自身的死数据/命名误导。本端如实落此字段,
        /// 但不建 GetLoginDay 这个死接口(spec_serverclock_round20.md §2.4 裁决存档)。</summary>
        public static int MergeCount { get; private set; }

        /// <summary>HOUR_REFRESH 命中的整点表(对标老端 refresh_hour_list,ServerTimeModel.ts:10)。</summary>
        public static readonly int[] RefreshHourList = { 4 };

        private static int _lastDay;
        private static int _lastHour;
        private static bool _hasLastHour;

        /// <summary>
        /// 落地入口(对标老端 InitServerTime 的字段赋值部分,ServerTimeModel.ts:34-37)。
        /// 唯一写入口(不留 SetServerTime/ApplyServerInfo 双写入口):GameStartController.On10201 传全 5 字段;
        /// LoginController.OnAccountLogin(10000)只想改 open_time 时,把另外三个参数原样传回当前值即可
        /// (裁决5:10000 不建第二个局部 setter,也不调 <see cref="TryFireEvent"/>)。
        /// </summary>
        public static void ApplyServerInfo(long openTimeSec, long mergeTimeSec, long mergeStartTimeSec, int mergeCount)
        {
            OpenTime = openTimeSec;
            MergeTime = mergeTimeSec;
            MergeStartTime = mergeStartTimeSec;
            MergeCount = mergeCount;
        }

        /// <summary>运行时链路零调用者(对标老端 ResetData 同样零调用者,ServerTimeModel.ts:43-46),
        /// 连带把跨天/整点状态(_lastDay/_lastHour/_hasLastHour)一并清掉保持自洽(裁决4)。
        /// 注:仅 CliVerify 的 ServerClockCase 在用例首尾各调一次做隔离,不构成运行时调用方。</summary>
        public static void Reset()
        {
            OpenTime = 0;
            MergeTime = 0;
            MergeStartTime = 0;
            MergeCount = 0;
            _lastDay = 0;
            _lastHour = 0;
            _hasLastHour = false;
        }

        /// <summary>对标 ServerTimeModel.GetOpenServerDay: ceil((now - open_time)/86400),不足一天算一天。</summary>
        public static int GetOpenServerDay()
        {
            if (OpenTime <= 0) return 0;
            long gap = TimeUtil.NowSec() - OpenTime;
            if (gap < 0) gap = 0;
            return (int)System.Math.Ceiling(gap / 86400.0);
        }

        /// <summary>对标 GetMergeServerDay(ServerTimeModel.ts:77-84):merge_start_time==0 时特判 gap=0
        /// (未合服),否则 ceil((now - merge_start_time)/86400)。</summary>
        public static int GetMergeServerDay()
        {
            long gap = MergeStartTime == 0 ? 0 : TimeUtil.NowSec() - MergeStartTime;
            return (int)System.Math.Ceiling(gap / 86400.0);
        }

        /// <summary>
        /// 跨天/整点事件源(第20轮)。老端全仓唯一调用者是 InitServerTime(ServerTimeModel.ts:39),
        /// 即每次 10201 落地后调用一次——本端同样只在 GameStartController.On10201 调用,不加本地 ticker
        /// (裁决3,断线重连会重发 10201,lastDay/lastHour 跨重连保留即可正确判跨天)。
        /// 严格镜像 ServerTimeModel.ts:47-65,两处故意偏离已逐行注释:
        ///  · 裁决1(订正,不复刻):老端 `if (this.lastHour && ...)`(ServerTimeModel.ts:59)用 truthy 判断
        ///    "是否已有上一次整点"——0 点时 lastHour 被置为 0,falsy,导致 4 点判定整条 if 恒 false,
        ///    跨夜在线玩家 4 点刷新永不触发。本端改用显式 <see cref="_hasLastHour"/> 布尔代替 truthy 判定,
        ///    订正此 bug。
        ///  · 裁决2(故意不同,不算订正):老端 `now_hour = new Date(getServerTime()*1000).getHours()`
        ///    (ServerTimeModel.ts:53-55)取的是浏览器本地时区;本端改用服务器时区
        ///    <see cref="TimeUtil.NowServerLocal"/>(服务端 04:00:01 推 10201 用的是服务端机器时区,见
        ///    yu_server\src\timer\timer_4_clock.erl:207)。国内玩家两端数值恰好等价。
        /// </summary>
        public static void TryFireEvent()
        {
            int curDay = GetOpenServerDay();
            if (_lastDay > 0 && _lastDay != curDay) // _lastDay>0 镜像老端 truthy(ServerTimeModel.ts:49)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_SERVER_DAY_CHANGE);
            }
            _lastDay = curDay;

            int nowHour = TimeUtil.NowServerLocal().Hour; // 裁决2:服务器时区,非浏览器本地时区
            if (_hasLastHour && _lastHour == nowHour) return; // 镜像老端 ServerTimeModel.ts:56-57

            foreach (int hour in RefreshHourList)
            {
                if (_hasLastHour && _lastHour != hour && nowHour == hour) // 订正:_hasLastHour 取代老端 truthy(裁决1)
                {
                    EventDispatcher.Emit(GlobalEvent.EVT_SERVER_HOUR_REFRESH, hour);
                    break;
                }
            }
            _hasLastHour = true;
            _lastHour = nowHour;
        }
    }
}
