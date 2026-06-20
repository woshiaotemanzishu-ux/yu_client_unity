// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendBlackListItm.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendBlackListItmBind : BaseView
    {
        public RectTransform clickGroup;
        public Image _Image1;
        public RectTransform _Group1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI lb_online;
        public TextMeshProUGUI _lb_fight;
        public RectTransform btn_no;
        public TextMeshProUGUI labelDisplay;
        public RectTransform touchGroup;
        public RectTransform head;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickGroup), clickGroup);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(lb_online), lb_online);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(btn_no), btn_no);
            EnsureBound(nameof(labelDisplay), labelDisplay);
            EnsureBound(nameof(touchGroup), touchGroup);
            EnsureBound(nameof(head), head);
        }
    }
}
