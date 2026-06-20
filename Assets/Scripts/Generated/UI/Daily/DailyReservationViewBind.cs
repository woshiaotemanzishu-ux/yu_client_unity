// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/daily/DailyReservationView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Daily
{
    public partial class DailyReservationViewBind : BaseView
    {
        public Image _Image11;
        public Image _Image2;
        public RectTransform _Group1;
        public Image _Image3;
        public TextMeshProUGUI title_img;
        public RectTransform enterBtn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;
        public Image closeBtn;
        public TextMeshProUGUI nameLb;
        public TextMeshProUGUI startLb;
        public ScrollRect _Scroller1;
        public RectTransform Content;
        public GameObject _tpl_BaseAwardItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image11), _Image11);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image3), _Image3);
            EnsureBound(nameof(title_img), title_img);
            EnsureBound(nameof(enterBtn), enterBtn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(closeBtn), closeBtn);
            EnsureBound(nameof(nameLb), nameLb);
            EnsureBound(nameof(startLb), startLb);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
        }
    }
}
