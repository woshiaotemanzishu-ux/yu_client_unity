using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Util;

namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Resource version API response shape returned by the server.
    /// </summary>
    [Serializable]
    public class ResourceVersionInfo
    {
        public string env;
        public string platform;
        public string resourceVersion;
        public string cdnBaseUrl;
        public string catalogUrl;
    }

    /// <summary>
    /// Applies the resource version handshake. Since the 2026-07 packaging plan the remote catalog
    /// is the player's built-in one (catalog_live.bin/.hash under {ResCdn.BaseUrl}/[BuildTarget]):
    /// Addressables checks the .hash at init and swaps the catalog itself, so this class no longer
    /// loads catalogs or rewrites InternalIds — it only feeds ResCdn before initialization.
    /// </summary>
    public static class ResVersionManager
    {
        public static ResourceVersionInfo Current { get; private set; }

        public static Task ApplyAsync(ResourceVersionInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            Current = info;

            if (!string.IsNullOrEmpty(info.cdnBaseUrl))
            {
                ResCdn.Configure(info.cdnBaseUrl);
                GameLog.Info("Res", "cdn base = {0} (version={1})", ResCdn.BaseUrl, info.resourceVersion);
            }

            if (!string.IsNullOrEmpty(info.catalogUrl))
            {
                // 附加 catalog 与内置 catalog 同 key 并存时行为未定义(union locator),该通道已废弃:
                // 内置远端 catalog 固定名 catalog_live.*,更新由 .hash 驱动,无需下发 catalogUrl。
                GameLog.Warn("Res", "catalogUrl deprecated (built-in remote catalog self-updates via .hash), ignored: {0}", info.catalogUrl);
            }

            return Task.CompletedTask;
        }
    }
}
