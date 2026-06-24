// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/LinkViewItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class LinkViewItemBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI targetName;
        public RectTransform btn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay;
        public Image _Image2;
        public Image icon;
        public Image @double;
        public TextMeshProUGUI double_time;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(targetName), targetName);
            EnsureBound(nameof(btn), btn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(@double), @double);
            EnsureBound(nameof(double_time), double_time);
        }
    }
}
