using Shenxiao.Framework.Util;

namespace Shenxiao.Common.Tips
{
    /// <summary>
    /// Toast / floating tip / confirmation dialog. Phase 0 skeleton: log only.
    /// </summary>
    public static class TipsManager
    {
        public static void Toast(string text)
        {
            GameLog.Info("Tip", "toast: {0}", text);
        }

        public static void Float(string text)
        {
            GameLog.Info("Tip", "float: {0}", text);
        }

        public static void Confirm(string text, System.Action onYes, System.Action onNo = null)
        {
            GameLog.Info("Tip", "confirm: {0}", text);
            onYes?.Invoke();
        }
    }
}
