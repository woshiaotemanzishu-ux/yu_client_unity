// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipRefinement/EquipRefiAttrsItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipRefinement
{
    public partial class EquipRefiAttrsItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_lv;
        public RectTransform gp_attrs;
        public TextMeshProUGUI _lb_attrs;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_lv), _lb_lv);
            EnsureBound(nameof(gp_attrs), gp_attrs);
            EnsureBound(nameof(_lb_attrs), _lb_attrs);
        }
    }
}
