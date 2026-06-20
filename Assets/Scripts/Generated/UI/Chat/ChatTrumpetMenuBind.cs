// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/chat/ChatTrumpetMenu.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Chat
{
    public partial class ChatTrumpetMenuBind : BaseView
    {
        public Image img_bg;
        public RectTransform btnGroup;
        public GameObject _tpl_ChatTrumpetMenuItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(img_bg), img_bg);
            EnsureBound(nameof(btnGroup), btnGroup);
            EnsureBound(nameof(_tpl_ChatTrumpetMenuItem), _tpl_ChatTrumpetMenuItem);
        }
    }
}
