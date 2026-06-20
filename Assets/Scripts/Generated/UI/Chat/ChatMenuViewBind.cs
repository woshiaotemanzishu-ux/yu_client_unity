// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/ChatMenuView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class ChatMenuViewBind : BaseView
    {
        public Image img_bg;
        public RectTransform _Group1;
        public ScrollRect _scroller;
        public RectTransform Content;
        public GameObject _tpl_FriendViewButtonSkin;
        public GameObject _tpl_MenuRedButtonSkin;
        public GameObject _tpl_MarketPlzShowItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_scroller), _scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_tpl_FriendViewButtonSkin), _tpl_FriendViewButtonSkin);
            EnsureBound(nameof(_tpl_MenuRedButtonSkin), _tpl_MenuRedButtonSkin);
            EnsureBound(nameof(_tpl_MarketPlzShowItem), _tpl_MarketPlzShowItem);
        }
    }
}
