using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神兵淬炼装备格(对标老客户端 equip/EquipSmeltItem.ts):淬炼面板里的一个部位槽,
    /// 内含装备图标(award_con 克隆 EquipmentItem)、淬炼等级 level(+N)、满阶提示 tips、
    /// 选中高亮 select_img、空位锁图标 lock_icon、强化特效 group_eff、点击背景 click_bg、红点 red_dot。
    /// 点击 click_bg → 选中该部位淬炼,经 <see cref="SetSelectCallback"/> 回调父面板(对标 SELECT_SMELT_EQUIP;
    /// 无装备时保持部位占位并提示「穿戴装备后才可以神兵淬炼该部位哦」。
    ///
    /// 已消费 BagGoods、EquipmentItem 与 EquipSmeltModel 15250 快照；装备详情复用 ItemTipsView.ShowEquipped。
    /// 降级仍限于 config_equip_refine_max 满阶判定、红点与 QiangHua 特效资源闭包，相关节点先隐藏。
    /// </summary>
    public sealed class EquipSmeltItem : EquipSmeltItemBind
    {
        /// <summary>该格对应的装备部位(对标 SetEquipPos 的 equip_type);0 = 未指定。</summary>
        private int _equipType;
        private bool _hasEquipment;
        private bool _selectable = true;
        private bool _tooltipEnabled = true;
        private EquipmentItem _equipmentItem;
        /// <summary>选中回调(对标 SELECT_SMELT_EQUIP 事件消费方,由 <see cref="EquipSmeltView"/> 铺格时挂)。</summary>
        private System.Action<int> _onSelect;

        public int EquipType => _equipType;

        protected override void OnInit()
        {
            // 红点:对标 UPDATE_SMELT_ITEM_RED,数据未移植 → 隐藏。
            if (red_dot != null) red_dot.gameObject.SetActive(false);
            // 选中高亮:对标 SetSelect,默认未选 → 隐藏。
            if (select_img != null) select_img.gameObject.SetActive(false);
            // 满阶提示:对标 is_max 时才显 tips,默认 → 隐藏。
            if (tips != null) tips.gameObject.SetActive(false);
            // 强化特效:对标 QiangHua 特效,特效系统未移植 → 隐藏占位。
            if (group_eff != null) group_eff.gameObject.SetActive(false);

            // 点击背景:对标 click_bg → 选中该部位淬炼(真调用:回调通知父面板 + 本地高亮,对标 SELECT_SMELT_EQUIP)。
            BindBtn(click_bg, () =>
            {
                if (!_selectable || _equipType == 0) return;
                if (!_hasEquipment)
                {
                    TipsManager.Toast("穿戴装备后才可以神兵淬炼该部位哦");
                    return;
                }
                SetSelect(true);
                _onSelect?.Invoke(_equipType);
                GameLog.Info("Equip", "选中部位淬炼 equip_type={0}(SELECT_SMELT_EQUIP)", _equipType);
            });
        }

        /// <summary>设置选中回调(由 <see cref="EquipSmeltView"/> 铺格时挂,驱动 SelectEquipType)。</summary>
        public void SetSelectCallback(System.Action<int> onSelect) => _onSelect = onSelect;

        public void SetInteractive(bool selectable, bool tooltipEnabled = true)
        {
            _selectable = selectable;
            _tooltipEnabled = tooltipEnabled;
            if (click_bg != null)
            {
                click_bg.gameObject.SetActive(selectable && _equipType != 0);
                click_bg.raycastTarget = selectable;
            }
            RefreshEquipmentClick();
        }

        /// <summary>设置该格部位(对标 SetEquipPos)。空位时保留占位图与可点击提示面。</summary>
        public void SetEquipPos(int equipType)
        {
            _equipType = equipType;
            bool hasPos = equipType != 0;
            if (click_bg != null) click_bg.gameObject.SetActive(hasPos);
            if (icon_bg != null) icon_bg.gameObject.SetActive(hasPos);
            // lock_icon 对标 role_default_{equip_type} 占位图,依赖 equipCom_asset 图集(未移植)→ 暂保持原样,不设图。
        }

        /// <summary>填穿戴装备与 15250 淬炼快照；满阶判断仍等待 config_equip_refine_max 资源闭包。</summary>
        public void SetData(BagGoods data, GameObject equipmentTemplate)
        {
            _hasEquipment = data != null;
            EnsureEquipmentItem(equipmentTemplate);

            if (_equipmentItem != null)
            {
                if (_hasEquipment)
                {
                    if (!_equipmentItem.IsShown) _equipmentItem.Show();
                    _equipmentItem.SetData(data.TypeId, 1);
                    _equipmentItem.SetDisplayColor(data.Color);
                    _equipmentItem.SetStrengthen(data.Stren);
                    _equipmentItem.SetClickCallBack(() => ItemTipsView.ShowEquipped(data));
                }
                else if (_equipmentItem.IsShown)
                {
                    _equipmentItem.Hide();
                }
            }

            if (icon_bg != null) icon_bg.gameObject.SetActive(!_hasEquipment && _equipType != 0);
            if (lock_icon != null) lock_icon.gameObject.SetActive(!_hasEquipment && _equipType != 0);

            RefreshSmeltLevel();
            if (tips != null) tips.gameObject.SetActive(false);
            if (!_hasEquipment) SetSelect(false);
            RefreshEquipmentClick();
        }

        public void SetMax(bool max)
        {
            if (tips != null) tips.gameObject.SetActive(max && _hasEquipment);
        }

        public void RefreshSmeltLevel()
        {
            (int refine, _) = EquipSmeltModel.Instance.GetSmelt(_equipType);
            if (level != null)
            {
                bool showLevel = _hasEquipment && EquipSmeltModel.Instance.HasSmelt(_equipType);
                level.gameObject.SetActive(showLevel);
                level.text = showLevel ? "+" + refine : "";
            }
        }

        protected override void OnShow(object args)
        {
            if (_hasEquipment && _equipmentItem != null && !_equipmentItem.IsShown) _equipmentItem.Show();
        }

        protected override void OnHide()
        {
            if (_equipmentItem != null && _equipmentItem.IsShown) _equipmentItem.Hide();
        }

        protected override void OnDispose()
        {
            if (_equipmentItem != null && _equipmentItem.IsShown) _equipmentItem.Hide();
            _equipmentItem = null;
            base.OnDispose();
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
                GameLog.Warn("Equip", "EquipSmeltItem 克隆的 _tpl_EquipmentItem 缺 EquipmentItem 业务组件");
            }
        }

        private void RefreshEquipmentClick()
        {
            if (_equipmentItem == null) return;
            _equipmentItem.SetClickCallBack(() =>
            {
                if (!_tooltipEnabled || !_hasEquipment) return;
                BagGoods worn = EquipAutoWear.GetWorn(_equipType);
                if (worn != null) ItemTipsView.ShowEquipped(worn);
            });
            if (_equipmentItem.click_group != null)
            {
                Image image = _equipmentItem.click_group.GetComponent<Image>();
                if (image != null) image.raycastTarget = _tooltipEnabled && _hasEquipment;
            }
        }

        /// <summary>设置选中高亮(对标 SetSelect)。</summary>
        public void SetSelect(bool selected)
        {
            if (select_img != null) select_img.gameObject.SetActive(selected);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindBtn(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
