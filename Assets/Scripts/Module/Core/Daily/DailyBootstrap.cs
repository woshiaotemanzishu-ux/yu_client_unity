using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日活动(资源找回)模块装配:启动即向主界面功能入口中央路由登记 "dailyfind" 打开器;二级 HUD 每日寻宝按钮
    /// (MainUISecondaryView._box_daily_find)点击 → MainUIRouter.Open("dailyfind") → DailyFlow.Toggle(降级直开资源找回页)。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class DailyBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("dailyfind", DailyFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            DailyFlow.Reset();
        }
    }
}
