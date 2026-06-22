// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/advertising/AdvertisingItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Advertising
{
    public partial class AdvertisingItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_desc;
        public ScrollRect _panel_item;
        public Image _img_has_get;
        public RectTransform _box_look;
        public TextMeshProUGUI _lb_look;
        public RectTransform _box_get;
        public TextMeshProUGUI _img_get;
        public Image _img_get_red;
        public RectTransform _box_cool;
        public TextMeshProUGUI _lb_cool;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
            EnsureBound(nameof(_panel_item), _panel_item);
            EnsureBound(nameof(_img_has_get), _img_has_get);
            EnsureBound(nameof(_box_look), _box_look);
            EnsureBound(nameof(_lb_look), _lb_look);
            EnsureBound(nameof(_box_get), _box_get);
            EnsureBound(nameof(_img_get), _img_get);
            EnsureBound(nameof(_img_get_red), _img_get_red);
            EnsureBound(nameof(_box_cool), _box_cool);
            EnsureBound(nameof(_lb_cool), _lb_cool);
        }
    }
}
