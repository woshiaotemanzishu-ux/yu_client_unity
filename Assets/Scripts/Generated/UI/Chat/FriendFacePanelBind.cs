// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/FriendFacePanel.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class FriendFacePanelBind : BaseView
    {
        public Image _Image1;
        public Image _Image2;
        public ScrollRect itemScroller;
        public RectTransform Content;
        public GameObject _tpl_ChatToolGridItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(itemScroller), itemScroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_ChatToolGridItem), _tpl_ChatToolGridItem);
        }
    }
}
