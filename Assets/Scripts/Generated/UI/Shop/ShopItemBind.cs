// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/shop/ShopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Shop
{
    public partial class ShopItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI goodsname;
        public RectTransform goods_icon;
        public Image img_disc;
        public RectTransform limit_conta;
        public TextMeshProUGUI limitLb;
        public TextMeshProUGUI limit_num;
        public RectTransform _Group1;
        public RectTransform cost_icon;
        public TextMeshProUGUI price;
        public RectTransform buy_btn;
        public Image _img_buy;
        public TextMeshProUGUI _lb;
        public TextMeshProUGUI condition;
        public RectTransform soldout;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(goodsname), goodsname);
            EnsureBound(nameof(goods_icon), goods_icon);
            EnsureBound(nameof(img_disc), img_disc);
            EnsureBound(nameof(limit_conta), limit_conta);
            EnsureBound(nameof(limitLb), limitLb);
            EnsureBound(nameof(limit_num), limit_num);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(cost_icon), cost_icon);
            EnsureBound(nameof(price), price);
            EnsureBound(nameof(buy_btn), buy_btn);
            EnsureBound(nameof(_img_buy), _img_buy);
            EnsureBound(nameof(_lb), _lb);
            EnsureBound(nameof(condition), condition);
            EnsureBound(nameof(soldout), soldout);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
