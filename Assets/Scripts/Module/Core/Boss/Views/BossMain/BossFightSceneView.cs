using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Boss;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossMain
{
    /// <summary>
    /// 老端 BossFightSceneView 的最小可确定接管：伤害榜显隐、免战倒计时只读显示，
    /// 以及与老端一致的 5 秒 Boss 血量查询节奏。真实场景动作由场景运行时接入后补齐。
    /// </summary>
    public sealed class BossFightSceneView : BossFightSceneViewBind
    {
        private BossDamageItemView _damageView;
        private float _nextHpPollAt;
        private float _warFreeObservedAt;
        private long _warFreeObservedSeconds;
        private long _lastRenderedWarFreeSeconds = -1;
        private bool _subscribed;

        protected override void OnInit()
        {
            _damageView = _tpl_BossDamageItem != null
                ? _tpl_BossDamageItem.GetComponent<BossDamageItemView>()
                : null;
            if (_damageView != null)
            {
                _damageView.SetItemTemplate(_tpl_BossDamageSubItem);
                _damageView.Hide();
            }

            if (_img_rank != null) UIUtil.AddClick(_img_rank, ShowDamagePanel);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            BossMainFlow.RequestReadonlySnapshot();
            _nextHpPollAt = Time.unscaledTime + BossMainFlow.BossHpPollIntervalSeconds;
            CaptureWarFreeCountdown();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            if (_damageView != null) _damageView.Hide();
        }

        protected override void OnDispose() => Unsubscribe();

        private void Update()
        {
            if (!IsShown) return;
            if (Time.unscaledTime >= _nextHpPollAt)
            {
                _nextHpPollAt = Time.unscaledTime + BossMainFlow.BossHpPollIntervalSeconds;
                BossMainFlow.RequestBossHp();
            }
            RenderWarFreeCountdown();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE, CaptureWarFreeCountdown);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BOSS_WAR_FREE_UPDATE, CaptureWarFreeCountdown);
            _subscribed = false;
        }

        private void ShowDamagePanel()
        {
            if (_damageView != null) _damageView.Show();
        }

        private void CaptureWarFreeCountdown()
        {
            _warFreeObservedSeconds = BossModel.Instance.WarFreeEndTimeLeft;
            _warFreeObservedAt = Time.unscaledTime;
            _lastRenderedWarFreeSeconds = -1;
            RenderWarFreeCountdown();
        }

        private void RenderWarFreeCountdown()
        {
            long left = _warFreeObservedSeconds <= 0
                ? 0
                : Mathf.Max(0, Mathf.CeilToInt(_warFreeObservedSeconds - (Time.unscaledTime - _warFreeObservedAt)));
            if (left == _lastRenderedWarFreeSeconds) return;
            _lastRenderedWarFreeSeconds = left;
            if (war_free_time_lb == null) return;
            war_free_time_lb.gameObject.SetActive(left > 0);
            war_free_time_lb.text = left > 0 ? FormatDuration(left) : string.Empty;
        }

        private static string FormatDuration(long seconds)
        {
            long hours = seconds / 3600;
            long minutes = seconds % 3600 / 60;
            long secs = seconds % 60;
            return hours > 0 ? $"{hours:00}:{minutes:00}:{secs:00}" : $"{minutes:00}:{secs:00}";
        }
    }
}
