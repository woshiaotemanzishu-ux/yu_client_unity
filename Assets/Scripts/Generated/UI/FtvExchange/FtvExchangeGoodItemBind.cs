// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvExchange/FtvExchangeGoodItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvExchange
{
    public partial class FtvExchangeGoodItemBind : BaseView
    {
        public RectTransform _gp;
        public RectTransform _gp_item;
        public Image _img_goods;
        public TextMeshProUGUI _lb_count;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp), _gp);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_img_goods), _img_goods);
            EnsureBound(nameof(_lb_count), _lb_count);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
