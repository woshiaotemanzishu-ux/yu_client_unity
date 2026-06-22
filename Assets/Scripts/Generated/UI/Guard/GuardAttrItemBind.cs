// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guard/GuardAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guard
{
    public partial class GuardAttrItemBind : BaseView
    {
        public RectTransform gp_attr;
        public TextMeshProUGUI lb_name;
        public TextMeshProUGUI lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_attr), gp_attr);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(lb_attr), lb_attr);
        }
    }
}
