// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/petEquip/PetEquipItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.PetEquip
{
    public partial class PetEquipItemBind : BaseView
    {
        public RectTransform _click;
        public RectTransform _group_data;
        public RectTransform _group_item;
        public RectTransform _group_left;
        public Image _img_color;
        public TextMeshProUGUI _lb_star;
        public Image _Image1;
        public Image _img_icon;
        public RectTransform _group_empty;
        public Image _Image2;
        public Image _reddot;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_click), _click);
            EnsureBound(nameof(_group_data), _group_data);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_group_left), _group_left);
            EnsureBound(nameof(_img_color), _img_color);
            EnsureBound(nameof(_lb_star), _lb_star);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_group_empty), _group_empty);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_reddot), _reddot);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
