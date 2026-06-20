using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临模块装配(对标 EquipBootstrap):启动即向主界面功能入口中央路由登记 "232" 打开器
    /// (主界面第二行功能图标 res="232" → GodBefallMainView);点击即打开/关闭神祇面板
    /// (MainUIRouter.Open("232") → GodBefallFlow.Toggle)。断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class GodBefallBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("232", GodBefallFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            GodBefallFlow.Reset();
        }
    }
}
