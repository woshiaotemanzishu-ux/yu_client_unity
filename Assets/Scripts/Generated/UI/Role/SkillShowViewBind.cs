// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/role/SkillShowView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Role
{
    public partial class SkillShowViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_icon;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_desc), _lb_desc);
        }
    }
}
