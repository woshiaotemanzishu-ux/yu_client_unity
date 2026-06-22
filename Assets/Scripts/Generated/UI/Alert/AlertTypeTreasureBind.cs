// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/alert/AlertTypeTreasure.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Alert
{
    public partial class AlertTypeTreasureBind : BaseView
    {
        public Image bg;
        public Image _img_bg_1;
        public Image _img_title;
        public RectTransform _content;
        public TextMeshProUGUI _content_html;
        public Image _close_btn;
        public Image _cancel_btn;
        public TextMeshProUGUI cancel_label;
        public Image _ok_btn;
        public TextMeshProUGUI ok_label;
        public RectTransform check;
        public Image _img_bg;
        public TextMeshProUGUI _lb_1;
        public RectTransform activity_icon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_img_bg_1), _img_bg_1);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_content), _content);
            EnsureBound(nameof(_content_html), _content_html);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(_cancel_btn), _cancel_btn);
            EnsureBound(nameof(cancel_label), cancel_label);
            EnsureBound(nameof(_ok_btn), _ok_btn);
            EnsureBound(nameof(ok_label), ok_label);
            EnsureBound(nameof(check), check);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_lb_1), _lb_1);
            EnsureBound(nameof(activity_icon), activity_icon);
        }
    }
}
