using System;
using System.Globalization;

namespace Shenxiao.Framework.Util
{
    /// <summary>
    /// Misc helpers. Keep this small; group into dedicated files when functionality grows.
    /// </summary>
    public static class Util
    {
        public static int ParseInt(string s, int fallback = 0)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
        }

        public static long ParseLong(string s, long fallback = 0)
        {
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : fallback;
        }

        public static float ParseFloat(string s, float fallback = 0f)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        public static string FormatInvariant(string fmt, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, fmt, args);

        public static string ToHex(byte[] bytes)
        {
            if (bytes == null) return string.Empty;
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
    }
}
