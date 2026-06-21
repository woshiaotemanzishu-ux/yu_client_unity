// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/vip/VipPrivilegeCardView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Vip
{
    public partial class VipPrivilegeCardViewBind : BaseView
    {
        public RectTransform _Group1;
        public Image bg_di;
        public ScrollRect card_scroller;
        public RectTransform Content;
        public Image _Image1;
        public Image _Image2;
        public Image _Image3;
        public Image _Image4;
        public TextMeshProUGUI tip_label;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(bg_di), bg_di);
            EnsureBound(nameof(card_scroller), card_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(tip_label), tip_label);
        }
    }
}
