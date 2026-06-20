// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/GodBefallButton.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class GodBefallButtonBind : BaseView
    {
        public Image _img_up;
        public Image _img_down;
        public TextMeshProUGUI _label;
        public Image red_dot;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_up), _img_up);
            EnsureBound(nameof(_img_down), _img_down);
            EnsureBound(nameof(_label), _label);
            EnsureBound(nameof(red_dot), red_dot);
        }
    }
}
