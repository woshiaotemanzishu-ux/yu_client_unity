// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/eyou/EyouAttentionItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Eyou
{
    public partial class EyouAttentionItemBind : BaseView
    {
        public RectTransform _box_con;
        public Image _img_bg;
        public TextMeshProUGUI _lb_title;
        public TextMeshProUGUI _html_desc;
        public Image _img_icon;
        public ScrollRect _panel_reward;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_con), _box_con);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_html_desc), _html_desc);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_panel_reward), _panel_reward);
        }
    }
}
