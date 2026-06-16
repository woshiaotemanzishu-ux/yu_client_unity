// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/TalkBoard.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class TalkBoardBind : BaseView
    {
        public RectTransform content_conta;
        public Image content_bg;
        public TextMeshProUGUI content;

        protected override void BindNodes()
        {
            EnsureBound(nameof(content_conta), content_conta);
            EnsureBound(nameof(content_bg), content_bg);
            EnsureBound(nameof(content), content);
        }
    }
}
