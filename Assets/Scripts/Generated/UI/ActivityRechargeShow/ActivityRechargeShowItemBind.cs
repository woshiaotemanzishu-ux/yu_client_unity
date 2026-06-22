// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/activityRechargeShow/ActivityRechargeShowItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.ActivityRechargeShow
{
    public partial class ActivityRechargeShowItemBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _list_reward;
        public TextMeshProUGUI _lb_title;
        public RectTransform _btn;
        public Image _Image1;
        public TextMeshProUGUI labelDisplay;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_list_reward), _list_reward);
            EnsureBound(nameof(_lb_title), _lb_title);
            EnsureBound(nameof(_btn), _btn);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(labelDisplay), labelDisplay);
        }
    }
}
