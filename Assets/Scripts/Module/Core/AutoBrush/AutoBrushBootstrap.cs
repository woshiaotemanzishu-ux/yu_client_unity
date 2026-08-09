using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// Auto-brush HUD and main entry wiring.
    /// </summary>
    public static class AutoBrushBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("autobrush", AutoBrushFlow.OpenMain);
            MainUIRouter.Register("autobrush_toggle", AutoBrushController.Instance.RequestToggle);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, AutoBrushFlow.Reset);
        }
    }
}
