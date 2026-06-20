// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/ChatToolPanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class ChatToolPanelBind : BaseView
    {
        public Image bg;
        public Image arrow;
        public ScrollRect itemScroller;
        public RectTransform Content;
        public GameObject _tpl_ChatToolGridItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bg), bg);
            EnsureBound(nameof(arrow), arrow);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_ChatToolGridItem), _tpl_ChatToolGridItem);
        }
    }
}
