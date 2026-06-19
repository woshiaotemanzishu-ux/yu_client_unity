// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/uiComponent/ProgressBar2.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.UiComponent
{
    public partial class ProgressBar2Bind : BaseView
    {
        public Image _img_bg;
        public Image _img_front;
        public TextMeshProUGUI _lb_content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_front), _img_front);
            EnsureBound(nameof(_lb_content), _lb_content);
        }
    }
}
