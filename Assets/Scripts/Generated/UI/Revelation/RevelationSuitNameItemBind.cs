// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/revelation/RevelationSuitNameItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Revelation
{
    public partial class RevelationSuitNameItemBind : BaseView
    {
        public Image img_bg;
        public TextMeshProUGUI lb_tips;
        public Image img_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(lb_tips), lb_tips);
            EnsureBound(nameof(img_red), img_red);
        }
    }
}
