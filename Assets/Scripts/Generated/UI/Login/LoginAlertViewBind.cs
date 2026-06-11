// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginAlertView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginAlertViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public Image _img_title;
        public RectTransform _box_cancel;
        public Image _img_cancel;
        public TextMeshProUGUI _lb_cancel;
        public RectTransform _box_ok;
        public Image _img_ok;
        public TextMeshProUGUI _lb_ok;
        public Image _img_close;
        public TextMeshProUGUI _html_content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_box_cancel), _box_cancel);
            EnsureBound(nameof(_img_cancel), _img_cancel);
            EnsureBound(nameof(_lb_cancel), _lb_cancel);
            EnsureBound(nameof(_box_ok), _box_ok);
            EnsureBound(nameof(_img_ok), _img_ok);
            EnsureBound(nameof(_lb_ok), _lb_ok);
            EnsureBound(nameof(_img_close), _img_close);
            EnsureBound(nameof(_html_content), _html_content);
        }
    }
}
