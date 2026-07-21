using Shenxiao.Generated.UI.Baby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyLikeItem : BabyLikeItemBind
    {
        private Image _rankImg;
        private TextMeshProUGUI _rankLb;
        private TextMeshProUGUI _nameLb;
        private TextMeshProUGUI _fightLb;
        private TextMeshProUGUI _numLb;

        public void SetData(BabyPraiseRankEntry entry, int rank)
        {
            CacheNodes();
            if (_rankImg != null) _rankImg.gameObject.SetActive(false);
            if (_rankLb != null)
            {
                _rankLb.gameObject.SetActive(true);
                _rankLb.text = rank.ToString();
            }
            if (_nameLb != null) _nameLb.text = entry != null ? entry.Name ?? string.Empty : string.Empty;
            if (_fightLb != null) _fightLb.text = entry != null ? entry.BabyPower.ToString() : string.Empty;
            if (_numLb != null) _numLb.text = entry != null ? entry.PraiseNum.ToString() : string.Empty;
        }

        private void CacheNodes()
        {
            if (_rankLb != null) return;
            Transform[] nodes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                switch (nodes[i].name)
                {
                    case "rankImg": _rankImg = nodes[i].GetComponent<Image>(); break;
                    case "rankLb": _rankLb = nodes[i].GetComponent<TextMeshProUGUI>(); break;
                    case "nameLb": _nameLb = nodes[i].GetComponent<TextMeshProUGUI>(); break;
                    case "fightLb": _fightLb = nodes[i].GetComponent<TextMeshProUGUI>(); break;
                    case "numLb": _numLb = nodes[i].GetComponent<TextMeshProUGUI>(); break;
                }
            }
        }
    }
}
