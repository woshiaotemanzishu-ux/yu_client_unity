// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/vip/VipTabButton.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Vip
{
    public partial class VipTabButtonBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI labelDisplay;
        public RectTransform _Group1;
        public Image vip_red;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(vip_red), vip_red);
        }
    }
}
