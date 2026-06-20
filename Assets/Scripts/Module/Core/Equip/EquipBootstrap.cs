using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备模块装配(对标 BagBootstrap/RoleBootstrap):启动即向主界面功能入口中央路由登记 "equip" 打开器,
    /// 使点击 HUD 底部装备功能图标即可打开/关闭装备面板(MainUIRouter.Open("equip") → EquipFlow.Toggle)。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class EquipBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("equip", EquipFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            EquipFlow.Reset();
        }
    }
}
