using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装模块装配:启动即向主界面功能入口中央路由登记 "fashion" 打开器(对标老端角色页 fashion_gp 按钮
    /// → SWITCH_ROLE_SECOND_BUTTON,Fashion=2;role/EquipmentView.ts:241-243)。
    /// ⚠本轮未接主界面实际按钮点击点(Role/EquipmentView.cs 不在本包 Module/Core/Fashion/** 所有权内)——
    /// 面板已可通过 MainUIRouter.Open("fashion") 打开,接线留给下一次碰 Role 家族的人补(见任务归档 todos_left)。
    /// 断线(非游戏内自动重连)时清面板,对标 PetBootstrap 同款处理。
    /// </summary>
    public static class FashionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("fashion", FashionFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            FashionFlow.Reset();
        }
    }
}
