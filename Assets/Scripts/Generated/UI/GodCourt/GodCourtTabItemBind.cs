// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtTabItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI nameLb;
        public TextMeshProUGUI lockLb;
        public Image redDot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(lockLb), lockLb);
            EnsureBound(nameof(redDot), redDot);
        }
    }
}
