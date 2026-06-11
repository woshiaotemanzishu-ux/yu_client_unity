using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Shenxiao.Module.Core.Login
{
    public sealed class LoginModel
    {
        private static readonly LoginModel _instance = new LoginModel();
        private readonly List<LoginServerInfo> _servers = new List<LoginServerInfo>();
        private readonly List<LoginAreaInfo> _areas = new List<LoginAreaInfo>();
        private readonly List<GameRoleInfo> _roles = new List<GameRoleInfo>();

        public static LoginModel Instance => _instance;

        public string Account { get; private set; }
        public string Token { get; private set; }
        public string PlatName { get; private set; }
        public long PlayerId { get; private set; }
        public int LastServerId { get; private set; }
        public LoginServerInfo SelectedServer { get; private set; }
        public IReadOnlyList<LoginServerInfo> Servers => _servers;
        /// <summary>大区列表(yu_gm buildServerList 的 areas:id/name/otime)。</summary>
        public IReadOnlyList<LoginAreaInfo> Areas => _areas;
        /// <summary>当前游戏服的角色列表(10000 回包)。</summary>
        public IReadOnlyList<GameRoleInfo> Roles => _roles;

        public void SetRoles(List<GameRoleInfo> roles)
        {
            _roles.Clear();
            if (roles != null) _roles.AddRange(roles);
        }

        private LoginModel()
        {
        }

        public void ResetSession(string account, string platName, string token)
        {
            Account = account;
            PlatName = platName;
            Token = token;
            PlayerId = 0;
            LastServerId = 0;
            SelectedServer = null;
            _servers.Clear();
        }

        public void ApplyPlayerLoginInfo(JObject info)
        {
            if (info == null) return;

            PlayerId = ReadLong(info, "id", PlayerId);
            Token = ReadString(info, "token", Token);
            LastServerId = ReadInt(info, "last", LastServerId);

            _servers.Clear();
            AddServerNodes(info["server"]);
            AddHistoryServers(info["player_server"]);
            AddFallbackLastServer(info);
            ApplyAreas(info["areas"]);
            SortServers();
            SelectServer(FindServer(LastServerId) ?? (_servers.Count > 0 ? _servers[0] : null));
        }

        public void ApplyServerListInfo(JObject info)
        {
            if (info == null) return;

            _servers.Clear();
            AddServerNodes(info["server"]);
            ApplyAreas(info["areas"]);
            SortServers();
            SelectServer(FindServer(LastServerId) ?? (_servers.Count > 0 ? _servers[0] : null));
        }

        private void ApplyAreas(JToken token)
        {
            _areas.Clear();
            var obj = token as JObject;
            if (obj == null) return;
            foreach (KeyValuePair<string, JToken> kv in obj)
            {
                var area = kv.Value as JObject;
                if (area == null) continue;
                _areas.Add(new LoginAreaInfo
                {
                    id = ReadInt(area, "id", ParseInt(kv.Key, 0)),
                    name = ReadString(area, "name", "第" + kv.Key + "区"),
                });
            }
            _areas.Sort((a, b) => a.id.CompareTo(b.id));
        }

        public void ApplySelectedServerInfo(JObject info)
        {
            if (info == null || SelectedServer == null) return;

            SelectedServer.host = ReadString(info, "host", SelectedServer.host);
            SelectedServer.port = ReadInt(info, "port", SelectedServer.port);
            SelectedServer.sslPort = ReadInt(info, "sslport", SelectedServer.sslPort);
            SelectedServer.lburl = ReadString(info, "lburl", SelectedServer.lburl);
            SelectedServer.accname = ReadString(info, "accname", SelectedServer.accname);
            SelectedServer.pid = ReadInt(info, "pid", SelectedServer.pid);
            SelectedServer.payMode = ReadString(info, "pay_mode", SelectedServer.payMode);
            SelectedServer.closed = ReadInt(info, "closed", SelectedServer.closed);
        }

        public void SelectServer(LoginServerInfo server)
        {
            SelectedServer = server;
            if (server != null) LastServerId = server.id;
        }

        public LoginServerInfo FindServer(int serverId)
        {
            if (serverId <= 0) return null;
            for (int i = 0; i < _servers.Count; i++)
            {
                if (_servers[i].id == serverId) return _servers[i];
            }
            return null;
        }

        private void AddServerNodes(JToken token)
        {
            AddServerNodes(token, string.Empty);
        }

        private void AddServerNodes(JToken token, string fallbackId)
        {
            if (token == null) return;
            if (token.Type == JTokenType.Array)
            {
                foreach (JToken child in token.Children()) AddServerNodes(child, string.Empty);
                return;
            }

            var obj = token as JObject;
            if (obj == null) return;

            if (LooksLikeServer(obj))
            {
                AddOrMergeServer(ReadServer(obj, fallbackId));
                return;
            }

            foreach (JProperty property in obj.Properties())
            {
                AddServerNodes(property.Value, property.Name);
            }
        }

        private void AddHistoryServers(JToken token)
        {
            if (token == null) return;

            foreach (JToken child in token.Children())
            {
                var obj = child as JObject;
                if (obj == null) continue;

                int serverId = ReadInt(obj, "server_id", ReadInt(obj, "id", 0));
                if (serverId <= 0) continue;

                LoginServerInfo existed = FindServer(serverId);
                if (existed != null)
                {
                    existed.name = ReadString(obj, "server_name", existed.name);
                    if (string.IsNullOrEmpty(existed.name)) existed.name = ReadString(obj, "name", existed.name);
                    ApplyHistoryRole(existed, obj);
                    continue;
                }

                var info = new LoginServerInfo { id = serverId };
                info.name = ReadString(obj, "server_name", info.name);
                if (string.IsNullOrEmpty(info.name)) info.name = ReadString(obj, "name", info.name);
                info.num = ReadInt(obj, "num", serverId);
                info.state = ReadInt(obj, "state", info.state);
                info.status = ReadInt(obj, "status", info.status);
                info.closed = ReadInt(obj, "closed", info.closed);
                info.closeDesc = ReadString(obj, "close_desc", info.closeDesc);
                ApplyHistoryRole(info, obj);
                AddOrMergeServer(info);
            }
        }

        private void AddFallbackLastServer(JObject info)
        {
            int serverId = ReadInt(info, "last", 0);
            if (serverId <= 0 || FindServer(serverId) != null) return;

            AddOrMergeServer(new LoginServerInfo
            {
                id = serverId,
                area = ReadInt(info, "area", 0),
                name = ReadString(info, "lastname", "S" + serverId),
                closed = ReadInt(info, "closed", 0),
                num = ReadInt(info, "num", serverId),
                closeDesc = ReadString(info, "close_desc", string.Empty),
            });
        }

        private LoginServerInfo ReadServer(JObject obj, string fallbackId)
        {
            int id = ReadInt(obj, "id", ReadInt(obj, "sid", ReadInt(obj, "server_id", ParseInt(fallbackId, 0))));
            return new LoginServerInfo
            {
                id = id,
                area = ReadInt(obj, "area", 0),
                name = ReadString(obj, "name", ReadString(obj, "server_name", "S" + id)),
                closed = ReadInt(obj, "closed", 0),
                status = ReadInt(obj, "status", 0),
                state = ReadInt(obj, "state", 0),
                num = ReadInt(obj, "num", id),
                closeDesc = ReadString(obj, "close_desc", string.Empty),
                host = ReadString(obj, "host", string.Empty),
                port = ReadInt(obj, "port", 0),
                isNew = ReadInt(obj, "is_new", 0) == 1,
            };
        }

        private static void ApplyHistoryRole(LoginServerInfo info, JObject obj)
        {
            info.roleId = ReadLong(obj, "role_id", info.roleId);
            info.nickname = ReadString(obj, "nickname", info.nickname);
            info.level = ReadInt(obj, "lv", info.level);
            info.career = ReadInt(obj, "career", info.career);
        }

        private void AddOrMergeServer(LoginServerInfo server)
        {
            if (server == null || server.id <= 0) return;

            LoginServerInfo existed = FindServer(server.id);
            if (existed == null)
            {
                _servers.Add(server);
                return;
            }

            if (!string.IsNullOrEmpty(server.name)) existed.name = server.name;
            if (!string.IsNullOrEmpty(server.closeDesc)) existed.closeDesc = server.closeDesc;
            if (!string.IsNullOrEmpty(server.host)) existed.host = server.host;
            if (!string.IsNullOrEmpty(server.lburl)) existed.lburl = server.lburl;
            if (!string.IsNullOrEmpty(server.accname)) existed.accname = server.accname;
            if (!string.IsNullOrEmpty(server.payMode)) existed.payMode = server.payMode;
            if (!string.IsNullOrEmpty(server.nickname)) existed.nickname = server.nickname;
            if (server.area != 0) existed.area = server.area;
            if (server.num != 0) existed.num = server.num;
            if (server.status != 0) existed.status = server.status;
            if (server.state != 0) existed.state = server.state;
            if (server.port != 0) existed.port = server.port;
            if (server.sslPort != 0) existed.sslPort = server.sslPort;
            if (server.pid != 0) existed.pid = server.pid;
            if (server.roleId != 0) existed.roleId = server.roleId;
            if (server.level != 0) existed.level = server.level;
            if (server.career != 0) existed.career = server.career;
            existed.isNew = server.isNew;
            existed.closed = server.closed;
        }

        private void SortServers()
        {
            _servers.Sort(CompareServer);
        }

        private static int CompareServer(LoginServerInfo left, LoginServerInfo right)
        {
            int leftNum = left.num != 0 ? left.num : left.id;
            int rightNum = right.num != 0 ? right.num : right.id;
            int numCompare = rightNum.CompareTo(leftNum);
            return numCompare != 0 ? numCompare : right.id.CompareTo(left.id);
        }

        private static bool LooksLikeServer(JObject obj)
        {
            return obj["id"] != null || obj["sid"] != null || obj["server_id"] != null || obj["closed"] != null || obj["host"] != null || obj["port"] != null;
        }

        private static int ParseInt(string raw, int fallback)
        {
            int value;
            return int.TryParse(raw, out value) ? value : fallback;
        }

        public static int ReadInt(JObject obj, string key, int fallback)
        {
            if (obj == null) return fallback;
            JToken token = obj[key];
            if (token == null) return fallback;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            int value;
            return int.TryParse(token.ToString(), out value) ? value : fallback;
        }

        public static long ReadLong(JObject obj, string key, long fallback)
        {
            if (obj == null) return fallback;
            JToken token = obj[key];
            if (token == null) return fallback;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            long value;
            return long.TryParse(token.ToString(), out value) ? value : fallback;
        }

        public static string ReadString(JObject obj, string key, string fallback)
        {
            if (obj == null) return fallback;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            string value = token.ToString();
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
