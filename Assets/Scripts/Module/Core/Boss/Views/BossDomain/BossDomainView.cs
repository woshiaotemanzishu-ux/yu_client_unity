using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Bossdomain;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainView : BossDomainViewBind
    {
        private readonly List<BossDomainItem> _items = new List<BossDomainItem>();
        private KfBossModel.DecorationBossEntry _selected;
        private BossDomainBuyView _buyView;
        private BossDomainHelpAlert _helpAlert;
        private BossDomainStageShow _stageShow;
        private bool _assistMode;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_BossDomainItem != null) _tpl_BossDomainItem.SetActive(false);
            if (_tpl_BossDomainRewardItem != null) _tpl_BossDomainRewardItem.SetActive(false);
            if (_img_add != null) UIUtil.AddClick(_img_add, ShowBuyView);
            if (_gp_check_touch != null) UIUtil.AddClick(_gp_check_touch, ToggleAssistMode);
            if (attention != null) UIUtil.AddClick(attention, ToggleAttention);
            if (_btn_drop != null) UIUtil.AddClick(_btn_drop, KfBossController.Instance.RequestDecorationDropLog);
            if (_img_btn != null) UIUtil.AddClick(_img_btn, ShowStageRewards);
            if (_btn != null) UIUtil.AddClick(_btn, EnterSelectedBoss);

            Transform root = transform.root;
            _buyView = root.GetComponentInChildren<BossDomainBuyView>(true);
            _helpAlert = root.GetComponentInChildren<BossDomainHelpAlert>(true);
            _stageShow = root.GetComponentInChildren<BossDomainStageShow>(true);
            if (_buyView != null) _buyView.Hide();
            if (_helpAlert != null) _helpAlert.Hide();
            if (_stageShow != null) _stageShow.Hide();
        }

        protected override async void OnShow(object args)
        {
            Subscribe();
            await BossDomainFlow.PrepareAsync();
            if (!this || !IsShown) return;
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            if (_buyView != null) _buyView.Hide();
            if (_helpAlert != null) _helpAlert.Hide();
            if (_stageShow != null) _stageShow.Hide();
        }

        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_KFBOSS_DECORATION_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            KfBossModel model = KfBossModel.Instance;
            List<KfBossModel.DecorationBossEntry> source = new List<KfBossModel.DecorationBossEntry>(model.DecorationBossList);
            source.Sort((a, b) => a.BossId.CompareTo(b.BossId));
            for (int i = 0; i < _items.Count; i++) _items[i].gameObject.SetActive(false);
            for (int i = 0; i < source.Count; i++)
            {
                BossDomainItem item = GetOrCreate(i);
                if (item == null) break;
                item.gameObject.SetActive(true);
                KfBossModel.DecorationBossEntry entry = source[i];
                item.SetData(entry, _selected != null && _selected.BossId == entry.BossId, () => Select(entry));
            }
            if ((_selected == null || source.Find(e => e.BossId == _selected.BossId) == null) && source.Count > 0)
                Select(source[0]);

            if (_lb_time != null) _lb_time.text = model.DecorationCount.ToString();
            if (_lb_help != null) _lb_help.text = model.DecorationAssistCount.ToString();
        }

        private BossDomainItem GetOrCreate(int index)
        {
            if (index < _items.Count) return _items[index];
            if (_tpl_BossDomainItem == null || Content == null) return null;
            GameObject go = Instantiate(_tpl_BossDomainItem, Content);
            go.name = "BossDomainItem_" + index;
            go.SetActive(true);
            BossDomainItem item = go.GetComponent<BossDomainItem>();
            if (item == null)
            {
                Destroy(go);
                return null;
            }
            item.Show();
            _items.Add(item);
            return item;
        }

        private void Select(KfBossModel.DecorationBossEntry entry)
        {
            _selected = entry;
            for (int i = 0; i < _items.Count; i++) _items[i].SetSelected(_items[i].BossId == entry.BossId);
            if (attention != null) attention.gameObject.SetActive(!KfBossModel.Instance.IsDecorationUnfollowed(entry.BossId));
        }

        private void ToggleAttention()
        {
            if (_selected == null) return;
            bool follow = KfBossModel.Instance.IsDecorationUnfollowed(_selected.BossId);
            KfBossController.Instance.SetDecorationFollowReq(_selected.BossId, follow);
        }

        private void ToggleAssistMode()
        {
            if (_assistMode)
            {
                SetAssistMode(false);
                return;
            }
            if (_helpAlert != null) _helpAlert.ShowAssistConfirm(() => SetAssistMode(true));
        }

        private void SetAssistMode(bool enabled)
        {
            _assistMode = enabled;
            if (_gp_check_touch != null) _gp_check_touch.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.65f);
        }

        private void ShowBuyView()
        {
            if (_buyView != null) _buyView.Show();
        }

        private void ShowStageRewards()
        {
            if (_stageShow != null) _stageShow.Show();
        }

        private void EnterSelectedBoss()
        {
            if (_selected == null) return;
            KfBossModel model = KfBossModel.Instance;
            int remaining = _assistMode ? model.DecorationAssistCount : model.DecorationCount;
            if (remaining <= 0) return;
            KfBossController.Instance.EnterDecorationBoss(_selected.BossId, _assistMode ? 2 : 1);
        }
    }
}
