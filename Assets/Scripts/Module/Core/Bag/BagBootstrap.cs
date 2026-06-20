using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包模块装配(对标 LoginBootstrap):启动即向主界面功能入口中央路由登记 "bag" 打开器,
    /// 使点击 HUD 底部背包功能图标即可打开/关闭背包面板(MainUIRouter.Open("bag") → BagFlow.Toggle)。
    /// 断线(非游戏内自动重连)时清背包,避免残留被销毁的旧根。
    /// </summary>
    public static class BagBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("bag", BagFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            // 游戏内自动重连保留背包(对标 MainUIFlow 的重连保活);真断线/登出才清。
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            BagFlow.Reset();
        }
    }
}
