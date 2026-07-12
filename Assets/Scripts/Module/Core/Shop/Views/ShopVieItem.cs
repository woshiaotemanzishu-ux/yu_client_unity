using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 抢购商品格(对标老端 shop/ShopVieItem.ts):名称+现价/原价(有折扣显删除线原价)+每日限购剩余
    /// (daily_limit_num - buy_num)+售罄/购买按钮三态(_btn_over 售罄/_btn_buy_dis 折扣价购买/_btn_buy_nor 原价购买)。
    /// 由 ShopVieView 按 64000 落地的 ShopModel.VieInfoVo.IdList 克隆填充。
    /// 降级:_group_item/_group_cost 图标格(老端 BaseAwardItem)未接,同 ShopItem/ShopLimitItem 已知阻塞,TODO。
    /// </summary>
    public sealed class ShopVieItem : ShopVieItemBind
    {
        private ShopModel.VieGoodVo _vo;

        protected override void OnInit()
        {
            BindClick(_btn_buy_dis, OnBuyClick);
            BindClick(_btn_buy_nor, OnBuyClick);
            BindClick(_btn_over, () => TipsManager.Toast("没货啦~"));
        }

        /// <summary>填一条抢购商品(对标 ShopVieItem.dataChanged 简化版)。</summary>
        public void SetData(ShopModel.VieGoodVo vo)
        {
            _vo = vo;
            bool hasDis = vo.NewPrice != vo.OldPrice;
            string name = GoodsModel.GetGoodsName(vo.GoodId);
            if (_lb_name != null) _lb_name.text = string.IsNullOrEmpty(name) ? ("物品" + vo.GoodId) : name;
            if (_lb_price != null) _lb_price.text = (hasDis ? vo.OldPrice : vo.NewPrice).ToString();
            if (_img_line != null) _img_line.gameObject.SetActive(hasDis);

            int remain = vo.DailyLimitNum - vo.BuyNum;
            if (_lb_limit != null) _lb_limit.text = remain + "/" + vo.DailyLimitNum;

            if (hasDis && vo.OldPrice > 0 && _img_disc != null)
            {
                int disc = (int)System.Math.Ceiling(vo.NewPrice / (double)vo.OldPrice * 10);
                string key = GameResPath.GetIcon("common2", "sh_discount_" + disc);
                _ = ResManager.SetImageAsync(_img_disc, key, false, false);
            }

            bool over1 = vo.BuyNum >= vo.DailyLimitNum;
            bool over2 = vo.LeftLimitNum <= 0;
            bool showOut = over1 || over2;
            if (_img_soldout != null) _img_soldout.gameObject.SetActive(showOut);
            if (_btn_buy_dis != null) _btn_buy_dis.gameObject.SetActive(!showOut && hasDis);
            if (_btn_buy_nor != null) _btn_buy_nor.gameObject.SetActive(!showOut && !hasDis);
            if (_btn_over != null) _btn_over.gameObject.SetActive(showOut);

            if (!showOut && hasDis && _lb_price_dis != null) _lb_price_dis.text = vo.NewPrice.ToString();
            if (_img_cost_dis != null) _img_cost_dis.gameObject.SetActive(!showOut && hasDis);
        }

        private void OnBuyClick()
        {
            if (_vo == null) return;
            ShopController.Instance.BuyVieGoods(_vo.Id, 1);
        }

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
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
