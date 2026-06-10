using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Shenxiao.Framework.Util
{
    /// <summary>
    /// Minimal HTTP utility based on UnityWebRequest.
    /// </summary>
    public static class HttpUtil
    {
        public static async Task<string> GetAsync(string url, int timeoutSec = 10)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = timeoutSec;
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    GameLog.Warn("Http", "GET fail url={0} err={1}", url, req.error);
                    return null;
                }
                return req.downloadHandler.text;
            }
        }

        public static async Task<string> PostJsonAsync(string url, string jsonBody, int timeoutSec = 10)
        {
            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] bytes = string.IsNullOrEmpty(jsonBody) ? Array.Empty<byte>() : System.Text.Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = timeoutSec;

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    GameLog.Warn("Http", "POST fail url={0} err={1}", url, req.error);
                    return null;
                }
                return req.downloadHandler.text;
            }
        }
    }
}
