using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Welfare;
using UnityEngine;

namespace Shenxiao.Module.Core.GameNotice
{
    /// <summary>
    /// 当前 WelfareView 外壳尚未迁移，417 入口先落到其已完成的“游戏公告”子页；
    /// 红点与签到/在线福利聚合，后续接回 WelfareView 时本子页和模型无需改动。
    /// </summary>
    public static class GameNoticeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("417", GameNoticeFlow.ToggleInside);
            EventDispatcher.On<bool>(GlobalEvent.EVT_LOGIN_NOTICE_RED_CHANGED, OnNoticeRedChanged);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        public static void RefreshEntranceRedDot()
        {
            bool show = LoginNoticeModel.Instance.HasUnreadInside || WelfareModel.Instance.HasEntranceRedDot();
            ActivityIconManager.Instance.SetIconRedDot("417", show);
        }

        private static void OnNoticeRedChanged(bool _) => RefreshEntranceRedDot();

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame) return;
            GameNoticeFlow.Reset();
        }
    }
}
