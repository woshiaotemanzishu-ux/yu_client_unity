using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.LookOver
{
    /// <summary>
    /// LookOver(他人资料卡)模块装配:无独立 HUD 图标入口——纯上下文触发,由聊天/公会/组队等各处
    /// "点头像" 调 <see cref="LookOverFlow.Show"/>,故不向 MainUIRouter 登记。仅登记断线清理,
    /// 同 <see cref="Shenxiao.Module.Core.Friend.FriendBootstrap"/> 套路。
    /// </summary>
    public static class LookOverBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            LookOverFlow.Reset();
        }
    }
}
