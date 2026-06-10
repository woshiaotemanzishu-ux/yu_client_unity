using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// Thin wrapper over yu_gm api.php endpoints.
    /// All GET requests carry method/time/sign params: sign = lower(md5(loginKey + time + method)).
    /// Returns the raw response as JObject; callers inspect "ret" (0 = ok).
    /// </summary>
    public static class GmApi
    {
        public static string BaseUrl;
        public static string LoginKey;

        /// <summary>method=player_login, returns account info + server list.</summary>
        public static Task<JObject> PlayerLogin(string accname, string platName, string deviceId, string token = null)
        {
            var args = new (string, string)[]
            {
                ("accname", accname),
                ("site", platName),
                ("idfa", deviceId),
                ("device_id", deviceId),
                ("os", "2"),
                ("token", token),
            };
            return Get("player_login", args);
        }

        /// <summary>method=player_check_login, validates user/pwd before login flow.</summary>
        public static Task<JObject> PlayerCheckLogin(string username, string pwd)
        {
            return Get("player_check_login", new (string, string)[]
            {
                ("username", username),
                ("pwd", pwd),
            });
        }

        /// <summary>method=player_register, creates an account and returns token on success.</summary>
        public static Task<JObject> PlayerRegister(string username, string pwd, string platName)
        {
            return Get("player_register", new (string, string)[]
            {
                ("username", username),
                ("pwd", pwd),
                ("site", platName),
            });
        }

        /// <summary>method=get_server_info, returns host/port for the chosen server id.</summary>
        public static Task<JObject> GetServerInfo(long playerId, int sid, string token = null)
        {
            return Get("get_server_info", new (string, string)[]
            {
                ("player_id", playerId.ToString()),
                ("sid", sid.ToString()),
                ("token", token),
            });
        }

        /// <summary>method=get_server_list, returns the formal server dictionary and area data.</summary>
        public static Task<JObject> GetServerList()
        {
            return Get("get_server_list", null);
        }

        // ---- internals ----

        private static async Task<JObject> Get(string method, (string k, string v)[] extra)
        {
            if (string.IsNullOrEmpty(BaseUrl)) { GameLog.Error("GmApi", "BaseUrl not set"); return null; }

            string time = (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
            string sign = Md5Lower((LoginKey ?? string.Empty) + time + method);
            string baseUrl = BaseUrl.Trim();
            while (baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
            }

            var sb = new StringBuilder();
            sb.Append(baseUrl);
            sb.Append(baseUrl.IndexOf('?') >= 0 ? '&' : '?');
            sb.Append("method=").Append(Uri.EscapeDataString(method));
            sb.Append("&time=").Append(time);
            sb.Append("&sign=").Append(sign);
            if (extra != null)
            {
                foreach (var p in extra)
                {
                    if (string.IsNullOrEmpty(p.v)) continue;
                    sb.Append('&').Append(p.k).Append('=').Append(Uri.EscapeDataString(p.v));
                }
            }
            string url = sb.ToString();
            GameLog.Info("GmApi", "GET {0}", url);

            string body = await HttpUtil.GetAsync(url);
            if (string.IsNullOrEmpty(body)) return null;
            try { return JObject.Parse(body); }
            catch (Exception e) { GameLog.Error("GmApi", "parse fail: {0} body={1}", e.Message, body); return null; }
        }

        private static string Md5Lower(string s)
        {
            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                var hex = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) hex.Append(bytes[i].ToString("x2"));
                return hex.ToString();
            }
        }
    }
}
