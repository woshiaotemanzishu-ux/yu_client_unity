// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equip/EquipAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Equip
{
    public partial class EquipAttrItemBind : BaseView
    {
        public RectTransform gp_now;
        public TextMeshProUGUI name;
        public TextMeshProUGUI attr;
        public TextMeshProUGUI up;

        protected override void BindNodes()
        {
            EnsureBound(nameof(gp_now), gp_now);
            EnsureBound(nameof(name), name);
            EnsureBound(nameof(attr), attr);
            EnsureBound(nameof(up), up);
        }
    }
}
