using System.Collections.Generic;
using Shenxiao.Generated.UI.ListDuobao;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListGoodsItem : ListGoodsItemBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
        }

        public void SetData(IReadOnlyList<ListDuobaoConfigs.RewardEntry> rewards)
        {
            Clear();
            if (_tpl_BaseAwardItem == null || _gp_item == null || rewards == null) return;
            for (int i = 0; i < rewards.Count; i++)
            {
                ListDuobaoConfigs.RewardEntry reward = rewards[i];
                GameObject go = Instantiate(_tpl_BaseAwardItem, _gp_item);
                go.SetActive(true);
                BaseAwardItem item = go.GetComponent<BaseAwardItem>();
                if (item != null) item.SetData(reward.GoodsId, reward.Num, reward.Type > 0);
                _cells.Add(go);
            }
        }

        protected override void OnDispose() => Clear();

        private void Clear()
        {
            for (int i = 0; i < _cells.Count; i++) if (_cells[i] != null) Destroy(_cells[i]);
            _cells.Clear();
        }
    }
}
