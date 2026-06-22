// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtRewardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtRewardViewBind : BaseView
    {
        public Image _img_bg1;
        public Image _img_bg2;
        public Image _btn_close;
        public Image _img_title;
        public TextMeshProUGUI _lb_title;
        public ScrollRect _item_scroller;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg1), _img_bg1);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_btn_close), _btn_close);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_item_scroller), _item_scroller);
        }
    }
}
