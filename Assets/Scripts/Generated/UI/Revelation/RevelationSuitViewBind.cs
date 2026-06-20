// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/revelation/RevelationSuitView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Revelation
{
    public partial class RevelationSuitViewBind : BaseView
    {
        public Image img_bg1;
        public Image img_bg2;
        public Image img_title;
        public TextMeshProUGUI lb_title;
        public Image btn_close;
        public Image img_bg3;
        public Image img_title2;
        public Image img_title3;
        public TextMeshProUGUI lb_tips;
        public ScrollRect gp_tabs;
        public ScrollRect gp_suit;
        public RectTransform gp_suitCont;
        public RectTransform gp_model;
        public RectTransform gp_use;
        public TextMeshProUGUI lb_use;
        public Image img_bg4;
        public Image img_use;
        public Image img_ins;
        public Image img_empty;
        public GameObject _tpl_RevelationPropItem;
        public GameObject _tpl_RevelationSuitNameItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg1), img_bg1);
            EnsureBound(nameof(img_bg2), img_bg2);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(img_bg3), img_bg3);
            EnsureBound(nameof(img_title2), img_title2);
            EnsureBound(nameof(img_title3), img_title3);
            EnsureBound(nameof(lb_tips), lb_tips);
            EnsureBound(nameof(gp_tabs), gp_tabs);
            EnsureBound(nameof(gp_suit), gp_suit);
            EnsureBound(nameof(gp_suitCont), gp_suitCont);
            EnsureBound(nameof(gp_model), gp_model);
            EnsureBound(nameof(gp_use), gp_use);
            EnsureBound(nameof(lb_use), lb_use);
            EnsureBound(nameof(img_bg4), img_bg4);
            EnsureBound(nameof(img_use), img_use);
            EnsureBound(nameof(img_ins), img_ins);
            EnsureBound(nameof(img_empty), img_empty);
            EnsureBound(nameof(_tpl_RevelationPropItem), _tpl_RevelationPropItem);
            EnsureBound(nameof(_tpl_RevelationSuitNameItem), _tpl_RevelationSuitNameItem);
        }
    }
}
