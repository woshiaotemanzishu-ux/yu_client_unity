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
