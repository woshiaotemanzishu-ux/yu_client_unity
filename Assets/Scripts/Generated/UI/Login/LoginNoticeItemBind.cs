// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginNoticeItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginNoticeItemBind : BaseView
    {
        public Image _img_lb;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_title;
        public RectTransform _gp_details;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_lb), _img_lb);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_gp_details), _gp_details);
        }
    }
}
