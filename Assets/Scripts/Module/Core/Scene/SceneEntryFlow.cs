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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        private static void OnGameStart()
        {
            SceneController.Instance.RequestEnterScene(RoleModel.Instance);
        }
    }
}
