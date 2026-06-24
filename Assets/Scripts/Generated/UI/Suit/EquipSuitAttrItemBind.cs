// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipSuitAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipSuitAttrItemBind : BaseView
    {
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _lb_num;
        public TextMeshProUGUI attrText;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_lb_num), _lb_num);
            EnsureBound(nameof(attrText), attrText);
        }
    }
}
