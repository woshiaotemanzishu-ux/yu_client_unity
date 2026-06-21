// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/godCourt/GodCourtSuitSingle.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GodCourt
{
    public partial class GodCourtSuitSingleBind : BaseView
    {
        public Image bg;
        public TextMeshProUGUI suitName;
        public TextMeshProUGUI suitLb;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(suitName), suitName);
            EnsureBound(nameof(suitLb), suitLb);
        }
    }
}
