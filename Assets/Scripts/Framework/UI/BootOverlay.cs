#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Shenxiao.Framework.UI
{
    /// <summary>
    /// 页面级启动加载层(老端式:HTML 加载页盖在引擎上面,直到游戏自己的 UI 就绪才撤)。
    /// WebGL 专用;其他平台全部 no-op。进度约定:引擎下载 0~0.85 由页面 loader 驱动,
    /// 0.85~1.0 由游戏侧(资源初始化/登录资源下载)驱动,Done() 淡出移除。
    /// </summary>
    public static class BootOverlay
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void SxBootUpdateJs(float p, string text);
        [DllImport("__Internal")] private static extern void SxBootDoneJs();

        private static bool _done;

        public static void Report(float progress, string text)
        {
            if (_done) return;
            SxBootUpdateJs(progress, text ?? "");
        }

        public static void Done()
        {
            if (_done) return;
            _done = true;
            SxBootDoneJs();
        }
#else
        public static void Report(float progress, string text) { }
        public static void Done() { }
#endif
    }
}
