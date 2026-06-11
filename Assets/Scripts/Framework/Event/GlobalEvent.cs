namespace Shenxiao.Framework.Event
{
    /// <summary>
    /// Global event constants. Append new entries here, do not scatter strings.
    /// Naming: EVT_{MODULE}_{ACTION}.
    /// </summary>
    public static class GlobalEvent
    {
        // ----- Boot -----
        public const string EVT_FRAMEWORK_READY = "EVT_FRAMEWORK_READY";

        // ----- Net -----
        public const string EVT_NET_CONNECTED = "EVT_NET_CONNECTED";
        public const string EVT_NET_DISCONNECTED = "EVT_NET_DISCONNECTED";
        public const string EVT_NET_ERROR = "EVT_NET_ERROR";

        // ----- Login -----
        public const string EVT_LOGIN_SUCCESS = "EVT_LOGIN_SUCCESS";
        public const string EVT_LOGIN_FAIL = "EVT_LOGIN_FAIL";
        public const string EVT_LOGIN_SERVER_SELECTED = "EVT_LOGIN_SERVER_SELECTED";
        /// <summary>游戏服 10000 回包解析完成,参数: roleCount (int)。</summary>
        public const string EVT_GAME_ROLE_LIST = "EVT_GAME_ROLE_LIST";
        /// <summary>创角结果(10003),参数: result (int,1=成功;3 重名/4 敏感字/5 长度/6 已有角色)。</summary>
        public const string EVT_GAME_CREATE_ROLE_RESULT = "EVT_GAME_CREATE_ROLE_RESULT";
        /// <summary>进入游戏成功(10004 result=1)。</summary>
        public const string EVT_GAME_ENTERED = "EVT_GAME_ENTERED";

        // ----- Bag -----
        public const string EVT_BAG_UPDATE = "EVT_BAG_UPDATE";

        // ----- Role -----
        public const string EVT_ROLE_INFO_UPDATE = "EVT_ROLE_INFO_UPDATE";

        // ----- Res -----
        public const string EVT_RES_UPDATE_PROGRESS = "EVT_RES_UPDATE_PROGRESS";
        public const string EVT_RES_UPDATE_DONE = "EVT_RES_UPDATE_DONE";
    }
}
