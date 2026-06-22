// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/jewel/EquipJewelCraveAttItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Jewel
{
    public partial class EquipJewelCraveAttItemBind : BaseView
    {
        public RectTransform gp_cur;
        public TextMeshProUGUI lb_curName;
        public TextMeshProUGUI lb_curValue;
        public RectTransform gp_next;
        public TextMeshProUGUI lb_nextName;
        public TextMeshProUGUI lb_nextValue;
        public Image img_arrow;
        public RectTransform effect_group;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_cur), gp_cur);
            EnsureBound(nameof(lb_curName), lb_curName);
            EnsureBound(nameof(lb_curValue), lb_curValue);
            EnsureBound(nameof(gp_next), gp_next);
            EnsureBound(nameof(lb_nextName), lb_nextName);
            EnsureBound(nameof(lb_nextValue), lb_nextValue);
            EnsureBound(nameof(img_arrow), img_arrow);
            EnsureBound(nameof(effect_group), effect_group);
        }
    }
}
