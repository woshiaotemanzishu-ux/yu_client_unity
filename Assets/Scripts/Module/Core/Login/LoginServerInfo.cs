namespace Shenxiao.Module.Core.Login
{
    public sealed class LoginServerInfo
    {
        public int id;
        public int area;
        public string name;
        public int closed;
        public int status;
        public int state;
        public int num;
        public string closeDesc;
        public string host;
        public int port;
        public int sslPort;
        public string lburl;
        public string accname;
        public int pid;
        public string time;     // get_server_info 下发的登录时间戳,10000 原样回传(老客户端 time_stamp)
        public string payMode;
        public long roleId;
        public string nickname;
        public int level;
        public int career;
        public bool isNew;

        public bool IsClosed => closed == 1;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(name)) return name;
                return id > 0 ? "S" + id : "Unknown";
            }
        }
    }
}

namespace Shenxiao.Module.Core.Login
{
    /// <summary>大区信息(选服页左侧 tab 数据源)。</summary>
    public sealed class LoginAreaInfo
    {
        public int id;
        public string name = "";
    }
}
