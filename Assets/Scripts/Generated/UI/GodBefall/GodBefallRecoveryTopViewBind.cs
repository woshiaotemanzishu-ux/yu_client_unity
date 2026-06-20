// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallRecoveryTopView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallRecoveryTopViewBind : BaseView
    {
        public Image img_icon;
        public Image img_title_bg;
        public Image img_title;
        public RectTransform box_property;
        public TextMeshProUGUI lable_lv_cur_name;
        public TextMeshProUGUI lable_lv_cur_value;
        public Image img_arrorw;
        public RectTransform box_next_lv;
        public TextMeshProUGUI lable_lv_next_name;
        public TextMeshProUGUI lable_lv_next_value;
        public RectTransform box_exp;
        public Image img_full;
        public Image img_exp_bg;
        public Image img_exp_bar;
        public RectTransform box_html;
        public TextMeshProUGUI html_exp_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_icon), img_icon);
            EnsureBound(nameof(img_title_bg), img_title_bg);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(box_property), box_property);
            EnsureBound(nameof(lable_lv_cur_name), lable_lv_cur_name);
            EnsureBound(nameof(lable_lv_cur_value), lable_lv_cur_value);
            EnsureBound(nameof(img_arrorw), img_arrorw);
            EnsureBound(nameof(box_next_lv), box_next_lv);
            EnsureBound(nameof(lable_lv_next_name), lable_lv_next_name);
            EnsureBound(nameof(lable_lv_next_value), lable_lv_next_value);
            EnsureBound(nameof(box_exp), box_exp);
            EnsureBound(nameof(img_full), img_full);
            EnsureBound(nameof(img_exp_bg), img_exp_bg);
            EnsureBound(nameof(img_exp_bar), img_exp_bar);
            EnsureBound(nameof(box_html), box_html);
            EnsureBound(nameof(html_exp_value), html_exp_value);
        }
    }
}
