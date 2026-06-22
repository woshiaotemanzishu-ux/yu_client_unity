// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GCComSelectView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GCComSelectViewBind : BaseView
    {
        public Image bg;
        public Image item_bg;
        public TextMeshProUGUI tips_label;
        public ScrollRect _Scroller1;
        public TextMeshProUGUI needLb;
        public Image close_btn;
        public Image img_title;
        public TextMeshProUGUI lb_title;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(tips_label), tips_label);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(needLb), needLb);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(lb_title), lb_title);
        }
    }
}
