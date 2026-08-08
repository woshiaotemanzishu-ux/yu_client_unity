// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/innateSkill/InnateInfoItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.InnateSkill
{
    public partial class InnateInfoItemBind : BaseView
    {
        public ScrollRect _scr_dec;
        public RectTransform _gp_scr;
        public Image _Image1;
        public Image _img_mask;
        public Image _img_icon;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_lv;
        public Image _img_arrow_left;
        public TextMeshProUGUI _lb_cur_info;
        public RectTransform _gp_mask;
        public ScrollRect _lb_panel;
        public RectTransform _gp_dec;
        public Image _img_arrow_right;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_scr_dec), _scr_dec);
            EnsureBound(nameof(_gp_scr), _gp_scr);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_mask), _img_mask);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(_img_arrow_left), _img_arrow_left);
            EnsureBound(nameof(_lb_cur_info), _lb_cur_info);
            EnsureBound(nameof(_gp_mask), _gp_mask);
            EnsureBound(nameof(_lb_panel), _lb_panel);
            EnsureBound(nameof(_gp_dec), _gp_dec);
            EnsureBound(nameof(_img_arrow_right), _img_arrow_right);
        }
    }
}
