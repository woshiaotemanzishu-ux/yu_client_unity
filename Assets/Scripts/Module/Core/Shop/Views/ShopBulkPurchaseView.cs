using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商城批量购买弹窗。对标老端 ShopBulkPurchaseView：数量增减、限购上限、总价和确认购买。
    /// 物品/货币图标继续等待共享 BaseAwardItem 身份链，不在商城内复制一份节点树。
    /// </summary>
    public sealed class ShopBulkPurchaseView : ShopBulkPurchaseViewBind
    {
        private ShopModel.GoodsVo _vo;
        private int _onePrice;
        private int _count = 1;
        private int _maxCount = 1;
        private bool _subscribed;

        public override UILayer Layer => UILayer.Popup;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick(closeBtn, Hide);
            BindClick(cancleBtn, Hide);
            BindClick(minus_one, () => ChangeCount(-1));
            BindClick(minus_ten, () => ChangeCount(-10));
            BindClick(add_one, () => ChangeCount(1));
            BindClick(add_ten, () => ChangeCount(10));
            BindClick(confirmBtn, ConfirmPurchase);
        }

        protected override void OnShow(object args)
        {
            _vo = args as ShopModel.GoodsVo;
            Subscribe();
            RefreshData();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            _vo = null;
        }

        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_BUY_SUCCESS, OnBuySuccess);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_BUY_SUCCESS, OnBuySuccess);
            _subscribed = false;
        }

        private void OnBuySuccess(int keyId)
        {
            if (_vo != null && _vo.KeyId == keyId) Hide();
        }

        private void RefreshData()
        {
            if (_vo == null) { Hide(); return; }

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(_vo.GoodsId);
            if (goodsname != null) goodsname.text = basic != null ? basic.Name : ("物品" + _vo.GoodsId);

            _onePrice = _vo.Discount < 100
                ? Mathf.RoundToInt(_vo.Price * _vo.Discount / 100f)
                : _vo.Price;
            int left = _vo.QuotaNum > 0 ? Mathf.Max(0, _vo.QuotaNum - _vo.SoldOut) : 999999;
            _maxCount = Mathf.Clamp(left, 1, 999999);
            _count = _vo.QuotaNum > 0 && _vo.QuotaNum < 10 ? _maxCount : 1;

            if (lb_limit != null)
            {
                bool limited = _vo.QuotaNum > 0;
                lb_limit.gameObject.SetActive(limited);
                if (limited)
                {
                    string prefix = _vo.QuotaType == 3 ? "终生限购：" : "每日存货：";
                    lb_limit.text = prefix + left + "/" + _vo.QuotaNum;
                }
            }
            if (_Label7 != null) _Label7.text = "购买";
            RefreshCountAndPrice();
        }

        private void ChangeCount(int delta)
        {
            int affordable = _maxCount;
            if (_onePrice > 0)
            {
                (bool _, int __, long have) = ShopModel.Instance.MoneyIsEnough(_vo.MoneyType, 0);
                affordable = Mathf.Min(affordable, (int)Mathf.Clamp(have / _onePrice, 0, 999999));
            }
            int next = Mathf.Clamp(_count + delta, 1, Mathf.Max(1, affordable));
            if (next == _count)
                TipsManager.Toast(delta < 0 ? "已经是最小购买数量了" : "已是最大可购买数量了");
            _count = next;
            RefreshCountAndPrice();
        }

        private void RefreshCountAndPrice()
        {
            if (cur_show_num != null) cur_show_num.text = _count.ToString();
            int total = _onePrice * _count;
            if (lb_price != null)
            {
                lb_price.text = total.ToString();
                bool enough = ShopModel.Instance.MoneyIsEnough(_vo.MoneyType, total).enough;
                lb_price.color = enough ? new Color32(74, 58, 50, 255) : new Color32(255, 79, 80, 255);
            }
        }

        private void ConfirmPurchase()
        {
            if (_vo == null || _count <= 0) return;
            int total = _onePrice * _count;
            if (!ShopModel.Instance.MoneyIsEnough(_vo.MoneyType, total).enough)
            {
                TipsManager.Toast("货币不足");
                return;
            }
            ShopController.Instance.BuyGoods(_vo.KeyId, _count);
        }

        private static void BindClick(Component target, System.Action action)
        {
            if (target == null) return;
            UIUtil.AddClick(target, action);
        }
    }
}
