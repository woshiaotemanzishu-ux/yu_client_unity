// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/UIJoyStick.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class UIJoyStickBind : BaseView
    {
        public RectTransform _gp_root;
        public Image _img_bg;
        public Image _img_arrow;
        public Image _img_middle_circle;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_gp_root), _gp_root);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_arrow), _img_arrow);
            EnsureBound(nameof(_img_middle_circle), _img_middle_circle);
        }
    }
}
