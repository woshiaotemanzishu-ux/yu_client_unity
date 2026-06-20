using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Composite
{
    /// <summary>
    /// 合成模块装配:启动即向主界面功能入口中央路由登记 "composite" 打开器(主界面合成图标 res="composite");
    /// 点击即打开/关闭合成面板(MainUIRouter.Open("composite") → CompositeFlow.Toggle)。合成容器未生成 → 取首页装备合成。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class CompositeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("composite", CompositeFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            CompositeFlow.Reset();
        }
    }
}
