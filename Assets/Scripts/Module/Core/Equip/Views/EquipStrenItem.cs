using System;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 天殒淬炉装备位。该业务组件只消费 Equip 私有模板；CommonModule 中仅挂
    /// EquipStrenItemBind 的同源节点不是已验收共享组件，不能跨模块直接克隆。
    /// </summary>
    public sealed class EquipStrenItem : EquipStrenItemBind
    {
        private int _equipType;
        private bool _interactive = true;
        private bool _equipmentTooltipEnabled = true;
        private BagGoods _data;
        private EquipmentItem _equipmentItem;
        private Action<int> _onSelect;

        public int EquipType => _equipType;
        public bool HasEquipment => _data != null;

        protected override void OnInit()
        {
            SetActive(select_img, false);
            SetActive(tips, false);
            SetActive(red_dot, false);
            SetActive(group_eff, false);
            SetActive(grade, false);

            BindClick(click_bg, OnClick);
        }

        public void SetEquipPos(int equipType)
        {
            _equipType = equipType;
            RefreshHitArea();
        }

        public void SetInteractive(bool interactive)
        {
            _interactive = interactive;
            RefreshHitArea();
            RefreshEquipmentItemInteraction();
        }

        public void SetEquipmentTooltipEnabled(bool enabled)
        {
            _equipmentTooltipEnabled = enabled;
            RefreshEquipmentItemInteraction();
        }

        public void SetSelectCallback(Action<int> onSelect) => _onSelect = onSelect;

        public void SetData(BagGoods data, GameObject equipmentTemplate)
        {
            _data = data;
            EnsureEquipmentItem(equipmentTemplate);

            if (_equipmentItem != null)
            {
                if (data != null)
                {
                    if (!_equipmentItem.IsShown) _equipmentItem.Show();
                    _equipmentItem.SetData(data.TypeId, 1);
                    _equipmentItem.SetDisplayColor(data.Color);
                }
                else if (_equipmentItem.IsShown)
                {
                    _equipmentItem.Hide();
                }
            }

            bool emptySlot = data == null && _equipType > 0;
            SetActive(icon_bg, emptySlot);
            SetActive(lock_icon, emptySlot);
            if (data == null) SetSelect(false);
            RefreshStrengthLevel();
            RefreshHitArea();
            RefreshEquipmentItemInteraction();
        }

        public void SetMax(bool isMax)
        {
            SetActive(tips, isMax && _data != null);
        }

        public void RefreshStrengthLevel()
        {
            if (level == null) return;
            bool visible = _data != null;
            level.gameObject.SetActive(visible);
            if (!visible)
            {
                level.text = string.Empty;
                return;
            }

            int value = _equipType > 0 && EquipStrenModel.Instance.HasStren(_equipType)
                ? EquipStrenModel.Instance.GetStren(_equipType)
                : _data.Stren;
            level.text = "+" + Mathf.Max(0, value);
        }

        public void SetSelect(bool selected)
        {
            SetActive(select_img, selected && _data != null && _equipType > 0);
        }

        protected override void OnShow(object args)
        {
            if (_data != null && _equipmentItem != null && !_equipmentItem.IsShown)
                _equipmentItem.Show();
        }

        protected override void OnHide()
        {
            if (_equipmentItem != null && _equipmentItem.IsShown) _equipmentItem.Hide();
        }

        protected override void OnDispose()
        {
            if (_equipmentItem != null && _equipmentItem.IsShown) _equipmentItem.Hide();
            _equipmentItem = null;
            _onSelect = null;
            base.OnDispose();
        }

        private void OnClick()
        {
            if (!_interactive || _equipType <= 0) return;
            if (_data == null)
            {
                TipsManager.Toast("穿戴装备后才可以天殒淬炉该部位哦");
                return;
            }

            SetSelect(true);
            _onSelect?.Invoke(_equipType);
        }

        private void EnsureEquipmentItem(GameObject equipmentTemplate)
        {
            if (_equipmentItem != null || equipmentTemplate == null || award_con == null) return;

            GameObject go = Instantiate(equipmentTemplate, award_con, false);
            go.name = "EquipmentItem_Runtime";
            go.SetActive(false);
            if (go.transform is RectTransform rt)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one * 0.9f;
            }

            _equipmentItem = go.GetComponent<EquipmentItem>();
            if (_equipmentItem == null)
            {
                Destroy(go);
                GameLog.Warn("Equip", "EquipStrenItem 的 EquipmentItem 模板缺少业务组件");
            }
        }

        private void RefreshHitArea()
        {
            if (click_bg == null) return;
            bool active = _interactive && _equipType > 0;
            click_bg.gameObject.SetActive(active);
            click_bg.raycastTarget = active;
        }

        private void RefreshEquipmentItemInteraction()
        {
            if (_equipmentItem == null) return;
            Action callback = () => { };
            if (_equipmentTooltipEnabled && _data != null)
                callback = () => ItemTipsView.ShowEquipped(_data);
            _equipmentItem.SetClickCallBack(callback);
            if (_equipmentItem.click_group != null)
            {
                Image image = _equipmentItem.click_group.GetComponent<Image>();
                if (image != null) image.raycastTarget = _equipmentTooltipEnabled && _data != null;
            }
        }

        private static void BindClick(Component target, Action callback)
        {
            if (target == null) return;
            Image image = target as Image;
            if (image == null) image = target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, callback);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
