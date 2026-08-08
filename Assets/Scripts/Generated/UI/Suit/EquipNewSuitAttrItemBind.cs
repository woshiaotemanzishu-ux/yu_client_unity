// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipNewSuitAttrItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipNewSuitAttrItemBind : BaseView
    {
        public TextMeshProUGUI numLab;
        public RectTransform combatBox;
        public TextMeshProUGUI attrHtml;

        protected override void BindNodes()
        {
            EnsureBound(nameof(numLab), numLab);
            EnsureBound(nameof(combatBox), combatBox);
            EnsureBound(nameof(attrHtml), attrHtml);
        }
    }
}
