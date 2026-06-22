// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dailylogin/DailyLogInView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dailylogin
{
    public partial class DailyLogInViewBind : BaseView
    {
        public Image _img_top_title;
        public RectTransform _gp_time;
        public TextMeshProUGUI _lb_time;
        public ScrollRect _scroller;
        public RectTransform content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_top_title), _img_top_title);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(_lb_time), _lb_time);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(content), content);
        }
    }
}
