// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dailySign/DailySignView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DailySign
{
    public partial class DailySignViewBind : BaseView
    {
        public Image bg;
        public Image bg1;
        public RectTransform _Group1;
        public Image _Image1;
        public Image _Image4;
        public TextMeshProUGUI reset_label;
        public Image _Image5;
        public TextMeshProUGUI retroactive_label;
        public Image bg2;
        public Image _Image6;
        public RectTransform _Group2;
        public TextMeshProUGUI _Label1;
        public TextMeshProUGUI timer_label;
        public Image bar_bg;
        public Image bar_heigh_light;
        public ScrollRect _data_scroller;
        public RectTransform bottom_group;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(bg1), bg1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image4), _Image4);
            EnsureBound(nameof(reset_label), reset_label);
            EnsureBound(nameof(_Image5), _Image5);
            EnsureBound(nameof(retroactive_label), retroactive_label);
            EnsureBound(nameof(bg2), bg2);
            EnsureBound(nameof(_Image6), _Image6);
            EnsureBound(nameof(_Group2), _Group2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(timer_label), timer_label);
            EnsureBound(nameof(bar_bg), bar_bg);
            EnsureBound(nameof(bar_heigh_light), bar_heigh_light);
            EnsureBound(nameof(_data_scroller), _data_scroller);
            EnsureBound(nameof(bottom_group), bottom_group);
        }
    }
}
