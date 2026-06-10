using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
    /// Manages the remote Addressables catalog version. Called at launch.
    /// </summary>
    public static class ResVersionManager
    {
        public static ResourceVersionInfo Current { get; private set; }

        /// <summary>
        /// Apply a resource version response: redirect Addressables remote URL and reload catalog.
        /// </summary>
        public static async Task ApplyAsync(ResourceVersionInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            Current = info;

            // Override Addressables remote load path via InternalIdTransformFunc.
            string baseUrl = info.cdnBaseUrl;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                Addressables.InternalIdTransformFunc = location =>
                {
                    string id = location.InternalId;
                    if (id.StartsWith("http://", StringComparison.Ordinal) ||
                        id.StartsWith("https://", StringComparison.Ordinal))
                    {
                        // Replace existing host with current cdnBaseUrl.
                        int schemeEnd = id.IndexOf("://", StringComparison.Ordinal) + 3;
                        int firstSlash = id.IndexOf('/', schemeEnd);
                        string tail = firstSlash >= 0 ? id.Substring(firstSlash + 1) : string.Empty;
                        return baseUrl.TrimEnd('/') + "/" + tail;
                    }
                    return id;
                };
            }

            if (!string.IsNullOrEmpty(info.catalogUrl))
            {
                var loadHandle = Addressables.LoadContentCatalogAsync(info.catalogUrl, true);
                await loadHandle.Task;
                GameLog.Info("Res", "catalog loaded version={0}", info.resourceVersion);
            }
        }
    }
}
