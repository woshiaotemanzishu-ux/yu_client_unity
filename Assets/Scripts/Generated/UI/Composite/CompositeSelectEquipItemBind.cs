// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeSelectEquipItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeSelectEquipItemBind : BaseView
    {
        public RectTransform item_group;
        public Image _img_wear;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(item_group), item_group);
            EnsureBound(nameof(_img_wear), _img_wear);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
