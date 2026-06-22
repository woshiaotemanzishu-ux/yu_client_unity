// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dungeonPartner/DungeonPartnerVsRewardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DungeonPartner
{
    public partial class DungeonPartnerVsRewardItemBind : BaseView
    {
        public RectTransform _box_item;
        public Image _img_get;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_item), _box_item);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
