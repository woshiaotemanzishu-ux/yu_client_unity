// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/demon/DemonShopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Demon
{
    public partial class DemonShopViewBind : BaseView
    {
        public Image top_title;
        public Image line_1;
        public Image line_2;
        public ScrollRect _Scroller1;
        public Image _Image1;
        public RectTransform temp2;
        public Image _Image2;
        public TextMeshProUGUI _lb_cost;
        public RectTransform _Group1;
        public Image temp;
        public RectTransform _btn;
        public Image _Image11;
        public TextMeshProUGUI lb_desc;
        public TextMeshProUGUI lb_desc2;
        public GameObject _tpl_DemonShopItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(top_title), top_title);
            EnsureBound(nameof(line_1), line_1);
            EnsureBound(nameof(line_2), line_2);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(temp2), temp2);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_cost), _lb_cost);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(temp), temp);
            EnsureBound(nameof(_btn), _btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(lb_desc), lb_desc);
            EnsureBound(nameof(lb_desc2), lb_desc2);
            EnsureBound(nameof(_tpl_DemonShopItem), _tpl_DemonShopItem);
        }
    }
}
