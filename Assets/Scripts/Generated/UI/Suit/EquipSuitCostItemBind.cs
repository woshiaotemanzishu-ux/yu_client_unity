// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipSuitCostItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipSuitCostItemBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _group_item;
        public TextMeshProUGUI num_text;
        public Image lockImg;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(num_text), num_text);
            EnsureBound(nameof(lockImg), lockImg);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
