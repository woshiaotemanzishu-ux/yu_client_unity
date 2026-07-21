using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyEquipIcon : BabyEquipIconBind
    {
        private Transform _itemGp;
        private Image _defaultImg;
        private Image _addImg;
        private Image _effectGp;
        private GameObject _template;
        private GameObject _item;
        public int PositionId { get; private set; }
        public bool IsOccupied { get; private set; }

        public void SetData(int positionId, BabyEquipEntry entry)
        {
            PositionId = positionId;
            CacheNodes();
            if (_item != null) { if (Application.isPlaying) Destroy(_item); else DestroyImmediate(_item); _item = null; }
            bool occupied = entry != null && entry.GoodsTypeId > 0;
            IsOccupied = occupied;
            if (_defaultImg != null) _defaultImg.gameObject.SetActive(!occupied);
            if (_addImg != null) _addImg.gameObject.SetActive(!occupied);
            if (_effectGp != null) _effectGp.gameObject.SetActive(false);
            if (!occupied || _itemGp == null || _template == null) return;
            _item = Instantiate(_template, _itemGp);
            _item.SetActive(true);
            BaseAwardItem award = _item.GetComponent<BaseAwardItem>();
            if (award != null)
            {
                award.SetScale(88f / 127f);
                award.SetData(entry.GoodsTypeId, 1);
            }
        }

        private void CacheNodes()
        {
            if (_itemGp != null) return;
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "itemGp") _itemGp = node;
                else if (node.name == "defaultImg") _defaultImg = node.GetComponent<Image>();
                else if (node.name == "addImg") _addImg = node.GetComponent<Image>();
                else if (node.name == "effectGp") _effectGp = node.GetComponent<Image>();
                else if (node.name == "BaseAwardItem" && node.parent != null && node.parent.name == "__Templates") _template = node.gameObject;
            }
        }
    }
}
