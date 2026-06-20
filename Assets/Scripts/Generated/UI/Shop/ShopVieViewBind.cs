// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/shop/ShopVieView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Shop
{
    public partial class ShopVieViewBind : BaseView
    {
        public ScrollRect _scroll;
        public ScrollRect _dgp_item;
        public RectTransform _Group2;
        public Image _Image1;
        public RectTransform _Group1;
        public Image _Image2;
        public TextMeshProUGUI _lb_time;
        public GameObject _tpl_ShopVieItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_scroll), _scroll);
            EnsureBound(nameof(_dgp_item), _dgp_item);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_tpl_ShopVieItem), _tpl_ShopVieItem);
        }
    }
}
