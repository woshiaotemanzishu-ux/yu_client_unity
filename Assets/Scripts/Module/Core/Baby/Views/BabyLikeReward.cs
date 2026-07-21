using System.Collections.Generic;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyLikeReward : BabyLikeRewardBind
    {
        private TextMeshProUGUI _lb;
        private Transform _content;
        private GameObject _itemTemplate;
        private readonly List<GameObject> _items = new List<GameObject>();

        public void SetData(BabyPraiseConfigs.PraiseCfg cfg)
        {
            CacheNodes();
            ClearItems();
            if (_lb != null) _lb.text = cfg == null ? string.Empty : cfg.Rank1 == cfg.Rank2 ? "第" + cfg.Rank1 + "名" : "第" + cfg.Rank1 + "-" + cfg.Rank2 + "名";
            if (cfg == null || _content == null || _itemTemplate == null) return;
            for (int i = 0; i < cfg.Rewards.Count; i++)
            {
                GameObject go = Instantiate(_itemTemplate, _content);
                go.SetActive(true);
                BaseAwardItem item = go.GetComponent<BaseAwardItem>();
                if (item == null) { DestroyItem(go); continue; }
                BabyPraiseConfigs.RewardItem reward = cfg.Rewards[i];
                if (reward.TypeId > 0 && reward.TypeId <= int.MaxValue) item.SetData((int)reward.TypeId, reward.Num);
                _items.Add(go);
            }
        }

        private void CacheNodes()
        {
            if (_content != null) return;
            if (_Scroller1 != null) _content = _Scroller1.content;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (_content == null && nodes[i].name == "Content") _content = nodes[i];
                if (nodes[i].name == "lb") _lb = nodes[i].GetComponent<TextMeshProUGUI>();
                else if (nodes[i].name == "BaseAwardItem" && nodes[i].parent != null && nodes[i].parent.name == "__Templates") _itemTemplate = nodes[i].gameObject;
            }
        }

        private void ClearItems() { for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]); _items.Clear(); }
        private static void DestroyItem(GameObject item) { if (item == null) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
    }
}
