using System.Collections.Generic;
using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>铭刻主窗的本地展示骨架；不负责材料选择、协议发送或窗口路由。</summary>
    public sealed class BabyImprintView : BabyImprintViewBind
    {
        private Transform _candidateContent;
        private Transform _selectedContent;
        private TextMeshProUGUI _skillLb;
        private GameObject _itemTemplate;
        private readonly List<GameObject> _items = new List<GameObject>();

        public int PositionId { get; private set; }
        public string SkillText => _skillLb != null ? _skillLb.text : string.Empty;

        public void SetPositionId(int positionId)
        {
            PositionId = positionId >= 1 && positionId <= 6 ? positionId : 0;
        }

        public void SetSkillText(string text)
        {
            CacheNodes();
            if (_skillLb != null) _skillLb.text = text ?? string.Empty;
        }

        public void Clear()
        {
            PositionId = 0;
            ClearItems();
            if (_skillLb != null) _skillLb.text = string.Empty;
        }

        protected override void OnHide() => Clear();
        protected override void OnDispose() => Clear();

        private void CacheNodes()
        {
            if (_candidateContent != null || _selectedContent != null || _itemTemplate != null) return;
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "Content1") _candidateContent = node;
                else if (node.name == "Content11") _selectedContent = node;
                else if (node.name == "skillLb") _skillLb = node.GetComponent<TextMeshProUGUI>();
                else if (node.name == "BabyImprintItem" && node.parent != null && node.parent.name == "__Templates") _itemTemplate = node.gameObject;
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++) DestroyItem(_items[i]);
            _items.Clear();
        }

        private static void DestroyItem(GameObject item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
