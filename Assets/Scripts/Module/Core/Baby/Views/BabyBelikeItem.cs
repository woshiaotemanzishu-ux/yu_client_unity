using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyBelikeItem : BabyBelikeItemBind
    {
        private Image _likeBtn;
        private TextMeshProUGUI _lb;
        private bool _bound;
        private long _praiserId;

        public void SetData(BabyPraiseRecordEntry entry)
        {
            CacheNodes();
            _praiserId = entry != null ? entry.PraiserId : 0;
            if (_lb != null) _lb.text = entry != null ? entry.Name ?? string.Empty : string.Empty;
            if (_likeBtn != null) _likeBtn.gameObject.SetActive(entry != null && !entry.IsPraiseBack);
            if (_bound || _likeBtn == null) return;
            _bound = true;
            UIUtil.AddClick(_likeBtn, () => BabyController.Instance.RequestPraise(_praiserId, 2));
        }

        private void CacheNodes()
        {
            if (_lb != null) return;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].name == "likeBtn") _likeBtn = nodes[i].GetComponent<Image>();
                else if (nodes[i].name == "lb") _lb = nodes[i].GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
