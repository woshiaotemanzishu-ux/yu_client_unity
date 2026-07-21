using System;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>主铭刻窗的材料/添加槽，仅提供本地点击回调。</summary>
    public sealed class BabyImprintItem : BabyImprintItemBind
    {
        private Transform _itemGp;
        private Image _addImg;
        private TextMeshProUGUI _numLb;
        private GameObject _awardTemplate;
        private GameObject _award;
        private Button _button;
        private Action _onClick;
        private bool _bound;

        public int TypeId { get; private set; }
        public int Num { get; private set; }
        public bool IsAdd { get; private set; }

        public void SetMaterial(int typeId, int num, Action onClick)
        {
            CacheNodes();
            TypeId = Math.Max(0, typeId); Num = Math.Max(0, num); IsAdd = false; _onClick = onClick;
            if (_addImg != null) _addImg.gameObject.SetActive(false);
            if (_numLb != null) { _numLb.gameObject.SetActive(Num > 0); _numLb.text = Num > 0 ? Num.ToString() : string.Empty; }
            SetAward(TypeId, Num);
        }

        public void SetAdd(Action onClick)
        {
            CacheNodes();
            TypeId = 0; Num = 0; IsAdd = true; _onClick = onClick;
            if (_addImg != null) _addImg.gameObject.SetActive(true);
            if (_numLb != null) { _numLb.text = string.Empty; _numLb.gameObject.SetActive(false); }
            SetAward(0, 0);
        }

        private void CacheNodes()
        {
            if (_itemGp != null || _awardTemplate != null) return;
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.parent == transform && node.name == "itemGp") _itemGp = node;
                else if (node.parent == transform && node.name == "addImg") _addImg = node.GetComponent<Image>();
                else if (node.parent == transform && node.name == "numLb") _numLb = node.GetComponent<TextMeshProUGUI>();
                else if (node.name == "BaseAwardItem" && node.parent != null && node.parent.name == "__Templates") _awardTemplate = node.gameObject;
            }
            Transform click = _itemGp != null ? _itemGp : transform;
            Image hit = click.GetComponent<Image>();
            if (hit == null) { hit = click.gameObject.AddComponent<Image>(); hit.color = Color.clear; }
            hit.raycastTarget = true;
            if (_button == null) _button = click.GetComponent<Button>() ?? click.gameObject.AddComponent<Button>();
            _button.targetGraphic = hit;
            if (!_bound) { _bound = true; _button.onClick.AddListener(() => _onClick?.Invoke()); }
        }

        private void SetAward(int typeId, int num)
        {
            if (_award != null) { if (Application.isPlaying) Destroy(_award); else DestroyImmediate(_award); _award = null; }
            if (_itemGp == null || _awardTemplate == null || typeId <= 0) return;
            _award = Instantiate(_awardTemplate, _itemGp); _award.SetActive(true);
            _award.GetComponent<BaseAwardItem>()?.SetData(typeId, Math.Max(1, num));
        }
    }
}
