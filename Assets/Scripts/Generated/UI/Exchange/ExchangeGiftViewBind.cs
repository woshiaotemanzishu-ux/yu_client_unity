// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/exchange/ExchangeGiftView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Exchange
{
    public partial class ExchangeGiftViewBind : BaseView
    {
        public Image _bg1;
        public Image _img_title;
        public TextMeshProUGUI _lb_url;
        public TextMeshProUGUI _lb_error;
        public RectTransform _btn_receive;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image _ti_input;
        public TMP_InputField _input_text;
        public TextMeshProUGUI Placeholder;
        public TextMeshProUGUI Text;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg1), _bg1);
            EnsureBound(nameof(_img_title), _img_title);
            EnsureBound(nameof(_lb_url), _lb_url);
            EnsureBound(nameof(_lb_error), _lb_error);
            EnsureBound(nameof(_btn_receive), _btn_receive);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(_ti_input), _ti_input);
            EnsureBound(nameof(_input_text), _input_text);
            EnsureBound(nameof(Placeholder), Placeholder);
            EnsureBound(nameof(Text), Text);
        }
    }
}
