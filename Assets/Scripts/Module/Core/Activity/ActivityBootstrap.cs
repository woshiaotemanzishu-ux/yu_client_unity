using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Activity
{
    public static class ActivityBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("AccumRechargeView", () => ActivityFlow.Toggle("AccumRechargeView"));
            MainUIRouter.Register("ConRechargeView", () => ActivityFlow.Toggle("ConRechargeView"));
            MainUIRouter.Register("DailySupplyView", () => ActivityFlow.Toggle("DailySupplyView"));
            MainUIRouter.Register("CreatRoleGiftView", () => ActivityFlow.Toggle("CreatRoleGiftView"));
            MainUIRouter.Register("rechargeReturnView", () => ActivityFlow.Toggle("rechargeReturnView"));
            // windowscomponent 的 331@109 只有连续充值一页，可以安全直达；多页容器键不在这里劫持。
            MainUIRouter.Register("331@109", () => ActivityFlow.Toggle("ConRechargeView"));
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (!LoginController.Instance.CanAutoReconnectInGame) ActivityFlow.Reset();
        }
    }
}
