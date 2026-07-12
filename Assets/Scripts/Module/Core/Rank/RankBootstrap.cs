using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Rank
{
    /// <summary>
    /// 排行榜模块装配(自动循环 轮12 #12):启动即向主界面功能入口中央路由登记 "activity_rank" 打开器,
    /// 修复 HUD 竞榜卡孤儿路由(MainUIRankView.cs:387 RouteClick(_box_rank, "activity_rank") 全仓此前零注册,
    /// 点击打不开任何东西——本条目仅新增注册,不改 MainUIRankView 自身业务)。
    /// 断线(非游戏内自动重连)时清排行榜面板。
    /// </summary>
    public static class RankBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("activity_rank", RankFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            RankFlow.Reset();
        }
    }
}
