// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dragonWhisper/dwAttrView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DragonWhisper
{
    public partial class DwAttrViewBind : BaseView
    {
        public Image img_bg;
        public Image img_top;
        public TextMeshProUGUI lb_title;
        public RectTransform gp_content;
        public TextMeshProUGUI _lb_attr;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(img_top), img_top);
            EnsureBound(nameof(lb_title), lb_title);
            EnsureBound(nameof(gp_content), gp_content);
            EnsureBound(nameof(_lb_attr), _lb_attr);
        }
    }
}
