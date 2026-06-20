// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/svip/SvipMainItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Svip
{
    public partial class SvipMainItemBind : BaseView
    {
        public Image image_privilege_bg;
        public Image image_privilege;
        public TextMeshProUGUI html_content;
        public Image image_line;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image_privilege_bg), image_privilege_bg);
            EnsureBound(nameof(image_privilege), image_privilege);
            EnsureBound(nameof(html_content), html_content);
            EnsureBound(nameof(image_line), image_line);
        }
    }
}
