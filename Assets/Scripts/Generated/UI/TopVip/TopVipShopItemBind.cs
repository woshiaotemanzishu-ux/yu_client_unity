// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/topVip/TopVipShopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.TopVip
{
    public partial class TopVipShopItemBind : BaseView
    {
        public Image bg_img;
        public TextMeshProUGUI name_lb;
        public RectTransform item_gp;
        public RectTransform buy_gp;
        public Image buy_img;
        public TextMeshProUGUI buy_lb;
        public TextMeshProUGUI price_lb;
        public Image diamond_img;
        public RectTransform buyed_gp;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public RectTransform limit_gp;
        public Image image;
        public TextMeshProUGUI _Label2;
        public TextMeshProUGUI can_buy_num_lb;
        public TextMeshProUGUI forecast_lb;
        public Image forecast_img;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg_img), bg_img);
            EnsureBound(nameof(name_lb), name_lb);
            EnsureBound(nameof(item_gp), item_gp);
            EnsureBound(nameof(buy_gp), buy_gp);
            EnsureBound(nameof(buy_img), buy_img);
            EnsureBound(nameof(buy_lb), buy_lb);
            EnsureBound(nameof(price_lb), price_lb);
            EnsureBound(nameof(diamond_img), diamond_img);
            EnsureBound(nameof(buyed_gp), buyed_gp);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(limit_gp), limit_gp);
            EnsureBound(nameof(image), image);
            EnsureBound(nameof(_Label2), _Label2);
            EnsureBound(nameof(can_buy_num_lb), can_buy_num_lb);
            EnsureBound(nameof(forecast_lb), forecast_lb);
            EnsureBound(nameof(forecast_img), forecast_img);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
