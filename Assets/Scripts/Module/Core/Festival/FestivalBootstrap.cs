using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Festival
{
    /// <summary>
    /// 祭典/宝录(Festival)模块装配(自动循环 轮18 便宜活批 PK3 实做,仿
    /// <see cref="Shenxiao.Module.Core.Halo.HaloBootstrap"/> 套路)。登记 MainUIRouter "223"
    /// (<see cref="FestivalModel.ICON_TYPE"/>,老端点击主界面宝录图标 → FestivalBaseView)→
    /// <see cref="FestivalFlow.Toggle"/>。断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class FestivalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("223", FestivalFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            FestivalFlow.Reset();
        }
    }
}
