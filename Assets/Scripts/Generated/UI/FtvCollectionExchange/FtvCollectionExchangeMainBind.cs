// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvCollectionExchange/FtvCollectionExchangeMain.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvCollectionExchange
{
    public partial class FtvCollectionExchangeMainBind : BaseView
    {
        public Image _img_titlebg;
        public Image title_img;
        public Image time_bg;
        public RectTransform _gp_time;
        public Image _img_bg;
        public ScrollRect _Scroller1;
        public RectTransform _gp_scroller;
        public TextMeshProUGUI _lb_dec;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_titlebg), _img_titlebg);
            EnsureBound(nameof(title_img), title_img);
            EnsureBound(nameof(time_bg), time_bg);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_gp_scroller), _gp_scroller);
            EnsureBound(nameof(_lb_dec), _lb_dec);
        }
    }
}
