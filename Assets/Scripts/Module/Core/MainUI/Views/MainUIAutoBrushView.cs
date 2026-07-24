using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 自动闯关入口(对标老客户端 MainUIAutoBrushView.ts)。
    /// 入口显隐由 FuncOpenConfig["AutoBrush"] 与 config_scene.type==1 共同决定;
    /// 进度完成特效由 Creator 中的 UIEffectSlot 提供资源配置,View 只消费状态。
    /// </summary>
    public sealed class MainUIAutoBrushView : MainUIAutoBrushViewBind
    {
        public const string CHALLENGE_EFFECT_SLOT_ID = "autobrush_challenge";

        private float _progressWidth;
        private bool _clickBound;
        private bool _openState;
        private bool _openStateLoading;
        private int _openStateVersion;
        private CanvasGroup _canvasGroup;
        private UIEffectSlot _challengeEffectSlot;
        private UIEffectStage.Handle _challengeEffect;
        private bool _challengeEffectLoading;
        private bool _challengeEffectShouldShow;
        private int _challengeEffectVersion;

        protected override void OnInit()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _challengeEffectSlot = FindChallengeEffectSlot();
            _progressWidth = _img_progress.rectTransform.sizeDelta.x;
            _img_red.gameObject.SetActive(false);
            _img_red2.gameObject.SetActive(false);
            _box_effect.gameObject.SetActive(false);
            SetDisplayVisible(false);
            BindClicks();

            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED, RefreshBrushInfo);
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, RefreshLevel);
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED, RefreshAutoState);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshOpenState);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshOpenState);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, RefreshOpenState);
            RefreshAll();
            RefreshOpenState();
        }

        protected override void OnShow(object args)
        {
            RefreshAll();
            RefreshOpenState();
        }

        protected override void OnHide()
        {
            ClearChallengeEffect();
        }

        protected override void OnDispose()
        {
            DisposeRuntime();
        }

        private void OnDestroy()
        {
            DisposeRuntime();
        }

        private void UnbindEvents()
        {
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_INFO_UPDATED, RefreshBrushInfo);
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, RefreshLevel);
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_STATE_UPDATED, RefreshAutoState);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshOpenState);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshOpenState);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_MAP_READY, RefreshOpenState);
        }

        private void RefreshAll()
        {
            RefreshAutoState();
            RefreshBrushInfo();
            RefreshLevel();
        }

        private void RefreshAutoState()
        {
            bool state = AutoBrushModel.Instance.AutoBrushState;
            _lb_auto_level.text = state ? "取消自动" : "自动闯关";
        }

        private void RefreshBrushInfo()
        {
            AutoBrushModel.BrushStrangeInfo info = AutoBrushModel.Instance.BrushInfo;
            if (info == null || info.NeedTimes <= 0)
            {
                SetProgressWidth(0f);
                SetChallengeEffectVisible(false);
                return;
            }

            float percent = Mathf.Clamp01((float)info.CurrentTimes / info.NeedTimes);
            SetProgressWidth(_progressWidth * percent);
            SetChallengeEffectVisible(_openState && info.CurrentTimes == info.NeedTimes);
        }

        private void RefreshLevel()
        {
            AutoBrushModel model = AutoBrushModel.Instance;
            if (model.CheckDoneState())
            {
                _lb_level.text = "";
                return;
            }

            _lb_level.text = "第" + (model.Level + 1) + "关";
        }

        private void SetProgressWidth(float width)
        {
            Vector2 size = _img_progress.rectTransform.sizeDelta;
            size.x = Mathf.Max(0f, width);
            _img_progress.rectTransform.sizeDelta = size;
        }

        private void RefreshOpenState()
        {
            if (FuncOpenConfig.IsLoaded && MainUIConfigs.IsSceneLoaded)
            {
                ApplyOpenState();
                return;
            }

            SetOpenState(false);
            if (_openStateLoading) return;
            _openStateLoading = true;
            int version = ++_openStateVersion;
            _ = LoadOpenStateAsync(version);
        }

        private async Task LoadOpenStateAsync(int version)
        {
            await Task.WhenAll(FuncOpenConfig.EnsureLoaded(), MainUIConfigs.EnsureSceneLoaded());
            if (this == null || version != _openStateVersion) return;

            _openStateLoading = false;
            if (!FuncOpenConfig.IsLoaded || !MainUIConfigs.IsSceneLoaded)
            {
                SetOpenState(false);
                return;
            }
            ApplyOpenState();
        }

        private void ApplyOpenState()
        {
            bool open = FuncOpenConfig.CheckFuncOpenState("AutoBrush")
                && MainUIConfigs.IsFieldScene(RoleModel.Instance.SceneId);
            SetOpenState(open);
        }

        private void SetOpenState(bool open)
        {
            bool changed = _openState != open;
            _openState = open;
            SetDisplayVisible(open);
            if (!open)
            {
                SetChallengeEffectVisible(false);
                return;
            }
            if (changed) RefreshAll();
        }

        private void SetDisplayVisible(bool visible)
        {
            if (_canvasGroup == null)
            {
                GameLog.Error("MainUI", "MainUIAutoBrushView 缺 CanvasGroup,请重新生成 HudAutoBrush");
                return;
            }
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private UIEffectSlot FindChallengeEffectSlot()
        {
            UIEffectSlot[] slots = GetComponentsInChildren<UIEffectSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                UIEffectSlot slot = slots[i];
                if (slot != null && slot.SlotId == CHALLENGE_EFFECT_SLOT_ID) return slot;
            }

            GameLog.Error("MainUI", "MainUIAutoBrushView 缺斩妖完成特效槽,请重新生成 HudAutoBrush");
            return null;
        }

        private void SetChallengeEffectVisible(bool visible)
        {
            _challengeEffectShouldShow = visible;
            if (!visible)
            {
                ClearChallengeEffect();
                return;
            }
            if (_challengeEffectSlot == null || _challengeEffect != null || _challengeEffectLoading) return;

            _challengeEffectLoading = true;
            int version = ++_challengeEffectVersion;
            _ = LoadChallengeEffectAsync(version);
        }

        private async Task LoadChallengeEffectAsync(int version)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                _challengeEffectSlot,
                _challengeEffectSlot.transform as RectTransform);
            if (this == null || version != _challengeEffectVersion || !_challengeEffectShouldShow)
            {
                handle?.Dispose();
                return;
            }

            _challengeEffectLoading = false;
            _challengeEffect = handle;
        }

        private void ClearChallengeEffect()
        {
            _challengeEffectVersion++;
            _challengeEffectLoading = false;
            if (_challengeEffect == null) return;
            _challengeEffect.Dispose();
            _challengeEffect = null;
        }

        private void DisposeRuntime()
        {
            _openStateVersion++;
            _openStateLoading = false;
            _challengeEffectShouldShow = false;
            ClearChallengeEffect();
            UnbindEvents();
        }

        private void BindClicks()
        {
            if (_clickBound) return;
            BindRoute(click_gp, "autobrush");
            BindRoute(_box_auto_level, "autobrush_toggle");
            BindRoute(_img_auto_level, "autobrush_toggle");
            _clickBound = true;
        }

        private static void BindRoute(Component target, string viewKey)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }
    }
}
