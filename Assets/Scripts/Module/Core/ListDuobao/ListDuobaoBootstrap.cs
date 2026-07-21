using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public static class ListDuobaoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // 331@110 是 windowscomponent 共享父入口；已有通用容器时不覆盖它。
            if (!MainUIRouter.IsRegistered("331@110"))
                MainUIRouter.Register("331@110", ListDuobaoFlow.Toggle);
            MainUIRouter.Register("331@116@0", ListDuobaoFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            ListDuobaoFlow.Reset();
        }
    }
}
