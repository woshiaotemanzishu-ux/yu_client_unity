// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtPropView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtPropViewBind : BaseView
    {
        public Image img_bg;
        public Image _img_bg2;
        public Image img_top;
        public TextMeshProUGUI lb_title;
        public ScrollRect gp_attr;
        public RectTransform gp_content;
        public TextMeshProUGUI _lb_attr0;
        public TextMeshProUGUI _lb_attr1;
        public TextMeshProUGUI lb_nothing;
        public Image _btn_close;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(img_top), img_top);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(gp_attr), gp_attr);
            EnsureBound(nameof(gp_content), gp_content);
            EnsureBound(nameof(_lb_attr0), _lb_attr0);
            EnsureBound(nameof(_lb_attr1), _lb_attr1);
            EnsureBound(nameof(lb_nothing), lb_nothing);
            EnsureBound(nameof(_btn_close), _btn_close);
        }
    }
}
