using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Boss;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossMain
{
    /// <summary>Boss 场景伤害榜。当前协议模型可确定提供前三名和自身伤害；协助归属仍由场景运行时补齐。</summary>
    public sealed class BossDamageItemView : BossDamageItemBind
    {
        private readonly List<BossDamageSubItemView> _rows = new List<BossDamageSubItemView>();
        private GameObject _itemTemplate;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_img_arrow != null) UIUtil.AddClick(_img_arrow, Hide);
            if (_itemTemplate != null) _itemTemplate.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        public void SetItemTemplate(GameObject template)
        {
            _itemTemplate = template;
            if (_itemTemplate != null) _itemTemplate.SetActive(false);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_BOSS_DAMAGE_RANK_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_BOSS_DAMAGE_RANK_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            IReadOnlyList<BossModel.DamageRankEntry> entries = BossModel.Instance.DamageTop3;
            for (int i = 0; i < entries.Count; i++)
            {
                BossDamageSubItemView row = GetOrCreateRow(i);
                if (row == null) break;
                row.Show();
                row.SetData(i + 1, entries[i].RoleName, entries[i].Damage);
            }
            for (int i = entries.Count; i < _rows.Count; i++) _rows[i].Hide();

            BossModel.DamageRankSelf self = BossModel.Instance.DamageSelf;
            if (_box_mine != null) _box_mine.gameObject.SetActive(self.HasData);
            if (!self.HasData) return;
            if (_lb_name != null) _lb_name.text = self.SelfName ?? string.Empty;
            if (_lb_damage != null) _lb_damage.text = self.SelfDamage.ToString("N0");
            bool medalRank = self.SelfRank > 0 && self.SelfRank <= 3;
            if (_img_rank != null) _img_rank.gameObject.SetActive(medalRank);
            if (_lb_rank != null)
            {
                _lb_rank.gameObject.SetActive(!medalRank);
                _lb_rank.text = self.SelfRank > 0 ? self.SelfRank.ToString() : "--";
            }
        }

        private BossDamageSubItemView GetOrCreateRow(int index)
        {
            if (index < _rows.Count) return _rows[index];
            if (_itemTemplate == null || _list_item == null || _list_item.content == null) return null;
            GameObject go = Object.Instantiate(_itemTemplate, _list_item.content);
            go.name = "BossDamageSubItem_" + index;
            BossDamageSubItemView row = go.GetComponent<BossDamageSubItemView>();
            if (row == null)
            {
                Debug.LogError("[BossDamageItemView] BossDamageSubItem 模板尚未由业务 View 接管", go);
                Object.Destroy(go);
                return null;
            }
            _rows.Add(row);
            return row;
        }
    }
}
