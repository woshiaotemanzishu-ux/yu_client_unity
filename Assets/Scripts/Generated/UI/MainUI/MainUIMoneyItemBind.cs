// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIMoneyItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIMoneyItemBind : BaseView
    {
        public Image _img_bg;
        public Image _img_icon;
        public Image _img_add;
        public TextMeshProUGUI _lb_value;
        public RectTransform _box_effect;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_img_icon), _img_icon);
            EnsureBound(nameof(_img_add), _img_add);
            EnsureBound(nameof(_lb_value), _lb_value);
            EnsureBound(nameof(_box_effect), _box_effect);
        }
    }
}
