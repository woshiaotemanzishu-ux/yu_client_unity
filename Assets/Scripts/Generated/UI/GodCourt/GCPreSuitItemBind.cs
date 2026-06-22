// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GCPreSuitItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GCPreSuitItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_suit;
        public TextMeshProUGUI attrLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_suit), _lb_suit);
            EnsureBound(nameof(attrLb), attrLb);
        }
    }
}
