// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/equip/EquipWashGoodsItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Equip
{
    public partial class EquipWashGoodsItemBind : BaseView
    {
        public RectTransform _Group2;
        public RectTransform _gp_tween;
        public Image _Image1;
        public TextMeshProUGUI _lb_dec;
        public RectTransform _gp_gooditem;
        public RectTransform _gp_item;
        public RectTransform _gp_awarditem;
        public Image _Image2;
        public RectTransform gp_num;
        public TextMeshProUGUI lb_num;
        public TextMeshProUGUI lb_need;
        public Image _img_select;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_gp_tween), _gp_tween);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_dec), _lb_dec);
            EnsureBound(nameof(_gp_gooditem), _gp_gooditem);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_gp_awarditem), _gp_awarditem);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(gp_num), gp_num);
            EnsureBound(nameof(lb_num), lb_num);
            EnsureBound(nameof(lb_need), lb_need);
            EnsureBound(nameof(_img_select), _img_select);
        }
    }
}
