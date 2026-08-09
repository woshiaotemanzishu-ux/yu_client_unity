using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 常规商城商品格(对标老端 shop/ShopItem.ts):名称+现价(price*discount/100,四舍五入)+限购展示
    /// (对标老端 SetState:remaining/quota_num,quota_type==3→"终生限购",否则"限购")+购买按钮+售罄态+
    /// 购买条件(仅接 lv/vip 两支,其余 condition_dic 分支未接线——guild_lv/guild_title/constellation_equip/
    /// god_pool_lv/rank_dun_level 依赖模块 Unity 侧未逐一核实,规格 §0 不臆造,TODO)。
    ///
    /// 降级:goods_icon/cost_icon 图标格(老端 BaseAwardItem)未接——同 <see cref="Shenxiao.Module.Core.Common.BaseAwardItem"/>
    /// 注释已知阻塞(BaseAwardItem.prefab 根未挂组件),本轮只做名称/价格/限购/条件/购买按钮的数据展示,TODO。
    /// 购买简化:老端"quota_num==0||sold_out&lt;quota_num 且非 Outward→开 ShopBulkPurchaseView 数量选择弹窗"
    /// 这条几乎总成立的路径,因该弹窗仍是死枝(规格§0)未接线,本端简化为按钮直接下单 num=1(偏差记汇报)。
    /// </summary>
    public sealed class ShopItem : ShopItemBind
    {
        private ShopModel.GoodsVo _vo;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (buy_btn != null)
            {
                buy_btn.gameObject.SetActive(true);
                UnityEngine.UI.Image img = buy_btn.GetComponent<UnityEngine.UI.Image>() ?? buy_btn.GetComponentInChildren<UnityEngine.UI.Image>(true);
                if (img != null) { img.raycastTarget = true; UIUtil.AddClick(img, OnBuyClick); }
            }
        }

        /// <summary>填一条商品(对标 ShopItem.dataChanged + SetState 合并简化版)。</summary>
        public void SetData(ShopModel.GoodsVo vo)
        {
            _vo = vo;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(vo.GoodsId);
            if (goodsname != null) goodsname.text = basic != null ? basic.Name : ("物品" + vo.GoodsId);

            int nowPrice = UnityEngine.Mathf.RoundToInt(vo.Price * vo.Discount / 100f);
            if (price != null) price.text = nowPrice.ToString();

            bool hasLimit = vo.QuotaNum > 0;
            if (limit_conta != null) limit_conta.gameObject.SetActive(hasLimit);
            if (hasLimit)
            {
                int remain = vo.QuotaNum - vo.SoldOut;
                if (limit_num != null) limit_num.text = remain + "/" + vo.QuotaNum;
                if (limitLb != null) limitLb.text = vo.QuotaType == 3 ? "终生限购" : "限购";
            }

            bool soldOut = hasLimit && (vo.QuotaNum - vo.SoldOut) <= 0;
            if (soldout != null) soldout.gameObject.SetActive(soldOut);
            if (_lb != null) _lb.text = soldOut ? "售罄" : "购买";

            ApplyCondition(vo);
        }

        /// <summary>购买条件(仅 lv/vip 两支,对标 ShopItem.ts SetState 条件段简化版)。</summary>
        private void ApplyCondition(ShopModel.GoodsVo vo)
        {
            bool showCond = false;
            string text = "";
            ErlangTerm term = ErlangParser.Parse(vo.Condition);
            if (term != null && term.IsCollection && term.Items.Count > 0)
            {
                ErlangTerm first = term.Items[0];
                if (first.IsCollection && first.Items.Count >= 2)
                {
                    string key = first.Get<string>(0);
                    int val = first.Get<int>(1);
                    if (key == "lv" && RoleModel.Instance.Level < val) { showCond = true; text = val + "级以上可购买"; }
                    else if (key == "vip" && GetVipFlag() < val) { showCond = true; text = "VIP" + val + "以上可购买"; }
                }
            }
            if (condition != null) { condition.text = text; condition.gameObject.SetActive(showCond); }
            if (buy_btn != null) buy_btn.gameObject.SetActive(!showCond);
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
            bool canChooseCount = (_vo.QuotaNum == 0 || _vo.SoldOut < _vo.QuotaNum)
                                  && _vo.ShopType != ShopModel.TYPE_OUTWARD;
            if (canChooseCount) ShopFlow.OpenBulkPurchase(_vo);
            else ShopController.Instance.BuyGoods(_vo.KeyId, 1);
        }
    }
}
