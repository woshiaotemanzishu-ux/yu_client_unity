// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/dress/DressProItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Dress
{
    public partial class DressProItemBind : BaseView
    {
        public TextMeshProUGUI now_label;
        public TextMeshProUGUI next_label;
        public Image next_arrow;

        protected override void BindNodes()
        {
            EnsureBound(nameof(now_label), now_label);
            EnsureBound(nameof(next_label), next_label);
            EnsureBound(nameof(next_arrow), next_arrow);
        }
    }
}
