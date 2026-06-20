// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/fairyWish/FairyWishAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FairyWish
{
    public partial class FairyWishAttrItemBind : BaseView
    {
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_attr_name;
        public TextMeshProUGUI _lb_attr_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_attr_name), _lb_attr_name);
            EnsureBound(nameof(_lb_attr_value), _lb_attr_value);
        }
    }
}
