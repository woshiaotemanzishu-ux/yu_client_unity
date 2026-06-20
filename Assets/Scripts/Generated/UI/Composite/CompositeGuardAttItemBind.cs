// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeGuardAttItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeGuardAttItemBind : BaseView
    {
        public RectTransform gp_attr;
        public TextMeshProUGUI _lb_att;
        public TextMeshProUGUI lb_num;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_attr), gp_attr);
            EnsureBound(nameof(_lb_att), _lb_att);
            EnsureBound(nameof(lb_num), lb_num);
        }
    }
}
