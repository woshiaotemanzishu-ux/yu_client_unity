using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Framework.Config
{
    /// <summary>
    /// 部署侧启动配置(壳同目录 boot_config.json):CDN 等地址不烧死在壳里,改配置=改服务器上一个 JSON,零重打包。
    /// 覆盖优先级:URL ?cdn= 参数 > boot_config.json > AppConfig 烧包默认值(本地开发用 127.0.0.1)。
    /// 仅 WebGL 生效(页面同源拉取,一次快速 RTT);Android/编辑器仍走 AppConfig。
    /// </summary>
    public static class BootConfig
    {
        [Serializable]
        private class Dto
        {
            public string cdnBaseUrl;
            public string astcCdnBaseUrl;
            public string gmApiUrl;
        }

        /// <summary>拉取并覆盖 config 的可部署字段(仅内存,不落盘)。任何失败静默走烧包默认,绝不挡启动。</summary>
        public static async Task ApplyAsync(AppConfig config)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string pageUrl = Application.absoluteURL; // 形如 http://host:89/web/index.html?cdn=...
                // 1) ?cdn= 调试参数最优先(临时指向任意源,免改任何文件)
                string param = GetQueryParam(pageUrl, "cdn");
                if (!string.IsNullOrEmpty(param))
                {
                    config.addressablesCdnBaseUrl = param.TrimEnd('/');
                    GameLog.Info("Boot", "cdn override by url param: {0}", config.addressablesCdnBaseUrl);
                    return;
                }

                // 2) 壳同目录 boot_config.json(部署时想改就改,服务器上一个文本文件)
                string baseDir = pageUrl;
                int cut = baseDir.IndexOf('?');
                if (cut >= 0) baseDir = baseDir.Substring(0, cut);
                cut = baseDir.LastIndexOf('/');
                if (cut > "https://".Length) baseDir = baseDir.Substring(0, cut + 1);
                string text = await HttpUtil.GetAsync(baseDir + "boot_config.json", 8);
                if (string.IsNullOrEmpty(text))
                {
                    GameLog.Info("Boot", "boot_config.json 缺席,用烧包默认 CDN: {0}", config.addressablesCdnBaseUrl);
                    return;
                }
                Dto dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null) return;
                if (!string.IsNullOrEmpty(dto.cdnBaseUrl)) config.addressablesCdnBaseUrl = dto.cdnBaseUrl.TrimEnd('/');
                if (!string.IsNullOrEmpty(dto.astcCdnBaseUrl)) config.astcCdnBaseUrl = dto.astcCdnBaseUrl.TrimEnd('/');
                if (!string.IsNullOrEmpty(dto.gmApiUrl)) config.gmApiUrl = dto.gmApiUrl;
                GameLog.Info("Boot", "boot_config.json applied: cdn={0}", config.addressablesCdnBaseUrl);
            }
            catch (Exception e)
            {
                GameLog.Warn("Boot", "boot_config 读取失败,用烧包默认: {0}", e.Message);
            }
#else
            await Task.CompletedTask;
#endif
        }

        private static string GetQueryParam(string url, string key)
        {
            int q = url.IndexOf('?');
            if (q < 0) return null;
            foreach (string pair in url.Substring(q + 1).Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                if (pair.Substring(0, eq) == key)
                    return UnityEngine.Networking.UnityWebRequest.UnEscapeURL(pair.Substring(eq + 1));
            }
            return null;
        }
    }
}
