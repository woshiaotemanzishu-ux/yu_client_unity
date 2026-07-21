using System;
using System.Globalization;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>铭刻候选行，仅显示名称/成功率并回传本地 type id。</summary>
    public sealed class BabyAddImprintItem : BabyAddImprintItemBind
    {
        private Transform _itemGp;
        private TextMeshProUGUI _nameLb, _probabilityLb;
        private GameObject _awardTemplate, _award;
        private Button _button;
        private Action<int> _onClick;
        private bool _bound;
        public int TypeId { get; private set; }
        public int Ratio { get; private set; }
        public string DisplayName => _nameLb != null ? _nameLb.text : string.Empty;
        public string ProbabilityText => _probabilityLb != null ? _probabilityLb.text : string.Empty;

        public void SetData(int typeId, int ratio, Action<int> onClick)
        {
            CacheNodes();
            TypeId = Math.Max(0, typeId); Ratio = Math.Max(0, ratio); _onClick = onClick;
            if (_nameLb != null) _nameLb.text = TypeId > 0 ? GoodsModel.GetGoodsName(TypeId) : string.Empty;
            if (_probabilityLb != null) _probabilityLb.text = (Ratio / 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";
            if (_award != null) { if (Application.isPlaying) Destroy(_award); else DestroyImmediate(_award); _award = null; }
            if (_itemGp == null || _awardTemplate == null || TypeId <= 0) return;
            _award = Instantiate(_awardTemplate, _itemGp); _award.SetActive(true);
            _award.GetComponent<BaseAwardItem>()?.SetData(TypeId, 1);
        }

        private void CacheNodes()
        {
            if (_itemGp != null || _awardTemplate != null) return;
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.parent == transform && node.name == "itemGp") _itemGp = node;
                else if (node.parent == transform && node.name == "nameLb") _nameLb = node.GetComponent<TextMeshProUGUI>();
                else if (node.parent == transform && node.name == "probabilityLb") _probabilityLb = node.GetComponent<TextMeshProUGUI>();
                else if (node.name == "BaseAwardItem" && node.parent != null && node.parent.name == "__Templates") _awardTemplate = node.gameObject;
            }
            Transform click = transform.Find("clickBg") ?? transform;
            Image hit = click.GetComponent<Image>();
            if (hit == null) { hit = click.gameObject.AddComponent<Image>(); hit.color = Color.clear; }
            hit.raycastTarget = true;
            if (_button == null) _button = click.GetComponent<Button>() ?? click.gameObject.AddComponent<Button>();
            _button.targetGraphic = hit;
            if (!_bound) { _bound = true; _button.onClick.AddListener(() => _onClick?.Invoke(TypeId)); }
        }
    }
}
