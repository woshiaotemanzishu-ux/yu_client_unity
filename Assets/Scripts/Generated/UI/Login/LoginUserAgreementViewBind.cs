// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginUserAgreementView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginUserAgreementViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_close;
        public ScrollRect _panel_content;
        public TextMeshProUGUI _lb_content;
        public Image _img_xieyi;
        public Image _img_privacy;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_panel_content), _panel_content);
            EnsureBound(nameof(_lb_content), _lb_content);
            EnsureBound(nameof(_img_xieyi), _img_xieyi);
            EnsureBound(nameof(_img_privacy), _img_privacy);
        }
    }
}
