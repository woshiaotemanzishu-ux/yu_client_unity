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
        }
    }
}
