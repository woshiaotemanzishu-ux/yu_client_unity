// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/revelation/RevelationSelectView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Revelation
{
    public partial class RevelationSelectViewBind : BaseView
    {
        public Image img_bg;
        public Image img_bg2;
        public Image img_title;
        public TextMeshProUGUI lb_title;
        public Image btn_close;
        public ScrollRect gp_items;
        public TextMeshProUGUI lb_nothing;
        public Image img_nothing;
        public TextMeshProUGUI lb_exp;
        public Image btn_dev;
        public Image img_redDev;
        public TextMeshProUGUI lb_dev;
        public RectTransform gp_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_bg2), img_bg2);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(gp_items), gp_items);
            EnsureBound(nameof(lb_nothing), lb_nothing);
            EnsureBound(nameof(img_nothing), img_nothing);
            EnsureBound(nameof(lb_exp), lb_exp);
            EnsureBound(nameof(btn_dev), btn_dev);
            EnsureBound(nameof(img_redDev), img_redDev);
            EnsureBound(nameof(lb_dev), lb_dev);
            EnsureBound(nameof(gp_effect), gp_effect);
        }
    }
}
