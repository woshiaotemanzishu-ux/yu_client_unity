using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Starts the first scene-enter request after 13001 role data is ready.
    /// </summary>
    public static class SceneEntryFlow
    {
        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_installed) return;
            _installed = true;
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        /// <summary>
        /// Editor RunCommand harness 调用入口：[RuntimeInitializeOnLoadMethod] 在编辑器非 Play 态不自动触发，
        /// harness 需在 Pump 开始前显式调用此方法完成 EVT_GAME_START 订阅。
        /// </summary>
        public static void EnsureInstalled() => Install();

        private static void OnGameStart()
        {
            SceneController.Instance.RequestEnterScene(RoleModel.Instance);
        }
    }
}
