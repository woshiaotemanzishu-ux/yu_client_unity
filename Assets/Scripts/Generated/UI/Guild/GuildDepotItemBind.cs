// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/guild/GuildDepotItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Guild
{
    public partial class GuildDepotItemBind : BaseView
    {
        public RectTransform _group_item;
        public Image tip_down;
        public Image tip_up;
        public Image tip_compesite;
        public Image tip_ban;
        public Image selectBg;
        public RectTransform _box_click;
        public GameObject _tpl_EquipmentItem;
        public GameObject _tpl_CompositeEquipResolveView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(tip_down), tip_down);
            EnsureBound(nameof(tip_up), tip_up);
            EnsureBound(nameof(tip_compesite), tip_compesite);
            EnsureBound(nameof(tip_ban), tip_ban);
            EnsureBound(nameof(selectBg), selectBg);
            EnsureBound(nameof(_box_click), _box_click);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
            EnsureBound(nameof(_tpl_CompositeEquipResolveView), _tpl_CompositeEquipResolveView);
        }
    }
}
