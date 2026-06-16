// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/BaseAwardItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class BaseAwardItemBind : BaseView
    {
        public RectTransform click_group;
        public Image item_bg;
        public Image icon;
        public TextMeshProUGUI num_text;
        public Image @lock;
        public Image select_image;
        public RectTransform effect_con;
        public Image time_limit;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_group), click_group);
            EnsureBound(nameof(item_bg), item_bg);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(num_text), num_text);
            EnsureBound(nameof(@lock), @lock);
            EnsureBound(nameof(select_image), select_image);
            EnsureBound(nameof(effect_con), effect_con);
            EnsureBound(nameof(time_limit), time_limit);
        }
    }
}
