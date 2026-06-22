// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/kf1vn/Kf1vnQuizRadioItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Kf1vn
{
    public partial class Kf1vnQuizRadioItemBind : BaseView
    {
        public Image radio_img;
        public TextMeshProUGUI cost_num;
        public RectTransform cost_icon;
        public RectTransform _box_click;

        protected override void BindNodes()
        {
            EnsureBound(nameof(radio_img), radio_img);
            EnsureBound(nameof(cost_num), cost_num);
            EnsureBound(nameof(cost_icon), cost_icon);
            EnsureBound(nameof(_box_click), _box_click);
        }
    }
}
