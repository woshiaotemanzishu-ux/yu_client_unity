using Shenxiao.Framework.Util;

namespace Shenxiao.Common.Loading
{
    /// <summary>
    /// Full-screen loading overlay. Phase 0 skeleton: log only.
    /// </summary>
    public static class LoadingManager
    {
        public static void Show(string hint = null)
        {
            GameLog.Info("Loading", "show {0}", hint ?? "");
        }

        public static void SetProgress(float p, string hint = null)
        {
            GameLog.Debug("Loading", "progress={0:0.00} {1}", p, hint ?? "");
        }

        public static void Hide()
        {
            GameLog.Info("Loading", "hide");
        }
    }
}
