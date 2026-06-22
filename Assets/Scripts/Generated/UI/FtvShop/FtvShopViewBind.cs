// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvShop/FtvShopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvShop
{
    public partial class FtvShopViewBind : BaseView
    {
        public Image _img_title;
        public Image _img_time_bg;
        public Image _img_left;
        public Image _img_right;
        public RectTransform _gp_time;
        public RectTransform _gp_item;
        public RectTransform _Group1;
        public Image _Image1;
        public TextMeshProUGUI _lb_desc;
        public Image _img_refresh;
        public Image _img_2;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_img_time_bg), _img_time_bg);
            EnsureBound(nameof(_img_left), _img_left);
            EnsureBound(nameof(_img_right), _img_right);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_img_refresh), _img_refresh);
            EnsureBound(nameof(_img_2), _img_2);
        }
    }
}
