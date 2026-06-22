// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/attributePotion/attributePotionTab.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.AttributePotion
{
    public partial class AttributePotionTabBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public Image iconDisplay;
        public Image _red_dot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(iconDisplay), iconDisplay);
            EnsureBound(nameof(_red_dot), _red_dot);
        }
    }
}
