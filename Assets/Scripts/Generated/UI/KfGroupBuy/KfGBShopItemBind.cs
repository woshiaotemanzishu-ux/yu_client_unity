// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfGroupBuy/KfGBShopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfGroupBuy
{
    public partial class KfGBShopItemBind : BaseView
    {
        public RectTransform _gp_item;
        public Image img_select;
        public Image _title0;
        public Image title1;
        public Image _red;
        public TextMeshProUGUI lable_order_value;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(img_select), img_select);
            EnsureBound(nameof(_title0), _title0);
            EnsureBound(nameof(title1), title1);
            EnsureBound(nameof(_red), _red);
            EnsureBound(nameof(lable_order_value), lable_order_value);
        }
    }
}
