using Shenxiao.Generated.UI.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Resonance;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包装备图标槽（对标老客户端 bag 装备槽图标）：展示已穿戴装备或空槽（加号 _img_add）。
    /// 独立 prefab（BagEquipmentIcon.prefab）复用共享 EquipmentItem；只有当前部位的真实已穿戴实例
    /// 满足共鸣状态时才显式启用槽位流光，普通背包格、奖励和详情不会随共享模板自动点亮。
    /// </summary>
    public sealed class BagEquipmentIcon : BagEquipmentIconBind
    {
        private EquipmentItem _item;
        private BagGoods _goods;
        private int _equipPosition;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (red_dot != null) red_dot.gameObject.SetActive(false);
            if (lock_icon != null) lock_icon.gameObject.SetActive(false);
            SetEmpty(true);
        }

        /// <summary>空槽(显加号)/有装备(显物品容器)切换。</summary>
        public void SetEmpty(bool empty)
        {
            if (icon_bg != null) icon_bg.gameObject.SetActive(empty);
            if (_img_add != null) _img_add.gameObject.SetActive(empty);
            if (award_con != null) award_con.gameObject.SetActive(!empty);
            if (_item != null) _item.gameObject.SetActive(!empty);
        }

        /// <summary>设置角色已穿戴容器中的装备部位；对应老端 EquipmentIcon.SetEquipPos。</summary>
        public void SetEquipPosition(int equipPosition)
        {
            _equipPosition = equipPosition;
        }

        /// <summary>绑定当前部位的真实穿戴实例；点击沿用通用实例物品 tips。</summary>
        public void SetData(BagGoods goods)
        {
            _goods = goods;
            bool hasItem = goods != null && goods.GoodsNum > 0;
            if (!hasItem)
            {
                if (_item != null) _item.SetData(0, 0);
                SetEmpty(true);
                return;
            }

            EnsureItem();
            SetEmpty(false);
            if (_item == null) return;
            _item.SetData(goods.TypeId, goods.GoodsNum, goods.Bind != 0);
            _item.SetDisplayColor(goods.Color);
            _item.SetStrengthen(goods.Stren);
            _item.SetTimeLimit(GoodsModel.HasConfigExpiry(goods.TypeId) || goods.ExpireTime > 0);
            byte effectTier = _equipPosition > 0 && _equipPosition <= byte.MaxValue
                ? ResonanceConfigs.GetPositionEffectTier((byte)_equipPosition, goods)
                : (byte)0;
            if (effectTier > 0)
                _item.SetSuitEffect(
                    ResonanceConfigs.GetEffectName(ResonanceConfigs.GetPositionSuitType((byte)_equipPosition), effectTier),
                    effectTier);
            _item.SetClickCallBack(() =>
            {
                if (_goods != null) ItemTipsView.ShowEquipped(_goods);
            });
        }

        private void EnsureItem()
        {
            if (_item != null || _tpl_EquipmentItem == null || award_con == null) return;
            GameObject go = Instantiate(_tpl_EquipmentItem, award_con);
            go.name = "EquipmentItem_Runtime";
            go.SetActive(true);
            _item = go.GetComponent<EquipmentItem>();
            if (_item != null)
            {
                _item.Show();
                _item.SetScale(0.68f); // 对齐老端 EquipmentIcon.UpdateAwardItem。
            }
        }
    }
}
