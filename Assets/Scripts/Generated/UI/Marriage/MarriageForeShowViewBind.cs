// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/marriage/MarriageForeShowView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Marriage
{
    public partial class MarriageForeShowViewBind : BaseView
    {
        public Image _bg;
        public RectTransform _box;
        public Image bg;
        public Image close;
        public RectTransform _btn;
        public Image _img_btn;
        public TextMeshProUGUI _lb_text;
        public TextMeshProUGUI _time_down;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_bg), _bg);
            EnsureBound(nameof(_box), _box);
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(close), close);
            EnsureBound(nameof(_btn), _btn);
            EnsureBound(nameof(_img_btn), _img_btn);
            EnsureBound(nameof(_lb_text), _lb_text);
            EnsureBound(nameof(_time_down), _time_down);
        }
    }
}
