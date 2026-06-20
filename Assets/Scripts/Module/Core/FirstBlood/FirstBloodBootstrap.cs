using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.FirstBlood
{
    /// <summary>
    /// 首杀模块装配:启动即向主界面功能入口中央路由登记 "firstblood" 打开器;二级 HUD 首杀按钮
    /// (MainUISecondaryView._box_firstblood)点击 → MainUIRouter.Open("firstblood") → FirstBloodFlow.Toggle。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class FirstBloodBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("firstblood", FirstBloodFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            FirstBloodFlow.Reset();
        }
    }
}
