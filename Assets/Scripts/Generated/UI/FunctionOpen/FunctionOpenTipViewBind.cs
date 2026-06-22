// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/functionOpen/FunctionOpenTipView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.FunctionOpen
{
    public partial class FunctionOpenTipViewBind : BaseView
    {
        public Image bg;
        public Image close_btn;
        public Image iconbg;
        public Image icon;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public TextMeshProUGUI tips;
        public Image _Image1;
        public Image _Image2;
        public TextMeshProUGUI condition;
        public Image line;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(close_btn), close_btn);
            EnsureBound(nameof(iconbg), iconbg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(tips), tips);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(condition), condition);
            EnsureBound(nameof(line), line);
        }
    }
}
