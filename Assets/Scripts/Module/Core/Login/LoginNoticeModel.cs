using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>运营公告中的一段展示内容。Type 目前使用 open_login/open_inside。</summary>
    public sealed class LoginNoticeContentInfo
    {
        public string Type { get; internal set; }
        public string Content { get; internal set; }
        public int RedDotRule { get; internal set; }
    }

    /// <summary>运营公告原始条目；顺序由 belong[plat_belong] 的逗号列表决定。</summary>
    public sealed class LoginNoticeInfo
    {
        private readonly List<LoginNoticeContentInfo> _contents = new List<LoginNoticeContentInfo>();

        public string Id { get; internal set; }
        public string Title { get; internal set; }
        public long StartTime { get; internal set; }
        public long EndTime { get; internal set; }
        public string Source { get; internal set; }
        public int NewReg { get; internal set; }
        public int ShowRule { get; internal set; }
        public IReadOnlyList<LoginNoticeContentInfo> Contents => _contents;

        internal void AddContent(LoginNoticeContentInfo content) => _contents.Add(content);

        internal LoginNoticeContentInfo FindContent(string type)
        {
            for (int i = 0; i < _contents.Count; i++)
            {
                if (string.Equals(_contents[i].Type, type, StringComparison.Ordinal)) return _contents[i];
            }
            return null;
        }
    }

    /// <summary>View 直接消费的公告条目；保留公告与所选内容段，同时携带当前未读态。</summary>
    public sealed class LoginNoticeDisplayInfo
    {
        public LoginNoticeInfo Notice { get; internal set; }
        public LoginNoticeContentInfo Content { get; internal set; }
        public string ReadKey { get; internal set; }
        public bool IsUnread { get; internal set; }
    }

    /// <summary>
    /// 10207 的数据模型。公告正文来自运营 CDN，协议只负责通知重新检查版本。
    /// 配置替换、平台/渠道筛选、登录弹出规则及游戏内公告红点均集中在这里，避免 UI 重复解释 JSON。
    /// </summary>
    public sealed class LoginNoticeModel
    {
        public const string LOGIN_CONTENT = "open_login";
        public const string INSIDE_CONTENT = "open_inside";

        private const string PREF_ROOT = "login.notice.";
        private const string PREF_RED_INDEX = ".red.index";
        private const string PREF_RED_STATE = ".red.";
        private const string PREF_SESSION_DAY = ".session.day";
        private const string PREF_POP_DAY = ".popup.day";
        private const string PREF_POP_VERSION = ".popup.version";

        public static readonly LoginNoticeModel Instance = new LoginNoticeModel();

        private readonly List<LoginNoticeInfo> _orderedNotices = new List<LoginNoticeInfo>();
        private string _accountIdentity = "account.anonymous";
        private string _roleIdentity = "role.anonymous";
        private bool _roleSessionReady;
        private bool _dailyFirstLogin;
        private bool _redSessionApplied;
        private bool _autoPopupChecked;
        private bool _hasUnreadInside;

        public bool Loaded { get; private set; }
        public string Version { get; private set; } = string.Empty;
        public byte LastPushType { get; private set; }
        public bool HasPush { get; private set; }
        public bool HasUnreadInside => _hasUnreadInside;

        private LoginNoticeModel() { }

        public void BeginSession(string account, string platName)
        {
            _accountIdentity = "account." + Md5(((platName ?? string.Empty) + "\n" + (account ?? string.Empty)).Trim());
            _roleIdentity = "role.anonymous";
            _roleSessionReady = false;
            _dailyFirstLogin = false;
            _redSessionApplied = false;
            _autoPopupChecked = false;
            HasPush = false;
            LastPushType = 0;
            SetUnreadFlag(false);
        }

        /// <summary>游戏内公告已读/红点按角色隔离；只在玩家主动选角进入时开始，自动重连不重复开始。</summary>
        public void BeginRoleSession(long roleId)
        {
            _roleIdentity = roleId > 0 ? "role." + roleId : "role.anonymous";
            _roleSessionReady = roleId > 0;
            string today = TimeUtil.NowServerLocal().ToString("yyyy-MM-dd");
            string dayKey = RolePrefKey(PREF_SESSION_DAY);
            _dailyFirstLogin = !string.Equals(PrefsManager.GetString(dayKey), today, StringComparison.Ordinal);
            PrefsManager.SetString(dayKey, today);
            _redSessionApplied = false;
            RebuildInsideRed();
        }

        public void ApplyPush(byte type)
        {
            HasPush = true;
            LastPushType = type;
        }

        /// <summary>解析成功后原子替换；平台没有 belong 项是合法的已加载空快照。</summary>
        public bool TryReplace(JObject root, string version, string platBelong, string platName, out string error)
        {
            error = null;
            if (!(root?["belong"] is JObject belong) || !(root["notice"] is JObject notice))
            {
                error = "缺少 belong/notice 对象";
                return false;
            }

            var parsed = new List<LoginNoticeInfo>();
            string belongValue = ReadString(belong[platBelong ?? string.Empty]);
            if (!string.IsNullOrWhiteSpace(belongValue))
            {
                string[] ids = belongValue.Split(',');
                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i].Trim();
                    if (id.Length == 0 || !(notice[id] is JObject item)) continue;
                    LoginNoticeInfo info = ParseNotice(id, item);
                    if (info != null && MatchesSource(info.Source, platName)) parsed.Add(info);
                }
            }

            _orderedNotices.Clear();
            _orderedNotices.AddRange(parsed);
            Version = version ?? string.Empty;
            Loaded = true;
            RebuildInsideRed();
            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED);
            return true;
        }

        /// <summary>版本未变时仍重算时间窗/红点，避免跨日或公告自然过期后保留陈旧入口状态。</summary>
        public void Reevaluate()
        {
            if (!Loaded) return;
            RebuildInsideRed();
            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED);
        }

        public List<LoginNoticeDisplayInfo> GetLoginNotices() => BuildDisplayList(LOGIN_CONTENT);
        public List<LoginNoticeDisplayInfo> GetInsideNotices() => BuildDisplayList(INSIDE_CONTENT);

        public void MarkInsideRead(string readKey)
        {
            if (string.IsNullOrEmpty(readKey)) return;
            if (!_roleSessionReady) return;
            string key = RolePrefKey(PREF_RED_STATE + readKey);
            if (PrefsManager.GetInt(key, 0) == 2) return;
            PrefsManager.SetInt(key, 2);
            RefreshUnreadFlag();
        }

        /// <summary>按 show_rule/new_reg 判定本次账号会话是否自动弹登录公告；每会话只消费一次。</summary>
        public bool ShouldAutoOpenLogin(bool isNewPlayer)
        {
            if (_autoPopupChecked || !Loaded) return false;
            _autoPopupChecked = true;

            List<LoginNoticeDisplayInfo> list = GetLoginNotices();
            bool open = false;
            bool checkedVersionRule = false;
            string today = TimeUtil.NowServerLocal().ToString("yyyy-MM-dd");
            string popDayKey = AccountPrefKey(PREF_POP_DAY);
            bool firstPopupToday = !string.Equals(PrefsManager.GetString(popDayKey), today, StringComparison.Ordinal);

            for (int i = 0; i < list.Count; i++)
            {
                LoginNoticeInfo notice = list[i].Notice;
                if (isNewPlayer && notice.NewReg == 0) continue;
                switch (notice.ShowRule)
                {
                    case 1:
                        checkedVersionRule = true;
                        open |= !string.Equals(PrefsManager.GetString(AccountPrefKey(PREF_POP_VERSION)), Version, StringComparison.Ordinal);
                        break;
                    case 2:
                        open |= firstPopupToday;
                        break;
                    case 3:
                        open = true;
                        break;
                }
            }

            if (checkedVersionRule) PrefsManager.SetString(AccountPrefKey(PREF_POP_VERSION), Version);
            if (firstPopupToday) PrefsManager.SetString(popDayKey, today);
            return open;
        }

        private List<LoginNoticeDisplayInfo> BuildDisplayList(string contentType)
        {
            var result = new List<LoginNoticeDisplayInfo>();
            if (!Loaded) return result;
            long now = TimeUtil.NowSec();
            for (int i = 0; i < _orderedNotices.Count; i++)
            {
                LoginNoticeInfo notice = _orderedNotices[i];
                if (!IsActive(notice, now)) continue;
                LoginNoticeContentInfo content = notice.FindContent(contentType);
                if (content == null || string.IsNullOrEmpty(content.Content)) continue;
                string readKey = contentType == INSIDE_CONTENT && content.RedDotRule > 0 ? Md5(content.Content) : string.Empty;
                result.Add(new LoginNoticeDisplayInfo
                {
                    Notice = notice,
                    Content = content,
                    ReadKey = readKey,
                    IsUnread = _roleSessionReady && readKey.Length > 0
                        && PrefsManager.GetInt(RolePrefKey(PREF_RED_STATE + readKey), 0) == 1,
                });
            }
            return result;
        }

        private void RebuildInsideRed()
        {
            if (!Loaded || !_roleSessionReady)
            {
                SetUnreadFlag(false);
                return;
            }

            List<LoginNoticeDisplayInfo> inside = BuildDisplayList(INSIDE_CONTENT);
            var currentRules = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < inside.Count; i++)
            {
                LoginNoticeDisplayInfo item = inside[i];
                if (item.ReadKey.Length > 0) currentRules[item.ReadKey] = item.Content.RedDotRule;
            }

            string indexKey = RolePrefKey(PREF_RED_INDEX);
            string[] oldKeys = PrefsManager.GetString(indexKey, string.Empty).Split(',');
            for (int i = 0; i < oldKeys.Length; i++)
            {
                string oldKey = oldKeys[i].Trim();
                if (oldKey.Length > 0 && !currentRules.ContainsKey(oldKey))
                    PrefsManager.Remove(RolePrefKey(PREF_RED_STATE + oldKey));
            }

            foreach (KeyValuePair<string, int> pair in currentRules)
            {
                string stateKey = RolePrefKey(PREF_RED_STATE + pair.Key);
                int state = PrefsManager.GetInt(stateKey, 0);
                if (state == 0)
                {
                    PrefsManager.SetInt(stateKey, 1);
                }
                else if (state == 2 && !_redSessionApplied
                         && (pair.Value == 3 || (pair.Value == 4 && _dailyFirstLogin)))
                {
                    PrefsManager.SetInt(stateKey, 1);
                }
            }

            PrefsManager.SetString(indexKey, string.Join(",", currentRules.Keys));
            _redSessionApplied = true;
            RefreshUnreadFlag();
        }

        private void RefreshUnreadFlag()
        {
            string[] keys = PrefsManager.GetString(RolePrefKey(PREF_RED_INDEX), string.Empty).Split(',');
            bool unread = false;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].Length > 0 && PrefsManager.GetInt(RolePrefKey(PREF_RED_STATE + keys[i]), 0) == 1)
                {
                    unread = true;
                    break;
                }
            }
            SetUnreadFlag(unread);
        }

        private void SetUnreadFlag(bool value)
        {
            if (_hasUnreadInside == value) return;
            _hasUnreadInside = value;
            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_NOTICE_RED_CHANGED, value);
        }

        private string AccountPrefKey(string suffix) => PREF_ROOT + _accountIdentity + suffix;
        private string RolePrefKey(string suffix) => PREF_ROOT + _roleIdentity + suffix;

        private static LoginNoticeInfo ParseNotice(string id, JObject item)
        {
            var info = new LoginNoticeInfo
            {
                Id = id,
                Title = ReadString(item["title"]),
                StartTime = ReadLong(item["start_time"]),
                EndTime = ReadLong(item["end_time"]),
                Source = ReadString(item["source"]),
                NewReg = ReadInt(item["new_reg"]),
                ShowRule = ReadInt(item["show_rule"]),
            };

            if (item["content"] is JArray contents)
            {
                for (int i = 0; i < contents.Count; i++)
                {
                    if (!(contents[i] is JObject content)) continue;
                    info.AddContent(new LoginNoticeContentInfo
                    {
                        Type = ReadString(content["title"]),
                        Content = ReadString(content["content"]),
                        RedDotRule = ReadInt(content["red_dot_rule"]),
                    });
                }
            }
            return info;
        }

        private static bool MatchesSource(string source, string platName)
        {
            if (string.IsNullOrWhiteSpace(source)) return true;
            string target = platName ?? string.Empty;
            string[] sources = source.Split(',');
            for (int i = 0; i < sources.Length; i++)
            {
                string prefix = sources[i].Trim();
                if (prefix.Length > 0 && target.StartsWith(prefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool IsActive(LoginNoticeInfo notice, long now)
        {
            if (notice.StartTime > 0 && notice.StartTime > now) return false;
            if (notice.EndTime > 0 && notice.EndTime <= now) return false;
            return true;
        }

        private static string ReadString(JToken token)
            => token == null || token.Type == JTokenType.Null ? string.Empty : token.ToString();

        private static int ReadInt(JToken token)
        {
            int value;
            return token != null && int.TryParse(token.ToString(), out value) ? value : 0;
        }

        private static long ReadLong(JToken token)
        {
            long value;
            return token != null && long.TryParse(token.ToString(), out value) ? value : 0L;
        }

        private static string Md5(string value)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
