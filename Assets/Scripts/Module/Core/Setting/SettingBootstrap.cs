using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置模块装配:启动即向主界面功能入口中央路由登记 "setting" 打开器;主 HUD 设置按钮(MainUIChatView._box_setting)
    /// 点击时 MainUIRouter.Open("setting") → SettingFlow.Toggle 打开/关闭设置面板。用路由解耦,MainUI 不直接依赖 Setting。
    /// 断线(非游戏内自动重连)时清设置面板。
    /// </summary>
    public static class SettingBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("setting", SettingFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            SettingFlow.Reset();
        }
    }
}
