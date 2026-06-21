using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Map
{
    /// <summary>
    /// 地图模块装配:启动即向中央路由登记 "map" 打开器;顶部状态条地图按钮(MainUITopView._box_map)点击 → MainUIRouter.Open("map") → MapFlow.Toggle。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class MapBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("map", MapFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            MapFlow.Reset();
        }
    }
}
