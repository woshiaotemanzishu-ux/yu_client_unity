using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    /// <summary>将 61201..61225 活动图标路由到其真实礼包；数值是图标 id，不是购买协议。</summary>
    public static class LimitLevelShopBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            for (int id = 61201; id <= 61225; id++)
            {
                string iconType = id.ToString();
                MainUIRouter.Register(iconType, () => LimitLevelShopFlow.Toggle(iconType));
            }
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            LimitLevelShopFlow.Reset();
        }
    }
}
