using UnityEngine;

namespace Shenxiao.Framework.Util
{
    /// <summary>
    /// Unified logger. Use module tag as first arg.
    /// </summary>
    public static class GameLog
    {
        public enum Level { Debug, Info, Warn, Error }

        public static Level MinLevel = Level.Debug;

        // Info/Debug 级关闭栈采集:编辑器里每条 Debug.Log 默认抓完整托管栈并写 Editor.log/刷 Console,
        // 战斗期 GameLog.Info 每秒几十条(实录单场会话 12 万行),栈采集是主线程肉眼可见的顿挫来源之一。
        // 定位 Info 靠 [模块] 标签即可;Warning/Error 保留完整栈不受影响。SubsystemRegistration 时机最早,
        // 进 Play 即生效;幂等,可被外部随时改回。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DisableInfoStackTrace()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }

        public static void Debug(string tag, string format, params object[] args)
        {
            if (MinLevel > Level.Debug) return;
            UnityEngine.Debug.Log(Format(tag, format, args));
        }

        public static void Info(string tag, string format, params object[] args)
        {
            if (MinLevel > Level.Info) return;
            UnityEngine.Debug.Log(Format(tag, format, args));
        }

        public static void Warn(string tag, string format, params object[] args)
        {
            if (MinLevel > Level.Warn) return;
            UnityEngine.Debug.LogWarning(Format(tag, format, args));
        }

        public static void Error(string tag, string format, params object[] args)
        {
            if (MinLevel > Level.Error) return;
            UnityEngine.Debug.LogError(Format(tag, format, args));
        }

        private static string Format(string tag, string format, object[] args)
        {
            string body = (args == null || args.Length == 0) ? format : string.Format(format, args);
            return "[" + tag + "] " + body;
        }
    }
}
