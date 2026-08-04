using System.Globalization;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.OnHook;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面挂机效率入口。显示取决于配置开放等级、功能前置任务、当前场景和 13212/13215 下发值。
    /// </summary>
    public sealed class MainUIOnHookView : MainUIOnHookViewBind
    {
        public const string AUTO_FIGHTING_EFFECT_SLOT_ID = "mainui_onhook_auto_fighting";
        public const string AUTO_PATHING_EFFECT_SLOT_ID = "mainui_onhook_auto_pathing";
        public const string BOOST_HINT_EFFECT_SLOT_ID = "mainui_onhook_boost_hint";

        private int _refreshVersion;
        private UIEffectSlot _autoFightingEffectSlot;
        private UIEffectSlot _autoPathingEffectSlot;
        private UIEffectSlot _boostHintEffectSlot;
        private UIEffectStage.Handle _autoStateEffect;
        private string _autoStateEffectSlotId;
        private int _autoStateEffectVersion;
        private UIEffectStage.Handle _boostHintEffect;
        private bool _boostHintEffectRequested;
        private int _boostHintEffectVersion;

        protected override void OnInit()
        {
            ResolveEffectSlots();
            if (_box_outline_exp != null) _box_outline_exp.gameObject.SetActive(false);
            if (_box_old_outline_exp != null) _box_old_outline_exp.gameObject.SetActive(false);

            RouteClick(_box_outline_exp, "onhook");
            RouteClick(_box_exp_btn, "onhook");
            RouteClick(_box_old_outline_exp, "onhook");
            if (add_btn != null) UIUtil.AddClick(add_btn, OpenAddition);
        }

        protected override void OnShow(object args)
        {
            OnHookModel.Instance.Changed += Refresh;
            GoodsBuffModel.Instance.Changed += Refresh;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, Refresh);
            EventDispatcher.On<bool>(GlobalEvent.EVT_AUTO_FIGHT_STATE, OnAutoStateChanged);
            EventDispatcher.On<bool>(GlobalEvent.EVT_AUTO_FIND_WAY_STATE, OnAutoStateChanged);
            ApplyAutoStateEffect();
            Refresh();
        }

        protected override void OnHide()
        {
            ++_refreshVersion;
            OnHookModel.Instance.Changed -= Refresh;
            GoodsBuffModel.Instance.Changed -= Refresh;
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_MAP_READY, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, Refresh);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_AUTO_FIGHT_STATE, OnAutoStateChanged);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_AUTO_FIND_WAY_STATE, OnAutoStateChanged);
            ClearAutoStateEffect();
            ClearBoostHintEffect();
        }

        private void ResolveEffectSlots()
        {
            UIEffectSlot[] slots = GetComponentsInChildren<UIEffectSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                UIEffectSlot slot = slots[i];
                if (slot == null) continue;
                if (slot.SlotId == AUTO_FIGHTING_EFFECT_SLOT_ID) _autoFightingEffectSlot = slot;
                else if (slot.SlotId == AUTO_PATHING_EFFECT_SLOT_ID) _autoPathingEffectSlot = slot;
                else if (slot.SlotId == BOOST_HINT_EFFECT_SLOT_ID) _boostHintEffectSlot = slot;
            }

            if (_autoFightingEffectSlot == null || _autoPathingEffectSlot == null)
                GameLog.Error("MainUI", "MainUIOnHookView 缺自动战斗/寻路特效槽,请重新生成 HudOnHook");
            if (_boostHintEffectSlot == null)
                GameLog.Error("MainUI", "MainUIOnHookView 缺挂机提升扫光特效槽,请修复 HudOnHook");
        }

        private void OnAutoStateChanged(bool ignored)
        {
            ApplyAutoStateEffect();
        }

        private void ApplyAutoStateEffect()
        {
            AutoFightModel model = AutoFightModel.Instance;
            UIEffectSlot target = model.AutoFindWayState
                ? _autoPathingEffectSlot
                : model.AutoFightState ? _autoFightingEffectSlot : null;
            string targetSlotId = target != null ? target.SlotId : null;
            if (_autoStateEffectSlotId == targetSlotId) return;

            _autoStateEffectSlotId = targetSlotId;
            int version = ++_autoStateEffectVersion;
            DisposeAutoStateEffectHandle();
            if (target != null) _ = LoadAutoStateEffectAsync(target, version);
        }

        private async Task LoadAutoStateEffectAsync(UIEffectSlot slot, int version)
        {
            RectTransform host = slot != null ? slot.transform.parent as RectTransform : null;
            UIEffectStage.Handle handle = host != null ? await UIEffectStage.AddAsync(slot, host) : null;
            if (this == null || version != _autoStateEffectVersion || slot == null || _autoStateEffectSlotId != slot.SlotId)
            {
                handle?.Dispose();
                return;
            }

            _autoStateEffect = handle;
            if (handle == null) _autoStateEffectSlotId = null;
        }

        private void ClearAutoStateEffect()
        {
            _autoStateEffectVersion++;
            _autoStateEffectSlotId = null;
            DisposeAutoStateEffectHandle();
        }

        private void DisposeAutoStateEffectHandle()
        {
            if (_autoStateEffect == null) return;
            _autoStateEffect.Dispose();
            _autoStateEffect = null;
        }

        private void ApplyBoostHintEffect(bool shouldShow)
        {
            if (_boostHintEffectRequested == shouldShow) return;

            _boostHintEffectRequested = shouldShow;
            int version = ++_boostHintEffectVersion;
            DisposeBoostHintEffectHandle();
            if (!shouldShow) return;
            if (_boostHintEffectSlot == null)
            {
                _boostHintEffectRequested = false;
                return;
            }
            _ = LoadBoostHintEffectAsync(_boostHintEffectSlot, version);
        }

        private async Task LoadBoostHintEffectAsync(UIEffectSlot slot, int version)
        {
            RectTransform host = slot != null ? slot.transform.parent as RectTransform : null;
            UIEffectStage.Handle handle = host != null ? await UIEffectStage.AddAsync(slot, host) : null;
            if (this == null || version != _boostHintEffectVersion || !_boostHintEffectRequested)
            {
                handle?.Dispose();
                return;
            }

            _boostHintEffect = handle;
            if (handle == null) _boostHintEffectRequested = false;
        }

        private void ClearBoostHintEffect()
        {
            _boostHintEffectVersion++;
            _boostHintEffectRequested = false;
            DisposeBoostHintEffectHandle();
        }

        private void DisposeBoostHintEffectHandle()
        {
            if (_boostHintEffect == null) return;
            _boostHintEffect.Dispose();
            _boostHintEffect = null;
        }

        private async void Refresh()
        {
            int version = ++_refreshVersion;
            await System.Threading.Tasks.Task.WhenAll(
                MainUIConfigs.EnsureSceneLoaded(),
                FuncOpenConfig.EnsureLoaded(),
                OnHookConfigs.EnsureLoaded());

            if (this == null || !IsShown || version != _refreshVersion) return;

            OnHookModel model = OnHookModel.Instance;
            bool hasOpenLevel = OnHookConfigs.TryGetOpenLevel(out int openLevel);
            bool baseVisible = hasOpenLevel
                               && RoleModel.Instance.Level >= openLevel
                               && MainUIConfigs.IsFieldScene(RoleModel.Instance.SceneId)
                               && model.ExpEffect > 0;
            bool standardVisible = baseVisible
                                   && FuncOpenConfig.IsLoaded
                                   && FuncOpenConfig.CheckFuncOpenState("OnHookMainView");

            if (_box_outline_exp != null) _box_outline_exp.gameObject.SetActive(standardVisible);
            if (_box_old_outline_exp != null) _box_old_outline_exp.gameObject.SetActive(baseVisible && !standardVisible);
            if (!baseVisible)
            {
                ApplyBoostHintEffect(false);
                return;
            }

            if (_lb_outline_exp != null)
            {
                _lb_outline_exp.text = "<color=#00fa64>" + FormatExpRate(model.ExpEffect) + "</color>经验/分";
            }
            if (_lb_old_outline_exp != null)
            {
                _lb_old_outline_exp.text = FormatExpRate(model.ExpEffect) + "经验/分";
            }

            bool maxed = model.HasExpAdditions
                         && OnHookConfigs.TryGetMaxAdditionRatio(out long maxRatio)
                         && SumAdditionRatio(model) >= maxRatio;
            bool hasExperienceBuff = HasExperienceBuff();
            if (add_btn != null) add_btn.gameObject.SetActive(!maxed);
            if (_img_add != null) _img_add.gameObject.SetActive(!maxed && hasExperienceBuff);
            ApplyBoostHintEffect(ShouldShowBoostHint(maxed, hasExperienceBuff));
        }

        private static bool ShouldShowBoostHint(bool maxed, bool hasExperienceBuff)
        {
            return !maxed && !hasExperienceBuff;
        }

        private static long SumAdditionRatio(OnHookModel model)
        {
            long total = 0;
            for (int i = 0; i < model.ExpAdditions.Count; i++)
            {
                total += model.ExpAdditions[i].Ratio;
            }
            return total;
        }

        /// <summary>
        /// 对标老端 WordManager.ConvertNum(value, true)。13212/13215 的模型值保持原始整数，
        /// 万/亿/万亿换算只发生在显示层。
        /// </summary>
        private static string FormatExpRate(long value)
        {
            if (value <= 9999L) return value.ToString(CultureInfo.InvariantCulture);
            if (value < 100000000L)
                return (value / 10000d).ToString("0.00", CultureInfo.InvariantCulture) + "万";
            if (value < 1000000000000L)
                return (value / 100000000d).ToString("0.00", CultureInfo.InvariantCulture) + "亿";
            return (value / 1000000000000d).ToString("0.00", CultureInfo.InvariantCulture) + "万亿";
        }

        private static bool HasExperienceBuff()
        {
            for (int i = 0; i < GoodsBuffModel.Instance.List.Count; i++)
            {
                if (GoodsBuffModel.Instance.List[i].BuffType == 1) return true;
            }
            return false;
        }

        private static void OpenAddition()
        {
            OnHookController.Instance.RequestExpAdditions();
            MainUIRouter.Open("onhook_addition");
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target != null) UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }
    }
}
