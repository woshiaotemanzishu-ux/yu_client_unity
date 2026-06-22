// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/adventure/AdventureItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Adventure
{
    public partial class AdventureItemBind : BaseView
    {
        public Image _bg;
        public Image _box;
        public RectTransform _item;
        public RectTransform _gp_crit;
        public RectTransform _reward_proview;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(_item), _item);
            EnsureBound(nameof(_gp_crit), _gp_crit);
            EnsureBound(nameof(_reward_proview), _reward_proview);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
