using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 九霄劫魄(符文)模块装配:启动即向主界面功能入口中央路由登记 "treasure" 打开器(主界面秘宝图标 res="treasure");
    /// 点击即打开/关闭九霄劫魄面板(MainUIRouter.Open("treasure") → RuneFlow.Toggle)。秘宝容器未生成 → 取首页九霄劫魄。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class RuneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("treasure", RuneFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            RuneFlow.Reset();
        }
    }
}
