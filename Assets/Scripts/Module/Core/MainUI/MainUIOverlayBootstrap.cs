using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面浮层装配:登记顶部状态条浮层入口 —— buff(_box_buff→MainUIBuffView)、fightmode(_box_fight_mode→MainUIFightModeView)。
    /// 均为独立 prefab 浮层,经 <see cref="MainUIOverlay.Toggle"/> 开关。断线(非游戏内自动重连)清理。
    /// </summary>
    public static class MainUIOverlayBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("buff", () => MainUIOverlay.Toggle("mainUI", "MainUIBuffView"));
            MainUIRouter.Register("fightmode", () => MainUIOverlay.Toggle("mainUI", "MainUIFightModeView"));
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            MainUIOverlay.ResetAll();
        }
    }
}
