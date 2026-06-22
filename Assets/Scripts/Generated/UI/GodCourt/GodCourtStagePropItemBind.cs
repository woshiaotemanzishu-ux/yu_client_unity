// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtStagePropItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtStagePropItemBind : BaseView
    {
        public TextMeshProUGUI curLb;
        public Image arrow;
        public TextMeshProUGUI nextLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(curLb), curLb);
            EnsureBound(nameof(arrow), arrow);
            EnsureBound(nameof(nextLb), nextLb);
        }
    }
}
