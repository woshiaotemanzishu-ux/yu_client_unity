using System;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Prefab 驱动的单次播放器。遮罩尺寸/位置、横幅宿主、横幅缩放与三个阶段时长均可在
    /// Inspector 调整；拖入任意 UI 场景后进入 Play Mode 也会自动预览。
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
        [SerializeField] private bool _previewOnPlay = true;

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

        private CanvasGroup _mainLayerGroup;
        private bool _addedMainLayerGroup;
        private float _savedMainAlpha;
        private bool _savedMainInteractable;
        private bool _savedMainBlocksRaycasts;

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
            if (_started) return;
            _started = true;
            _onFinished = onFinished;
            _phase = Phase.SlideIn;
            _time = 0f;
            _effectElapsed = 0f;
            MoveMasks(hidden: true);
            HideMainLayer();
        }

        public void ConfigurePrefab(RectTransform maskTop, RectTransform maskBottom, RectTransform bannerHost,
            UIEffectSlot bannerSlot, float slideInSeconds, float effectSeconds, float loadFallbackSeconds,
            float slideOutSeconds)
        {
            _maskTop = maskTop;
            _maskBottom = maskBottom;
            _bannerHost = bannerHost;
            _bannerSlot = bannerSlot;
            _slideInSeconds = Mathf.Max(0.01f, slideInSeconds);
            _effectSeconds = Mathf.Max(0f, effectSeconds);
            _loadFallbackSeconds = Mathf.Max(_effectSeconds, loadFallbackSeconds);
            _slideOutSeconds = Mathf.Max(0.01f, slideOutSeconds);
        }

        private void Update()
        {
            if (!_started || _finished) return;
            _time += Time.deltaTime;

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
                    if (_bannerReady) _effectElapsed += Time.deltaTime;
                    if ((_bannerReady && _effectElapsed >= _effectSeconds)
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
