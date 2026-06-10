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
