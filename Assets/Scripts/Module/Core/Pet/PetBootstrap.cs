using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.PetEquip;
using UnityEngine;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 宠物/坐骑模块装配:启动即向主界面功能入口中央路由登记 "pet" 打开器(主界面宠物图标 res="pet");
    /// 点击即打开/关闭外观面板(MainUIRouter.Open("pet") → PetFlow.Toggle)。MountPetView 容器未生成 → 取首页御风云骑外观。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class PetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("pet", PetFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            PetEquipFlow.Reset();
            PetFlow.Reset();
        }
    }
}
