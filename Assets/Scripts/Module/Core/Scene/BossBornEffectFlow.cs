using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.Scene.Vo;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 大妖副本 Boss 入场演出入口。老端在 boss>=3 的怪物真正加入场景后才打开
    /// DungeonFightSceneMaskView；本端只在主线大妖占位怪 7001 上接管，避免所有副本 Boss
    /// 都误播。静态布局与时序参数全部在 BossBornIntro.prefab 中维护。
    /// </summary>
    public static class BossBornEffectFlow
    {
        private const string PrefabModule = "scene";
        private const string PrefabName = "BossBornIntro";

        private static readonly HashSet<int> Shown = new HashSet<int>();
        private static GameObject _activeRoot;
        private static bool _loading;
        private static int _epoch;
        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_installed) return;
            _installed = true;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, Reset);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, Reset);
        }

        public static void NotifyMonsterAdded(MonsterVo vo)
        {
            if (vo == null || !vo.IsBoss) return;
            if (vo.TypeId != AutoBrushModel.AutoBrushMonsterId) return;
            if (!Shown.Add(vo.InstanceId)) return;
            _ = PlayAsync(vo);
        }

        public static void Reset()
        {
            Shown.Clear();
            _epoch++;
            _loading = false;
            ReleaseActive();
        }

        private static async Task PlayAsync(MonsterVo vo)
        {
            if (_loading || _activeRoot != null) return;
            if (!(ViewManager.GetLayer(UILayer.Top) is RectTransform topLayer))
            {
                GameLog.Warn("Scene", "大妖来袭:Top 层不可用,跳过 ins={0}", vo.InstanceId);
                AutoBrushBattleFlow.OnBossIntroUnavailable();
                return;
            }

            int epoch = ++_epoch;
            _loading = true;
            AutoBrushBattleFlow.OnBossIntroStarted();

            string key = GameResPath.GetUIPrefab(PrefabModule, PrefabName);
            GameObject root = null;
            try
            {
                root = await ResManager.InstantiateAsync(key, topLayer);
            }
            catch (Exception e)
            {
                GameLog.Warn("Scene", "大妖来袭 prefab 加载异常:key={0} error={1}", key, e.Message);
            }
            if (epoch != _epoch)
            {
                if (root != null) ResManager.ReleaseInstance(root);
                return;
            }

            _loading = false;
            if (root == null)
            {
                GameLog.Warn("Scene", "大妖来袭 prefab 加载失败:key={0};直接进入战斗,不锁死流程", key);
                AutoBrushBattleFlow.OnBossIntroUnavailable();
                return;
            }

            BossBornEffectPlayer player = root.GetComponent<BossBornEffectPlayer>();
            if (player == null)
            {
                GameLog.Error("Scene", "大妖来袭 prefab 缺少 BossBornEffectPlayer:key={0}", key);
                ResManager.ReleaseInstance(root);
                AutoBrushBattleFlow.OnBossIntroUnavailable();
                return;
            }

            _activeRoot = root;
            root.name = PrefabName;
            player.Begin(OnPlayerFinished);
            GameLog.Info("Scene", "大妖来袭:play ins={0} type={1} name=\"{2}\"", vo.InstanceId, vo.TypeId, vo.Name);
        }

        private static void OnPlayerFinished()
        {
            ReleaseActive();
            AutoBrushBattleFlow.OnBossIntroFinished();
        }

        private static void ReleaseActive()
        {
            GameObject root = _activeRoot;
            _activeRoot = null;
            if (root != null) ResManager.ReleaseInstance(root);
        }
    }

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

        [Header("Timing (old client: 0.15 / 3.0 / 0.15)")]
        [SerializeField, Min(0.01f)] private float _slideInSeconds = 0.15f;
        [SerializeField, Min(0f)] private float _holdSeconds = 3f;
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
            MoveMasks(hidden: true);
            HideMainLayer();
        }

        public void ConfigurePrefab(RectTransform maskTop, RectTransform maskBottom, RectTransform bannerHost,
            UIEffectSlot bannerSlot, float slideInSeconds, float holdSeconds, float slideOutSeconds)
        {
            _maskTop = maskTop;
            _maskBottom = maskBottom;
            _bannerHost = bannerHost;
            _bannerSlot = bannerSlot;
            _slideInSeconds = Mathf.Max(0.01f, slideInSeconds);
            _holdSeconds = Mathf.Max(0f, holdSeconds);
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
                    if (_time >= _holdSeconds)
                    {
                        _phase = Phase.SlideOut;
                        _time = 0f;
                    }
                    break;
                case Phase.SlideOut:
                    AnimateMasks(hiddenToShown: false, _time / Mathf.Max(0.01f, _slideOutSeconds));
                    if (_time >= _slideOutSeconds) Finish();
                    break;
            }
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
            if (_finished || this == null)
            {
                handle?.Dispose();
                return;
            }
            _banner = handle;
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
            _banner?.Dispose();
            _banner = null;
            RestoreMainLayer();
            Action callback = _onFinished;
            _onFinished = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            _banner?.Dispose();
            _banner = null;
            RestoreMainLayer();
        }
    }
}
