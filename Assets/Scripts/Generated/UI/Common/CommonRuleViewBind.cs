// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/CommonRuleView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class CommonRuleViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI titleLabel;
        public Image closeBtn;
        public ScrollRect scroll;
        public RectTransform Content;
        public GameObject _tpl_CommonRuleItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(titleLabel), titleLabel);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(scroll), scroll);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_CommonRuleItem), _tpl_CommonRuleItem);
        }
    }
}
