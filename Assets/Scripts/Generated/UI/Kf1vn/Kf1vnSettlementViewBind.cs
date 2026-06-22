// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnSettlementView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnSettlementViewBind : BaseView
    {
        public Image _bg;
        public Image settlement_state;
        public RectTransform ticket_conta;
        public Image _Image1;
        public TextMeshProUGUI ticket_label1;
        public TextMeshProUGUI ticket_label2;
        public RectTransform platform_conta;
        public Image _Image2;
        public TextMeshProUGUI platform_label1;
        public TextMeshProUGUI platform_label2;
        public TextMeshProUGUI tips;
        public TextMeshProUGUI tips0;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(settlement_state), settlement_state);
            EnsureBound(nameof(ticket_conta), ticket_conta);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(ticket_label1), ticket_label1);
            EnsureBound(nameof(ticket_label2), ticket_label2);
            EnsureBound(nameof(platform_conta), platform_conta);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(platform_label1), platform_label1);
            EnsureBound(nameof(platform_label2), platform_label2);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(tips0), tips0);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
