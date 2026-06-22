// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/alert/AlertTypeTwo.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Alert
{
    public partial class AlertTypeTwoBind : BaseView
    {
        public Image bg;
        public Image _title;
        public RectTransform _content;
        public TextMeshProUGUI _content_html;
        public Image _close_btn;
        public Image _cancel_btn;
        public TextMeshProUGUI cancel_label;
        public Image _ok_btn;
        public TextMeshProUGUI ok_label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(_title), _title);
            EnsureBound(nameof(_content), _content);
            EnsureBound(nameof(_content_html), _content_html);
            EnsureBound(nameof(_close_btn), _close_btn);
            EnsureBound(nameof(_cancel_btn), _cancel_btn);
            EnsureBound(nameof(cancel_label), cancel_label);
            EnsureBound(nameof(_ok_btn), _ok_btn);
            EnsureBound(nameof(ok_label), ok_label);
        }
    }
}
