using System;
using System.Text;

namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Normalize resource paths to Addressable Key form.
    /// Rule: lowercase forward slashes, no extension, no CDN/host prefix, no leading slash.
    /// </summary>
    public static class ResourcePath
    {
        public const string RoleSkillCareerArtAssetPath =
            "Assets/GameRes/resource/game/role/other/uijn_001.png";
        public const string RoleSkillCareerArtAddress =
            "resource/game/role/other/uijn_001_character";

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

        /// <summary>
        /// Extension-stripped Addressables can collide when sibling files share a basename.
        /// Keep the exceptional asset-path-to-address mapping here so the build pipeline and
        /// editor fallback resolve the exact same key.
        /// </summary>
        public static string ApplyAssetAddressAlias(string assetPath, string defaultAddress)
        {
            string normalizedPath = (assetPath ?? string.Empty).Replace('\\', '/');
            return string.Equals(normalizedPath, RoleSkillCareerArtAssetPath,
                StringComparison.OrdinalIgnoreCase)
                ? RoleSkillCareerArtAddress
                : defaultAddress;
        }

        public static bool TryGetAliasedAssetPath(string address, out string assetPath)
        {
            if (string.Equals(Normalize(address), RoleSkillCareerArtAddress,
                    StringComparison.Ordinal))
            {
                assetPath = RoleSkillCareerArtAssetPath;
                return true;
            }

            assetPath = null;
            return false;
        }
    }
}
