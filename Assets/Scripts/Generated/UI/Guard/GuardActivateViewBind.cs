// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guard/GuardActivateView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guard
{
    public partial class GuardActivateViewBind : BaseView
    {
        public Image img_bg;
        public Image img_title;
        public Image btn_active;
        public TextMeshProUGUI lb_active;
        public RectTransform gp_item;
        public Image btn_close;
        public GameObject _tpl_GuardActivateItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_title), img_title);
            EnsureBound(nameof(btn_active), btn_active);
            EnsureBound(nameof(lb_active), lb_active);
            EnsureBound(nameof(gp_item), gp_item);
            EnsureBound(nameof(btn_close), btn_close);
            EnsureBound(nameof(_tpl_GuardActivateItem), _tpl_GuardActivateItem);
        }
    }
}
