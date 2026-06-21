// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/GiftPushIcon.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class GiftPushIconBind : BaseView
    {
        public RectTransform _gp_effect;
        public Image _img_icon;
        public Image _img_desc_bg;
        public TextMeshProUGUI _lb_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_desc_bg), _img_desc_bg);
            EnsureBound(nameof(_lb_desc), _lb_desc);
        }
    }
}
