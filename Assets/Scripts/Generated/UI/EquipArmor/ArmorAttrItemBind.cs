// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equipArmor/ArmorAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.EquipArmor
{
    public partial class ArmorAttrItemBind : BaseView
    {
        public RectTransform gp_attr;
        public TextMeshProUGUI attr;
        public TextMeshProUGUI up;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_attr), gp_attr);
            EnsureBound(nameof(attr), attr);
            EnsureBound(nameof(up), up);
        }
    }
}
