// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginCreateRoleItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginCreateRoleItemBind : BaseView
    {
        public RectTransform _box_head;
        public Image _img_bg;
        public Image _img_icon;
        public TextMeshProUGUI _lb_career;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_head), _box_head);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_career), _lb_career);
        }
    }
}
