// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/friend/FriendAddPopItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Friend
{
    public partial class FriendAddPopItemBind : BaseView
    {
        public RectTransform clickGroup;
        public Image _Image1;
        public TextMeshProUGUI _lb_name;
        public TextMeshProUGUI _lb_level;
        public TextMeshProUGUI _lb_fight;
        public Image _Image2;
        public RectTransform addBtn;
        public Image haveAdd;
        public Image _vip_icon;
        public RectTransform touchGroup;
        public RectTransform head;

        protected override void BindNodes()
        {
            EnsureBound(nameof(clickGroup), clickGroup);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(_lb_name), _lb_name);
            EnsureBound(nameof(_lb_level), _lb_level);
            EnsureBound(nameof(_lb_fight), _lb_fight);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(addBtn), addBtn);
            EnsureBound(nameof(haveAdd), haveAdd);
            EnsureBound(nameof(_vip_icon), _vip_icon);
            EnsureBound(nameof(touchGroup), touchGroup);
            EnsureBound(nameof(head), head);
        }
    }
}
