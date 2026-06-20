using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励模块装配:启动即向主界面功能入口中央路由登记 "levelreward" 打开器;二级 HUD 等级奖励按钮
    /// (MainUISecondaryView._box_level_rew)点击 → MainUIRouter.Open("levelreward") → LevelRewardFlow.Toggle。
    /// 断线(非游戏内自动重连)时清面板。
    /// </summary>
    public static class LevelRewardBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("levelreward", LevelRewardFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            LevelRewardFlow.Reset();
        }
    }
}
