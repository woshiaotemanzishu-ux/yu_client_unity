// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyResFindItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyResFindItemBind : BaseView
    {
        public Image bg;
        public TextMeshProUGUI title;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public RectTransform findBtn;
        public Image _Image11;
        public TextMeshProUGUI labelDisplay1;
        public RectTransform price_gp;
        public Image icon;
        public RectTransform cost_icon;
        public TextMeshProUGUI price;
        public RectTransform doneBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public TextMeshProUGUI free_lb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(title), title);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(findBtn), findBtn);
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(labelDisplay1), labelDisplay1);
            EnsureBound(nameof(price_gp), price_gp);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(cost_icon), cost_icon);
            EnsureBound(nameof(price), price);
            EnsureBound(nameof(doneBtn), doneBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(free_lb), free_lb);
        }
    }
}
