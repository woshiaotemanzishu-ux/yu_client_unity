// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipRefinement/EquipRefiUpTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipRefinement
{
    public partial class EquipRefiUpTabItemBind : BaseView
    {
        public Image img_bg;
        public TextMeshProUGUI lb_name;
        public TextMeshProUGUI lb_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(lb_value), lb_value);
        }
    }
}
