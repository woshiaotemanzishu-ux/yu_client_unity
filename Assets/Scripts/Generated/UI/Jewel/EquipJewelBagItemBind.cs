// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/jewel/EquipJewelBagItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Jewel
{
    public partial class EquipJewelBagItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform _gp_item;
        public Image _img_now;
        public TextMeshProUGUI _lb_name0;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_attr;
        public Image _reddot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_gp_item), _gp_item);
            EnsureBound(nameof(_img_now), _img_now);
            EnsureBound(nameof(_lb_name0), _lb_name0);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_attr), _lb_attr);
            EnsureBound(nameof(_reddot), _reddot);
        }
    }
}
