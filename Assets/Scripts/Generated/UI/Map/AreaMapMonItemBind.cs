// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/map/AreaMapMonItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Map
{
    public partial class AreaMapMonItemBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public Image mon_icon;
        public TextMeshProUGUI level;
        public TextMeshProUGUI desc;
        public RectTransform click_bg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(mon_icon), mon_icon);
            EnsureBound(nameof(level), level);
            EnsureBound(nameof(desc), desc);
            EnsureBound(nameof(click_bg), click_bg);
        }
    }
}
