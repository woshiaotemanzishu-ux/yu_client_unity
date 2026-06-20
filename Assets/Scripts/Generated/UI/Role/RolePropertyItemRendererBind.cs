// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/role/RolePropertyItemRenderer.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Role
{
    public partial class RolePropertyItemRendererBind : BaseView
    {
        public RectTransform property_group;
        public TextMeshProUGUI property_name;
        public TextMeshProUGUI property_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(property_group), property_group);
            EnsureBound(nameof(property_name), property_name);
            EnsureBound(nameof(property_value), property_value);
        }
    }
}
