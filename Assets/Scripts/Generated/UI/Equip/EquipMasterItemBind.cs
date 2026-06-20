// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equip/EquipMasterItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Equip
{
    public partial class EquipMasterItemBind : BaseView
    {
        public RectTransform gp_msg;
        public TextMeshProUGUI lb_name;
        public TextMeshProUGUI lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_msg), gp_msg);
            EnsureBound(nameof(lb_name), lb_name);
            EnsureBound(nameof(lb_attr), lb_attr);
        }
    }
}
