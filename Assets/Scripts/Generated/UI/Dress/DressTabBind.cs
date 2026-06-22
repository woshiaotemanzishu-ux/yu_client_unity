// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dress/DressTab.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dress
{
    public partial class DressTabBind : BaseView
    {
        public Image _Image1;
        public TextMeshProUGUI _lb;
        public RectTransform _Group1;
        public Image iconDisplay;
        public Image redDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb), _lb);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(iconDisplay), iconDisplay);
            EnsureBound(nameof(redDisplay), redDisplay);
        }
    }
}
