using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.BossMystery;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossMystery
{
    public sealed class BossMysteryRewardView : BossMysteryRewardViewBind
    {
        private readonly List<BossMysteryRewardItem> _items = new List<BossMysteryRewardItem>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_img_close != null) Shenxiao.Framework.UI.UIUtil.AddClick(_img_close, Hide);
            if (_Label1 != null) _Label1.text = "击杀指定数量的首领可领取奖励(4点重置)";
            if (_tpl_BossMysteryRewardItem != null) _tpl_BossMysteryRewardItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            KfBossController.Instance.RequestGreatDemonRewardState();
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();

        protected override void OnDispose() => Unsubscribe();

        public void Refresh()
        {
            if (_tpl_BossMysteryRewardItem == null || _Scroller1 == null || _Scroller1.content == null) return;
            int[] thresholds = { 1, 3, 6 };
            for (int i = 0; i < thresholds.Length; i++)
            {
                BossMysteryRewardItem item = GetOrCreate(i);
                if (item == null) break;
                item.gameObject.SetActive(true);
                item.SetData(i + 1, thresholds[i]);
            }
        }

        private BossMysteryRewardItem GetOrCreate(int index)
        {
            if (index < _items.Count) return _items[index];
            GameObject go = Instantiate(_tpl_BossMysteryRewardItem, _Scroller1.content);
            go.name = "BossMysteryRewardItem_" + index;
            go.SetActive(true);
            BossMysteryRewardItem item = go.GetComponent<BossMysteryRewardItem>();
            if (item == null)
            {
                Destroy(go);
                return null;
            }
            item.Show();
            _items.Add(item);
            return item;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_KFBOSS_GREAT_DEMON_REWARD_UPDATE, Refresh);
            _subscribed = false;
        }
    }
}
