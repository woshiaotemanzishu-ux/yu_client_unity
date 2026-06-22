// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activity/DailySupplyView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Activity
{
    public partial class DailySupplyViewBind : BaseView
    {
        public Image _img_bg;
        public Image _img_bg2;
        public RectTransform _gp_time;
        public TextMeshProUGUI dailyLb;
        public TextMeshProUGUI tipsLabel;
        public Image _Image1;
        public ScrollRect _Scroller1;
        public GameObject _tpl_DailySupplyItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_bg2), _img_bg2);
            EnsureBound(nameof(_gp_time), _gp_time);
            EnsureBound(nameof(dailyLb), dailyLb);
            EnsureBound(nameof(tipsLabel), tipsLabel);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Scroller1), _Scroller1);
            EnsureBound(nameof(_tpl_DailySupplyItem), _tpl_DailySupplyItem);
        }
    }
}
