using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>注册宝宝主界面入口，并在非自动重连断线时释放页面实例。</summary>
    public static class BabyBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("182", BabyFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            BabyFlow.Reset();
        }
    }
}
