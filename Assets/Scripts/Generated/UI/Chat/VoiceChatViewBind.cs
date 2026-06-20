// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/VoiceChatView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class VoiceChatViewBind : BaseView
    {
        public Image image;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI text;

        protected override void BindNodes()
        {
            EnsureBound(nameof(image), image);
            EnsureBound(nameof(timeText), timeText);
            EnsureBound(nameof(text), text);
        }
    }
}
