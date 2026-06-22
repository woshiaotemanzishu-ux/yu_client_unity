// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/destinyTurntable/DestinyTurntableIcon.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.DestinyTurntable
{
    public partial class DestinyTurntableIconBind : BaseView
    {
        public RectTransform click_gp;
        public Image Image1;
        public Image icon;
        public Image other;
        public Image tips;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click_gp), click_gp);
            EnsureBound(nameof(Image1), Image1);
            EnsureBound(nameof(icon), icon);
            EnsureBound(nameof(other), other);
            EnsureBound(nameof(tips), tips);
        }
    }
}
