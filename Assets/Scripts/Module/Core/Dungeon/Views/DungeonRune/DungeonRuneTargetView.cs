using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.DungeonRune;
using UnityEngine;

namespace Shenxiao.Module.Core.Dungeon.Views.DungeonRune
{
    public sealed class DungeonRuneTargetView : DungeonRuneTargetViewBind
    {
        private readonly List<GameObject> _items = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_img_close != null) UIUtil.AddClick(_img_close, Hide);
            if (_tpl_DungeonRuneTargetItem != null) _tpl_DungeonRuneTargetItem.SetActive(false);
            if (_lb_title != null) _lb_title.text = "层数奖励";
            if (_gp_get != null) _gp_get.gameObject.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DungeonController.Instance.RequestDungeonRuneRewardInfo(DungeonModel.TYPE_RUNE);
            Refresh();
        }

        protected override void OnHide() { Unsubscribe(); ClearItems(); }
        protected override void OnDispose() { Unsubscribe(); ClearItems(); }

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
            ClearItems();
            if (_list_item == null || _list_item.content == null || _tpl_DungeonRuneTargetItem == null) return;
            if (!DungeonModel.Instance.TryGetDungeonRuneRewardInfo(DungeonModel.TYPE_RUNE, out DungeonModel.RuneRewardSnapshot snapshot) || snapshot == null) return;
            foreach (DungeonModel.RuneRewardEntry entry in snapshot.Entries)
            {
                GameObject itemGo = Object.Instantiate(_tpl_DungeonRuneTargetItem, _list_item.content);
                DungeonRuneTargetItem item = itemGo.GetComponent<DungeonRuneTargetItem>();
                if (item == null)
                {
                    GameLog.Error("Dungeon", "DungeonRuneTargetItem template missing business component; item skipped");
                    Object.Destroy(itemGo);
                    continue;
                }
                itemGo.SetActive(true);
                item.Show();
                item.SetData(entry);
                _items.Add(itemGo);
            }
        }

        private void ClearItems()
        {
            foreach (GameObject item in _items) if (item != null) Object.Destroy(item);
            _items.Clear();
        }
    }
}
