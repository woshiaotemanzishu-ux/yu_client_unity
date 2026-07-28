using System;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Prefab 驱动的单次播放器。遮罩尺寸/位置、横幅宿主、横幅缩放与三个阶段时长均可在
    /// Inspector 调整。生产流程显式调用 Begin；编辑器预览由菜单入口显式触发，避免实例化后
    /// Start 抢先以空回调启动，导致战斗流程永远收不到演出完成通知。
    /// </summary>
    public sealed class BossBornEffectPlayer : MonoBehaviour
    {
        [Header("Editable prefab nodes")]
        [SerializeField] private RectTransform _maskTop;
        [SerializeField] private RectTransform _maskBottom;
        [SerializeField] private RectTransform _bannerHost;
        [SerializeField] private UIEffectSlot _bannerSlot;

        [Header("Timing (old client: 0.15 / effect 1.5 / fallback 3.0 / 0.15)")]
        [SerializeField, Min(0.01f)] private float _slideInSeconds = 0.15f;
        [SerializeField, Min(0f)] private float _effectSeconds = 1.5f;
        [SerializeField, Min(0.01f)] private float _loadFallbackSeconds = 3f;
        [SerializeField, Min(0.01f)] private float _slideOutSeconds = 0.15f;
        [SerializeField] private bool _previewOnPlay;

        private enum Phase { SlideIn, Hold, SlideOut, Done }

        private UIEffectStage.Handle _banner;
        private Action _onFinished;
        private Phase _phase;
        private float _time;
        private float _topHidden;
        private float _bottomHidden;
        private bool _started;
        private bool _finished;
        private bool _bannerStarted;
        private bool _bannerReady;
        private float _effectElapsed;
        private float _resolvedEffectSeconds;

        private CanvasGroup _mainLayerGroup;
        private bool _addedMainLayerGroup;
        private float _savedMainAlpha;
        private bool _savedMainInteractable;
        private bool _savedMainBlocksRaycasts;

        /// <summary>播放器自身最迟应完成的真实时间，供外层独立看门狗使用。</summary>
        public float MaxPlaybackSeconds => _slideInSeconds + _loadFallbackSeconds + _slideOutSeconds;

        private void Awake()
        {
            MoveMasks(hidden: true);
        }

        private void Start()
        {
            if (_previewOnPlay && !_started) Begin(null);
        }

        public void Begin(Action onFinished)
        {
            if (_finished)
            {
                onFinished?.Invoke();
                return;
            }
            if (_started)
            {
                // 兼容已经打进旧 Addressables 的 _previewOnPlay=1：Start 可能先以 null 启动，
                // 生产流程稍后仍必须能补绑唯一的拥有者回调。
                if (_onFinished == null && onFinished != null) _onFinished = onFinished;
                return;
            }
            _started = true;
            _onFinished = onFinished;
            _phase = Phase.SlideIn;
            _time = 0f;
            _effectElapsed = 0f;
            _resolvedEffectSeconds = _effectSeconds;
            MoveMasks(hidden: true);
            HideMainLayer();
        }

        public void ConfigurePrefab(RectTransform maskTop, RectTransform maskBottom, RectTransform bannerHost,
            UIEffectSlot bannerSlot, float slideInSeconds, float effectSeconds, float loadFallbackSeconds,
            float slideOutSeconds, bool previewOnPlay = false)
        {
            _maskTop = maskTop;
            _maskBottom = maskBottom;
            _bannerHost = bannerHost;
            _bannerSlot = bannerSlot;
            _slideInSeconds = Mathf.Max(0.01f, slideInSeconds);
            _effectSeconds = Mathf.Max(0f, effectSeconds);
            _loadFallbackSeconds = Mathf.Max(_effectSeconds, loadFallbackSeconds);
            _slideOutSeconds = Mathf.Max(0.01f, slideOutSeconds);
            _previewOnPlay = previewOnPlay;
        }

        private void Update()
        {
            if (!_started || _finished) return;
            float deltaTime = Time.unscaledDeltaTime;
            _time += deltaTime;

            switch (_phase)
            {
                case Phase.SlideIn:
                    AnimateMasks(hiddenToShown: true, _time / Mathf.Max(0.01f, _slideInSeconds));
                    if (_time >= _slideInSeconds)
                    {
                        MoveMasks(hidden: false);
                        _phase = Phase.Hold;
                        _time = 0f;
                        StartBanner();
                    }
                    break;
                case Phase.Hold:
                    if (!_bannerStarted) StartBanner();
                    if (_bannerReady) _effectElapsed += deltaTime;
                    if ((_bannerReady && _effectElapsed >= _resolvedEffectSeconds)
                        || _time >= _loadFallbackSeconds)
                    {
                        BeginSlideOut();
                    }
                    break;
                case Phase.SlideOut:
                    AnimateMasks(hiddenToShown: false, _time / Mathf.Max(0.01f, _slideOutSeconds));
                    if (_time >= _slideOutSeconds) Finish();
                    break;
            }
        }

        private void BeginSlideOut()
        {
            // 老端 UIEffect 到 1.5 秒时先从宿主移除，再回调遮罩退场。这里保持相同顺序，
            // 避免文字粒子结束后 liutizuo/liutiyou 仍以循环网格形成一条常驻橙色底。
            DisposeBanner();
            _phase = Phase.SlideOut;
            _time = 0f;
        }

        private async void StartBanner()
        {
            if (_bannerStarted) return;
            _bannerStarted = true;
            if (_bannerSlot == null || _bannerHost == null)
            {
                GameLog.Warn("Scene", "大妖来袭 prefab 未绑定 BannerHost/UIEffectSlot");
                return;
            }

            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(_bannerSlot, _bannerHost);
            if (_finished || this == null || _phase != Phase.Hold)
            {
                handle?.Dispose();
                return;
            }
            if (handle == null) return;
            _banner = handle;
            _bannerReady = true;
            _effectElapsed = 0f;
            _resolvedEffectSeconds = ResolveEffectSeconds(
                _effectSeconds, handle.LongestLegacyAnimationSeconds);
            GameLog.Info("Scene",
                "大妖来袭:横幅时长 resolved={0:F3}s configured={1:F3}s legacyClip={2:F3}s",
                _resolvedEffectSeconds, _effectSeconds, handle.LongestLegacyAnimationSeconds);
        }

        private static float ResolveEffectSeconds(float configuredSeconds, float legacyAnimationSeconds)
        {
            configuredSeconds = Mathf.Max(0f, configuredSeconds);
            if (configuredSeconds <= 0f || legacyAnimationSeconds <= 0f) return configuredSeconds;
            // effect_ui_dayaolaixi 的文字/主体动画 UI_2103 在 1.083s 同步退场；两侧流体是循环粒子。
            // 取真实主体片段与配置上限的较短值，避免文字消失后只剩循环橙色底纹。
            return Mathf.Min(configuredSeconds, legacyAnimationSeconds);
        }

        private void AnimateMasks(bool hiddenToShown, float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            float fromTop = hiddenToShown ? _topHidden : 0f;
            float toTop = hiddenToShown ? 0f : _topHidden;
            float fromBottom = hiddenToShown ? _bottomHidden : 0f;
            float toBottom = hiddenToShown ? 0f : _bottomHidden;
            SetY(_maskTop, Mathf.Lerp(fromTop, toTop, t));
            SetY(_maskBottom, Mathf.Lerp(fromBottom, toBottom, t));
        }

        private void MoveMasks(bool hidden)
        {
            _topHidden = _maskTop != null ? Mathf.Abs(_maskTop.rect.height) : 670f;
            _bottomHidden = _maskBottom != null ? -Mathf.Abs(_maskBottom.rect.height) : -670f;
            SetY(_maskTop, hidden ? _topHidden : 0f);
            SetY(_maskBottom, hidden ? _bottomHidden : 0f);
        }

        private static void SetY(RectTransform node, float y)
        {
            if (node == null) return;
            Vector2 p = node.anchoredPosition;
            p.y = y;
            node.anchoredPosition = p;
        }

        private void HideMainLayer()
        {
            Transform main = ViewManager.GetLayer(UILayer.Main);
            if (main == null) return;
            _mainLayerGroup = main.GetComponent<CanvasGroup>();
            if (_mainLayerGroup == null)
            {
                _mainLayerGroup = main.gameObject.AddComponent<CanvasGroup>();
                _addedMainLayerGroup = true;
            }
            _savedMainAlpha = _mainLayerGroup.alpha;
            _savedMainInteractable = _mainLayerGroup.interactable;
            _savedMainBlocksRaycasts = _mainLayerGroup.blocksRaycasts;
            _mainLayerGroup.alpha = 0f;
            _mainLayerGroup.interactable = false;
            _mainLayerGroup.blocksRaycasts = false;
        }

        private void RestoreMainLayer()
        {
            if (_mainLayerGroup == null) return;
            _mainLayerGroup.alpha = _savedMainAlpha;
            _mainLayerGroup.interactable = _savedMainInteractable;
            _mainLayerGroup.blocksRaycasts = _savedMainBlocksRaycasts;
            if (_addedMainLayerGroup) Destroy(_mainLayerGroup);
            _mainLayerGroup = null;
        }

        private void Finish()
        {
            if (_finished) return;
            _finished = true;
            _phase = Phase.Done;
            DisposeBanner();
            RestoreMainLayer();
            Action callback = _onFinished;
            _onFinished = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            DisposeBanner();
            RestoreMainLayer();
        }

        private void DisposeBanner()
        {
            _bannerReady = false;
            _banner?.Dispose();
            _banner = null;
        }
    }
}
