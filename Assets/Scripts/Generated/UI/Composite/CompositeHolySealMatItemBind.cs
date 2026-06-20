// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeHolySealMatItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeHolySealMatItemBind : BaseView
    {
        public RectTransform itemGp;
        public RectTransform gp_count;
        public TextMeshProUGUI _lb_count;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(itemGp), itemGp);
            EnsureBound(nameof(gp_count), gp_count);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
