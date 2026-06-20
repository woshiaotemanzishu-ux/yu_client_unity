using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商城模块装配:启动即向主界面功能入口中央路由登记 "shop" 打开器;主 HUD 商城按钮(MainUIChatView._img_shop)
    /// 点击时 MainUIRouter.Open("shop") → ShopFlow.Toggle 打开/关闭商城面板。路由解耦,MainUI 不直接依赖 Shop。
    /// 断线(非游戏内自动重连)时清商城面板。
    /// </summary>
    public static class ShopBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("shop", ShopFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            ShopFlow.Reset();
        }
    }
}
