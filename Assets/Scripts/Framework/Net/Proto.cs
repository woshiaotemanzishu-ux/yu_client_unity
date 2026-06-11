namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// 协议号常量,与 yu_client/yu_server 一致(请求与回包同号,注册回调即收该号回包)。
    /// 出处:yu_client h5/src/login/LoginController.ts。新协议号统一加在这里,不要散落。
    /// </summary>
    public static class Proto
    {
        // ----- 登录链(1xxxx) -----
        /// <summary>账号登录游戏服。发 "iiss"(pid, 时间戳秒, account_id, plat_name);
        /// 回包 "clihi"(career, 服务器时间l, 开服时间i, 角色数h, 注册数i)+ 逐角色数据。</summary>
        public const int ACCOUNT_LOGIN = 10000;

        /// <summary>创角。发 "cccsslsscscc",见 LoginController.ts TRY_CREATE_ROLE。</summary>
        public const int CREATE_ROLE = 10003;

        /// <summary>选角进入游戏。发 "lsisisscscsh",见 TRY_LOGIN_GAME。</summary>
        public const int ENTER_GAME = 10004;

        /// <summary>心跳(无字段)。</summary>
        public const int HEARTBEAT = 10006;

        /// <summary>顶号/踢下线通知(yu_client On10007)。</summary>
        public const int KICK_NOTIFY = 10007;
    }
}
