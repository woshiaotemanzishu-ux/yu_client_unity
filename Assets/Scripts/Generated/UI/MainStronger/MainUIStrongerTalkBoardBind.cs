// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainStronger/MainUIStrongerTalkBoard.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainStronger
{
    public partial class MainUIStrongerTalkBoardBind : BaseView
    {
        public RectTransform content_conta;
        public Image content_bg;
        public TextMeshProUGUI content;
        public TextMeshProUGUI htmlContent;

        protected override void BindNodes()
        {
            EnsureBound(nameof(content_conta), content_conta);
            EnsureBound(nameof(content_bg), content_bg);
            EnsureBound(nameof(content), content);
            EnsureBound(nameof(htmlContent), htmlContent);
        }
    }
}
