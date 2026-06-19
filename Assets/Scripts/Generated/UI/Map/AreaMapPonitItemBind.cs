// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/map/AreaMapPonitItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Map
{
    public partial class AreaMapPonitItemBind : BaseView
    {
        public Image Image;
        public Image point;
        public TextMeshProUGUI desc;

        protected override void BindNodes()
        {
            EnsureBound(nameof(Image), Image);
            EnsureBound(nameof(point), point);
            EnsureBound(nameof(desc), desc);
        }
    }
}
