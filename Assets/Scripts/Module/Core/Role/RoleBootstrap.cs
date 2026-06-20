using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 角色模块装配(对标 BagBootstrap):启动即向主界面功能入口中央路由登记 "role" 打开器,
    /// 使点击 HUD 底部角色功能图标即可打开/关闭角色面板(MainUIRouter.Open("role") → RoleFlow.Toggle)。
    /// 断线(非游戏内自动重连)时清角色面板。
    /// </summary>
    public static class RoleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("role", RoleFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            RoleFlow.Reset();
        }
    }
}
