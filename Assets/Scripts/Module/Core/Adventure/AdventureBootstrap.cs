using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Adventure
{
    /// <summary>注册天天冒险两种活动图标；图标数值同时也是老端的入口身份。</summary>
    public static class AdventureBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register(AdventureModel.ICON_TYPE_A, AdventureFlow.Toggle);
            MainUIRouter.Register(AdventureModel.ICON_TYPE_B, AdventureFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            AdventureFlow.Reset();
        }
    }
}
