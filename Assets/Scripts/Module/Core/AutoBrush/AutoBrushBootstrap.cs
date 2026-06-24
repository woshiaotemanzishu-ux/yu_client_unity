using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// Auto-brush HUD toggle wiring. The main page route remains unregistered until AutoBrushBaseView is ported.
    /// </summary>
    public static class AutoBrushBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MainUIRouter.Register("autobrush_toggle", AutoBrushController.Instance.RequestToggle);
        }
    }
}
