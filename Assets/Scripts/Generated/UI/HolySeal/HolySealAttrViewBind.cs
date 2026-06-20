// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/holySeal/HolySealAttrView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.HolySeal
{
    public partial class HolySealAttrViewBind : BaseView
    {
        public Image bg1;
        public Image _Image1;
        public TextMeshProUGUI _Label1;
        public ScrollRect _Scroller1;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_attr0;
        public TextMeshProUGUI _lb_attr1;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_attr0), _lb_attr0);
            EnsureBound(nameof(_lb_attr1), _lb_attr1);
        }
    }
}
