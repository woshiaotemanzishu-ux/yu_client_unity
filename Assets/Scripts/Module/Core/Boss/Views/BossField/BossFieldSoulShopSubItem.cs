using System;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldSoulShopSubItem : BossFieldSoulShopSubItemBind
    {
        private ShopModel.GoodsVo _goods;
        protected override void OnInit()
        {
            BindClick(_box_buy, Buy);
            BindClick(_box_icon, () => GameLog.Info("BossField", "商品详情由 Common blocker 承载 goods={0}", _goods?.GoodsId ?? 0));
        }
        protected override void OnShow(object args)
        {
            _goods = args as ShopModel.GoodsVo;
            if (_goods == null) return;
            string name = GoodsModel.GetGoodsName(_goods.GoodsId);
            if (_lb_name != null) _lb_name.text = string.IsNullOrEmpty(name) ? "物品 " + _goods.GoodsId : name;
            if (_lb_prive != null) _lb_prive.text = _goods.Price.ToString();
            int remain = _goods.QuotaType == 0 ? int.MaxValue : Math.Max(0, _goods.QuotaNum - _goods.SoldOut);
            bool sold = remain == 0;
            if (_img_quota != null) _img_quota.gameObject.SetActive(_goods.QuotaType != 0);
            if (_lb_quota != null) _lb_quota.text = _goods.QuotaType == 0 ? "" : "剩余 " + remain;
            if (_box_buy != null) _box_buy.gameObject.SetActive(!sold);
            if (_box_soldout != null) _box_soldout.gameObject.SetActive(sold);
        }
        private void Buy() { if (_goods != null) ShopController.Instance.BuyGoods(_goods.KeyId, 1); }
        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }
}
