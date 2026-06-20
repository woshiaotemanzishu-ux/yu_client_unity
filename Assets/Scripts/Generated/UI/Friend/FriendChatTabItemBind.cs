// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendChatTabItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendChatTabItemBind : BaseView
    {
        public Image click;
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public RectTransform _gp_redDot;
        public Image _Image2;
        public TextMeshProUGUI _lb_num;
        public RectTransform head;

        protected override void BindNodes()
        {
            EnsureBound(nameof(click), click);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_gp_redDot), _gp_redDot);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_lb_num), _lb_num);
            EnsureBound(nameof(head), head);
        }
    }
}
