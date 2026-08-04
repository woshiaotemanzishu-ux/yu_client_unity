using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 资源加载页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginLoadingView。
    ///
    /// 结构(对标老 LoginLoadingViewSkin.scene + 截图):全屏龙纹立绘背景 + 左上 version 文本 +
    /// 底部进度条(条底图 + Filled 横向填充前景 + 进度端标) + 进度/状态文案 + 首次加载提示。
    /// 视觉结构与参数以 LoadingView.prefab 为唯一来源；运行时代码只写进度值和文案。
    ///
    /// 本类只做:① 数据绑定(进度/文案)② 功能性状态切换(进度端标显隐)。不写颜色/字号/尺寸/坐标样式。
    /// 进度推进 / 关闭时机 / 倒计时 / 网络锁 / runner 每帧 Update / 防沉迷小窗 / 3D 切换 等都是
    /// 【真·运行时】,这里不实现,由外部驱动 SetProgress(对标老端 OnChange 的数据部分)。
    ///
    /// 不调用任何 LoginFlow 方法(加载页只被外部喂进度)。
    /// 暴露给外部:
    ///   public void SetProgress(float progress01, string label = null)  // 设填充 + 文案 + 进度端标
    /// </summary>
    [UIView("prefabs/ui/login/loadingview")]
    public sealed class LoadingView : BaseView
    {
        private float _estimateStartTime;
        private float _lastProgress;
        private float _smoothedRate;
        private bool _estimating;

        // 加载页必须压在 Tip(toast)之上:不设则落默认 Window 层,进世界加载期间"穿戴成功"等
        // toast 会穿透叠在加载页上(实测)。UILayer.Loading(5) 本就是为它预留的层。
        public override UILayer Layer => UILayer.Loading;

        [Header("背景 / 文本")]
        public Image bg;                       // 全屏背景立绘
        public TextMeshProUGUI versionLabel;   // 左上 version 文本

        [Header("进度条")]
        public Image progressBack;             // 进度条底图
        public Image progressFront;            // 进度条前景(type=Filled, Horizontal)
        public Slider progressSlider;           // 只驱动 ProgressEnd handle；轨道/偏移均在 prefab 中配置
        public Image progressEnd;              // 进度端标(随进度移动)
        public TextMeshProUGUI progressLabel;  // 进度文案 "loading......N%" / 状态文案
        public TextMeshProUGUI firstLoadLabel; // 首次加载提示(静态文案)

        protected override void OnShow(object args)
        {
            ResetEstimate();
            SetProgress(0f, "准备加载");
        }

        // ---------------------------------------------------------------- 数据绑定(外部驱动)

        /// <summary>
        /// 进度驱动(对标老端 OnChange 的数据部分):progress01 = 0~1。
        /// 设前景 fillAmount、Slider 值、端标显隐并刷新文案。
        /// label 不传时显示 "loading......N%"(老端 _lb_load_progress 文案,N=progress*100 两位小数)。
        /// </summary>
        public void SetProgress(float progress01, string label = null, float? estimatedSeconds = null)
        {
            float p = Mathf.Clamp01(progress01);
            if (!_estimating || p + 0.02f < _lastProgress) ResetEstimate();

            float now = Time.realtimeSinceStartup;
            float elapsed = Mathf.Max(0f, now - _estimateStartTime);
            if (p > _lastProgress + 0.001f && elapsed >= 0.25f)
            {
                float observedRate = p / elapsed;
                _smoothedRate = _smoothedRate <= 0f
                    ? observedRate
                    : Mathf.Lerp(_smoothedRate, observedRate, 0.25f);
            }
            if (p > _lastProgress) _lastProgress = p;

            // 填充类型/方向与端标 Slider handle 均由 prefab 配置，这里只驱动 0~1 数据。
            if (progressFront != null) progressFront.fillAmount = p;
            if (progressSlider != null) progressSlider.SetValueWithoutNotify(p);

            if (progressEnd != null)
            {
                progressEnd.gameObject.SetActive(p > 0f);
            }

            if (progressLabel != null)
            {
                string stage = string.IsNullOrWhiteSpace(label) ? "加载资源" : label.Trim();
                progressLabel.text = BuildProgressText(stage, p, elapsed, estimatedSeconds);
            }
        }

        private void ResetEstimate()
        {
            _estimating = true;
            _estimateStartTime = Time.realtimeSinceStartup;
            _lastProgress = 0f;
            _smoothedRate = 0f;
        }

        private string BuildProgressText(string stage, float progress, float elapsed, float? estimatedSeconds)
        {
            float percent = progress * 100f;
            if (progress >= 0.999f) return $"{stage}  {percent:0.00}%";
            if (estimatedSeconds.HasValue)
                return $"{stage}  {percent:0.00}% · {FormatEta(estimatedSeconds.Value)}";
            if (progress <= 0.001f || elapsed < 0.5f || _smoothedRate <= 0.0001f)
                return $"{stage}  {percent:0.00}% · 正在估算";

            float seconds = Mathf.Max(0f, (1f - progress) / _smoothedRate);
            return $"{stage}  {percent:0.00}% · {FormatEta(seconds)}";
        }

        private static string FormatEta(float seconds)
        {
            if (seconds < 1f) return "预计不到 1 秒";
            if (seconds < 60f) return $"预计约 {Mathf.CeilToInt(seconds)} 秒";
            return $"预计约 {Mathf.CeilToInt(seconds / 60f)} 分钟";
        }
    }
}
