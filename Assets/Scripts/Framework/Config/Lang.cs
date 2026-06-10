using System.Collections.Generic;
using Shenxiao.Framework.Util;

namespace Shenxiao.Framework.Config
{
    /// <summary>
    /// Localized text lookup. Currently single-language (zh-CN), backed by a config table.
    /// </summary>
    public static class Lang
    {
        private static Dictionary<string, string> _texts = new Dictionary<string, string>();

        /// <summary>
        /// Replace the in-memory text table. Called once after the language config is loaded.
        /// </summary>
        public static void SetTable(Dictionary<string, string> table)
        {
            _texts = table ?? new Dictionary<string, string>();
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_texts.TryGetValue(key, out var v)) return v;
            GameLog.Warn("Lang", "missing key={0}", key);
            return key;
        }

        public static string Format(string key, params object[] args)
        {
            string fmt = Get(key);
            return (args == null || args.Length == 0) ? fmt : string.Format(fmt, args);
        }
    }
}
