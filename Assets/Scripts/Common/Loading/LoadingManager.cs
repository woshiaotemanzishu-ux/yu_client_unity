using System;
using Shenxiao.Framework.Util;

namespace Shenxiao.Common.Loading
{
    /// <summary>
    /// 全屏加载状态的唯一运行时来源。具体视觉由登录模块注册 presenter，场景/资源模块只汇报
    /// 真实阶段和 0~1 连续进度；登录页释放后 presenter 解绑，普通切场景继续走黑幕过渡。
    /// </summary>
    public static class LoadingManager
    {
        private static Action<bool, float, string, float?> _presenter;

        public static bool IsVisible { get; private set; }
        public static float Progress { get; private set; }
        public static string Hint { get; private set; } = string.Empty;
        public static float? EstimatedSeconds { get; private set; }

        public static void BindPresenter(Action<bool, float, string, float?> presenter)
        {
            _presenter = presenter;
            Publish();
        }

        public static void UnbindPresenter(Action<bool, float, string, float?> presenter)
        {
            if (_presenter == presenter) _presenter = null;
        }

        public static void Show(string hint = null)
        {
            if (!IsVisible)
            {
                Progress = 0f;
                EstimatedSeconds = null;
            }
            IsVisible = true;
            if (!string.IsNullOrWhiteSpace(hint)) Hint = hint;
            GameLog.Info("Loading", "show {0}", hint ?? "");
            Publish();
        }

        public static void SetProgress(float p, string hint = null, float? estimatedSeconds = null)
        {
            float clamped = p < 0f ? 0f : (p > 1f ? 1f : p);
            if (clamped > Progress) Progress = clamped; // 同一加载会话只进不退
            if (!string.IsNullOrWhiteSpace(hint)) Hint = hint;
            EstimatedSeconds = estimatedSeconds.HasValue
                ? Math.Max(0f, estimatedSeconds.Value)
                : null;
            GameLog.Debug("Loading", "progress={0:0.00} {1}", Progress, Hint);
            Publish();
        }

        public static void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            GameLog.Info("Loading", "hide");
            Publish();
        }

        private static void Publish()
        {
            try
            {
                _presenter?.Invoke(IsVisible, Progress, Hint, EstimatedSeconds);
            }
            catch (Exception e)
            {
                GameLog.Warn("Loading", "presenter 更新失败: {0}", e.Message);
            }
        }
    }
}
