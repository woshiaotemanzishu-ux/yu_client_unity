using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 折扣/限购商品格(对标老端 shop/ShopLimitItem.ts,荣耀/勋章/冲霄/功勋/天境/幸运/神庭/善缘等页签用):
    /// 有折扣(discount&lt;100)显 discount_style(现价+删除线原价+折扣角标),否则显 normal_style(单价);
    /// 三态限购文案(每日/每周/终生/限购)+ 售罄态(sell_out/sell_out_btn 遮罩,点击仅 toast)。
    ///
    /// 降级同 <see cref="ShopItem"/>:goods_icon/*_cost_icon 图标格未接(BaseAwardItem.prefab 已知阻塞,TODO);
    /// 购买条件仅接 lv/vip 两支(guild_lv/guild_title/constellation_equip/god_pool_lv/rank_dun_level 等分支
    /// 依赖模块 Unity 侧未逐一核实,不臆造,TODO);购买简化为直接下单 num=1(同 ShopItem,偏差记汇报)。
    /// </summary>
    public sealed class ShopLimitItem : ShopLimitItemBind
    {
        private ShopModel.GoodsVo _vo;

        protected override void OnInit()
        {
            BindClick(buy_btn, OnBuyClick);
            BindClick(normal_buy_btn, OnBuyClick);
            BindClick(sell_out_btn, () => TipsManager.Toast("没货啦~"));
        }

        /// <summary>填一条商品(对标 ShopLimitItem.dataChanged + SetState 合并简化版)。</summary>
        public void SetData(ShopModel.GoodsVo vo)
        {
            _vo = vo;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(vo.GoodsId);
            if (goodsname != null) goodsname.text = basic != null ? basic.Name : ("物品" + vo.GoodsId);

            bool haveDiscount = vo.Discount < 100;
            if (discount_style != null) discount_style.gameObject.SetActive(haveDiscount);
            if (normal_style != null) normal_style.gameObject.SetActive(!haveDiscount);

            int nowPrice = vo.Price * vo.Discount / 100;
            if (price != null) price.text = nowPrice.ToString();
            if (orignal_price != null) orignal_price.text = vo.Price.ToString();
            if (normal_price != null) normal_price.text = nowPrice.ToString();
            if (haveDiscount && img_disc != null)
            {
                string key = GameResPath.GetIcon("common2", "sh_discount_" + (vo.Discount / 10));
                _ = ResManager.SetImageAsync(img_disc, key, false, false);
            }

            bool hasLimit = vo.QuotaNum > 0;
            if (limit_conta != null) limit_conta.gameObject.SetActive(hasLimit);
            if (hasLimit)
            {
                int remain = vo.QuotaNum - vo.SoldOut;
                if (limit_num != null) limit_num.text = remain + "/" + vo.QuotaNum;
            }
            if (limitLb != null)
            {
                limitLb.text = vo.QuotaType == 1 ? "每日限购" : vo.QuotaType == 2 ? "每周限购" : vo.QuotaType == 3 ? "终生限购" : "限购";
            }

            bool soldOut = hasLimit && (vo.QuotaNum - vo.SoldOut) <= 0;
            if (sell_out != null) sell_out.gameObject.SetActive(soldOut);
            if (sell_out_btn != null) sell_out_btn.gameObject.SetActive(soldOut);
            if (buy_btn != null) buy_btn.gameObject.SetActive(!soldOut);
            if (normal_buy_btn != null) normal_buy_btn.gameObject.SetActive(!soldOut);

            ApplyCondition(vo);
        }

        /// <summary>购买条件(仅 lv/vip 两支,对标 SetCondition 简化版)。</summary>
        private void ApplyCondition(ShopModel.GoodsVo vo)
        {
            bool showCond = false;
            string text = "";
            ErlangTerm term = ErlangParser.Parse(vo.Condition);
            if (term != null && term.IsCollection)
            {
                foreach (ErlangTerm item in term.Items)
                {
                    if (!item.IsCollection || item.Items.Count < 2) continue;
                    string key = item.Get<string>(0);
                    int val = item.Get<int>(1);
                    if (key == "lv" && RoleModel.Instance.Level < val) { showCond = true; text = val + "级以上可购买"; break; }
                    if (key == "vip" && GetVipFlag() < val) { showCond = true; text = "VIP" + val + "以上可购买"; break; }
                }
            }
            if (condition != null) { condition.text = text; condition.gameObject.SetActive(showCond); }
            if (showCond)
            {
                if (sell_out_btn != null) sell_out_btn.gameObject.SetActive(false);
                if (buy_btn != null) buy_btn.gameObject.SetActive(false);
                if (normal_buy_btn != null) normal_buy_btn.gameObject.SetActive(false);
            }
        }

        private static int GetVipFlag()
        {
            Shenxiao.Common.Proto.FigureProto fig = RoleModel.Instance.Figure;
            if (fig == null) return 0;
            return fig.Raw.TryGetValue("vip_flag", out object v) ? System.Convert.ToInt32(v) : 0;
        }

        private void OnBuyClick()
        {
            if (_vo == null) return;
            ShopController.Instance.BuyGoods(_vo.KeyId, 1);
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
