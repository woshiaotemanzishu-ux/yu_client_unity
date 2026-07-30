using Shenxiao.Generated.UI.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包装备图标槽(对标老客户端 bag 装备槽图标):展示已穿戴装备 / 空槽(加号 _img_add),带锁/红点/特效。
    /// 独立 prefab(BagEquipmentIcon.prefab),多处复用。
    ///
    /// 降级:装备数据(EquipModel/BagModel)未移植 → 物品模板(BaseAwardItem/EquipmentItem)/红点/锁隐藏、
    /// 默认空槽(显加号);SetEmpty 预留(数据接上后:有装备显 award_con 内物品、隐加号)。事件驱动,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class BagEquipmentIcon : BagEquipmentIconBind
    {
        private EquipmentItem _item;
        private BagGoods _goods;

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
