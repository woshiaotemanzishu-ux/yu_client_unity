using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 运营公告 CDN 加载器。10207(type!=0)与登录页初次检查共用同一串行入口；失败保留旧快照，
    /// 只有正文成功解析后才提交版本号，避免“版本已记住但正文没落地”的半更新状态。
    /// </summary>
    public static class LoginNoticeService
    {
        private const string VERSION_FILE = "login_notice/jzy/login_notice.json.cfg.v";
        private const string CONTENT_FILE = "login_notice/jzy/login_notice.json.cfg";
        private const string PREF_VERSION = "login.notice.cdn.version";

        private static AppConfig _config;
        private static bool _refreshing;
        private static bool _refreshPending;
        private static Task _refreshTask;

        public static void Setup(AppConfig config) => _config = config;

        public static Task RefreshAsync()
        {
            if (_refreshing)
            {
                _refreshPending = true;
                return _refreshTask ?? Task.CompletedTask;
            }
            _refreshTask = RefreshLoopAsync();
            return _refreshTask;
        }

        private static async Task RefreshLoopAsync()
        {
            _refreshing = true;
            try
            {
                do
                {
                    _refreshPending = false;
                    await RefreshOnceAsync();
                }
                while (_refreshPending);
            }
            finally
            {
                _refreshing = false;
            }
        }

        private static async Task RefreshOnceAsync()
        {
            if (_config == null || string.IsNullOrWhiteSpace(_config.noticeCdnBaseUrl))
            {
                GameLog.Warn("LoginNotice", "noticeCdnBaseUrl 为空，跳过运营公告检查");
                return;
            }

            string baseUrl = _config.noticeCdnBaseUrl.Trim().TrimEnd('/') + "/";
            string bust = "?v=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string versionText = await HttpUtil.GetAsync(baseUrl + VERSION_FILE + bust, 10);
            if (string.IsNullOrEmpty(versionText)) return;

            string remoteVersion;
            try
            {
                remoteVersion = (string)JObject.Parse(versionText)["version"] ?? string.Empty;
            }
            catch (Exception e)
            {
                GameLog.Warn("LoginNotice", "公告版本 JSON 解析失败: {0}", e.Message);
                return;
            }

            LoginNoticeModel model = LoginNoticeModel.Instance;
            string localVersion = PrefsManager.GetString(PREF_VERSION, string.Empty);
            if (model.Loaded && remoteVersion.Length > 0 && string.Equals(localVersion, remoteVersion, StringComparison.Ordinal))
            {
                model.Reevaluate();
                return;
            }

            string contentText = await HttpUtil.GetAsync(baseUrl + CONTENT_FILE + bust, 10);
            if (string.IsNullOrEmpty(contentText)) return;

            JObject root;
            try
            {
                root = JObject.Parse(contentText);
            }
            catch (Exception e)
            {
                GameLog.Warn("LoginNotice", "公告正文 JSON 解析失败，保留旧快照: {0}", e.Message);
                return;
            }

            if (!model.TryReplace(root, remoteVersion, _config.platBelong, _config.platName, out string error))
            {
                GameLog.Warn("LoginNotice", "公告正文结构无效，保留旧快照: {0}", error);
                return;
            }

            PrefsManager.SetString(PREF_VERSION, remoteVersion);
            GameLog.Info("LoginNotice", "公告 CDN 快照更新 version={0} login={1} inside={2}",
                remoteVersion, model.GetLoginNotices().Count, model.GetInsideNotices().Count);
        }
    }
}
