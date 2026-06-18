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

        /// <summary>GM 秘籍清单到达(GmCheatController,11100 回包解析完)。</summary>
        public const string EVT_GM_CHEAT_LIST = "EVT_GM_CHEAT_LIST";
        /// <summary>进入游戏成功(10004 result=1)。</summary>
        public const string EVT_GAME_ENTERED = "EVT_GAME_ENTERED";
        public const string EVT_GAME_START_FLAG_READY = "EVT_GAME_START_FLAG_READY";
        public const string EVT_GAME_START = "EVT_GAME_START";

        // ----- Bag -----
        public const string EVT_BAG_UPDATE = "EVT_BAG_UPDATE";

        // ----- Role -----
        public const string EVT_ROLE_INFO_UPDATE = "EVT_ROLE_INFO_UPDATE";
        /// <summary>主角全量(13001)到齐,可进主城/场景。GameEntryFlow 发。</summary>
        public const string EVT_ROLE_READY = "EVT_ROLE_READY";

        // ----- Scene -----
        public const string EVT_SCENE_MAP_READY = "EVT_SCENE_MAP_READY";
        /// <summary>12002 场景快照解析完成、场景对象表(SceneManager)已就绪。携带数据用 SceneManager 的强类型事件。</summary>
        public const string EVT_SCENE_SNAPSHOT_READY = "EVT_SCENE_SNAPSHOT_READY";
        /// <summary>切场景/登出:场景对象表已清空。</summary>
        public const string EVT_SCENE_OBJECTS_CLEARED = "EVT_SCENE_OBJECTS_CLEARED";

        // ----- Notice / 公告 -----
        /// <summary>系统公告(11020)到达,读 NoticeModel.LastSysNotice。</summary>
        public const string EVT_SYS_NOTICE = "EVT_SYS_NOTICE";
        /// <summary>传闻广播(11015/11018)到达,读 NoticeModel.RecentChuanwen。</summary>
        public const string EVT_CHUANWEN = "EVT_CHUANWEN";

        // ----- Task -----
        public const string EVT_TASK_LIST_UPDATED = "EVT_TASK_LIST_UPDATED";
        public const string EVT_TASK_ONE_UPDATED = "EVT_TASK_ONE_UPDATED";

        // ----- AutoBrush -----
        public const string EVT_AUTOBRUSH_INFO_UPDATED = "EVT_AUTOBRUSH_INFO_UPDATED";
        public const string EVT_AUTOBRUSH_LEVEL_UPDATED = "EVT_AUTOBRUSH_LEVEL_UPDATED";
        public const string EVT_AUTOBRUSH_STATE_UPDATED = "EVT_AUTOBRUSH_STATE_UPDATED";

        // ----- MainUI -----
        public const string EVT_MAINUI_ACTIVITY_ICON_ADD = "EVT_MAINUI_ACTIVITY_ICON_ADD";
        public const string EVT_MAINUI_ACTIVITY_ICON_DELETE = "EVT_MAINUI_ACTIVITY_ICON_DELETE";
        public const string EVT_MAINUI_ACTIVITY_ICON_UPDATE = "EVT_MAINUI_ACTIVITY_ICON_UPDATE";

        // ----- Res -----
        public const string EVT_RES_UPDATE_PROGRESS = "EVT_RES_UPDATE_PROGRESS";
        public const string EVT_RES_UPDATE_DONE = "EVT_RES_UPDATE_DONE";
    }
}
