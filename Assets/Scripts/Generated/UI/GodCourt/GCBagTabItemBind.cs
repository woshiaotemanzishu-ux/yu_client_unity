// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GCBagTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GCBagTabItemBind : BaseView
    {
        public Image _Image1;
        public Image _img_select;
        public TextMeshProUGUI nameLb;
        public Image redDot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(redDot), redDot);
        }
    }
}
