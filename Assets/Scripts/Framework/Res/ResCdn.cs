namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Runtime CDN base for Addressables remote content.
    /// The Remote.LoadPath profile value is "{Shenxiao.Framework.Res.ResCdn.BaseUrl}/[BuildTarget]":
    /// [BuildTarget] is baked at build time, the {static property} part is evaluated by Addressables
    /// on first use — so Configure() MUST run before Addressables.InitializeAsync (AppLauncher does).
    /// One content build therefore serves every environment; only the URL fed here changes.
    /// </summary>
    public static class ResCdn
    {
        // Relative default keeps editor PackedPlayMode / desktop-next-to-ServerData working when no
        // URL is configured. Real devices and WebGL must Configure() an http(s) URL (from AppConfig
        // or the resource version API) or every Remote_* load will fail.
        private const string DefaultBase = "ServerData";

        private static string _baseUrl = "";

        public static string BaseUrl => string.IsNullOrEmpty(_baseUrl) ? DefaultBase : _baseUrl;

        public static bool IsConfigured => !string.IsNullOrEmpty(_baseUrl);

        /// <summary>Set the CDN base URL (trailing slash optional). Empty/whitespace keeps the current value.</summary>
        public static void Configure(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return;
            string url = baseUrl.Trim().TrimEnd('/');
            // "{streaming}" 前缀 = 随包资源(整包 APK):解析成 StreamingAssets 绝对路径
            // (Android 为 jar:file://…!/assets,UnityWebRequest 可直读)。整包构建把内容拷进
            // StreamingAssets/cdn 并烧 "{streaming}/cdn",运行时零网络依赖。
            const string streamingToken = "{streaming}";
            if (url.StartsWith(streamingToken, System.StringComparison.Ordinal))
            {
                url = UnityEngine.Application.streamingAssetsPath + url.Substring(streamingToken.Length);
            }
            _baseUrl = url.TrimEnd('/');
        }
    }
}
