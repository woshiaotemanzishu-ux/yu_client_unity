using Shenxiao.Framework.Util;

namespace Shenxiao.Common.Guide
{
    /// <summary>
    /// Newbie guide manager. Phase 0 skeleton: log only.
    /// </summary>
    public static class GuideManager
    {
        public static void Start(int guideId)
        {
            GameLog.Info("Guide", "start id={0}", guideId);
        }

        public static void Stop(int guideId)
        {
            GameLog.Info("Guide", "stop id={0}", guideId);
        }
    }
}
