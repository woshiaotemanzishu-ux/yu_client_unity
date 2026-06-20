// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/composite/CompositeEquipResolveItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Composite
{
    public partial class CompositeEquipResolveItemBind : BaseView
    {
        public Image bg;
        public RectTransform equip_con;
        public RectTransform click_bg;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(equip_con), equip_con);
            EnsureBound(nameof(click_bg), click_bg);
        }
    }
}
