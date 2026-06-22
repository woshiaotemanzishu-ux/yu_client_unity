// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kfStart/kfStageItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.KfStart
{
    public partial class KfStageItemBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_now;
        public TextMeshProUGUI _lb_next;
        public Image _btn_kf;
        public TextMeshProUGUI _lb_kf;
        public Image _img_icon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_now), _lb_now);
            EnsureBound(nameof(_lb_next), _lb_next);
            EnsureBound(nameof(_btn_kf), _btn_kf);
            EnsureBound(nameof(_lb_kf), _lb_kf);
            EnsureBound(nameof(_img_icon), _img_icon);
        }
    }
}
