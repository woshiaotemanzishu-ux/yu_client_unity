#if UNITY_EDITOR
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using UnityEditor;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 编辑器退出 Play 时强制注销所有运行时控制器循环。
    ///
    /// 背景:战斗/心跳等逻辑是 static 单例 + async Task 死循环(AutoFightController.RunLoopAsync /
    /// FightController.DrainAttackQueueAsync / LoginController 心跳 watcher 等)。Unity 退出 Play **不会**
    /// 杀掉这些 Task——domain reload 只在“进入”Play 时发生,不在退出时。UnitySynchronizationContext 在编辑
    /// 模式照样 pump,于是 `await Task.Delay(...)` 续体在退出 Play 后仍持续触发,循环空跑刷日志,且 NetManager
    /// 连接也未关、IsConnected 仍为 true,导致 AutoFight 继续攻击 + 20001 队列堆积 + 心跳超时(全是幽灵运行时)。
    ///
    /// 运行时唯一的清理入口是断线事件 → <see cref="GameEntryFlow.OnDisconnected"/> → <see cref="ControllerHub.DisposeAll"/>。
    /// 退出 Play 不会触发它,所以这里在编辑器侧补一刀,走与断线一致的清理路径。
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeTeardown
    {
        static PlayModeTeardown()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;

            // 先注销登录控制器:停心跳 watcher + 取消自动重连,并解订阅 EVT_NET_DISCONNECTED,
            // 避免随后的 DisconnectAsync 触发断线回调把连接又拉起来。
            LoginController.Instance.Dispose();

            // 停游戏内全部控制器循环(AutoFight / Fight / Task / Scene ...)。Initialized 守卫保证幂等。
            if (ControllerHub.Initialized) ControllerHub.DisposeAll();

            // 关闭 WebSocket(fire-and-forget;编辑器正在退出,无需等待)。
            _ = NetManager.DisconnectAsync();

            GameLog.Info("Game", "退出 Play:已强制注销运行时控制器循环 + 断开连接(防 async 循环泄漏进编辑模式)");
        }
    }
}
#endif
