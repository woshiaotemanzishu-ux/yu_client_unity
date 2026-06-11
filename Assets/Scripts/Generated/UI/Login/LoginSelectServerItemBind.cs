// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginSelectServerItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginSelectServerItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_mode;
        public Image _img_tips;
        public TextMeshProUGUI _lb_server_name;
        public Image _img_career;
        public TextMeshProUGUI _lb_level;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_mode), _img_mode);
            EnsureBound(nameof(_img_tips), _img_tips);
            EnsureBound(nameof(_lb_server_name), _lb_server_name);
            EnsureBound(nameof(_img_career), _img_career);
            EnsureBound(nameof(_lb_level), _lb_level);
        }
    }
}
