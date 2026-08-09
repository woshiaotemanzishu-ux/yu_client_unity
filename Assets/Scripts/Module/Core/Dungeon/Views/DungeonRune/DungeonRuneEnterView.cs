using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.DungeonRune;
using UnityEngine;

namespace Shenxiao.Module.Core.Dungeon.Views.DungeonRune
{
    /// <summary>只消费 61020、61113、61115 已落地权威字段；缺 rec_data/首杀/解锁/奖励配置时不推断。</summary>
    public sealed class DungeonRuneEnterView : DungeonRuneEnterViewBind
    {
        private readonly List<GameObject> _items = new List<GameObject>();
        private DungeonRuneTargetView _targetView;
        private DungeonRuneDailyRewardView _dailyView;
        private bool _subscribed;

        protected override void OnInit()
        {
            Transform moduleRoot = transform.parent;
            _targetView = moduleRoot != null ? moduleRoot.GetComponentInChildren<DungeonRuneTargetView>(true) : null;
            _dailyView = moduleRoot != null ? moduleRoot.GetComponentInChildren<DungeonRuneDailyRewardView>(true) : null;
            if (_gp_target != null) UIUtil.AddClick(_gp_target, ShowTargetView);
            if (_gp_daily_reward != null) UIUtil.AddClick(_gp_daily_reward, ShowDailyView);
            if (_tpl_DungeonRuneEnterItem != null) _tpl_DungeonRuneEnterItem.SetActive(false);
            if (_tpl_DungeonRuneEnterBgItem != null) _tpl_DungeonRuneEnterBgItem.SetActive(false);
            if (_tpl_DungeonRuneEnterLineItem != null) _tpl_DungeonRuneEnterLineItem.SetActive(false);
            if (_tpl_CommonRewardItem != null) _tpl_CommonRewardItem.SetActive(false);
            if (_tpl_GiftPushIcon != null) _tpl_GiftPushIcon.SetActive(false);
            if (_targetView != null) _targetView.Hide();
            if (_dailyView != null) _dailyView.Hide();
        }

        protected override async void OnShow(object args)
        {
            Subscribe();
            DungeonController.Instance.RequestState(DungeonModel.TYPE_RUNE);
            DungeonController.Instance.RequestDungeonRuneRewardInfo(DungeonModel.TYPE_RUNE);
            DungeonController.Instance.RequestDungeonRuneDailyStatus();
            await DungeonConfigs.EnsureLoaded();
            if (!this || !IsShown) return;
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearItems();
            if (_targetView != null) _targetView.Hide();
            if (_dailyView != null) _dailyView.Hide();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearItems();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DUNGEON_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DUNGEON_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            if (!IsShown) return;
            BuildDungeonList();
            DungeonModel model = DungeonModel.Instance;
            bool rewardRed = false;
            if (model.TryGetDungeonRuneRewardInfo(DungeonModel.TYPE_RUNE, out DungeonModel.RuneRewardSnapshot rewards) && rewards != null)
                foreach (DungeonModel.RuneRewardEntry entry in rewards.Entries)
                    if (entry.RewardStatus == 0) { rewardRed = true; break; }
            SetActive(_img_target_red, rewardRed);
            SetActive(_img_daily_reward_red, model.RuneDailyStatus != null && model.RuneDailyStatus.DailyStatus == 1);
            SetActive(_img_first_red, false);
            SetActive(_gp_target, true);
            SetActive(_gp_daily_reward, true);
            SetActive(_gp_first, false);
            SetActive(_gp_unlock, false);
            SetActive(_gp_rec_fight, false);
            SetActive(_gp_challenge, false);
            SetActive(_gp_reward, false);
            SetActive(_lb_done, false);
            SetActive(giftIcon, false);
        }

        private void BuildDungeonList()
        {
            ClearItems();
            SetActive(_list_bg, false);
            SetActive(_list_line, false);
            if (_list_item == null || _list_item.content == null || _tpl_DungeonRuneEnterItem == null)
            {
                GameLog.Error("Dungeon", "DungeonRuneEnterView list/content/template missing");
                return;
            }
            if (!DungeonModel.Instance.DunStatesByType.TryGetValue(DungeonModel.TYPE_RUNE, out List<DungeonModel.DunState> states) || states == null) return;
            foreach (DungeonModel.DunState state in states)
            {
                GameObject itemGo = Object.Instantiate(_tpl_DungeonRuneEnterItem, _list_item.content);
                DungeonRuneEnterItem item = itemGo.GetComponent<DungeonRuneEnterItem>();
                if (item == null)
                {
                    GameLog.Error("Dungeon", "DungeonRuneEnterItem template missing business component; item skipped");
                    Object.Destroy(itemGo);
                    continue;
                }
                itemGo.SetActive(true);
                item.Show();
                item.SetData(state);
                _items.Add(itemGo);
            }
        }

        private void ClearItems()
        {
            foreach (GameObject item in _items) if (item != null) Object.Destroy(item);
            _items.Clear();
        }

        private void ShowTargetView()
        {
            if (_targetView == null) { GameLog.Error("Dungeon", "DungeonRuneTargetView business component missing"); return; }
            _targetView.Show();
            _targetView.transform.SetAsLastSibling();
        }

        private void ShowDailyView()
        {
            if (_dailyView == null) { GameLog.Error("Dungeon", "DungeonRuneDailyRewardView business component missing"); return; }
            DungeonModel.RuneDailyStatusSnapshot status = DungeonModel.Instance.RuneDailyStatus;
            if (status == null)
            {
                DungeonController.Instance.RequestDungeonRuneDailyStatus();
                TipsManager.Toast("每日奖励状态尚未加载");
                return;
            }
            switch (status.DailyStatus)
            {
                case 0:
                    TipsManager.Toast("暂无奖励可以领取");
                    return;
                case 1:
                    _dailyView.Show();
                    _dailyView.transform.SetAsLastSibling();
                    return;
                case 2:
                    TipsManager.Toast("每日奖励已领取，请明天再来吧!");
                    return;
                default:
                    GameLog.Error("Dungeon", "unknown rune daily reward status: {0}", status.DailyStatus);
                    return;
            }
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
