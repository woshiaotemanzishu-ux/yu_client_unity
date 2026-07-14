using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;

namespace Shenxiao.Framework
{
    /// <summary>
    /// Bootstraps all framework systems. Place a single instance in the launch scene
    /// and assign AppConfig in the inspector.
    /// </summary>
    public class AppLauncher : MonoBehaviour
    {
        [Header("Config")]
        public AppConfig appConfig;

        [Header("UI Root")]
        public Canvas rootCanvas;

        private LayerManager _layers;

        private async void Start()
        {
            DontDestroyOnLoad(gameObject);

            if (appConfig == null) { GameLog.Error("App", "AppConfig not assigned"); return; }
            if (rootCanvas == null) { GameLog.Error("App", "rootCanvas not assigned"); return; }

            GameLog.Info("App", "launching env={0} appVersion={1}", appConfig.env, appConfig.appVersion);

            // 1. UI layer system
            _layers = new LayerManager();
            _layers.Init(rootCanvas);
            ViewManager.Init(_layers);

            // 2. CDN base must be set before Addressables.InitializeAsync:
            //    Remote.LoadPath = "{Shenxiao.Framework.Res.ResCdn.BaseUrl}/[BuildTarget]",
            //    the {} part is evaluated (and cached) by Addressables on first use.
            // 部署侧覆盖(壳同目录 boot_config.json / ?cdn= 参数):CDN 地址不烧死在壳里,改配置零重打包。
            await BootConfig.ApplyAsync(appConfig);
            string cdnBase = appConfig.addressablesCdnBaseUrl;
            // 手机 GPU 无 DXT:主内容的 DXT 纹理会被软解成 RGBA32(内存数倍+加载慢)。
            // 配置了 ASTC 变体源且设备缺 DXT、有 ASTC 时自动换源(内容由 打包/⑦ ASTC变体 构建)。
            if (!string.IsNullOrEmpty(appConfig.astcCdnBaseUrl)
                && !SystemInfo.SupportsTextureFormat(TextureFormat.DXT5)
                && SystemInfo.SupportsTextureFormat(TextureFormat.ASTC_6x6))
            {
                cdnBase = appConfig.astcCdnBaseUrl;
                GameLog.Info("App", "device lacks DXT → ASTC content source: {0}", cdnBase);
            }
            ResCdn.Configure(cdnBase);

            // 2b. Resource version handshake (skipped if URL empty)
            if (!string.IsNullOrEmpty(appConfig.addressablesCdnBaseUrl)
                || !string.IsNullOrEmpty(appConfig.addressablesCatalogUrl))
            {
                await ApplyResourceVersion(new ResourceVersionInfo
                {
                    env = appConfig.env,
                    platform = Application.platform.ToString(),
                    resourceVersion = appConfig.appVersion,
                    cdnBaseUrl = appConfig.addressablesCdnBaseUrl,
                    catalogUrl = appConfig.addressablesCatalogUrl,
                });
            }

            if (!string.IsNullOrEmpty(appConfig.resourceVersionApiUrl))
            {
                await TryApplyResourceVersion();
            }

            // 3. Initialize Addressables (no-op if catalog already loaded above).
            BootOverlay.Report(0.86f, "正在连接资源服务器…");
            var initHandle = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            await AddressablesAwaiter.Wait(initHandle);

            BootOverlay.Report(0.88f, "正在准备登录资源…");
            GameLog.Info("App", "framework ready");

            // Hand off to game flow (login bootstrap listens for this event).
            EventDispatcher.Emit(GlobalEvent.EVT_FRAMEWORK_READY, appConfig);
        }

        private void Update()
        {
            // Pump network inbox on the main thread.
            NetManager.Pump();
        }

        private async Task TryApplyResourceVersion()
        {
            string body = await HttpUtil.GetAsync(appConfig.resourceVersionApiUrl);
            if (string.IsNullOrEmpty(body))
            {
                GameLog.Warn("App", "resource version api unreachable, falling back to local catalog");
                return;
            }
            try
            {
                var wrapper = JsonConvert.DeserializeObject<ApiWrapper<ResourceVersionInfo>>(body);
                if (wrapper?.data != null) await ApplyResourceVersion(wrapper.data);
            }
            catch (System.Exception e)
            {
                GameLog.Error("App", "parse resource version fail: {0}", e.Message);
            }
        }

        private async Task ApplyResourceVersion(ResourceVersionInfo info)
        {
            if (info == null) return;

            if (IsLegacyRawCdn(info.cdnBaseUrl) || IsLegacyRawCdn(info.catalogUrl))
            {
                GameLog.Error("App", "reject legacy raw CDN for Unity Addressables: cdn={0} catalog={1}", info.cdnBaseUrl, info.catalogUrl);
                return;
            }

            await ResVersionManager.ApplyAsync(info);
        }

        private bool IsLegacyRawCdn(string url)
        {
            string legacy = NormalizeUrl(appConfig.legacyResourceCdnBaseUrl);
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(legacy)) return false;

            string normalized = NormalizeUrl(url);
            return string.Equals(normalized, legacy, System.StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(legacy, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            return url.Trim().TrimEnd('/') + "/";
        }

        [System.Serializable]
        private class ApiWrapper<T>
        {
            public int code { get; set; }
            public string msg { get; set; }
            public T data { get; set; }
        }
    }
}
