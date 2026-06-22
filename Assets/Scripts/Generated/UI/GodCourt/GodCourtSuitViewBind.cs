// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtSuitView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtSuitViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image closeBtn;
        public Image _img_title;
        public TextMeshProUGUI lb_title;
        public ScrollRect _Scroller1;
        public GameObject _tpl_GCPreSuitItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_GCPreSuitItem), _tpl_GCPreSuitItem);
        }
    }
}
