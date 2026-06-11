// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginBgView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginBgViewBind : BaseView
    {
        public Image _img_bg;
        public TextMeshProUGUI _lb_version;
        public TextMeshProUGUI _lb_down;
        public TextMeshProUGUI _lb_copy_right;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_version), _lb_version);
            EnsureBound(nameof(_lb_down), _lb_down);
            EnsureBound(nameof(_lb_copy_right), _lb_copy_right);
        }
    }
}
