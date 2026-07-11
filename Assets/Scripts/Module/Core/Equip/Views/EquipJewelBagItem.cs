using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 宝石背包条目(对标老客户端 jewel/EquipJewelBagItem.ts):图标格(_gp_item)+ 当前已镶嵌标(_img_now/_lb_name0)+
    /// 名称(_lb_name)+ 属性/描述(_lb_attr)+ 红点(_reddot)。老端支持三态(商城引导/直升丹/常规宝石),本轮
    /// <see cref="EquipJewelBagView"/> 只铺"直升丹"这一固定条目(常规宝石列表依赖未移植的
    /// config_equip_stone_inlay/config_equip_stone_lv,见 EquipJewelBagView 类注释),故本项只实现
    /// <see cref="SetUpOneData"/> 一种数据模式。
    /// </summary>
    public sealed class EquipJewelBagItem : EquipJewelBagItemBind
    {
        private System.Action _onClick;

        protected override void OnInit()
        {
            BindClick(_Image1, () => _onClick?.Invoke());
        }

        /// <summary>直升丹固定条目(对标老端 is_up_one 分支):名称 + "可使用宝石直升卡" + 红点常亮。
        /// 图标(_gp_item 内嵌 BaseAwardItem)依赖未在本项接入的图标克隆管线 → 暂不设图标,仅文字展示。</summary>
        public void SetUpOneData(int typeId, long haveNum, System.Action onClick)
        {
            _onClick = onClick;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (_lb_name != null) _lb_name.text = basic != null && !string.IsNullOrEmpty(basic.Name) ? basic.Name : "宝石直升丹";
            if (_lb_attr != null) _lb_attr.text = "可使用宝石直升卡(拥有 " + haveNum + ")";
            if (_reddot != null) _reddot.gameObject.SetActive(true);
            if (_img_now != null) _img_now.gameObject.SetActive(false);
            if (_lb_name0 != null) _lb_name0.gameObject.SetActive(false);
        }

        private void BindClick(Component target, System.Action onClick)
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
