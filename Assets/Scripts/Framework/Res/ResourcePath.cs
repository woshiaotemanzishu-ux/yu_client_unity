using System.Text;

namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Normalize resource paths to Addressable Key form.
    /// Rule: lowercase forward slashes, no extension, no CDN/host prefix, no leading slash.
    /// </summary>
    public static class ResourcePath
    {
        /// <summary>
        /// Normalize an arbitrary resource reference into a canonical Addressable key.
        /// </summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            string s = raw.Replace('\\', '/').Trim();

            // strip scheme + host
            int schemeIdx = s.IndexOf("://");
            if (schemeIdx >= 0)
            {
                int firstSlash = s.IndexOf('/', schemeIdx + 3);
                s = firstSlash >= 0 ? s.Substring(firstSlash + 1) : string.Empty;
            }

            // strip leading slashes
            int start = 0;
            while (start < s.Length && s[start] == '/') start++;
            if (start > 0) s = s.Substring(start);

            // strip Assets/GameRes/ prefix if any
            const string assetsPrefix = "assets/";
            if (s.Length > assetsPrefix.Length && s.Substring(0, assetsPrefix.Length).ToLowerInvariant() == assetsPrefix)
            {
                s = s.Substring(assetsPrefix.Length);
            }
            const string gameResPrefix = "gameres/";
            if (s.Length > gameResPrefix.Length && s.Substring(0, gameResPrefix.Length).ToLowerInvariant() == gameResPrefix)
            {
                s = s.Substring(gameResPrefix.Length);
            }

            // strip extension
            int lastSlash = s.LastIndexOf('/');
            int lastDot = s.LastIndexOf('.');
            if (lastDot > lastSlash)
            {
                s = s.Substring(0, lastDot);
            }

            return s.ToLowerInvariant();
        }
    }
}
