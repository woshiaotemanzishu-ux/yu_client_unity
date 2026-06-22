// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkBossEntryPoint.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkBossEntryPointBind : BaseView
    {
        public RectTransform clickArea;
        public TextMeshProUGUI lblPoint;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickArea), clickArea);
            EnsureBound(nameof(lblPoint), lblPoint);
        }
    }
}
