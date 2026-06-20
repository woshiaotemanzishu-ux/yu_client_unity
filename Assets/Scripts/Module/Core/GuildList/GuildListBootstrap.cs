using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.GuildList
{
    /// <summary>
    /// 公会列表模块装配(对标 BagBootstrap/RoleBootstrap):启动即向主界面功能入口中央路由登记 "guild" 打开器,
    /// 使点击 HUD 底部公会功能图标即可打开/关闭公会列表面板(MainUIRouter.Open("guild") → GuildListFlow.Toggle)。
    /// 注:老端 "guild" 按会籍分支(有会主界面 / 无会列表),Unity 侧目前只接无会列表 GuildListView,作降级入口;
    /// 待 GuildMainBaseView/GuildJoinBaseView 移植后再在此分支。断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class GuildListBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("guild", GuildListFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            GuildListFlow.Reset();
        }
    }
}
