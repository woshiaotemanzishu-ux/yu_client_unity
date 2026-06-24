// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/GoodsFuncView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class GoodsFuncViewBind : BaseView
    {
        public Image img_bg;
        public Image img_bg2;
        public TextMeshProUGUI lb_name;
        public RectTransform gp_item;
        public RectTransform gp_price;
        public TextMeshProUGUI price_text;
        public TextMeshProUGUI lb_price;
        public Image price_image;
        public RectTransform gp_slider;
        public Image btn_close;
        public Image btn_cancel;
        public TextMeshProUGUI lb_cancel;
        public Image btn_enter;
        public TextMeshProUGUI lb_enter;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_WithBtnHSlider;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_bg2), img_bg2);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(gp_item), gp_item);
            EnsureBound(nameof(gp_price), gp_price);
            EnsureBound(nameof(price_text), price_text);
            EnsureBound(nameof(lb_price), lb_price);
            EnsureBound(nameof(price_image), price_image);
            EnsureBound(nameof(gp_slider), gp_slider);
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(btn_cancel), btn_cancel);
            EnsureBound(nameof(lb_cancel), lb_cancel);
            EnsureBound(nameof(btn_enter), btn_enter);
            EnsureBound(nameof(lb_enter), lb_enter);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_WithBtnHSlider), _tpl_WithBtnHSlider);
        }
    }
}
