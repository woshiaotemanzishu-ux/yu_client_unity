using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyEquipSubItem : BabyEquipSubItemBind
    {
        private TextMeshProUGUI _nameLb;
        private Image _redImg;
        private Transform _equipGp;
        private Image _selectImg;
        private GameObject _template;
        private GameObject _award;
        private Action<BagGoods> _onClick;
        private bool _bound;

        public long GoodsId => _goods != null ? _goods.GoodsId : 0;
        public int TypeId => _goods != null ? _goods.TypeId : 0;
        private BagGoods _goods;

        public void SetData(BagGoods goods, Action<BagGoods> onClick)
        {
            CacheNodes();
            _goods = goods;
            _onClick = onClick;
            if (!_bound) { _bound = true; UIUtil.AddClick(this, () => _onClick?.Invoke(_goods)); }
            int typeId = TypeId;
            if (_nameLb != null)
            {
                string name = GoodsModel.GetGoodsName(typeId);
                _nameLb.text = string.IsNullOrEmpty(name) ? typeId.ToString() : name;
            }
            if (_redImg != null) _redImg.gameObject.SetActive(false);
            if (_selectImg != null) _selectImg.gameObject.SetActive(false);
            if (_award != null) { if (Application.isPlaying) Destroy(_award); else DestroyImmediate(_award); _award = null; }
            if (_equipGp == null || _template == null || typeId <= 0) return;
            _award = Instantiate(_template, _equipGp);
            _award.SetActive(true);
            BaseAwardItem item = _award.GetComponent<BaseAwardItem>();
            if (item != null) { item.SetScale(88f / 127f); item.SetData(typeId, 1); }
        }

        private void CacheNodes()
        {
            if (_equipGp != null) return;
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "nameLb") _nameLb = node.GetComponent<TextMeshProUGUI>();
                else if (node.name == "redImg") _redImg = node.GetComponent<Image>();
                else if (node.name == "equipGp") _equipGp = node;
                else if (node.name == "selectImg") _selectImg = node.GetComponent<Image>();
                else if (node.name == "BaseAwardItem" && node.parent != null && node.parent.name == "__Templates") _template = node.gameObject;
            }
        }
    }
}
