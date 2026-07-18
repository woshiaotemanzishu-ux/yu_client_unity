using System;

namespace Shenxiao.Framework.Util
{
    /// <summary>
    /// Time helpers. Server time is pushed by login flow; subsequent calls add elapsed real time.
    /// </summary>
    public static class TimeUtil
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long _serverEpochMs;
        private static float _localBaseTime;

        /// <summary>
        /// Sync server time. epochMs is unix time in milliseconds.
        /// </summary>
        public static void SyncServerTime(long epochMs)
        {
            _serverEpochMs = epochMs;
            _localBaseTime = UnityEngine.Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Current server unix time in milliseconds.
        /// </summary>
        public static long NowMs()
        {
            if (_serverEpochMs <= 0) return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float elapsed = UnityEngine.Time.realtimeSinceStartup - _localBaseTime;
            return _serverEpochMs + (long)(elapsed * 1000f);
        }

        public static long NowSec() => NowMs() / 1000L;

        public static DateTime NowUtc() => _epoch.AddMilliseconds(NowMs());

        /// <summary>
        /// 服务器墙钟时区(UTC+8),唯一事实源。BossModel/DailyModel/ShopModel 既有同名
        /// SERVER_ZONE_HOURS 常量(轮20起)均转发这里,值不变、零行为变更。
        /// </summary>
        public const int SERVER_ZONE_HOURS = 8;

        /// <summary>
        /// 服务器墙钟本地时间(裁决2,spec_serverclock_round20.md 裁决表#2)。用于"4点刷新"等按服务器时区
        /// 判定的整点事件(ServerTimeModel.TryFireEvent)。老端等价物是
        /// new Date(getServerTime()*1000).getHours()——取的是浏览器本地时区(yu_client\h5\src\util\TimeUtil.ts:14-15,
        /// ServerTimeModel.ts:53-55),本端故意改用服务器时区:服务端 04:00:01 推 10201 用的是服务端机器时区
        /// (yu_server\src\timer\timer_4_clock.erl:207 → utime:unixdate → calendar:now_to_local_time)。
        /// 国内玩家两端数值恰好等价,差异已在 ServerTimeModel.TryFireEvent 注释存档。
        /// </summary>
        public static DateTime NowServerLocal() => NowUtc().AddHours(SERVER_ZONE_HOURS);

        /// <summary>
        /// 跨平台延迟(替代 Task.Delay)。⚠ WebGL 上 Task.Delay 永不完成(依赖 System.Threading.Timer,
        /// WebGL 无线程),曾造成:自动战斗环一拍即死、combo 副技能(真实伤害包)永不补发、任务轮询停摆。
        /// WebGL 走 Unity 6 Awaitable.WaitForSecondsAsync(主线程帧驱动);其他平台保持 Task.Delay。
        /// 只能在主线程 async 链中使用(本项目所有游戏逻辑均满足)。
        /// </summary>
        public static async System.Threading.Tasks.Task Delay(int ms, System.Threading.CancellationToken token = default)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            await UnityEngine.Awaitable.WaitForSecondsAsync(ms / 1000f, token);
#else
            await System.Threading.Tasks.Task.Delay(ms, token);
#endif
        }
    }
}
