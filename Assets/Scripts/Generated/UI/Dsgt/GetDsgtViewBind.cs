// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dsgt/GetDsgtView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dsgt
{
    public partial class GetDsgtViewBind : BaseView
    {
        public RectTransform _gp_anime;
        public RectTransform _gp_line_effect;
        public Image dsgt_bg_1_image;
        public Image dsgt_bg_2_image;
        public Image _img_item_bg_left;
        public Image _img_item_bg_right;
        public Image light;
        public Image dsgt_geticon_image;
        public Image dsgt_icon_image;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI dsgt_dec_label;
        public TextMeshProUGUI dsgt_timer_label;
        public RectTransform _gp_effect;
        public RectTransform _gp_dsgt;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_anime), _gp_anime);
            EnsureBound(nameof(_gp_line_effect), _gp_line_effect);
            EnsureBound(nameof(dsgt_bg_1_image), dsgt_bg_1_image);
            EnsureBound(nameof(dsgt_bg_2_image), dsgt_bg_2_image);
            EnsureBound(nameof(_img_item_bg_left), _img_item_bg_left);
            EnsureBound(nameof(_img_item_bg_right), _img_item_bg_right);
            EnsureBound(nameof(light), light);
            EnsureBound(nameof(dsgt_geticon_image), dsgt_geticon_image);
            EnsureBound(nameof(dsgt_icon_image), dsgt_icon_image);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(dsgt_dec_label), dsgt_dec_label);
            EnsureBound(nameof(dsgt_timer_label), dsgt_timer_label);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_gp_dsgt), _gp_dsgt);
        }
    }
}
