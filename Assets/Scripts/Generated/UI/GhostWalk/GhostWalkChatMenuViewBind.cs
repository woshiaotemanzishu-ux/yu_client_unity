// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkChatMenuView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkChatMenuViewBind : BaseView
    {
        public Image img_bg;
        public Image btnGo;
        public TextMeshProUGUI lblGo;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(btnGo), btnGo);
            EnsureBound(nameof(lblGo), lblGo);
        }
    }
}
