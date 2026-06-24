// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/PropertyTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class PropertyTipsViewBind : BaseView
    {
        public Image _Image1;
        public ScrollRect _scroll_attr;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_attr;
        public RectTransform _gp_none_conta;
        public Image tips_icon;
        public TextMeshProUGUI tips;
        public RectTransform _Group2;
        public Image _Image2;
        public TextMeshProUGUI _lb_title;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_scroll_attr), _scroll_attr);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_attr), _lb_attr);
            EnsureBound(nameof(_gp_none_conta), _gp_none_conta);
            EnsureBound(nameof(tips_icon), tips_icon);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_title), _lb_title);
        }
    }
}
