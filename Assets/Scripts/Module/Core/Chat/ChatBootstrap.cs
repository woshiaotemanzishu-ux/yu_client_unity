using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 聊天模块装配:启动即向主界面功能入口中央路由登记 "chat" 打开器;主 HUD 聊天框(MainUIChatView)点击时
    /// MainUIRouter.Open("chat") → ChatFlow.Toggle 打开/关闭全屏聊天窗(对标老端 OPEN_CHAT_VIEW)。
    /// 用 MainUIRouter 解耦:MainUI 不直接依赖 Chat 模块。断线(非游戏内自动重连)时清聊天窗。
    /// </summary>
    public static class ChatBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("chat", ChatFlow.Toggle);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnDisconnected()
        {
            if (LoginController.Instance.CanAutoReconnectInGame)
            {
                return;
            }
            ChatModel.Instance.Reset();
            ChatFlow.Reset();
        }
    }
}
