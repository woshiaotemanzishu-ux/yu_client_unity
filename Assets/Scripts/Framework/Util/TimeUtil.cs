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
    }
}
