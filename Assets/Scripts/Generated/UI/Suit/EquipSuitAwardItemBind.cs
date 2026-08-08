// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/suit/EquipSuitAwardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Suit
{
    public partial class EquipSuitAwardItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image0;
        public Image item_bg;
        public Image _img_icon;
        public Image Image;
        public Image _img_select;
        public TextMeshProUGUI _lb_suit;
        public TextMeshProUGUI _lb_count;
        public Image _reddot;
        public RectTransform _group_item;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image0), _Image0);
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_lb_suit), _lb_suit);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_reddot), _reddot);
            EnsureBound(nameof(_group_item), _group_item);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
