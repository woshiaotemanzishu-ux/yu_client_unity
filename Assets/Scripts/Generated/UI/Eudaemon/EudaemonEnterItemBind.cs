// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eudaemon/EudaemonEnterItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eudaemon
{
    public partial class EudaemonEnterItemBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _box_att;
        public Image _img_attention;
        public TextMeshProUGUI _lb_attention;
        public Image _img_bg3;
        public Image _img_icon;
        public Image _img_bg4;
        public TextMeshProUGUI _lb_name;
        public Image _img_select;
        public Image _img_active;
        public TextMeshProUGUI _lb_time;
        public Image _img_shop_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_box_att), _box_att);
            EnsureBound(nameof(_img_attention), _img_attention);
            EnsureBound(nameof(_lb_attention), _lb_attention);
            EnsureBound(nameof(_img_bg3), _img_bg3);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_bg4), _img_bg4);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_img_select), _img_select);
            EnsureBound(nameof(_img_active), _img_active);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_img_shop_red), _img_shop_red);
        }
    }
}
