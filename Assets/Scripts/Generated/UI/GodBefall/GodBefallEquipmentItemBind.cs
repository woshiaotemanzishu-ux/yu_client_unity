// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godBefall/GodBefallEquipmentItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodBefall
{
    public partial class GodBefallEquipmentItemBind : BaseView
    {
        public RectTransform box;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(box), box);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
