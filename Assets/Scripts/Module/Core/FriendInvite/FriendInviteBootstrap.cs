using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.FriendInvite
{
    /// <summary>好友邀请入口装配；仅登记已落地的只读主窗，不恢复任何分享或领奖事务。</summary>
    public static class FriendInviteBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register(FriendInviteModel.ICON_TYPE, FriendInviteFlow.Toggle);
            MainUIRouter.Register("FriendInviteView", FriendInviteFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (!LoginController.Instance.CanAutoReconnectInGame) FriendInviteFlow.Reset();
        }
    }
}
