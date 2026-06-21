using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// VIP 模块装配:登记顶部状态条两入口 —— "vip"(_box_vip→VipBaseView)、"recharge"(_box_recharge→RechargeView)。
    /// 均经 VipFlow.Open(viewName)(共用 VipModule)。断线(非游戏内自动重连)清理。
    /// </summary>
    public static class VipBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("vip", () => VipFlow.Open("VipBaseView"));
            MainUIRouter.Register("recharge", () => VipFlow.Open("RechargeView"));
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            VipFlow.Reset();
        }
    }
}
