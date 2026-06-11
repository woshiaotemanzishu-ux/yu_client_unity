// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/login/LoginCreatRoleVideoView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Login
{
    public partial class LoginCreatRoleVideoViewBind : BaseView
    {
        public Image _img_bg;
        public RectTransform _box_video;
        public RectTransform _box_skip;
        public Image _img_skip;
        public TextMeshProUGUI _lb_skip;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_box_video), _box_video);
            EnsureBound(nameof(_box_skip), _box_skip);
            EnsureBound(nameof(_img_skip), _img_skip);
            EnsureBound(nameof(_lb_skip), _lb_skip);
        }
    }
}
