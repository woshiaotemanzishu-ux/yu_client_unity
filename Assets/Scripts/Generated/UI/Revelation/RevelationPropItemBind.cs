// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/revelation/RevelationPropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Revelation
{
    public partial class RevelationPropItemBind : BaseView
    {
        public Image img_bg;
        public TextMeshProUGUI lb_count;
        public TextMeshProUGUI lb_prop;
        public Image img_tips;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(lb_count), lb_count);
            EnsureBound(nameof(lb_prop), lb_prop);
            EnsureBound(nameof(img_tips), img_tips);
        }
    }
}
