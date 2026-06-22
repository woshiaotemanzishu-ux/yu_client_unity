// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ftvActiveness/FtvActivenessTipsView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FtvActiveness
{
    public partial class FtvActivenessTipsViewBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image _btn_ok;
        public TextMeshProUGUI _title_lb;
        public RectTransform _gp_desc;
        public TextMeshProUGUI _lb_dec;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_btn_ok), _btn_ok);
            EnsureBound(nameof(_title_lb), _title_lb);
            EnsureBound(nameof(_gp_desc), _gp_desc);
            EnsureBound(nameof(_lb_dec), _lb_dec);
        }
    }
}
