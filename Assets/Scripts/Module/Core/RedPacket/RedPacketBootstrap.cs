using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.RedPacket
{
    /// <summary>
    /// 红包模块装配:启动即向主界面功能入口中央路由登记 "redpacket" 打开器;二级 HUD 红包按钮(MainUISecondaryView._box_red_packet)
    /// 点击时 MainUIRouter.Open("redpacket") → RedPacketFlow.Toggle 打开/关闭红包面板。路由解耦,MainUI 不直接依赖 RedPacket。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class RedPacketBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("redpacket", RedPacketFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            RedPacketFlow.Reset();
        }
    }
}
