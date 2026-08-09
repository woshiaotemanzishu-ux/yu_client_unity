using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossMystery;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossMystery
{
    public sealed class BossMysteryEnterView : BossMysteryEnterViewBind
    {
        private BossMysteryRoomView _room;
        private BossMysteryRewardView _rewardView;
        private BossModel.BossEntry _selected;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_btn_drop != null) UIUtil.AddClick(_btn_drop, KfBossController.Instance.RequestGreatDemonDropLog);
            if (_btn_reward != null) UIUtil.AddClick(_btn_reward, ShowRewardView);
            if (_gp_attention != null) UIUtil.AddClick(_gp_attention, ToggleAttention);
            if (switchBtn != null) UIUtil.AddClick(switchBtn, SelectNextBox);

            _room = _tpl_BossMysteryRoomView != null
                ? _tpl_BossMysteryRoomView.GetComponent<BossMysteryRoomView>()
                : GetComponentInChildren<BossMysteryRoomView>(true);
            _rewardView = GetComponentInChildren<BossMysteryRewardView>(true);
            if (_tpl_BossMysteryMonItem != null) _tpl_BossMysteryMonItem.SetActive(false);
            if (_room != null)
            {
                _room.SetItemTemplate(_tpl_BossMysteryMonItem);
                _room.SelectionChanged = SelectBoss;
            }
            if (_rewardView != null) _rewardView.Hide();
        }

        protected override async void OnShow(object args)
        {
            Subscribe();
            if (_room != null) _room.Show(args);
            await BossMysteryFlow.PrepareAsync();
            if (!this || !IsShown) return;
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            if (_room != null) _room.Hide();
            if (_rewardView != null) _rewardView.Hide();
        }

        protected override void OnDispose() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_BOSS_LIST_UPDATE, OnBossListUpdated);
            EventDispatcher.On<int, int>(GlobalEvent.EVT_BOSS_REMIND_UPDATE, OnRemindUpdated);
            EventDispatcher.On(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_BOX_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BOSS_LIST_UPDATE, OnBossListUpdated);
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_BOSS_REMIND_UPDATE, OnRemindUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_BOX_UPDATE, Refresh);
            _subscribed = false;
        }

        private void OnBossListUpdated(int bossType)
        {
            if (bossType == BossMysteryFlow.BossType) Refresh();
        }

        private void OnRemindUpdated(int bossType, int bossId)
        {
            if (bossType == BossMysteryFlow.BossType && _selected != null && _selected.BossId == bossId) Refresh();
        }

        private void Refresh()
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossMysteryFlow.BossType);
            if (_room != null) _room.RefreshFromModel();
            if (_selected == null && state != null && state.BossList.Count > 0) SelectBoss(state.BossList[0]);

            if (progress_num != null)
            {
                int kill = KfBossModel.Instance.GreatDemonKillNum;
                progress_num.text = kill + "/6";
            }
            if (red_dot != null) red_dot.gameObject.SetActive(HasClaimableReward());
        }

        private void SelectBoss(BossModel.BossEntry entry)
        {
            _selected = entry;
            if (entry == null) return;
            if (_lb_boss_name != null) _lb_boss_name.text = "Boss " + entry.BossId;
            if (attention != null) attention.gameObject.SetActive(entry.IsRemind);
            if (mon_con != null) mon_con.gameObject.SetActive(true);
            if (box_con != null) box_con.gameObject.SetActive(false);
            if (special_con != null) special_con.gameObject.SetActive(false);
            if (mon_num != null) mon_num.text = entry.IsAlive ? "首领已刷新" : "首领复活中";
        }

        private void ToggleAttention()
        {
            if (_selected == null) return;
            BossController.Instance.SetBossRemind(BossMysteryFlow.BossType, _selected.BossId, !_selected.IsRemind);
        }

        private void SelectNextBox()
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossMysteryFlow.BossType);
            if (state == null || state.BossList.Count < 2) return;
            int index = _selected == null ? -1 : state.BossList.IndexOf(_selected);
            SelectBoss(state.BossList[(index + 1 + state.BossList.Count) % state.BossList.Count]);
        }

        private void ShowRewardView()
        {
            if (_rewardView == null) return;
            _rewardView.Show();
        }

        private static bool HasClaimableReward()
        {
            KfBossModel model = KfBossModel.Instance;
            int[] thresholds = { 1, 3, 6 };
            for (int i = 0; i < thresholds.Length; i++)
                if (model.GreatDemonKillNum >= thresholds[i] && !model.GreatDemonHadRewardStages.Contains(i + 1)) return true;
            return false;
        }
    }
}
