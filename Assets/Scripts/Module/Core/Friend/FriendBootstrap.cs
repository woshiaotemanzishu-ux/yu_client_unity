using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友模块装配:启动即向主界面功能入口中央路由登记 "friend" 打开器;主 HUD 好友按钮(MainUIChatView._img_friend)
    /// 点击时 MainUIRouter.Open("friend") → FriendFlow.Toggle 打开/关闭好友面板。路由解耦,MainUI 不直接依赖 Friend。
    /// 断线(非游戏内自动重连)时清好友面板。
    /// </summary>
    public static class FriendBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("friend", FriendFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            FriendFlow.Reset();
        }
    }
}
