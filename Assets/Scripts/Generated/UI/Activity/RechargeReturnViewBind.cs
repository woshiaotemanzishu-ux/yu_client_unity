// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activity/rechargeReturnView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Activity
{
    public partial class RechargeReturnViewBind : BaseView
    {
        public Image _img_1;
        public Image _img_2;
        public RectTransform _gp_btn;
        public Image _img_btn;
        public TextMeshProUGUI _lb_btn;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_1), _img_1);
            EnsureBound(nameof(_img_2), _img_2);
            EnsureBound(nameof(_gp_btn), _gp_btn);
            EnsureBound(nameof(_img_btn), _img_btn);
            EnsureBound(nameof(_lb_btn), _lb_btn);
        }
    }
}
