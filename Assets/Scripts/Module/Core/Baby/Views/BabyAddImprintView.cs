using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>铭刻材料候选列表骨架；只回传本地点击，不自行开关窗口或发协议。</summary>
    public sealed class BabyAddImprintView : BabyAddImprintViewBind
    {
        private Transform _content;
        private TextMeshProUGUI _nothingLb;
        private GameObject _itemTemplate;
        private readonly List<GameObject> _items = new List<GameObject>();
        private Action<int> _onSelected;

        public bool NothingVisible => _nothingLb != null && _nothingLb.gameObject.activeSelf;
        public int CandidateCount => _items.Count;

        public void SetCandidates(IReadOnlyList<BabyEquipEngraveConfigs.EngraveCfg> candidates, Action<int> onSelected)
        {
            CacheNodes();
            Clear();
            _onSelected = onSelected;
            int count = candidates != null ? candidates.Count : 0;
            if (_nothingLb != null) _nothingLb.gameObject.SetActive(count == 0);
            if (_content == null || _itemTemplate == null || candidates == null) return;
            for (int i = 0; i < candidates.Count; i++)
            {
                BabyEquipEngraveConfigs.EngraveCfg cfg = candidates[i];
                if (cfg == null || cfg.GoodsId <= 0) continue;
                GameObject go = Instantiate(_itemTemplate, _content);
                go.SetActive(true);
                BabyAddImprintItem item = go.GetComponent<BabyAddImprintItem>();
                if (item == null) { DestroyItem(go); continue; }
                item.SetData(cfg.GoodsId, cfg.Ratio, SelectOnce);
                _items.Add(go);
            }
            if (_nothingLb != null) _nothingLb.gameObject.SetActive(_items.Count == 0);
        }

        public void Clear()
        {
            _onSelected = null;
            for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]);
            _items.Clear();
            if (_nothingLb != null) _nothingLb.gameObject.SetActive(false);
        }

        protected override void OnHide() => Clear();
        protected override void OnDispose() => Clear();

        private void SelectOnce(int typeId)
        {
            Action<int> callback = _onSelected;
            _onSelected = null;
            callback?.Invoke(typeId);
        }

        private void CacheNodes()
        {
            if (_content != null || _itemTemplate != null) return;
            if (_Scroller1 != null) _content = _Scroller1.content;
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (_content == null && node.name == "Content") _content = node;
                else if (node.name == "nothingLb") _nothingLb = node.GetComponent<TextMeshProUGUI>();
                else if (node.name == "BabyAddImprintItem" && node.parent != null && node.parent.name == "__Templates") _itemTemplate = node.gameObject;
            }
        }

        private static void DestroyItem(GameObject item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
