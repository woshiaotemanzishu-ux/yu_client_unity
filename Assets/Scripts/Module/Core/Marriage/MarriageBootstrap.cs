using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋模块装配(对标 EquipBootstrap/GodBefallBootstrap):启动即向主界面功能入口中央路由登记 "love" 打开器
    /// (主界面婚恋图标 res="love");点击即打开/关闭婚恋面板(MainUIRouter.Open("love") → MarriageFlow.Toggle)。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class MarriageBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("love", MarriageFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            MarriageFlow.Reset();
        }
    }
}
