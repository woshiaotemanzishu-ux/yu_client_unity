// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/runeTreasure/RuneResultItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.RuneTreasure
{
    public partial class RuneResultItemBind : BaseView
    {
        public RectTransform _btn_click;
        public Image _img_icon;
        public Image _img_color;
        public TextMeshProUGUI _lb_msg1;
        public TextMeshProUGUI _lb_msg2;
        public TextMeshProUGUI _lb_msg3;
        public Image _img_bg;
        public RectTransform _gp_effect;
        public RectTransform _gp_anieff;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_btn_click), _btn_click);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_color), _img_color);
            EnsureBound(nameof(_lb_msg1), _lb_msg1);
            EnsureBound(nameof(_lb_msg2), _lb_msg2);
            EnsureBound(nameof(_lb_msg3), _lb_msg3);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_gp_effect), _gp_effect);
            EnsureBound(nameof(_gp_anieff), _gp_anieff);
        }
    }
}
